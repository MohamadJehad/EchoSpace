# Local Security Tools Setup Guide

This guide explains how to set up and use security tools locally for development.

## 🛠️ Required Tools

### 1. Pre-commit Hooks

**Installation:**
```bash
# Install pre-commit
pip install pre-commit

# Or using Homebrew (macOS)
brew install pre-commit

# Or using Chocolatey (Windows)
choco install pre-commit
```

**Setup:**
```bash
# Install hooks
pre-commit install

# Run on all files
pre-commit run --all-files

# Run on staged files only (automatic on commit)
pre-commit run
```

**What it checks:**
- ✅ Trailing whitespace
- ✅ File formatting (YAML, JSON)
- ✅ Terraform formatting and validation
- ✅ Secrets detection
- ✅ Code formatting (.NET, TypeScript)
- ✅ Large files
- ✅ Merge conflicts

### 2. Checkov (Terraform Security)

**Installation:**
```bash
# Using pip
pip install checkov

# Or using Homebrew
brew install checkov

# Or using Docker (no installation needed)
docker pull bridgecrew/checkov
```

**Usage:**
```bash
# Scan Terraform files
checkov -d terraform/ --framework terraform

# With Docker
docker run --rm -v $(pwd)/terraform:/src bridgecrew/checkov -d /src --framework terraform
```

### 3. Gitleaks (Secrets Scanner)

**Installation:**
```bash
# macOS
brew install gitleaks

# Linux
wget https://github.com/gitleaks/gitleaks/releases/download/v8.18.0/gitleaks_8.18.0_linux_x64.tar.gz
tar -xzf gitleaks_8.18.0_linux_x64.tar.gz
sudo mv gitleaks /usr/local/bin/

# Windows (using Chocolatey)
choco install gitleaks

# Or using Docker
docker pull zricethezav/gitleaks:latest
```

**Usage:**
```bash
# Scan repository
gitleaks detect --source . --verbose

# Scan specific path
gitleaks detect --source ./src --verbose

# With Docker
docker run --rm -v $(pwd):/path zricethezav/gitleaks:latest detect --source="/path" --verbose
```

### 4. CycloneDX (SBOM Generation)

**Installation:**

**.NET:**
```bash
dotnet tool install --global CycloneDX
```

**Node.js:**
```bash
npm install -g @cyclonedx/cyclonedx-npm
```

**Usage:**
```bash
# Generate .NET SBOM
dotnet CycloneDX EchoSpace.CleanArchitecture.sln -o sbom/

# Generate Node.js SBOM
cd src/EchoSpace.Web.Client
cyclonedx-npm --output-file ../../sbom/npm-sbom.json
```

### 5. Security Code Scan (.NET SAST)

**Installation:**
```bash
# Install as NuGet package
dotnet add package SecurityCodeScan.VS2019 --version 5.6.7

# Or install as global tool
dotnet tool install --global security-scan
```

**Usage:**
```bash
# Run during build
dotnet build /p:SecurityCodeScan=true

# Or use the tool
security-scan EchoSpace.CleanArchitecture.sln
```

## 📜 Local Security Scripts

### Security Scan Script

**Location:** `scripts/security-scan-local.sh`

**Usage:**
```bash
# Make executable (first time)
chmod +x scripts/security-scan-local.sh

# Run scan
./scripts/security-scan-local.sh
```

**What it does:**
- ✅ Terraform security scan (Checkov)
- ✅ Terraform format check
- ✅ Terraform validation
- ✅ Secrets scanning (Gitleaks)
- ✅ .NET dependency scan
- ✅ Node.js dependency scan
- ✅ .NET code formatting check

### SBOM Generation Script

**Location:** `scripts/generate-sbom.sh`

**Usage:**
```bash
# Make executable (first time)
chmod +x scripts/generate-sbom.sh

# Generate SBOMs
./scripts/generate-sbom.sh
```

**Output:**
- `sbom/bom.xml` - .NET SBOM
- `sbom/npm-sbom.json` - Node.js SBOM

## 🔄 Daily Workflow

### Before Committing

1. **Run pre-commit hooks** (automatic):
   ```bash
   git commit -m "Your message"
   # Pre-commit hooks run automatically
   ```

2. **Or run manually**:
   ```bash
   pre-commit run --all-files
   ```

### Before Pushing

1. **Run security scan**:
   ```bash
   ./scripts/security-scan-local.sh
   ```

2. **Fix any issues** found

3. **Push to remote**:
   ```bash
   git push origin your-branch
   ```

### Weekly Tasks

1. **Generate SBOM**:
   ```bash
   ./scripts/generate-sbom.sh
   ```

2. **Update dependencies**:
   ```bash
   # .NET
   dotnet list package --outdated
   
   # Node.js
   npm outdated
   ```

3. **Review security alerts**:
   - Check GitHub Security tab
   - Review Dependabot PRs

## 🐛 Troubleshooting

### Pre-commit Hooks Not Running

```bash
# Reinstall hooks
pre-commit uninstall
pre-commit install

# Check if hooks are installed
ls -la .git/hooks/
```

### Checkov Not Found

```bash
# Verify installation
checkov --version

# Or use Docker
docker run --rm bridgecrew/checkov --version
```

### Gitleaks False Positives

Create `.gitleaksignore` file:
```
# Ignore specific files
*.test.json
*.example.env
```

### Terraform Validation Fails

```bash
# Format Terraform files
terraform fmt -recursive

# Validate again
terraform validate
```

## 📚 Additional Tools (Optional)

### SonarQube (Cloud)

- Free tier available
- Comprehensive code analysis
- Integrates with GitHub

### Snyk

```bash
# Install Snyk CLI
npm install -g snyk

# Authenticate
snyk auth

# Test
snyk test
snyk monitor
```

### OWASP ZAP (DAST)

```bash
# Using Docker
docker run -t owasp/zap2docker-stable zap-baseline.py -t https://your-app.com
```

## ✅ Quick Setup Checklist

- [ ] Install pre-commit: `pip install pre-commit`
- [ ] Install Checkov: `pip install checkov`
- [ ] Install Gitleaks: `brew install gitleaks` (or Docker)
- [ ] Install CycloneDX: `dotnet tool install -g CycloneDX`
- [ ] Install CycloneDX npm: `npm install -g @cyclonedx/cyclonedx-npm`
- [ ] Run `pre-commit install`
- [ ] Test: `pre-commit run --all-files`
- [ ] Test: `./scripts/security-scan-local.sh`

## 🎯 Integration with IDE

### Visual Studio Code

Install extensions:
- **Terraform** (HashiCorp)
- **Checkov** (Bridgecrew)
- **ESLint** (Microsoft)
- **C#** (Microsoft)

### Visual Studio

- Security Code Scan (NuGet package)
- SonarLint extension
- Code Analysis rules

---

**Remember**: Run security scans regularly, not just before committing!

