// using EchoSpace.Core.DTOs;
// using EchoSpace.Core.Interfaces;
// using Microsoft.Extensions.Options;
// using System.Net;
// using System.Net.Http;
// using System.Web;

// namespace EchoSpace.Infra.Services
// {
//     public class PollinationsOptions
//     {
//         public string BaseUrl { get; set; } = "https://image.pollinations.ai";
//     }

//     public class PollinationsProvider : IAiImageGenerationService // we implement minimal image via GenerateImageAsync, other methods forward to Gemini or throw
//     {
//         private readonly HttpClient _client;
//         private readonly PollinationsOptions _opts;
//         private readonly GeminiService? _gemini;

//         public PollinationsProvider(HttpClient client, IOptions<PollinationsOptions> opts, GeminiService? gemini = null)
//         {
//             _client = client;
//             _opts = opts.Value;
//             _gemini = gemini;
//         }

//         // Tag/translate/summarize forward to Gemini if available
//         public async Task<ImageResultDto> GenerateImageAsync(string prompt)
//         {
//             var encoded = HttpUtility.UrlEncode(prompt);
//             var url = $"{_opts.BaseUrl.TrimEnd('/')}/prompt/{encoded}";
//             // Pollinations supports just returning an image directly. We'll probe the URL to confirm availability and return the final resource URL.
//             // The API returns an image binary; we can also return the GET URL for simple usage.
//             // Optionally, you can fetch the image to test availability:
//             HttpResponseMessage response = await _client.GetAsync(url);
//             response.EnsureSuccessStatusCode(); // throws if not 2xx
//             byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
//             // If Pollinations redirects to the final image, capture the final URI
//             // var finalUri = response.RequestMessage?.RequestUri?.ToString() ?? url;
//             // return new ImageResultDto(finalUri);
//         }
//     }
// }
