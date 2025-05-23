using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// Verwijder onnodige usings die nu in de AI-provider klassen zitten:
// using System.Net.Http;
// using Newtonsoft.Json;
// using OpenAI.Chat;
// using Azure.AI.OpenAI;
// using Azure;

using AI_FileOrganizer2.Utils; // Nodig voor FileUtils

namespace AI_FileOrganizer2.Services
{
    public class AiClassificationService
    {
        // Deze constanten zijn nu overbodig hier, ze horen bij de GeminiAiProvider
        // private const string GEMINI_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
        // private static readonly HttpClient _httpClient = new HttpClient();

        private const string DEFAULT_FALLBACK_CATEGORY = "Overig";

        // Nieuwe constanten voor de AI-parameters, per taak
        private const int CATEGORY_MAX_TOKENS = 50;
        private const float CATEGORY_TEMPERATURE = 0.0f; // Lager voor precieze classificatie

        private const int SUBFOLDER_MAX_TOKENS = 20;
        private const float SUBFOLDER_TEMPERATURE = 0.2f; // Iets hoger voor creativiteit, maar nog steeds gericht

        private const int FILENAME_MAX_TOKENS = 30;
        private const float FILENAME_TEMPERATURE = 0.3f; // Nog iets hoger voor creativiteit


        // ======= Publieke AI-methodes =======

        public async Task<string> ClassifyCategoryAsync(
            string textToClassify,
            List<string> categories,
            string provider,
            string apiKey,
            IAiProvider aiProvider, // Hier wordt de AI-provider nu doorgegeven
            string modelName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(textToClassify))
                return DEFAULT_FALLBACK_CATEGORY;

            var categoryListForPrompt = string.Join("\n", categories.Select(cat => $"- {cat}"));
            var prompt = $@"
                Je bent een AI-assistent gespecialiseerd in het organiseren van documenten.
                Jouw taak is om de volgende tekst te analyseren en te bepalen in welke van de onderstaande categorieën deze het beste past.

                Beschikbare categorieën:
                {categoryListForPrompt}
                - {DEFAULT_FALLBACK_CATEGORY} (gebruik deze als geen andere categorie duidelijk past)

                Geef ALLEEN de naam van de gekozen categorie terug, exact zoals deze in de lijst staat. Geen extra uitleg, nummers of opmaak.

                Tekstfragment om te classificeren:
                ---
                {textToClassify.Substring(0, Math.Min(textToClassify.Length, 8000))}
                ---

                Categorie:";

            string chosenCategory = null;

            try
            {
                // Roep de algemene methode van de IAiProvider interface aan
                chosenCategory = await aiProvider.GetTextCompletionAsync(
                    prompt,
                    modelName,
                    CATEGORY_MAX_TOKENS,
                    CATEGORY_TEMPERATURE,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw; // Belangrijk om annulering door te geven
            }
            catch (Exception ex)
            {
                // Je kunt hier loggen dat de AI-aanroep mislukte
                Console.WriteLine($"Fout bij classificatie AI-aanroep: {ex.Message}");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            if (string.IsNullOrWhiteSpace(chosenCategory))
                return DEFAULT_FALLBACK_CATEGORY;

            var validCategories = new List<string>(categories) { DEFAULT_FALLBACK_CATEGORY };
            chosenCategory = chosenCategory.Trim();

            if (validCategories.Contains(chosenCategory))
                return chosenCategory;

            // Fuzzy match (kleine afwijkingen opvangen)
            foreach (var validCat in validCategories)
            {
                if (validCat.ToLower().Contains(chosenCategory.ToLower()) || chosenCategory.ToLower().Contains(validCat.ToLower()))
                    return validCat;
            }

            return DEFAULT_FALLBACK_CATEGORY;
        }

        public async Task<string> SuggestSubfolderNameAsync(
            string textToAnalyze,
            string originalFilename,
            IAiProvider aiProvider, // Hier wordt de AI-provider nu doorgegeven
            string modelName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(textToAnalyze))
                return null;

            var prompt = $@"
Je bent een AI-assistent die helpt bij het organiseren van bestanden.
Analyseer de volgende tekst van een document (oorspronkelijke bestandsnaam: ""{originalFilename}"") en stel een KORTE, BESCHRIJVENDE submapnaam voor (maximaal 5 woorden).
Deze submapnaam moet het hoofdonderwerp of de essentie van het document samenvatten.
Voorbeelden: ""Belastingaangifte 2023"", ""Hypotheekofferte Rabobank"", ""Notulen vergadering Project X"", ""CV Jan Jansen"".
Vermijd generieke namen zoals ""Document"", ""Bestand"", ""Info"" of simpelweg een datum zonder context.
Geef ALLEEN de voorgestelde submapnaam terug, zonder extra uitleg of opmaak.

Tekstfragment:
---
{textToAnalyze.Substring(0, Math.Min(textToAnalyze.Length, 2000))}
---

Voorgestelde submapnaam:";

            string suggestedName = null;
            try
            {
                // Roep de algemene methode van de IAiProvider interface aan
                suggestedName = await aiProvider.GetTextCompletionAsync(
                    prompt,
                    modelName,
                    SUBFOLDER_MAX_TOKENS,
                    SUBFOLDER_TEMPERATURE,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij submapnaam AI-aanroep: {ex.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(suggestedName)) return null;

            string sanitized = FileUtils.SanitizeFolderOrFileName(suggestedName);
            // Je kunt overwegen om een meer specifieke lijst van "generieke" namen bij te houden
            // of een LLM te vragen of de naam generiek is. Voor nu, behoud de huidige logica.
            if (sanitized.Length < 3 || new[] { "document", "bestand", "info", "overig", "algemeen" }.Contains(sanitized.ToLower()))
                return null;

            return sanitized;
        }

        public async Task<string> SuggestFileNameAsync(
            string textToAnalyze,
            string originalFilename,
            IAiProvider aiProvider, // Hier wordt de AI-provider nu doorgegeven
            string modelName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(textToAnalyze))
                return System.IO.Path.GetFileNameWithoutExtension(originalFilename);

            var prompt = $@"
Je bent een AI-assistent die helpt bij het organiseren van bestanden.
Analyseer de volgende tekst van een document (oorspronkelijke bestandsnaam: ""{originalFilename}"") en stel een KORTE, BESCHRIJVENDE bestandsnaam voor (maximaal 10 woorden).
Deze bestandsnaam moet het hoofdonderwerp of de essentie van het document samenvatten, zonder de bestandsextensie.
Gebruik geen ongeldige karakters voor bestandsnamen.
Voorbeelden: ""Jaarverslag 2023 Hypotheekofferte Rabobank"", ""Notulen Project X"", ""CV Jan Jansen"".
Vermijd generieke namen zoals ""Document"", ""Bestand"", ""Info"", ""Factuur"" of simpelweg een datum zonder context.
Geef ALLEEN de voorgestelde bestandsnaam terug, zonder extra uitleg of opmaak, en ZONDER extensie.

Tekstfragment:
---
{textToAnalyze.Substring(0, Math.Min(textToAnalyze.Length, 2000))}
---

Voorgestelde bestandsnaam:";

            string suggestedName = null;
            try
            {
                // Roep de algemene methode van de IAiProvider interface aan
                suggestedName = await aiProvider.GetTextCompletionAsync(
                    prompt,
                    modelName,
                    FILENAME_MAX_TOKENS,
                    FILENAME_TEMPERATURE,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fout bij bestandsnaam AI-aanroep: {ex.Message}");
                return System.IO.Path.GetFileNameWithoutExtension(originalFilename);
            }

            if (string.IsNullOrWhiteSpace(suggestedName))
                return System.IO.Path.GetFileNameWithoutExtension(originalFilename);

            string cleanedName = FileUtils.SanitizeFolderOrFileName(suggestedName);

            if (cleanedName.Length < 3 || new[] { "document", "bestand", "info", "overig", "algemeen", "factuur" }.Contains(cleanedName.ToLower()))
                return System.IO.Path.GetFileNameWithoutExtension(originalFilename);

            if (cleanedName.Length > 100)
                cleanedName = cleanedName.Substring(0, 100);

            return cleanedName;
        }
    }
}