# EchoSpace Authentication System - Complete Guide

## Table of Contents
1. [Overview](#overview)
2. [Authentication Architecture](#authentication-architecture)
3. [Local Authentication Flow](#local-authentication-flow)
4. [Google OAuth Flow](#google-oauth-flow)
5. [Token Management](#token-management)
6. [Security Features](#security-features)
7. [Component Responsibilities](#component-responsibilities)
8. [Database Schema](#database-schema)

---

## Overview

EchoSpace implements a modern, secure authentication system using JWT (JSON Web Tokens) with support for both local authentication (email/password) and Google OAuth. The system follows OAuth 2.0 and OpenID Connect principles for external authentication.

### Key Components

- **Backend (ASP.NET Core)**: Handles authentication logic, token generation, and user management
- **Frontend (Angular)**: Manages user sessions, handles token storage, and intercepts API requests
- **Database**: Stores user credentials, sessions, and OAuth provider links

---

## Authentication Architecture

### High-Level Flow

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐
│   Angular   │◄────────┤   ASP.NET    │◄────────┤  Database   │
│  Frontend   │────────►│    Core      │────────►│    (SQL)    │
└─────────────┘         └──────────────┘         └─────────────┘
     │                         │                         │
     │                         │                         │
     └─────────────────────────┴─────────────────────────┘
                        │
                        ▼
                  ┌─────────────┐
                  │   Google    │
                  │    OAuth    │
                  └─────────────┘
```

### Authentication Methods

1. **Local Authentication**: Email + Password
2. **Google OAuth**: Social login via Google's OAuth 2.0

---

## Local Authentication Flow

### Step-by-Step Process

#### 1. User Registration (`/api/auth/register`)

**Backend (.NET):**
```csharp
// 1. Validate input
// 2. Check if user exists
if (await _context.Users.AnyAsync(u => u.Email == request.Email))
    throw new InvalidOperationException("User already exists");

// 3. Hash password using PBKDF2
var passwordHash = HashPassword(request.Password);
// Algorithm: PBKDF2-HMAC-SHA256, 100,000 iterations, 32-byte salt

// 4. Create user entity
var user = new User
{
    Id = Guid.NewGuid(),
    Name = request.Name,
    Email = request.Email,
    UserName = request.Email,
    PasswordHash = passwordHash,
    EmailConfirmed = false,
    LockoutEnabled = true,
    AccessFailedCount = 0,
    CreatedAt = DateTime.UtcNow
};

// 5. Save to database
_context.Users.Add(user);
await _context.SaveChangesAsync();

// 6. Generate tokens
return await GenerateTokensAsync(user);
```

**Frontend (Angular):**
```typescript
// RegisterComponent calls AuthService
this.authService.register({ name, email, password }).subscribe({
  next: (response) => {
    // Tokens automatically stored by AuthService
    this.router.navigate(['/']);
  }
});
```

#### 2. Token Generation

**Purpose of Tokens:**
- **Access Token**: Short-lived (15 minutes) for API authorization
- **Refresh Token**: Long-lived (7 days) for obtaining new access tokens

**Backend Token Generation:**
```csharp
private async Task<AuthResponse> GenerateTokensAsync(User user)
{
    // 1. Generate JWT Access Token
    var accessToken = GenerateAccessToken(user);
    // Contains: User ID, Email, Name, Expiration
    // Signed with: HMAC-SHA256
    
    // 2. Generate cryptographically secure Refresh Token
    var refreshToken = GenerateRefreshToken();
    // Random 32-byte array, Base64 encoded
    
    // 3. Save refresh token to database (UserSession table)
    var session = new UserSession
    {
        SessionId = Guid.NewGuid(),
        UserId = user.Id,
        RefreshToken = refreshToken,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow
    };
    
    _context.UserSessions.Add(session);
    await _context.SaveChangesAsync();
    
    // 4. Return both tokens
    return new AuthResponse
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresIn = 900, // 15 minutes in seconds
        User = new UserDto { Id = user.Id, Name = user.Name, Email = user.Email }
    };
}
```

**Frontend Token Storage:**
```typescript
private setSession(authResult: AuthResponse): void {
    // Store in localStorage (persists across browser sessions)
    localStorage.setItem('accessToken', authResult.accessToken);
    localStorage.setItem('refreshToken', authResult.refreshToken);
    localStorage.setItem('user', JSON.stringify(authResult.user));
    
    // Update BehaviorSubject (triggers UI updates)
    this.currentUserSubject.next(authResult.user);
}
```

#### 3. User Login (`/api/auth/login`)

**Backend Process:**
```csharp
public async Task<AuthResponse> LoginAsync(LoginRequest request)
{
    // 1. Find user by email
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    
    // 2. Check account lockout
    if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        throw new UnauthorizedAccessException("Account is locked");
    
    // 3. Verify password using PBKDF2
    if (!VerifyPassword(request.Password, user.PasswordHash))
    {
        // Increment failed attempts
        user.AccessFailedCount++;
        if (user.AccessFailedCount >= 3)
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        await _context.SaveChangesAsync();
        throw new UnauthorizedAccessException("Invalid credentials");
    }
    
    // 4. Reset failed attempts on success
    user.AccessFailedCount = 0;
    user.LockoutEnd = null;
    user.LastLoginAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    
    // 5. Generate tokens
    return await GenerateTokensAsync(user);
}
```

**Security Features:**
- **Account Lockout**: After 3 failed attempts, account locked for 15 minutes
- **Generic Error Messages**: Prevents user enumeration attacks
- **Password Hashing**: PBKDF2 with 100,000 iterations
- **Salt Per Password**: Unique salt for each password

#### 4. Accessing Protected Routes

**Frontend Guard (`auth.guard.ts`):**
```typescript
export const authGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    
    if (authService.isAuthenticated()) {
        return true;
    }
    
    router.navigate(['/login']);
    return false;
};
```

**HTTP Interceptor (`auth.interceptor.ts`):**
```typescript
intercept(req: HttpRequest<any>, next: HttpHandler) {
    // 1. Get token from storage
    const token = this.authService.getToken();
    
    // 2. Add Bearer token to header
    if (token) {
        req = req.clone({
            setHeaders: { Authorization: `Bearer ${token}` }
        });
    }
    
    // 3. Handle 401 errors (token expired)
    return next.handle(req).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status === 401) {
                // Refresh token and retry
                return this.authService.refreshToken().pipe(
                    switchMap(() => {
                        const newToken = this.authService.getToken();
                        req = req.clone({
                            setHeaders: { Authorization: `Bearer ${newToken}` }
                        });
                        return next.handle(req);
                    })
                );
            }
            return throwError(() => error);
        })
    );
}
```

**Backend JWT Validation:**
```csharp
// In Program.cs
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,           // Check token issuer
        ValidateAudience = true,         // Check token audience
        ValidateLifetime = true,         // Check expiration
        ValidateIssuerSigningKey = true, // Verify signature
        ValidIssuer = "https://localhost:7131",
        ValidAudience = "https://localhost:4200",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
```

#### 5. Token Refresh (`/api/auth/refresh`)

**Why Token Refresh?**
- Access tokens are short-lived (15 minutes) for security
- Refresh tokens allow obtaining new access tokens without re-authentication
- Refresh tokens can be revoked (deleted from database)

**Backend Process:**
```csharp
public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
{
    // 1. Find session by refresh token
    var session = await _context.UserSessions
        .Include(s => s.User)
        .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
    
    // 2. Check if token exists and not expired
    if (session == null || session.ExpiresAt < DateTime.UtcNow)
        throw new UnauthorizedAccessException("Invalid refresh token");
    
    // 3. Extend session expiration
    session.ExpiresAt = DateTime.UtcNow.AddDays(7);
    await _context.SaveChangesAsync();
    
    // 4. Generate new access token
    return await GenerateTokensAsync(session.User);
}
```

**Frontend Process:**
```typescript
refreshToken(): Observable<AuthResponse> {
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken })
        .pipe(
            tap(response => this.setSession(response))
        );
}
```

#### 6. Logout (`/api/auth/logout`)

**Backend Process:**
```csharp
public async Task LogoutAsync(string refreshToken)
{
    // Find and delete the session
    var session = await _context.UserSessions
        .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
    
    if (session != null)
    {
        _context.UserSessions.Remove(session);
        await _context.SaveChangesAsync();
    }
}
```

**Frontend Process:**
```typescript
logout(): void {
    const refreshToken = localStorage.getItem('refreshToken');
    if (refreshToken) {
        this.http.post(`${this.apiUrl}/logout`, { refreshToken }).subscribe();
    }
    this.clearSession();
}

private clearSession(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
}
```

---

## Google OAuth Flow

### Step-by-Step Process

#### 1. Initiate Google Login

**Frontend (`login.component.ts`):**
```typescript
loginWithGoogle() {
    // Redirect to backend OAuth endpoint
    window.location.href = `${this.apiUrl}/google`;
}
```

#### 2. Backend Redirects to Google (`/api/auth/google`)

**Purpose:** Redirect user to Google's authorization server

**Backend Process:**
```csharp
[HttpGet("google")]
public IActionResult GoogleLogin()
{
    var clientId = configuration["Google:ClientId"];
    var redirectUri = configuration["OAuth:CallbackUrl"];
    
    // Generate CSRF protection state
    var state = Guid.NewGuid().ToString();
    HttpContext.Session.SetString("oauth_state", state);
    
    // Build Google OAuth URL
    var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
        $"client_id={Uri.EscapeDataString(clientId!)}&" +
        $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
        $"response_type=code&" +
        $"scope=openid email profile&" +
        $"state={state}";
    
    return Redirect(googleAuthUrl);
}
```

**Security Features:**
- **State Parameter**: CSRF protection (prevents unauthorized redirects)
- **Session Storage**: State stored in server session
- **Scope Request**: Requests openid, email, and profile

#### 3. User Authorizes on Google

**What Happens:**
1. User logs into Google (if not already)
2. Google shows consent screen (name, email, profile picture)
3. User clicks "Allow"
4. Google redirects back to our callback URL with authorization code

**Google Redirect:**
```
https://localhost:7131/api/auth/google-callback?code=AUTHORIZATION_CODE&state=CSRF_STATE
```

#### 4. Backend Exchanges Code for Tokens (`/api/auth/google-callback`)

**Backend Process:**
```csharp
[HttpGet("google-callback")]
public async Task<IActionResult> GoogleCallback(string code, string state)
{
    // 1. Verify CSRF state
    var storedState = HttpContext.Session.GetString("oauth_state");
    if (storedState != state)
        return BadRequest("Invalid state parameter");
    
    // 2. Exchange authorization code for access token
    var tokenRequest = new Dictionary<string, string>
    {
        { "code", code },
        { "client_id", clientId },
        { "client_secret", clientSecret },
        { "redirect_uri", redirectUri },
        { "grant_type", "authorization_code" }
    };
    
    var tokenResponse = await httpClient.PostAsync(
        "https://oauth2.googleapis.com/token",
        new FormUrlEncodedContent(tokenRequest)
    );
    
    var tokenData = JsonSerializer.Deserialize<JsonElement>(
        await tokenResponse.Content.ReadAsStringAsync()
    );
    var accessToken = tokenData.GetProperty("access_token").GetString();
    
    // 3. Get user info from Google
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", accessToken);
    
    var userInfoResponse = await httpClient.GetAsync(
        "https://www.googleapis.com/oauth2/v2/userinfo"
    );
    
    var userInfo = JsonSerializer.Deserialize<JsonElement>(
        await userInfoResponse.Content.ReadAsStringAsync()
    );
    
    var email = userInfo.GetProperty("email").GetString();
    var name = userInfo.GetProperty("name").GetString();
    var googleId = userInfo.GetProperty("id").GetString();
    
    // 4. Create or link user account
    var authResponse = await _authService.GoogleLoginAsync(email, name, googleId);
    
    // 5. Redirect to Angular with tokens in URL
    var encodedUser = Uri.EscapeDataString(
        JsonSerializer.Serialize(authResponse.User, jsonOptions)
    );
    
    var redirectUrl = $"{frontendCallbackUrl}?" +
        $"accessToken={Uri.EscapeDataString(authResponse.AccessToken)}&" +
        $"refreshToken={Uri.EscapeDataString(authResponse.RefreshToken)}&" +
        $"user={encodedUser}";
    
    return Redirect(redirectUrl);
}
```

#### 5. User Creation/Linking (`GoogleLoginAsync`)

**Backend Process:**
```csharp
public async Task<AuthResponse> GoogleLoginAsync(string email, string name, string googleId)
{
    // 1. Check if user exists
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    
    if (user == null)
    {
        // 2. Create new user
        user = new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Name = name,
            EmailConfirmed = true,  // Google verifies emails
            LockoutEnabled = false,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        // 3. Create AuthProvider entry
        var authProvider = new AuthProvider
        {
            AuthId = Guid.NewGuid(),
            UserId = user.Id,
            Provider = "Google",
            ProviderUid = googleId,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.AuthProviders.Add(authProvider);
        await _context.SaveChangesAsync();
    }
    else
    {
        // 4. User exists - link Google account if not already linked
        var existingProvider = await _context.AuthProviders
            .FirstOrDefaultAsync(ap => ap.UserId == user.Id && ap.Provider == "Google");
        
        if (existingProvider == null)
        {
            var authProvider = new AuthProvider
            {
                AuthId = Guid.NewGuid(),
                UserId = user.Id,
                Provider = "Google",
                ProviderUid = googleId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.AuthProviders.Add(authProvider);
            await _context.SaveChangesAsync();
        }
    }
    
    // 5. Update last login
    user.LastLoginAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    
    // 6. Generate tokens
    return await GenerateTokensAsync(user);
}
```

#### 6. Frontend Processes Callback (`auth-callback.component.ts`)

**Angular Component:**
```typescript
ngOnInit() {
    this.route.queryParams.subscribe(params => {
        const accessToken = params['accessToken'];
        const refreshToken = params['refreshToken'];
        const userStr = params['user'];
        
        if (accessToken && refreshToken && userStr) {
            try {
                // 1. Decode and parse user data
                const decodedUserStr = decodeURIComponent(userStr);
                const user = JSON.parse(decodedUserStr);
                
                // 2. Create auth response object
                const authResponse = {
                    accessToken: accessToken,
                    refreshToken: refreshToken,
                    expiresIn: 3600,
                    user: user
                };
                
                // 3. Set session (stores in localStorage and updates observable)
                this.authService.setSessionFromCallback(authResponse);
                
                // 4. Redirect to home
                this.router.navigate(['/']);
            } catch (error) {
                console.error('Error processing callback:', error);
                this.router.navigate(['/login']);
            }
        }
    });
}
```

---

## Token Management

### Access Token Structure

**What is a JWT?**
A JSON Web Token is a compact, URL-safe token consisting of three parts separated by dots (`.`):

```
header.payload.signature
```

**Example:**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

**1. Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**2. Payload (Claims):**
```json
{
  "sub": "user-guid",
  "jti": "unique-token-id",
  "email": "user@example.com",
  "name": "John Doe",
  "exp": 1234567890,
  "iat": 1234567890
}
```

**3. Signature:**
```
HMACSHA256(
  base64UrlEncode(header) + "." + base64UrlEncode(payload),
  secret
)
```

### Token Expiration Strategy

**Why Short-Lived Access Tokens?**
- **Security**: If compromised, damage is limited to 15 minutes
- **Revocation**: Can't revoke individual tokens, but can revoke refresh tokens
- **Stateless**: No need to check database for each request

**Why Long-Lived Refresh Tokens?**
- **User Experience**: Users don't need to log in every 15 minutes
- **Stored in Database**: Can be revoked (deleted)
- **Rotation**: New refresh token issued on each refresh

### Token Storage Security

**Why localStorage?**
- Persists across browser sessions
- Not sent in requests automatically (prevents CSRF)
- Accessible to JavaScript

**Why NOT Cookies for Tokens?**
- Cookies sent with every request (CSRF risk)
- Can be accessed by JavaScript (XSS risk if HttpOnly not set)
- We use cookies for OAuth state (session-based, server-side)

---

## Security Features

### Password Security

**Hashing Algorithm: PBKDF2**
- **Why PBKDF2?**: Proven, secure, designed for password hashing
- **Iterations**: 100,000 iterations (slow enough to prevent brute force)
- **Salt**: Unique 16-byte salt per password
- **Algorithm**: HMAC-SHA256
- **Output**: 32-byte hash

**Password Storage Format:**
```
salt.base64Hash
```
Example: `YWJjZGVmZ2hpams=` (salt) + `.` + `MTIzNDU2Nzg5MGFiY2RlZmdoaWpr` (hash)

**Verification Process:**
```csharp
private bool VerifyPassword(string password, string storedHash)
{
    // 1. Split salt and hash
    var parts = storedHash.Split('.', 2);
    var salt = Convert.FromBase64String(parts[0]);
    var hash = parts[1];
    
    // 2. Hash input password with same salt
    string hashedInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
        password: password,
        salt: salt,
        prf: KeyDerivationPrf.HMACSHA256,
        iterationCount: 100000,
        numBytesRequested: 32
    ));
    
    // 3. Compare hashes
    return hashedInput == hash;
}
```

### Account Lockout

**Implementation:**
- After 3 failed login attempts: Account locked for 15 minutes
- `AccessFailedCount` incremented on each failure
- `LockoutEnd` set when count reaches 3
- Reset on successful login

### CSRF Protection

**OAuth State Parameter:**
- Random GUID generated on server
- Stored in session (server-side)
- Sent to Google and returned in callback
- Verified on callback to prevent CSRF attacks

**Session Configuration:**
```csharp
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;      // Not accessible to JavaScript
    options.Cookie.IsEssential = true;   // Always sent
});
```

### CORS Configuration

**Backend:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Important for cookies
    });
});
```

**Why `AllowCredentials`?**
Necessary for session cookies used in OAuth flow

---

## Component Responsibilities

### Backend Components

#### `AuthService.cs`
- **Responsibility**: Core authentication logic
- **Methods**:
  - `RegisterAsync`: User registration
  - `LoginAsync`: Email/password login
  - `GoogleLoginAsync`: Google OAuth login
  - `RefreshTokenAsync`: Token refresh
  - `LogoutAsync`: User logout
  - `GenerateTokensAsync`: Token generation
  - `HashPassword`: Password hashing
  - `VerifyPassword`: Password verification

#### `AuthController.cs`
- **Responsibility**: HTTP endpoints for authentication
- **Endpoints**:
  - `POST /api/auth/register`: User registration
  - `POST /api/auth/login`: User login
  - `POST /api/auth/refresh`: Token refresh
  - `POST /api/auth/logout`: User logout
  - `GET /api/auth/google`: Initiate Google OAuth
  - `GET /api/auth/google-callback`: Google OAuth callback

#### `EchoSpaceDbContext.cs`
- **Responsibility**: Database context and entity configuration
- **Entities**:
  - `User`: User accounts
  - `UserSession`: Active sessions (refresh tokens)
  - `AuthProvider`: OAuth provider links

### Frontend Components

#### `AuthService.ts`
- **Responsibility**: Authentication state management
- **Key Features**:
  - `BehaviorSubject` for current user
  - Token storage in localStorage
  - Token expiration checking
  - Session management

#### `AuthInterceptor.ts`
- **Responsibility**: HTTP request/response interception
- **Functions**:
  - Add Bearer token to requests
  - Handle 401 errors (token refresh)
  - Retry failed requests after refresh

#### `AuthGuard.ts`
- **Responsibility**: Route protection
- **Function**: Prevent access to protected routes without authentication

#### `AuthCallbackComponent.ts`
- **Responsibility**: Handle OAuth redirect
- **Function**: Parse tokens from URL and store in session

---

## Database Schema

### User Table
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    UserName NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(500) NULL,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    LockoutEnabled BIT NOT NULL DEFAULT 1,
    LockoutEnd DATETIMEOFFSET NULL,
    AccessFailedCount INT NOT NULL DEFAULT 0,
    LastLoginAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL
);
```

**Purpose:**
- Stores user account information
- Password hash nullable (Google users don't have passwords)
- Email unique constraint prevents duplicates
- Lockout fields for account security

### UserSession Table
```sql
CREATE TABLE UserSessions (
    SessionId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    RefreshToken NVARCHAR(500) NOT NULL UNIQUE,
    DeviceInfo NVARCHAR(200) NULL,
    IpAddress NVARCHAR(45) NULL,
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

**Purpose:**
- Stores active sessions
- Refresh token unique constraint
- Cascade delete when user is deleted
- ExpiresAt for automatic cleanup

### AuthProvider Table
```sql
CREATE TABLE AuthProviders (
    AuthId UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Provider NVARCHAR(50) NOT NULL,
    ProviderUid NVARCHAR(255) NOT NULL,
    AccessToken NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

**Purpose:**
- Links user accounts to OAuth providers
- Stores provider-specific user IDs
- Optional access token storage (for future use)
- Supports multiple providers per user

---

## JSON Serialization

### Backend Configuration

**Why camelCase?**
JavaScript/TypeScript convention uses camelCase, while C# uses PascalCase.

**Configuration:**
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = 
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
```

**Before:**
```json
{
  "Id": "123",
  "Name": "John",
  "Email": "john@example.com"
}
```

**After:**
```json
{
  "id": "123",
  "name": "John",
  "email": "john@example.com"
}
```

---

## Best Practices Implemented

### Security
✅ Password hashing with PBKDF2 and unique salts  
✅ Account lockout after failed attempts  
✅ CSRF protection for OAuth  
✅ Short-lived access tokens  
✅ Refresh token revocation  
✅ Generic error messages (prevents user enumeration)  
✅ HTTPS only in production  
✅ HttpOnly cookies for sessions  

### User Experience
✅ Seamless token refresh  
✅ Remember user session  
✅ Clear error messages  
✅ Loading states  
✅ Responsive design  

### Code Quality
✅ Separation of concerns  
✅ SOLID principles  
✅ Clean architecture  
✅ Dependency injection  
✅ Error handling  
✅ Comprehensive logging  

---

## Troubleshooting

### Common Issues

**Issue**: "User" displayed instead of actual name  
**Cause**: JSON property name mismatch  
**Solution**: Backend configured for camelCase serialization

**Issue**: Token refresh fails  
**Cause**: Expired refresh token or deleted session  
**Solution**: User must log in again

**Issue**: Google OAuth redirect fails  
**Cause**: Invalid callback URL in Google Console  
**Solution**: Verify callback URL matches exactly

**Issue**: CSRF error on Google callback  
**Cause**: Session expired or state mismatch  
**Solution**: Session timeout is 10 minutes

---

## Summary

The EchoSpace authentication system provides:
- **Secure** password hashing and token management
- **Flexible** support for local and OAuth authentication
- **User-friendly** seamless session management
- **Scalable** database-driven session storage
- **Modern** JWT-based stateless API authentication

The system follows industry best practices and provides a solid foundation for secure user authentication in the EchoSpace social media platform.

