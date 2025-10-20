using System.ComponentModel.DataAnnotations;

namespace EchoSpace.DTOs
{
    public class CreateUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}


