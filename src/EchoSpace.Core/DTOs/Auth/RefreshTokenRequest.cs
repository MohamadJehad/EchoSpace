using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for token refresh request.
    /// Used to obtain a new access token using a valid refresh token.
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// The refresh token obtained during login or registration.
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}

