using System;
using System.Collections.Generic;
using System.IO; // Nodig voor Path.GetFileNameWithoutExtension
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// Verwijder onnodige usings die nu in de AI-provider klassen zitten:
// using System.Net.Http;
// using Newtonsoft.Json;
// using OpenAI.Chat;
// using Azure.AI.OpenAI;
// using Azure;

using AI_FileOrganizer2.Utils; // Nodig voor FileUtils en ILogger

namespace AI_FileOrganizer2.Services
{
    public class AiClassificationService
    {
        private readonly ILogger _logger; // NIEUW: Logger field

        private const string DEFAULT_FALLBACK_CATEGORY = "Overig";

        // Nieuwe constanten voor de AI-parameters, per taak
        private const int CATEGORY_MAX_TOKENS = 50;
        private const float CATEGORY_TEMPERATURE = 0.0f; // Lager voor precieze classificatie

        private const int SUBFOLDER_MAX_TOKENS = 20;
        private const float SUBFOLDER_TEMPERATURE = 0.2f; // Iets hoger voor creativiteit, maar nog steeds gericht

        private const int FILENAME_MAX_TOKENS = 30;
        private const float FILENAME_TEMPERATURE = 0.3f; // Nog iets hoger voor creativiteit

        // NIEUW: Constructor om ILogger te injecteren
        public AiClassificationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ======= Publieke AI-methodes =======

        public async Task<string> ClassifyCategoryAsync(
            string textToClassify,
            List<string> categories,
            IAiProvider aiProvider, // Hier wordt ALLEEN de IAiProvider doorgegeven
            string modelName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(textToClassify))
            {
                _logger.Log("WAARSCHUWING: Geen tekst om te classificeren. Retourneer fallback categorie.");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            if (aiProvider == null)
            {
                _logger.Log("FOUT: AI-provider is null voor categorieclassificatie.");
                return DEFAULT_FALLBACK_CATEGORY;
            }
            if (string.IsNullOrWhiteSpace(modelName))
            {
                _logger.Log("FOUT: Modelnaam is leeg voor categorieclassificatie.");
                return DEFAULT_FALLBACK_CATEGORY;
            }

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
                _logger.Log("INFO: Categorieclassificatie geannuleerd.");
                throw; // Belangrijk om annulering door te geven
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Fout bij classificatie AI-aanroep: {ex.Message}");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            if (string.IsNullOrWhiteSpace(chosenCategory))
            {
                _logger.Log("WAARSCHUWING: AI retourneerde geen bruikbare categorie. Val terug op default.");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            var validCategories = new List<string>(categories) { DEFAULT_FALLBACK_CATEGORY };
            chosenCategory = chosenCategory.Trim();

            if (validCategories.Contains(chosenCategory))
                return chosenCategory;

            // Fuzzy match (kleine afwijkingen opvangen)
            foreach (var validCat in validCategories)
            {
                if (validCat.ToLowerInvariant().Contains(chosenCategory.ToLowerInvariant()) || chosenCategory.ToLowerInvariant().Contains(validCat.ToLowerInvariant()))
                {
                    _logger.Log($"INFO: Gevonden categorie '{chosenCategory}' fuzzy-matched naar '{validCat}'.");
                    return validCat;
                }
            }
            _logger.Log($"WAARSCHUWING: AI-gekozen categorie '{chosenCategory}' is niet valide en kon niet fuzzy-matched worden. Val terug op default.");
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
            {
                _logger.Log($"WAARSCHUWING: Geen tekst om te analyseren voor submapnaam van '{originalFilename}'.");
                return null;
            }
            if (aiProvider == null)
            {
                _logger.Log("FOUT: AI-provider is null voor submapnaam-suggestie.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(modelName))
            {
                _logger.Log("FOUT: Modelnaam is leeg voor submapnaam-suggestie.");
                return null;
            }

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
                _logger.Log($"INFO: Submapnaam AI-suggestie voor '{originalFilename}' geannuleerd.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Fout bij submapnaam AI-aanroep voor '{originalFilename}': {ex.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                _logger.Log($"WAARSCHUWING: AI retourneerde geen bruikbare submapnaam voor '{originalFilename}'.");
                return null;
            }

            string sanitized = FileUtils.SanitizeFolderOrFileName(suggestedName);
            var genericNames = new[] { "document", "bestand", "info", "overig", "algemeen" };
            if (sanitized.Length < 3 || genericNames.Contains(sanitized.ToLowerInvariant()))
            {
                _logger.Log($"WAARSCHUWING: AI-suggestie '{suggestedName}' voor '{originalFilename}' is te kort of te generiek na opschonen. Wordt niet gebruikt.");
                return null;
            }

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
            {
                _logger.Log($"INFO: Geen tekst geanalyseerd voor bestandsnaam van '{originalFilename}'. Retourneer originele naam (zonder extensie).");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }

            if (aiProvider == null)
            {
                _logger.Log("FOUT: AI-provider is null voor bestandsnaam-suggestie.");
                return Path.GetFileNameWithoutExtension(originalFilename); // Fallback gracefully
            }
            if (string.IsNullOrWhiteSpace(modelName))
            {
                _logger.Log("FOUT: Modelnaam is leeg voor bestandsnaam-suggestie.");
                return Path.GetFileNameWithoutExtension(originalFilename); // Fallback gracefully
            }

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
                _logger.Log($"INFO: Bestandsnaam AI-suggestie voor '{originalFilename}' geannuleerd.");
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Fout bij bestandsnaam AI-aanroep voor '{originalFilename}': {ex.Message}");
                return Path.GetFileNameWithoutExtension(originalFilename); // Fallback on error
            }

            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                _logger.Log($"WAARSCHUWING: AI retourneerde geen bruikbare bestandsnaam voor '{originalFilename}'. Gebruik originele naam.");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }

            // Use a temporary variable for the cleaned name to ensure subsequent checks operate on the sanitized version.
            string cleanedName = FileUtils.SanitizeFolderOrFileName(suggestedName);

            // Check for generic/unhelpful names after sanitization
            // Use ToLowerInvariant for culture-agnostic comparison
            var genericNames = new[] { "document", "bestand", "info", "overig", "algemeen", "factuur" };
            if (cleanedName.Length < 3 || genericNames.Contains(cleanedName.ToLowerInvariant()))
            {
                _logger.Log($"WAARSCHUWING: AI-suggestie '{suggestedName}' voor '{originalFilename}' is te kort of te generiek na opschonen. Gebruik originele naam.");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }

            // Apply max length constraint
            // Hardcoded 100 for now, as MAX_FILENAME_LENGTH from Form1 is not directly accessible here.
            // Consider making it a constant in AiClassificationService if it's a service-level limit.
            if (cleanedName.Length > 100)
            {
                cleanedName = cleanedName.Substring(0, 100);
                _logger.Log($"INFO: AI-gegenereerde bestandsnaam voor '{originalFilename}' afgekort naar '{cleanedName}' wegens lengtebeperking.");
            }

            return cleanedName;
        }
    }
}