using EchoSpace.Core.DTOs.Images;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Enums;
using EchoSpace.Core.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace EchoSpace.Core.Services
{
    /// <summary>
    /// Service implementation for image business logic
    /// </summary>
    public class ImageService : IImageService
    {
        private readonly IImageRepository _imageRepository;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<ImageService> _logger;
        private const string DefaultContainer = "images";
        private readonly string[] AllowedImageTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public ImageService(
            IImageRepository imageRepository,
            IBlobStorageService blobStorageService,
            ILogger<ImageService> logger)
        {
            _imageRepository = imageRepository;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        public async Task<ImageDto> UploadImageAsync(UploadImageRequest request)
        {
            // Validate file
            if (request.File == null || request.File.Length == 0)
            {
                throw new ArgumentException("File is required and cannot be empty");
            }

            if (request.File.Length > MaxFileSize)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB");
            }

            if (!AllowedImageTypes.Contains(request.File.ContentType.ToLower()))
            {
                throw new ArgumentException($"File type {request.File.ContentType} is not allowed. Allowed types: {string.Join(", ", AllowedImageTypes)}");
            }

            // Determine container based on source
            var containerName = GetContainerForSource(request.Source);

            // Generate unique blob name (GUID)
            var imageId = Guid.NewGuid();
            var blobName = imageId.ToString();

            // Read file content
            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            // Validate actual file content (magic bytes)
            if (!ValidateImageContent(fileContent, request.File.ContentType))
            {
                throw new ArgumentException("File content does not match declared type. File may be corrupted or malicious.");
            }

            // Validate image can be decoded
            if (!ValidateImageCanBeDecoded(fileContent))
            {
                throw new ArgumentException("File is not a valid or decodable image.");
            }

            // Upload to blob storage
            var blobUrl = await _blobStorageService.UploadBlobAsync(
                containerName,
                blobName,
                fileContent,
                request.File.ContentType);

            // Create image entity
            var image = new Entities.Image
            {
                ImageId = imageId,
                Source = request.Source,
                OriginalFileName = request.File.FileName,
                ContentType = request.File.ContentType,
                SizeInBytes = request.File.Length,
                BlobName = blobName,
                ContainerName = containerName,
                UserId = request.UserId,
                PostId = request.PostId,
                Description = request.Description,
                Url = blobUrl,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            var savedImage = await _imageRepository.AddAsync(image);

            _logger.LogInformation(
                "Image uploaded: {ImageId}, Source: {Source}, Container: {Container}, Size: {Size} bytes",
                imageId, request.Source, containerName, request.File.Length);

            return MapToDto(savedImage);
        }

        public async Task<ImageDto?> GetImageAsync(Guid imageId)
        {
            var image = await _imageRepository.GetByIdAsync(imageId);
            if (image == null)
            {
                return null;
            }

            // Generate access URL with SAS token
            var url = await _blobStorageService.GetBlobUrlAsync(image.ContainerName, image.BlobName, 60);
            
            var dto = MapToDto(image);
            dto.Url = url;
            
            return dto;
        }

        public async Task<IEnumerable<ImageDto>> GetUserImagesAsync(Guid userId)
        {
            var images = await _imageRepository.GetByUserIdAsync(userId);
            return images.Select(image =>
            {
                var dto = MapToDto(image);
                // URLs will be generated on-demand when needed
                return dto;
            });
        }

        public async Task<IEnumerable<ImageDto>> GetImagesBySourceAsync(ImageSource source)
        {
            var images = await _imageRepository.GetBySourceAsync(source);
            return images.Select(MapToDto);
        }

        public async Task<IEnumerable<ImageDto>> GetImagesByPostIdAsync(Guid postId)
        {
            var images = await _imageRepository.GetByPostIdAsync(postId);
            return images.Select(MapToDto);
        }

        public async Task<bool> DeleteImageAsync(Guid imageId)
        {
            var image = await _imageRepository.GetByIdAsync(imageId);
            if (image == null)
            {
                return false;
            }

            // Delete from blob storage
            var blobDeleted = await _blobStorageService.DeleteBlobAsync(image.ContainerName, image.BlobName);
            
            // Delete from database
            var dbDeleted = await _imageRepository.DeleteAsync(imageId);

            if (blobDeleted && dbDeleted)
            {
                _logger.LogInformation("Image deleted: {ImageId}", imageId);
            }

            return blobDeleted && dbDeleted;
        }

        public async Task<string?> GetImageUrlAsync(Guid imageId, int expiryMinutes = 60)
        {
            var image = await _imageRepository.GetByIdAsync(imageId);
            if (image == null)
            {
                return null;
            }

            return await _blobStorageService.GetBlobUrlAsync(image.ContainerName, image.BlobName, expiryMinutes);
        }

        private string GetContainerForSource(ImageSource source)
        {
            return source switch
            {
                ImageSource.UserUpload => "images",
                ImageSource.AIGenerated => "ai-images",
                ImageSource.SystemGenerated => "system-images",
                ImageSource.ExternalImport => "imported-images",
                _ => "other-images"
            };
        }

        private ImageDto MapToDto(Entities.Image image)
        {
            return new ImageDto
            {
                ImageId = image.ImageId,
                Source = image.Source,
                SourceName = image.Source.ToString(),
                OriginalFileName = image.OriginalFileName,
                ContentType = image.ContentType,
                SizeInBytes = image.SizeInBytes,
                Url = image.Url,
                UserId = image.UserId,
                PostId = image.PostId,
                Description = image.Description,
                CreatedAt = image.CreatedAt
            };
        }

        /// <summary>
        /// Validates image content by checking magic bytes (file signature)
        /// This prevents spoofing where a malicious file is renamed with .jpg extension
        /// </summary>
        private bool ValidateImageContent(byte[] fileBytes, string contentType)
        {
            if (fileBytes == null || fileBytes.Length < 4)
            {
                return false;
            }

            // Check magic bytes (file signature) - this is the actual file type, not spoofable
            var signature = fileBytes.Take(4).ToArray();
            
            return contentType.ToLower() switch
            {
                "image/jpeg" or "image/jpg" => 
                    signature[0] == 0xFF && signature[1] == 0xD8, // JPEG signature: FF D8
                
                "image/png" => 
                    signature[0] == 0x89 && signature[1] == 0x50 && 
                    signature[2] == 0x4E && signature[3] == 0x47, // PNG signature: 89 50 4E 47
                
                "image/gif" => 
                    signature[0] == 0x47 && signature[1] == 0x49 && signature[2] == 0x46, // GIF signature: GIF
                
                "image/webp" => 
                    fileBytes.Length >= 12 &&
                    signature[0] == 0x52 && signature[1] == 0x49 && 
                    signature[2] == 0x46 && signature[3] == 0x46 && // WebP signature: RIFF
                    fileBytes[8] == 0x57 && fileBytes[9] == 0x45 && 
                    fileBytes[10] == 0x42 && fileBytes[11] == 0x50, // WEBP
                
                _ => false
            };
        }

        /// <summary>
        /// Validates that the image can actually be decoded by ImageSharp
        /// This ensures the file is a valid, non-corrupted image
        /// </summary>
        private bool ValidateImageCanBeDecoded(byte[] fileBytes)
        {
            try
            {
                using var image = ImageSharpImage.Load(fileBytes);
                return image != null; // If it can be decoded, it's a valid image
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Image validation failed: {Error}", ex.Message);
                return false; // Invalid or corrupted image
            }
        }
    }
}

