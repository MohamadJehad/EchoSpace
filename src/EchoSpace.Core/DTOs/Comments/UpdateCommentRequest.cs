using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Comments
{
    public class UpdateCommentRequest
    {
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}
