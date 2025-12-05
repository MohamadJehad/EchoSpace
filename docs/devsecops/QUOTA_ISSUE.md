# ⚠️ Azure Subscription Quota Issue

## Problem

Your Azure subscription (`b4742259-fa65-431e-b45f-d2846e96ff80`) **does not have quota** for App Service Plans. This affects both:
- **Free tier (F1)**: Quota = 0
- **Basic tier (B1)**: Quota = 0

## What Was Successfully Deployed

✅ **SQL Server**: `echospace-sql-dev` (West US 2)
✅ **SQL Database**: `EchoSpaceDb` (Basic tier)
✅ **SQL Firewall Rule**: AllowAzureServices

## What Failed

❌ **App Service Plan**: No quota available
❌ **App Services** (Backend & Frontend): Cannot create without App Service Plan
❌ **Storage Account**: Name conflict (needs unique name)

## Solutions

### Option 1: Request Quota Increase (Recommended)

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to: **Subscriptions** → Your Subscription → **Usage + quotas**
3. Search for: **App Service Plans**
4. Click **Request increase**
5. Fill out the form:
   - **Quota type**: App Service Plans
   - **Region**: West US 2 (or your preferred region)
   - **SKU Family**: Basic (or Free if available)
   - **New limit**: 1 (minimum)
6. Submit the request (usually approved within 24-48 hours)

**Alternative**: Use Azure Support
- Go to: **Help + support** → **New support request**
- Issue type: **Service and subscription limits (quotas)**
- Request App Service Plan quota

### Option 2: Use Different Subscription

If you have access to another subscription with App Service quota:
1. Switch subscription: `az account set --subscription <subscription-id>`
2. Update `terraform.tfvars` with new subscription ID
3. Re-run `terraform apply`

### Option 3: Deploy Without App Services (Temporary)

For now, you can:
1. Comment out App Service resources in `main.tf`
2. Deploy only SQL Server and Storage Account
3. Deploy App Services later when quota is available

## Next Steps

1. **Request quota** from Azure (Option 1 above)
2. **Update storage account name** in `terraform.tfvars`:
   ```hcl
   storage_account_name = "echospacestg"  # Or any unique name
   ```
3. **Wait for quota approval** (usually 24-48 hours)
4. **Re-run deployment**: `terraform apply`

## Current Status

- ✅ SQL Server and Database: **Deployed**
- ❌ App Services: **Blocked by quota**
- ⏳ Storage Account: **Needs unique name**

## Verify Quota Status

```bash
# Check current quota
az vm list-usage --location westus2 --query "[?contains(name.value, 'AppService')]"
```

## Contact

If quota request is urgent, contact Azure Support or your subscription administrator.

---

**Note**: This is a common issue with new Azure subscriptions, especially student/educational subscriptions. Quota increases are usually approved quickly.

