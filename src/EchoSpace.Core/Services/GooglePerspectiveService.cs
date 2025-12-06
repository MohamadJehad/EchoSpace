using System.Threading.Tasks;
using EchoSpace.Core.Interfaces;
using Newtonsoft.Json;
using RestSharp;

namespace EchoSpace.Core.Services
{
    public class GooglePerspectiveService : IGooglePerspectiveService
    {
        private readonly string _apiKey;

        public GooglePerspectiveService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<double> GetToxicityScoreAsync(string text)
{
    // Correct endpoint
    string url = $"https://commentanalyzer.googleapis.com/v1alpha1/comments:analyze?key={_apiKey}";
    Console.WriteLine("Full REST Endpoint:");
    Console.WriteLine(url);

    // Build request body
    var body = new
    {
        comment = new { text },
        languages = new[] { "en" },
        requestedAttributes = new { TOXICITY = new { } }
    };
    string jsonBody = JsonConvert.SerializeObject(body, Formatting.Indented);

    // Print request body
    Console.WriteLine("Request Body JSON:");
    Console.WriteLine(jsonBody);

    // Create RestSharp client and request
    var client = new RestClient(url);
    var request = new RestRequest();
    request.AddStringBody(jsonBody, DataFormat.Json);
    request.AddHeader("Content-Type", "application/json");
    // Send request
    RestResponse response;
    try
    {
        response = await client.PostAsync(request);
    }
    catch (Exception ex)
    {
        // Network / transport level exception
        throw new Exception($"Failed to call Perspective API: {ex.Message}", ex);
    }

    // Check HTTP response
    if (!response.IsSuccessful)
    {
        string content = response.Content ?? "<empty>";
        // Print and throw a detailed error
        Console.WriteLine("Perspective API returned an error:");
        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine("Response Content: " + content);

        throw new Exception(
            $"Perspective API request failed.\n" +
            $"Status: {response.StatusCode}\n" +
            $"Response: {content}"
        );
    }

    // Parse and return the toxicity score
    if (string.IsNullOrWhiteSpace(response.Content))
    {
        Console.WriteLine("Warning: Response content is null or empty");
        return 0.0;
    }
    
    dynamic result = JsonConvert.DeserializeObject(response.Content);
    double score = (double?)result?.attributeScores?.TOXICITY?.summaryScore?.value ?? 0.0;

    Console.WriteLine("Toxicity Score: " + score);
    return score;
}

    }
}
