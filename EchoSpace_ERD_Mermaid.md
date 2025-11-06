# EchoSpace Entity Relationship Diagram (ERD)

## Mermaid ERD Script

```mermaid
erDiagram
    USER {
        Guid Id PK
        string Name "Required, MaxLength(100)"
        string Email "Required, MaxLength(255), Unique"
        string UserName "Required, MaxLength(100)"
        string PasswordHash "MaxLength(500)"
        UserRole Role "Required"
        bool EmailConfirmed
        bool LockoutEnabled
        DateTimeOffset LockoutEnd
        int AccessFailedCount
        DateTime LastLoginAt
        DateTime CreatedAt
        DateTime UpdatedAt
        string TotpSecretKey
        bool EmailVerified
        string EmailVerificationCode
        DateTime EmailVerificationCodeExpiry
        int EmailVerificationAttempts
    }

    USER_SESSION {
        Guid SessionId PK
        Guid UserId FK
        string RefreshToken "MaxLength(500), Unique"
        string DeviceInfo "MaxLength(200)"
        string IpAddress "MaxLength(45)"
        DateTime ExpiresAt
        DateTime CreatedAt
    }

    AUTH_PROVIDER {
        Guid AuthId PK
        Guid UserId FK
        string Provider "Required, MaxLength(50)"
        string ProviderUid "Required, MaxLength(255)"
        string AccessToken "MaxLength(500)"
        DateTime CreatedAt
    }

    PASSWORD_RESET_TOKEN {
        Guid Id PK
        Guid UserId FK
        string Token "Required, MaxLength(500)"
        DateTime ExpiresAt "Required"
        DateTime CreatedAt
        bool IsUsed
        DateTime UsedAt
    }

    POST {
        Guid PostId PK
        Guid UserId FK
        string Content "Required, MaxLength(5000)"
        string ImageUrl "MaxLength(500)"
        DateTime CreatedAt
        DateTime UpdatedAt
    }

    COMMENT {
        Guid CommentId PK
        Guid PostId FK
        Guid UserId FK
        string Content "Required, MaxLength(2000)"
        DateTime CreatedAt
    }

    LIKE {
        Guid LikeId PK
        Guid PostId FK
        Guid UserId FK
        DateTime CreatedAt
    }

    FOLLOW {
        Guid FollowerId FK
        Guid FollowedId FK
        DateTime CreatedAt
    }

    %% Relationships
    USER ||--o{ USER_SESSION : "has"
    USER ||--o{ AUTH_PROVIDER : "has"
    USER ||--o{ PASSWORD_RESET_TOKEN : "has"
    USER ||--o{ POST : "creates"
    USER ||--o{ COMMENT : "writes"
    USER ||--o{ LIKE : "gives"
    USER ||--o{ FOLLOW : "followers"
    USER ||--o{ FOLLOW : "following"
    
    POST ||--o{ COMMENT : "has"
    POST ||--o{ LIKE : "receives"
```

## Relationship Details

### User Relationships
- **User → UserSession**: One-to-Many (User has many sessions)
- **User → AuthProvider**: One-to-Many (User can have multiple OAuth providers)
- **User → PasswordResetToken**: One-to-Many (User can have multiple reset tokens for security)
- **User → Post**: One-to-Many (User creates many posts)
- **User → Comment**: One-to-Many (User writes many comments)
- **User → Like**: One-to-Many (User gives many likes)
- **User → Follow (Followers)**: One-to-Many (User can have many followers)
- **User → Follow (Following)**: One-to-Many (User can follow many users)

### Post Relationships
- **Post → Comment**: One-to-Many (Post has many comments)
- **Post → Like**: One-to-Many (Post receives many likes)

### Follow Relationship
- **Follow**: Many-to-Many self-referential (Users following other users)
  - FollowerId → User (the one who follows)
  - FollowedId → User (the one being followed)

## Security Fields

### Authentication & Security
- `TotpSecretKey`: Encrypted TOTP secret for 2FA
- `EmailVerificationCode`: 6-digit email verification code
- `EmailVerificationCodeExpiry`: Expiry time for verification code (10 minutes)
- `EmailVerificationAttempts`: Track failed verification attempts (max 3)
- `LockoutEnd`: Account lockout until this time
- `AccessFailedCount`: Track failed login attempts

### Session Management
- `RefreshToken`: One-time use tokens for session management
- `DeviceInfo` & `IpAddress`: Track session metadata for security
- `ExpiresAt`: Automatic session expiration

### Password Reset Security
- `IsUsed`: Single-use token enforcement
- `UsedAt`: Track when token was consumed
- `ExpiresAt`: 1-hour expiry for reset tokens

## Indexes (Recommended)

```sql
-- Performance indexes
CREATE UNIQUE INDEX IX_Users_Email ON Users (Email);
CREATE INDEX IX_UserSessions_UserId ON UserSessions (UserId);
CREATE INDEX IX_UserSessions_RefreshToken ON UserSessions (RefreshToken);
CREATE INDEX IX_UserSessions_ExpiresAt ON UserSessions (ExpiresAt);
CREATE INDEX IX_Posts_UserId ON Posts (UserId);
CREATE INDEX IX_Posts_CreatedAt ON Posts (CreatedAt DESC);
CREATE INDEX IX_Comments_PostId ON Comments (PostId);
CREATE INDEX IX_Likes_PostId ON Likes (PostId);
CREATE INDEX IX_Follows_FollowerId ON Follows (FollowerId);
CREATE INDEX IX_Follows_FollowedId ON Follows (FollowedId);

-- Unique constraint for Follow relationship
CREATE UNIQUE INDEX IX_Follows_Unique ON Follows (FollowerId, FollowedId);
```

## Enum Types

### UserRole
```csharp
public enum UserRole
{
    User = 0,
    Moderator = 1,
    Admin = 2
}
```
