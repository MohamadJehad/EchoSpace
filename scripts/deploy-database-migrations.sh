#!/bin/bash
# Database Migration Script for Azure
# This script runs Entity Framework migrations against Azure SQL Database

set -e

echo "🚀 Starting database migration..."

# Check if connection string is provided
if [ -z "$CONNECTION_STRING" ]; then
    echo "❌ Error: CONNECTION_STRING environment variable is not set"
    echo "Usage: CONNECTION_STRING='your-connection-string' ./deploy-database-migrations.sh"
    exit 1
fi

# Navigate to project root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
cd "$PROJECT_ROOT"

echo "📦 Restoring dependencies..."
dotnet restore EchoSpace.CleanArchitecture.sln

echo "🔄 Running database migrations..."
dotnet ef database update \
    --project src/EchoSpace.Infrastructure/EchoSpace.Infrastructure.csproj \
    --startup-project src/EchoSpace.UI/EchoSpace.UI.csproj \
    --connection "$CONNECTION_STRING" \
    --verbose

echo "✅ Database migration completed successfully!"

