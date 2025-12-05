# Quick Deployment Script for Azure App Services
# This script builds and deploys both backend and frontend

param(
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$SkipMigrations
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Quick Deployment..." -ForegroundColor Cyan
Write-Host ""

# Configuration
$ResourceGroup = "echospace-resources"
$BackendAppName = "echospace-backend-app-dev"
$FrontendAppName = "echospace-angular-app-dev"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ProjectRoot

# Deploy Backend
if (-not $SkipBackend) {
    Write-Host "📦 Building Backend (.NET)..." -ForegroundColor Yellow
    
    # Build and publish
    dotnet publish src/EchoSpace.UI/EchoSpace.UI.csproj `
        --configuration Release `
        --output ./publish-backend `
        --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Backend build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "📤 Deploying Backend to Azure..." -ForegroundColor Yellow
    
    # Create zip
    if (Test-Path "./publish-backend.zip") { Remove-Item "./publish-backend.zip" }
    Compress-Archive -Path "./publish-backend/*" -DestinationPath "./publish-backend.zip" -Force
    
    # Deploy
    az webapp deployment source config-zip `
        --resource-group $ResourceGroup `
        --name $BackendAppName `
        --src "./publish-backend.zip"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Backend deployment failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Backend deployed successfully!" -ForegroundColor Green
    Write-Host "   URL: https://$BackendAppName.azurewebsites.net" -ForegroundColor Cyan
    Write-Host ""
}

# Run Database Migrations
if (-not $SkipMigrations) {
    Write-Host "🔄 Running Database Migrations..." -ForegroundColor Yellow
    
    # Get connection string from terraform.tfvars
    $TerraformDir = Join-Path $ProjectRoot "terraform"
    $TfvarsPath = Join-Path $TerraformDir "terraform.tfvars"
    
    if (Test-Path $TfvarsPath) {
        $tfvarsContent = Get-Content $TfvarsPath -Raw
        
        if ($tfvarsContent -match 'sql_admin_password\s*=\s*"([^"]+)"') {
            $sqlPassword = $matches[1]
        } else {
            $sqlPassword = Read-Host "Enter SQL Admin Password"
        }
        
        $connectionString = "Server=tcp:echospace-sql-dev.database.windows.net,1433;Initial Catalog=EchoSpaceDb;Persist Security Info=False;User ID=sqladmin;Password=$sqlPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        
        $env:ConnectionStrings__DefaultConnection = $connectionString
        
        dotnet ef database update `
            --project src/EchoSpace.Infrastructure/EchoSpace.Infrastructure.csproj `
            --startup-project src/EchoSpace.UI/EchoSpace.UI.csproj `
            --connection $connectionString
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Database migrations completed!" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Database migrations may have failed (check output above)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️  Could not find terraform.tfvars, skipping migrations" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Deploy Frontend
if (-not $SkipFrontend) {
    Write-Host "📦 Building Frontend (Angular)..." -ForegroundColor Yellow
    
    $FrontendDir = Join-Path $ProjectRoot "src/EchoSpace.Web.Client"
    Set-Location $FrontendDir
    
    # Install dependencies
    npm ci
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ npm install failed!" -ForegroundColor Red
        exit 1
    }
    
    # Build
    npm run build -- --configuration production
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Frontend build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "📤 Deploying Frontend to Azure..." -ForegroundColor Yellow
    
    # Create zip
    $DistPath = Join-Path $FrontendDir "dist/echo-space.web.client"
    if (Test-Path "../frontend-deploy.zip") { Remove-Item "../frontend-deploy.zip" }
    Compress-Archive -Path "$DistPath/*" -DestinationPath "../frontend-deploy.zip" -Force
    
    # Deploy
    az webapp deployment source config-zip `
        --resource-group $ResourceGroup `
        --name $FrontendAppName `
        --src "../frontend-deploy.zip"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Frontend deployment failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Frontend deployed successfully!" -ForegroundColor Green
    Write-Host "   URL: https://$FrontendAppName.azurewebsites.net" -ForegroundColor Cyan
}

Set-Location $ProjectRoot

Write-Host ""
Write-Host "🎉 Deployment Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Configure App Settings in Azure Portal:" -ForegroundColor White
Write-Host "   - Connection strings" -ForegroundColor Gray
Write-Host "   - JWT secrets" -ForegroundColor Gray
Write-Host "   - API keys" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Test your apps:" -ForegroundColor White
Write-Host "   Backend: https://$BackendAppName.azurewebsites.net/swagger" -ForegroundColor Gray
Write-Host "   Frontend: https://$FrontendAppName.azurewebsites.net" -ForegroundColor Gray
Write-Host ""

