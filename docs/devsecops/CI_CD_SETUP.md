# CI/CD Security Setup Guide

This guide explains the CI/CD security workflows and how they integrate with your DevSecOps pipeline.

## ✅ What's Been Set Up

### 1. Security Scanning Workflow (`.github/workflows/security-scan.yml`)

A comprehensive security scanning workflow that runs on every PR and push to main/develop branches.

#### Security Checks Included:

1. **Terraform Security (Checkov)**
   - Scans all Terraform files for security misconfigurations
   - Checks Azure-specific security best practices
   - Uploads results to GitHub Security tab
   - Fails on HIGH and CRITICAL issues

2. **Secrets Scanning (Gitleaks)**
   - Scans commits for exposed secrets
   - Prevents accidental secret commits
   - Fails if secrets are detected

3. **.NET Dependency Scanning**
   - Checks for vulnerable NuGet packages
   - Includes transitive dependencies
   - Fails on any vulnerabilities found

4. **Node.js Dependency Scanning**
   - Runs `npm audit` on Angular project
   - Checks for HIGH and CRITICAL vulnerabilities
   - Fails if critical issues found

5. **.NET SAST (Security Code Scan)**
   - Static analysis of .NET code
   - Detects security vulnerabilities in C# code
   - Generates security reports

6. **TypeScript SAST (ESLint Security)**
   - Security-focused linting for TypeScript
   - Detects common security issues
   - Integrates with ESLint

7. **SBOM Generation**
   - Generates Software Bill of Materials for .NET and npm
   - Uses CycloneDX format
   - Uploads as artifacts

8. **Terraform Validation**
   - Validates Terraform syntax
   - Checks formatting
   - Runs `terraform plan` (dry run)

### 2. Dependabot Configuration (`.github/dependabot.yml`)

Automatically creates PRs for dependency updates:

- **.NET NuGet packages** - Weekly updates
- **Node.js npm packages** - Weekly updates
- **GitHub Actions** - Weekly updates
- **Terraform providers** - Monthly updates

Features:
- Groups related updates together
- Limits open PRs to prevent spam
- Adds appropriate labels
- Ignores major version updates (for manual review)

### 3. CODEOWNERS (`.github/CODEOWNERS`)

Defines code ownership for automatic PR review requests:

- Infrastructure → DevOps team
- Backend → Backend team
- Frontend → Frontend team
- Tests → QA team

### 4. Enhanced PR Build Workflow

Updated existing `pr-build.yml` to:
- Work with branch protection rules
- Run in parallel with security scans
- Support multiple branches

## 🚀 How It Works

### On Pull Request:

1. **PR Build Check** runs:
   - Builds .NET solution
   - Runs tests
   - Builds Angular frontend

2. **Security Scan** runs in parallel:
   - Terraform security check
   - Secrets scan
   - Dependency scans
   - SAST scans
   - SBOM generation
   - Terraform validation

3. **Dependabot** (if enabled):
   - Automatically creates PRs for dependency updates
   - Runs security scans on those PRs too

### On Push to Main/Develop:

- Same security scans run
- Ensures main branch stays secure

## 📋 Required GitHub Settings

### 1. Enable Required Status Checks

Go to: **Settings → Branches → Add rule**

For `main` branch:
- ✅ Require a pull request before merging
- ✅ Require approvals: 2
- ✅ Require status checks to pass before merging
- ✅ Required status checks:
  - `Build and Test` (from pr-build.yml)
  - `Terraform Security Scan (Checkov)` (from security-scan.yml)
  - `Secrets Scan (Gitleaks)` (from security-scan.yml)
  - `Terraform Validate` (from security-scan.yml)

### 2. Update CODEOWNERS

Edit `.github/CODEOWNERS` and replace placeholder teams:
- `@echospace-team` → Your actual team
- `@backend-team` → Your backend team
- `@frontend-team` → Your frontend team
- `@devops-team` → Your DevOps team

### 3. Configure Dependabot (Optional)

Dependabot is already configured but you can:
- Adjust update frequency
- Change PR limits
- Add reviewers
- Modify ignore rules

## 🔍 Viewing Security Results

### GitHub Security Tab

1. Go to your repository
2. Click **Security** tab
3. View:
   - **Code scanning alerts** (Checkov results)
   - **Dependabot alerts** (dependency vulnerabilities)
   - **Secret scanning** (Gitleaks results)

### Workflow Runs

1. Go to **Actions** tab
2. Click on any workflow run
3. View detailed logs for each security check

### Artifacts

SBOM files are uploaded as artifacts:
1. Go to **Actions** tab
2. Click on a workflow run
3. Scroll to **Artifacts** section
4. Download `sbom-files`

## ⚙️ Customization

### Adjusting Severity Thresholds

Edit `.github/workflows/security-scan.yml`:

```yaml
# Checkov - fail on HIGH and CRITICAL
fail_on: HIGH,CRITICAL

# npm audit - fail on HIGH and CRITICAL
npm audit --audit-level=high
```

### Adding More Security Tools

Add new jobs to `security-scan.yml`:

```yaml
new-security-tool:
  name: New Security Tool
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Run tool
      run: tool-command
```

### Skipping Checks

Edit `terraform/.checkov.yml`:

```yaml
skip_check:
  - CKV_AZURE_1: "Reason for skipping"
```

## 🐛 Troubleshooting

### Security Scan Failing

1. **Check the workflow logs** in Actions tab
2. **Review the specific error** in the failed job
3. **Fix the issue** or adjust thresholds
4. **Re-run the workflow** if needed

### Dependabot Not Creating PRs

1. Check if Dependabot is enabled:
   - Settings → Security → Dependabot
2. Verify `.github/dependabot.yml` syntax
3. Check Dependabot logs in Insights → Dependency graph

### Terraform Validation Failing

1. Run locally: `terraform fmt -check`
2. Format code: `terraform fmt`
3. Validate: `terraform validate`

## 📊 Security Metrics

Track your security posture:

1. **GitHub Security Tab**:
   - Open security alerts
   - Resolved alerts
   - Dependabot updates

2. **Workflow Runs**:
   - Success rate
   - Average run time
   - Failed checks

3. **SBOM Files**:
   - Component inventory
   - License compliance
   - Vulnerability tracking

## ✅ Next Steps

1. ✅ **CI/CD Security Workflows** - DONE
2. ⏳ **Set up branch protection rules** - Manual step in GitHub
3. ⏳ **Update CODEOWNERS** - Replace placeholder teams
4. ⏳ **Review first security scan results**
5. ⏳ **Configure alerts** for security findings

## 📚 Resources

- [GitHub Actions Documentation](https://docs.github.com/actions)
- [Dependabot Documentation](https://docs.github.com/code-security/dependabot)
- [Checkov Documentation](https://www.checkov.io/)
- [Gitleaks Documentation](https://github.com/gitleaks/gitleaks)
- [CycloneDX Documentation](https://cyclonedx.org/)

