# Branch Protection Setup Guide

This guide explains how to set up branch protection rules in GitHub to enforce DevSecOps practices.

## 🎯 Purpose

Branch protection rules ensure that:
- ✅ Code is reviewed before merging
- ✅ Security scans pass before merging
- ✅ Tests pass before merging
- ✅ No force pushes to protected branches
- ✅ No deletion of protected branches

## 📋 Step-by-Step Setup

### 1. Navigate to Branch Protection Settings

1. Go to your GitHub repository
2. Click **Settings**
3. Click **Branches** (in left sidebar)
4. Under **Branch protection rules**, click **Add rule**

### 2. Configure Protection for `main` Branch

#### Basic Settings

**Branch name pattern:**
```
main
```

#### Protect Matching Branches

✅ **Require a pull request before merging**
- ✅ Require approvals: **2**
- ✅ Dismiss stale pull request approvals when new commits are pushed
- ✅ Require review from Code Owners (if CODEOWNERS file exists)
- ✅ Restrict who can dismiss pull request reviews: **Repository admins**

✅ **Require status checks to pass before merging**
- ✅ Require branches to be up to date before merging
- ✅ **Required status checks** (add these):
  ```
  Build and Test
  Terraform Security Scan (Checkov)
  Secrets Scan (Gitleaks)
  Terraform Validate
  .NET Dependency Scan
  Node.js Dependency Scan
  ```
- ✅ Require conversation resolution before merging

✅ **Require linear history**
- Prevents merge commits, enforces rebase/squash

✅ **Do not allow bypassing the above settings**
- Even admins must follow rules

✅ **Restrict who can push to matching branches**
- Only allow specific people/teams (optional)

✅ **Allow force pushes**
- ❌ **Unchecked** (prevents force pushes)

✅ **Allow deletions**
- ❌ **Unchecked** (prevents branch deletion)

### 3. Configure Protection for `develop` Branch (Optional)

Repeat the same steps for `develop` branch with slightly relaxed rules:

**Branch name pattern:**
```
develop
```

**Settings:**
- Require approvals: **1** (instead of 2)
- Same status checks as main
- Same other protections

### 4. Configure Protection for Feature Branches (Optional)

**Branch name pattern:**
```
feature/*
```

**Settings:**
- Require pull request (optional)
- Require status checks (optional)
- Less strict than main/develop

## 🔍 Required Status Checks

Based on your workflows, these checks should be required:

### From `pr-build.yml`:
- ✅ `Build and Test`

### From `security-scan.yml`:
- ✅ `Terraform Security Scan (Checkov)`
- ✅ `Secrets Scan (Gitleaks)`
- ✅ `Terraform Validate`
- ✅ `.NET Dependency Scan`
- ✅ `Node.js Dependency Scan`
- ⚠️ `.NET SAST Scan` (optional - may have false positives)
- ⚠️ `TypeScript SAST Scan` (optional - may have false positives)
- ⚠️ `Generate SBOM` (optional - informational)

## 📝 Example Configuration

### For `main` Branch:

```yaml
Branch: main
Protection Rules:
  - Require PR: Yes (2 approvals)
  - Require status checks: Yes
    - Build and Test
    - Terraform Security Scan (Checkov)
    - Secrets Scan (Gitleaks)
    - Terraform Validate
    - .NET Dependency Scan
    - Node.js Dependency Scan
  - Require branches up to date: Yes
  - Require linear history: Yes
  - Require conversation resolution: Yes
  - Allow force pushes: No
  - Allow deletions: No
  - Do not allow bypassing: Yes
```

### For `develop` Branch:

```yaml
Branch: develop
Protection Rules:
  - Require PR: Yes (1 approval)
  - Require status checks: Yes (same as main)
  - Require branches up to date: Yes
  - Require linear history: Yes
  - Require conversation resolution: Yes
  - Allow force pushes: No
  - Allow deletions: No
```

## ✅ Verification

After setting up branch protection:

1. **Create a test PR**:
   ```bash
   git checkout -b test-branch-protection
   git commit --allow-empty -m "Test branch protection"
   git push origin test-branch-protection
   ```

2. **Create PR** from test branch to main

3. **Verify**:
   - ✅ PR shows "Required" status checks
   - ✅ Cannot merge without approvals
   - ✅ Cannot merge if checks fail
   - ✅ Cannot force push to main

## 🔧 Troubleshooting

### Status Checks Not Showing

1. **Check workflow file names**:
   - Must match exactly (case-sensitive)
   - Check job names in workflow files

2. **Run workflow once**:
   - Push a commit or create a PR
   - Let workflows run
   - Status checks will appear after first run

3. **Check workflow triggers**:
   - Ensure workflows run on `pull_request` event
   - Check branch filters

### Can't Merge Even After Approvals

1. **Check status checks**:
   - All required checks must pass
   - Check Actions tab for failed workflows

2. **Check branch is up to date**:
   - Update branch: `git pull origin main`
   - Or use "Update branch" button in PR

3. **Check conversation resolution**:
   - Resolve all review comments
   - Close all discussions

### Admins Can Bypass Rules

1. **Check "Do not allow bypassing"**:
   - Should be enabled for strict enforcement
   - Even admins must follow rules

2. **Review admin permissions**:
   - Settings → Manage access
   - Verify admin permissions

## 📊 Best Practices

### 1. Start Strict, Relax Later

- Begin with strict rules
- Relax if needed based on team feedback
- Better to be too strict than too lenient

### 2. Use CODEOWNERS

- Automatically request reviews from code owners
- Reduces manual review assignment
- Ensures experts review their code

### 3. Require Status Checks

- Don't allow merging with failing tests
- Don't allow merging with security issues
- Enforce quality gates

### 4. Require Linear History

- Easier to track changes
- Cleaner git history
- Better for debugging

### 5. Protect Multiple Branches

- `main` - Production-ready code
- `develop` - Integration branch
- `release/*` - Release branches

## 🎯 Integration with DevSecOps

Branch protection integrates with:

1. **Security Scanning**:
   - Prevents merging with security issues
   - Enforces security gates

2. **Code Review**:
   - Ensures code quality
   - Knowledge sharing
   - Catch bugs early

3. **Testing**:
   - Prevents broken code in main
   - Ensures tests pass

4. **Compliance**:
   - Audit trail
   - Approval records
   - Change tracking

## 📚 Resources

- [GitHub Branch Protection Documentation](https://docs.github.com/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [CODEOWNERS Documentation](https://docs.github.com/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)
- [Required Status Checks](https://docs.github.com/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging)

