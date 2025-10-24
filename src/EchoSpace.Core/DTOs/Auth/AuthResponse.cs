namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for authentication response.
    /// Contains access token, refresh token, and user information.
    /// Returned by registration, login, token refresh, and Google OAuth authentication.
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// JWT access token used for API authentication.
        /// Expires in 15 minutes. Must be included in Authorization header.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token used to obtain new access tokens without re-authentication.
        /// Expires in 7 days. Stored securely in localStorage.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time in seconds.
        /// Default: 900 seconds (15 minutes).
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// User information including ID, name, and email.
        /// </summary>
        public UserDto User { get; set; } = null!;
    }
}
