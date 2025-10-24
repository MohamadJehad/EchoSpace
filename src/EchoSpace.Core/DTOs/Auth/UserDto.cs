namespace EchoSpace.Core.DTOs.Auth
{
    /// <summary>
    /// Data transfer object for user information.
    /// Contains basic user details returned in authentication responses.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Unique identifier for the user.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The user's full name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The user's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}

