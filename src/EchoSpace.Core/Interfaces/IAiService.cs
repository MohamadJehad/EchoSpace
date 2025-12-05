namespace EchoSpace.Core.Interfaces
{
    public interface IAiService
    {
        Task<string> TranslateTextAsync(string text, string language);
        Task<string> SummarizeTextAsync(string text);
    }
}
