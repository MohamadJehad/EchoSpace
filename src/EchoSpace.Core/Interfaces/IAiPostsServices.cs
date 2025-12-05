using System.Threading.Tasks;
using EchoSpace.Core.DTOs;

namespace EchoSpace.Core.Interfaces
{
    public interface IAiPostsService
    {
        Task<TagResultDto> TagAsync(string text);
        Task<TranslateResultDto> TranslateAsync(string text, string targetLanguage);
        Task<SummarizeResultDto> SummarizeAsync(string text, int maxTokens = 256);
    }
}
