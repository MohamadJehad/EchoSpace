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
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsUsed { get; set; } = false;
        
        public DateTime? UsedAt { get; set; }
        
        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
