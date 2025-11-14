using System.ComponentModel.DataAnnotations;
using EchoSpace.Core.Enums;

namespace EchoSpace.Core.DTOs
{
    public class ChangeUserRoleRequest
    {
        [Required]
        public UserRole Role { get; set; }
    }
}


