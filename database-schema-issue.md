# Database Schema Implementation: Entity Framework Models and Relations

## 🎯 **Issue Summary**
Implement comprehensive Entity Framework models with all required entities and relationships for EchoSpace social media application based on the system requirements and threat modeling analysis.

## 📋 **Description**

Based on the EchoSpace system requirements and functional specifications, we need to implement a complete Entity Framework schema that supports:

- **User Management**: Authentication, profiles, and session management
- **Content Management**: Posts, images, and AI-generated metadata
- **Social Features**: Reactions, comments, and notifications
- **AI Integration**: Classification, tagging, translation, and summarization
- **Security**: Audit trails and access control

## 🏗️ **Entity Framework Models Implementation**

### **Core User Entities**

#### 1. **User Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Bio { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }

        public bool IsEmailVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation Properties
        public virtual UserProfile? UserProfile { get; set; }
        public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<SecurityEvent> SecurityEvents { get; set; } = new List<SecurityEvent>();
    }
}
```

#### 2. **UserProfile Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class UserProfile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(255)]
        public string? Website { get; set; }

        [MaxLength(50)]
        public string? Pronouns { get; set; }

        [Column(TypeName = "json")]
        public string? Interests { get; set; }

        [Column(TypeName = "json")]
        public string? PrivacySettings { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

#### 3. **UserSession Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class UserSession
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string SessionToken { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string RefreshToken { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DeviceInfo { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

### **Authentication & Security Entities**

#### 4. **ThirdPartyAuthProvider Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class ThirdPartyAuthProvider
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ProviderName { get; set; } = string.Empty; // Google, Facebook, Apple

        [Required]
        [MaxLength(255)]
        public string ProviderUserId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? ProviderEmail { get; set; }

        [MaxLength(1000)]
        public string? AccessToken { get; set; } // Encrypted

        [MaxLength(1000)]
        public string? RefreshToken { get; set; } // Encrypted

        public DateTime? TokenExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

#### 5. **PasswordResetToken Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class PasswordResetToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

#### 6. **EmailVerificationToken Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class EmailVerificationToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

### **Content Management Entities**

#### 7. **Post Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public enum PostType
    {
        Text,
        Image,
        Video,
        Link
    }

    public enum Visibility
    {
        Public,
        Private,
        Friends,
        Followers
    }

    public class Post
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [MaxLength(200)]
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public PostType PostType { get; set; }

        [Required]
        public Visibility Visibility { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        public virtual ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();
        public virtual ICollection<PostCategoryAssignment> PostCategoryAssignments { get; set; } = new List<PostCategoryAssignment>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public virtual ICollection<AIClassification> AIClassifications { get; set; } = new List<AIClassification>();
        public virtual ICollection<AITag> AITags { get; set; } = new List<AITag>();
        public virtual ICollection<AITranslation> AITranslations { get; set; } = new List<AITranslation>();
        public virtual ICollection<AISummary> AISummaries { get; set; } = new List<AISummary>();
    }
}
```

#### 8. **PostImage Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class PostImage
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string ImageName { get; set; } = string.Empty;

        public long ImageSize { get; set; }

        [Required]
        [MaxLength(50)]
        public string ImageType { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AltText { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;
    }
}
```

#### 9. **PostCategory Entity**
```csharp
using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.Entities
{
    public class PostCategory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        [MaxLength(100)]
        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<PostCategoryAssignment> PostCategoryAssignments { get; set; } = new List<PostCategoryAssignment>();
        public virtual ICollection<AIClassification> AIClassifications { get; set; } = new List<AIClassification>();
    }
}
```

#### 10. **PostCategoryAssignment Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class PostCategoryAssignment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [Column(TypeName = "decimal(5,4)")]
        public decimal? Confidence { get; set; } // AI confidence score

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual PostCategory Category { get; set; } = null!;
    }
}
```

### **AI Integration Entities**

#### 11. **AIClassification Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class AIClassification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid CategoryId { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,4)")]
        public decimal Confidence { get; set; }

        [Required]
        [MaxLength(50)]
        public string ModelVersion { get; set; } = string.Empty;

        public DateTime ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public virtual PostCategory Category { get; set; } = null!;
    }
}
```

#### 12. **AITag Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public enum TagType
    {
        Auto,
        Manual,
        AI
    }

    public class AITag
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Tag { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,4)")]
        public decimal Confidence { get; set; }

        [Required]
        public TagType TagType { get; set; }

        [MaxLength(50)]
        public string? ModelVersion { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;
    }
}
```

#### 13. **AITranslation Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class AITranslation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        [MaxLength(10)]
        public string OriginalLanguage { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string TargetLanguage { get; set; } = string.Empty;

        [Required]
        public string TranslatedContent { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,4)")]
        public decimal Confidence { get; set; }

        [Required]
        [MaxLength(50)]
        public string ModelVersion { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;
    }
}
```

#### 14. **AISummary Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class AISummary
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public string Summary { get; set; } = string.Empty;

        [Required]
        public int SummaryLength { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,4)")]
        public decimal Confidence { get; set; }

        [Required]
        [MaxLength(50)]
        public string ModelVersion { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;
    }
}
```

### **Social Features Entities**

#### 15. **Reaction Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public enum ReactionType
    {
        Like,
        Love,
        Laugh,
        Angry,
        Sad,
        Wow
    }

    public class Reaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public ReactionType ReactionType { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

#### 16. **Comment Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class Comment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? ParentCommentId { get; set; } // For replies

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("PostId")]
        public virtual Post Post { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public virtual ICollection<CommentReaction> CommentReactions { get; set; } = new List<CommentReaction>();
    }
}
```

#### 17. **CommentReaction Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class CommentReaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CommentId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public ReactionType ReactionType { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("CommentId")]
        public virtual Comment Comment { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

### **Notification System Entities**

#### 18. **Notification Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public enum NotificationType
    {
        Comment,
        Reaction,
        Follow,
        Mention,
        System
    }

    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Column(TypeName = "json")]
        public string? Data { get; set; } // Additional context data

        public bool IsRead { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

#### 19. **NotificationSetting Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class NotificationSetting
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public NotificationType NotificationType { get; set; }

        public bool IsEnabled { get; set; } = true;
        public bool EmailEnabled { get; set; } = true;
        public bool PushEnabled { get; set; } = true;
        public bool InAppEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
```

### **Audit & Security Entities**

#### 20. **AuditLog Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, LOGIN, LOGOUT

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty; // User, Post, Comment, etc.

        public Guid? EntityId { get; set; }

        [Column(TypeName = "json")]
        public string? OldValues { get; set; }

        [Column(TypeName = "json")]
        public string? NewValues { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
```

#### 21. **SecurityEvent Entity**
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public enum Severity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class SecurityEvent
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty; // FailedLogin, SuspiciousActivity, etc.

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [Required]
        public Severity Severity { get; set; }

        public bool IsResolved { get; set; } = false;

        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
```

## 🗄️ **DbContext Configuration**

### **EchoSpaceDbContext Implementation**
```csharp
using Microsoft.EntityFrameworkCore;
using EchoSpace.Core.Entities;

namespace EchoSpace.Infrastructure.Data
{
    public class EchoSpaceDbContext : DbContext
    {
        public EchoSpaceDbContext(DbContextOptions<EchoSpaceDbContext> options) : base(options)
        {
        }

        // Core User Tables
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        // Authentication & Security
        public DbSet<ThirdPartyAuthProvider> ThirdPartyAuthProviders { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

        // Content Management
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostImage> PostImages { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }
        public DbSet<PostCategoryAssignment> PostCategoryAssignments { get; set; }

        // AI Integration
        public DbSet<AIClassification> AIClassifications { get; set; }
        public DbSet<AITag> AITags { get; set; }
        public DbSet<AITranslation> AITranslations { get; set; }
        public DbSet<AISummary> AISummaries { get; set; }

        // Social Features
        public DbSet<Reaction> Reactions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentReaction> CommentReactions { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationSetting> NotificationSettings { get; set; }

        // Audit & Security
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SecurityEvent> SecurityEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Post relationships
            modelBuilder.Entity<Post>()
                .HasMany(p => p.PostImages)
                .WithOne(pi => pi.Post)
                .HasForeignKey(pi => pi.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Post>()
                .HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Post>()
                .HasMany(p => p.Reactions)
                .WithOne(r => r.Post)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Comment self-referencing relationship
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<UserSession>()
                .HasIndex(us => us.SessionToken)
                .IsUnique();

            modelBuilder.Entity<UserSession>()
                .HasIndex(us => us.RefreshToken)
                .IsUnique();

            // Configure composite indexes for performance
            modelBuilder.Entity<Reaction>()
                .HasIndex(r => new { r.PostId, r.UserId })
                .IsUnique();

            modelBuilder.Entity<CommentReaction>()
                .HasIndex(cr => new { cr.CommentId, cr.UserId })
                .IsUnique();

            // Configure JSON columns for SQL Server
            modelBuilder.Entity<UserProfile>()
                .Property(up => up.Interests)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<UserProfile>()
                .Property(up => up.PrivacySettings)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<Notification>()
                .Property(n => n.Data)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<AuditLog>()
                .Property(al => al.OldValues)
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<AuditLog>()
                .Property(al => al.NewValues)
                .HasColumnType("nvarchar(max)");

            // Configure decimal precision
            modelBuilder.Entity<AIClassification>()
                .Property(aic => aic.Confidence)
                .HasPrecision(5, 4);

            modelBuilder.Entity<AITag>()
                .Property(ait => ait.Confidence)
                .HasPrecision(5, 4);

            modelBuilder.Entity<AITranslation>()
                .Property(ait => ait.Confidence)
                .HasPrecision(5, 4);

            modelBuilder.Entity<AISummary>()
                .Property(ais => ais.Confidence)
                .HasPrecision(5, 4);

            modelBuilder.Entity<PostCategoryAssignment>()
                .Property(pca => pca.Confidence)
                .HasPrecision(5, 4);
        }
    }
}
```

## 🔗 **Key Relationships**

### **Primary Relationships**
- **Users** → **UserProfiles** (1:1)
- **Users** → **Posts** (1:Many)
- **Users** → **Comments** (1:Many)
- **Users** → **Reactions** (1:Many)
- **Posts** → **PostImages** (1:Many)
- **Posts** → **Comments** (1:Many)
- **Posts** → **Reactions** (1:Many)
- **Comments** → **CommentReactions** (1:Many)

### **AI Integration Relationships**
- **Posts** → **AIClassifications** (1:Many)
- **Posts** → **AITags** (1:Many)
- **Posts** → **AITranslations** (1:Many)
- **Posts** → **AISummaries** (1:Many)

### **Security & Audit Relationships**
- **Users** → **AuditLogs** (1:Many)
- **Users** → **SecurityEvents** (1:Many)
- **Users** → **UserSessions** (1:Many)

## 🛡️ **Security Considerations**

### **Data Protection**
- All sensitive data (passwords, tokens) must be encrypted
- Implement soft deletes for user data retention
- Add data retention policies for audit logs
- Use proper indexing for performance and security

### **Access Control**
- Implement row-level security where appropriate
- Add proper foreign key constraints
- Use database-level permissions for different service accounts

### **Performance Optimization**
- Add indexes on frequently queried columns
- Implement database partitioning for large tables (audit logs)
- Use connection pooling and query optimization

## 📊 **Entity Framework Implementation Strategy**

### **Phase 1: Core Entities & DbContext**
1. **Create Entity Models**: User, UserProfile, UserSession
2. **Create DbContext**: EchoSpaceDbContext with basic configuration
3. **Add Migrations**: `dotnet ef migrations add InitialCreate`
4. **Update Database**: `dotnet ef database update`
5. **Test Basic CRUD**: Create, Read, Update, Delete operations

### **Phase 2: Content Management**
1. **Create Content Entities**: Post, PostImage, PostCategory, PostCategoryAssignment
2. **Update DbContext**: Add new DbSets and relationships
3. **Add Migration**: `dotnet ef migrations add AddContentEntities`
4. **Update Database**: Apply new migration
5. **Test Content Operations**: Post creation, image upload, category assignment

### **Phase 3: Social Features**
1. **Create Social Entities**: Reaction, Comment, CommentReaction
2. **Update DbContext**: Add social features configuration
3. **Add Migration**: `dotnet ef migrations add AddSocialFeatures`
4. **Update Database**: Apply social features migration
5. **Test Social Operations**: Reactions, comments, replies

### **Phase 4: AI Integration**
1. **Create AI Entities**: AIClassification, AITag, AITranslation, AISummary
2. **Update DbContext**: Add AI integration configuration
3. **Add Migration**: `dotnet ef migrations add AddAIIntegration`
4. **Update Database**: Apply AI integration migration
5. **Test AI Operations**: Classification, tagging, translation, summarization

### **Phase 5: Notifications & Security**
1. **Create Notification Entities**: Notification, NotificationSetting
2. **Create Security Entities**: AuditLog, SecurityEvent
3. **Update DbContext**: Add final entities and configurations
4. **Add Migration**: `dotnet ef migrations add AddNotificationsAndSecurity`
5. **Update Database**: Apply final migration
6. **Test Complete System**: End-to-end functionality testing

### **Migration Commands**
```bash
# Create migration
dotnet ef migrations add <MigrationName> --project src/EchoSpace.Infrastructure --startup-project src/EchoSpace.UI

# Update database
dotnet ef database update --project src/EchoSpace.Infrastructure --startup-project src/EchoSpace.UI

# Remove last migration (if needed)
dotnet ef migrations remove --project src/EchoSpace.Infrastructure --startup-project src/EchoSpace.UI
```

## 🧪 **Testing Requirements**

### **Unit Tests**
- Test all entity relationships
- Validate constraint enforcement
- Test data integrity rules

### **Integration Tests**
- Test complete user workflows
- Validate AI integration data flow
- Test notification system

### **Performance Tests**
- Test with large datasets
- Validate query performance
- Test concurrent access scenarios

## 📈 **Success Criteria**

- [ ] All Entity Framework models created with proper relationships
- [ ] DbContext configured with all DbSets and relationships
- [ ] Data annotations and validation attributes applied
- [ ] Unique constraints and indexes configured
- [ ] Migration scripts generated and tested
- [ ] Database schema created successfully
- [ ] Entity relationships working correctly
- [ ] CRUD operations tested for all entities
- [ ] Unit tests passing for entity models
- [ ] Integration tests passing with database
- [ ] Performance optimization implemented
- [ ] Documentation updated with Entity Framework examples

## 🏷️ **Labels**
- `enhancement`
- `database`
- `architecture`
- `security`
- `ai-integration`

## 👥 **Assignees**
- Backend Developer
- Database Administrator
- Security Engineer

## 📅 **Milestone**
- Target: End of current sprint
- Priority: High
- Estimated effort: 5-8 story points

---

**Note**: This Entity Framework schema is designed to support the full EchoSpace feature set while maintaining security, performance, and scalability. All entities include proper data annotations, navigation properties, and security considerations based on the threat modeling analysis. The DbContext configuration ensures proper relationships, constraints, and performance optimization for the social media application.
