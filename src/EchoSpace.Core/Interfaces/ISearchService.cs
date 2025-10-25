using EchoSpace.Core.DTOs;

namespace EchoSpace.Core.Interfaces
{
    public interface ISearchService
    {
        Task<IEnumerable<SearchResultDto>> SearchUsersAsync(string query, int limit = 10);
    }
}

