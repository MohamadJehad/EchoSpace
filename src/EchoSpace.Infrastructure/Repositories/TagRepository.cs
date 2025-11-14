using EchoSpace.Core.Entities;
using EchoSpace.Core.Interfaces;
using EchoSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EchoSpace.Infrastructure.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly EchoSpaceDbContext _dbContext;

        public TagRepository(EchoSpaceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Tag>> GetAllAsync()
        {
            return await _dbContext.Tags
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Tag?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TagId == id);
        }

        public async Task<Tag?> GetByNameAsync(string name)
        {
            return await _dbContext.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
        }

        public async Task<Tag> AddAsync(Tag tag)
        {
            _dbContext.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();
            return tag;
        }

        public async Task<Tag?> UpdateAsync(Tag tag)
        {
            var existing = await _dbContext.Tags.FirstOrDefaultAsync(t => t.TagId == tag.TagId);
            if (existing == null)
            {
                return null;
            }

            existing.Name = tag.Name;
            existing.Description = tag.Description;
            existing.Color = tag.Color;

            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _dbContext.Tags.FirstOrDefaultAsync(t => t.TagId == id);
            if (existing == null)
            {
                return false;
            }

            _dbContext.Tags.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbContext.Tags.AnyAsync(t => t.TagId == id);
        }
    }
}

