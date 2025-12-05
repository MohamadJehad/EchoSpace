# 🔧 Solutions for Azure App Service Plan Quota Error

## Error Message
```
Error: creating App Service Plan
Status: 401 (Unauthorized)
Message: "Operation cannot be completed without additional quota.
Current Limit (Basic VMs): 0
Current Usage: 0
Amount required for this deployment (Basic VMs): 0"
```

## ✅ Solution 1: Request Quota Increase (Recommended)

### Method A: Via Azure Portal (Easiest)

1. **Navigate to Azure Portal**
   - Go to: https://portal.azure.com
   - Sign in with your account

2. **Create Support Request**
   - Click **Help + support** in the left menu
   - Click **New support request** or **Create a support request**

3. **Fill in Support Request Details**
   - **Issue type**: Select **Service and subscription limits (quotas)**
   - **Subscription**: Select your subscription (`b4742259-fa65-431e-b45f-d2846e96ff80`)
   - **Quota type**: Select **Web App (Windows and Linux)**
   - **Region**: Select **West US 2** (or your preferred region)
   - **SKU family / cores requested**: 
     - For Basic tier: Enter **1** (minimum)
     - For Free tier: Enter **1** (if available)
   - **Additional details**: 
     ```
     Requesting quota increase for App Service Plan (Basic tier) to deploy 
     EchoSpace application. Need 1 Basic VM instance in West US 2 region.
     ```

4. **Submit Request**
   - Review and submit
   - Approval typically takes **24-48 hours** (sometimes faster)

### Method B: Via Usage + Quotas Page

1. **Navigate to Quotas**
   - Azure Portal → **Subscriptions** → Your Subscription
   - Click **Usage + quotas** in the left menu

2. **Search for App Service Plans**
   - Search for: **"App Service Plans"** or **"Basic VMs"**
   - Filter by region: **West US 2**

3. **Request Increase**
   - Click on the quota entry
   - Click **Request increase** button
   - Fill in the form:
     - **New limit**: 1 (minimum)
     - **Justification**: "Need to deploy web application"
   - Submit

### Method C: Via Azure CLI (Alternative)

```bash
# List current quotas
az vm list-usage --location westus2 --query "[?contains(name.value, 'AppService')]"

# Note: Quota increases must be requested via Portal or Support
```

## ✅ Solution 2: Deploy to Different Region

If you have quota in another region:

1. **Check Available Regions**
   ```bash
   az account list-locations --query "[].{Name:name, DisplayName:displayName}" -o table
   ```

2. **Update Terraform Configuration**
   - Edit `terraform/terraform.tfvars`
   - Change `location = "westus2"` to a region with quota (e.g., `centralus`, `northeurope`)

3. **Redeploy**
   ```bash
   terraform apply
   ```

## ✅ Solution 3: Use Alternative Services (Immediate Workaround)

### Option A: Azure Static Web Apps (Free Tier)
- **No quota required** for Free tier
- Good for frontend (Angular)
- **Limitations**: 
  - Free tier has usage limits
  - Backend needs separate solution

**Terraform Example:**
```hcl
resource "azurerm_static_site" "frontend" {
  name                = "echospace-frontend"
  resource_group_name = azurerm_resource_group.main.name
  location            = "West US 2"
  sku_tier            = "Free"
  sku_size            = "Free"
}
```

### Option B: Azure Container Apps (Consumption Plan)
- **No upfront quota** - pay per use
- Can run both frontend and backend
- **Cost**: Pay only for what you use

**Note**: Requires Container Apps provider registration

### Option C: Azure Functions (Consumption Plan)
- **No App Service Plan required**
- Good for backend APIs
- **Limitations**: 
  - Not ideal for full web apps
  - Cold start delays

## ✅ Solution 4: Use Existing App Service Plan

If you have an existing App Service Plan in your subscription:

1. **Find Existing Plan**
   ```bash
   az appservice plan list --query "[].{Name:name, ResourceGroup:resourceGroup, Location:location, SKU:sku.tier}" -o table
   ```

2. **Update Terraform to Use Existing Plan**
   - Edit `terraform/main.tf`
   - Comment out `azurerm_service_plan.shared`
   - Use data source instead:
   ```hcl
   data "azurerm_service_plan" "existing" {
     name                = "your-existing-plan-name"
     resource_group_name = "your-resource-group"
   }
   
   # Then reference it:
   service_plan_id = data.azurerm_service_plan.existing.id
   ```

## ✅ Solution 5: Upgrade Subscription Type

Some subscription types have different quota limits:

1. **Check Subscription Type**
   ```bash
   az account show --query "{Name:name, SubscriptionId:id, State:state}" -o table
   ```

2. **Upgrade Options** (if applicable):
   - **Student/Educational**: May have limited quota
   - **Pay-As-You-Go**: Usually has higher quotas
   - **Enterprise Agreement**: Highest quotas

## 📋 Quick Action Checklist

- [ ] **Option 1**: Request quota increase via Portal (24-48 hours)
- [ ] **Option 2**: Try different region (immediate)
- [ ] **Option 3**: Use Static Web Apps for frontend (immediate)
- [ ] **Option 4**: Check for existing App Service Plan (immediate)
- [ ] **Option 5**: Consider subscription upgrade (if applicable)

## 🚀 Recommended Immediate Action

**For immediate deployment:**
1. Try **Solution 2** (different region) - fastest
2. Or use **Solution 3A** (Static Web Apps) for frontend
3. Meanwhile, submit **Solution 1** (quota request) for future use

**For long-term solution:**
1. Submit quota increase request (Solution 1)
2. Wait for approval
3. Deploy full infrastructure

## 📚 Additional Resources

- [Azure Quota Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/troubleshooting/error-resource-quota)
- [How to Increase Azure Quota (Video)](https://www.youtube.com/watch?v=Y-zXORgsCFg)
- [Azure App Service Quotas](https://docs.microsoft.com/en-us/azure/app-service/quotas)

## 💡 Pro Tips

1. **Quota requests are usually approved quickly** (often within 24 hours)
2. **Student subscriptions** may have stricter limits - consider upgrading
3. **Free tier quotas** are often more restricted than paid tiers
4. **Multiple regions** may have different quota limits - check all regions
5. **Contact Azure Support** if quota request is urgent

---

**Current Status**: Your subscription has **0 quota** for App Service Plans in West US 2.  
**Next Step**: Request quota increase via Azure Portal (Solution 1) or use alternative region/service.

