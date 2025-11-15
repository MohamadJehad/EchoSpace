using EchoSpace.Core.DTOs.Tags;

namespace EchoSpace.Core.Interfaces
{
    public interface ITagService
    {
        Task<IEnumerable<TagDto>> GetAllAsync();
        Task<TagDto?> GetByIdAsync(Guid id);
        Task<TagDto> CreateAsync(CreateTagRequest request);
        Task<TagDto?> UpdateAsync(Guid id, UpdateTagRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task InitializeDefaultTagsAsync();
        Task<IEnumerable<TrendingTagDto>> GetTrendingAsync(int count = 10);
    }
}

