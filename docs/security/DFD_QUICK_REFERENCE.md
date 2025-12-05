# Data Flow Diagram (DFD) Quick Reference Guide

## Overview

This guide explains how to use and interpret the Data Flow Diagrams (DFDs) for the EchoSpace authentication service.

## Diagram Files

1. **AUTHENTICATION_DFD.mmd** - Level 1 DFD (Context Diagram)
   - Shows the authentication service as a single process
   - Displays external entities (User, Google OAuth, Email Service)
   - Shows data stores (SQL Server Database)
   - Use this for high-level overview

2. **AUTHENTICATION_DFD_LEVEL2.mmd** - Level 2 DFD (Physical DFD)
   - Shows detailed internal components
   - Displays trust boundaries clearly marked
   - Shows all middleware, services, and data stores
   - Use this for detailed security analysis

3. **AUTHENTICATION_LOGIN_FLOW.mmd** - Simplified Login Flow
   - Focuses on the login process specifically
   - Shows trust boundary transitions step-by-step
   - Use this for understanding the login security flow

## How to View the Diagrams

### Option 1: Using Mermaid Live Editor
1. Go to https://mermaid.live/
2. Copy the contents of any `.mmd` file
3. Paste into the editor
4. View the rendered diagram

### Option 2: Using VS Code
1. Install the "Markdown Preview Mermaid Support" extension
2. Open the `.mmd` file
3. Use the preview feature

### Option 3: Using GitHub
1. The diagrams in `AUTHENTICATION_DFD.md` will render automatically on GitHub
2. View the markdown file directly

## Understanding Trust Boundaries

### Trust Boundary Colors
- **Red (Untrusted)**: External entities and untrusted data
- **Yellow (Boundary)**: Security controls that validate/transform data
- **Green (Trusted)**: Validated, secure internal components

### Key Trust Boundary Transitions

1. **HTTPS Enforcement** → Rejects HTTP, only allows HTTPS
2. **Security Headers** → Adds CSP, X-Frame-Options, etc.
3. **Rate Limiter** → Prevents brute force attacks
4. **Input Validation** → Validates all input data
5. **JWT Bearer** → Validates token signature and expiration
6. **OAuth State Validation** → Prevents CSRF attacks

## Diagram Symbols

### External Entities
- **User**: Browser/client making requests
- **Google OAuth**: External OAuth provider
- **Email Service**: SMTP server for sending emails

### Processes
- **AuthController**: API endpoints
- **AuthService**: Business logic
- **TotpService**: 2FA logic
- **PasswordHasher**: Password hashing (PBKDF2)
- **JWTGenerator**: Token creation

### Data Stores
- **Users Table**: User accounts, passwords, TOTP secrets
- **UserSessions Table**: Refresh tokens and sessions
- **PasswordResetTokens**: Password reset tokens
- **AuditLogs Table**: Security audit logs

### Trust Boundaries
- **Entry Point**: HTTPS, Security Headers, Rate Limiter
- **Authentication Layer**: JWT Bearer, Session
- **Application Layer**: Controllers, Services (Trusted Zone)

## Data Flow Labels

Data flows are numbered and labeled to show:
- What data is being transmitted
- Whether data is trusted or untrusted
- The direction of data flow

Example: `"1. HTTP Request<br/>UNTRUSTED"` means:
- Flow #1
- Contains HTTP request data
- Data is untrusted at this point

## Security Analysis

### Using the DFDs for Security Review

1. **Identify Entry Points**: Look for all external entities
2. **Trace Trust Boundaries**: Follow data from untrusted to trusted
3. **Verify Validation**: Ensure all untrusted data passes through validators
4. **Check Data Stores**: Verify sensitive data is properly stored
5. **Review Audit Logging**: Ensure security events are logged

### Common Security Questions

**Q: Where does untrusted data enter the system?**
A: Through User, Google OAuth, and Email Service external entities

**Q: How is untrusted data validated?**
A: Through Input Validator (FluentValidation) after passing through middleware

**Q: Where are trust boundaries enforced?**
A: At HTTPS Enforcement, Security Headers, Rate Limiter, JWT Bearer, and Input Validator

**Q: How are passwords protected?**
A: Passwords are hashed using PBKDF2-HMAC-SHA256 before storage in Users Table

**Q: How are tokens secured?**
A: Tokens are cryptographically signed by JWT Generator and validated by JWT Bearer middleware

## Updating the Diagrams

When updating the authentication service:

1. Update the relevant `.mmd` file
2. Update the markdown documentation in `AUTHENTICATION_DFD.md`
3. Test the diagram in Mermaid Live Editor
4. Update this reference guide if symbols or flows change

## Best Practices

1. **Keep diagrams current**: Update when architecture changes
2. **Document trust boundaries**: Always mark where untrusted → trusted transitions occur
3. **Label data flows**: Include data types and trust status
4. **Use consistent colors**: Follow the color scheme for trust levels
5. **Simplify complex flows**: Break into multiple diagrams if needed

