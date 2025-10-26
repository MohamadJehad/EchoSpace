using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using EchoSpace.Core.Authorization.Requirements;

namespace EchoSpace.Core.Authorization.Handlers
{
    /// <summary>
    /// Authorization handler for role-based access control
    /// Checks if the current user has the required role
    /// </summary>
    public class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
    {
        private readonly ILogger<RoleRequirementHandler> _logger;

        public RoleRequirementHandler(ILogger<RoleRequirementHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            var userRoleClaim = context.User.FindFirst(ClaimTypes.Role);
            
            if (userRoleClaim == null)
            {
                _logger.LogWarning("Role claim not found for user");
                return Task.CompletedTask;
            }

            var userRole = userRoleClaim.Value;
            
            if (requirement.AllowedRoles.Contains(userRole))
            {
                context.Succeed(requirement);
                _logger.LogInformation("User has required role: {Role}", userRole);
            }
            else
            {
                _logger.LogWarning("User role {UserRole} is not in allowed roles: {AllowedRoles}", userRole, string.Join(", ", requirement.AllowedRoles));
            }

            return Task.CompletedTask;
        }
    }
}

