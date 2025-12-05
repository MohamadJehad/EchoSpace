# ⚡ Quick Fix: Deploy Code to Empty App Services

## 🔍 Problem

Your App Services exist but are empty (showing default Azure placeholder). The logs show:
```
Unable to find the startup DLL name
Running the default app using command: dotnet "/defaulthome/hostingstart/hostingstart.dll"
```

## ✅ Solution: Deploy Your Code

### Option 1: Quick Deploy Script (Easiest - 5 minutes)

I've created a script that does everything for you:

```powershell
.\scripts\quick-deploy.ps1
```

This will:
1. ✅ Build backend (.NET)
2. ✅ Deploy backend to Azure
3. ✅ Run database migrations
4. ✅ Build frontend (Angular)
5. ✅ Deploy frontend to Azure

**Just run it and wait!** (~5-10 minutes)

### Option 2: Manual Steps

If you prefer to do it manually:

#### Step 1: Deploy Backend

```powershell
# Build
dotnet publish src/EchoSpace.UI/EchoSpace.UI.csproj --configuration Release --output ./publish-backend

# Create zip
Compress-Archive -Path ./publish-backend/* -DestinationPath ./publish-backend.zip -Force

# Deploy
az webapp deployment source config-zip `
  --resource-group echospace-resources `
  --name echospace-backend-app-dev `
  --src ./publish-backend.zip
```

#### Step 2: Run Database Migrations

```powershell
# Get connection string (or use the helper script)
.\scripts\get-connection-string.ps1

# Run migrations
dotnet ef database update `
  --project src/EchoSpace.Infrastructure/EchoSpace.Infrastructure.csproj `
  --startup-project src/EchoSpace.UI/EchoSpace.UI.csproj `
  --connection "YOUR_CONNECTION_STRING_HERE"
```

#### Step 3: Deploy Frontend

```powershell
# Navigate to Angular project
cd src/EchoSpace.Web.Client

# Build
npm install
npm run build -- --configuration production

# Create zip
Compress-Archive -Path dist/echo-space.web.client/* -DestinationPath ../frontend-deploy.zip -Force

# Deploy
az webapp deployment source config-zip `
  --resource-group echospace-resources `
  --name echospace-angular-app-dev `
  --src ../frontend-deploy.zip
```

### Option 3: Use GitHub Actions (For Future Deployments)

Set up automatic deployments:
1. Add GitHub secrets (see `docs/devsecops/GITHUB_SECRETS_SETUP.md`)
2. Merge to main → Automatic deployment!

## 🔍 Verify Deployment

After deploying:

1. **Backend**: https://echospace-backend-app-dev.azurewebsites.net/swagger
   - Should show Swagger UI (not 404)

2. **Frontend**: https://echospace-angular-app-dev.azurewebsites.net
   - Should show your Angular app (not default page)

3. **Check Logs**:
   ```powershell
   az webapp log tail --name echospace-backend-app-dev --resource-group echospace-resources
   ```

## ⚙️ Configure App Settings

After deployment, configure these in Azure Portal:

1. **Go to App Service** → **Configuration** → **Application settings**

2. **Add/Update these settings:**

   **Backend (`echospace-backend-app-dev`):**
   - `ConnectionStrings__DefaultConnection` = Your SQL connection string
   - `Jwt__Key` = Your JWT secret key
   - `Google__ClientId` = Your Google OAuth client ID
   - `Google__ClientSecret` = Your Google OAuth secret
   - `ASPNETCORE_ENVIRONMENT` = `Production`

   **Frontend (`echospace-angular-app-dev`):**
   - Usually no settings needed (uses environment.prod.ts)

## 🆘 Troubleshooting

### Deployment Fails

- Check Azure CLI is logged in: `az account show`
- Verify resource group exists: `az group show --name echospace-resources`
- Check app service names are correct

### App Still Shows Default Page

- Wait 1-2 minutes for deployment to complete
- Check deployment status: Azure Portal → App Service → Deployment Center
- Restart the app: `az webapp restart --name echospace-backend-app-dev --resource-group echospace-resources`

### Database Connection Fails

- Verify SQL Server firewall allows Azure services
- Check connection string format
- Ensure database exists

---

**Recommended**: Use `.\scripts\quick-deploy.ps1` for the fastest deployment! 🚀

