using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Tags
{
    public class CreateTagRequest
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(7)]
        public string? Color { get; set; }
    }
}

