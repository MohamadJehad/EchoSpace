using System.ComponentModel.DataAnnotations;
using EchoSpace.Core.Enums;

namespace EchoSpace.Core.DTOs
{
    public class UpdateUserRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public UserRole? Role { get; set; }
    }
}
