# Terraform Variables
# Update terraform.tfvars with your subscription details when available

variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
  # Set this in terraform.tfvars or via environment variable
  # For now, can use placeholder - will use Azure CLI default if not set
  default = ""
}

variable "resource_group_name" {
  description = "Name of the Azure Resource Group"
  type        = string
  default     = "echospace-resources"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "eastus"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "app_name" {
  description = "Application name prefix"
  type        = string
  default     = "echospace"
}

# App Service Configuration
variable "app_service_plan_sku" {
  description = "App Service Plan SKU (F1=Free, B1=Basic, S1=Standard)"
  type        = string
  default     = "F1"
}

variable "backend_app_name" {
  description = "Backend App Service name"
  type        = string
  default     = "echospace-backend-app"
}

variable "frontend_app_name" {
  description = "Frontend App Service name"
  type        = string
  default     = "echospace-angular-app"
}

# Security Configuration
variable "enable_https_only" {
  description = "Enforce HTTPS only"
  type        = bool
  default     = true
}

variable "minimum_tls_version" {
  description = "Minimum TLS version (1.0, 1.1, 1.2)"
  type        = string
  default     = "1.2"
}

variable "enable_managed_identity" {
  description = "Enable Managed Identity for App Services"
  type        = bool
  default     = true
}

# Key Vault Configuration (optional for now)
variable "enable_key_vault" {
  description = "Enable Key Vault creation"
  type        = bool
  default     = false # Set to true when subscription is available
}

variable "key_vault_name" {
  description = "Key Vault name (must be globally unique)"
  type        = string
  default     = "echospace-vault"
}

# Monitoring Configuration (optional for now)
variable "enable_application_insights" {
  description = "Enable Application Insights"
  type        = bool
  default     = false # Set to true when subscription is available
}

variable "application_insights_name" {
  description = "Application Insights name"
  type        = string
  default     = "echospace-insights"
}

# Database Configuration
variable "sql_server_name" {
  description = "SQL Server name (must be globally unique, will have environment suffix)"
  type        = string
  default     = "echospace-sql"
}

variable "sql_database_name" {
  description = "SQL Database name"
  type        = string
  default     = "EchoSpaceDb"
}

variable "sql_admin_login" {
  description = "SQL Server administrator login"
  type        = string
  sensitive   = true
  default     = "sqladmin"
}

variable "sql_admin_password" {
  description = "SQL Server administrator password (min 8 chars, must contain uppercase, lowercase, numbers, special chars)"
  type        = string
  sensitive   = true
  default     = "" # Must be set in terraform.tfvars
}

# Storage Account Configuration
variable "storage_account_name" {
  description = "Storage account name (must be globally unique, lowercase, no special chars except hyphens)"
  type        = string
  default     = "echospacestorage"
}

# Tags
variable "common_tags" {
  description = "Common tags to apply to all resources"
  type        = map(string)
  default = {
    Environment = "dev"
    Project     = "EchoSpace"
    ManagedBy   = "Terraform"
  }
}

