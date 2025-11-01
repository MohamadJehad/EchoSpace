using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using EchoSpace.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EchoSpace.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for Azure Blob Storage operations
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("AzureStorage connection string is not configured");
            }

            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger = logger;
        }

        public async Task<string> UploadBlobAsync(string containerName, string blobName, byte[] content, string contentType)
        {
            try
            {
                await EnsureContainerExistsAsync(containerName);
                
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                using var stream = new MemoryStream(content);
                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                };

                await blobClient.UploadAsync(stream, uploadOptions);
                
                _logger.LogInformation("Uploaded blob {BlobName} to container {ContainerName}", blobName, containerName);
                
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading blob {BlobName} to container {ContainerName}", blobName, containerName);
                throw;
            }
        }

        public async Task<byte[]?> DownloadBlobAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogWarning("Blob {BlobName} not found in container {ContainerName}", blobName, containerName);
                    return null;
                }

                using var memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);
                
                _logger.LogInformation("Downloaded blob {BlobName} from container {ContainerName}", blobName, containerName);
                
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading blob {BlobName} from container {ContainerName}", blobName, containerName);
                throw;
            }
        }

        public async Task<bool> DeleteBlobAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                var result = await blobClient.DeleteIfExistsAsync();
                
                if (result.Value)
                {
                    _logger.LogInformation("Deleted blob {BlobName} from container {ContainerName}", blobName, containerName);
                }
                
                return result.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting blob {BlobName} from container {ContainerName}", blobName, containerName);
                throw;
            }
        }

        public async Task<bool> BlobExistsAsync(string containerName, string blobName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                
                return await blobClient.ExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of blob {BlobName} in container {ContainerName}", blobName, containerName);
                return false;
            }
        }

        public async Task<string> GetBlobUrlAsync(string containerName, string blobName, int expiryMinutes = 60)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    throw new FileNotFoundException($"Blob {blobName} not found in container {containerName}");
                }

                // Generate SAS token for secure access
                if (blobClient.CanGenerateSasUri)
                {
                    var sasBuilder = new BlobSasBuilder
                    {
                        BlobContainerName = containerName,
                        BlobName = blobName,
                        Resource = "b",
                        ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
                    };

                    sasBuilder.SetPermissions(BlobSasPermissions.Read);

                    var sasUri = blobClient.GenerateSasUri(sasBuilder);
                    return sasUri.ToString();
                }

                // If SAS generation is not available, return public URL (if container allows public access)
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating URL for blob {BlobName} in container {ContainerName}", blobName, containerName);
                throw;
            }
        }

        public async Task EnsureContainerExistsAsync(string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
                
                _logger.LogDebug("Ensured container {ContainerName} exists", containerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring container {ContainerName} exists", containerName);
                throw;
            }
        }
    }
}

