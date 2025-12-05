# 🔐 GitHub Secrets Setup Guide

This guide explains how to set up the required secrets in GitHub for automatic deployment to Azure.

## 📋 Required Secrets

You need to add the following secrets to your GitHub repository:

### 1. `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND`
- **Description**: Publish profile for the backend App Service
- **How to get it**:
  1. Go to [Azure Portal](https://portal.azure.com)
  2. Navigate to: **App Services** → **echospace-backend-app-dev**
  3. Click **Get publish profile** (top menu)
  4. Download the `.PublishSettings` file
  5. Open the file in a text editor
  6. Copy the entire XML content
  7. Paste it as the secret value in GitHub

### 2. `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND`
- **Description**: Publish profile for the frontend App Service
- **How to get it**:
  1. Go to [Azure Portal](https://portal.azure.com)
  2. Navigate to: **App Services** → **echospace-angular-app-dev**
  3. Click **Get publish profile** (top menu)
  4. Download the `.PublishSettings` file
  5. Open the file in a text editor
  6. Copy the entire XML content
  7. Paste it as the secret value in GitHub

### 3. `AZURE_SQL_CONNECTION_STRING`
- **Description**: SQL Server connection string for database migrations
- **How to get it**:
  1. Go to [Azure Portal](https://portal.azure.com)
  2. Navigate to: **App Services** → **echospace-backend-app-dev**
  3. Go to **Configuration** → **Application settings**
  4. Find `ConnectionStrings__DefaultConnection`
  5. Click **Show value** and copy it
  6. Or get it from Terraform output:
     ```bash
     cd terraform
     terraform output storage_account_primary_connection_string
     ```
  7. Format: `Server=tcp:echospace-sql-dev.database.windows.net,1433;Initial Catalog=EchoSpaceDb;Persist Security Info=False;User ID=sqladmin;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`

## 🔧 How to Add Secrets to GitHub

### Method 1: Via GitHub Web Interface (Recommended)

1. **Navigate to Repository Settings**
   - Go to your GitHub repository
   - Click **Settings** (top menu)
   - Click **Secrets and variables** → **Actions** (left sidebar)

2. **Add Each Secret**
   - Click **New repository secret**
   - Enter the secret name (e.g., `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND`)
   - Paste the secret value
   - Click **Add secret**
   - Repeat for all 3 secrets

### Method 2: Via GitHub CLI

```bash
# Install GitHub CLI if not installed
# https://cli.github.com/

# Login to GitHub
gh auth login

# Add secrets
gh secret set AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND < publish-profile-backend.PublishSettings
gh secret set AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND < publish-profile-frontend.PublishSettings
gh secret set AZURE_SQL_CONNECTION_STRING --body "Server=tcp:echospace-sql-dev.database.windows.net,1433;Initial Catalog=EchoSpaceDb;..."
```

## ✅ Verification

After adding secrets, verify they're set:

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. You should see all 3 secrets listed (values are hidden)

## 🚀 Testing the Deployment

Once secrets are configured:

1. **Push to main branch** (or merge a PR)
2. **Check GitHub Actions**:
   - Go to **Actions** tab in your repository
   - You should see "Deploy to Azure" workflow running
   - Wait for it to complete (usually 5-10 minutes)

3. **Verify Deployment**:
   - Backend: https://echospace-backend-app-dev.azurewebsites.net/swagger
   - Frontend: https://echospace-angular-app-dev.azurewebsites.net

## 🔒 Security Best Practices

1. **Never commit secrets** to the repository
2. **Use GitHub Secrets** for all sensitive values
3. **Rotate secrets** periodically
4. **Limit access** to repository secrets (only admins)
5. **Use environment-specific secrets** for different environments (dev/staging/prod)

## 📝 Alternative: Using Azure Service Principal

If you prefer using Azure Service Principal instead of publish profiles:

1. Create a Service Principal:
   ```bash
   az ad sp create-for-rbac --name "EchoSpaceGitHubActions" \
     --role contributor \
     --scopes /subscriptions/b4742259-fa65-431e-b45f-d2846e96ff80/resourceGroups/echospace-resources \
     --sdk-auth
   ```

2. Add these secrets instead:
   - `AZURE_CLIENT_ID`
   - `AZURE_CLIENT_SECRET`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`

3. Update the workflow to use `azure/login@v2` action

## 🆘 Troubleshooting

### Secret Not Found Error
- Verify secret name matches exactly (case-sensitive)
- Check you're in the correct repository
- Ensure you have admin access

### Deployment Fails
- Check Azure Portal → App Service → Deployment Center → Logs
- Verify publish profile is valid and not expired
- Check connection string format is correct

### Database Migration Fails
- Verify connection string is correct
- Check SQL Server firewall allows Azure services
- Ensure database exists

---

**Next Step**: After setting up secrets, push to main branch to trigger deployment!

