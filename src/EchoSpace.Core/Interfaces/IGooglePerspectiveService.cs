using System.Threading.Tasks;

namespace EchoSpace.Core.Interfaces
{
    public interface IGooglePerspectiveService
    {
        Task<double> GetToxicityScoreAsync(string text);
    }
}
