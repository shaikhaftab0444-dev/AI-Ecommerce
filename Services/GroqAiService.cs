using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BrandsStore.Services
{
    public class GroqAiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GroqAiService> _logger;

        public GroqAiService(IConfiguration config, HttpClient httpClient, ILogger<GroqAiService> logger)
        {
            _apiKey = config["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq API Key is missing in appsettings.json");
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> GenerateProductDescriptionAsync(string productName, string categoryName)
        {
            try
            {
                var prompt = $"Write a highly engaging, SEO-friendly e-commerce product description for a product named '{productName}' in the '{categoryName}' category. Keep it professional, under 4 sentences. Do not include any introductory text.";
                var requestUrl = "https://api.groq.com/openai/v1/chat/completions";

                var payload = new
                {
                    model = "openai/gpt-oss-20b",
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = 0.7
                };

                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.Add("User-Agent", "BrandsStore/1.0");
                request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(responseText);
                    var generatedText = doc.RootElement.GetProperty("choices")[0]
                                           .GetProperty("message")
                                           .GetProperty("content").GetString();
                    return generatedText?.Trim() ?? "Could not extract text.";
                }
                
                _logger.LogError($"Groq API Failed: {response.StatusCode} - {responseText}");
                return $"API Error ({response.StatusCode}): {responseText}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in Groq API call");
                return $"Exception: {ex.Message}";
            }
        }
    }
}
