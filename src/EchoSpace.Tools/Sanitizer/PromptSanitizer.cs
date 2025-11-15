using System.Text.Json;

namespace EchoSpace.Tools.Sanitizer
{
    public static class PromptSanitizer
    {
        public static string Clean(string input)
        {
            var text = input.Replace("<", "").Replace(">", "").Trim();
            return JsonEncodedText.Encode(text).ToString();
        }
    }
}
