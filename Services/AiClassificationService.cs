using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AI_FileOrganizer.Utils; // Voor ILogger en FileUtils

// Zorg ervoor dat de IAiProvider interface en alle provider implementaties
// (OnnxRobBERTProvider, GeminiAiProvider, OpenAiProvider, AzureOpenAiProvider)
// correct zijn gedefinieerd en toegankelijk zijn via hun namespaces.

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

        private const int EFFECTIVE_FILENAME_MAX_TOKENS = 20;
        private const float EFFECTIVE_FILENAME_TEMPERATURE = 0.2f;

        public AiClassificationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LastCallSimulatedTokensUsed = 0;
        }

        public async Task<string> ClassifyCategoryAsync(
            string textToClassify,
            string originalFilename,
            List<string> categories,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken,
            Dictionary<string, float[]> categoryEmbeddings = null)
        {
            this.LastCallSimulatedTokensUsed = 0;

            if (aiProvider is OnnxRobBERTProvider robbertProvider && categoryEmbeddings != null)
            {
                _logger.Log("INFO: Embedding-gebaseerde classificatie (RobBERT/ONNX) wordt gebruikt.");
                string embeddingStr = await robbertProvider.GetTextCompletionAsync(
                    textToClassify, modelName, 0, 0, cancellationToken);

                if (string.IsNullOrWhiteSpace(embeddingStr))
                {
                    _logger.Log("FOUT: RobBERT provider retourneerde een lege embedding string.");
                    return DEFAULT_FALLBACK_CATEGORY;
                }

                float[] docEmbedding;
                try
                {
                    docEmbedding = embeddingStr.Split(',')
                       .Select(s => float.Parse(s.Trim(), CultureInfo.InvariantCulture))
                       .ToArray();
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT: Kon RobBERT embedding string niet parsen: {ex.Message}. Embedding string: '{embeddingStr}'");
                    return DEFAULT_FALLBACK_CATEGORY;
                }

                string bestCategory = null;
                double bestSim = double.MinValue;
                foreach (var cat in categories)
                {
                    if (!categoryEmbeddings.ContainsKey(cat))
                    {
                        _logger.Log($"WAARSCHUWING: Geen embedding gevonden voor categorie '{cat}' in de dictionary.");
                        continue;
                    }
                    float[] catEmbedding = categoryEmbeddings[cat];
                    if (catEmbedding == null || catEmbedding.Length != docEmbedding.Length)
                    {
                        _logger.Log($"WAARSCHUWING: Ongeldige of niet-overeenkomende embedding voor categorie '{cat}'. Overgeslagen.");
                        continue;
                    }
                    double sim = CosineSimilarity(docEmbedding, catEmbedding);
                    if (sim > bestSim)
                    {
                        bestSim = sim;
                        bestCategory = cat;
                    }
                }
                _logger.Log($"INFO: RobBERT cosine similarity: '{bestCategory ?? "Geen"}' (sim score: {bestSim:0.###})");
                return bestCategory ?? DEFAULT_FALLBACK_CATEGORY;
            }

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

            var validCategories = new List<string>(categories);
            if (!validCategories.Contains(DEFAULT_FALLBACK_CATEGORY, StringComparer.OrdinalIgnoreCase))
            {
                validCategories.Add(DEFAULT_FALLBACK_CATEGORY);
            }
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
                chosenCategory = Regex.Replace(chosenCategory, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                chosenCategory = Regex.Replace(chosenCategory, "Categorie:", "", RegexOptions.IgnoreCase).Trim();
                chosenCategory = chosenCategory.Trim('\'', '\"', '.', '-').Trim();
            }

            if (string.IsNullOrWhiteSpace(chosenCategory))
            {
                _logger.Log("WAARSCHUWING: AI retourneerde geen bruikbare categorie (leeg of witruimte). Val terug op default.");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            var exactMatch = validCategories.FirstOrDefault(vc => vc.Equals(chosenCategory, StringComparison.Ordinal));
            if (exactMatch != null) return exactMatch;

            var caseInsensitiveMatch = validCategories.FirstOrDefault(vc => vc.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase));
            if (caseInsensitiveMatch != null)
            {
                _logger.Log($"INFO: Gevonden categorie '{chosenCategory}' case-insensitive matched naar '{caseInsensitiveMatch}'.");
                return caseInsensitiveMatch;
            }

            foreach (var validCat in validCategories)
            {
                if (validCat.ToLowerInvariant().Contains(chosenCategory.ToLowerInvariant()) ||
                    chosenCategory.ToLowerInvariant().Contains(validCat.ToLowerInvariant()))
                {
                    _logger.Log($"INFO: Gevonden categorie '{chosenCategory}' fuzzy-matched naar '{validCat}'.");
                    return validCat;
                }
            }

            _logger.Log($"WAARSCHUWING: AI-gekozen categorie '{chosenCategory}' is niet valide en kon niet fuzzy-matched worden. Val terug op default.");
            return DEFAULT_FALLBACK_CATEGORY;
        }

        public Task<string> SuggestSubfolderNameAsync(
            string textToAnalyze,
            string originalFilename,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0;
            _logger.Log($"INFO: AI-suggestie voor submapnaam voor '{originalFilename}' wordt overgeslagen zoals geconfigureerd.");
            return Task.FromResult<string>(null);
        }

        public async Task<string> SuggestFileNameAsync(
            string textToAnalyze,
            string originalFilename,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0;
            string originalFilenameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilename);

            if (aiProvider == null)
            {
                _logger.Log("FOUT: AI-provider is null voor bestandsnaam-suggestie. Gebruik originele naam.");
                return originalFilenameWithoutExtension;
            }
            if (string.IsNullOrWhiteSpace(modelName))
            {
                _logger.Log("FOUT: Modelnaam is leeg voor bestandsnaam-suggestie. Gebruik originele naam.");
                return originalFilenameWithoutExtension;
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
Je bent een AI-assistent die helpt bij het organiseren van bestanden.
Je taak is om een **korte en beschrijvende bestandsnaam (zonder extensie)** te suggereren op basis van de documentinhoud of de oorspronkelijke bestandsnaam als fallback.

### INSTRUCTIES
- Gebruik maximaal **5 woorden**.
- Vat het hoofdonderwerp of doel van het document bondig samen.
- Vermijd generieke termen zoals 'document', 'info', 'bestand', 'overig', 'factuur', 'algemeen', 'diversen' of alleen een datum zonder context.
- Gebruik bij voorkeur betekenisvolle termen zoals 'Belastingaangifte 2023' of 'CV Jan Jansen'.
- **GEEF ENKEL DE BESTANDSNAAM TERUG – GEEN uitleg, GEEN opmaak, GEEN opsomming, GEEN quotes, GEEN bestandsextensie, GEEN inleidende zinnen (zoals 'De bestandsnaam is:').**
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
                    EFFECTIVE_FILENAME_MAX_TOKENS,
                    EFFECTIVE_FILENAME_TEMPERATURE,
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
                _logger.Log($"FOUT: Fout bij bestandsnaam AI-aanroep voor '{originalFilename}': {ex.Message}. Gebruik originele naam.");
                return originalFilenameWithoutExtension;
            }

            _logger.Log($"DEBUG: Ruwe AI-antwoord voor bestandsnaam: '{suggestedName?.Replace("\n", "\\n")}'.");

            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = Regex.Replace(suggestedName, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                suggestedName = suggestedName.Trim('\'', '\"', '.', '-').Trim();
            }

            string cleanedName = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");
            var generiek = new[] { "document", "bestand", "info", "overig", "algemeen", "diversen", "factuur", "" };

            bool needsRetry = string.IsNullOrWhiteSpace(cleanedName) ||
                              cleanedName.Length < 3 ||
                              generiek.Any(g => cleanedName.Equals(g, StringComparison.OrdinalIgnoreCase)) ||
                              Regex.IsMatch(cleanedName, @"^\d{1,2}[-/]\d{1,2}[-/]\d{2,4}$") ||
                              Regex.IsMatch(cleanedName, @"^\d{4}[-/]\d{1,2}[-/]\d{1,2}$");

            if (needsRetry)
            {
                _logger.Log($"INFO: Eerste AI-suggestie voor bestandsnaam '{suggestedName?.Trim() ?? "[LEEG]"}' voor '{originalFilename}' was onbruikbaar. Start retry...");
                long previousTokens = this.LastCallSimulatedTokensUsed;

                var retryPrompt = prompt + "\n\nDe vorige suggestie was niet bruikbaar. Geef nu een CONCRETE, KORTE EN BESCHRIJVENDE bestandsnaam (zonder extensie). De output moet DIRECT de naam zijn, niet alleen een datum.";
                try
                {
                    suggestedName = await aiProvider.GetTextCompletionAsync(
                        retryPrompt,
                        modelName,
                        EFFECTIVE_FILENAME_MAX_TOKENS,
                        EFFECTIVE_FILENAME_TEMPERATURE,
                        cancellationToken
                    );

                    if (suggestedName != null)
                    {
                        this.LastCallSimulatedTokensUsed = previousTokens + (retryPrompt.Length / 4) + (suggestedName.Length / 4);
                    }

                    _logger.Log($"DEBUG: Ruwe AI-antwoord voor bestandsnaam (retry): '{suggestedName?.Replace("\n", "\\n")}'.");

                    if (!string.IsNullOrWhiteSpace(suggestedName))
                    {
                        suggestedName = Regex.Replace(suggestedName, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                        suggestedName = suggestedName.Trim('\'', '\"', '.', '-').Trim();
                    }
                    cleanedName = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT: Retry bestandsnaam AI-suggestie faalde voor '{originalFilename}': {ex.Message}");
                    cleanedName = originalFilenameWithoutExtension;
                    _logger.Log($"INFO: Gebruik originele bestandsnaam '{cleanedName}' na mislukte retry.");
                }
            }

            if (string.IsNullOrWhiteSpace(cleanedName) ||
                cleanedName.Length < 3 ||
                generiek.Any(g => cleanedName.Equals(g, StringComparison.OrdinalIgnoreCase)) ||
                Regex.IsMatch(cleanedName, @"^\d{1,2}[-/]\d{1,2}[-/]\d{2,4}$") ||
                Regex.IsMatch(cleanedName, @"^\d{4}[-/]\d{1,2}[-/]\d{1,2}$"))
            {
                _logger.Log($"WAARSCHUWING: AI faalde een bruikbare bestandsnaam te genereren voor '{originalFilename}'. Gebruik originele naam.");
                cleanedName = originalFilenameWithoutExtension;
            }

            int maxFilenameLength = 100;
            if (cleanedName.Length > maxFilenameLength)
            {
                cleanedName = cleanedName.Substring(0, maxFilenameLength);
                int lastSpace = cleanedName.LastIndexOf(' ');
                if (lastSpace > maxFilenameLength / 2 && lastSpace > 0)
                {
                    cleanedName = cleanedName.Substring(0, lastSpace);
                }
                _logger.Log($"INFO: AI-gegenereerde bestandsnaam voor '{originalFilename}' afgekort naar '{cleanedName}' wegens lengtebeperking.");
            }

            if (!cleanedName.Equals(originalFilenameWithoutExtension, StringComparison.Ordinal))
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanedName.ToLowerInvariant());
            }
            return cleanedName;
        }

        public static double CosineSimilarity(float[] v1, float[] v2)
        {
            if (v1 == null || v2 == null || v1.Length != v2.Length || v1.Length == 0)
            {
                return 0.0;
            }

            double dot = 0.0, mag1 = 0.0, mag2 = 0.0;
            for (int i = 0; i < v1.Length; i++)
            {
                dot += v1[i] * v2[i];
                mag1 += v1[i] * v1[i];
                mag2 += v2[i] * v2[i];
            }

            if (mag1 == 0.0 || mag2 == 0.0) return 0.0;

            double similarity = dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
            return Math.Max(-1.0, Math.Min(1.0, similarity));
        }

        private string GetRelevantTextForAI(string extractedText, string originalFilename, int maxLength, out bool wasTextMeaningful)
        {
            if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Trim().Length < 10)
            {
                wasTextMeaningful = false;
                return $"Dit document heeft de bestandsnaam '{Path.GetFileNameWithoutExtension(originalFilename)}'. Er kon geen inhoud uit het document worden geëxtraheerd, of de inhoud was leeg/niet-betekenisvol. Analyseer alleen de bestandsnaam en probeer daaruit de essentie te halen.";
            }
            else
            {
                wasTextMeaningful = true;
                string cleanText = Regex.Replace(extractedText, @"\s+", " ").Trim();
                return cleanText.Length <= maxLength ? cleanText : cleanText.Substring(0, maxLength);
            }
        }
    }
}