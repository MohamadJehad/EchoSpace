using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Posts
{
    public class UpdatePostRequest
    {
        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }
}
