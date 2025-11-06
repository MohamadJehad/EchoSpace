using EchoSpace.Core.DTOs.Images;
using EchoSpace.Core.Enums;

namespace EchoSpace.Core.Interfaces
{
    /// <summary>
    /// Service interface for image business logic
    /// </summary>
    public interface IImageService
    {
        /// <summary>
        /// Upload an image file and create image record
        /// </summary>
        Task<ImageDto> UploadImageAsync(UploadImageRequest request);
        
        /// <summary>
        /// Get image by ID with URL
        /// </summary>
        Task<ImageDto?> GetImageAsync(Guid imageId);
        
        /// <summary>
        /// Get all images for a user
        /// </summary>
        Task<IEnumerable<ImageDto>> GetUserImagesAsync(Guid userId);
        
        /// <summary>
        /// Get images by source
        /// </summary>
        Task<IEnumerable<ImageDto>> GetImagesBySourceAsync(ImageSource source);
        
        /// <summary>
        /// Get images by post ID
        /// </summary>
        Task<IEnumerable<ImageDto>> GetImagesByPostIdAsync(Guid postId);
        
        /// <summary>
        /// Delete an image (both database record and blob)
        /// </summary>
        Task<bool> DeleteImageAsync(Guid imageId);
        
        /// <summary>
        /// Get image download URL (with SAS token)
        /// </summary>
        Task<string?> GetImageUrlAsync(Guid imageId, int expiryMinutes = 60);
    }
}

