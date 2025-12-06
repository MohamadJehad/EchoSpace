using System.Threading.Tasks;
using EchoSpace.Core.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace EchoSpace.Core.Services
{
    public class GoogleSafeBrowsingService : IGoogleSafeBrowsingService
    {
        private readonly string _apiKey;

        public GoogleSafeBrowsingService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<bool> IsUrlSafeAsync(string url)
        {
            var client = new RestClient($"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={_apiKey}");

            var body = new
            {
                client = new { clientId = "EchoSpaceApp", clientVersion = "1.0" },
                threatInfo = new
                {
                    threatTypes = new[] { "MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION" },
                    platformTypes = new[] { "ANY_PLATFORM" },
                    threatEntryTypes = new[] { "URL" },
                    threatEntries = new[] { new { url } }
                }
            };
            Console.WriteLine("Checking URL safety via SafeBrowsing API:");
            Console.WriteLine($"URL to check: {url}");  

            var request = new RestRequest();
            request.AddJsonBody(body);
            Console.WriteLine("SafeBrowsing API Request Body:");
            Console.WriteLine(JsonConvert.SerializeObject(body, Formatting.Indented));
            var response = await client.PostAsync(request);
            Console.WriteLine("SafeBrowsing API Response:");
            Console.WriteLine(response.Content);
            
            if (string.IsNullOrWhiteSpace(response.Content))
                return true;

            dynamic? result = JsonConvert.DeserializeObject(response.Content);
            return result?.matches == null;
        }
    }
}
