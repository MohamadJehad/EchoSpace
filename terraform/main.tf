# Terraform Configuration
terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

# Azure Provider Configuration
# Subscription ID comes from variable or Azure CLI default
provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }

  # Use subscription_id from variable if provided, otherwise use Azure CLI default
  subscription_id = var.subscription_id != "" ? var.subscription_id : null

  # Skip automatic provider registration to avoid timeouts
  skip_provider_registration = false
}

# Create resource group (or use existing)
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  tags = merge(var.common_tags, {
    Name = var.resource_group_name
  })
}

# Use the resource group (created above or existing)
locals {
  resource_group_name = azurerm_resource_group.main.name
  resource_group_id   = azurerm_resource_group.main.id
}

# OPTION 1: If you have an existing service plan, uncomment and use this data source instead
# Replace "your-existing-plan-name" with the actual name of your existing service plan
# data "azurerm_service_plan" "existing" {
#   name                = "your-existing-plan-name"
#   resource_group_name = data.azurerm_resource_group.echospace.name
# }

# OPTION 2: Create a new service plan (requires quota - see error below)
# NOTE: Your subscription has no quota for App Service Plans in eastus region
# To resolve this, you need to request a quota increase:
# 1. Go to Azure Portal -> Subscriptions -> Your Subscription -> Usage + quotas
# 2. Search for "App Service Plans" or "App Service (Linux)"
# 3. Click "Request increase" and request quota for Free or Basic tier
# 4. Wait for approval (usually takes a few hours to a day)
# 
# Alternatively, if you have an existing service plan in this resource group,
# use OPTION 1 above and reference it instead of creating a new one.
resource "azurerm_service_plan" "shared" {
  name                = "${var.app_name}-shared-plan-${var.environment}"
  location            = var.location
  resource_group_name = local.resource_group_name
  os_type             = "Linux"
  sku_name            = var.app_service_plan_sku

  tags = merge(var.common_tags, {
    Name = "${var.app_name}-shared-plan"
  })
}

# Angular web app (using Node.js runtime for Angular)
resource "azurerm_linux_web_app" "angular" {
  name                = var.frontend_app_name
  location            = var.location
  resource_group_name = local.resource_group_name
  service_plan_id     = azurerm_service_plan.shared.id

  # Security: Enforce HTTPS only
  https_only = var.enable_https_only

  # Enable Managed Identity for Key Vault access
  identity {
    type = var.enable_managed_identity ? "SystemAssigned" : null
  }

  site_config {
    always_on = var.app_service_plan_sku != "F1" ? true : false # Free tier doesn't support always_on

    # Security: TLS version
    minimum_tls_version = var.minimum_tls_version

    application_stack {
      node_version = "20-lts"
    }

    # Startup command for serving static Angular files
    app_command_line = "npm start"
  }

  app_settings = {
    "WEBSITE_NODE_DEFAULT_VERSION"   = "~20"
    "SCM_DO_BUILD_DURING_DEPLOYMENT" = "false" # Disable build - we deploy pre-built static files
    "ASPNETCORE_ENVIRONMENT"         = var.environment
  }

  tags = merge(var.common_tags, {
    Name        = var.frontend_app_name
    Component   = "Frontend"
    Environment = var.environment
  })
}

# .NET 9 backend web app
resource "azurerm_linux_web_app" "backend" {
  name                = var.backend_app_name
  location            = var.location
  resource_group_name = local.resource_group_name
  service_plan_id     = azurerm_service_plan.shared.id

  # Security: Enforce HTTPS only
  https_only = var.enable_https_only

  # Enable Managed Identity for Key Vault access
  identity {
    type = var.enable_managed_identity ? "SystemAssigned" : null
  }

  site_config {
    always_on = var.app_service_plan_sku != "F1" ? true : false # Free tier doesn't support always_on

    # Security: TLS version
    minimum_tls_version = var.minimum_tls_version

    application_stack {
      dotnet_version = "8.0" # Azure App Service supports up to 8.0
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = var.environment
    # Connection string for SQL Database
    "ConnectionStrings__DefaultConnection" = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    # Storage account connection string
    "AzureStorage__ConnectionString" = azurerm_storage_account.main.primary_connection_string
  }

  tags = merge(var.common_tags, {
    Name        = var.backend_app_name
    Component   = "Backend"
    Environment = var.environment
  })
}

# Azure SQL Server (cheap tier - Basic)
resource "azurerm_mssql_server" "main" {
  name                         = "${var.sql_server_name}-${var.environment}"
  resource_group_name          = local.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"

  tags = merge(var.common_tags, {
    Name        = "${var.sql_server_name}-${var.environment}"
    Component   = "Database"
    Environment = var.environment
  })
}

# Azure SQL Database (cheap tier - Basic)
resource "azurerm_mssql_database" "main" {
  name           = var.sql_database_name
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 2       # Basic tier: 2GB max
  sku_name       = "Basic" # Cheapest tier
  zone_redundant = false

  tags = merge(var.common_tags, {
    Name        = var.sql_database_name
    Component   = "Database"
    Environment = var.environment
  })
}

# SQL Server Firewall Rule - Allow Azure Services
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Storage Account for Blob Storage (cheap tier - Standard_LRS)
resource "azurerm_storage_account" "main" {
  name                     = lower(replace("${var.storage_account_name}${var.environment}", "-", ""))
  resource_group_name      = local.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS" # Locally redundant - cheapest option
  min_tls_version          = "TLS1_2"

  # Storage account name must be 3-24 chars, lowercase, alphanumeric only
  # This ensures it's valid

  # Enable blob storage
  blob_properties {
    delete_retention_policy {
      days = 7
    }
  }

  tags = merge(var.common_tags, {
    Name        = "${var.storage_account_name}-${var.environment}"
    Component   = "Storage"
    Environment = var.environment
  })
}

# Blob Container for application files
resource "azurerm_storage_container" "app_files" {
  name                  = "app-files"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

# Blob Container for user uploads
resource "azurerm_storage_container" "user_uploads" {
  name                  = "user-uploads"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

# Key Vault (optional - enable when subscription is available)
resource "azurerm_key_vault" "main" {
  count = var.enable_key_vault ? 1 : 0

  name                = "${var.key_vault_name}-${var.environment}"
  location            = var.location
  resource_group_name = local.resource_group_name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  # Soft delete and purge protection for security
  soft_delete_retention_days = 7
  purge_protection_enabled   = false # Set to true for production

  # Network ACLs - restrict access
  network_acls {
    default_action = "Deny"
    bypass         = "AzureServices"
  }

  # Access policies for App Service Managed Identities
  # Note: Access policies will be added after App Services are created
  # Use Azure Portal or separate terraform apply to add access policies
  # Or use azurerm_key_vault_access_policy resource separately

  tags = merge(var.common_tags, {
    Name        = "${var.key_vault_name}-${var.environment}"
    Component   = "Security"
    Environment = var.environment
  })
}

# Application Insights (optional - enable when subscription is available)
resource "azurerm_application_insights" "main" {
  count = var.enable_application_insights ? 1 : 0

  name                = "${var.application_insights_name}-${var.environment}"
  location            = var.location
  resource_group_name = local.resource_group_name
  application_type    = "web"

  tags = merge(var.common_tags, {
    Name        = "${var.application_insights_name}-${var.environment}"
    Component   = "Monitoring"
    Environment = var.environment
  })
}

# Get current Azure client config (for tenant_id, etc.)
data "azurerm_client_config" "current" {}

# Outputs
output "resource_group_name" {
  description = "Name of the resource group"
  value       = local.resource_group_name
}

output "backend_app_url" {
  description = "Backend App Service URL"
  value       = "https://${azurerm_linux_web_app.backend.default_hostname}"
}

output "frontend_app_url" {
  description = "Frontend App Service URL"
  value       = "https://${azurerm_linux_web_app.angular.default_hostname}"
}

output "sql_server_fqdn" {
  description = "SQL Server fully qualified domain name"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_name" {
  description = "SQL Database name"
  value       = azurerm_mssql_database.main.name
}

output "storage_account_name" {
  description = "Storage account name"
  value       = azurerm_storage_account.main.name
}

output "storage_account_primary_connection_string" {
  description = "Storage account primary connection string"
  value       = azurerm_storage_account.main.primary_connection_string
  sensitive   = true
}

output "key_vault_name" {
  description = "Key Vault name (if enabled)"
  value       = var.enable_key_vault ? azurerm_key_vault.main[0].name : null
}

output "application_insights_id" {
  description = "Application Insights ID (if enabled)"
  value       = var.enable_application_insights ? azurerm_application_insights.main[0].id : null
}

output "backend_managed_identity_principal_id" {
  description = "Backend App Service Managed Identity Principal ID"
  value       = var.enable_managed_identity ? azurerm_linux_web_app.backend.identity[0].principal_id : null
}