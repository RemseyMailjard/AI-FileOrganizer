// AI_FileOrganizer2/Services/OpenAiProvider.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace AI_FileOrganizer2.Services
{
    public class OpenAiProvider : IAiProvider
    {
        private readonly string _apiKey;

        public OpenAiProvider(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("OpenAI API key mag niet leeg zijn.", nameof(apiKey));

            _apiKey = apiKey;
        }

        public async Task<string> GetTextCompletionAsync(string prompt, string modelName, int maxTokens, float temperature, CancellationToken cancellationToken)
        {
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
                var client = new ChatClient(model: modelName, apiKey: _apiKey);
                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(prompt)
                };

                var chatCompletionOptions = new ChatCompletionOptions
                {
                    Temperature = temperature,
                    MaxOutputTokenCount = maxTokens
                };

                var completionResult = await client.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
                var chatCompletion = completionResult.Value;

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
                Console.WriteLine($"[OpenAiProvider] Fout: {ex.Message}");
                return null;
            }
        }
    }
}
