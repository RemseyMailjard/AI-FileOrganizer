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

        // AI-parameters voor hoofdcategorie
        private const int CATEGORY_MAX_TOKENS = 50;
        private const float CATEGORY_TEMPERATURE = 0.0f;

        // AI-parameters voor bestandsnaam (voorheen subfolder)
        private const int EFFECTIVE_FILENAME_MAX_TOKENS = 20;
        private const float EFFECTIVE_FILENAME_TEMPERATURE = 0.2f;

        // AI-parameters voor gedetailleerde submappen (nieuw)
        private const int DETAILED_SUBFOLDER_TYPE_MAX_TOKENS = 15; // Voor document type identificatie
        private const float DETAILED_SUBFOLDER_TYPE_TEMPERATURE = 0.1f;
        private const int DETAILED_SUBFOLDER_NAME_MAX_TOKENS = 25; // Voor specifieke submapnaam
        private const float DETAILED_SUBFOLDER_NAME_TEMPERATURE = 0.3f;


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
            this.LastCallSimulatedTokensUsed = 0; // Reset voor deze hoofdtaak

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
                    if (catEmbedding == null || docEmbedding == null || catEmbedding.Length != docEmbedding.Length)
                    {
                        _logger.Log($"WAARSCHUWING: Ongeldige of niet-overeenkomende embedding voor categorie '{cat}'. Embedding lengtes: Cat={catEmbedding?.Length}, Doc={docEmbedding?.Length}. Overgeslagen.");
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

            _logger.Log("INFO: Prompt-based classificatie (GPT/Gemini/OpenAI) wordt gebruikt voor categorie.");
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
                    this.LastCallSimulatedTokensUsed = CalculateSimulatedTokens(prompt, chosenCategory);
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

        /// <summary>
        /// Stelt een gedetailleerde submap voor gebaseerd op een tweestaps AI-analyse.
        /// Retourneert een relatief pad (bijv. "Facturen\Verkoopfacturen Februari 2024") of null.
        /// </summary>
        public async Task<string> SuggestDetailedSubfolderAsync(
            string textToAnalyze,
            string originalFilename,
            string determinedCategory, // De al bepaalde hoofdcategorie
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken)
        {
            // Reset token count voor deze specifieke sub-taak
            // De totale tokens worden in de FileOrganizerService geaccumuleerd
            long currentTaskTokens = 0;

            if (aiProvider == null || string.IsNullOrWhiteSpace(modelName))
            {
                _logger.Log("FOUT: AI Provider of modelnaam ongeldig voor gedetailleerde submap suggestie.");
                this.LastCallSimulatedTokensUsed = 0; // Zorg dat het gereset is als we hieruit stappen
                return null;
            }

            _logger.Log($"INFO: Start gedetailleerde submap suggestie voor categorie '{determinedCategory}'.");

            bool wasTextMeaningful;
            string aiInputText = GetRelevantTextForAI(textToAnalyze, originalFilename, 2000, out wasTextMeaningful); // Iets meer tekst voor context

            string textContext = wasTextMeaningful ?
                $@"<tekst_inhoud>
{aiInputText}
</tekst_inhoud>" :
                $@"<document_zonder_inhoud>
{aiInputText}
</document_zonder_inhoud>";

            string documentType = null;

            // --- Stap 1: Identificeer het specifieke documenttype binnen de hoofdcategorie ---
            if (determinedCategory.Equals("Financiën", StringComparison.OrdinalIgnoreCase) ||
                determinedCategory.Equals("Belastingen", StringComparison.OrdinalIgnoreCase) ||
                determinedCategory.Equals("Verzekeringen", StringComparison.OrdinalIgnoreCase)) // Voeg meer categorieën toe waarvoor je dit wilt
            {
                string documentTypePrompt = $@"
Analyseer het document binnen de categorie '{determinedCategory}'.
Identificeer het specifieke type document. Mogelijke types voor '{determinedCategory}' zijn bijvoorbeeld:
- Voor Financiën: Factuur, Bankafschrift, Offerte, Leningsovereenkomst, Jaaropgave, Onkostennota, Salarisspecificatie, Beleggingsoverzicht
- Voor Belastingen: Belastingaangifte, Belastingaanslag, Voorlopige aanslag, Toeslagbeschikking
- Voor Verzekeringen: Polisblad, Schadeclaim, Verzekeringsvoorwaarden, Opzegging
- Anders (geef een korte, specifieke beschrijving van maximaal 3 woorden als het niet past)

Documentinformatie:
<bestandsnaam>
{originalFilename}
</bestandsnaam>
{textContext}

Antwoord (alleen het type, bijv. 'Factuur' of 'Belastingaangifte'): ";

                try
                {
                    _logger.Log($"DEBUG: Prompt voor document type (cat: {determinedCategory}): {documentTypePrompt.Substring(0, Math.Min(200, documentTypePrompt.Length))}...");
                    documentType = await aiProvider.GetTextCompletionAsync(
                        documentTypePrompt, modelName, DETAILED_SUBFOLDER_TYPE_MAX_TOKENS, DETAILED_SUBFOLDER_TYPE_TEMPERATURE, cancellationToken);

                    if (documentType != null) currentTaskTokens += CalculateSimulatedTokens(documentTypePrompt, documentType);

                    documentType = documentType?.Trim('\'', '\"', '.', '-').Trim();
                    _logger.Log($"DEBUG: AI document type binnen '{determinedCategory}': '{documentType}'");
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT bij AI-aanroep voor document type (cat: {determinedCategory}): {ex.Message}");
                    this.LastCallSimulatedTokensUsed = currentTaskTokens;
                    return null;
                }
            }
            else
            {
                _logger.Log($"INFO: Geen specifieke document type logica geïmplementeerd voor categorie '{determinedCategory}'.");
                this.LastCallSimulatedTokensUsed = 0; // Geen AI call gedaan voor deze stap
                return null;
            }

            if (string.IsNullOrWhiteSpace(documentType) || documentType.Equals("Anders", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Log($"INFO: Geen specifiek documenttype geïdentificeerd of 'Anders'. Geen verdere submap generatie.");
                this.LastCallSimulatedTokensUsed = currentTaskTokens;
                return null;
            }

            // --- Stap 2: Genereer een specifieke submapnaam gebaseerd op het documenttype ---
            string suggestedSubfolderNamePart = null;
            string subfolderPromptForSpecificType = ""; // Placeholder

            if (determinedCategory.Equals("Financiën", StringComparison.OrdinalIgnoreCase) &&
                documentType.ToLowerInvariant().Contains("factuur")) // Ruime check op "factuur"
            {
                subfolderPromptForSpecificType = $@"
Het document is een '{documentType}' (categorie: {determinedCategory}). 
Stel een submapnaam voor van maximaal 3-4 woorden die deze factuur goed beschrijft.
Focus op:
1. Type factuur (bijv. Verkoop, Inkoop, Credit) - als dit niet al in '{documentType}' zit.
2. Maand en Jaar (bijv. Februari 2024, Q1 2023).
3. Eventueel de naam van de klant/leverancier als die prominent en kort is.

Voorbeelden:
- Verkoop Februari 2024
- Inkoop BedrijfX Maart 2023
- Creditnota's Q2

Documentinformatie:
<bestandsnaam>
{originalFilename}
</bestandsnaam>
{textContext}

Antwoord (alleen de beschrijvende submapnaam, bijv. 'Verkoop Maart 2024' of 'KlantY Januari'): ";
            }
            // VOEG HIER MEER `ELSE IF` BLOKKEN TOE voor andere combinaties van `determinedCategory` en `documentType`
            // Voorbeeld voor Belastingaangifte:
            else if (determinedCategory.Equals("Belastingen", StringComparison.OrdinalIgnoreCase) &&
                     documentType.ToLowerInvariant().Contains("aangifte"))
            {
                subfolderPromptForSpecificType = $@"
Het document is een '{documentType}' (categorie: {determinedCategory}). 
Stel een submapnaam voor van maximaal 2-3 woorden. Focus op het jaartal.
Voorbeeld:
- 2023
- IB 2022

Documentinformatie:
<bestandsnaam>
{originalFilename}
</bestandsnaam>
{textContext}

Antwoord (alleen de submapnaam, bijv. '2023'): ";
            }
            // Einde voorbeeld

            if (!string.IsNullOrWhiteSpace(subfolderPromptForSpecificType))
            {
                try
                {
                    _logger.Log($"DEBUG: Prompt voor specifieke submap (type: {documentType}): {subfolderPromptForSpecificType.Substring(0, Math.Min(200, subfolderPromptForSpecificType.Length))}...");
                    suggestedSubfolderNamePart = await aiProvider.GetTextCompletionAsync(
                        subfolderPromptForSpecificType, modelName, DETAILED_SUBFOLDER_NAME_MAX_TOKENS, DETAILED_SUBFOLDER_NAME_TEMPERATURE, cancellationToken);

                    if (suggestedSubfolderNamePart != null) currentTaskTokens += CalculateSimulatedTokens(subfolderPromptForSpecificType, suggestedSubfolderNamePart);

                    suggestedSubfolderNamePart = suggestedSubfolderNamePart?.Trim('\'', '\"', '.', '-').Trim();
                    _logger.Log($"DEBUG: AI specifieke submapnaam deel: '{suggestedSubfolderNamePart}'");

                    if (!string.IsNullOrWhiteSpace(suggestedSubfolderNamePart))
                    {
                        // Construeer het volledige relatieve subpad
                        // Bijvoorbeeld: "Facturen\Verkoop Februari 2024"
                        // Of: "Belastingaangiften\2023"
                        string basePathForSubfolder = PluralizeDocumentType(documentType); // Maak een helper hiervoor
                        string finalDetailedPath = Path.Combine(basePathForSubfolder, FileUtils.SanitizeFolderOrFileName(suggestedSubfolderNamePart));

                        this.LastCallSimulatedTokensUsed = currentTaskTokens;
                        _logger.Log($"INFO: Gedetailleerd subpad voorgesteld: '{finalDetailedPath}'");
                        return finalDetailedPath;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT bij AI-aanroep voor specifieke submap (type: {documentType}): {ex.Message}");
                }
            }
            else
            {
                _logger.Log($"INFO: Geen specifieke submap prompt logica voor document type '{documentType}' in categorie '{determinedCategory}'.");
            }

            this.LastCallSimulatedTokensUsed = currentTaskTokens; // Update tokens ook als we hier eindigen
            _logger.Log($"INFO: Kon geen gedetailleerde submap genereren voor '{originalFilename}'.");
            return null;
        }


        public Task<string> SuggestSubfolderNameAsync( // Deze blijft bestaan voor compatibiliteit, maar wordt niet meer gebruikt voor AI-submappen
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
            this.LastCallSimulatedTokensUsed = 0; // Reset voor deze hoofdtaak
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
            long currentTaskTokens = 0;

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
                    currentTaskTokens = CalculateSimulatedTokens(prompt, suggestedName);
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
                this.LastCallSimulatedTokensUsed = currentTaskTokens;
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
                        currentTaskTokens += CalculateSimulatedTokens(retryPrompt, suggestedName); // Accumuleer tokens van retry
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
            this.LastCallSimulatedTokensUsed = currentTaskTokens;


            if (string.IsNullOrWhiteSpace(cleanedName) ||
                cleanedName.Length < 3 ||
                generiek.Any(g => cleanedName.Equals(g, StringComparison.OrdinalIgnoreCase)) ||
                Regex.IsMatch(cleanedName, @"^\d{1,2}[-/]\d{1,2}[-/]\d{2,4}$") ||
                Regex.IsMatch(cleanedName, @"^\d{4}[-/]\d{1,2}[-/]\d{1,2}$"))
            {
                _logger.Log($"WAARSCHUWING: AI faalde een bruikbare bestandsnaam te genereren voor '{originalFilename}'. Gebruik originele naam.");
                cleanedName = originalFilenameWithoutExtension;
            }

            int maxFilenameLength = 100; // Haal dit idealiter uit ApplicationSettings
            if (cleanedName.Length > maxFilenameLength)
            {
                cleanedName = cleanedName.Substring(0, maxFilenameLength);
                int lastSpace = cleanedName.LastIndexOf(' ');
                if (lastSpace > maxFilenameLength / 2 && lastSpace > 0) // Alleen als het niet te kort wordt
                {
                    cleanedName = cleanedName.Substring(0, lastSpace);
                }
                _logger.Log($"INFO: AI-gegenereerde bestandsnaam voor '{originalFilename}' afgekort naar '{cleanedName}' wegens lengtebeperking.");
            }

            if (!cleanedName.Equals(originalFilenameWithoutExtension, StringComparison.Ordinal)) // Gebruik Ordinal voor exacte vergelijking
            {
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanedName.ToLowerInvariant());
            }
            return cleanedName; // Retourneer met originele casing als het niet veranderd is
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
            if (string.IsNullOrWhiteSpace(extractedText) || extractedText.Trim().Length < 10) // Beschouw zeer korte tekst als niet-betekenisvol
            {
                wasTextMeaningful = false;
                return $"Dit document heeft de bestandsnaam '{Path.GetFileNameWithoutExtension(originalFilename)}'. Er kon geen inhoud uit het document worden geëxtraheerd, of de inhoud was leeg/niet-betekenisvol. Analyseer alleen de bestandsnaam en probeer daaruit de essentie te halen.";
            }
            else
            {
                wasTextMeaningful = true;
                string cleanText = Regex.Replace(extractedText, @"\s+", " ").Trim(); // Normaliseer witruimte
                return cleanText.Length <= maxLength ? cleanText : cleanText.Substring(0, maxLength);
            }
        }

        private long CalculateSimulatedTokens(string prompt, string completion)
        {
            if (prompt == null || completion == null) return 0;
            // Ruwe schatting: 1 token per ~4 karakters. Dit is zeer afhankelijk van de taal en het model.
            // Voor een nauwkeurigere telling zou je een tokenizer specifiek voor het gebruikte model moeten gebruiken.
            return (prompt.Length / 4) + (completion.Length / 4);
        }

        // Helper voor pluralisatie (zeer basaal, uitbreiden indien nodig)
        private string PluralizeDocumentType(string documentType)
        {
            if (string.IsNullOrWhiteSpace(documentType)) return "Overige Documenten";

            // Eenvoudige regels, niet uitputtend
            if (documentType.EndsWith("f", StringComparison.OrdinalIgnoreCase)) // bijv. Brief -> Brieven
                return documentType.Substring(0, documentType.Length -1) + "ven";
            if (documentType.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
                documentType.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
                documentType.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
                documentType.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
                documentType.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
                return documentType + "en"; // Polis -> Polissen
            if (documentType.EndsWith("y", StringComparison.OrdinalIgnoreCase) && documentType.Length > 1 && !"aeiou".Contains(documentType.ToLowerInvariant()[documentType.Length-2])) // family -> families
                return documentType.Substring(0, documentType.Length -1) + "ies";

            // Algemene regel: voeg 'en' toe, of 's' als het eindigt op een klinker (behalve 'e')
            char lastChar = documentType.ToLowerInvariant().Last();
            if ("aoui".Contains(lastChar))
                return documentType + "s"; // Auto -> Auto's (apostrof S is lastiger, simpel 's')

            // Standaard is 'en' toevoegen voor veel Nederlandse woorden
            if (documentType.EndsWith("e", StringComparison.OrdinalIgnoreCase))
                return documentType + "n"; // Offerte -> Offerten

            return documentType + "en"; // Factuur -> Facturen
        }
    }
}