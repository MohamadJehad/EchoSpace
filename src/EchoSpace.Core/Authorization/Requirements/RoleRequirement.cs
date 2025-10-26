namespace EchoSpace.Core.Authorization.Requirements
{
    /// <summary>
    /// Authorization requirement for role-based access control
    /// </summary>
    public class RoleRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    {
        public string[] AllowedRoles { get; }

        public RoleRequirement(params string[] allowedRoles)
        {
            AllowedRoles = allowedRoles;
        }
    }
}

