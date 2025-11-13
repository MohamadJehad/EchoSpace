using System.Threading.Tasks;
using EchoSpace.Core.DTOs;

namespace EchoSpace.Core.Interfaces
{
    public interface IAiImageGenerationService
    {
        Task<ImageResultDto> GenerateImageAsync(string prompt);
    }
}
