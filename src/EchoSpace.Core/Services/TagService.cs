using EchoSpace.Core.Entities;
using EchoSpace.Core.DTOs.Tags;
using EchoSpace.Core.Interfaces;

namespace EchoSpace.Core.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<IEnumerable<TagDto>> GetAllAsync()
        {
            var tags = await _tagRepository.GetAllAsync();
            return tags.Select(t => MapToDto(t));
        }

        public async Task<TagDto?> GetByIdAsync(Guid id)
        {
            var tag = await _tagRepository.GetByIdAsync(id);
            return tag == null ? null : MapToDto(tag);
        }

        public async Task<TagDto> CreateAsync(CreateTagRequest request)
        {
            var tag = new Tag
            {
                TagId = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Color = request.Color,
                CreatedAt = DateTime.UtcNow
            };

            var createdTag = await _tagRepository.AddAsync(tag);
            return MapToDto(createdTag);
        }

        public async Task<TagDto?> UpdateAsync(Guid id, UpdateTagRequest request)
        {
            var existing = await _tagRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                existing.Name = request.Name;
            }
            if (request.Description != null)
            {
                existing.Description = request.Description;
            }
            if (request.Color != null)
            {
                existing.Color = request.Color;
            }

            var updatedTag = await _tagRepository.UpdateAsync(existing);
            return updatedTag == null ? null : MapToDto(updatedTag);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _tagRepository.DeleteAsync(id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _tagRepository.ExistsAsync(id);
        }

        public async Task InitializeDefaultTagsAsync()
        {
            var defaultTags = new[]
            {
                new { Name = "General", Description = "General discussions", Color = "#6B7280" },
                new { Name = "Technology", Description = "Tech news and discussions", Color = "#3B82F6" },
                new { Name = "Science", Description = "Scientific discoveries and research", Color = "#10B981" },
                new { Name = "Art", Description = "Artistic creations and discussions", Color = "#F59E0B" },
                new { Name = "Music", Description = "Music, songs, and artists", Color = "#8B5CF6" },
                new { Name = "Sports", Description = "Sports news and updates", Color = "#EF4444" },
                new { Name = "Food", Description = "Food recipes and culinary discussions", Color = "#F97316" },
                new { Name = "Travel", Description = "Travel experiences and tips", Color = "#06B6D4" },
                new { Name = "Fashion", Description = "Fashion trends and style", Color = "#EC4899" },
                new { Name = "Gaming", Description = "Video games and gaming culture", Color = "#6366F1" },
                new { Name = "Books", Description = "Books and literature", Color = "#14B8A6" },
                new { Name = "Movies", Description = "Movies and cinema", Color = "#DC2626" },
                new { Name = "Health", Description = "Health and wellness", Color = "#22C55E" },
                new { Name = "Education", Description = "Educational content and learning", Color = "#2563EB" },
                new { Name = "Business", Description = "Business and entrepreneurship", Color = "#7C3AED" }
            };

            foreach (var tagData in defaultTags)
            {
                var existingTag = await _tagRepository.GetByNameAsync(tagData.Name);
                if (existingTag == null)
                {
                    var tag = new Tag
                    {
                        TagId = Guid.NewGuid(),
                        Name = tagData.Name,
                        Description = tagData.Description,
                        Color = tagData.Color,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _tagRepository.AddAsync(tag);
                }
            }
        }

        private TagDto MapToDto(Tag tag)
        {
            return new TagDto
            {
                TagId = tag.TagId,
                Name = tag.Name,
                Description = tag.Description,
                Color = tag.Color,
                CreatedAt = tag.CreatedAt
            };
        }
    }
}

