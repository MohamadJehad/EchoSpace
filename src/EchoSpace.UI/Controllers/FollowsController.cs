using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.DTOs.Auth;
using System.Security.Claims;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FollowsController : ControllerBase
    {
        private readonly ILogger<FollowsController> _logger;
        private readonly IFollowService _followService;

        public FollowsController(ILogger<FollowsController> logger, IFollowService followService)
        {
            _logger = logger;
            _followService = followService;
        }

        /// <summary>
        /// Follow a user
        /// </summary>
        [HttpPost("{userId}")]
        public async Task<ActionResult> FollowUser(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                var followerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (followerIdClaim == null || !Guid.TryParse(followerIdClaim.Value, out var followerId))
                {
                    return Unauthorized("User ID not found in token");
                }

                if (followerId == userId)
                {
                    return BadRequest(new { message = "Cannot follow yourself" });
                }

                var success = await _followService.FollowUserAsync(followerId, userId);
                if (!success)
                {
                    return BadRequest(new { message = "Already following this user or user not found" });
                }

                _logger.LogInformation("User {FollowerId} followed user {UserId}", followerId, userId);
                return Ok(new { message = "User followed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while following user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Unfollow a user
        /// </summary>
        [HttpDelete("{userId}")]
        public async Task<ActionResult> UnfollowUser(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                var followerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (followerIdClaim == null || !Guid.TryParse(followerIdClaim.Value, out var followerId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var success = await _followService.UnfollowUserAsync(followerId, userId);
                if (!success)
                {
                    return NotFound(new { message = "Not following this user" });
                }

                _logger.LogInformation("User {FollowerId} unfollowed user {UserId}", followerId, userId);
                return Ok(new { message = "User unfollowed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while unfollowing user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Check if current user is following a user
        /// </summary>
        [HttpGet("{userId}/status")]
        public async Task<ActionResult> GetFollowStatus(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                var followerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (followerIdClaim == null || !Guid.TryParse(followerIdClaim.Value, out var followerId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var isFollowing = await _followService.IsFollowingAsync(followerId, userId);
                return Ok(new { isFollowing });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking follow status for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get followers of a user
        /// </summary>
        [HttpGet("{userId}/followers")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetFollowers(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                var followers = await _followService.GetFollowersAsync(userId);
                return Ok(followers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting followers for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get users that a user is following
        /// </summary>
        [HttpGet("{userId}/following")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetFollowing(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                var following = await _followService.GetFollowingAsync(userId);
                return Ok(following);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting following for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get follower count for a user
        /// </summary>
        [HttpGet("{userId}/followers/count")]
        public async Task<ActionResult> GetFollowerCount(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                var count = await _followService.GetFollowerCountAsync(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting follower count for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get following count for a user
        /// </summary>
        [HttpGet("{userId}/following/count")]
        public async Task<ActionResult> GetFollowingCount(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                var count = await _followService.GetFollowingCountAsync(userId);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting following count for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}

