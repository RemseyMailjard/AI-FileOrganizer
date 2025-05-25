// AI_FileOrganizer/Services/GeminiAiProvider.cs
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AI_FileOrganizer.Services
{
    public class GeminiAiProvider : IAiProvider
    {
        private const string GEMINI_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiAiProvider(string apiKey, HttpClient httpClient)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("De Gemini API key mag niet leeg zijn.", nameof(apiKey));

            _apiKey = apiKey;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> GetTextCompletionAsync(string prompt, string modelName, int maxTokens, float temperature, CancellationToken cancellationToken)
        {
            // === Validatie ===
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("De prompt voor Gemini mag niet leeg zijn.", nameof(prompt));
            if (string.IsNullOrWhiteSpace(modelName))
                throw new ArgumentException("Modelnaam mag niet leeg zijn.", nameof(modelName));
            if (maxTokens <= 0 || maxTokens > 2048)
                throw new ArgumentOutOfRangeException(nameof(maxTokens), "maxTokens moet tussen 1 en 2048 zijn.");
            if (temperature < 0 || temperature > 1)
                throw new ArgumentOutOfRangeException(nameof(temperature), "temperature moet tussen 0.0 en 1.0 zijn.");

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    maxOutputTokens = maxTokens,
                    temperature = temperature
                }
            };

            string jsonRequest = JsonConvert.SerializeObject(requestBody);
            string endpoint = $"{GEMINI_BASE_URL}{modelName}:generateContent?key={_apiKey}";

            try
            {
                var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, httpContent, cancellationToken);

                response.EnsureSuccessStatusCode(); // Gooi een exception als het geen succesvolle statuscode is

                string jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(jsonResponse);

                string resultText = result?.candidates?[0]?.content?.parts?[0]?.text;
                if (!string.IsNullOrWhiteSpace(resultText))
                    return resultText.Trim();

                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[GeminiAiProvider] HTTP-fout: {httpEx.Message}");
                return null;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[GeminiAiProvider] JSON-fout: {jsonEx.Message}");
                return null;
            }
            catch (OperationCanceledException)
            {
                throw; // Belangrijk: propagatie van annulering
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeminiAiProvider] Onbekende fout: {ex.Message}");
                return null;
            }
        }
    }
}
