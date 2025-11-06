using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Comments
{
    public class CreateCommentRequest
    {
        [Required]
        public Guid PostId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}
