namespace EchoSpace.Core.Interfaces
{
    /// <summary>
    /// Service interface for Azure Blob Storage operations
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Upload a file to blob storage
        /// </summary>
        /// <param name="containerName">Container name (e.g., "images", "uploads")</param>
        /// <param name="blobName">Name of the blob (typically GUID as string)</param>
        /// <param name="content">File content as byte array</param>
        /// <param name="contentType">MIME type of the file</param>
        /// <returns>URL to the uploaded blob</returns>
        Task<string> UploadBlobAsync(string containerName, string blobName, byte[] content, string contentType);
        
        /// <summary>
        /// Download a blob from blob storage
        /// </summary>
        /// <param name="containerName">Container name</param>
        /// <param name="blobName">Name of the blob</param>
        /// <returns>File content as byte array, or null if not found</returns>
        Task<byte[]?> DownloadBlobAsync(string containerName, string blobName);
        
        /// <summary>
        /// Delete a blob from blob storage
        /// </summary>
        /// <param name="containerName">Container name</param>
        /// <param name="blobName">Name of the blob</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteBlobAsync(string containerName, string blobName);
        
        /// <summary>
        /// Check if a blob exists
        /// </summary>
        /// <param name="containerName">Container name</param>
        /// <param name="blobName">Name of the blob</param>
        /// <returns>True if exists, false otherwise</returns>
        Task<bool> BlobExistsAsync(string containerName, string blobName);
        
        /// <summary>
        /// Get a URL to access the blob (with SAS token if needed)
        /// </summary>
        /// <param name="containerName">Container name</param>
        /// <param name="blobName">Name of the blob</param>
        /// <param name="expiryMinutes">Expiry time for the URL in minutes (default 60)</param>
        /// <returns>URL to access the blob</returns>
        Task<string> GetBlobUrlAsync(string containerName, string blobName, int expiryMinutes = 60);
        
        /// <summary>
        /// Ensure a container exists, create it if it doesn't
        /// </summary>
        /// <param name="containerName">Container name</param>
        Task EnsureContainerExistsAsync(string containerName);
    }
}

