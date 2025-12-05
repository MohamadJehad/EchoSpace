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
        public DbSet<AccountUnlockToken> AccountUnlockTokens { get; set; }

        // Content Tables
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PostTag> PostTags { get; set; }
        public DbSet<PostReport> PostReports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                
                entity.HasOne(u => u.ProfilePhoto)
                    .WithMany()
                    .HasForeignKey(u => u.ProfilePhotoId)
                    .OnDelete(DeleteBehavior.SetNull);
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

            // Configure AccountUnlockToken
            modelBuilder.Entity<AccountUnlockToken>(entity =>
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

            // Configure Tag
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasIndex(t => t.Name).IsUnique();
            });

            // Configure PostTag (many-to-many junction table)
            modelBuilder.Entity<PostTag>(entity =>
            {
                entity.HasKey(pt => pt.PostTagId);

                entity.HasOne(pt => pt.Post)
                    .WithMany(p => p.PostTags)
                    .HasForeignKey(pt => pt.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pt => pt.Tag)
                    .WithMany(t => t.PostTags)
                    .HasForeignKey(pt => pt.TagId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Prevent duplicate post-tag combinations
                entity.HasIndex(pt => new { pt.PostId, pt.TagId }).IsUnique();
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

            // Configure PostReport
            modelBuilder.Entity<PostReport>(entity =>
            {
                entity.HasOne(r => r.Post)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(r => r.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Prevent duplicate reports from same user for same post
                entity.HasIndex(r => new { r.PostId, r.UserId }).IsUnique();
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
            //AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.TimestampUtc);
                entity.ToTable("AuditLog");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.TimestampUtc).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(x => x.UserIpAddress).HasMaxLength(45);
                entity.Property(x => x.ActionType).HasMaxLength(100).IsRequired();
                entity.Property(x => x.ResourceId).HasMaxLength(100);
                entity.Property(x => x.CorrelationId).HasMaxLength(100);
                entity.Property(x => x.ActionDetails).HasColumnType("nvarchar(max)");
                entity.HasIndex(x => x.TimestampUtc);
                entity.HasIndex(x => new { x.UserId, x.TimestampUtc });
                entity.HasIndex(x => new { x.ActionType, x.TimestampUtc });

            });
            // Configure Image
            modelBuilder.Entity<Image>(entity =>
            {
                entity.HasIndex(i => i.BlobName).IsUnique();
                entity.HasIndex(i => new { i.ContainerName, i.BlobName }).IsUnique();
                entity.HasIndex(i => i.UserId);
                entity.HasIndex(i => i.PostId);
                entity.HasIndex(i => i.Source);

                // Use NO ACTION to avoid cascade path conflicts with Posts -> Users cascade
                entity.HasOne(i => i.User)
                    .WithMany()
                    .HasForeignKey(i => i.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(i => i.Post)
                    .WithMany()
                    .HasForeignKey(i => i.PostId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
