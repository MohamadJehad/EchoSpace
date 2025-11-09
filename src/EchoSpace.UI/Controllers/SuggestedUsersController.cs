using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using EchoSpace.Core.Interfaces;
using System.Security.Claims;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("GeneralApiPolicy")]
    public class SuggestedUsersController : ControllerBase
    {
        private readonly ILogger<SuggestedUsersController> _logger;
        private readonly IUserService _userService;
        private readonly IPostService _postService;
        private readonly IFollowService _followService;

        public SuggestedUsersController(ILogger<SuggestedUsersController> logger, IUserService userService, IPostService postService, IFollowService followService)
        {
            _logger = logger;
            _userService = userService;
            _postService = postService;
            _followService = followService;
        }

        /// <summary>
        /// Get suggested users for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSuggestedUsers([FromQuery] int count = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting suggested users (count: {Count})", count);

                // Get current user ID from authentication context (if authenticated)
                Guid? currentUserId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("User is authenticated. Claims: {Claims}", string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));

                    var currentUserIdClaim = User.FindFirst("sub") ?? User.FindFirst("id") ?? User.FindFirst("user_id") ?? User.FindFirst("userId");
                    if (currentUserIdClaim != null && Guid.TryParse(currentUserIdClaim.Value, out var parsedUserId))
                    {
                        currentUserId = parsedUserId;
                        _logger.LogInformation("Found current user ID: {UserId}", currentUserId);
                    }
                    else
                    {
                        _logger.LogWarning("Current user ID not found in authentication context. Available claims: {Claims}",
                            string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                    }
                }
                else
                {
                    _logger.LogInformation("User is not authenticated, will show all users");
                }

                var users = await _userService.GetAllAsync();

                // Get users that current user is already following (to exclude them)
                var followingUserIds = new HashSet<Guid>();
                if (currentUserId.HasValue)
                {
                    var following = await _followService.GetFollowingAsync(currentUserId.Value);
                    followingUserIds = following.Select(u => u.Id).ToHashSet();
                    _logger.LogInformation("User {UserId} is following {Count} users", currentUserId, followingUserIds.Count);
                }

                // Filter and order users first
                var filteredUsers = users
                    .Where(u => u.Role == Core.Enums.UserRole.User) // Only regular users
                    .Where(u => currentUserId == null || u.Id != currentUserId) // Exclude current user if authenticated
                    .Where(u => !followingUserIds.Contains(u.Id)) // Exclude users already being followed
                    .OrderByDescending(u => u.CreatedAt) // Most recent first
                    .Take(count)
                    .ToList();

                // Get posts count for each user
                var suggestedUsers = new List<object>();
                foreach (var user in filteredUsers)
                {
                    var userPosts = await _postService.GetByUserIdAsync(user.Id);
                    var postsCount = userPosts.Count();

                    suggestedUsers.Add(new
                    {
                        id = user.Id,
                        name = user.Name,
                        username = user.UserName,
                        email = user.Email,
                        createdAt = user.CreatedAt,
                        postsCount = postsCount
                    });
                }

                return Ok(suggestedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting suggested users");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
