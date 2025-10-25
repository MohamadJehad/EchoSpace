using Microsoft.AspNetCore.Mvc;
using EchoSpace.Core.DTOs.Posts;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ILogger<PostsController> _logger;
        private readonly IPostService _postService;

        public PostsController(ILogger<PostsController> logger, IPostService postService)
        {
            _logger = logger;
            _postService = postService;
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
                var posts = await _postService.GetAllAsync();
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
                var post = await _postService.GetByIdAsync(id);
                if (post == null)
                {
                    return NotFound($"Post with ID {id} not found");
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
                var posts = await _postService.GetByUserIdAsync(userId);
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
                var posts = await _postService.GetRecentAsync(count);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting recent posts");
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
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PostDto>> UpdatePost(Guid id, [FromBody] UpdatePostRequest request, [FromQuery] Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user owns the post (authorization)
                var isOwner = await _postService.IsOwnerAsync(id, userId);
                if (!isOwner)
                {
                    return Forbid("You can only update your own posts");
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
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePost(Guid id, [FromQuery] Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                // Check if user owns the post (authorization)
                var isOwner = await _postService.IsOwnerAsync(id, userId);
                if (!isOwner)
                {
                    return Forbid("You can only delete your own posts");
                }

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
