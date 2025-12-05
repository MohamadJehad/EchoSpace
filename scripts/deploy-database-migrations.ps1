# Database Migration Script for Azure (PowerShell)
# This script runs Entity Framework migrations against Azure SQL Database

param(
    [Parameter(Mandatory=$true)]
    [string]$ConnectionString
)

Write-Host "🚀 Starting database migration..." -ForegroundColor Cyan

# Navigate to project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
Set-Location $ProjectRoot

Write-Host "📦 Restoring dependencies..." -ForegroundColor Yellow
dotnet restore EchoSpace.CleanArchitecture.sln

Write-Host "🔄 Running database migrations..." -ForegroundColor Yellow
dotnet ef database update `
    --project src/EchoSpace.Infrastructure/EchoSpace.Infrastructure.csproj `
    --startup-project src/EchoSpace.UI/EchoSpace.UI.csproj `
    --connection $ConnectionString `
    --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database migration completed successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Database migration failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

