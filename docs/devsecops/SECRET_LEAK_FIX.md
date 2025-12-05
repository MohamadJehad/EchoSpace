# 🔒 Fixing Secret Leak: SSL Private Key

## Problem

Gitleaks detected a private key file committed to the repository:
- **File**: `src/EchoSpace.Web.Client/ssl/localhost+2-key.pem`
- **Type**: SSL private key (local development certificate)
- **Risk**: Low (local dev cert, but should not be in repo)

## ✅ Solution Applied

### 1. Fixed Gitleaks Workflow

Removed invalid inputs (`no-git`, `verbose`) and configured properly:
- Added `.gitleaksignore` support
- Enabled redaction of secrets in output
- Set proper exit code

### 2. Updated .gitignore

Added comprehensive SSL certificate patterns:
- `*.pem`, `*.key`, `*.crt`, `*.cert` files
- `localhost*` files (local dev certificates)

### 3. Created .gitleaksignore

Created ignore file to exclude SSL certificates from secret scanning:
- SSL certificates and keys
- Test files
- Example/template files
- Documentation

## 📋 What You Need to Do

### Step 1: Remove the Secret from Git History

**Important**: The private key is already in git history. You need to remove it:

```powershell
# Option 1: Remove file and commit (if it still exists)
git rm src/EchoSpace.Web.Client/ssl/localhost+2-key.pem
git commit -m "security: remove SSL private key from repository"

# Option 2: Remove from history (if needed)
# WARNING: This rewrites history - coordinate with team
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch src/EchoSpace.Web.Client/ssl/localhost+2-key.pem" \
  --prune-empty --tag-name-filter cat -- --all
```

### Step 2: Verify .gitignore

Check that SSL files are ignored:

```powershell
git status
# Should not show any .pem, .key files
```

### Step 3: Regenerate SSL Certificates (If Needed)

If you need the SSL certificate for local development:

```powershell
cd src/EchoSpace.Web.Client/ssl
.\generate-cert-simple.ps1
```

**Important**: The generated files will be ignored by git (thanks to .gitignore).

### Step 4: Commit Changes

```powershell
git add .gitignore .gitleaksignore .github/workflows/security-scan.yml
git commit -m "security: fix Gitleaks workflow and ignore SSL certificates"
git push
```

## 🔍 Verification

After committing:

1. **Check Gitleaks workflow**: Should pass or only show warnings
2. **Verify files are ignored**: `git status` should not show SSL files
3. **Test locally**: Generate new SSL certs - they should be ignored

## 🛡️ Security Best Practices

### SSL Certificates

1. **Never commit private keys** to git
2. **Use .gitignore** for all certificate files
3. **Regenerate certificates** if accidentally committed
4. **Use different certificates** for dev/staging/prod

### For Local Development

1. Generate certificates locally
2. Add to `.gitignore` (already done)
3. Document generation process in README
4. Use environment variables for paths

### For Production

1. Use Azure Key Vault for certificates
2. Use Azure App Service SSL certificates
3. Never store production keys in code
4. Use managed identities

## 📝 Next Steps

1. ✅ Remove the committed private key (see Step 1 above)
2. ✅ Commit the fixes (`.gitignore`, `.gitleaksignore`, workflow)
3. ✅ Verify Gitleaks passes
4. ✅ Regenerate SSL certs locally if needed

## 🔗 References

- [Gitleaks Documentation](https://github.com/gitleaks/gitleaks)
- [GitHub Secret Scanning](https://docs.github.com/en/code-security/secret-scanning)
- [SSL Certificate Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/Transport_Layer_Protection_Cheat_Sheet.html)

---

**Important**: Even though this is a local development certificate, it's a security best practice to never commit private keys to version control. Always use `.gitignore` for sensitive files!

