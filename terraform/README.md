# EchoSpace Terraform Infrastructure

This directory contains Terraform configurations for deploying EchoSpace infrastructure to Azure.

## Prerequisites

- [Terraform](https://www.terraform.io/downloads) >= 1.5.0
- Azure CLI installed and configured (`az login`)
- Azure subscription (can use placeholder for now)

## Current Status

✅ **Can work without active Azure subscription** - Terraform will validate configuration without applying

## Setup Instructions

### 1. Initial Setup (Without Active Subscription)

1. Copy the example variables file:
   ```bash
   cp terraform.tfvars.example terraform.tfvars
   ```

2. Edit `terraform.tfvars` (optional for now):
   - Leave `subscription_id` empty to use Azure CLI default
   - Or set placeholder values

3. Validate Terraform configuration:
   ```bash
   cd terraform
   terraform init
   terraform validate
   terraform plan  # Will show what would be created (may fail without subscription, but validates syntax)
   ```

### 2. When You Have a New Azure Subscription

1. **Update `terraform.tfvars`**:
   ```hcl
   subscription_id = "your-new-subscription-id-here"
   ```

2. **Set up Terraform Backend (Optional but Recommended)**:
   - Create Azure Storage Account for remote state
   - Copy `backend.tf.example` to `backend.tf`
   - Update with your storage account details
   - Initialize: `terraform init -backend-config=backend.hcl`

3. **Enable Optional Features**:
   ```hcl
   enable_key_vault = true
   enable_application_insights = true
   ```

4. **Apply Infrastructure**:
   ```bash
   terraform plan   # Review changes
   terraform apply  # Create resources
   ```

## Configuration Files

- `main.tf` - Main infrastructure configuration
- `variables.tf` - Variable definitions
- `terraform.tfvars` - Variable values (not committed, create from example)
- `backend.tf` - Remote state configuration (optional, create from example)
- `terraform.tfvars.example` - Example variables file

## Security Features

✅ **HTTPS Enforcement** - All App Services enforce HTTPS
✅ **TLS 1.2 Minimum** - Secure TLS configuration
✅ **Managed Identity** - No secrets in App Service settings
✅ **Key Vault Integration** - Centralized secrets management (when enabled)
✅ **Application Insights** - Monitoring and logging (when enabled)

## Resources Created

- **App Service Plan** - Shared plan for backend and frontend
- **Backend App Service** - .NET 9 API application
- **Frontend App Service** - Angular application
- **Key Vault** (optional) - Secrets management
- **Application Insights** (optional) - Application monitoring

## Variables

Key variables you can customize:

- `subscription_id` - Azure subscription ID
- `resource_group_name` - Resource group name
- `location` - Azure region
- `environment` - Environment (dev/staging/prod)
- `app_service_plan_sku` - App Service Plan tier (F1/B1/S1/etc.)
- `enable_key_vault` - Enable Key Vault creation
- `enable_application_insights` - Enable Application Insights

See `variables.tf` for complete list.

## DevSecOps Integration

This Terraform configuration is designed to work with:
- ✅ Checkov for IaC security scanning
- ✅ GitHub Actions for CI/CD
- ✅ Azure Key Vault for secrets management
- ✅ Application Insights for monitoring

## Next Steps

1. ✅ Set up Terraform structure (DONE)
2. ⏳ Add Checkov scanning to CI/CD
3. ⏳ Set up Key Vault when subscription is available
4. ⏳ Configure Application Insights
5. ⏳ Add deployment workflows

## Troubleshooting

### "Subscription not found" error
- This is expected without an active subscription
- Terraform will still validate configuration syntax
- Update `subscription_id` in `terraform.tfvars` when subscription is available

### "Resource group not found"
- Create the resource group manually in Azure Portal first
- Or modify `main.tf` to create the resource group

### Authentication issues
- Run `az login` to authenticate with Azure CLI
- Terraform will use Azure CLI credentials automatically

