using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Auth
{
    public class EmailVerificationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;
    }
}
