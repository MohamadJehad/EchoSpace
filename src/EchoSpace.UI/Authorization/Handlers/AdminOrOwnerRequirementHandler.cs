using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using EchoSpace.Core.Authorization.Requirements;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.UI.Authorization.Handlers
{
    /// <summary>
    /// Authorization handler for combined Admin OR Owner requirement
    /// Grants access if user is Admin OR if user owns the resource
    /// </summary>
    public class AdminOrOwnerRequirementHandler : AuthorizationHandler<AdminOrOwnerRequirement>
    {
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;
        private readonly ILogger<AdminOrOwnerRequirementHandler> _logger;

        public AdminOrOwnerRequirementHandler(
            IPostService postService,
            ICommentService commentService,
            ILogger<AdminOrOwnerRequirementHandler> logger)
        {
            _postService = postService;
            _commentService = commentService;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminOrOwnerRequirement requirement)
        {
            // First, check if user is Admin or Moderator
            var userRoleClaim = context.User.FindFirst(ClaimTypes.Role);
            if (userRoleClaim != null)
            {
                var role = userRoleClaim.Value;
                if (role == "Admin" || role == "Moderator")
                {
                    context.Succeed(requirement);
                    _logger.LogInformation("User has admin/moderator role: {Role}", role);
                    return;
                }
            }

            // Second, check if user owns the resource
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("User ID claim not found or invalid");
                return;
            }

            var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("Unable to access HTTP context");
                return;
            }

            var resourceId = httpContext.Request.RouteValues["id"]?.ToString();

            if (string.IsNullOrEmpty(resourceId) || !Guid.TryParse(resourceId, out var resourceGuid))
            {
                _logger.LogWarning("Resource ID not found or invalid in route");
                return;
            }

            bool isOwner = false;

            switch (requirement.ResourceType.ToLower())
            {
                case "post":
                    isOwner = await _postService.IsOwnerAsync(resourceGuid, userId);
                    break;
                case "comment":
                    isOwner = await _commentService.IsOwnerAsync(resourceGuid, userId);
                    break;
                default:
                    _logger.LogWarning("Unknown resource type: {ResourceType}", requirement.ResourceType);
                    return;
            }

            if (isOwner)
            {
                context.Succeed(requirement);
                _logger.LogInformation("User {UserId} is owner of {ResourceType} {ResourceId}", userId, requirement.ResourceType, resourceGuid);
            }
            else
            {
                _logger.LogWarning("User {UserId} is neither admin nor owner of {ResourceType} {ResourceId}", userId, requirement.ResourceType, resourceGuid);
            }
        }
    }
}


