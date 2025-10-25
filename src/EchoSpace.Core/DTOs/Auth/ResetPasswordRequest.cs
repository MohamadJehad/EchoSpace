using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for reset password request.
    /// </summary>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// The reset token received via email.
        /// </summary>
        [Required]
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// The new password.
        /// </summary>
        [Required]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
            ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
        public string NewPassword { get; set; } = string.Empty;
        
        /// <summary>
        /// Confirmation of the new password.
        /// </summary>
        [Required]
        [Compare("NewPassword", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
