using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Authorization.Attributes;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostsController : ControllerBase
    {
        private readonly ILogger<PostsController> _logger;
        private readonly IPostService _postService;
        private readonly ILikeService _likeService;

        public PostsController(ILogger<PostsController> logger, IPostService postService, ILikeService likeService)
        {
            _logger = logger;
            _postService = postService;
            _likeService = likeService;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Get all posts
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all posts");
                var currentUserId = GetCurrentUserId();
                var posts = await _postService.GetAllAsync(currentUserId);
                
                // Populate like status for current user
                if (currentUserId.HasValue)
                {
                    foreach (var post in posts)
                    {
                        post.IsLikedByCurrentUser = await _likeService.IsLikedByUserAsync(post.PostId, currentUserId.Value);
                        post.LikesCount = await _likeService.GetLikeCountAsync(post.PostId);
                    }
                }
                
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting all posts");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get a specific post by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PostDto>> GetPost(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var post = await _postService.GetByIdAsync(id, currentUserId);
                if (post == null)
                {
                    return NotFound($"Post with ID {id} not found");
                }
                
                // Populate like status for current user
                if (currentUserId.HasValue)
                {
                    post.IsLikedByCurrentUser = await _likeService.IsLikedByUserAsync(id, currentUserId.Value);
                    post.LikesCount = await _likeService.GetLikeCountAsync(id);
                }
                
                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting post {PostId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get posts by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPostsByUser(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting posts for user {UserId}", userId);
                var currentUserId = GetCurrentUserId();
                var posts = await _postService.GetByUserIdAsync(userId, currentUserId);
                
                // Populate like status for current user
                if (currentUserId.HasValue)
                {
                    foreach (var post in posts)
                    {
                        post.IsLikedByCurrentUser = await _likeService.IsLikedByUserAsync(post.PostId, currentUserId.Value);
                        post.LikesCount = await _likeService.GetLikeCountAsync(post.PostId);
                    }
                }
                
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting posts for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get recent posts
        /// </summary>
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetRecentPosts([FromQuery] int count = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting recent posts (count: {Count})", count);
                var currentUserId = GetCurrentUserId();
                var posts = await _postService.GetRecentAsync(count, currentUserId);
                
                // Populate like status for current user
                if (currentUserId.HasValue)
                {
                    foreach (var post in posts)
                    {
                        post.IsLikedByCurrentUser = await _likeService.IsLikedByUserAsync(post.PostId, currentUserId.Value);
                        post.LikesCount = await _likeService.GetLikeCountAsync(post.PostId);
                    }
                }
                
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting recent posts");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get posts from users that the current user is following
        /// </summary>
        [HttpGet("following")]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPostsFromFollowing(CancellationToken cancellationToken)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                _logger.LogInformation("Getting posts from following for user {UserId}", userId);
                var posts = await _postService.GetPostsFromFollowingAsync(userId);
                
                // Populate like status for current user
                foreach (var post in posts)
                {
                    post.IsLikedByCurrentUser = await _likeService.IsLikedByUserAsync(post.PostId, userId);
                    post.LikesCount = await _likeService.GetLikeCountAsync(post.PostId);
                }
                
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting posts from following");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Create a new post
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var created = await _postService.CreateAsync(request);
                return CreatedAtAction(nameof(GetPost), new { id = created.PostId }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating post for user {UserId}", request?.UserId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Update an existing post
        /// ABAC: Requires user to own the post OR be Admin/Moderator
        /// </summary>
        [HttpPut("{id}")]
        [RequireAdminOrOwner("Post")]
        public async Task<ActionResult<PostDto>> UpdatePost(Guid id, [FromBody] UpdatePostRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var post = await _postService.UpdateAsync(id, request);
                if (post == null)
                {
                    return NotFound($"Post with ID {id} not found");
                }
                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating post {PostId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Delete a post
        /// ABAC: Requires user to own the post OR be Admin/Moderator
        /// </summary>
        [HttpDelete("{id}")]
        [RequireAdminOrOwner("Post")]
        public async Task<ActionResult> DeletePost(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _postService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Post with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting post {PostId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Check if a post exists
        /// </summary>
        [HttpHead("{id}")]
        public async Task<ActionResult> PostExists(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var exists = await _postService.ExistsAsync(id);
                if (!exists)
                {
                    return NotFound();
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking if post {PostId} exists", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
