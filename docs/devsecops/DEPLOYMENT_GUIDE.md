# Deployment Guide - First Time Setup

This guide walks you through deploying EchoSpace infrastructure to Azure for the first time.

## Prerequisites

1. ✅ Azure subscription active (ID: `b4742259-fa65-431e-b45f-d2846e96ff80`)
2. ✅ Azure CLI installed and logged in
3. ✅ Terraform installed (>= 1.5.0)

## Step 1: Authenticate with Azure

```bash
# Login to Azure
az login

# Set your subscription
az account set --subscription "b4742259-fa65-431e-b45f-d2846e96ff80"

# Verify subscription
az account show
```

## Step 2: Configure Terraform Variables

1. **Copy the example file** (if not already done):
   ```bash
   cd terraform
   cp terraform.tfvars.example terraform.tfvars
   ```

2. **Edit `terraform.tfvars`**:
   - ✅ Subscription ID is already set
   - ⚠️ **IMPORTANT**: Change `sql_admin_password` to a secure password
     - Minimum 8 characters
     - Must contain: uppercase, lowercase, numbers, special characters
   - ⚠️ **IMPORTANT**: Change `storage_account_name` if needed (must be globally unique)

## Step 3: Initialize Terraform

```bash
cd terraform

# Initialize Terraform
terraform init
```

This will:
- Download Azure provider
- Set up Terraform workspace
- Prepare for deployment

## Step 4: Review What Will Be Created

```bash
# See what Terraform will create
terraform plan
```

**Expected Resources:**
- ✅ Resource Group: `echospace-resources`
- ✅ App Service Plan: `echospace-shared-plan-dev` (Free tier)
- ✅ Backend App Service: `echospace-backend-app`
- ✅ Frontend App Service: `echospace-angular-app`
- ✅ SQL Server: `aechospace-sql-dev`
- ✅ SQL Database: `EchoSpaceDb` (Basic tier - 2GB)
- ✅ Storage Account: `echospacestorage` (Standard_LRS - cheapest)
- ✅ Blob Containers: `app-files`, `user-uploads`

## Step 5: Deploy Infrastructure

```bash
# Apply the configuration
terraform apply
```

**You will be prompted to confirm:**
- Type `yes` to proceed
- Terraform will create all resources
- This may take 5-10 minutes

## Step 6: Verify Deployment

After deployment completes, you'll see outputs:

```bash
# View outputs
terraform output
```

**Key Outputs:**
- `backend_app_url` - Your backend API URL
- `frontend_app_url` - Your frontend app URL
- `sql_server_fqdn` - SQL Server connection string
- `storage_account_name` - Storage account name

## Step 7: Update Application Configuration

### Backend Configuration

The App Service already has connection strings configured, but you may want to verify:

1. Go to Azure Portal
2. Navigate to your App Service (`echospace-backend-app`)
3. Go to **Configuration** → **Application settings**
4. Verify:
   - `ConnectionStrings__DefaultConnection` is set
   - `AzureStorage__ConnectionString` is set

### Frontend Configuration

Update your Angular app to point to the backend URL:

1. Edit `src/EchoSpace.Web.Client/src/environments/environment.prod.ts`
2. Update `apiUrl` to your backend URL from outputs

## Step 8: Deploy Your Application

### Option 1: GitHub Actions (Recommended)

1. Set up GitHub Secrets:
   - `AZURE_SUBSCRIPTION_ID`: `b4742259-fa65-431e-b45f-d2846e96ff80`
   - `AZURE_CLIENT_ID`: (Service Principal)
   - `AZURE_CLIENT_SECRET`: (Service Principal)
   - `AZURE_TENANT_ID`: (Your tenant ID)

2. Create deployment workflows (see deployment workflow examples)

### Option 2: Manual Deployment

```bash
# Build and publish .NET app
cd src/EchoSpace.UI
dotnet publish -c Release -o ./publish

# Deploy to App Service
az webapp deployment source config-zip \
  --resource-group echospace-resources \
  --name echospace-backend-app \
  --src ./publish.zip
```

## Cost Estimation

**Monthly Cost (Approximate):**

- **App Service Plan (F1)**: $0 (Free tier)
- **SQL Database (Basic)**: ~$5/month
- **Storage Account (Standard_LRS)**: ~$0.02/GB/month
- **Total**: ~$5-10/month for development

**To Reduce Costs Further:**
- Use F1 App Service Plan (free)
- Use Basic SQL Database (cheapest)
- Use Standard_LRS storage (cheapest)
- Disable Application Insights (if not needed)

## Troubleshooting

### Error: Storage Account Name Already Exists

**Solution**: Change `storage_account_name` in `terraform.tfvars` to something unique.

### Error: SQL Server Name Already Exists

**Solution**: Change `sql_server_name` in `terraform.tfvars` to something unique.

### Error: Subscription Not Found

**Solution**: 
```bash
az login
az account set --subscription "b4742259-fa65-431e-b45f-d2846e96ff80"
```

### Error: Resource Group Already Exists

**Solution**: Either:
1. Use existing resource group by changing `resource_group_name` in `terraform.tfvars`
2. Or delete existing resource group (if safe to do so)

## Next Steps

1. ✅ **Infrastructure deployed** - DONE
2. ⏳ **Deploy application code** - Next step
3. ⏳ **Configure Key Vault** - When ready
4. ⏳ **Set up Application Insights** - When ready
5. ⏳ **Configure custom domains** - When ready

## Cleanup (If Needed)

To remove all resources:

```bash
terraform destroy
```

**Warning**: This will delete all resources created by Terraform!

---

**Congratulations!** Your infrastructure is now deployed and ready for your application! 🎉

