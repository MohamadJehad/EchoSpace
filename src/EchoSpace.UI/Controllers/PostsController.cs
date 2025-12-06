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
        private readonly IAuditLogDBService _auditLogDBService;
        private readonly IPostReportService _postReportService;
        private readonly IFollowRepository _followRepository;
        public PostsController(ILogger<PostsController> logger, IPostService postService, ILikeService likeService, IAuditLogDBService auditLogDBService, IPostReportService postReportService, IFollowRepository followRepository)
        {
            _logger = logger;
            _postService = postService;
            _likeService = likeService;
            _auditLogDBService = auditLogDBService;
            _postReportService = postReportService;
            _followRepository = followRepository;
        }   


        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
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
                
                // Populate like status and follow status for current user
                if (currentUserId.HasValue)
                {
                    // Get unique author IDs
                    var authorIds = posts.Select(p => p.UserId).Distinct().ToList();
                    
                    // Batch get follow statuses
                    var followStatuses = await _followRepository.GetFollowStatusesAsync(currentUserId.Value, authorIds);
                    
                    foreach (var post in posts)
                    {
                        post.IsLikedByCurrentUser = await _likeService.IsLikedByUserAsync(post.PostId, currentUserId.Value);
                        post.LikesCount = await _likeService.GetLikeCountAsync(post.PostId);
                        // Set follow status (false if not following or if it's own post)
                        post.IsFollowingAuthor = post.UserId != currentUserId.Value && followStatuses.GetValueOrDefault(post.UserId, false);
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
                
                // Get unique author IDs
                var authorIds = posts.Select(p => p.UserId).Distinct().ToList();
                
                // Batch get follow statuses
                var followStatuses = await _followRepository.GetFollowStatusesAsync(userId, authorIds);
                
                // Populate like status and follow status for current user
                foreach (var post in posts)
                {
                    post.IsLikedByCurrentUser = await _likeService.IsLikedByUserAsync(post.PostId, userId);
                    post.LikesCount = await _likeService.GetLikeCountAsync(post.PostId);
                    // Set follow status (false if not following or if it's own post)
                    post.IsFollowingAuthor = post.UserId != userId && followStatuses.GetValueOrDefault(post.UserId, false);
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
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                 
                if (HttpContext?.Request?.Headers.ContainsKey("X-Forwarded-For") == true)
                {
                    ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                }
                 // Log the action to the database
                 await _auditLogDBService.LogAsync(
                    actionType: "PostCreated",
                    userId: GetCurrentUserId(),                   // Or from claims: Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                    resourceId: created.PostId.ToString(),
                    details: new 
                    { 
                        ContentLength = created.Content?.Length ?? 0 
                    },
                    correlationId: Guid.NewGuid().ToString(),
                    ipAddress: ipAddress
                );

                return CreatedAtAction(nameof(GetPost), new { id = created.PostId }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating post for user {UserId}", request?.UserId);
                  // Optional: log failure to AuditLog as well
                   var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                   
                    if (HttpContext?.Request?.Headers.ContainsKey("X-Forwarded-For") == true)
                    {
                        ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    }
                   await _auditLogDBService.LogAsync(
                    actionType: "PostCreateFailed",
                    userId: GetCurrentUserId(),
                    details: new { ErrorMessage = ex.Message },
                    correlationId: Guid.NewGuid().ToString(),
                    ipAddress: ipAddress
                );
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
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                 
                if (HttpContext?.Request?.Headers.ContainsKey("X-Forwarded-For") == true)
                {
                    ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                }
                 // Log the action to the database
                 await _auditLogDBService.LogAsync(
                    actionType: "PostUpdated",
                    userId: GetCurrentUserId(),                   // Or from claims: Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                    resourceId: post.PostId.ToString(),
                    details: new 
                    { 
                        ContentLength = post.Content?.Length ?? 0 
                    },
                    correlationId: Guid.NewGuid().ToString(),
                    ipAddress: ipAddress
                );

                return Ok(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating post {PostId}", id);
                 // Optional: log failure to AuditLog as well
                   var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                   
                    if (HttpContext?.Request?.Headers.ContainsKey("X-Forwarded-For") == true)
                    {
                        ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    }
                   await _auditLogDBService.LogAsync(
                    actionType: "PostUpdateFailed",
                    userId: GetCurrentUserId(),
                    details: new { ErrorMessage = ex.Message },
                    correlationId: Guid.NewGuid().ToString(),
                    ipAddress: ipAddress
                );
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Delete a post
        /// ABAC: Requires user to own the post OR be Operation/Admin/Moderator
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "OperationOrAdminOrOwnerOfPost")]
        public async Task<ActionResult> DeletePost(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _postService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"Post with ID {id} not found");
                }
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                 
                if (HttpContext?.Request?.Headers.ContainsKey("X-Forwarded-For") == true)
                {
                    ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                }
                 // Log the action to the database
                 await _auditLogDBService.LogAsync(
                    actionType: "PostDeleted",
                    userId: GetCurrentUserId(),                   // Or from claims: Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                    resourceId: id.ToString(),
                    details: null,
                    correlationId: Guid.NewGuid().ToString(),
                    ipAddress: ipAddress
                );
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting post {PostId}", id);
                 // Optional: log failure to AuditLog as well
                   var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                   
                    if (HttpContext?.Request?.Headers.ContainsKey("X-Forwarded-For") == true)
                    {
                        ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    }
                   await _auditLogDBService.LogAsync(
                    actionType: "PostUpdateFailed",
                    userId: GetCurrentUserId(),
                    details: new { ErrorMessage = ex.Message },
                    correlationId: Guid.NewGuid().ToString(),
                    ipAddress: ipAddress
                );
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

        /// <summary>
        /// Report a post
        /// Users cannot report their own posts
        /// </summary>
        [HttpPost("{id}/report")]
        public async Task<ActionResult> ReportPost(Guid id, [FromBody] ReportPostRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User ID not found in token");
                }

                var reported = await _postReportService.ReportPostAsync(id, currentUserId.Value, request.Reason);
                if (!reported)
                {
                    return BadRequest("Post has already been reported by this user or you cannot report your own post");
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                if (HttpContext?.Request?.Headers.ContainsKey("X-Forwarded-For") == true)
                {
                    ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                }

                await _auditLogDBService.LogAsync(
                    actionType: "PostReported",
                    userId: currentUserId,
                    resourceId: id.ToString(),
                    details: new { Reason = request.Reason },
                    correlationId: Guid.NewGuid().ToString(),
                    ipAddress: ipAddress
                );

                return Ok(new { message = "Post reported successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Post not found for report: {PostId}", id);
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid report attempt: {PostId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reporting post {PostId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get all reported posts (Operation role only)
        /// Returns posts with their report counts
        /// </summary>
        [HttpGet("reported")]
        [Authorize(Policy = "OperationOrAdmin")]
        public async Task<ActionResult<IEnumerable<ReportedPostDto>>> GetReportedPosts(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting reported posts");
                var reportedPosts = await _postReportService.GetReportedPostsAsync();
                return Ok(reportedPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting reported posts");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get posts by tag ID
        /// </summary>
        [HttpGet("tag/{tagId}")]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPostsByTag(Guid tagId, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting posts for tag {TagId}", tagId);
                var currentUserId = GetCurrentUserId();
                var posts = await _postService.GetByTagIdAsync(tagId, currentUserId);
                
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
                _logger.LogError(ex, "Error while getting posts for tag {TagId}", tagId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
