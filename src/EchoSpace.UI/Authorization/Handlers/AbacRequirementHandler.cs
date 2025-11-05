using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using EchoSpace.Core.Authorization.Requirements;
using EchoSpace.Core.Authorization.ABAC;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces; 

namespace EchoSpace.UI.Authorization.Handlers
{
    /// <summary>
    /// ABAC (Attribute-Based Access Control) authorization handler
    /// Evaluates access based on Subject, Resource, Action, and Environment attributes
    /// </summary>
    public class AbacRequirementHandler : AuthorizationHandler<AbacRequirement>
    {
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;
        private readonly IUserService _userService;
        private readonly ILogger<AbacRequirementHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AbacRequirementHandler(
            IPostService postService,
            ICommentService commentService,
            IUserService userService,
            ILogger<AbacRequirementHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _postService = postService;
            _commentService = commentService;
            _userService = userService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AbacRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HTTP context is null");
                return;
            }

            // Build ABAC context from current request
            var abacContext = await BuildAbacContext(context, requirement, httpContext);

            // Special handling for AdminOrOwner policies (OR logic)
            if (requirement.Policy.PolicyName.Contains("AdminOrOwner"))
            {
                // Check if user is Admin/Moderator OR owner
                bool isAdminOrModerator = abacContext.Subject.Role == "Admin" || abacContext.Subject.Role == "Moderator";
                bool isOwner = abacContext.Resource.OwnerId.HasValue && 
                               abacContext.Subject.UserId == abacContext.Resource.OwnerId.Value;

                if (isAdminOrModerator || isOwner)
                {
                    context.Succeed(requirement);
                    _logger.LogInformation(
                        "ABAC policy '{PolicyName}' evaluated successfully (Admin={IsAdmin}, Owner={IsOwner}) for user {UserId} on {ResourceType}",
                        requirement.Policy.PolicyName,
                        isAdminOrModerator,
                        isOwner,
                        abacContext.Subject.UserId,
                        requirement.ResourceType);
                    return;
                }
            }
            
            // Special handling for Owner policies (UserId equals OwnerId)
            if (requirement.Policy.PolicyName.Contains("Owner") && !requirement.Policy.PolicyName.Contains("Admin"))
            {
                bool isOwner = abacContext.Resource.OwnerId.HasValue && 
                               abacContext.Subject.UserId == abacContext.Resource.OwnerId.Value;

                if (isOwner)
                {
                    context.Succeed(requirement);
                    _logger.LogInformation(
                        "ABAC policy '{PolicyName}' evaluated successfully for owner {UserId} on {ResourceType}",
                        requirement.Policy.PolicyName,
                        abacContext.Subject.UserId,
                        requirement.ResourceType);
                    return;
                }
            }

            // Evaluate standard policy rules
            if (requirement.Policy.Evaluate(abacContext))
            {
                context.Succeed(requirement);
                _logger.LogInformation(
                    "ABAC policy '{PolicyName}' evaluated successfully for user {UserId} on {ResourceType}",
                    requirement.Policy.PolicyName,
                    abacContext.Subject.UserId,
                    requirement.ResourceType);
            }
            else
            {
                _logger.LogWarning(
                    "ABAC policy '{PolicyName}' evaluation failed for user {UserId} on {ResourceType}",
                    requirement.Policy.PolicyName,
                    abacContext.Subject.UserId,
                    requirement.ResourceType);
            }
        }

        private async Task<AbacContext> BuildAbacContext(
            AuthorizationHandlerContext context,
            AbacRequirement requirement,
            HttpContext httpContext)
        {
            var abacContext = new AbacContext();

            // Build Subject attributes
            abacContext.Subject = BuildSubjectAttributes(context);

            // Build Resource attributes
            abacContext.Resource = await BuildResourceAttributes(requirement, httpContext, abacContext.Subject.UserId);

            // Build Action attributes
            abacContext.Action = BuildActionAttributes(requirement, httpContext);

            // Build Environment attributes
            abacContext.Environment = BuildEnvironmentAttributes(httpContext);

            return abacContext;
        }

        private SubjectAttributes BuildSubjectAttributes(AuthorizationHandlerContext context)
        {
            var subject = new SubjectAttributes
            {
                IsAuthenticated = context.User.Identity?.IsAuthenticated ?? false
            };

            // Extract user ID
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                subject.UserId = userId;
            }

            // Extract role
            var roleClaim = context.User.FindFirst(ClaimTypes.Role);
            subject.Role = roleClaim?.Value;

            // Extract email
            var emailClaim = context.User.FindFirst(ClaimTypes.Email);
            subject.Email = emailClaim?.Value;

            // Extract name
            var nameClaim = context.User.FindFirst(ClaimTypes.Name);
            subject.Name = nameClaim?.Value;

            return subject;
        }

        private async Task<ResourceAttributes> BuildResourceAttributes(
            AbacRequirement requirement,
            HttpContext httpContext,
            Guid userId)
        {
            var resource = new ResourceAttributes
            {
                ResourceType = requirement.ResourceType
            };

            // Extract resource ID from route
            var resourceId = httpContext.Request.RouteValues["id"]?.ToString();
            if (!string.IsNullOrEmpty(resourceId) && Guid.TryParse(resourceId, out var resourceGuid))
            {
                resource.ResourceId = resourceGuid;

                // Fetch resource owner based on resource type
                switch (requirement.ResourceType.ToLower())
                {
                    case "post":
                        resource.OwnerId = await GetPostOwnerId(resourceGuid);
                        break;
                    case "comment":
                        resource.OwnerId = await GetCommentOwnerId(resourceGuid);
                        break;
                    case "user":
                        resource.OwnerId = resourceGuid; // For user resources, owner is the resource itself
                        break;
                }
            }

            return resource;
        }

        private async Task<Guid?> GetPostOwnerId(Guid postId)
        {
            try
            {
                var post = await _postService.GetByIdAsync(postId, null);
                return post?.UserId;
            }
            catch
            {
                return null;
            }
        }

        private async Task<Guid?> GetCommentOwnerId(Guid commentId)
        {
            try
            {
                var comment = await _commentService.GetByIdAsync(commentId);
                return comment?.UserId;
            }
            catch
            {
                return null;
            }
        }

        private ActionAttributes BuildActionAttributes(AbacRequirement requirement, HttpContext httpContext)
        {
            return new ActionAttributes
            {
                Action = requirement.Action ?? httpContext.Request.Method,
                HttpMethod = httpContext.Request.Method,
                Controller = httpContext.Request.RouteValues["controller"]?.ToString(),
                Endpoint = httpContext.Request.Path.Value
            };
        }

        private EnvironmentAttributes BuildEnvironmentAttributes(HttpContext httpContext)
        {
            var connection = httpContext.Connection;
            return new EnvironmentAttributes
            {
                RequestTime = DateTime.UtcNow,
                IpAddress = connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers["User-Agent"].ToString()
            };
        }
    }
}

