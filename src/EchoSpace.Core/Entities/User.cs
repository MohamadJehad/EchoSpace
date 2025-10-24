using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class User
    {
        [Required]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        // Authentication Fields
        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        public bool IsEmailVerified { get; set; } = false;
        
        public bool IsActive { get; set; } = true;

        public DateTime? LockoutEnd { get; set; }
        
        public int AccessFailedCount { get; set; } = 0;

        public DateTime? LastLoginAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
        
        public virtual ICollection<AuthProvider> AuthProviders { get; set; } = new List<AuthProvider>();

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

        public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();

        public virtual ICollection<Follow> Following { get; set; } = new List<Follow>();
    }
}
