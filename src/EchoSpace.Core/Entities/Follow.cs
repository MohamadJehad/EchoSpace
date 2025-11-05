using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EchoSpace.Core.Entities
{
    public class Follow
    {
        [Required]
        public Guid FollowerId { get; set; }

        [Required]
        public Guid FollowedId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(FollowerId))]
        public virtual User Follower { get; set; } = null!;

        [ForeignKey(nameof(FollowedId))]
        public virtual User Followed { get; set; } = null!;
    }
}

