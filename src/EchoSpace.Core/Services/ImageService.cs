using EchoSpace.Core.DTOs.Images;
using EchoSpace.Core.Entities;
using EchoSpace.Core.Enums;
using EchoSpace.Core.Interfaces;
using Microsoft.Extensions.Logging;

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

            // Upload to blob storage
            var blobUrl = await _blobStorageService.UploadBlobAsync(
                containerName,
                blobName,
                fileContent,
                request.File.ContentType);

            // Create image entity
            var image = new Image
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

        private ImageDto MapToDto(Image image)
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
    }
}

