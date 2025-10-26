using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for user registration request.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// The user's full name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The user's email address. Must be unique in the system.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The user's password. Must be at least 10 characters with one uppercase letter and one special character.
        /// </summary>
        [Required]
        [MinLength(10)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[@$!%*?&#^~])[A-Za-z\d@$!%*?&#^~]{10,}$", 
            ErrorMessage = "Password must be at least 10 characters with at least one uppercase letter and one special character (@$!%*?&#^~).")]
        public string Password { get; set; } = string.Empty;
    }
}

