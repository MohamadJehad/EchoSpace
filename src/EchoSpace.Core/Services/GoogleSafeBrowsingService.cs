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

            var request = new RestRequest();
            request.AddJsonBody(body);

            var response = await client.PostAsync(request);
            if (response.Content == null)
                return true;

            dynamic result = JsonConvert.DeserializeObject(response.Content);
            return result?.matches == null;
        }
    }
}
