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
        /// The user's password. Must be at least 10 characters long.
        /// </summary>
        [Required]
        [MinLength(10)]
        public string Password { get; set; } = string.Empty;
    }
}

