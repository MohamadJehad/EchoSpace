#!/bin/bash
# SBOM Generation Script
# Generates Software Bill of Materials for .NET and Node.js projects

set -e

echo "📦 Generating Software Bill of Materials (SBOM)"
echo "=============================================="
echo ""

# Create SBOM directory
SBOM_DIR="sbom"
mkdir -p "$SBOM_DIR"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# 1. Generate .NET SBOM
echo "1️⃣  Generating .NET SBOM..."
if command -v dotnet >/dev/null 2>&1; then
    # Check if CycloneDX tool is installed
    if dotnet tool list -g | grep -q "cyclonedx"; then
        echo "   Using CycloneDX .NET tool..."
        dotnet CycloneDX EchoSpace.CleanArchitecture.sln -o "$SBOM_DIR"
        echo -e "${GREEN}✅ .NET SBOM generated: $SBOM_DIR/bom.xml${NC}"
    else
        echo -e "${YELLOW}⚠️  CycloneDX .NET tool not installed${NC}"
        echo "   Install: dotnet tool install --global CycloneDX"
    fi
else
    echo -e "${YELLOW}⚠️  .NET SDK not found${NC}"
fi
echo ""

# 2. Generate Node.js SBOM
echo "2️⃣  Generating Node.js SBOM..."
if command -v npm >/dev/null 2>&1; then
    cd src/EchoSpace.Web.Client
    
    # Check if cyclonedx-npm is installed
    if command -v cyclonedx-npm >/dev/null 2>&1; then
        echo "   Using CycloneDX npm tool..."
        cyclonedx-npm --output-file "../../$SBOM_DIR/npm-sbom.json"
        echo -e "${GREEN}✅ Node.js SBOM generated: $SBOM_DIR/npm-sbom.json${NC}"
    else
        echo -e "${YELLOW}⚠️  CycloneDX npm tool not installed${NC}"
        echo "   Install: npm install -g @cyclonedx/cyclonedx-npm"
        echo "   Or using npx..."
        npx @cyclonedx/cyclonedx-npm --output-file "../../$SBOM_DIR/npm-sbom.json" || true
    fi
    
    cd ../../..
else
    echo -e "${YELLOW}⚠️  npm not found${NC}"
fi
echo ""

# Summary
echo "=============================================="
echo -e "${GREEN}✅ SBOM generation complete!${NC}"
echo ""
echo "SBOM files location:"
ls -lh "$SBOM_DIR"/* 2>/dev/null || echo "No SBOM files generated"
echo ""
echo "These files can be used for:"
echo "  - Security vulnerability tracking"
echo "  - License compliance"
echo "  - Supply chain management"
echo "  - Compliance reporting"

