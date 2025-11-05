using Microsoft.EntityFrameworkCore;
using EchoSpace.Infrastructure.Logging;
using EchoSpace.Core.Entities; // Ensure you have the correct using directive for your entities
using EchoSpace.Core.Interfaces.Services;

public class EchoSpaceDbContext : DbContext
{
    private readonly IAuditLogService _auditLogService;

    public EchoSpaceDbContext(DbContextOptions<EchoSpaceDbContext> options, IAuditLogService auditLogService) 
        : base(options)
    {
        _auditLogService = auditLogService;
    }
    public EchoSpaceDbContext(DbContextOptions<EchoSpaceDbContext> options) : base(options)
    {
    }

    public DbSet<Post> Posts { get; set; } // Define DbSet for Posts
    public DbSet<User> Users { get; set; } // Define DbSet for Users
    public DbSet<Follow> Follows { get; set; } // Define DbSet for Follows
    public DbSet<Comment> Comments { get; set; } // Define DbSet for Comments
    public DbSet<UserSession> UserSessions { get; set; } // Define DbSet for UserSessions
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } // Define DbSet for PasswordResetTokens
    public DbSet<AuthProvider> AuthProviders { get; set; } // Define DbSet for AuthProviders
    public DbSet<Like> Likes { get; set; } // Define DbSet for Likes

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Additional model configurations can go here
         // ✅ Define composite key
            modelBuilder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FollowedId });

         // Follow relationships
    modelBuilder.Entity<Follow>()
        .HasOne(f => f.Follower)
        .WithMany(u => u.Following)
        .HasForeignKey(f => f.FollowerId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Follow>()
        .HasOne(f => f.Followed)
        .WithMany(u => u.Followers)
        .HasForeignKey(f => f.FollowedId)
        .OnDelete(DeleteBehavior.Restrict);
    }

     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new EchoSpaceDbCommandInterceptor(_auditLogService));
        base.OnConfiguring(optionsBuilder);
    }
}