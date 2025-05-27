// AI_FileOrganizer/Services/AiClassificationService.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO; // Nodig voor Path.GetFileNameWithoutExtension
using System.Linq;
using System.Text.RegularExpressions; // NIEUW: Nodig voor Regex.Replace
using System.Threading;
using System.Threading.Tasks;

using AI_FileOrganizer.Utils; // Nodig voor FileUtils en ILogger

namespace AI_FileOrganizer.Services
{
    public class AiClassificationService
    {
        private readonly ILogger _logger;

        private const string DEFAULT_FALLBACK_CATEGORY = "Overig";

        // Nieuwe constanten voor de AI-parameters, per taak
        private const int CATEGORY_MAX_TOKENS = 50;
        private const float CATEGORY_TEMPERATURE = 0.0f; // Lager voor precieze classificatie

        private const int SUBFOLDER_MAX_TOKENS = 20;
        private const float SUBFOLDER_TEMPERATURE = 0.2f; // Iets hoger voor creativiteit, maar nog steeds gericht

        private const int FILENAME_MAX_TOKENS = 30;
        private const float FILENAME_TEMPERATURE = 0.3f; // Nog iets hoger voor creativiteit

        // <<< NEW PROPERTY >>>
        public long LastCallSimulatedTokensUsed { get; private set; }


        public AiClassificationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LastCallSimulatedTokensUsed = 0; // Initialize
        }

        /// <summary>
        /// Helpt bij het voorbereiden van tekst voor de AI door te controleren op zinvolle inhoud.
        /// Geeft een fallback-tekst terug met instructies als de originele tekst niet zinvol is.
        /// </summary>
        /// <param name="extractedText">De reeds geëxtraheerde tekst uit het document.</param>
        /// <param name="originalFilename">De originele bestandsnaam, gebruikt voor fallback context.</param>
        /// <param name="maxLength">Maximale lengte van de tekst die naar de AI wordt gestuurd.</param>
        /// <param name="wasTextMeaningful">Output parameter die aangeeft of de originele tekst zinvol was.</param>
        /// <returns>De tekst die naar de AI moet worden gestuurd.</returns>
        private string GetRelevantTextForAI(string extractedText, string originalFilename, int maxLength, out bool wasTextMeaningful)
        {
            // Controleer of de geëxtraheerde tekst zinvolle karakters bevat na trimmen
            if (string.IsNullOrWhiteSpace(extractedText?.Trim()))
            {
                wasTextMeaningful = false;
                // Geef de AI een expliciete instructie dat er geen tekstinhoud is en dat de focus op de bestandsnaam moet liggen.
                return $"Dit document heeft de bestandsnaam '{Path.GetFileNameWithoutExtension(originalFilename)}'. Er kon geen inhoud uit het document worden geëxtraheerd, of de inhoud was leeg. Analyseer alleen de bestandsnaam en probeer daaruit de essentie te halen.";
            }
            else
            {
                wasTextMeaningful = true;
                // Retourneer een afgekorte versie van de geëxtraheerde tekst
                return extractedText.Substring(0, Math.Min(extractedText.Length, maxLength));
            }
        }


        // ======= Publieke AI-methodes =======

        /// <summary>
        /// Classificeert de categorie van een document op basis van de tekstinhoud of bestandsnaam.
        /// </summary>
        /// <param name="textToClassify">De geëxtraheerde tekstinhoud van het document.</param>
        /// <param name="originalFilename">De originele bestandsnaam van het document. BELANGRIJK: Deze parameter is NIEUW.</param>
        /// <param name="categories">Lijst van mogelijke categorieën.</param>
        /// <param name="aiProvider">De AI-provider om de classificatie uit te voeren.</param>
        /// <param name="modelName">De naam van het AI-model.</param>
        /// <param name="cancellationToken">Token om de operatie te annuleren.</param>
        /// <returns>De geclassificeerde categorienaam.</returns>
        public async Task<string> ClassifyCategoryAsync(
            string textToClassify,
            string originalFilename, // NIEUW: originalFilename is nu een parameter
            List<string> categories,
            IAiProvider aiProvider,
            string modelName,
            CancellationToken cancellationToken)
        {
            this.LastCallSimulatedTokensUsed = 0; // Reset for this call

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
</document_zonder_inhode>";

            // AANGEPAST: Few-shot voorbeelden genereren nu ALLEEN de categorienaam.
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

Antwoord: "; // AANGEPAST: Maak de laatste promptregel consistenter met de voorbeelden

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
                // <<< SIMULATE TOKEN USAGE >>>
                if (chosenCategory != null)
                {
                    this.LastCallSimulatedTokensUsed = (prompt.Length / 4) + (chosenCategory.Length / 4); // Simple estimation
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

            // GECORRIGEERD: Gebruik Regex.Replace voor case-insensitive vervanging
            if (!string.IsNullOrWhiteSpace(chosenCategory))
            {
                // Verwijder specifieke prefixes die de AI mogelijk nog toevoegt
                chosenCategory = Regex.Replace(chosenCategory, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                chosenCategory = Regex.Replace(chosenCategory, "Categorie:", "", RegexOptions.IgnoreCase).Trim();
                // Verwijder eventuele quotes als de AI deze onverhoopt toevoegt
                chosenCategory = chosenCategory.Trim('\'', '\"');
            }


            if (string.IsNullOrWhiteSpace(chosenCategory))
            {
                _logger.Log("WAARSCHUWING: AI retourneerde geen bruikbare categorie (leeg of witruimte). Val terug op default.");
                return DEFAULT_FALLBACK_CATEGORY;
            }

            // `chosenCategory` is al getrimd door de bovenstaande opschoning, maar deze regel kan blijven voor consistentie.
            // chosenCategory = chosenCategory.Trim(); 

            if (validCategories.Contains(chosenCategory))
                return chosenCategory;

            // Verbeterde fuzzy matching: controleer op containment en gelijkenis
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
            this.LastCallSimulatedTokensUsed = 0; // Reset for this call

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
            string aiInputText = GetRelevantTextForAI(textToAnalyze, originalFilename, 2000, out wasTextMeaningful); // Max 2000 chars

            // Pas de prompt aan op basis van de aanwezigheid van zinvolle tekst
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

Antwoord: "; // AANGEPAST: Maak de laatste promptregel consistenter met de voorbeelden

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
                // <<< SIMULATE TOKEN USAGE >>>
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

            // GECORRIGEERD: Gebruik Regex.Replace voor case-insensitive vervanging
            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = Regex.Replace(suggestedName, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                suggestedName = suggestedName.Trim('\'', '\"');
            }

            string cleaned = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");
            var generiek = new[] { "document", "bestand", "info", "overig", "algemeen", "diversen", "" };

            // Controleer of de eerste AI-suggestie bruikbaar is.
            bool needsRetry = string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 3 || generiek.Contains(cleaned.ToLowerInvariant());

            if (needsRetry)
            {
                _logger.Log($"INFO: Eerste AI-suggestie '{suggestedName?.Trim() ?? "[LEEG]"}' voor '{originalFilename}' was onbruikbaar (leeg/te kort/generiek). Start retry...");
                long previousTokens = this.LastCallSimulatedTokensUsed; // Store tokens from first attempt

                // Sterkere retry prompt om de AI te dwingen een bruikbare naam te geven.
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

                    // <<< SIMULATE TOKEN USAGE (RETRY) >>>
                    if (suggestedName != null)
                    {
                        this.LastCallSimulatedTokensUsed = previousTokens + (retryPrompt.Length / 4) + (suggestedName.Length / 4);
                    }


                    _logger.Log($"DEBUG: Ruwe AI-antwoord voor submapnaam (retry): '{suggestedName?.Replace("\n", "\\n")}'.");
                    // GECORRIGEERD: Opschoning na retry met Regex.Replace
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

            // Finale validatie na (eventuele) retry
            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Length < 3 || generiek.Contains(cleaned.ToLowerInvariant()))
            {
                _logger.Log($"INFO: AI faalde voor '{originalFilename}' na retry of initieel. Probeer patroon-gebaseerde fallback...");

                cleaned = FileUtils.FallbackFolderNameFromFilename(originalFilename);

                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    _logger.Log($"WAARSCHUWING: Geen bruikbare submapnaam gevonden voor '{originalFilename}'. Bestand blijft mogelijk in hoofdmap van de categorie.");
                    return null; // Geen bruikbare submapnaam, laat hoger niveau bepalen
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
            this.LastCallSimulatedTokensUsed = 0; // Reset for this call

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

            bool wasTextMeaningful;
            string aiInputText = GetRelevantTextForAI(textToAnalyze, originalFilename, 2000, out wasTextMeaningful); // Max 2000 chars

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

Antwoord: "; // AANGEPAST: Maak de laatste promptregel consistenter met de voorbeelden

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
                // <<< SIMULATE TOKEN USAGE >>>
                if (suggestedName != null)
                {
                    this.LastCallSimulatedTokensUsed = (prompt.Length / 4) + (suggestedName.Length / 4);
                }
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

            _logger.Log($"DEBUG: Ruwe AI-antwoord voor bestandsnaam: '{suggestedName?.Replace("\n", "\\n")}'.");

            // GECORRIGEERD: Gebruik Regex.Replace voor case-insensitive vervanging
            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = Regex.Replace(suggestedName, "Antwoord:", "", RegexOptions.IgnoreCase).Trim();
                suggestedName = suggestedName.Trim('\'', '\"');
            }


            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                _logger.Log($"WAARSCHUWING: AI retourneerde geen bruikbare bestandsnaam (leeg of witruimte) voor '{originalFilename}'. Gebruik originele naam.");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }

            string cleanedName = FileUtils.SanitizeFolderOrFileName(suggestedName?.Trim() ?? "");

            var genericNames = new[] { "document", "bestand", "info", "overig", "algemeen", "factuur", "" };
            if (cleanedName.Length < 3 || genericNames.Contains(cleanedName.ToLowerInvariant()))
            {
                _logger.Log($"WAARSCHUWING: AI-suggestie '{suggestedName?.Trim() ?? "[LEEG]"}' voor '{originalFilename}' is te kort of te generiek na opschonen. Gebruik originele naam.");
                return Path.GetFileNameWithoutExtension(originalFilename);
            }

            // Apply max length constraint
            if (cleanedName.Length > 100) // Hardcoded max length for filenames, adjust as needed
            {
                cleanedName = cleanedName.Substring(0, 100);
                _logger.Log($"INFO: AI-gegenereerde bestandsnaam voor '{originalFilename}' afgekort naar '{cleanedName}' wegens lengtebeperking.");
            }

            return cleanedName;
        }
    }
}