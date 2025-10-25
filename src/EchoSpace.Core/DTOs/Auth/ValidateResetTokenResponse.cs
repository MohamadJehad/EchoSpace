namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for validate reset token response.
    /// </summary>
    public class ValidateResetTokenResponse
    {
        /// <summary>
        /// Indicates if the token is valid.
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Message describing the validation result.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
