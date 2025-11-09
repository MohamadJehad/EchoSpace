using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("GeneralApiPolicy")]
    public class LikesController : ControllerBase
    {
        private readonly ILogger<LikesController> _logger;
        private readonly ILikeService _likeService;

        public LikesController(ILogger<LikesController> logger, ILikeService likeService)
        {
            _logger = logger;
            _likeService = likeService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        /// <summary>
        /// Like a post
        /// </summary>
        [HttpPost("{postId}")]
        public async Task<ActionResult> LikePost(Guid postId, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                _logger.LogInformation("User {UserId} attempting to like post {PostId}", currentUserId, postId);
                
                var result = await _likeService.LikePostAsync(postId, currentUserId);
                
                if (!result)
                {
                    return BadRequest("Post is already liked by this user");
                }

                var likeCount = await _likeService.GetLikeCountAsync(postId);
                
                return Ok(new { 
                    success = true, 
                    message = "Post liked successfully",
                    likesCount = likeCount 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("User ID not found in token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while liking post {PostId}", postId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Unlike a post
        /// </summary>
        [HttpDelete("{postId}")]
        public async Task<ActionResult> UnlikePost(Guid postId, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                _logger.LogInformation("User {UserId} attempting to unlike post {PostId}", currentUserId, postId);
                
                var result = await _likeService.UnlikePostAsync(postId, currentUserId);
                
                if (!result)
                {
                    return BadRequest("Post is not liked by this user");
                }

                var likeCount = await _likeService.GetLikeCountAsync(postId);
                
                return Ok(new { 
                    success = true, 
                    message = "Post unliked successfully",
                    likesCount = likeCount 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("User ID not found in token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while unliking post {PostId}", postId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Toggle like status (like if not liked, unlike if liked)
        /// </summary>
        [HttpPost("{postId}/toggle")]
        public async Task<ActionResult> ToggleLike(Guid postId, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                _logger.LogInformation("User {UserId} toggling like for post {PostId}", currentUserId, postId);
                
                var isLiked = await _likeService.IsLikedByUserAsync(postId, currentUserId);
                bool result;
                
                if (isLiked)
                {
                    result = await _likeService.UnlikePostAsync(postId, currentUserId);
                }
                else
                {
                    result = await _likeService.LikePostAsync(postId, currentUserId);
                }

                if (!result)
                {
                    return BadRequest("Failed to toggle like status");
                }

                var likeCount = await _likeService.GetLikeCountAsync(postId);
                
                return Ok(new { 
                    success = true, 
                    isLiked = !isLiked,
                    likesCount = likeCount 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("User ID not found in token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while toggling like for post {PostId}", postId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get like status for a post
        /// </summary>
        [HttpGet("{postId}/status")]
        public async Task<ActionResult> GetLikeStatus(Guid postId, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isLiked = await _likeService.IsLikedByUserAsync(postId, currentUserId);
                var likeCount = await _likeService.GetLikeCountAsync(postId);
                
                return Ok(new { 
                    isLiked,
                    likesCount = likeCount 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("User ID not found in token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting like status for post {PostId}", postId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}

