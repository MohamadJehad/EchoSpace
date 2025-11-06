using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using EchoSpace.Core.DTOs.Images;
using EchoSpace.Core.Interfaces;
using EchoSpace.Core.Enums;
using System.Security.Claims;

namespace EchoSpace.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("GeneralApiPolicy")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(IImageService imageService, ILogger<ImagesController> logger)
        {
            _imageService = imageService;
            _logger = logger;
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
        /// Upload an image
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ImageDto>> UploadImage(UploadImageRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                // Set user ID from authenticated user
                request.UserId = currentUserId;

                var imageDto = await _imageService.UploadImageAsync(request);
                
                _logger.LogInformation("Image uploaded: {ImageId} by user {UserId}", imageDto.ImageId, currentUserId);
                
                return Ok(imageDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid image upload request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new { message = "An error occurred while uploading the image" });
            }
        }

        /// <summary>
        /// Upload an AI-generated image
        /// </summary>
        [HttpPost("upload-ai")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ImageDto>> UploadAIImage(UploadImageRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                request.Source = ImageSource.AIGenerated;
                request.UserId = currentUserId;

                var imageDto = await _imageService.UploadImageAsync(request);
                
                _logger.LogInformation("AI image uploaded: {ImageId} by user {UserId}", imageDto.ImageId, currentUserId);
                
                return Ok(imageDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading AI image");
                return StatusCode(500, new { message = "An error occurred while uploading the AI image" });
            }
        }

        /// <summary>
        /// Get image by ID
        /// </summary>
        [HttpGet("{imageId}")]
        public async Task<ActionResult<ImageDto>> GetImage(Guid imageId)
        {
            try
            {
                var image = await _imageService.GetImageAsync(imageId);
                if (image == null)
                {
                    return NotFound(new { message = "Image not found" });
                }

                return Ok(image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving image {ImageId}", imageId);
                return StatusCode(500, new { message = "An error occurred while retrieving the image" });
            }
        }

        /// <summary>
        /// Get all images for current user
        /// </summary>
        [HttpGet("my-images")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetMyImages()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                var images = await _imageService.GetUserImagesAsync(currentUserId.Value);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user images");
                return StatusCode(500, new { message = "An error occurred while retrieving images" });
            }
        }

        /// <summary>
        /// Get images by source
        /// </summary>
        [HttpGet("source/{source}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetImagesBySource(ImageSource source)
        {
            try
            {
                var images = await _imageService.GetImagesBySourceAsync(source);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images by source {Source}", source);
                return StatusCode(500, new { message = "An error occurred while retrieving images" });
            }
        }

        /// <summary>
        /// Get images by post ID
        /// </summary>
        [HttpGet("post/{postId}")]
        public async Task<ActionResult<IEnumerable<ImageDto>>> GetImagesByPostId(Guid postId)
        {
            try
            {
                var images = await _imageService.GetImagesByPostIdAsync(postId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images for post {PostId}", postId);
                return StatusCode(500, new { message = "An error occurred while retrieving images" });
            }
        }

        /// <summary>
        /// Get image download URL
        /// </summary>
        [HttpGet("{imageId}/url")]
        public async Task<ActionResult<object>> GetImageUrl(Guid imageId, [FromQuery] int expiryMinutes = 60)
        {
            try
            {
                var url = await _imageService.GetImageUrlAsync(imageId, expiryMinutes);
                if (url == null)
                {
                    return NotFound(new { message = "Image not found" });
                }

                return Ok(new { url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating URL for image {ImageId}", imageId);
                return StatusCode(500, new { message = "An error occurred while generating image URL" });
            }
        }

        /// <summary>
        /// Delete an image
        /// </summary>
        [HttpDelete("{imageId}")]
        public async Task<ActionResult> DeleteImage(Guid imageId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Unauthorized("User not authenticated");
                }

                // TODO: Add authorization check - only allow deletion if user owns the image or is admin
                // For now, allowing deletion if image exists
                
                var deleted = await _imageService.DeleteImageAsync(imageId);
                if (!deleted)
                {
                    return NotFound(new { message = "Image not found" });
                }

                _logger.LogInformation("Image deleted: {ImageId} by user {UserId}", imageId, currentUserId);
                
                return Ok(new { message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
                return StatusCode(500, new { message = "An error occurred while deleting the image" });
            }
        }
    }
}

