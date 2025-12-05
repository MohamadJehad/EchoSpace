# ⚡ Quick Start: Deploy to Azure

## 🎯 Goal
Set up automatic deployment so that when you merge to `main`, your app automatically deploys to Azure.

## ✅ What's Already Done
- ✅ GitHub Actions workflow created (`.github/workflows/deploy.yml`)
- ✅ Frontend configured to point to backend
- ✅ CORS updated for production
- ✅ Database migration scripts ready

## 📝 What You Need to Do (5 minutes)

### Step 1: Get Publish Profiles (2 minutes)

**Backend:**
1. Azure Portal → **App Services** → **echospace-backend-app-dev**
2. Click **Get publish profile** (top menu)
3. Save the file
4. Open it and copy the **entire XML content**

**Frontend:**
1. Azure Portal → **App Services** → **echospace-angular-app-dev**
2. Click **Get publish profile** (top menu)
3. Save the file
4. Open it and copy the **entire XML content**

### Step 2: Get SQL Connection String (1 minute)

**Option A: Use Helper Script (Easiest)**
```powershell
.\scripts\get-connection-string.ps1
```
This will generate and copy the connection string to your clipboard!

**Option B: From Azure Portal**
1. Azure Portal → **App Services** → **echospace-backend-app-dev**
2. **Configuration** → **Application settings**
3. Find `ConnectionStrings__DefaultConnection`
4. Click **Show value** and copy

**Option C: From Terraform**
```bash
cd terraform
terraform output storage_account_primary_connection_string
```

### Step 3: Add Secrets to GitHub (2 minutes)

1. Go to your GitHub repository
2. **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each:

   **Secret 1:**
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND`
   - Value: Paste backend publish profile XML
   
   **Secret 2:**
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND`
   - Value: Paste frontend publish profile XML
   
   **Secret 3:**
   - Name: `AZURE_SQL_CONNECTION_STRING`
   - Value: Paste SQL connection string

## 🚀 Test It!

1. Make a small change (e.g., update README)
2. Commit and push to `main` branch
3. Go to **Actions** tab in GitHub
4. Watch the deployment run! 🎉

## 📊 Expected Timeline

- **Build Backend**: ~2-3 minutes
- **Deploy Backend**: ~1-2 minutes
- **Database Migrations**: ~30 seconds
- **Build Frontend**: ~2-3 minutes
- **Deploy Frontend**: ~1-2 minutes
- **Total**: ~7-10 minutes

## ✅ Verify Deployment

After deployment completes:

1. **Backend**: https://echospace-backend-app-dev.azurewebsites.net/swagger
2. **Frontend**: https://echospace-angular-app-dev.azurewebsites.net

## 🆘 Need Help?

- **Detailed Guide**: `docs/devsecops/DEPLOYMENT_SETUP.md`
- **Secrets Setup**: `docs/devsecops/GITHUB_SECRETS_SETUP.md`
- **Troubleshooting**: Check GitHub Actions logs

---

**That's it!** Once secrets are set, every merge to `main` will automatically deploy! 🚀

