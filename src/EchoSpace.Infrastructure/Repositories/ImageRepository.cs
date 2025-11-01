using EchoSpace.Core.Entities;
using EchoSpace.Core.Enums;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for image data access
    /// </summary>
    public class ImageRepository : IImageRepository
    {
        private readonly EchoSpaceDbContext _dbContext;

        public ImageRepository(EchoSpaceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Image?> GetByIdAsync(Guid imageId)
        {
            return await _dbContext.Images
                .Include(i => i.User)
                .Include(i => i.Post)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.ImageId == imageId);
        }

        public async Task<IEnumerable<Image>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.Images
                .Include(i => i.User)
                .Include(i => i.Post)
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Image>> GetBySourceAsync(ImageSource source)
        {
            return await _dbContext.Images
                .Include(i => i.User)
                .Include(i => i.Post)
                .AsNoTracking()
                .Where(i => i.Source == source)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Image>> GetByPostIdAsync(Guid postId)
        {
            return await _dbContext.Images
                .Include(i => i.User)
                .Include(i => i.Post)
                .AsNoTracking()
                .Where(i => i.PostId == postId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<Image?> GetByBlobNameAsync(string blobName, string containerName)
        {
            return await _dbContext.Images
                .Include(i => i.User)
                .Include(i => i.Post)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.BlobName == blobName && i.ContainerName == containerName);
        }

        public async Task<Image> AddAsync(Image image)
        {
            _dbContext.Images.Add(image);
            await _dbContext.SaveChangesAsync();
            return image;
        }

        public async Task<Image?> UpdateAsync(Image image)
        {
            var existing = await _dbContext.Images.FindAsync(image.ImageId);
            if (existing == null)
            {
                return null;
            }

            existing.Source = image.Source;
            existing.OriginalFileName = image.OriginalFileName;
            existing.ContentType = image.ContentType;
            existing.SizeInBytes = image.SizeInBytes;
            existing.PostId = image.PostId;
            existing.Description = image.Description;
            existing.Url = image.Url;

            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid imageId)
        {
            var image = await _dbContext.Images.FindAsync(imageId);
            if (image == null)
            {
                return false;
            }

            _dbContext.Images.Remove(image);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid imageId)
        {
            return await _dbContext.Images.AnyAsync(i => i.ImageId == imageId);
        }

        public async Task<IEnumerable<Image>> GetAllAsync()
        {
            return await _dbContext.Images
                .Include(i => i.User)
                .Include(i => i.Post)
                .AsNoTracking()
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
    }
}

