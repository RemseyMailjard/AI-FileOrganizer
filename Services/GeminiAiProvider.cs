// AI_FileOrganizer2/Services/GeminiAiProvider.cs
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AI_FileOrganizer2.Services
{
    public class GeminiAiProvider : IAiProvider
    {
        private const string GEMINI_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiAiProvider(string apiKey, HttpClient httpClient)
        {
            _apiKey = apiKey;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> GetTextCompletionAsync(string prompt, string modelName, int maxTokens, float temperature, CancellationToken cancellationToken)
        {
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
                // Log de HTTP-fout, bijvoorbeeld: Console.WriteLine($"HTTP Fout bij Gemini: {httpEx.Message}");
                return null;
            }
            catch (JsonException jsonEx)
            {
                // Log de JSON-parse fout: Console.WriteLine($"JSON Fout bij Gemini: {jsonEx.Message}");
                return null;
            }
            catch (OperationCanceledException)
            {
                // Annulering gevraagd, geen fout
                throw; // Belangrijk om deze door te gooien zodat de roepende methode het kan afhandelen
            }
            catch (Exception ex)
            {
                // Algemene fout: Console.WriteLine($"Algemene Fout bij Gemini: {ex.Message}");
                return null;
            }
        }
    }
}