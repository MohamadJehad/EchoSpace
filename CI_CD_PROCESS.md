# EchoSpace CI/CD Process Documentation

This document describes the complete CI/CD pipeline process for EchoSpace, from pull request creation through deployment to Azure.

## Overview

The CI/CD pipeline consists of three main stages:
1. **Pull Request Stage** - Build, test, and security checks
2. **Approval & Merge Stage** - Code review and branch protection
3. **Deployment Stage** - Automated deployment to Azure

---

## Stage 1: Pull Request Creation

When a developer creates a pull request targeting `main` or `develop` branches, two workflows are triggered **in parallel**:

### Workflow 1: PR Build Check (`pr-build.yml`)

**Trigger:** Pull request to `main` or `develop` branches

**Job:** `Build and Test`

#### Step 1: Checkout Code
- **Action:** `actions/checkout@v4`
- **Purpose:** Retrieves the source code from the pull request branch
- **What happens:** The entire repository is checked out to the GitHub Actions runner

#### Step 2: Setup .NET Environment
- **Action:** `actions/setup-dotnet@v4`
- **Version:** .NET 9.0.x
- **Purpose:** Installs the .NET SDK required for building the backend
- **What happens:** .NET SDK is installed and configured on the Ubuntu runner

#### Step 3: Restore .NET Dependencies
- **Command:** `dotnet restore EchoSpace.CleanArchitecture.sln`
- **Purpose:** Downloads all NuGet packages required by the solution
- **What happens:** 
  - Reads all `.csproj` files in the solution
  - Downloads missing packages from NuGet
  - Restores package references for all projects

#### Step 4: Build .NET Project
- **Command:** `dotnet build EchoSpace.CleanArchitecture.sln --configuration Release --no-restore`
- **Purpose:** Compiles all .NET projects in Release configuration
- **What happens:**
  - Compiles all C# code in the solution
  - Validates code syntax and type checking
  - Creates compiled assemblies (DLLs)
  - **Fails if:** Build errors, compilation errors, or missing dependencies

#### Step 5: Run .NET Unit Tests
- **Command:** `dotnet test EchoSpace.CleanArchitecture.sln --configuration Release --no-build --verbosity normal`
- **Purpose:** Executes all unit tests in the test projects
- **What happens:**
  - Discovers all test methods (e.g., in `EchoSpace.Tests` project)
  - Executes tests using the test framework (xUnit/NUnit/MSTest)
  - Reports test results (passed/failed)
  - **Fails if:** Any test fails
- **Test projects included:** `tests/EchoSpace.Tests/`

#### Step 6: Setup Node.js Environment
- **Action:** `actions/setup-node@v4`
- **Version:** Node.js 20.x
- **Purpose:** Installs Node.js and npm for Angular frontend
- **Caching:** Enables npm package caching for faster builds
- **What happens:** Node.js runtime and npm are installed

#### Step 7: Install Angular Dependencies
- **Command:** `npm ci` (in `src/EchoSpace.Web.Client` directory)
- **Purpose:** Installs all npm packages from `package-lock.json`
- **What happens:**
  - Reads `package-lock.json` for exact versions
  - Installs all dependencies (including Angular, TypeScript, etc.)
  - Uses `npm ci` for clean, reproducible installs (faster than `npm install`)

#### Step 8: Build Angular Project
- **Command:** `npm run build -- --configuration production`
- **Purpose:** Compiles the Angular TypeScript code into production-ready JavaScript
- **What happens:**
  - Compiles TypeScript to JavaScript
  - Bundles and minifies code
  - Optimizes assets (images, CSS)
  - Creates production build in `dist/` folder
  - **Fails if:** TypeScript errors, build errors, or missing dependencies

**Status Check Name:** `Build and Test`

---

### Workflow 2: Security Scanning (`security-scan.yml`)

**Trigger:** Pull request to `main` or `develop` branches (runs in parallel with PR Build)

This workflow runs **multiple security jobs in parallel**:

#### Job 1: Secrets Scan (Gitleaks)
- **Tool:** Gitleaks
- **Purpose:** Detects accidentally committed secrets (API keys, passwords, tokens)
- **What happens:**
  - Scans all commits in the PR
  - Checks for patterns matching secrets (AWS keys, Azure keys, passwords, etc.)
  - **Fails if:** Any secrets are detected
- **Status Check Name:** `Secrets Scan (Gitleaks)`

#### Job 2: .NET Dependency Scan
- **Tool:** `dotnet list package --vulnerable`
- **Purpose:** Checks for vulnerable NuGet packages
- **What happens:**
  - Scans all .NET packages (including transitive dependencies)
  - Checks against vulnerability databases
  - Reports HIGH and CRITICAL vulnerabilities
  - **Warning only:** Does not fail the build (warns about vulnerabilities)
- **Status Check Name:** `.NET Dependency Scan`

#### Job 3: Node.js Dependency Scan
- **Tool:** `npm audit`
- **Purpose:** Checks for vulnerable npm packages
- **What happens:**
  - Scans all npm packages in Angular project
  - Checks against npm security advisories
  - Reports HIGH and CRITICAL vulnerabilities
  - Attempts automatic fixes (non-blocking)
  - **Warning only:** Does not fail the build (warns about vulnerabilities)
- **Status Check Name:** `Node.js Dependency Scan`

#### Job 4: .NET SAST Scan
- **Tool:** Security Code Scan
- **Purpose:** Static Application Security Testing for C# code
- **What happens:**
  - Analyzes C# source code for security vulnerabilities
  - Detects common security issues (SQL injection, XSS, etc.)
  - Generates security report
  - **Non-blocking:** Uses `continue-on-error: true`
- **Status Check Name:** `.NET SAST Scan`

#### Job 5: TypeScript SAST Scan
- **Tool:** ESLint with security plugin
- **Purpose:** Static Application Security Testing for TypeScript code
- **What happens:**
  - Analyzes TypeScript source code
  - Checks for security anti-patterns
  - Generates ESLint security report
  - **Fails if:** Security issues found
- **Status Check Name:** `TypeScript SAST Scan`

#### Job 6: SBOM Generation
- **Tool:** CycloneDX
- **Purpose:** Generates Software Bill of Materials for compliance
- **What happens:**
  - Generates SBOM for .NET packages (CycloneDX format)
  - Generates SBOM for npm packages (CycloneDX format)
  - Uploads SBOM files as artifacts (retained for 30 days)
- **Status Check Name:** `Generate SBOM`

#### Job 7: Terraform Validation
- **Tool:** Terraform CLI
- **Purpose:** Validates Terraform infrastructure code
- **What happens:**
  - Checks Terraform syntax (`terraform fmt -check`)
  - Validates Terraform configuration (`terraform validate`)
  - Runs dry-run plan (`terraform plan`)
  - **Fails if:** Syntax errors or validation failures
- **Status Check Name:** `Terraform Validate`

**Note:** Terraform Security Scan (Checkov) is currently disabled in the workflow.

---

## Stage 2: Approval & Merge

After all status checks pass, the pull request requires:

### Branch Protection Rules

For the `main` branch (configured in GitHub Settings → Branches):

1. **Require Pull Request Before Merging**
   - PRs cannot be pushed directly to `main`
   - Must go through pull request process

2. **Require Approvals**
   - **Minimum:** 2 approvals required
   - **Code Owners:** Automatic review requests based on `.github/CODEOWNERS`
   - **What happens:**
     - CODEOWNERS file defines which team reviews which files
     - Backend changes → `@backend-team` reviews
     - Frontend changes → `@frontend-team` reviews
     - Infrastructure changes → `@devops-team` reviews

3. **Require Status Checks to Pass**
   - All required status checks must pass before merge
   - **Required checks:**
     - ✅ `Build and Test` (from `pr-build.yml`)
     - ✅ `Secrets Scan (Gitleaks)` (from `security-scan.yml`)
     - ✅ `Terraform Validate` (from `security-scan.yml`)
   - **Optional checks** (warnings, don't block merge):
     - `.NET Dependency Scan`
     - `Node.js Dependency Scan`
     - `.NET SAST Scan`
     - `TypeScript SAST Scan`
     - `Generate SBOM`

### Merge Process

Once:
- ✅ All required status checks pass
- ✅ Required approvals obtained (2 reviewers)
- ✅ No merge conflicts

The PR can be merged to `main` branch.

---

## Stage 3: Post-Merge Deployment

When code is merged to `main`, the **Deploy to Azure** workflow (`deploy.yml`) is triggered automatically.

**Trigger:** Push to `main` branch

### Job 1: Build and Deploy Backend (.NET)

#### Step 1: Checkout Code
- Retrieves the merged code from `main` branch

#### Step 2: Setup .NET
- Installs .NET 9.0.x SDK

#### Step 3: Restore Dependencies
- `dotnet restore EchoSpace.CleanArchitecture.sln`
- Downloads all NuGet packages

#### Step 4: Build Solution
- `dotnet build EchoSpace.CleanArchitecture.sln --configuration Release`
- Compiles all .NET projects in Release mode

#### Step 5: Run Tests
- `dotnet test EchoSpace.CleanArchitecture.sln --configuration Release`
- Executes all unit tests
- **Note:** Uses `continue-on-error: true` (tests don't block deployment)

#### Step 6: Publish Backend
- `dotnet publish src/EchoSpace.UI/EchoSpace.UI.csproj --configuration Release --output ./publish-backend`
- Creates production-ready deployment package
- Includes all dependencies and compiled code

#### Step 7: Create Deployment Package
- Creates `backend-deploy.zip` from published files
- Packages everything needed for Azure deployment

#### Step 8: Deploy to Azure App Service (Backend)
- **Action:** `azure/webapps-deploy@v3`
- **Target:** `echospace-backend-app-dev` Azure App Service
- **What happens:**
  - Uploads deployment package to Azure
  - Deploys to backend App Service
  - Restarts the application
  - **Uses:** `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND` secret

**Job Name:** `Build and Deploy Backend (.NET)`

---

### Job 2: Run Database Migrations

**Depends on:** `build-and-deploy-backend` (runs after backend deployment)

#### Step 1: Checkout Code
- Retrieves code from `main` branch

#### Step 2: Setup .NET
- Installs .NET 9.0.x SDK

#### Step 3: Restore Dependencies
- Restores NuGet packages

#### Step 4: Run Database Migrations
- **Command:** `dotnet ef database update`
- **Purpose:** Applies Entity Framework migrations to Azure SQL Database
- **What happens:**
  - Connects to Azure SQL Database using connection string from secrets
  - Applies pending migrations
  - Updates database schema
  - **Uses:** `AZURE_SQL_CONNECTION_STRING` secret
  - **Note:** Uses `continue-on-error: true` (migrations may already be applied)

**Job Name:** `Run Database Migrations`

---

### Job 3: Build and Deploy Frontend (Angular)

**Depends on:** `build-and-deploy-backend` (runs in parallel with database migrations)

#### Step 1: Checkout Code
- Retrieves code from `main` branch

#### Step 2: Setup Node.js
- Installs Node.js 20.x
- Enables npm caching

#### Step 3: Install Dependencies
- `npm ci` in `src/EchoSpace.Web.Client`
- Installs all Angular dependencies

#### Step 4: Build Angular App
- `npm run build -- --configuration production`
- Creates production build in `dist/echo-space.web.client/browser`

#### Step 5: Create Static Site Package.json
- Creates a `package.json` in the dist folder for Azure deployment
- Includes `serve` package for static file serving

#### Step 6: Create Deployment Package
- Creates `frontend-deploy.zip` from Angular build output
- Packages the static files for Azure

#### Step 7: Deploy to Azure App Service (Frontend)
- **Action:** `azure/webapps-deploy@v3`
- **Target:** `echospace-angular-app-dev` Azure App Service
- **What happens:**
  - Uploads deployment package to Azure
  - Deploys to frontend App Service
  - Serves Angular app as static files
  - **Uses:** `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND` secret

**Job Name:** `Build and Deploy Frontend (Angular)`

---

### Job 4: Verify Deployment

**Depends on:** All previous jobs complete

#### Step 1: Check Backend Health
- **URL:** `https://echospace-backend-app-dev.azurewebsites.net/swagger`
- **Purpose:** Verifies backend is running and accessible
- **What happens:** Makes HTTP request to Swagger endpoint
- **Note:** Uses `continue-on-error: true` (may take time to start)

#### Step 2: Check Frontend Health
- **URL:** `https://echospace-angular-app-dev.azurewebsites.net`
- **Purpose:** Verifies frontend is running and accessible
- **What happens:** Makes HTTP request to frontend
- **Note:** Uses `continue-on-error: true` (may take time to start)

**Job Name:** `Verify Deployment`

---

## Workflow Summary

### Pull Request Flow
```
PR Created
    ↓
┌─────────────────────────────────────┐
│  PR Build Check (pr-build.yml)     │
│  - Build .NET                       │
│  - Run .NET Tests                   │
│  - Build Angular                    │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  Security Scan (security-scan.yml)  │
│  - Secrets Scan                     │
│  - Dependency Scans                 │
│  - SAST Scans                       │
│  - SBOM Generation                   │
│  - Terraform Validation             │
└─────────────────────────────────────┘
    ↓
All Status Checks Pass
    ↓
Code Review & Approvals (2 required)
    ↓
Merge to main
```

### Deployment Flow
```
Merge to main
    ↓
┌─────────────────────────────────────┐
│  Build & Deploy Backend             │
│  - Build .NET                       │
│  - Run Tests                        │
│  - Publish & Deploy                 │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  Run Database Migrations            │
│  (parallel with frontend)           │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  Build & Deploy Frontend            │
│  - Build Angular                    │
│  - Deploy Static Files              │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  Verify Deployment                  │
│  - Check Backend Health             │
│  - Check Frontend Health            │
└─────────────────────────────────────┘
    ↓
Deployment Complete ✅
```

---

## Key Configuration Files

- **`.github/workflows/pr-build.yml`** - PR build and test workflow
- **`.github/workflows/security-scan.yml`** - Security scanning workflow
- **`.github/workflows/deploy.yml`** - Deployment workflow
- **`.github/CODEOWNERS`** - Code ownership and review assignments
- **`.github/dependabot.yml`** - Automated dependency updates

---

## Environment Variables & Secrets

### Environment Variables (in workflows)
- `AZURE_WEBAPP_NAME_BACKEND`: `echospace-backend-app-dev`
- `AZURE_WEBAPP_NAME_FRONTEND`: `echospace-angular-app-dev`
- `AZURE_RESOURCE_GROUP`: `echospace-resources`
- `DOTNET_VERSION`: `9.0.x`
- `NODE_VERSION`: `20.x`

### GitHub Secrets (required)
- `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND` - Azure publish profile for backend
- `AZURE_WEBAPP_PUBLISH_PROFILE_FRONTEND` - Azure publish profile for frontend
- `AZURE_SQL_CONNECTION_STRING` - Azure SQL Database connection string
- `GITHUB_TOKEN` - Automatically provided by GitHub Actions

---

## Troubleshooting

### PR Build Fails
- Check build errors in `.NET Build` step
- Check test failures in `.NET Tests` step
- Check Angular build errors in `Build Angular project` step

### Security Scan Fails
- **Secrets Scan:** Remove any committed secrets from PR
- **Terraform Validate:** Fix Terraform syntax errors
- **SAST Scans:** Review and fix security issues in code

### Deployment Fails
- Check Azure credentials in GitHub Secrets
- Verify Azure App Service names are correct
- Check database connection string is valid
- Review deployment logs in GitHub Actions

---

## Additional Workflows

### DAST Scan (`dast-scan.yml`)
- **Trigger:** Manual (`workflow_dispatch`) or scheduled (weekly)
- **Purpose:** Dynamic Application Security Testing using OWASP ZAP
- **Usage:** Run manually after deployment to scan live application

---

## Notes

- All workflows run on `ubuntu-latest` runners
- Tests are run but don't block deployment (using `continue-on-error`)
- Security scans run in parallel for faster feedback
- SBOM files are retained for 30 days as artifacts
- Database migrations may skip if already applied (non-blocking)

