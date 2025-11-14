using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.DTOs.Images;
using EchoSpace.Core.Enums;
using System.Security.Claims;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;
        private readonly IImageService _imageService;

        public UsersController(
            ILogger<UsersController> logger, 
            IUserService userService,
            IImageService imageService)
        {
            _logger = logger;
            _userService = userService;
            _imageService = imageService;
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }


        /// <summary>
        /// Get all users (Operation or Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "OperationOrAdmin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all users");
                var users = await _userService.GetAllAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting all users");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Get a specific user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting user {UserId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var created = await _userService.CreateAsync(request);
                return CreatedAtAction(nameof(GetUser), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating user {UserName}", request?.Name);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<User>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var user = await _userService.UpdateAsync(id, request);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating user {UserId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var deleted = await _userService.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound($"User with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting user {UserId}", id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        /// <summary>
        /// Upload profile photo for current user
        /// </summary>
        [HttpPost("me/profile-photo")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<ActionResult<object>> UploadProfilePhoto(IFormFile file)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "File is required" });
                }

                // Upload image
                var uploadRequest = new UploadImageRequest
                {
                    File = file,
                    Source = ImageSource.UserUpload,
                    UserId = currentUserId,
                    Description = "Profile photo"
                };

                var imageDto = await _imageService.UploadImageAsync(uploadRequest);

                // Update user's profile photo
                var user = await _userService.UpdateProfilePhotoAsync(currentUserId.Value, imageDto.ImageId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Get image URL
                var imageUrl = await _imageService.GetImageUrlAsync(imageDto.ImageId, 60);

                _logger.LogInformation("Profile photo uploaded for user {UserId}, image {ImageId}", currentUserId, imageDto.ImageId);

                return Ok(new 
                { 
                    message = "Profile photo uploaded successfully",
                    imageId = imageDto.ImageId,
                    imageUrl = imageUrl
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid profile photo upload");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile photo");
                return StatusCode(500, new { message = "An error occurred while uploading the profile photo" });
            }
        }

        /// <summary>
        /// Remove profile photo for current user
        /// </summary>
        [HttpDelete("me/profile-photo")]
        [Authorize]
        public async Task<ActionResult> RemoveProfilePhoto()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                var user = await _userService.UpdateProfilePhotoAsync(currentUserId.Value, null);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                _logger.LogInformation("Profile photo removed for user {UserId}", currentUserId);

                return Ok(new { message = "Profile photo removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing profile photo");
                return StatusCode(500, new { message = "An error occurred while removing the profile photo" });
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                var user = await _userService.GetByIdAsync(currentUserId.Value);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An error occurred while retrieving user" });
            }
        }

        /// <summary>
        /// Lock a user account (Operation or Admin only)
        /// </summary>
        [HttpPost("{id}/lock")]
        [Authorize(Policy = "OperationOrAdmin")]
        public async Task<ActionResult<User>> LockUser(Guid id)
        {
            try
            {
                var user = await _userService.LockUserAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                _logger.LogInformation("User {UserId} locked by admin", id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while locking the user" });
            }
        }

        /// <summary>
        /// Unlock a user account (Operation or Admin only)
        /// </summary>
        [HttpPost("{id}/unlock")]
        [Authorize(Policy = "OperationOrAdmin")]
        public async Task<ActionResult<User>> UnlockUser(Guid id)
        {
            try
            {
                var user = await _userService.UnlockUserAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"User with ID {id} not found" });
                }

                _logger.LogInformation("User {UserId} unlocked by admin", id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user {UserId}", id);
                return StatusCode(500, new { message = "An error occurred while unlocking the user" });
            }
        }
    }
}
