using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.Interfaces;
using System.Security.Claims;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuggestedUsersController : ControllerBase
    {
        private readonly ILogger<SuggestedUsersController> _logger;
        private readonly IUserService _userService;

        public SuggestedUsersController(ILogger<SuggestedUsersController> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
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

                // Transform to suggested user format
                var suggestedUsers = users
                    .Where(u => u.Role == Core.Enums.UserRole.User) // Only regular users
                    .Where(u => currentUserId == null || u.Id != currentUserId) // Exclude current user if authenticated
                    .OrderByDescending(u => u.CreatedAt) // Most recent first
                    .Take(count)
                    .Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        username = u.UserName,
                        email = u.Email,
                        createdAt = u.CreatedAt,
                        postsCount = u.Posts?.Count ?? 0
                    })
                    .ToList();

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
