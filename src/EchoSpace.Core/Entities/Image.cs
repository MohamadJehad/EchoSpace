using EchoSpace.Core.Enums;

namespace EchoSpace.Core.Entities
{
    /// <summary>
    /// Entity representing an image stored in the system
    /// </summary>
    public class Image
    {
        /// <summary>
        /// Unique identifier for the image (GUID)
        /// </summary>
        public Guid ImageId { get; set; }
        
        /// <summary>
        /// Source of the image (UserUpload, AIGenerated, etc.)
        /// </summary>
        public ImageSource Source { get; set; }
        
        /// <summary>
        /// Original filename when uploaded
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;
        
        /// <summary>
        /// MIME type of the image (e.g., image/jpeg, image/png)
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
        
        /// <summary>
        /// Size of the image in bytes
        /// </summary>
        public long SizeInBytes { get; set; }
        
        /// <summary>
        /// Name of the blob in Azure Blob Storage (typically the ImageId as string)
        /// </summary>
        public string BlobName { get; set; } = string.Empty;
        
        /// <summary>
        /// Container name in blob storage (e.g., "images", "uploads")
        /// </summary>
        public string ContainerName { get; set; } = string.Empty;
        
        /// <summary>
        /// User ID who uploaded/owns the image (nullable for system/AI images)
        /// </summary>
        public Guid? UserId { get; set; }
        
        /// <summary>
        /// Post ID associated with this image (optional)
        /// </summary>
        public Guid? PostId { get; set; }
        
        /// <summary>
        /// URL to access the image (can be generated or stored)
        /// </summary>
        public string? Url { get; set; }
        
        /// <summary>
        /// Optional description or metadata
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Date and time when the image was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Navigation property to the user who owns the image
        /// </summary>
        public User? User { get; set; }
        
        /// <summary>
        /// Navigation property to the post this image belongs to
        /// </summary>
        public Post? Post { get; set; }
    }
}

