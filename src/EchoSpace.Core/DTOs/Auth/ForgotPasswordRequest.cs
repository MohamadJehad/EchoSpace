using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for forgot password request.
    /// </summary>
    public class ForgotPasswordRequest
    {
        /// <summary>
        /// The user's email address to send reset instructions to.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
