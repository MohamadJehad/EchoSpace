# 🚀 Complete Deployment Setup Guide

This guide walks you through setting up automatic deployment to Azure when you merge to the main branch.

## ✅ What's Already Done

1. ✅ **Frontend Configuration Updated**
   - `environment.prod.ts` now points to: `https://echospace-backend-app-dev.azurewebsites.net/api`
   
2. ✅ **GitHub Actions Workflow Created**
   - `.github/workflows/deploy.yml` - Automatically deploys on merge to main
   
3. ✅ **Database Migration Scripts Created**
   - `scripts/deploy-database-migrations.sh` (Linux/Mac)
   - `scripts/deploy-database-migrations.ps1` (Windows)

4. ✅ **CORS Updated**
   - Backend now allows requests from production frontend URL

## 📋 What You Need to Do

### Step 1: Get Azure Publish Profiles

#### For Backend App Service:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to: **App Services** → **echospace-backend-app-dev**
3. Click **Get publish profile** (top menu bar)
4. Save the file as `backend-publish-profile.PublishSettings`
5. Open the file in a text editor
6. Copy the **entire XML content**

#### For Frontend App Service:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to: **App Services** → **echospace-angular-app-dev**
3. Click **Get publish profile** (top menu bar)
4. Save the file as `frontend-publish-profile.PublishSettings`
5. Open the file in a text editor
6. Copy the **entire XML content**

### Step 2: Get SQL Connection String

You have two options:

#### Option A: From Azure Portal
1. Go to **App Services** → **echospace-backend-app-dev**
2. Click **Configuration** → **Application settings**
3. Find `ConnectionStrings__DefaultConnection`
4. Click **Show value** and copy it

#### Option B: From Terraform Output
```bash
cd terraform
terraform output storage_account_primary_connection_string
```

**Note**: The connection string format should be:
```
Server=tcp:echospace-sql-dev.database.windows.net,1433;Initial Catalog=EchoSpaceDb;Persist Security Info=False;User ID=sqladmin;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### Step 3: Add Secrets to GitHub

1. **Go to your GitHub repository**
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each:

   **Secret 1: `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND`**
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND`
   - Value: Paste the entire XML from `backend-publish-profile.PublishSettings`
   - Click **Add secret**

   **Secret 2: `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND`**
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND`
   - Value: Paste the entire XML from `frontend-publish-profile.PublishSettings`
   - Click **Add secret**

   **Secret 3: `AZURE_SQL_CONNECTION_STRING`**
   - Name: `AZURE_SQL_CONNECTION_STRING`
   - Value: Paste the connection string (from Step 2)
   - Click **Add secret**

### Step 4: Verify Secrets

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. You should see all 3 secrets listed:
   - ✅ `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND`
   - ✅ `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND`
   - ✅ `AZURE_SQL_CONNECTION_STRING`

## 🚀 Testing the Deployment

### Option 1: Merge to Main (Automatic)

1. Create a branch and make a small change
2. Create a Pull Request
3. Merge to `main` branch
4. Go to **Actions** tab in GitHub
5. Watch the "Deploy to Azure" workflow run

### Option 2: Manual Trigger

1. Go to **Actions** tab
2. Select **Deploy to Azure** workflow
3. Click **Run workflow** → **Run workflow**
4. Select `main` branch
5. Click **Run workflow**

## 📊 What the Workflow Does

The deployment workflow (`deploy.yml`) performs these steps:

1. **Build Backend** (.NET)
   - Restores dependencies
   - Builds the solution
   - Runs tests (optional)
   - Publishes the app

2. **Deploy Backend**
   - Creates deployment package
   - Deploys to Azure App Service

3. **Run Database Migrations**
   - Connects to Azure SQL Database
   - Runs Entity Framework migrations
   - Updates database schema

4. **Build Frontend** (Angular)
   - Installs dependencies
   - Builds production bundle
   - Creates deployment package

5. **Deploy Frontend**
   - Deploys to Azure App Service

6. **Verify Deployment**
   - Checks if services are responding

## 🔍 Monitoring Deployment

### In GitHub Actions:
1. Go to **Actions** tab
2. Click on the latest workflow run
3. Watch each job complete:
   - ✅ Green checkmark = Success
   - ❌ Red X = Failed (check logs)

### In Azure Portal:
1. Go to **App Services** → Your app
2. Click **Deployment Center**
3. View deployment history and logs

## 🐛 Troubleshooting

### Deployment Fails

**Error: "Secret not found"**
- Verify secret names match exactly (case-sensitive)
- Check you're in the correct repository
- Ensure you have admin access

**Error: "Publish profile invalid"**
- Re-download publish profile from Azure Portal
- Ensure you copied the entire XML content
- Check the profile hasn't expired

**Error: "Database migration fails"**
- Verify connection string is correct
- Check SQL Server firewall allows Azure services
- Ensure database exists
- Check password in connection string

**Error: "Build fails"**
- Check GitHub Actions logs for specific error
- Verify .NET SDK version matches (9.0.x)
- Verify Node.js version matches (20.x)

### App Not Responding After Deployment

1. **Check App Service Status**:
   - Azure Portal → App Service → Overview
   - Verify status is "Running"

2. **Check Logs**:
   - Azure Portal → App Service → Log stream
   - Look for errors

3. **Check Configuration**:
   - Azure Portal → App Service → Configuration
   - Verify connection strings are set

4. **Test Endpoints**:
   - Backend: https://echospace-backend-app-dev.azurewebsites.net/swagger
   - Frontend: https://echospace-angular-app-dev.azurewebsites.net

## 📝 Next Steps After First Deployment

1. **Verify Database Migrations**:
   - Check Azure Portal → SQL Database → Query editor
   - Verify tables were created

2. **Test API Endpoints**:
   - Visit Swagger: https://echospace-backend-app-dev.azurewebsites.net/swagger
   - Test a few endpoints

3. **Test Frontend**:
   - Visit: https://echospace-angular-app-dev.azurewebsites.net
   - Verify it loads and connects to backend

4. **Monitor Logs**:
   - Set up Application Insights (optional)
   - Monitor App Service logs

## 🔄 Workflow Details

### When It Runs:
- ✅ On push to `main` branch
- ✅ Manual trigger (workflow_dispatch)

### What It Builds:
- ✅ Backend: `.NET 9.0` application
- ✅ Frontend: `Angular` application (production build)
- ✅ Database: Entity Framework migrations

### Deployment Order:
1. Backend → Database Migrations → Frontend
2. This ensures backend is ready before frontend tries to connect

## 📚 Additional Resources

- **GitHub Secrets Setup**: `docs/devsecops/GITHUB_SECRETS_SETUP.md`
- **Deployment Guide**: `docs/devsecops/DEPLOYMENT_GUIDE.md`
- **Terraform Outputs**: Run `terraform output` in `terraform/` directory

---

**Ready to deploy?** Set up the secrets and merge to main! 🚀

