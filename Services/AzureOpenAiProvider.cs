// AI_FileOrganizer2/Services/AzureOpenAiProvider.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure; // Nodig voor AzureKeyCredential
using Azure.AI.OpenAI; // Nodig voor AzureOpenAIClient en ChatMessage
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat; // Specifiek voor ChatMessage en ChatCompletionOptions

namespace AI_FileOrganizer2.Services
{
    public class AzureOpenAiProvider : IAiProvider
    {
        private readonly Uri _azureEndpoint;
        private readonly string _apiKey;

        public AzureOpenAiProvider(string azureEndpoint, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(azureEndpoint) || !Uri.TryCreate(azureEndpoint, UriKind.Absolute, out _azureEndpoint))
            {
                throw new ArgumentException("Ongeldig Azure Endpoint URL.", nameof(azureEndpoint));
            }
            _apiKey = apiKey;
        }

        public async Task<string> GetTextCompletionAsync(string prompt, string modelName, int maxTokens, float temperature, CancellationToken cancellationToken)
        {
            try
            {
                var azureClient = new AzureOpenAIClient(_azureEndpoint, new AzureKeyCredential(_apiKey));
                var chatClient = azureClient.GetChatClient(modelName); // deploymentOrModelName is hier modelName

                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(prompt)
                };

                var chatCompletionOptions = new ChatCompletionOptions
                {
                 //   MaxTokens = maxTokens,
                    Temperature = temperature
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
            catch (Exception ex)
            {
                // Log de fout: Console.WriteLine($"Fout bij Azure OpenAI: {ex.Message}");
                return null;
            }
        }
    }
}