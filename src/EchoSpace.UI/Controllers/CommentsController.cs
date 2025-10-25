using Microsoft.AspNetCore.Mvc;
using EchoSpace.Core.DTOs.Comments;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ILogger<CommentsController> _logger;
        private readonly ICommentService _commentService;

        public CommentsController(ILogger<CommentsController> logger, ICommentService commentService)
        {
            _logger = logger;
            _commentService = commentService;
        }

        /// <summary>
        /// Get all comments
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all comments");
                var comments = await _commentService.GetAllAsync();
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting all comments");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get a specific comment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CommentDto>> GetComment(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var comment = await _commentService.GetByIdAsync(id);
                if (comment == null)
                {
                    return NotFound($"Comment with ID {id} not found");
                }
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting comment {CommentId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get comments by post ID
        /// </summary>
        [HttpGet("post/{postId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByPost(Guid postId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting comments for post {PostId}", postId);
                var comments = await _commentService.GetByPostIdAsync(postId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting comments for post {PostId}", postId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get comments by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByUser(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting comments for user {UserId}", userId);
                var comments = await _commentService.GetByUserIdAsync(userId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting comments for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get comment count for a post
        /// </summary>
        [HttpGet("post/{postId}/count")]
        public async Task<ActionResult<int>> GetCommentCountByPost(Guid postId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting comment count for post {PostId}", postId);
                var count = await _commentService.GetCountByPostIdAsync(postId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting comment count for post {PostId}", postId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Create a new comment
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CreateCommentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var created = await _commentService.CreateAsync(request);
                return CreatedAtAction(nameof(GetComment), new { id = created.CommentId }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating comment for user {UserId} on post {PostId}", request?.UserId, request?.PostId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Update an existing comment
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CommentDto>> UpdateComment(Guid id, [FromBody] UpdateCommentRequest request, [FromQuery] Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user owns the comment (authorization)
                var isOwner = await _commentService.IsOwnerAsync(id, userId);
                if (!isOwner)
                {
                    return Forbid("You can only update your own comments");
                }

                var comment = await _commentService.UpdateAsync(id, request);
                if (comment == null)
                {
                    return NotFound($"Comment with ID {id} not found");
                }
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating comment {CommentId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteComment(Guid id, [FromQuery] Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                // Check if user owns the comment (authorization)
                var isOwner = await _commentService.IsOwnerAsync(id, userId);
                if (!isOwner)
                {
                    return Forbid("You can only delete your own comments");
                }

                var deleted = await _commentService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Comment with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting comment {CommentId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Check if a comment exists
        /// </summary>
        [HttpHead("{id}")]
        public async Task<ActionResult> CommentExists(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var exists = await _commentService.ExistsAsync(id);
                if (!exists)
                {
                    return NotFound();
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking if comment {CommentId} exists", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}

