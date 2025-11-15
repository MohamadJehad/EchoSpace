using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class Tag
    {
        [Key]
        public Guid TagId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(7)]
        public string? Color { get; set; } // Hex color code for UI display

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    }
}

