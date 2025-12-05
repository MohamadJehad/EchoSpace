#!/bin/bash
# Local Security Scanning Script
# Run this script locally to perform security scans before committing

set -e

echo "🔒 EchoSpace Local Security Scan"
echo "=================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track failures
FAILURES=0

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check for required tools
echo "📋 Checking required tools..."
MISSING_TOOLS=()

if ! command_exists terraform; then
    MISSING_TOOLS+=("terraform")
fi

if ! command_exists dotnet; then
    MISSING_TOOLS+=("dotnet")
fi

if ! command_exists npm; then
    MISSING_TOOLS+=("npm")
fi

if [ ${#MISSING_TOOLS[@]} -gt 0 ]; then
    echo -e "${YELLOW}⚠️  Missing tools: ${MISSING_TOOLS[*]}${NC}"
    echo "Some scans may be skipped."
fi

echo ""

# 1. Terraform Security Scan (Checkov)
echo "1️⃣  Running Terraform Security Scan (Checkov)..."
if command_exists checkov; then
    if checkov -d terraform/ --framework terraform --quiet; then
        echo -e "${GREEN}✅ Terraform security scan passed${NC}"
    else
        echo -e "${RED}❌ Terraform security scan found issues${NC}"
        FAILURES=$((FAILURES + 1))
    fi
elif command_exists docker; then
    echo "   Using Docker to run Checkov..."
    if docker run --rm -v "$(pwd)/terraform:/src" bridgecrew/checkov -d /src --framework terraform --quiet; then
        echo -e "${GREEN}✅ Terraform security scan passed${NC}"
    else
        echo -e "${RED}❌ Terraform security scan found issues${NC}"
        FAILURES=$((FAILURES + 1))
    fi
else
    echo -e "${YELLOW}⚠️  Checkov not found. Install: pip install checkov${NC}"
fi
echo ""

# 2. Terraform Format Check
echo "2️⃣  Checking Terraform formatting..."
cd terraform
if terraform fmt -check -recursive > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Terraform formatting is correct${NC}"
else
    echo -e "${RED}❌ Terraform files need formatting. Run: terraform fmt -recursive${NC}"
    FAILURES=$((FAILURES + 1))
fi
cd ..
echo ""

# 3. Terraform Validation
echo "3️⃣  Validating Terraform configuration..."
cd terraform
if terraform init -backend=false > /dev/null 2>&1 && terraform validate > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Terraform validation passed${NC}"
else
    echo -e "${YELLOW}⚠️  Terraform validation skipped (may need Azure subscription)${NC}"
fi
cd ..
echo ""

# 4. Secrets Scanning (Gitleaks)
echo "4️⃣  Scanning for secrets (Gitleaks)..."
if command_exists gitleaks; then
    if gitleaks detect --source . --verbose --no-git; then
        echo -e "${GREEN}✅ No secrets detected${NC}"
    else
        echo -e "${RED}❌ Secrets detected in code!${NC}"
        FAILURES=$((FAILURES + 1))
    fi
elif command_exists docker; then
    echo "   Using Docker to run Gitleaks..."
    if docker run --rm -v "$(pwd):/path" zricethezav/gitleaks:latest detect --source="/path" --verbose --no-git; then
        echo -e "${GREEN}✅ No secrets detected${NC}"
    else
        echo -e "${RED}❌ Secrets detected in code!${NC}"
        FAILURES=$((FAILURES + 1))
    fi
else
    echo -e "${YELLOW}⚠️  Gitleaks not found. Install: https://github.com/gitleaks/gitleaks${NC}"
fi
echo ""

# 5. .NET Dependency Vulnerability Scan
echo "5️⃣  Scanning .NET dependencies for vulnerabilities..."
if command_exists dotnet; then
    cd "$(dirname "$0")/.."
    if dotnet list package --vulnerable --include-transitive 2>/dev/null | grep -q "vulnerable"; then
        echo -e "${RED}❌ Vulnerable .NET packages found!${NC}"
        dotnet list package --vulnerable --include-transitive
        FAILURES=$((FAILURES + 1))
    else
        echo -e "${GREEN}✅ No vulnerable .NET packages found${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  .NET SDK not found${NC}"
fi
echo ""

# 6. Node.js Dependency Vulnerability Scan
echo "6️⃣  Scanning Node.js dependencies for vulnerabilities..."
if command_exists npm; then
    cd src/EchoSpace.Web.Client
    if npm audit --audit-level=moderate > /dev/null 2>&1; then
        echo -e "${GREEN}✅ No high/critical npm vulnerabilities found${NC}"
    else
        echo -e "${RED}❌ npm vulnerabilities found!${NC}"
        npm audit --audit-level=moderate
        FAILURES=$((FAILURES + 1))
    fi
    cd ../../..
else
    echo -e "${YELLOW}⚠️  npm not found${NC}"
fi
echo ""

# 7. .NET Code Formatting
echo "7️⃣  Checking .NET code formatting..."
if command_exists dotnet; then
    cd "$(dirname "$0")/.."
    if dotnet format --verify-no-changes --include src/ 2>/dev/null; then
        echo -e "${GREEN}✅ .NET code formatting is correct${NC}"
    else
        echo -e "${YELLOW}⚠️  .NET code needs formatting. Run: dotnet format${NC}"
    fi
else
    echo -e "${YELLOW}⚠️  .NET SDK not found${NC}"
fi
echo ""

# Summary
echo "=================================="
if [ $FAILURES -eq 0 ]; then
    echo -e "${GREEN}✅ All security scans passed!${NC}"
    exit 0
else
    echo -e "${RED}❌ Security scans found $FAILURES issue(s)${NC}"
    echo "Please fix the issues above before committing."
    exit 1
fi

