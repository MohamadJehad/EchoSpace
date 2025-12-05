# 🔧 Fix: App Services Exist But Are Empty

## Problem

The App Services were created successfully by Terraform, but they're empty (no application code deployed yet). This is why you're seeing 404 errors or "Resource not found" in the Azure Portal.

## ✅ What's Working

- ✅ App Services exist: `echospace-backend-app-dev` and `echospace-angular-app-dev`
- ✅ Both are running and enabled
- ✅ Infrastructure is ready

## ❌ What's Missing

- ❌ No application code deployed yet
- ❌ No database migrations run
- ❌ Apps are showing default/empty pages

## 🚀 Solution: Deploy Your Code

You have two options:

### Option 1: Use GitHub Actions (Recommended - Automatic)

This is the best option for ongoing deployments:

1. **Set up GitHub Secrets** (see `docs/devsecops/GITHUB_SECRETS_SETUP.md`)
2. **Merge to main branch** or manually trigger the workflow
3. **GitHub Actions will automatically:**
   - Build your code
   - Deploy to Azure
   - Run database migrations

**Steps:**
1. Get publish profiles from Azure Portal
2. Get SQL connection string
3. Add 3 secrets to GitHub
4. Push to main → Automatic deployment!

### Option 2: Manual Deployment (Quick Test)

If you want to test immediately without setting up GitHub Actions:

#### Deploy Backend (.NET)

**Using Azure CLI:**
```powershell
# Navigate to project root
cd C:\Users\Jehad\Documents\GitHub\EchoSpace

# Build and publish
dotnet publish src/EchoSpace.UI/EchoSpace.UI.csproj --configuration Release --output ./publish-backend

# Deploy using Azure CLI
az webapp deployment source config-zip `
  --resource-group echospace-resources `
  --name echospace-backend-app-dev `
  --src ./publish-backend.zip
```

**Or using Visual Studio:**
1. Right-click `EchoSpace.UI` project
2. **Publish** → **Azure** → **Azure App Service (Linux)**
3. Select `echospace-backend-app-dev`
4. Click **Publish**

#### Deploy Frontend (Angular)

**Using Azure CLI:**
```powershell
# Navigate to Angular project
cd src/EchoSpace.Web.Client

# Build for production
npm install
npm run build -- --configuration production

# Create zip
Compress-Archive -Path dist/echo-space.web.client/* -DestinationPath ../frontend-deploy.zip

# Deploy
az webapp deployment source config-zip `
  --resource-group echospace-resources `
  --name echospace-angular-app-dev `
  --src ../frontend-deploy.zip
```

**Or using VS Code Azure Extension:**
1. Install "Azure App Service" extension
2. Right-click `dist/echo-space.web.client` folder
3. **Deploy to Web App** → Select `echospace-angular-app-dev`

#### Run Database Migrations

```powershell
# Get connection string first
$connectionString = "Server=tcp:echospace-sql-dev.database.windows.net,1433;Initial Catalog=EchoSpaceDb;Persist Security Info=False;User ID=sqladmin;Password=ChangeThisPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Run migrations
dotnet ef database update `
  --project src/EchoSpace.Infrastructure/EchoSpace.Infrastructure.csproj `
  --startup-project src/EchoSpace.UI/EchoSpace.UI.csproj `
  --connection $connectionString
```

## 🔍 Verify Deployment

After deploying:

1. **Backend**: https://echospace-backend-app-dev.azurewebsites.net/swagger
   - Should show Swagger UI
   
2. **Frontend**: https://echospace-angular-app-dev.azurewebsites.net
   - Should show your Angular app

3. **Check Logs**:
   ```powershell
   az webapp log tail --name echospace-backend-app-dev --resource-group echospace-resources
   ```

## 📝 Next Steps

1. **Deploy code** (choose Option 1 or 2 above)
2. **Configure App Settings** in Azure Portal:
   - Connection strings
   - JWT secrets
   - API keys
3. **Set up GitHub Actions** for automatic deployments (Option 1)

## 🆘 Troubleshooting

### App Still Shows 404

- Check if deployment succeeded: Azure Portal → App Service → Deployment Center
- Check logs: Azure Portal → App Service → Log stream
- Verify app is running: `az webapp show --name echospace-backend-app-dev --resource-group echospace-resources`

### Database Connection Fails

- Verify SQL Server firewall allows Azure services
- Check connection string is correct
- Ensure database exists: `az sql db show --name EchoSpaceDb --server echospace-sql-dev --resource-group echospace-resources`

### Frontend Can't Connect to Backend

- Check CORS settings in backend
- Verify `environment.prod.ts` has correct backend URL
- Check browser console for CORS errors

---

**Recommended**: Use Option 1 (GitHub Actions) for automatic deployments going forward!

