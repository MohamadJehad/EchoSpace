# Terraform Setup Guide - Working Without Active Subscription

This guide explains how to set up and validate Terraform configurations without an active Azure subscription.

## ✅ What's Been Done

1. **Enhanced `main.tf`** with:
   - Variables for subscription ID (can be empty/placeholder)
   - Security configurations (HTTPS, TLS, Managed Identity)
   - Optional Key Vault and Application Insights
   - Proper tagging

2. **Created `variables.tf`** with:
   - All configurable variables
   - Default values that work without subscription
   - Optional features (Key Vault, Application Insights)

3. **Created `terraform.tfvars.example`**:
   - Template for your configuration
   - Placeholder values
   - Instructions for when subscription is available

4. **Created `backend.tf.example`**:
   - Template for remote state storage
   - Instructions for setup

5. **Updated `.gitignore`**:
   - Excludes sensitive Terraform files
   - Keeps example files

## 🚀 Current Status

✅ **Can validate Terraform syntax without subscription**
✅ **Can run `terraform plan` (may show errors but validates structure)**
✅ **Ready to apply when subscription is available**

## 📋 Step-by-Step Setup

### Step 1: Copy Example Variables File

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
```

**Note:** `terraform.tfvars` is in `.gitignore` and won't be committed.

### Step 2: Edit terraform.tfvars (Optional for Now)

You can leave it as-is or customize:

```hcl
# Leave empty to use Azure CLI default subscription
subscription_id = ""

# Or use placeholder (will fail validation but syntax is correct)
# subscription_id = "PLACEHOLDER_SUBSCRIPTION_ID"
```

### Step 3: Initialize Terraform

```bash
terraform init
```

This will:
- Download Azure provider
- Set up Terraform workspace
- **Work even without active subscription**

### Step 4: Validate Configuration

```bash
terraform validate
```

This checks:
- ✅ Syntax correctness
- ✅ Variable references
- ✅ Resource configurations
- ✅ Provider requirements

**This will work without subscription!**

### Step 5: Format Code

```bash
terraform fmt
```

Formats all `.tf` files consistently.

### Step 6: Plan (Optional - May Show Errors)

```bash
terraform plan
```

**Expected behavior:**
- ✅ Validates configuration structure
- ✅ Shows what would be created
- ⚠️ May show errors about subscription/resource group (expected)

**This is OK!** The configuration is valid, it just can't connect to Azure.

## 🔄 When You Get a New Azure Subscription

### Step 1: Update terraform.tfvars

```hcl
subscription_id = "your-new-subscription-id-here"
```

### Step 2: Authenticate with Azure

```bash
az login
az account set --subscription "your-subscription-id"
```

### Step 3: Create Resource Group (if needed)

```bash
az group create \
  --name echospace-resources \
  --location eastus
```

### Step 4: Set Up Backend (Optional but Recommended)

1. Create Storage Account:
   ```bash
   az storage account create \
     --name echospacetfstate \
     --resource-group echospace-resources \
     --location eastus \
     --sku Standard_LRS
   ```

2. Create Container:
   ```bash
   az storage container create \
     --name tfstate \
     --account-name echospacetfstate
   ```

3. Copy backend example:
   ```bash
   cp backend.tf.example backend.tf
   ```

4. Create `backend.hcl`:
   ```hcl
   resource_group_name  = "echospace-resources"
   storage_account_name = "echospacetfstate"
   container_name       = "tfstate"
   key                  = "terraform.tfstate"
   ```

5. Initialize backend:
   ```bash
   terraform init -backend-config=backend.hcl
   ```

### Step 5: Enable Optional Features

Edit `terraform.tfvars`:

```hcl
enable_key_vault = true
enable_application_insights = true
```

### Step 6: Apply Infrastructure

```bash
terraform plan   # Review changes
terraform apply  # Create resources
```

## 🔒 Security Features Added

### App Service Security

✅ **HTTPS Only** - Enforced via `enable_https_only = true`
✅ **TLS 1.2 Minimum** - Secure TLS configuration
✅ **Managed Identity** - No secrets in App Service settings
✅ **Proper Tagging** - Environment, component, project tags

### Key Vault (When Enabled)

✅ **Soft Delete** - 7-day retention
✅ **Network ACLs** - Restricted access
✅ **Managed Identity Access** - App Services can access secrets

### Application Insights (When Enabled)

✅ **Application Monitoring** - Performance and error tracking
✅ **Centralized Logging** - All logs in one place

## 📁 File Structure

```
terraform/
├── main.tf                    # Main infrastructure (enhanced)
├── variables.tf               # Variable definitions (NEW)
├── terraform.tfvars.example   # Example variables (NEW)
├── terraform.tfvars           # Your variables (create from example, NOT committed)
├── backend.tf.example         # Backend example (NEW)
├── backend.tf                 # Backend config (create when ready, NOT committed)
├── backend.hcl                # Backend values (create when ready, NOT committed)
├── README.md                  # Terraform documentation (NEW)
└── .terraform/                # Terraform cache (ignored)
```

## 🧪 Testing Without Subscription

You can test these commands right now:

```bash
# Initialize
terraform init

# Validate syntax
terraform validate

# Format code
terraform fmt -check

# Show variables
terraform console
# Then type: var.subscription_id
```

## ⚠️ Common Issues

### "Subscription not found"
- **Expected** without active subscription
- Configuration is still valid
- Update `subscription_id` when subscription is available

### "Resource group not found"
- Create resource group manually first
- Or modify `main.tf` to create resource group (currently uses data source)

### "Authentication failed"
- Run `az login`
- Terraform uses Azure CLI credentials automatically

## ✅ Next Steps

1. ✅ **Terraform structure** - DONE
2. ⏳ **Add Checkov scanning** - Can add to CI/CD now
3. ⏳ **Set up CI/CD workflows** - Can validate Terraform in GitHub Actions
4. ⏳ **Add Key Vault** - When subscription is available
5. ⏳ **Add Application Insights** - When subscription is available

## 📚 Resources

- [Terraform Azure Provider Docs](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Azure CLI Documentation](https://docs.microsoft.com/cli/azure/)
- [Terraform Best Practices](https://www.terraform.io/docs/cloud/guides/recommended-practices/index.html)

