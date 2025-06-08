using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text; // Voor StringBuilder
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AI_FileOrganizer.Models;
using AI_FileOrganizer.Utils;
// using GenerativeAI.Core; // Verwijder indien niet gebruikt in deze file

namespace AI_FileOrganizer.Services
{
    public class AiClassificationService
    {
        private readonly ILogger _logger;
        private const string DEFAULT_FALLBACK_CATEGORY = ApplicationSettings.FallbackCategoryKey;
        public long LastCallSimulatedTokensUsed { get; private set; }

        private static readonly string[] AiResponsePrefixesToClean = { "Antwoord:", "Categorie:" };

        public AiClassificationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LastCallSimulatedTokensUsed = 0;
        }

        private async Task<Tuple<string, long>> CallAiProviderAsync( // Tuple<string, long> voor C# 7.3
            IAiProvider aiProvider,
            string modelName,
            string prompt,
            AiModelParams modelParams,
            CancellationToken cancellationToken,
            string taskDescriptionForLogging)
        {
            if (aiProvider == null)
            {
                _logger.Log($"FOUT: AI Provider is null voor {taskDescriptionForLogging}.");
                return Tuple.Create<string, long>(null, 0);
            }
            if (string.IsNullOrWhiteSpace(modelName))
            {
                _logger.Log($"FOUT: Modelnaam is leeg voor {taskDescriptionForLogging}.");
                return Tuple.Create<string, long>(null, 0);
            }

            try
            {
                _logger.Log($"DEBUG: Prompt voor {taskDescriptionForLogging} (max 200 chars): {(prompt.Length > 200 ? prompt.Substring(0, 200) + "..." : prompt)}");

                string completion = await aiProvider.GetTextCompletionAsync(
                    prompt,
                    modelName,
                    modelParams.MaxTokens,
                    modelParams.Temperature,
                    cancellationToken
                );

                long tokensUsed = 0;
                if (!string.IsNullOrWhiteSpace(completion))
                {
                    tokensUsed = CalculateSimulatedTokens(prompt, completion);
                    _logger.Log($"DEBUG: Ruwe AI-antwoord voor {taskDescriptionForLogging}: '{completion?.Replace("\n", "\\n")}'. Tokens: {tokensUsed}");
                }
                else
                {
                    _logger.Log($"DEBUG: AI retourneerde een leeg of null antwoord voor {taskDescriptionForLogging}.");
                }
                return Tuple.Create(completion, tokensUsed);
            }
            catch (OperationCanceledException)
            {
                _logger.Log($"INFO: {taskDescriptionForLogging} geannuleerd.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Fout bij AI-aanroep voor {taskDescriptionForLogging}: {ex.Message}");
                return Tuple.Create<string, long>(null, 0);
            }
        }

        private string CleanAiResponse(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return rawResponse;
            }
            string cleaned = rawResponse;
            foreach (var prefix in AiResponsePrefixesToClean)
            {
                cleaned = Regex.Replace(cleaned, Regex.Escape(prefix), "", RegexOptions.IgnoreCase).Trim();
            }
            return cleaned.Trim('\'', '\"', '.', '-').Trim();
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

            if (aiProvider is OnnxRobBERTProvider && categoryEmbeddings != null)
            {
                _logger.Log("INFO: Embedding-gebaseerde classificatie (RobBERT/ONNX) wordt gebruikt.");
                OnnxRobBERTProvider robbertProvider = (OnnxRobBERTProvider)aiProvider;
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
                    float[] catEmbedding;
                    if (!categoryEmbeddings.TryGetValue(cat, out catEmbedding) || catEmbedding == null)
                    {
                        _logger.Log($"WAARSCHUWING: Geen of null embedding gevonden voor categorie '{cat}' in de dictionary.");
                        continue;
                    }

                    if (docEmbedding == null || catEmbedding.Length != docEmbedding.Length)
                    {
                        _logger.Log($"WAARSCHUWING: Ongeldige of niet-overeenkomende embedding voor categorie '{cat}'. Embedding lengtes: Cat={(catEmbedding != null ? catEmbedding.Length.ToString() : "null")}, Doc={(docEmbedding != null ? docEmbedding.Length.ToString() : "null")}. Overgeslagen.");
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

            _logger.Log("INFO: Prompt-based classificatie wordt gebruikt voor categorie.");
            if (string.IsNullOrWhiteSpace(textToClassify) && string.IsNullOrWhiteSpace(originalFilename))
            {
                _logger.Log("WAARSCHUWING: Geen tekst en geen bestandsnaam om te classificeren. Retourneer fallback categorie.");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            var validCategories = new List<string>(categories);
            if (!validCategories.Contains(DEFAULT_FALLBACK_CATEGORY, StringComparer.OrdinalIgnoreCase))
            {
                validCategories.Add(DEFAULT_FALLBACK_CATEGORY);
            }
            var categoryListForPrompt = string.Join("\n- ", validCategories.Distinct(StringComparer.OrdinalIgnoreCase));

            bool wasTextMeaningful;
            string aiInputText = GetRelevantTextForAI(textToClassify, originalFilename, 8000, out wasTextMeaningful);
            string textContext = GetTextContextForPrompt(aiInputText, originalFilename, wasTextMeaningful);
            var fewShotExamples = PromptSnippets.CategoryFewShotExamples;

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Je bent een AI-classificatiemodel. Je taak is om tekstfragmenten te classificeren in één van de exact opgegeven categorieën.");
            promptBuilder.AppendLine("**Retourneer uitsluitend de exacte categorienaam. Absoluut GEEN andere tekst, uitleg, nummering of opmaak (zoals quotes, opsommingstekens, of inleidende zinnen).**");
            promptBuilder.AppendLine("Gebruik de fallbackcategorie als geen enkele andere categorie duidelijk past.");
            promptBuilder.AppendLine("\nJe krijgt informatie over een document. Kies exact één van de volgende categorieën:\n");
            promptBuilder.AppendLine("<categories>");
            promptBuilder.AppendLine($"- {categoryListForPrompt}");
            promptBuilder.AppendLine("</categories>\n");
            promptBuilder.AppendLine("Regels:");
            promptBuilder.AppendLine("- Retourneer **ENKEL EN ALLEEN** één categorie uit bovenstaande lijst.");
            promptBuilder.AppendLine("- **GEEN uitleg, GEEN nummering, GEEN opmaak, GEEN inleidende zinnen (zoals 'De categorie is:').**");
            promptBuilder.AppendLine("- Als meerdere categorieën mogelijk zijn: kies de meest specifieke.");
            promptBuilder.AppendLine($"- Gebruik de fallbackcategorie ('{DEFAULT_FALLBACK_CATEGORY}') alleen als echt niets past.");
            promptBuilder.AppendLine("- Als het document geen leesbare inhoud heeft (<document_zonder_inhoud>), baseer je dan uitsluitend op de bestandsnaam en de context daarvan.\n");
            promptBuilder.AppendLine(fewShotExamples);
            promptBuilder.AppendLine("\nDocumentinformatie:");
            promptBuilder.AppendLine($"<bestandsnaam>\n{originalFilename}\n</bestandsnaam>\n");
            promptBuilder.AppendLine(textContext);
            promptBuilder.AppendLine("\nAntwoord: ");

            Tuple<string, long> aiResult = await CallAiProviderAsync(aiProvider, modelName, promptBuilder.ToString(),
                                                               AiTaskSettings.CategoryClassification, cancellationToken, "categorie classificatie");
            this.LastCallSimulatedTokensUsed = aiResult.Item2;
            string rawCompletion = aiResult.Item1;

            if (rawCompletion == null) return DEFAULT_FALLBACK_CATEGORY;

            string chosenCategory = CleanAiResponse(rawCompletion);

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

        public async Task<string> SuggestDetailedSubfolderAsync(
            string textToAnalyze,
            string originalFilename,
            string determinedCategoryKey,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0;
            long totalTokensForThisTask = 0;

            if (!ApplicationSettings.UseDetailedSubfolders)
            {
                _logger.Log("INFO: Gedetailleerde submappen zijn uitgeschakeld in ApplicationSettings.");
                return null;
            }

            _logger.Log($"INFO: Start gedetailleerde submap suggestie voor categorie key '{determinedCategoryKey}'.");

            bool wasTextMeaningful;
            string aiInputText = GetRelevantTextForAI(textToAnalyze, originalFilename, 2000, out wasTextMeaningful);
            string textContext = GetTextContextForPrompt(aiInputText, originalFilename, wasTextMeaningful);
            string documentType;

            string examplesForPrompt;
            ApplicationSettings.DetailedSubfolderPrompts.DocumentTypeExamples.TryGetValue(determinedCategoryKey, out examplesForPrompt);
            examplesForPrompt = examplesForPrompt ?? "- N.v.t.";

            string documentTypePrompt = ApplicationSettings.DetailedSubfolderPrompts.DocumentTypeBasePrompt
                .Replace("{category}", determinedCategoryKey)
                .Replace("{examples}", examplesForPrompt)
                .Replace("{originalFilename}", originalFilename)
                .Replace("{textContext}", textContext);

            Tuple<string, long> step1Result = await CallAiProviderAsync(aiProvider, modelName, documentTypePrompt,
                                                                  AiTaskSettings.DetailedSubfolderType, cancellationToken, $"document type voor {determinedCategoryKey}");
            totalTokensForThisTask += step1Result.Item2;
            string rawDocType = step1Result.Item1;
            documentType = CleanAiResponse(rawDocType);

            if (string.IsNullOrWhiteSpace(documentType) || documentType.Equals("Anders", StringComparison.OrdinalIgnoreCase) || rawDocType == null)
            {
                _logger.Log($"INFO: Geen specifiek documenttype geïdentificeerd (of 'Anders') voor '{determinedCategoryKey}'. Geen verdere submap generatie.");
                this.LastCallSimulatedTokensUsed = totalTokensForThisTask;
                return null;
            }
            _logger.Log($"DEBUG: AI document type binnen '{determinedCategoryKey}': '{documentType}'");

            string subfolderNamePromptTemplate = null;
            // Itereren over keys is robuuster voor C# 7.3 if de dictionary's custom comparer niet perfect werkt voor alle lookups.
            foreach (var kvpEntry in ApplicationSettings.DetailedSubfolderPrompts.SpecificSubfolderNamePrompts)
            {
                if (kvpEntry.Key.Item1.Equals(determinedCategoryKey, StringComparison.OrdinalIgnoreCase) &&
                    documentType.ToLowerInvariant().Contains(kvpEntry.Key.Item2.ToLowerInvariant()))
                {
                    subfolderNamePromptTemplate = kvpEntry.Value;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(subfolderNamePromptTemplate))
            {
                _logger.Log($"INFO: Geen specifieke submap prompt logica gevonden voor document type '{documentType}' in categorie '{determinedCategoryKey}'.");
                this.LastCallSimulatedTokensUsed = totalTokensForThisTask;
                return null;
            }

            string subfolderNamePrompt = subfolderNamePromptTemplate
                .Replace("{documentType}", documentType)
                .Replace("{category}", determinedCategoryKey)
                .Replace("{originalFilename}", originalFilename)
                .Replace("{textContext}", textContext);

            Tuple<string, long> step2Result = await CallAiProviderAsync(aiProvider, modelName, subfolderNamePrompt,
                                                                           AiTaskSettings.DetailedSubfolderName, cancellationToken, $"submap naam voor {documentType}");
            totalTokensForThisTask += step2Result.Item2;
            string rawSubfolderNamePart = step2Result.Item1;
            string suggestedSubfolderNamePart = CleanAiResponse(rawSubfolderNamePart);

            if (string.IsNullOrWhiteSpace(suggestedSubfolderNamePart) || rawSubfolderNamePart == null)
            {
                _logger.Log($"INFO: AI gaf geen bruikbare specifieke submapnaam voor '{documentType}'.");
                this.LastCallSimulatedTokensUsed = totalTokensForThisTask;
                return null;
            }

            _logger.Log($"DEBUG: AI specifieke submapnaam deel: '{suggestedSubfolderNamePart}'");

            string basePathForSubfolder = PluralizeDocumentType(documentType);
            string finalDetailedPath = Path.Combine(basePathForSubfolder, FileUtils.SanitizeFolderOrFileName(suggestedSubfolderNamePart));

            this.LastCallSimulatedTokensUsed = totalTokensForThisTask;
            _logger.Log($"INFO: Gedetailleerd subpad voorgesteld: '{finalDetailedPath}'");
            return finalDetailedPath;
        }

        [Obsolete("SuggestSubfolderNameAsync is verouderd. Gebruik SuggestDetailedSubfolderAsync voor AI-gebaseerde submap suggesties.")]
        public Task<string> SuggestSubfolderNameAsync(
            string textToAnalyze,
            string originalFilename,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0;
            _logger.Log($"INFO: Standaard AI-suggestie voor submapnaam voor '{originalFilename}' wordt overgeslagen (gebruik SuggestDetailedSubfolderAsync).");
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
            long totalTokensForThisTask = 0;
            string originalFilenameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilename);

            bool wasTextMeaningful;
            string aiInputText = GetRelevantTextForAI(textToAnalyze, originalFilename, 2000, out wasTextMeaningful);
            string textContext = GetTextContextForPrompt(aiInputText, originalFilename, wasTextMeaningful);

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("### SYSTEM INSTRUCTIE");
            promptBuilder.AppendLine("Je bent een AI-assistent die helpt bij het organiseren van bestanden.");
            promptBuilder.AppendLine("Je taak is om een **korte en beschrijvende bestandsnaam (zonder extensie)** te suggereren op basis van de documentinhoud of de oorspronkelijke bestandsnaam als fallback.\n");
            promptBuilder.AppendLine("### INSTRUCTIES");
            promptBuilder.AppendLine("- Gebruik maximaal **5 woorden**.");
            promptBuilder.AppendLine("- Vat het hoofdonderwerp of doel van het document bondig samen.");
            promptBuilder.AppendLine($"- Vermijd generieke termen zoals '{string.Join("', '", ApplicationSettings.GenericFilenameTerms.Where(t => !string.IsNullOrEmpty(t)))}' of alleen een datum zonder context.");
            promptBuilder.AppendLine("- Gebruik bij voorkeur betekenisvolle termen zoals 'Belastingaangifte 2023' of 'CV Jan Jansen'.");
            promptBuilder.AppendLine("- **GEEF ENKEL DE BESTANDSNAAM TERUG – GEEN uitleg, GEEN opmaak, GEEN opsomming, GEEN quotes, GEEN bestandsextensie, GEEN inleidende zinnen (zoals 'De bestandsnaam is:').**");
            promptBuilder.AppendLine("- Als het document geen leesbare inhoud heeft (<document_zonder_inhoud>), focus dan op de originele bestandsnaam en de algemene beschrijving in dat blok.\n");
            promptBuilder.AppendLine("### FEW-SHOT VOORBEELDEN");
            promptBuilder.AppendLine(PromptSnippets.FilenameFewShotExamples);
            promptBuilder.AppendLine("\n### INPUT");
            promptBuilder.AppendLine($"<bestandsnaam>\n{originalFilename}\n</bestandsnaam>\n");
            promptBuilder.AppendLine(textContext);
            promptBuilder.AppendLine("\nAntwoord: ");
            string basePrompt = promptBuilder.ToString();

            Tuple<string, long> nameResult = await CallAiProviderAsync(aiProvider, modelName, basePrompt,
                                                                    AiTaskSettings.EffectiveFilename, cancellationToken, $"bestandsnaam suggestie voor {originalFilename}");
            totalTokensForThisTask += nameResult.Item2;
            string rawSuggestedName = nameResult.Item1;
            string suggestedName = CleanAiResponse(rawSuggestedName);
            string cleanedName = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");

            bool needsRetry = string.IsNullOrWhiteSpace(cleanedName) ||
                              cleanedName.Length < ApplicationSettings.MinSubfolderNameLength ||
                              ApplicationSettings.GenericFilenameTerms.Any(g => cleanedName.Equals(g, StringComparison.OrdinalIgnoreCase)) ||
                              Regex.IsMatch(cleanedName, @"^\d{1,2}[-/]\d{1,2}[-/]\d{2,4}$") ||
                              Regex.IsMatch(cleanedName, @"^\d{4}[-/]\d{1,2}[-/]\d{1,2}$");

            if (needsRetry && rawSuggestedName != null)
            {
                _logger.Log($"INFO: Eerste AI-suggestie voor bestandsnaam '{suggestedName?.Trim() ?? "[LEEG]"}' voor '{originalFilename}' was onbruikbaar. Start retry...");
                string retryPrompt = basePrompt + "\n\nDe vorige suggestie was niet bruikbaar. Geef nu een CONCRETE, KORTE EN BESCHRIJVENDE bestandsnaam (zonder extensie). De output moet DIRECT de naam zijn, niet alleen een datum.";

                Tuple<string, long> retryNameResult = await CallAiProviderAsync(aiProvider, modelName, retryPrompt,
                                                                       AiTaskSettings.EffectiveFilename, cancellationToken, $"bestandsnaam suggestie (retry) voor {originalFilename}");
                totalTokensForThisTask += retryNameResult.Item2;
                // rawSuggestedName = retryNameResult.Item1; // Commented out - do not overwrite if retry fails
                suggestedName = CleanAiResponse(retryNameResult.Item1); // Clean the retry result
                cleanedName = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");

                // If retry still results in unusable name, revert to original for safety
                if (string.IsNullOrWhiteSpace(cleanedName) ||
                    cleanedName.Length < ApplicationSettings.MinSubfolderNameLength ||
                    ApplicationSettings.GenericFilenameTerms.Any(g => cleanedName.Equals(g, StringComparison.OrdinalIgnoreCase)) ||
                    Regex.IsMatch(cleanedName, @"^\d{1,2}[-/]\d{1,2}[-/]\d{2,4}$") ||
                    Regex.IsMatch(cleanedName, @"^\d{4}[-/]\d{1,2}[-/]\d{1,2}$"))
                {
                    _logger.Log($"INFO: Retry AI-suggestie voor bestandsnaam was ook onbruikbaar voor '{originalFilename}'.");
                    cleanedName = originalFilenameWithoutExtension; // Fallback to original
                }
            }
            else if (rawSuggestedName == null)
            {
                _logger.Log($"INFO: Eerste AI-aanroep voor bestandsnaam voor '{originalFilename}' mislukt. Gebruik originele naam.");
                cleanedName = originalFilenameWithoutExtension;
            }

            this.LastCallSimulatedTokensUsed = totalTokensForThisTask;

            // Final check, in case the first attempt was good enough and no retry happened, or if retry failed.
            if (string.IsNullOrWhiteSpace(cleanedName) ||
                cleanedName.Length < ApplicationSettings.MinSubfolderNameLength ||
                ApplicationSettings.GenericFilenameTerms.Any(g => cleanedName.Equals(g, StringComparison.OrdinalIgnoreCase)) ||
                Regex.IsMatch(cleanedName, @"^\d{1,2}[-/]\d{1,2}[-/]\d{2,4}$") ||
                Regex.IsMatch(cleanedName, @"^\d{4}[-/]\d{1,2}[-/]\d{1,2}$"))
            {
                if (!cleanedName.Equals(originalFilenameWithoutExtension)) // Log only if it was different and then deemed unusable
                {
                    _logger.Log($"WAARSCHUWING: AI faalde een bruikbare bestandsnaam te genereren voor '{originalFilename}'. Gebruik originele naam: '{originalFilenameWithoutExtension}'.");
                }
                cleanedName = originalFilenameWithoutExtension;
            }


            if (cleanedName.Length > ApplicationSettings.MaxFilenameLength)
            {
                cleanedName = cleanedName.Substring(0, ApplicationSettings.MaxFilenameLength);
                int lastSpace = cleanedName.LastIndexOf(' ');
                if (lastSpace > 0 && lastSpace > ApplicationSettings.MaxFilenameLength / 2)
                {
                    cleanedName = cleanedName.Substring(0, lastSpace);
                }
                _logger.Log($"INFO: AI-gegenereerde bestandsnaam voor '{originalFilename}' afgekort naar '{cleanedName}' wegens lengtebeperking ({ApplicationSettings.MaxFilenameLength} chars).");
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
                dot += (double)v1[i] * v2[i]; // Cast to double for precision
                mag1 += Math.Pow(v1[i], 2);
                mag2 += Math.Pow(v2[i], 2);
            }
            if (mag1 == 0.0 || mag2 == 0.0) return 0.0;
            double similarity = dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
            return Math.Max(-1.0, Math.Min(1.0, similarity));
        }

        private string GetTextContextForPrompt(string aiInputText, string originalFilename, bool wasTextMeaningful)
        {
            return wasTextMeaningful ?
               $@"<tekst_inhoud>
{aiInputText}
</tekst_inhoud>" :
               $@"<document_zonder_inhoud>
Dit document heeft de bestandsnaam '{Path.GetFileNameWithoutExtension(originalFilename)}'. Er kon geen inhoud uit het document worden geëxtraheerd, of de inhoud was leeg/niet-betekenisvol. Analyseer alleen de bestandsnaam en probeer daaruit de essentie te halen.
</document_zonder_inhoud>";
        }

        private string GetRelevantTextForAI(string extractedText, string originalFilename, int maxLength, out bool wasTextMeaningful)
        {
            const int MinMeaningfulTextLength = 20;
            if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Trim().Length < MinMeaningfulTextLength)
            {
                wasTextMeaningful = false;
                return Path.GetFileNameWithoutExtension(originalFilename);
            }
            else
            {
                wasTextMeaningful = true;
                string cleanText = Regex.Replace(extractedText, @"\s+", " ").Trim();
                return cleanText.Length <= maxLength ? cleanText : cleanText.Substring(0, maxLength);
            }
        }

        private long CalculateSimulatedTokens(string prompt, string completion)
        {
            if (string.IsNullOrEmpty(prompt) && string.IsNullOrEmpty(completion)) return 0;
            long promptTokens = string.IsNullOrEmpty(prompt) ? 0 : (prompt.Length / 4) + 1;
            long completionTokens = string.IsNullOrEmpty(completion) ? 0 : (completion.Length / 4) + 1;
            return promptTokens + completionTokens;
        }

        private string PluralizeDocumentType(string documentType)
        {
            if (string.IsNullOrWhiteSpace(documentType)) return "Overige Documenten";

            string plural;
            if (ApplicationSettings.DocumentTypePlurals.TryGetValue(documentType, out plural))
            {
                return plural;
            }

            string lowerDocType = documentType.ToLowerInvariant();
            if (lowerDocType.Length == 0) return documentType + "en";

            char lastChar = lowerDocType.Last();
            char secondLastChar = lowerDocType.Length > 1 ? lowerDocType[lowerDocType.Length - 2] : '\0';

            if (documentType.EndsWith("f", StringComparison.OrdinalIgnoreCase) &&
                !lowerDocType.Equals("of") && !lowerDocType.Equals("gif") && !lowerDocType.Equals("chef"))
                return documentType.Substring(0, documentType.Length - 1) + "ven";

            if (documentType.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
                documentType.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
                documentType.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
                (documentType.EndsWith("ch", StringComparison.OrdinalIgnoreCase) && !lowerDocType.Equals("lach")) ||
                documentType.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
            {
                if (lowerDocType.EndsWith("ssen") || lowerDocType.EndsWith("xen") || lowerDocType.EndsWith("zen")) return documentType;
                return documentType + "en";
            }

            if (documentType.EndsWith("y", StringComparison.OrdinalIgnoreCase) && documentType.Length > 1 &&
                !"aeiou".Contains(secondLastChar.ToString(CultureInfo.InvariantCulture))) // Use InvariantCulture for char comparison
                return documentType.Substring(0, documentType.Length - 1) + "ies";

            if ("aoui".Contains(lastChar.ToString(CultureInfo.InvariantCulture)) && lastChar != 'e')
                return documentType + "s";

            if (lastChar == 'e')
            {
                if (lowerDocType.EndsWith("ie")) return documentType.Substring(0, documentType.Length - 1) + "ieën"; // Correct plural for -ie
                return documentType + "n";
            }
            return documentType + "en";
        }
    }

    // Kleine helper klasse voor prompt snippets, kan uitgebreid worden.
    // Kan ook in een apart bestand of direct in ApplicationSettings als strings.
    public static class PromptSnippets
    {
        public const string CategoryFewShotExamples = @"
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
Antwoord: Persoonlijke Documenten";

        public const string FilenameFewShotExamples = @"
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
</voorbeeld>";
    }
}