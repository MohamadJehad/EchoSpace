using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class AuthProvider
    {
        [Key]
        public Guid AuthId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Provider { get; set; } = string.Empty; // Google, Facebook, etc.

        [Required]
        [MaxLength(255)]
        public string ProviderUid { get; set; } = string.Empty; // External provider's user ID

        [MaxLength(500)]
        public string? AccessToken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}

