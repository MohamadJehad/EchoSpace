using EchoSpace.Core.DTOs;
using EchoSpace.Core.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace EchoSpace.Infrastructure.Services
{
    public class PollinationsOptions
    {
        public string BaseUrl { get; set; } = "https://image.pollinations.ai";
    }

    public class PollinationsProvider : IAiImageGenerationService
    {
        private readonly HttpClient _client;
        private readonly PollinationsOptions _opts;

        public PollinationsProvider(HttpClient client, IOptions<PollinationsOptions> opts)
        {
            _client = client;
            _opts = opts.Value;
        }

        public async Task<ImageResultDto> GenerateImageAsync(string prompt)
        {
            // Pollinations API: https://image.pollinations.ai/prompt/{prompt}
            // Returns image binary directly
            var encoded = Uri.EscapeDataString(prompt);
            var url = $"{_opts.BaseUrl.TrimEnd('/')}/prompt/{encoded}";
            
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            // Return the URL - the image can be accessed directly from this URL
            // We'll download it in the service layer to store in blob storage
            return new ImageResultDto(url);
        }

        public async Task<byte[]> DownloadImageBytesAsync(string imageUrl)
        {
            var response = await _client.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
