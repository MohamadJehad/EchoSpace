using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class PostTag
    {
        [Key]
        public Guid PostTagId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid TagId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(PostId))]
        public virtual Post Post { get; set; } = null!;

        [ForeignKey(nameof(TagId))]
        public virtual Tag Tag { get; set; } = null!;
    }
}

