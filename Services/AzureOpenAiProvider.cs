// AI_FileOrganizer/Services/AzureOpenAiProvider.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AI_FileOrganizer.Utils;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;

namespace AI_FileOrganizer.Services
{
    public class AzureOpenAiProvider : IAiProvider
    {
        private readonly Uri _azureEndpoint;
        private readonly string _apiKey;

        public AzureOpenAiProvider(string azureEndpoint, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(azureEndpoint) || !Uri.TryCreate(azureEndpoint, UriKind.Absolute, out _azureEndpoint))
                throw new ArgumentException("Ongeldig Azure Endpoint URL.", nameof(azureEndpoint));

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key voor Azure OpenAI mag niet leeg zijn.", nameof(apiKey));

            _apiKey = apiKey;
        }

        public async Task<string> GetTextCompletionAsync(string prompt, string modelName, int maxTokens, float temperature, CancellationToken cancellationToken)
        {
            // === Validatie ===
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("De prompt mag niet leeg zijn.", nameof(prompt));
            if (string.IsNullOrWhiteSpace(modelName))
                throw new ArgumentException("Modelnaam mag niet leeg zijn.", nameof(modelName));
            if (maxTokens <= 0 || maxTokens > 4096)
                throw new ArgumentOutOfRangeException(nameof(maxTokens), "maxTokens moet tussen 1 en 4096 zijn.");
            if (temperature < 0 || temperature > 1)
                throw new ArgumentOutOfRangeException(nameof(temperature), "temperature moet tussen 0.0 en 1.0 zijn.");

            try
            {
                var azureClient = new AzureOpenAIClient(_azureEndpoint, new AzureKeyCredential(_apiKey));
                var chatClient = azureClient.GetChatClient(modelName);

                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(prompt)
                };

                var chatCompletionOptions = new ChatCompletionOptions
                {
                    Temperature = temperature,
                    MaxOutputTokenCount = maxTokens
                };

                var completion = await chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);

                var firstContent = completion.Value.Content.FirstOrDefault();
                if (firstContent != null && !string.IsNullOrWhiteSpace(firstContent.Text))
                    return firstContent.Text.Trim();

                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"[AzureOpenAiProvider] Azure API-fout: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AzureOpenAiProvider] Algemene fout: {ex.Message}");
                return null;
            }
        }
    }
}
