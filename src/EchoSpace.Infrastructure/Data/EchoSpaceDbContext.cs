using EchoSpace.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Infrastructure.Data
{
    public class EchoSpaceDbContext : DbContext
    {
        public EchoSpaceDbContext(DbContextOptions<EchoSpaceDbContext> options) : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }

        // Authentication Tables
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<AuthProvider> AuthProviders { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        // Content Tables
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Follow> Follows { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Configure UserSession
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasIndex(us => us.RefreshToken).IsUnique();
                entity.HasOne(us => us.User)
                    .WithMany(u => u.UserSessions)
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure AuthProvider
            modelBuilder.Entity<AuthProvider>(entity =>
            {
                entity.HasOne(ap => ap.User)
                    .WithMany(u => u.AuthProviders)
                    .HasForeignKey(ap => ap.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PasswordResetToken
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasIndex(t => t.Token).IsUnique();
                entity.HasIndex(t => t.UserId);
                entity.HasIndex(t => t.ExpiresAt);
                
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Post
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasOne(p => p.User)
                    .WithMany(u => u.Posts)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Comment
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasOne(c => c.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Like
            modelBuilder.Entity<Like>(entity =>
            {
                entity.HasOne(l => l.Post)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(l => l.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(l => l.User)
                    .WithMany(u => u.Likes)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Prevent duplicate likes
                entity.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();
            });

            // Configure Follow
            modelBuilder.Entity<Follow>(entity =>
            {
                entity.HasKey(f => new { f.FollowerId, f.FollowedId });

                entity.HasOne(f => f.Follower)
                    .WithMany(u => u.Following)
                    .HasForeignKey(f => f.FollowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Followed)
                    .WithMany(u => u.Followers)
                    .HasForeignKey(f => f.FollowedId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Prevent self-follow
                entity.ToTable(t => t.HasCheckConstraint("CK_Follow_NotSelf", "[FollowerId] != [FollowedId]"));
            });
        }
    }
}
