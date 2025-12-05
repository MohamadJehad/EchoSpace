# Helper script to get SQL connection string from Terraform outputs
# Usage: .\scripts\get-connection-string.ps1

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$TerraformDir = Join-Path $ProjectRoot "terraform"

Set-Location $TerraformDir

Write-Host "`n🔍 Getting Azure SQL Connection String..." -ForegroundColor Cyan
Write-Host ""

# Get values from Terraform
$sqlServerFqdn = terraform output -raw sql_server_fqdn
$sqlDatabaseName = terraform output -raw sql_database_name

# Read from terraform.tfvars
$tfvarsPath = Join-Path $TerraformDir "terraform.tfvars"
$tfvarsContent = Get-Content $tfvarsPath -Raw

# Extract values using regex
if ($tfvarsContent -match 'sql_admin_login\s*=\s*"([^"]+)"') {
    $sqlAdminLogin = $matches[1]
} else {
    $sqlAdminLogin = "sqladmin"
}

if ($tfvarsContent -match 'sql_admin_password\s*=\s*"([^"]+)"') {
    $sqlAdminPassword = $matches[1]
} else {
    Write-Host "⚠️  Warning: Could not find sql_admin_password in terraform.tfvars" -ForegroundColor Yellow
    $sqlAdminPassword = Read-Host "Enter SQL Admin Password"
}

# Build connection string
$connectionString = "Server=tcp:$sqlServerFqdn,1433;Initial Catalog=$sqlDatabaseName;Persist Security Info=False;User ID=$sqlAdminLogin;Password=$sqlAdminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "📋 SQL Connection String:" -ForegroundColor Green
Write-Host $connectionString -ForegroundColor White
Write-Host ""
Write-Host "💡 Copy this to GitHub Secret: AZURE_SQL_CONNECTION_STRING" -ForegroundColor Yellow
Write-Host ""

# Copy to clipboard if available
try {
    $connectionString | Set-Clipboard
    Write-Host "✅ Connection string copied to clipboard!" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Could not copy to clipboard. Please copy manually." -ForegroundColor Yellow
}

