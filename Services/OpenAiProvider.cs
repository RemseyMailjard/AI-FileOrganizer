// AI_FileOrganizer2/Services/OpenAiProvider.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenAI; // Zorg dat deze using er is voor de OpenAI.Chat namespace
using OpenAI.Chat;

namespace AI_FileOrganizer2.Services
{
    public class OpenAiProvider : IAiProvider
    {
        private readonly string _apiKey;

        public OpenAiProvider(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string> GetTextCompletionAsync(string prompt, string modelName, int maxTokens, float temperature, CancellationToken cancellationToken)
        {
            try
            {
                var client = new ChatClient(model: modelName, apiKey: _apiKey);
                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(prompt)
                };

                var chatCompletionOptions = new ChatCompletionOptions
                {
                  //  MaxTokens = maxTokens,
                    Temperature = temperature
                };

                var completionResult = await client.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
                var chatCompletion = completionResult.Value; // Gebruik .Value voor de resultaten

                var firstContent = chatCompletion.Content.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstContent?.Text))
                    return firstContent.Text.Trim();

                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Log de fout: Console.WriteLine($"Fout bij OpenAI: {ex.Message}");
                return null;
            }
        }
    }
}