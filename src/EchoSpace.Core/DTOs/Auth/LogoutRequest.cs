using System.ComponentModel.DataAnnotations;

namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for user logout request.
    /// Used to invalidate a user session by revoking the refresh token.
    /// </summary>
    public class LogoutRequest
    {
        /// <summary>
        /// The refresh token associated with the session to be terminated.
        /// </summary>
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}

