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
        /// The new password. Must be at least 10 characters with one uppercase letter and one special character.
        /// </summary>
        [Required]
        [MinLength(10)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[@$!%*?&#^~])[A-Za-z\d@$!%*?&#^~]{10,}$",
            ErrorMessage = "Password must be at least 10 characters with at least one uppercase letter and one special character (@$!%*?&#^~).")]
        public string NewPassword { get; set; } = string.Empty;
        
        /// <summary>
        /// Confirmation of the new password.
        /// </summary>
        [Required]
        [Compare("NewPassword", ErrorMessage = "Password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
