using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using EchoSpace.Core.Authorization.Requirements;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.UI.Authorization.Handlers
{
    /// <summary>
    /// Authorization handler for owner-based access control
    /// Checks if the current user owns the resource being accessed
    /// </summary>
    public class OwnerRequirementHandler : AuthorizationHandler<OwnerRequirement>
    {
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;
        private readonly ILogger<OwnerRequirementHandler> _logger;

        public OwnerRequirementHandler(
            IPostService postService,
            ICommentService commentService,
            ILogger<OwnerRequirementHandler> logger)
        {
            _postService = postService;
            _commentService = commentService;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OwnerRequirement requirement)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                _logger.LogWarning("User ID claim not found");
                return;
            }

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}", userIdClaim.Value);
                return;
            }

            // Get resource ID from route data
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
                _logger.LogWarning("User {UserId} is NOT owner of {ResourceType} {ResourceId}", userId, requirement.ResourceType, resourceGuid);
            }
        }
    }
}


