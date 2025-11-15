using System.Threading.Tasks;

namespace EchoSpace.Core.Interfaces
{
    public interface IGoogleSafeBrowsingService
    {
        Task<bool> IsUrlSafeAsync(string url);
    }
}
