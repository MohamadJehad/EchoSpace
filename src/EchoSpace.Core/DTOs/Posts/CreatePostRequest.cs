using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Posts
{
    public class CreatePostRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public List<Guid>? TagIds { get; set; }
    }
}
