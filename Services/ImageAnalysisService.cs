// AI_FileOrganizer/Services/ImageAnalysisService.cs
using AI_FileOrganizer.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// --- OpenAI GPT-4 Vision Request & Response Klassen (blijven hetzelfde) ---
// OpenAiGpt4VisionRequest, OpenAiMessage, OpenAiContentPart, OpenAiImageUrl
// OpenAiGpt4VisionResponse, OpenAiChoice, OpenAiResponseMessage, OpenAiUsage, OpenAiError
// (Deze klassen zijn hier weggelaten voor beknoptheid, maar moeten in het bestand staan zoals eerder)

// --- Azure AI Vision Analyze v3.2 Response Klassen (voorbeeld) ---
public class AzureAnalyzeV32Response
{
    [JsonPropertyName("description")]
    public AzureDescriptionV32 Description { get; set; }
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; }
    [JsonPropertyName("modelVersion")]
    public string ModelVersion { get; set; }
}

public class AzureDescriptionV32
{
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [JsonPropertyName("captions")]
    public List<AzureCaptionV32> Captions { get; set; }
}

public class AzureCaptionV32
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}
// --- Einde Azure specifieke klassen ---

namespace AI_FileOrganizer.Services
{
    public class ImageAnalysisService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        public long LastCallSimulatedTokensUsed { get; private set; }

        // OpenAI specifieke configuratie
        private string _openAiApiKey;
        private string _openAiApiEndpoint = "https://api.openai.com/v1/chat/completions";
        private string _gpt4VisionModel = "gpt-4o";

        // Azure AI Vision specifieke configuratie
        private string _azureVisionEndpoint;
        private string _azureVisionApiKey;

        public ImageAnalysisService(ILogger logger, HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            LastCallSimulatedTokensUsed = 0;
        }

        public void ConfigureAzureVision(string endpoint, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentNullException(nameof(endpoint), "Azure Vision endpoint mag niet leeg zijn.");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "Azure Vision API key mag niet leeg zijn.");

            _azureVisionEndpoint = endpoint.TrimEnd('/');
            _azureVisionApiKey = apiKey;
            _logger.Log($"INFO: ImageAnalysisService geconfigureerd voor Azure AI Vision endpoint: {_azureVisionEndpoint}");
        }

        public void ConfigureOpenAi(string apiKey, string modelName = "gpt-4o")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey), "OpenAI API key mag niet leeg zijn.");

            _openAiApiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(modelName))
            {
                _gpt4VisionModel = modelName;
            }
            _logger.Log($"INFO: ImageAnalysisService geconfigureerd voor OpenAI GPT-4 Vision model: {_gpt4VisionModel}");
        }

        /// <summary>
        /// Genereert een beschrijvende naam voor een afbeelding met OpenAI GPT-4 Vision.
        /// </summary>
        public async Task<string> SuggestImageNameOpenAiAsync( // Hernoemd voor duidelijkheid
           string imagePath,
           string originalFilename,
           CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0;

            if (string.IsNullOrWhiteSpace(_openAiApiKey))
            {
                _logger.Log("FOUT: OpenAI API Key is niet geconfigureerd. Roep ConfigureOpenAi aan.");
                return null;
            }

            if (!System.IO.File.Exists(imagePath))
            {
                _logger.Log($"FOUT: Afbeeldingsbestand niet gevonden: {imagePath}");
                return null;
            }

            try
            {
                // *** GEBRUIK System.IO.File.ReadAllBytesAsync ***
                byte[] imageBytes =  System.IO.File.ReadAllBytes(imagePath);
                cancellationToken.ThrowIfCancellationRequested();

                string base64Image = Convert.ToBase64String(imageBytes);
                string imageMimeType = GetMimeType(imagePath);

                if (string.IsNullOrEmpty(imageMimeType))
                {
                    _logger.Log($"WAARSCHUWING: Kon MimeType niet bepalen voor {Path.GetFileName(imagePath)}. Gebruik generiek 'image/jpeg'.");
                    imageMimeType = "image/jpeg";
                }

                string promptText = $"Beschrijf de inhoud van deze afbeelding kort en bondig, met als doel een goede bestandsnaam te genereren (maximaal 5-7 woorden). Focus op de belangrijkste objecten, personen, of de algehele setting. De originele bestandsnaam was '{originalFilename}'. Geef alleen de beschrijvende tekst, geen extra uitleg.";

                var requestPayload = new OpenAiGpt4VisionRequest
                {
                    Model = _gpt4VisionModel,
                    Messages = new List<OpenAiMessage>
                    {
                        new OpenAiMessage
                        {
                            Role = "user",
                            Content = new List<OpenAiContentPart>
                            {
                                new OpenAiContentPart { Type = "text", Text = promptText },
                                new OpenAiContentPart
                                {
                                    Type = "image_url",
                                    ImageUrl = new OpenAiImageUrl
                                    {
                                        Url = $"data:{imageMimeType};base64,{base64Image}",
                                        Detail = "low"
                                    }
                                }
                            }
                        }
                    },
                    MaxTokens = 70,
                    Temperature = 0.3f
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload,
                    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

                using (var request = new HttpRequestMessage(HttpMethod.Post, _openAiApiEndpoint))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    _logger.Log($"INFO: Aanroep naar OpenAI GPT-4 Vision (model: {_gpt4VisionModel}). Prompt (tekstdeel): {promptText.Substring(0, Math.Min(promptText.Length, 100))}...");
                    HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    cancellationToken.ThrowIfCancellationRequested();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Log($"DEBUG: OpenAI GPT-4 Vision succesvol antwoord (eerste 500 karakters): {responseBody.Substring(0, Math.Min(responseBody.Length, 500))}");
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        OpenAiGpt4VisionResponse visionResponse = JsonSerializer.Deserialize<OpenAiGpt4VisionResponse>(responseBody, options);

                        if (visionResponse?.Choices != null && visionResponse.Choices.Any())
                        {
                            string description = visionResponse.Choices[0].Message?.Content?.Trim();
                            if (!string.IsNullOrWhiteSpace(description))
                            {
                                _logger.Log($"INFO: GPT-4 Vision beschrijving: '{description}'");
                                if (visionResponse.Usage != null)
                                {
                                    this.LastCallSimulatedTokensUsed = visionResponse.Usage.TotalTokens;
                                    _logger.Log($"INFO: OpenAI Tokens gebruikt: Prompt={visionResponse.Usage.PromptTokens}, Completion={visionResponse.Usage.CompletionTokens}, Totaal={this.LastCallSimulatedTokensUsed}");
                                }
                                else
                                {
                                    this.LastCallSimulatedTokensUsed = CalculateSimulatedTokens(promptText + " (afbeeldingdata)", description);
                                }

                                string filenameSuggestion = Regex.Replace(description, @"[\r\n]+", " ").Trim();
                                filenameSuggestion = FileUtils.SanitizeFolderOrFileName(filenameSuggestion);
                                filenameSuggestion = Regex.Replace(filenameSuggestion, @"\s+", " ").Trim();
                                var words = filenameSuggestion.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                int maxWordsForFilename = 7;
                                filenameSuggestion = string.Join(" ", words.Take(maxWordsForFilename));

                                if (string.IsNullOrWhiteSpace(filenameSuggestion))
                                {
                                    _logger.Log($"WAARSCHUWING: Na opschonen was de bestandsnaamsuggestie leeg. Originele naam '{originalFilename}' wordt mogelijk gebruikt.");
                                    return Path.GetFileNameWithoutExtension(originalFilename);
                                }
                                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filenameSuggestion.ToLowerInvariant());
                            }
                        }
                        _logger.Log("WAARSCHUWING: OpenAI GPT-4 Vision gaf geen bruikbare content (description) terug in de response structuur.");
                        if (visionResponse?.Usage != null) this.LastCallSimulatedTokensUsed = visionResponse.Usage.TotalTokens;
                        else this.LastCallSimulatedTokensUsed = CalculateSimulatedTokens(promptText, "");
                        return null;
                    }
                    else
                    {
                        _logger.Log($"FOUT: OpenAI GPT-4 Vision API fout {response.StatusCode}: {responseBody}");
                        try
                        {
                            var errorResponse = JsonSerializer.Deserialize<OpenAiGpt4VisionResponse>(responseBody);
                            if (errorResponse?.Error != null)
                            {
                                _logger.Log($"OpenAI Error Details: Type='{errorResponse.Error.Type}', Message='{errorResponse.Error.Message}', Code='{errorResponse.Error.Code}'");
                            }
                        }
                        catch (JsonException jsonEx) { _logger.Log($"FOUT: Kon OpenAI error response niet parsen: {jsonEx.Message}"); }
                        this.LastCallSimulatedTokensUsed = CalculateSimulatedTokens(promptText, "");
                        return null;
                    }
                }
            }
            catch (OperationCanceledException) { _logger.Log("INFO: Afbeeldingsanalyse met GPT-4 Vision geannuleerd."); throw; }
            catch (HttpRequestException httpEx) { _logger.Log($"FOUT: Netwerkfout bij aanroepen OpenAI API: {httpEx.Message}"); this.LastCallSimulatedTokensUsed = CalculateSimulatedTokens("", ""); return null; }
            catch (JsonException jsonEx) { _logger.Log($"FOUT: Fout bij (de)serialiseren JSON voor OpenAI: {jsonEx.Message}"); this.LastCallSimulatedTokensUsed = CalculateSimulatedTokens("", ""); return null; }
            catch (Exception ex) { _logger.Log($"FOUT: Onbekende fout bij analyseren afbeelding '{Path.GetFileName(imagePath)}' met GPT-4 Vision: {ex.Message}\nStackTrace: {ex.StackTrace}"); return null; }
        }

        /// <summary>
        /// Genereert een beschrijvende naam voor een afbeelding met Azure AI Vision.
        /// </summary>
        public async Task<string> SuggestImageNameAzureAsync( // Nieuwe methode voor Azure
            string imagePath,
            string originalFilename,
            CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0;

            if (string.IsNullOrWhiteSpace(_azureVisionEndpoint) || string.IsNullOrWhiteSpace(_azureVisionApiKey))
            {
                _logger.Log("FOUT: Azure AI Vision is niet geconfigureerd. Roep ConfigureAzureVision aan.");
                return null;
            }

            if (!System.IO.File.Exists(imagePath))
            {
                _logger.Log($"FOUT: Afbeeldingsbestand niet gevonden: {imagePath}");
                return null;
            }

            try
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                cancellationToken.ThrowIfCancellationRequested();

                // Gebruik de Azure AI Vision v3.2 Analyze endpoint voor 'Description'
                string analyzeV32Url = $"{_azureVisionEndpoint}/vision/v3.2/analyze?visualFeatures=Description&language=nl&descriptionExclude=Celebrities,Landmarks&maxCandidates=1";

                using (var request = new HttpRequestMessage(HttpMethod.Post, analyzeV32Url))
                {
                    request.Headers.Add("Ocp-Apim-Subscription-Key", _azureVisionApiKey);
                    request.Content = new ByteArrayContent(imageBytes);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    _logger.Log($"INFO: Aanroep naar Azure AI Vision (Analyze v3.2): {analyzeV32Url}");
                    HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    cancellationToken.ThrowIfCancellationRequested();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Log($"DEBUG: Azure AI Vision (Analyze v3.2) succesvol antwoord: {responseBody.Substring(0, Math.Min(responseBody.Length, 500))}");
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        AzureAnalyzeV32Response analyzeResponse = JsonSerializer.Deserialize<AzureAnalyzeV32Response>(responseBody, options);

                        if (analyzeResponse?.Description?.Captions != null && analyzeResponse.Description.Captions.Any())
                        {
                            var bestCaption = analyzeResponse.Description.Captions.OrderByDescending(c => c.Confidence).First();
                            string captionText = bestCaption.Text;
                            _logger.Log($"INFO: Beste caption van Azure: '{captionText}' (Confidence: {bestCaption.Confidence:P1})");

                            // Azure Vision telt transacties, geen tokens zoals LLMs.
                            // We simuleren 1 "grote" transactie hier.
                            this.LastCallSimulatedTokensUsed = 1; // Of een andere waarde die een transactie representeert.
                            _logger.Log($"INFO: Azure AI Vision transactie geteld: 1");


                            string filenameSuggestion = Regex.Replace(captionText, @"[\r\n]+", " ").Trim();
                            filenameSuggestion = FileUtils.SanitizeFolderOrFileName(filenameSuggestion);
                            filenameSuggestion = Regex.Replace(filenameSuggestion, @"\s+", " ").Trim();
                            var words = filenameSuggestion.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            int maxWordsForFilename = 7;
                            filenameSuggestion = string.Join(" ", words.Take(maxWordsForFilename));

                            if (string.IsNullOrWhiteSpace(filenameSuggestion))
                            {
                                _logger.Log($"WAARSCHUWING: Na opschonen was de Azure bestandsnaamsuggestie leeg. Originele naam '{originalFilename}' wordt mogelijk gebruikt.");
                                return Path.GetFileNameWithoutExtension(originalFilename);
                            }
                            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filenameSuggestion.ToLowerInvariant());
                        }
                        else
                        {
                            _logger.Log("WAARSCHUWING: Azure AI Vision gaf geen caption terug.");
                            this.LastCallSimulatedTokensUsed = 0; // Geen succesvolle transactie
                            return null;
                        }
                    }
                    else
                    {
                        _logger.Log($"FOUT: Azure AI Vision (Analyze v3.2) fout {response.StatusCode}: {responseBody}");
                        this.LastCallSimulatedTokensUsed = 0;
                        return null;
                    }
                }
            }
            catch (OperationCanceledException) { _logger.Log("INFO: Afbeeldingsanalyse met Azure Vision geannuleerd."); throw; }
            catch (HttpRequestException httpEx) { _logger.Log($"FOUT: Netwerkfout bij aanroepen Azure Vision API: {httpEx.Message}"); return null; }
            catch (JsonException jsonEx) { _logger.Log($"FOUT: Fout bij (de)serialiseren JSON voor Azure Vision: {jsonEx.Message}"); return null; }
            catch (Exception ex) { _logger.Log($"FOUT: Onbekende fout bij analyseren afbeelding '{Path.GetFileName(imagePath)}' met Azure Vision: {ex.Message}\nStackTrace: {ex.StackTrace}"); return null; }
        }


        private string GetMimeType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".bmp": return "image/bmp";
                case ".webp": return "image/webp";
                default:
                    _logger.Log($"WAARSCHUWING: Onbekende MimeType voor extensie '{extension}'.");
                    return "application/octet-stream"; // Generieke fallback
            }
        }

        private long CalculateSimulatedTokens(string textPart, string completionPart)
        {
            long tokens = 0;
            if (!string.IsNullOrEmpty(textPart)) tokens += textPart.Length / 4;
            if (!string.IsNullOrEmpty(completionPart)) tokens += completionPart.Length / 4;
            // Dit is alleen relevant als de 'usage' van OpenAI ontbreekt.
            // Voor Azure Vision is dit niet van toepassing (transactie-gebaseerd).
            return tokens;
        }
    }
}

// Plaats deze OpenAI specifieke klassen bovenaan het bestand ImageAnalysisService.cs, of in een apart bestand.
public class OpenAiGpt4VisionRequest
{ /* ... zoals eerder gedefinieerd ... */
    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("messages")]
    public List<OpenAiMessage> Messages { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }
}
public class OpenAiMessage
{ /* ... */
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public List<OpenAiContentPart> Content { get; set; }
}
public class OpenAiContentPart
{ /* ... */
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Text { get; set; }

    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAiImageUrl ImageUrl { get; set; }
}
public class OpenAiImageUrl
{ /* ... */
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("detail")]
    public string Detail { get; set; }
}
public class OpenAiGpt4VisionResponse
{ /* ... */
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenAiChoice> Choices { get; set; }

    [JsonPropertyName("usage")]
    public OpenAiUsage Usage { get; set; }

    [JsonPropertyName("error")]
    public OpenAiError Error { get; set; }
}
public class OpenAiChoice
{ /* ... */
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public OpenAiResponseMessage Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}
public class OpenAiResponseMessage
{ /* ... */
    [JsonPropertyName("role")]
    public string Role { get; set; }
    [JsonPropertyName("content")]
    public string Content { get; set; }
}
public class OpenAiUsage
{ /* ... */
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
public class OpenAiError
{ /* ... */
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("param")]
    public string Param { get; set; }
    [JsonPropertyName("code")]
    public string Code { get; set; }
}