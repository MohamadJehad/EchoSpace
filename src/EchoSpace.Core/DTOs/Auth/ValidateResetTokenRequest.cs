using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for validating reset token request.
    /// </summary>
    public class ValidateResetTokenRequest
    {
        /// <summary>
        /// The reset token to validate.
        /// </summary>
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
