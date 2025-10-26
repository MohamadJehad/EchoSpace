# EchoSpace Security Technologies - Actual Implementation

This document lists the actual technologies and frameworks used in the EchoSpace backend implementation.

## Core Technologies

### Backend Framework
- **ASP.NET Core 8** - Modern web framework for building RESTful APIs

### Database & ORM
- **SQL Server** - Relational database
- **Entity Framework Core** - ORM for database access, automatically parameterizes queries to prevent SQL injection
- **Microsoft.EntityFrameworkCore.SqlServer** - SQL Server provider

### Authentication & Security

#### Password Security
- **PBKDF2 (Password-Based Key Derivation Function 2)**
- **HMAC-SHA256** - Hashing algorithm
- **100,000 iterations** - Computational cost to slow down brute-force attacks
- **128-bit salt** - Unique salt per password
- **256-bit hash output**
- **Library**: `Microsoft.AspNetCore.Cryptography.KeyDerivation`

#### JWT Authentication
- **JWT (JSON Web Tokens)** - Token-based authentication
- **HMAC-SHA256** - Symmetric key signing algorithm (NOT RS256)
- **Token Lifetime**: 15 minutes (access tokens), 7 days (refresh tokens)
- **Library**: `Microsoft.AspNetCore.Authentication.JwtBearer`
- **Libraries**: `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.Tokens`

#### Multi-Factor Authentication
- **TOTP (Time-based One-Time Password)** - RFC 6238 standard
- **HMAC-SHA1** - Algorithm for TOTP code generation
- **Secret Key**: 20 bytes (160 bits)
- **Time Step**: 30 seconds
- **Code Length**: 6 digits
- **Window**: ±1 time step (90 seconds validity)
- **Base32 Encoding** - For compatibility with authenticator apps
- **Library**: `QRCoder` - For QR code generation

#### OAuth Integration
- **OAuth 2.0 / OpenID Connect** - Google authentication
- **State Parameter** - CSRF protection
- **Session Storage** - For OAuth state validation

### Email Service
- **MailKit** - Email sending library
- **SMTP TLS** - STARTTLS encryption (port 587)
- **Library**: `MailKit.Net.Smtp`
- **MIME Processing**: `MimeKit`

### Cryptography
- **System.Security.Cryptography.RandomNumberGenerator** - Cryptographically secure random number generation
- **System.Security.Cryptography.HMACSHA1** - TOTP code generation
- **System.Security.Cryptography.HMACSHA256** - JWT signing and password hashing
- **System.Security.Cryptography.KeyDerivation** - PBKDF2 password hashing

### Frontend
- **Angular** - Web application framework
- **TypeScript** - Programming language
- **Reactive Forms** - Form validation

### Other Security Features

#### Input Validation
- **ASP.NET Core Data Annotations** - Model validation
- **Regular Expressions** - Password complexity validation
- **Model Binding** - Automatic request validation

#### Authorization
- **ASP.NET Core Policies** - Role-based access control (RBAC)
- **[Authorize] Attributes** - Controller-level authorization
- **Claims-Based Authorization** - Using JWT claims

#### Session Management
- **Distributed Memory Cache** - For session storage
- **HttpOnly Cookies** - For refresh tokens
- **Session Expiry**: 10 minutes idle timeout

#### CORS
- **ASP.NET Core CORS Middleware** - Cross-origin resource sharing
- **Policy**: Allow Angular frontend on port 4200

### Project Structure
```
EchoSpace.Core/           - Business logic, entities, DTOs, interfaces
EchoSpace.Infrastructure/ - Database, repositories, services
EchoSpace.UI/            - Controllers, API endpoints, configuration
EchoSpace.Tools/         - Email service, utilities
EchoSpace.Web.Client/    - Angular frontend
```

## Libraries & Packages

### Authentication & Security
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.AspNetCore.Cryptography.KeyDerivation
- System.IdentityModel.Tokens.Jwt
- Microsoft.IdentityModel.Tokens
- QRCoder

### Database
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools

### Email
- MailKit
- MimeKit

### Other
- Microsoft.AspNetCore.Session
- System.Text.Json

## Security Features Implemented

✅ **Password Security**
- PBKDF2 with HMAC-SHA256 (100,000 iterations)
- 128-bit salt per password
- Strong password requirements (10+ chars, uppercase, special character)

✅ **JWT Authentication**
- HMAC-SHA256 signing (symmetric key)
- 15-minute token lifetime
- Refresh token rotation
- Claims-based authorization

✅ **Multi-Factor Authentication**
- TOTP (RFC 6238) with HMAC-SHA1
- QR code generation for authenticator apps
- 90-second validity window

✅ **Email Verification**
- 6-digit codes
- 10-minute expiry
- 3-attempt limit
- Intelligent duplicate prevention

✅ **Account Security**
- Account lockout after 3 failed attempts
- 15-minute lockout duration
- Generic error messages (prevents user enumeration)

✅ **Session Management**
- Short-lived access tokens
- Refresh token rotation
- Session invalidation on password reset

✅ **Database Security**
- Entity Framework Core ORM (parameterized queries)
- SQL injection prevention
- Foreign key constraints
- Unique indexes on critical fields

✅ **Email Security**
- SMTP TLS (STARTTLS)
- Secure password reset tokens (32-byte)
- HTML email templates

✅ **Authorization**
- Role-Based Access Control (RBAC)
- Resource ownership validation
- Policy-based authorization

## Not Yet Implemented (Future Enhancements)

❌ **Rate Limiting** - ASP.NET Core Rate Limiting middleware
❌ **Security Headers** - HTTP Security Headers middleware (HSTS, X-Frame-Options, CSP)
❌ **WAF** - Web Application Firewall (Azure Front Door)
❌ **Application Insights** - Real-time monitoring and alerting
❌ **Azure Key Vault** - Centralized secrets management
❌ **Redis Cache** - Distributed caching
❌ **Argon2id** - More advanced password hashing (currently using PBKDF2)

## Configuration Files

- `appsettings.json` - Development configuration
- `appsettings.Production.json` - Production configuration
- `Program.cs` - Dependency injection and middleware configuration
- `EchoSpaceDbContext.cs` - Database context and entity configuration

## Key Security Principles

1. **Defense in Depth** - Multiple layers of security
2. **Fail Securely** - Generic error messages prevent information leakage
3. **Least Privilege** - Role-based access control
4. **Secure by Default** - HTTPS, session security, CORS restrictions
5. **Input Validation** - Frontend and backend validation
6. **Secure Storage** - Salted password hashing
7. **Secure Transmission** - TLS for email and API

