namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for forgot password response.
    /// </summary>
    public class ForgotPasswordResponse
    {
        /// <summary>
        /// Success message indicating that reset instructions have been sent.
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Indicates if the email was found and reset instructions were sent.
        /// Always returns true for security (prevents user enumeration).
        /// </summary>
        public bool Success { get; set; } = true;
    }
}
