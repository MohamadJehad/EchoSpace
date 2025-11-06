using EchoSpace.Core.Entities;
using EchoSpace.Core.Enums;

namespace EchoSpace.Core.Interfaces
{
    /// <summary>
    /// Repository interface for image data access operations
    /// </summary>
    public interface IImageRepository
    {
        /// <summary>
        /// Get an image by its unique identifier
        /// </summary>
        Task<Image?> GetByIdAsync(Guid imageId);
        
        /// <summary>
        /// Get all images for a specific user
        /// </summary>
        Task<IEnumerable<Image>> GetByUserIdAsync(Guid userId);
        
        /// <summary>
        /// Get images by source type
        /// </summary>
        Task<IEnumerable<Image>> GetBySourceAsync(ImageSource source);
        
        /// <summary>
        /// Get images by post ID
        /// </summary>
        Task<IEnumerable<Image>> GetByPostIdAsync(Guid postId);
        
        /// <summary>
        /// Get an image by blob name
        /// </summary>
        Task<Image?> GetByBlobNameAsync(string blobName, string containerName);
        
        /// <summary>
        /// Add a new image record
        /// </summary>
        Task<Image> AddAsync(Image image);
        
        /// <summary>
        /// Update an existing image record
        /// </summary>
        Task<Image?> UpdateAsync(Image image);
        
        /// <summary>
        /// Delete an image record
        /// </summary>
        Task<bool> DeleteAsync(Guid imageId);
        
        /// <summary>
        /// Check if an image exists
        /// </summary>
        Task<bool> ExistsAsync(Guid imageId);
        
        /// <summary>
        /// Get all images
        /// </summary>
        Task<IEnumerable<Image>> GetAllAsync();
    }
}

