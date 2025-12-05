# Security Best Practices Guide

This guide outlines security best practices for EchoSpace development and operations.

## 🔒 Development Security

### Code Security

#### 1. Input Validation
- ✅ **Always validate input** at API boundaries
- ✅ **Use FluentValidation** for .NET models
- ✅ **Sanitize user input** before processing
- ✅ **Validate file uploads** (type, size, content)

```csharp
// Good: Using FluentValidation
public class CreatePostValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(5000)
            .Must(BeSafeContent);
    }
}
```

#### 2. Authentication & Authorization
- ✅ **Use JWT tokens** with short expiration (15 minutes)
- ✅ **Implement refresh token rotation**
- ✅ **Require MFA** for sensitive operations
- ✅ **Use role-based access control (RBAC)**
- ✅ **Validate permissions** on every request

```csharp
// Good: Authorization check
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeletePost(int id)
{
    // Additional ownership check
    if (!await _postService.UserOwnsPostAsync(id, User.GetUserId()))
    {
        return Forbid();
    }
    // ...
}
```

#### 3. Password Security
- ✅ **Use PBKDF2** with 100,000+ iterations
- ✅ **Require strong passwords** (10+ chars, complexity)
- ✅ **Never store plaintext passwords**
- ✅ **Implement account lockout** after failed attempts

#### 4. SQL Injection Prevention
- ✅ **Use Entity Framework Core** (parameterized queries)
- ✅ **Never concatenate SQL strings**
- ✅ **Use stored procedures** when needed
- ✅ **Validate database inputs**

```csharp
// Good: EF Core (automatically parameterized)
var users = await _context.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// Bad: String concatenation (DON'T DO THIS)
var query = $"SELECT * FROM Users WHERE Email = '{email}'";
```

#### 5. XSS Prevention
- ✅ **Encode output** in views
- ✅ **Use Content Security Policy (CSP)**
- ✅ **Sanitize HTML** if allowing rich content
- ✅ **Use Angular's built-in sanitization**

```typescript
// Good: Angular sanitization
import { DomSanitizer } from '@angular/platform-browser';

constructor(private sanitizer: DomSanitizer) {}

getSafeHtml(content: string) {
    return this.sanitizer.sanitize(SecurityContext.HTML, content);
}
```

### Dependency Security

#### 1. Keep Dependencies Updated
- ✅ **Review Dependabot PRs** promptly
- ✅ **Update critical vulnerabilities** immediately
- ✅ **Test updates** before merging
- ✅ **Use dependency scanning** in CI/CD

#### 2. Audit Dependencies
```bash
# .NET
dotnet list package --vulnerable --include-transitive

# Node.js
npm audit
npm audit fix
```

#### 3. Use Trusted Sources
- ✅ **NuGet.org** for .NET packages
- ✅ **npm registry** for Node.js packages
- ✅ **Verify package integrity**
- ✅ **Check package maintainers**

### Secrets Management

#### 1. Never Commit Secrets
- ❌ **Don't commit** API keys, passwords, tokens
- ❌ **Don't commit** connection strings
- ❌ **Don't commit** private keys
- ✅ **Use Azure Key Vault** for production
- ✅ **Use environment variables** for development
- ✅ **Use GitHub Secrets** for CI/CD

#### 2. Secret Scanning
```bash
# Before committing
gitleaks detect --source . --verbose

# Or use pre-commit hooks
pre-commit run --all-files
```

#### 3. Rotate Secrets Regularly
- ✅ **Rotate API keys** quarterly
- ✅ **Rotate database passwords** monthly
- ✅ **Rotate JWT signing keys** annually
- ✅ **Revoke compromised secrets** immediately

## 🏗️ Infrastructure Security

### Terraform Security

#### 1. Use Variables
- ✅ **Never hardcode** subscription IDs
- ✅ **Use variables** for all sensitive values
- ✅ **Use terraform.tfvars** (not committed)
- ✅ **Use Azure Key Vault** for secrets

#### 2. Security Scanning
```bash
# Checkov scan
checkov -d terraform/ --framework terraform

# Terraform validation
terraform validate
terraform fmt -check
```

#### 3. State File Security
- ✅ **Use remote state** (Azure Storage)
- ✅ **Enable state encryption**
- ✅ **Restrict access** to state files
- ✅ **Never commit** state files

### Azure Security

#### 1. App Service Security
- ✅ **Enable HTTPS only**
- ✅ **Use TLS 1.2 minimum**
- ✅ **Enable Managed Identity**
- ✅ **Use Key Vault references**
- ✅ **Configure IP restrictions** if needed

#### 2. Network Security
- ✅ **Use VNet integration** for private access
- ✅ **Configure NSGs** properly
- ✅ **Use private endpoints** for databases
- ✅ **Restrict public access** where possible

#### 3. Monitoring
- ✅ **Enable Application Insights**
- ✅ **Set up security alerts**
- ✅ **Monitor failed authentications**
- ✅ **Track unusual access patterns**

## 🔍 Security Scanning

### Pre-Commit Checks
```bash
# Install pre-commit hooks
pre-commit install

# Run all hooks
pre-commit run --all-files
```

### Local Security Scan
```bash
# Run comprehensive security scan
./scripts/security-scan-local.sh
```

### CI/CD Security
- ✅ **All PRs** run security scans
- ✅ **Fail builds** on critical issues
- ✅ **Review scan results** before merging
- ✅ **Fix security issues** promptly

## 📋 Security Checklist

### Before Committing
- [ ] Run `gitleaks` to check for secrets
- [ ] Run `terraform fmt` and `terraform validate`
- [ ] Check for vulnerable dependencies
- [ ] Review code for security issues
- [ ] Test authentication/authorization

### Before Deploying
- [ ] All security scans pass
- [ ] Dependencies updated
- [ ] Secrets stored in Key Vault
- [ ] HTTPS enabled
- [ ] Monitoring configured
- [ ] Backup strategy in place

### Regular Maintenance
- [ ] Review security scan results weekly
- [ ] Update dependencies monthly
- [ ] Rotate secrets quarterly
- [ ] Review access permissions quarterly
- [ ] Conduct security audits annually

## 🚨 Incident Response

### If Security Issue Found

1. **Assess Severity**
   - Critical: Immediate action required
   - High: Fix within 7 days
   - Medium: Fix within 30 days
   - Low: Next release cycle

2. **Containment**
   - Revoke compromised credentials
   - Disable affected features if needed
   - Isolate affected systems

3. **Remediation**
   - Fix the vulnerability
   - Test the fix
   - Deploy patch

4. **Communication**
   - Notify affected users (if applicable)
   - Document the incident
   - Update security documentation

## 📚 Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP .NET Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/DotNet_Security_Cheat_Sheet.html)
- [Angular Security Guide](https://angular.io/guide/security)
- [Azure Security Best Practices](https://docs.microsoft.com/azure/security/fundamentals/best-practices-and-patterns)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)

## 🔄 Continuous Improvement

- Review security practices quarterly
- Update this guide as needed
- Share security knowledge with team
- Learn from security incidents
- Stay updated with security trends

---

**Remember**: Security is everyone's responsibility. When in doubt, ask the security team.

