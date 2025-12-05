# DevSecOps Implementation Plan for EchoSpace

## Overview

This document outlines the complete DevSecOps implementation plan for EchoSpace, aligned with Milestone 5 requirements. This plan focuses on Azure App Service deployment (without Docker/Kubernetes) while maintaining all security best practices.

---

## Table of Contents

1. [Current State Assessment](#current-state-assessment)
2. [Implementation Phases](#implementation-phases)
3. [Immediate Action Items](#immediate-action-items)
4. [Tool Recommendations](#tool-recommendations)
5. [Azure App Service Specific Security](#azure-app-service-specific-security)
6. [File Structure](#file-structure)

---

## Current State Assessment

### ✅ What You Have

- .NET 9.0 backend API (ASP.NET Core)
- Angular frontend
- Basic CI/CD (GitHub Actions PR build)
- Terraform IaC for Azure
- Clean architecture
- Authentication/authorization implemented

### ❌ What's Missing (Per Milestone 5 Requirements)

- Security scanning in CI/CD
- Secrets management (Azure Key Vault)
- SBOM generation
- IaC security scanning
- Branch protection policies
- Runtime security monitoring
- Comprehensive security automation suite

---

## Implementation Phases

### Phase 1: Infrastructure as Code (IaC) Governance

#### 1.1 Enhance Terraform Structure

Create modular Terraform modules:

```
terraform/
├── modules/
│   ├── networking/     # VNet, subnets, NSGs
│   ├── app-service/   # App Service plans and apps
│   ├── database/      # SQL Server with security
│   ├── security/      # Key Vault, IAM policies
│   └── monitoring/    # Log Analytics, Application Insights
├── environments/
│   ├── dev/
│   ├── staging/
│   └── prod/
└── policies/          # Azure Policy definitions
```

**Tasks:**
- [ ] Create Terraform backend configuration (Azure Storage)
- [ ] Implement Terraform state locking
- [ ] Create reusable modules for App Service
- [ ] Standardize configuration templates
- [ ] Add environment-specific configurations

#### 1.2 Add IaC Security Scanning

**Tasks:**
- [ ] Add Checkov to CI/CD pipeline
- [ ] Configure Checkov rules for Azure
- [ ] Scan Terraform files on every PR
- [ ] Block PRs with critical IaC issues
- [ ] Generate IaC security reports

**Implementation:**
- Add Checkov step in `.github/workflows/security-scan.yml`
- Configure `.checkov.yml` with custom rules
- Set severity thresholds (fail on HIGH/CRITICAL)

#### 1.3 Azure App Service Templates

**Tasks:**
- [ ] Create reusable Terraform module for App Service
- [ ] Standardize security configurations:
  - HTTPS enforcement
  - Minimum TLS version (1.2)
  - Always On configuration
  - Managed Identity
- [ ] Template for staging and production environments
- [ ] Deployment slot configuration

#### 1.4 Azure Policy and Guardrails

**Tasks:**
- [ ] Create Azure Policy definitions:
  - Enforce HTTPS-only
  - Require minimum TLS version
  - Enforce resource naming conventions
  - Require tags on resources
  - Deny public access where not needed
- [ ] Apply policies via Terraform
- [ ] Document policy exceptions process

---

### Phase 2: CI/CD Security Automation

#### 2.1 SAST (Static Application Security Testing)

**For .NET Backend:**
- [ ] Add Security Code Scan NuGet package
- [ ] Configure SonarQube (optional, cloud-based)
- [ ] Run SAST on every commit/PR
- [ ] Fail build on critical vulnerabilities

**For TypeScript/Angular Frontend:**
- [ ] Add ESLint security plugins
- [ ] Configure Semgrep for TypeScript
- [ ] Run SAST on every commit/PR
- [ ] Fail build on critical vulnerabilities

**Implementation:**
- Add SAST steps to `.github/workflows/security-scan.yml`
- Configure severity thresholds
- Generate SARIF reports for GitHub Security tab

#### 2.2 Dependency Vulnerability Scanning

**For .NET:**
- [ ] Add `dotnet list package --vulnerable` check
- [ ] Configure Dependabot for .NET
- [ ] Add Snyk scanning (optional)
- [ ] Block builds on critical vulnerabilities

**For Node.js/Angular:**
- [ ] Add `npm audit` check
- [ ] Configure Dependabot for npm
- [ ] Add Snyk scanning (optional)
- [ ] Block builds on critical vulnerabilities

**Implementation:**
- Add dependency scanning to `.github/workflows/security-scan.yml`
- Configure Dependabot in `.github/dependabot.yml`
- Set vulnerability thresholds

#### 2.3 IaC Security Scanner

**Tasks:**
- [ ] Add Checkov step in CI/CD
- [ ] Scan all Terraform files
- [ ] Fail on high-severity issues
- [ ] Generate security reports

**Implementation:**
```yaml
- name: Run Checkov
  uses: bridgecrewio/checkov-action@master
  with:
    directory: terraform/
    framework: terraform
    fail_on: HIGH
```

#### 2.4 Secrets Scanner

**Tasks:**
- [ ] Add Gitleaks to CI/CD
- [ ] Scan commits for secrets
- [ ] Block PRs with detected secrets
- [ ] Configure allowed patterns (if needed)

**Implementation:**
```yaml
- name: Run Gitleaks
  uses: gitleaks/gitleaks-action@v2
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

#### 2.5 SCA and License Compliance

**Tasks:**
- [ ] Generate SBOM using CycloneDX
  - For .NET: `dotnet CycloneDX`
  - For npm: `cyclonedx-npm`
- [ ] Check open-source licenses
- [ ] Block incompatible licenses
- [ ] Publish SBOM as artifact

**Implementation:**
- Add CycloneDX tools to CI/CD
- Generate SBOM on every release
- Store SBOM in repository or artifact storage

#### 2.6 Cloud Configuration Security Scanner

**Tasks:**
- [ ] Add Prowler or Azure Security Center checks
- [ ] Validate Azure resource configurations
- [ ] Report misconfigurations
- [ ] Generate compliance reports

**Implementation:**
- Run Prowler scans post-deployment
- Integrate Azure Security Center API
- Generate security posture reports

#### 2.7 DAST (Dynamic Application Security Testing)

**Tasks:**
- [ ] Add OWASP ZAP to CI/CD
- [ ] Run DAST after deployment to staging
- [ ] Generate security reports
- [ ] Fail deployment on critical findings

**Implementation:**
- Add OWASP ZAP step after staging deployment
- Configure baseline scans
- Generate HTML/JSON reports

---

### Phase 3: GitOps and Change Management

#### 3.1 Branch Protection Rules

**Tasks:**
- [ ] Configure branch protection for `main`:
  - Require PR reviews (2 approvals)
  - Require status checks to pass
  - Require branches to be up to date
  - Enforce linear history
  - Restrict who can push
- [ ] Configure branch protection for `production`
- [ ] Document branch strategy

**GitHub Settings:**
- Settings → Branches → Add rule
- Configure required reviewers
- Require status checks: `security-scan`, `build`, `test`

#### 3.2 Azure App Service Deployment Automation

**Tasks:**
- [ ] Create deployment workflow for staging
- [ ] Create deployment workflow for production
- [ ] Use Terraform for infrastructure changes
- [ ] Configure deployment slots
- [ ] Implement auto-rollback on health check failures
- [ ] Add deployment approvals for production

**Implementation:**
- `.github/workflows/deploy-staging.yml`
- `.github/workflows/deploy-production.yml`
- Use Azure CLI or GitHub Actions for App Service deployment

#### 3.3 Policy as Code

**Tasks:**
- [ ] Create Azure Policy definitions in Terraform
- [ ] Enforce resource naming conventions
- [ ] Enforce tagging standards
- [ ] Enforce security standards
- [ ] Document policy exceptions process

---

### Phase 4: Secrets Management

#### 4.1 Azure Key Vault Integration

**Tasks:**
- [ ] Create Key Vault via Terraform
- [ ] Store secrets:
  - JWT signing keys
  - Database connection strings
  - API keys (Google, Gemini, etc.)
  - SMTP credentials
- [ ] Use Managed Identity for access
- [ ] Configure secret rotation policies

**Implementation:**
- Create Key Vault Terraform module
- Update `Program.cs` to use Key Vault
- Configure App Service Managed Identity

#### 4.2 GitHub Secrets

**Tasks:**
- [ ] Move sensitive CI/CD variables to GitHub Secrets:
  - Azure service principal credentials
  - Deployment tokens
  - API keys for external services
- [ ] Use secrets securely in workflows
- [ ] Rotate secrets regularly

#### 4.3 Runtime Secrets Injection

**Tasks:**
- [ ] Configure App Service Application Settings with Key Vault references
- [ ] Use format: `@Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/secret-name/)`
- [ ] Remove hardcoded secrets from config files
- [ ] Document secret management process

**Example:**
```json
{
  "Jwt:Key": "@Microsoft.KeyVault(SecretUri=https://echospace-vault.vault.azure.net/secrets/jwt-key/)",
  "ConnectionStrings:DefaultConnection": "@Microsoft.KeyVault(SecretUri=https://echospace-vault.vault.azure.net/secrets/db-connection/)"
}
```

---

### Phase 5: Monitoring and Runtime Security

#### 5.1 Application Monitoring

**Tasks:**
- [ ] Set up Azure Application Insights
- [ ] Configure custom metrics
- [ ] Set up alerts for:
  - High error rates
  - Slow response times
  - Failed authentication attempts
- [ ] Create dashboards

**Implementation:**
- Add Application Insights SDK to project
- Configure in `Program.cs`
- Create Application Insights resource via Terraform

#### 5.2 Security Monitoring

**Tasks:**
- [ ] Enable Azure Security Center
- [ ] Configure security alerts
- [ ] Monitor failed authentication attempts
- [ ] Detect unusual access patterns
- [ ] Set up security incident response

#### 5.3 Audit Logging

**Tasks:**
- [ ] Enhance existing AuditLog entity
- [ ] Centralize logs in Azure Log Analytics
- [ ] Correlate security events
- [ ] Set up log retention policies
- [ ] Create audit reports

#### 5.4 Azure App Service Monitoring

**Tasks:**
- [ ] Configure health checks
- [ ] Set up auto-healing
- [ ] Monitor deployment logs
- [ ] Integrate application logs
- [ ] Set up alerting

---

## Immediate Action Items (Priority Order)

### Week 1: Foundation

1. **Enhance Terraform with Security Modules**
   - [ ] Create `terraform/modules/app-service/` module
   - [ ] Create `terraform/modules/key-vault/` module
   - [ ] Create `terraform/modules/monitoring/` module
   - [ ] Add security configurations to modules

2. **Add Security Scanning to CI/CD**
   - [ ] Create `.github/workflows/security-scan.yml`
   - [ ] Add Gitleaks for secrets scanning
   - [ ] Add Dependabot configuration
   - [ ] Add basic SAST tools

3. **Set Up Branch Protection Rules**
   - [ ] Configure `main` branch protection
   - [ ] Configure `production` branch protection
   - [ ] Document branch strategy

### Week 2: Enhanced Scanning

4. **Add Checkov for Terraform Scanning**
   - [ ] Add Checkov to CI/CD
   - [ ] Configure `.checkov.yml`
   - [ ] Set severity thresholds

5. **Add SBOM Generation**
   - [ ] Install CycloneDX tools
   - [ ] Generate SBOM for .NET projects
   - [ ] Generate SBOM for npm packages
   - [ ] Publish SBOM as artifact

6. **Add SAST Tools**
   - [ ] Add Security Code Scan for .NET
   - [ ] Add ESLint security plugins
   - [ ] Configure Semgrep

### Week 3: Secrets and Governance

7. **Set Up Azure Key Vault**
   - [ ] Create Key Vault via Terraform
   - [ ] Configure Managed Identity
   - [ ] Migrate secrets to Key Vault

8. **Migrate Secrets**
   - [ ] Move JWT keys to Key Vault
   - [ ] Move connection strings to Key Vault
   - [ ] Move API keys to Key Vault
   - [ ] Update code to use Key Vault references

9. **Configure App Service Key Vault Integration**
   - [ ] Update App Service settings
   - [ ] Use Key Vault references
   - [ ] Test secret retrieval

10. **Enhance Terraform with Security Policies**
    - [ ] Create Azure Policy definitions
    - [ ] Apply policies via Terraform
    - [ ] Document exceptions process

### Week 4: Monitoring and Deployment

11. **Set Up Application Insights**
    - [ ] Create Application Insights resource
    - [ ] Add SDK to application
    - [ ] Configure custom metrics
    - [ ] Set up alerts

12. **Create Deployment Workflows**
    - [ ] Create staging deployment workflow
    - [ ] Create production deployment workflow
    - [ ] Add deployment approvals
    - [ ] Configure deployment slots

13. **Add DAST Testing**
    - [ ] Add OWASP ZAP to CI/CD
    - [ ] Run DAST after staging deployment
    - [ ] Generate security reports

14. **Set Up Azure Security Center Monitoring**
    - [ ] Enable Security Center
    - [ ] Configure security alerts
    - [ ] Set up incident response

---

## Tool Recommendations

| Category | Tool | Why | Priority |
|----------|------|-----|----------|
| **SAST (.NET)** | Security Code Scan | .NET-specific vulnerabilities, free | High |
| **SAST (.NET)** | SonarQube | Comprehensive analysis, cloud option | Medium |
| **SAST (TypeScript)** | ESLint + security plugins | Built-in, fast | High |
| **SAST (TypeScript)** | Semgrep | Multi-language, CI/CD friendly | Medium |
| **Dependency Scanner** | Dependabot | Native GitHub integration, free | High |
| **Dependency Scanner** | Snyk | Advanced features, multi-language | Medium |
| **IaC Scanner** | Checkov | Terraform + Azure support, free | High |
| **Secrets Scanner** | Gitleaks | Fast, CI/CD friendly, free | High |
| **SBOM Generator** | CycloneDX | Industry standard, multi-language | High |
| **DAST** | OWASP ZAP | Free, CI/CD integration | High |
| **Cloud Scanner** | Prowler | Azure support, comprehensive | Medium |
| **Cloud Scanner** | Azure Security Center | Native Azure, integrated | High |
| **Secrets Management** | Azure Key Vault | Native Azure integration | High |
| **Monitoring** | Application Insights | Azure-native, comprehensive | High |
| **Deployment** | GitHub Actions + Azure CLI | Direct App Service deployment | High |

---

## Azure App Service Specific Security Practices

### 1. Deployment Slots

- Use staging slot for testing
- Swap to production after validation
- Auto-rollback on health check failures

**Configuration:**
```terraform
resource "azurerm_linux_web_app_slot" "staging" {
  name           = "staging"
  app_service_id = azurerm_linux_web_app.backend.id
  
  site_config {
    # Configuration
  }
}
```

### 2. Managed Identity

- Use Managed Identity for Key Vault access
- No secrets in App Service settings
- Eliminate credential management

**Configuration:**
```terraform
resource "azurerm_linux_web_app" "backend" {
  # ... other config ...
  
  identity {
    type = "SystemAssigned"
  }
}
```

### 3. HTTPS Enforcement

- Force HTTPS in App Service configuration
- TLS 1.2 minimum
- Custom domains with SSL certificates

**Configuration:**
```terraform
site_config {
  https_only = true
  minimum_tls_version = "1.2"
}
```

### 4. Network Security

- VNet integration (if needed)
- IP restrictions
- Private endpoints for database/Key Vault

**Configuration:**
```terraform
resource "azurerm_app_service_virtual_network_swift_connection" "vnet" {
  app_service_id = azurerm_linux_web_app.backend.id
  subnet_id     = azurerm_subnet.app_service.id
}
```

### 5. Application Settings Security

- Use Key Vault references instead of plain values
- Format: `@Microsoft.KeyVault(SecretUri=https://vault.vault.azure.net/secrets/secret-name/)`

**Example:**
```terraform
app_settings = {
  "Jwt__Key" = "@Microsoft.KeyVault(SecretUri=https://echospace-vault.vault.azure.net/secrets/jwt-key/)"
  "ConnectionStrings__DefaultConnection" = "@Microsoft.KeyVault(SecretUri=https://echospace-vault.vault.azure.net/secrets/db-connection/)"
}
```

---

## File Structure

### New Files to Create

```
.github/
├── workflows/
│   ├── security-scan.yml          # Security scanning workflow
│   ├── deploy-staging.yml         # Staging deployment to App Service
│   ├── deploy-production.yml     # Production deployment to App Service
│   └── terraform-plan.yml         # Terraform validation workflow
├── dependabot.yml                  # Dependency updates
└── CODEOWNERS                      # Code ownership

terraform/
├── modules/
│   ├── app-service/
│   │   ├── main.tf                # App Service with security defaults
│   │   ├── variables.tf
│   │   └── outputs.tf
│   ├── key-vault/
│   │   ├── main.tf                # Key Vault module
│   │   ├── variables.tf
│   │   └── outputs.tf
│   ├── monitoring/
│   │   ├── main.tf                # Application Insights module
│   │   ├── variables.tf
│   │   └── outputs.tf
│   └── security/
│       ├── main.tf                # Security policies
│       ├── variables.tf
│       └── outputs.tf
├── environments/
│   ├── dev/
│   │   ├── main.tf
│   │   └── terraform.tfvars
│   ├── staging/
│   │   ├── main.tf
│   │   └── terraform.tfvars
│   └── prod/
│       ├── main.tf
│       └── terraform.tfvars
└── .checkov.yml                    # Checkov config

docs/
└── devsecops/
    ├── DEVSECOPS_IMPLEMENTATION_PLAN.md  # This file
    ├── SECURITY_SCANNING.md
    ├── SECRETS_MANAGEMENT.md
    ├── DEPLOYMENT_PROCESS.md
    └── AZURE_APP_SERVICE_SECURITY.md
```

### Files to Enhance

- `.github/workflows/pr-build.yml` - Add security steps
- `terraform/main.tf` - Refactor to use modules, add security configurations
- `src/EchoSpace.UI/Program.cs` - Add Key Vault integration
- `src/EchoSpace.UI/appsettings.json.template` - Document Key Vault references

---

## Success Criteria

### Milestone 5 Requirements Checklist

- [x] Governed change management that allows for rapid development and multi-releases per week
- [x] Security infrastructure governance by utilizing Infrastructure-as-Code (IaC) controls
- [x] Security guidelines through templating of IaC (App Service templates, Terraform modules)
- [x] Guard rails for code repository branching and code-merging
- [x] Guard rails for infrastructure automated changes
- [x] Security automation suite through CI/CD pipelines including:
  - [x] Code SAST scanning
  - [x] Deployment DAST Testing
  - [x] Dependencies vulnerability scanner
  - [x] IaC Security Scanner
  - [x] Cloud configuration security scanner
  - [x] SCA scanning and open source license checks
  - [x] Secrets and PII scanner
  - [x] SBOM dynamic construction and publishing

---

## Next Steps

1. **Review this plan** with your team
2. **Prioritize phases** based on business needs
3. **Start with Week 1 tasks** (Foundation)
4. **Iterate and improve** based on feedback

---

## Resources

- [OWASP Cloud-native Top 10 Project](https://owasp.org/www-project-cloud-native-top-10/)
- [NIST 800-204C, Implementation of DevSecOps for Microservices](https://csrc.nist.gov/publications/detail/sp/800-204c/final)
- [The DoD DevSecOps Playbook](https://dodcio.defense.gov/Library/)
- [SBOM vs. SCA](https://www.synopsys.com/software-integrity/application-security/software-composition-analysis-tools.html)
- [Gitlab DevSecOps Survey](https://about.gitlab.com/developer-survey/)
- [Azure App Service Security Best Practices](https://docs.microsoft.com/azure/app-service/security-recommendations)

---

## Document Version

- **Version:** 1.0
- **Last Updated:** 2024
- **Author:** DevSecOps Implementation Team

