using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AI_FileOrganizer.Utils;

namespace AI_FileOrganizer.Services
{
    public class AiClassificationService
    {
        private readonly ILogger _logger;
        private const string DEFAULT_FALLBACK_CATEGORY = "Overig";
        public long LastCallSimulatedTokensUsed { get; private set; }

        // AI-parameters
        private const int CATEGORY_MAX_TOKENS = 50;
        private const float CATEGORY_TEMPERATURE = 0.0f;
        private const int SUBFOLDER_MAX_TOKENS = 20;
        private const float SUBFOLDER_TEMPERATURE = 0.2f;
        private const int FILENAME_MAX_TOKENS = 30;
        private const float FILENAME_TEMPERATURE = 0.3f;

        public AiClassificationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LastCallSimulatedTokensUsed = 0;
        }

        /// <summary>
        /// Classificeert een document naar categorie. Werkt met prompt-based (GPT, OpenAI, Gemini) en ONNX/RobBERT (cosine similarity).
        /// </summary>
        public async Task<string> ClassifyCategoryAsync(
            string textToClassify,
            string originalFilename,
            List<string> categories,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken,
            Dictionary<string, float[]> categoryEmbeddings = null // Alleen voor ONNX RobBERT
        )
        {
            this.LastCallSimulatedTokensUsed = 0; // Reset

            // Embedding-based classificatie (RobBERT ONNX of vergelijkbaar)
            if (aiProvider is OnnxRobBERTProvider robbertProvider && categoryEmbeddings != null)
            {
                _logger.Log("INFO: Embedding-gebaseerde classificatie (RobBERT/ONNX) wordt gebruikt.");
                string embeddingStr = await robbertProvider.GetTextCompletionAsync(
                    textToClassify, modelName, 0, 0, cancellationToken);
                float[] docEmbedding = embeddingStr.Split(',').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();

                // Zoek de beste categorie via cosine similarity
                string bestCategory = null;
                double bestSim = double.MinValue;
                foreach (var cat in categories)
                {
                    if (!categoryEmbeddings.ContainsKey(cat)) continue;
                    double sim = CosineSimilarity(docEmbedding, categoryEmbeddings[cat]);
                    if (sim > bestSim)
                    {
                        bestSim = sim;
                        bestCategory = cat;
                    }
                }
                _logger.Log($"INFO: RobBERT cosine similarity: '{bestCategory}' (sim score: {bestSim:0.###})");
                return bestCategory ?? DEFAULT_FALLBACK_CATEGORY;
            }

            // Prompt-based AI
            _logger.Log("INFO: Prompt-based classificatie (GPT/Gemini/OpenAI) wordt gebruikt.");
            if (string.IsNullOrWhiteSpace(textToClassify) && string.IsNullOrWhiteSpace(originalFilename))
            {
                _logger.Log("WAARSCHUWING: Geen tekst en geen bestandsnaam om te classificeren. Retourneer fallback categorie.");
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

            var validCategories = new List<string>(categories) { DEFAULT_FALLBACK_CATEGORY };
            var categoryListForPrompt = string.Join("\n- ", categories);

            bool wasTextMeaningful;
            string aiInputText = GetRelevantTextForAI(textToClassify, originalFilename, 8000, out wasTextMeaningful);

            string textContext = wasTextMeaningful ?
                $@"<tekst_inhoud>
{aiInputText}
</tekst_inhoud>" :
                $@"<document_zonder_inhoud>
{aiInputText}
</document_zonder_inhoud>";

            var fewShotExamples = @"
Voorbeeld 1:
Tekst: 'Ik heb mijn autoverzekering aangepast bij Interpolis.'
Antwoord: Verzekeringen

Voorbeeld 2:
Tekst: 'Onze zomervakantie naar Spanje is geboekt.'
Antwoord: Reizen en vakanties

Voorbeeld 3:
Tekst: 'Ik heb mijn salaris ontvangen en overgemaakt naar mijn spaarrekening.'
Antwoord: Financiën

Voorbeeld 4:
Tekst: 'De belastingaangifte voor 2022 is binnen.'
Antwoord: Belastingen

Voorbeeld 5:
Bestandsnaam: huwelijksakte_familie_Jansen.pdf
Tekst: Dit document heeft de bestandsnaam 'huwelijksakte_familie_Jansen'. Er kon geen inhoud uit het document worden geëxtraheerd, of de inhoud was leeg. Analyseer alleen de bestandsnaam en probeer daaruit de essentie te halen.
Antwoord: Persoonlijke documenten
";

            var prompt = $@"
Je bent een AI-classificatiemodel. Je taak is om tekstfragmenten te classificeren in één van de exact opgegeven categorieën.
**Retourneer uitsluitend de exacte categorienaam. Absoluut GEEN andere tekst, uitleg, nummering of opmaak (zoals quotes, opsommingstekens, of inleidende zinnen).**
Gebruik de fallbackcategorie als geen enkele andere categorie duidelijk past.

Je krijgt informatie over een document. Kies exact één van de volgende categorieën:

<categories>
- {categoryListForPrompt}
- {DEFAULT_FALLBACK_CATEGORY} (gebruik deze als geen enkele andere categorie duidelijk past)
</categories>

Regels:
- Retourneer **ENKEL EN ALLEEN** één categorie uit bovenstaande lijst.
- **GEEN uitleg, GEEN nummering, GEEN opmaak, GEEN inleidende zinnen (zoals 'De categorie is:').**
- Als meerdere categorieën mogelijk zijn: kies de meest specifieke.
- Gebruik de fallbackcategorie alleen als echt niets past.
- Als het document geen leesbare inhoud heeft (<document_zonder_inhoud>), baseer je dan uitsluitend op de bestandsnaam en de context daarvan.

{fewShotExamples}

Documentinformatie:
<bestandsnaam>
{originalFilename}
</bestandsnaam>

{textContext}

Antwoord: ";

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
                if (chosenCategory != null)
                {
                    this.LastCallSimulatedTokensUsed = (prompt.Length / 4) + (chosenCategory.Length / 4);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log("INFO: Categorieclassificatie geannuleerd.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Fout bij classificatie AI-aanroep: {ex.Message}");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            _logger.Log($"DEBUG: Ruwe AI-antwoord voor categorie: '{chosenCategory?.Replace("\n", "\\n")}'.");

            if (!string.IsNullOrWhiteSpace(chosenCategory))
            {
                // Opschonen van AI-antwoord
                chosenCategory = Regex.Replace(chosenCategory, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                chosenCategory = Regex.Replace(chosenCategory, "Categorie:", "", RegexOptions.IgnoreCase).Trim();
                chosenCategory = chosenCategory.Trim('\'', '\"');
            }

            if (string.IsNullOrWhiteSpace(chosenCategory))
            {
                _logger.Log("WAARSCHUWING: AI retourneerde geen bruikbare categorie (leeg of witruimte). Val terug op default.");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            if (validCategories.Contains(chosenCategory))
                return chosenCategory;

            foreach (var validCat in validCategories)
            {
                if (validCat.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase) ||
                    validCat.ToLowerInvariant().Contains(chosenCategory.ToLowerInvariant()) ||
                    chosenCategory.ToLowerInvariant().Contains(validCat.ToLowerInvariant()))
                {
                    _logger.Log($"INFO: Gevonden categorie '{chosenCategory}' fuzzy-matched naar '{validCat}'.");
                    return validCat;
                }
            }

            _logger.Log($"WAARSCHUWING: AI-gekozen categorie '{chosenCategory}' is niet valide en kon niet fuzzy-matched worden. Val terug op default.");
            return DEFAULT_FALLBACK_CATEGORY;
        }

        /// <summary>
        /// Sugereert een submapnaam op basis van de inhoud van een document en de originele bestandsnaam.
        /// </summary>
        public async Task<string> SuggestSubfolderNameAsync(
            string textToAnalyze,
            string originalFilename,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0;

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

            bool wasTextMeaningful;
            string aiInputText = GetRelevantTextForAI(textToAnalyze, originalFilename, 2000, out wasTextMeaningful);

            string textContext = wasTextMeaningful ?
                $@"<tekst_inhoud>
{aiInputText}
</tekst_inhoud>" :
                $@"<document_zonder_inhoud>
{aiInputText}
</document_zonder_inhoud>";

            var prompt = $@"
### SYSTEM INSTRUCTIE
Je bent een AI-assistent die helpt bij het organiseren van documenten in logische mappen. 
Je taak is om een **korte en beschrijvende submapnaam** te suggereren op basis van de documentinhoud of de bestandsnaam als fallback.

### INSTRUCTIES
- Gebruik maximaal **5 woorden**.
- Vat het hoofdonderwerp of doel van het document bondig samen.
- Vermijd generieke termen zoals 'document', 'info', 'bestand', 'overig' of alleen een datum.
- Gebruik bij voorkeur betekenisvolle termen zoals 'Belastingaangifte 2023' of 'CV Jan Jansen'.
- **GEEF ENKEL DE SUBMAPNAAM TERUG – GEEN uitleg, GEEN opmaak, GEEN opsomming, GEEN quotes of padscheidingstekens, GEEN inleidende zinnen (zoals 'De submapnaam is:').**
- Als het document geen leesbare inhoud heeft (<document_zonder_inhoud>), focus dan op de originele bestandsnaam en de algemene beschrijving in dat blok.

### FEW-SHOT VOORBEELDEN
<voorbeeld>
Bestandsnaam: jaaropgave_2023_ing.pdf  
Tekst: Dit document betreft uw jaarlijkse jaaropgave voor belastingdoeleinden...  
Antwoord: Jaaropgave ING 2023
</voorbeeld>

<voorbeeld>
Bestandsnaam: cv_jan.docx  
Tekst: Curriculum Vitae van Jan Jansen met werkervaring in IT...  
Antwoord: CV Jan Jansen
</voorbeeld>

<voorbeeld>
Bestandsnaam: offerte_hypotheek_rabobank.pdf  
Tekst: Geachte heer, hierbij ontvangt u uw hypotheekofferte...  
Antwoord: Hypotheekofferte Rabobank
</voorbeeld>

<voorbeeld>
Bestandsnaam: huwelijksakte_piet_en_nel_2023.pdf
Tekst: Dit document heeft de bestandsnaam 'huwelijksakte_piet_en_nel_2023'. Er kon geen inhoud uit het document worden geëxtraheerd, of de inhoud was leeg. Analyseer alleen de bestandsnaam en probeer daaruit de essentie te halen.
Antwoord: Huwelijksakte Piet en Nel
</voorbeeld>

### INPUT
<bestandsnaam>
{originalFilename}
</bestandsnaam>

{textContext}

Antwoord: ";

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
                if (suggestedName != null)
                {
                    this.LastCallSimulatedTokensUsed = (prompt.Length / 4) + (suggestedName.Length / 4);
                }
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

            _logger.Log($"DEBUG: Ruwe AI-antwoord voor submapnaam: '{suggestedName?.Replace("\n", "\\n")}'.");

            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = Regex.Replace(suggestedName, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                suggestedName = suggestedName.Trim('\'', '\"');
            }

            string cleaned = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");
            var generiek = new[] { "document", "bestand", "info", "overig", "algemeen", "diversen", "" };

            bool needsRetry = string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 3 || generiek.Contains(cleaned.ToLowerInvariant());

            if (needsRetry)
            {
                _logger.Log($"INFO: Eerste AI-suggestie '{suggestedName?.Trim() ?? "[LEEG]"}' voor '{originalFilename}' was onbruikbaar (leeg/te kort/generiek). Start retry...");
                long previousTokens = this.LastCallSimulatedTokensUsed;

                var retryPrompt = prompt + "\n\nDe vorige suggestie was niet bruikbaar. Denk goed na en geef nu alsnog een CONCRETE, KORTE EN BESCHRIJVENDE mapnaam. De output moet DIRECT de naam zijn.";
                try
                {
                    suggestedName = await aiProvider.GetTextCompletionAsync(
                        retryPrompt,
                        modelName,
                        SUBFOLDER_MAX_TOKENS,
                        SUBFOLDER_TEMPERATURE,
                        cancellationToken
                    );

                    if (suggestedName != null)
                    {
                        this.LastCallSimulatedTokensUsed = previousTokens + (retryPrompt.Length / 4) + (suggestedName.Length / 4);
                    }

                    _logger.Log($"DEBUG: Ruwe AI-antwoord voor submapnaam (retry): '{suggestedName?.Replace("\n", "\\n")}'.");

                    if (!string.IsNullOrWhiteSpace(suggestedName))
                    {
                        suggestedName = Regex.Replace(suggestedName, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                        suggestedName = suggestedName.Trim('\'', '\"');
                    }
                    cleaned = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT: Retry submapnaam faalde voor '{originalFilename}': {ex.Message}");
                }
            }

            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 3 || generiek.Contains(cleaned.ToLowerInvariant()))
            {
                _logger.Log($"INFO: AI faalde voor '{originalFilename}' na retry of initieel. Probeer patroon-gebaseerde fallback...");
                cleaned = FileUtils.FallbackFolderNameFromFilename(originalFilename);

                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    _logger.Log($"WAARSCHUWING: Geen bruikbare submapnaam gevonden voor '{originalFilename}'. Bestand blijft mogelijk in hoofdmap van de categorie.");
                    return null;
                }
            }

            // Zet naar title case voor consistentie
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
        }

        /// <summary>
        /// Sugereert een nieuwe bestandsnaam op basis van de inhoud van een document en de originele bestandsnaam.
        /// </summary>
        public async Task<string> SuggestFileNameAsync(
            string textToAnalyze,
            string originalFilename,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0;

            if (aiProvider == null)
            {
                _logger.Log("FOUT: AI-provider is null voor bestandsnaam-suggestie.");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }
            if (string.IsNullOrWhiteSpace(modelName))
            {
                _logger.Log("FOUT: Modelnaam is leeg voor bestandsnaam-suggestie.");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }

            bool wasTextMeaningful;
            string aiInputText = GetRelevantTextForAI(textToAnalyze, originalFilename, 2000, out wasTextMeaningful);

            string textContext = wasTextMeaningful ?
                $@"<tekst_inhoud>
{aiInputText}
</tekst_inhoud>" :
                $@"<document_zonder_inhoud>
{aiInputText}
</document_zonder_inhoud>";

            var prompt = $@"
Je bent een AI-assistent die helpt bij het organiseren van bestanden.
Analyseer de volgende informatie over een document (oorspronkelijke bestandsnaam: ""{originalFilename}"") en stel een KORTE, BESCHRIJVENDE bestandsnaam voor (maximaal 10 woorden).
Deze bestandsnaam moet het hoofdonderwerp of de essentie van het document samenvatten, zonder de bestandsextensie.
Gebruik geen ongeldige karakters voor bestandsnamen.
Voorbeelden: ""Jaarverslag 2023 Hypotheekofferte Rabobank"", ""Notulen Project X"", ""CV Jan Jansen"".
Vermijd generieke namen zoals ""Document"", ""Bestand"", ""Info"", ""Factuur"" of simpelweg een datum zonder context.
**Geef ALLEEN de voorgestelde bestandsnaam terug, zonder extra uitleg of opmaak, en ZONDER quotes of extensie, en GEEN inleidende zinnen (zoals 'De bestandsnaam is:').**
Als het document geen leesbare inhoud heeft (<document_zonder_inhoud>), focus dan op de originele bestandsnaam en de algemene beschrijving in dat blok.

<bestandsnaam>
{originalFilename}
</bestandsnaam>

{textContext}

Antwoord: ";

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
                if (suggestedName != null)
                {
                    this.LastCallSimulatedTokensUsed = (prompt.Length / 4) + (suggestedName.Length / 4);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log($"INFO: Bestandsnaam AI-suggestie voor '{originalFilename}' geannuleerd.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Fout bij bestandsnaam AI-aanroep voor '{originalFilename}': {ex.Message}");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }

            _logger.Log($"DEBUG: Ruwe AI-antwoord voor bestandsnaam: '{suggestedName?.Replace("\n", "\\n")}'.");

            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = Regex.Replace(suggestedName, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                suggestedName = suggestedName.Trim('\'', '\"');
            }

            string cleanedName = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");

            var genericNames = new[] { "document", "bestand", "info", "overig", "algemeen", "factuur", "" };
            if (cleanedName.Length < 3 || genericNames.Contains(cleanedName.ToLowerInvariant()))
            {
                _logger.Log($"WAARSCHUWING: AI-suggestie '{suggestedName?.Trim() ?? "[LEEG]"}' voor '{originalFilename}' is te kort of te generiek na opschonen. Gebruik originele naam.");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }

            // Apply max length constraint
            if (cleanedName.Length > 100)
            {
                cleanedName = cleanedName.Substring(0, 100);
                _logger.Log($"INFO: AI-gegenereerde bestandsnaam voor '{originalFilename}' afgekort naar '{cleanedName}' wegens lengtebeperking.");
            }

            return cleanedName;
        }

        /// <summary>
        /// Berekent cosine similarity tussen twee vectors
        /// </summary>
        public static double CosineSimilarity(float[] v1, float[] v2)
        {
            double dot = 0.0, mag1 = 0.0, mag2 = 0.0;
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += v1[i] * v1[i];
                mag2 += v2[i] * v2[i];
            }
            return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }

        /// <summary>
        /// Selecteert relevante tekst voor AI-input
        /// </summary>
        private string GetRelevantTextForAI(string extractedText, string originalFilename, int maxLength, out bool wasTextMeaningful)
        {
            if (string.IsNullOrWhiteSpace(extractedText?.Trim()))
            {
                wasTextMeaningful = false;
                return $"Dit document heeft de bestandsnaam '{Path.GetFileNameWithoutExtension(originalFilename)}'. Er kon geen inhoud uit het document worden geëxtraheerd, of de inhoud was leeg. Analyseer alleen de bestandsnaam en probeer daaruit de essentie te halen.";
            }
            else
            {
                wasTextMeaningful = true;
                return extractedText.Substring(0, Math.Min(extractedText.Length, maxLength));
            }
        }
    }
}
