// AI_FileOrganizer/Services/FileOrganizerService.cs
using AI_FileOrganizer.Models;
using AI_FileOrganizer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // Voor DialogResult

namespace AI_FileOrganizer.Services
{
    public class FileOrganizerService
    {
        private readonly ILogger _logger;
        private readonly AiClassificationService _aiService;
        private readonly TextExtractionService _textExtractionService;
        private readonly CredentialStorageService _credentialStorageService;
        private readonly HttpClient _httpClient;
        private readonly ImageAnalysisService _imageAnalysisService;

        public event Action<int, int> ProgressChanged;
        public event Action<long> TokensUsedUpdated;
        public event Func<string, string, Task<(DialogResult result, string newFileName, bool skipFile)>> RequestRenameFile;

        private long _totalTokensUsed = 0;

        public string SelectedOnnxModelPath { get; set; }
        public string SelectedOnnxVocabPath { get; set; }

        public FileOrganizerService(
            ILogger logger,
            AiClassificationService aiService,
            TextExtractionService textExtractionService,
            CredentialStorageService credentialStorageService,
            HttpClient httpClient,
            ImageAnalysisService imageAnalysisService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _textExtractionService = textExtractionService ?? throw new ArgumentNullException(nameof(textExtractionService));
            _credentialStorageService = credentialStorageService ?? throw new ArgumentNullException(nameof(credentialStorageService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _imageAnalysisService = imageAnalysisService ?? throw new ArgumentNullException(nameof(imageAnalysisService));
        }

        public async Task OrganizeFilesAsync(
            string sourcePath,
            string destinationBasePath,
            string apiKey,
            string providerName, // Dit is de algemeen geselecteerde provider in de UI
            string modelName,
            string azureEndpoint,
            bool shouldRenameFiles,
            CancellationToken cancellationToken)
        {
            _totalTokensUsed = 0;
            if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);

            // --- Configureer services op basis van geselecteerde provider ---
            bool isExplicitImageProvider = providerName.Contains("Azure AI Vision") || providerName.Contains("OpenAI GPT-4 Vision");
            bool isPotentiallyDualPurposeProvider = providerName.Contains("OpenAI") || providerName.Contains("Azure"); // Kan tekst of vision zijn (bijv. GPT-4o, of Azure OpenAI met vision model)

            IAiProvider currentTextAiProvider = null;

            if (isExplicitImageProvider)
            {
                _logger.Log($"INFO: Expliciete afbeeldingsprovider '{providerName}' geselecteerd. Configureren ImageAnalysisService...");
                try
                {
                    if (providerName.Contains("Azure AI Vision"))
                    {
                        if (string.IsNullOrWhiteSpace(azureEndpoint) || string.IsNullOrWhiteSpace(apiKey))
                            throw new ArgumentException("Azure Endpoint en API Key zijn vereist voor Azure AI Vision.");
                        _imageAnalysisService.ConfigureAzureVision(azureEndpoint, apiKey);
                    }
                    else if (providerName.Contains("OpenAI GPT-4 Vision"))
                    {
                        if (string.IsNullOrWhiteSpace(apiKey))
                            throw new ArgumentException("OpenAI API Key is vereist voor GPT-4 Vision.");
                        _imageAnalysisService.ConfigureOpenAi(apiKey, string.IsNullOrWhiteSpace(modelName) ? "gpt-4o" : modelName);
                    }
                }
                catch (ArgumentException argEx)
                {
                    _logger.Log($"FOUT bij configureren Image Analysis Provider: {argEx.Message}. Afbeeldingsanalyse zal falen.");
                    // Overweeg te stoppen als afbeeldingsverwerking cruciaal is. Voor nu gaan we door.
                }
            }
            else // Het is een tekst-provider of een multi-purpose provider
            {
                currentTextAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint, modelName);
                if (currentTextAiProvider == null && providerName != "Lokaal ONNX-model")
                {
                    _logger.Log("FOUT: Kon Tekst AI Provider niet initialiseren. Organisatie gestopt voor documenten.");
                    return;
                }
                // Als het een multi-purpose provider is (zoals GPT-4o via "OpenAI (openai.com)"), configureer ook ImageAnalysisService
                if (providerName == "OpenAI (openai.com)" && (modelName.Contains("gpt-4o") || modelName.Contains("vision"))) // Check of het model vision aankan
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(apiKey)) _imageAnalysisService.ConfigureOpenAi(apiKey, modelName);
                        else _logger.Log("WAARSCHUWING: OpenAI (multi-purpose) provider geselecteerd, maar API key ontbreekt voor vision deel.");
                    }
                    catch (ArgumentException argEx) { _logger.Log($"FOUT bij configureren OpenAI voor vision: {argEx.Message}"); }
                }
                // Voeg vergelijkbare logica toe voor Azure OpenAI als het een vision-enabled deployment is
            }

            if (providerName != "Lokaal ONNX-model" && !string.IsNullOrEmpty(apiKey))
            {
                _credentialStorageService.SaveApiKey(providerName, apiKey, azureEndpoint);
            }

            if (!Directory.Exists(destinationBasePath))
            {
                try { Directory.CreateDirectory(destinationBasePath); _logger.Log($"[MAP] Basisdoelmap '{destinationBasePath}' aangemaakt."); }
                catch (Exception ex) { _logger.Log($"FOUT: Fout bij aanmaken basisdoelmap '{destinationBasePath}': {ex.Message}"); return; }
            }

            var allFiles = new List<string>();
            try
            {
                allFiles = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                   .Where(f => ApplicationSettings.GetAllSupportedExtensions().Contains(Path.GetExtension(f).ToLowerInvariant()))
                                   .ToList();
            }
            catch (Exception ex) { _logger.Log($"FOUT: Kon bestanden niet lezen uit bronmap '{sourcePath}': {ex.Message}"); return; }

            int processedCount = 0, movedFiles = 0, filesWithDetailedSubfolders = 0, renamedFiles = 0;
            if (ProgressChanged != null) ProgressChanged.Invoke(0, allFiles.Count);

            foreach (string filePath in allFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                { _logger.Log("INFO: Organisatieproces geannuleerd door gebruiker."); cancellationToken.ThrowIfCancellationRequested(); }

                FileInfo fileInfo = new FileInfo(filePath);
                processedCount++;
                _logger.Log($"\n[BESTAND {processedCount}/{allFiles.Count}] Verwerken van: {fileInfo.Name} (locatie: {Path.GetDirectoryName(filePath)})");

                try
                {
                    bool currentFileIsImage = ApplicationSettings.ImageExtensions.Contains(fileInfo.Extension.ToLowerInvariant());

                    var resultTuple = await ProcessAndMoveSingleFileInternalAsync(
                        filePath, fileInfo, sourcePath, destinationBasePath,
                        currentTextAiProvider, // Dit is de geconfigureerde tekst-AI provider (kan null zijn)
                        modelName,             // Dit is de geselecteerde modelnaam
                        shouldRenameFiles, cancellationToken,
                        currentFileIsImage,
                        providerName           // De algemeen geselecteerde providernaam uit de UI
                        ).ConfigureAwait(false);

                    if (resultTuple.processed)
                    {
                        if (resultTuple.moved) movedFiles++;
                        if (resultTuple.hadSubfolder) filesWithDetailedSubfolders++;
                        if (resultTuple.renamed) renamedFiles++;
                    }
                }
                catch (OperationCanceledException) { _logger.Log("INFO: Verwerking van bestand geannuleerd."); throw; }
                catch (Exception ex) { _logger.Log($"FOUT: Onverwachte fout bij verwerken van {fileInfo.Name}: {ex.Message}\nStackTrace: {ex.StackTrace}"); }
                finally { if (ProgressChanged != null) ProgressChanged.Invoke(processedCount, allFiles.Count); }
            }

            _logger.Log($"\n--- SAMENVATTING ---");
            _logger.Log($"Totaal aantal bestanden bekeken (documenten & afbeeldingen): {processedCount}");
            _logger.Log($"Aantal bestanden succesvol verplaatst: {movedFiles}");
            _logger.Log($"Aantal documenten geplaatst in een AI-gegenereerde gedetailleerde submap: {filesWithDetailedSubfolders}");
            _logger.Log($"Aantal bestanden (documenten & afbeeldingen) hernoemd: {renamedFiles}");
            _logger.Log($"Totaal gesimuleerde/gebruikte tokens/transacties: {_totalTokensUsed}");
            _logger.Log($"--- EINDE SAMENVATTING ---");
        }

        public async Task RenameSingleFileInteractiveAsync(
            string filePath,
            string apiKey,
            string providerName, // De geselecteerde provider uit de UI
            string modelName,
            string azureEndpoint,
            CancellationToken cancellationToken)
        {
            _totalTokensUsed = 0;
            if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);

            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            bool isImageFile = ApplicationSettings.ImageExtensions.Contains(fileExtension);
            bool isDocumentFile = ApplicationSettings.DocumentExtensions.Contains(fileExtension) && !isImageFile;

            IAiProvider textAiProvider = null;

            try
            {
                if (isImageFile)
                {
                    _logger.Log($"INFO: Bestand '{Path.GetFileName(filePath)}' gedetecteerd als afbeelding. Configureren Image Analysis Service met provider '{providerName}'...");
                    if (providerName.Contains("Azure AI Vision"))
                    {
                        if (string.IsNullOrWhiteSpace(azureEndpoint) || string.IsNullOrWhiteSpace(apiKey))
                            throw new ArgumentException("Azure Endpoint en API Key zijn vereist voor Azure AI Vision.");
                        _imageAnalysisService.ConfigureAzureVision(azureEndpoint, apiKey);
                        _logger.Log($"INFO: Azure AI Vision geconfigureerd.");
                    }
                    else if (providerName.Contains("OpenAI GPT-4 Vision"))
                    {
                        if (string.IsNullOrWhiteSpace(apiKey))
                            throw new ArgumentException("OpenAI API Key is vereist voor GPT-4 Vision.");
                        _imageAnalysisService.ConfigureOpenAi(apiKey, string.IsNullOrWhiteSpace(modelName) ? "gpt-4o" : modelName);
                        _logger.Log($"INFO: OpenAI GPT-4 Vision geconfigureerd.");
                    }
                    else
                    {
                        // Als het een afbeelding is, maar de geselecteerde provider is niet expliciet een Vision provider,
                        // probeer dan of het een multi-purpose provider is (zoals OpenAI (openai.com) met gpt-4o).
                        if (providerName == "OpenAI (openai.com)" && (modelName.Contains("gpt-4o") || modelName.Contains("vision")))
                        {
                            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("OpenAI API Key is vereist voor Vision features.");
                            _imageAnalysisService.ConfigureOpenAi(apiKey, modelName);
                            _logger.Log($"INFO: OpenAI (multi-purpose) geconfigureerd voor vision voor afbeelding.");
                        }
                        // Voeg hier logica toe voor Azure OpenAI als multi-purpose vision provider
                        else
                        {
                            _logger.Log($"FOUT: Geselecteerde provider '{providerName}' is niet herkend als een ondersteunde Vision provider voor afbeelding '{Path.GetFileName(filePath)}'. Hernoemen gestopt.");
                            return;
                        }
                    }
                }
                else if (isDocumentFile)
                {
                    _logger.Log($"INFO: Bestand '{Path.GetFileName(filePath)}' gedetecteerd als document. Configureren Text AI Service met provider '{providerName}'...");
                    textAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint, modelName);
                    if (textAiProvider == null && providerName != "Lokaal ONNX-model")
                    {
                        _logger.Log("FOUT: Kon tekst-AI provider niet initialiseren. Hernoemen gestopt.");
                        return;
                    }
                    if (providerName != "Lokaal ONNX-model" && !string.IsNullOrEmpty(apiKey))
                    {
                        _credentialStorageService.SaveApiKey(providerName, apiKey, azureEndpoint);
                    }
                }
                else
                {
                    _logger.Log($"FOUT: Bestandstype van '{Path.GetFileName(filePath)}' wordt niet ondersteund. Extensie: '{fileExtension}'");
                    return;
                }
            }
            catch (ArgumentException argEx)
            {
                _logger.Log($"Configuratiefout: {argEx.Message}");
                return;
            }

            if (!System.IO.File.Exists(filePath))
            {
                _logger.Log($"FOUT: Bestand niet gevonden voor hernoemen: '{Path.GetFileName(filePath)}'.");
                return;
            }

            FileInfo fileInfo = new FileInfo(filePath);
            _logger.Log($"\n[BESTAND] Voorbereiden van hernoemen voor: {fileInfo.Name}");
            string suggestedNewBaseName = null;

            try
            {
                if (isImageFile)
                {
                    _logger.Log($"INFO: AI-afbeeldingsnaamsuggestie genereren voor '{fileInfo.Name}' met provider '{providerName}'...");
                    if (providerName.Contains("Azure AI Vision"))
                    {
                        suggestedNewBaseName = await _imageAnalysisService.SuggestImageNameAzureAsync(
                            filePath, fileInfo.Name, cancellationToken).ConfigureAwait(false);
                    }
                    else if (providerName.Contains("OpenAI GPT-4 Vision") || (providerName == "OpenAI (openai.com)" && (modelName.Contains("gpt-4o") || modelName.Contains("vision"))))
                    {
                        suggestedNewBaseName = await _imageAnalysisService.SuggestImageNameOpenAiAsync(
                            filePath, fileInfo.Name, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.Log($"WAARSCHUWING: Geen image analysis methode aangeroepen voor provider '{providerName}'. Kan afbeelding niet hernoemen via AI.");
                    }
                    _totalTokensUsed += _imageAnalysisService.LastCallSimulatedTokensUsed;
                }
                else if (isDocumentFile)
                {
                    if (textAiProvider == null && providerName != "Lokaal ONNX-model")
                    {
                        _logger.Log("FOUT: Text AI Provider is niet beschikbaar voor document. Kan niet hernoemen.");
                        return;
                    }
                    string extractedText = _textExtractionService.ExtractText(filePath);
                    _logger.Log($"INFO: Geëxtraheerde tekst (eerste 100 karakters): '{(extractedText?.Length > 100 ? extractedText.Substring(0, 100) : extractedText)}'...");
                    if (string.IsNullOrWhiteSpace(extractedText)) extractedText = fileInfo.Name;
                    if (extractedText.Length > ApplicationSettings.MaxTextLengthForLlm)
                        extractedText = extractedText.Substring(0, ApplicationSettings.MaxTextLengthForLlm);

                    _logger.Log($"INFO: AI-bestandsnaamsuggestie genereren voor '{fileInfo.Name}'...");
                    suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                        extractedText, fileInfo.Name, textAiProvider, modelName, cancellationToken).ConfigureAwait(false);
                    _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                }

                if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);

                if (string.IsNullOrWhiteSpace(suggestedNewBaseName))
                {
                    _logger.Log($"WAARSCHUWING: AI kon geen basisnaam suggereren voor '{fileInfo.Name}'. Hernoem-dialoog wordt getoond met originele naam.");
                    suggestedNewBaseName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                }

                if (RequestRenameFile == null)
                {
                    _logger.Log("FOUT: UI callback voor hernoemen is niet ingesteld. Kan bestand niet interactief hernoemen.");
                    return;
                }

                var (dialogResult, returnedFileName, skipFile) = await RequestRenameFile.Invoke(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension).ConfigureAwait(false);

                if (dialogResult == DialogResult.OK)
                {
                    if (skipFile) { _logger.Log($"INFO: Gebruiker koos om '{fileInfo.Name}' niet te hernoemen."); }
                    else
                    {
                        string proposedFullName = returnedFileName;
                        string proposedBaseName = Path.GetFileNameWithoutExtension(proposedFullName);
                        string proposedExtension = Path.GetExtension(proposedFullName);

                        if (string.IsNullOrEmpty(proposedExtension)) proposedFullName = proposedBaseName + fileInfo.Extension;
                        else if (!proposedExtension.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.Log($"WAARSCHUWING: Bestandsnaam '{proposedFullName}' heeft afwijkende extensie. Originele extensie '{fileInfo.Extension}' behouden.");
                            proposedFullName = proposedBaseName + fileInfo.Extension;
                        }

                        string newFileNameSanitized = FileUtils.SanitizeFileName(proposedFullName);
                        string baseNameWithoutExt = Path.GetFileNameWithoutExtension(newFileNameSanitized);
                        string extension = Path.GetExtension(newFileNameSanitized);
                        if (baseNameWithoutExt.Length > ApplicationSettings.MaxFilenameLength)
                        {
                            baseNameWithoutExt = baseNameWithoutExt.Substring(0, ApplicationSettings.MaxFilenameLength);
                            newFileNameSanitized = baseNameWithoutExt + extension;
                            _logger.Log($"WAARSCHUWING: Nieuwe bestandsnaam te lang. Afgekapt naar '{newFileNameSanitized}'.");
                        }

                        if (!string.IsNullOrWhiteSpace(newFileNameSanitized) && newFileNameSanitized != fileInfo.Name)
                        {
                            string destinationFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileNameSanitized);
                            if (System.IO.File.Exists(destinationFilePath) && !destinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                string baseNameConflict = Path.GetFileNameWithoutExtension(newFileNameSanitized);
                                string extensionConflict = Path.GetExtension(newFileNameSanitized);
                                int counter = 1;
                                string uniqueDestinationFilePath = destinationFilePath;
                                while (System.IO.File.Exists(uniqueDestinationFilePath))
                                {
                                    uniqueDestinationFilePath = Path.Combine(Path.GetDirectoryName(filePath), $"{baseNameConflict}_{counter}{extensionConflict}");
                                    counter++;
                                }
                                _logger.Log($"INFO: Bestand '{newFileNameSanitized}' bestaat al. Hernoemd naar '{Path.GetFileName(uniqueDestinationFilePath)}'.");
                                destinationFilePath = uniqueDestinationFilePath;
                            }
                            if (!destinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                System.IO.File.Move(filePath, destinationFilePath);
                                _logger.Log($"OK: '{fileInfo.Name}' hernoemd naar '{Path.GetFileName(destinationFilePath)}'.");
                            }
                            else { _logger.Log($"INFO: Doel bestandsnaam '{Path.GetFileName(destinationFilePath)}' is hetzelfde als origineel. Niet hernoemd."); }
                        }
                        else { _logger.Log($"INFO: AI-suggestie was gelijk aan origineel, leeg, of ongeldig na opschonen. '{fileInfo.Name}' niet hernoemd."); }
                    }
                }
                else { _logger.Log($"INFO: Hernoem-actie voor '{fileInfo.Name}' geannuleerd door gebruiker."); }
            }
            catch (OperationCanceledException) { _logger.Log("Hernoem-actie geannuleerd."); throw; }
            catch (Exception ex) { _logger.Log($"FOUT: Fout bij hernoemen van {fileInfo.Name}: {ex.Message}\nStackTrace: {ex.StackTrace}"); }
            finally { _logger.Log("\nEnkel bestand hernoemen voltooid."); if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed); }
        }

        private async Task<(bool processed, bool moved, bool hadSubfolder, bool renamed)> ProcessAndMoveSingleFileInternalAsync(
            string filePath,
            FileInfo fileInfo,
            string currentSourcePath,
            string destinationBasePath,
            IAiProvider currentTextAiProvider, // Kan null zijn als de algemene provider een image provider is
            string modelName,
            bool shouldRenameFiles,
            CancellationToken cancellationToken,
            bool isImageFileCurrent,
            string uiSelectedProviderName // De providernaam zoals geselecteerd in de UI
            )
        {
            string extractedText = null;
            string suggestedNewBaseName = null;
            bool wasRenamed = false;

            if (!isImageFileCurrent) // Document logica
            {
                extractedText = _textExtractionService.ExtractText(filePath);
                if (string.IsNullOrWhiteSpace(extractedText)) { extractedText = fileInfo.Name; _logger.Log($"INFO: Geen tekst geëxtraheerd uit {fileInfo.Name}. Gebruik bestandsnaam."); }
                if (extractedText.Length > ApplicationSettings.MaxTextLengthForLlm) { extractedText = extractedText.Substring(0, ApplicationSettings.MaxTextLengthForLlm); _logger.Log($"WAARSCHUWING: Tekst voor '{fileInfo.Name}' afgekapt."); }

                IAiProvider providerForClassification = currentTextAiProvider;
                if (providerForClassification == null && uiSelectedProviderName == "Lokaal ONNX-model") // ONNX kan nog steeds werken
                {
                    _logger.Log($"INFO: Geen actieve Text AI provider, maar ONNX geselecteerd. AiClassificationService zal proberen ONNX te gebruiken.");
                    // AiClassificationService zal intern proberen de OnnxRobBERTProvider te gebruiken als categoryEmbeddings worden meegegeven.
                    // In dit scenario zou je currentTextAiProvider moeten instantiëren als OnnxRobBERTProvider of de logica in AiClassificationService aanpassen.
                    // Voor nu, als ONNX is geselecteerd, wordt currentTextAiProvider in GetAiProvider al correct gezet.
                }
                else if (providerForClassification == null)
                {
                    // Fallback naar een default tekst provider als die geconfigureerd is en de huidige provider niet voor tekst is
                    if (!string.IsNullOrWhiteSpace(ApplicationSettings.DefaultProviderForDocumentsIfNotSpecified) &&
                        ApplicationSettings.DefaultProviderForDocumentsIfNotSpecified != "Lokaal ONNX-model") // Voorkom recursie of onnodige ONNX hier
                    {
                        _logger.Log($"INFO: Geselecteerde provider '{uiSelectedProviderName}' niet geschikt voor tekst. Probeer default document provider: {ApplicationSettings.DefaultProviderForDocumentsIfNotSpecified}");
                        // Haal API key/endpoint op voor deze default provider (dit ontbreekt nu)
                        // Voorbeeld: string defaultApiKey = _credentialStorageService.GetApiKey(ApplicationSettings.DefaultProviderForDocumentsIfNotSpecified).apiKey;
                        // providerForClassification = GetAiProvider(defaultApiKey, ApplicationSettings.DefaultProviderForDocumentsIfNotSpecified, defaultAzureEndpoint, defaultModel);
                        // Dit vereist dat je keys voor de default provider beschikbaar hebt.
                        _logger.Log($"WAARSCHUWING: Default provider ophalen niet volledig geïmplementeerd. Classificatie kan falen.");
                    }

                    if (providerForClassification == null) // Nog steeds null na default poging
                    {
                        _logger.Log($"FOUT: Geen geschikte Text AI provider beschikbaar voor document '{fileInfo.Name}'. Kan niet classificeren/hernoemen.");
                        return (false, false, false, false);
                    }
                }


                string llmCategoryChoice = await _aiService.ClassifyCategoryAsync(
                    extractedText, fileInfo.Name, ApplicationSettings.FolderCategories.Keys.ToList(),
                    providerForClassification, modelName, cancellationToken).ConfigureAwait(false);
                _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);

                if (string.IsNullOrWhiteSpace(llmCategoryChoice) ||
                    (llmCategoryChoice.Equals(ApplicationSettings.FallbackCategoryKey, StringComparison.OrdinalIgnoreCase) &&
                     !ApplicationSettings.OrganizeFallbackCategoryIfNoMatch))
                {
                    _logger.Log($"WAARSCHUWING: Document '{fileInfo.Name}' niet geclassificeerd of viel in '{ApplicationSettings.FallbackCategoryKey}' (fallback organisatie uit). Niet verplaatst.");
                    return (false, false, false, false);
                }

                string targetCategoryFolderName;
                if (llmCategoryChoice.Equals(ApplicationSettings.FallbackCategoryKey, StringComparison.OrdinalIgnoreCase))
                { targetCategoryFolderName = ApplicationSettings.FallbackFolderName; }
                else if (!ApplicationSettings.FolderCategories.TryGetValue(llmCategoryChoice, out targetCategoryFolderName))
                { _logger.Log($"FOUT: Categorie '{llmCategoryChoice}' onbekend. Gebruik fallback voor '{fileInfo.Name}'."); targetCategoryFolderName = ApplicationSettings.FallbackFolderName; }

                string targetCategoryFolderPath = Path.Combine(destinationBasePath, targetCategoryFolderName);
                Directory.CreateDirectory(targetCategoryFolderPath);

                string detailedSubfolderRelativePath = null;
                bool hadDetailedSubfolder = false;
                if (ApplicationSettings.UseDetailedSubfolders)
                {
                    detailedSubfolderRelativePath = await _aiService.SuggestDetailedSubfolderAsync(
                        extractedText, fileInfo.Name, llmCategoryChoice,
                        providerForClassification, modelName, cancellationToken).ConfigureAwait(false);
                    _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                    if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);
                    hadDetailedSubfolder = !string.IsNullOrWhiteSpace(detailedSubfolderRelativePath);
                }

                string finalTargetDirectory;
                if (hadDetailedSubfolder)
                {
                    finalTargetDirectory = Path.Combine(targetCategoryFolderPath, detailedSubfolderRelativePath);
                    _logger.Log($"INFO: AI suggereerde gedetailleerd subpad: '{detailedSubfolderRelativePath}' voor document.");
                }
                else
                {
                    string originalFileDir = Path.GetDirectoryName(filePath);
                    string normSourcePath = currentSourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string relPathToPreserve = !originalFileDir.Equals(normSourcePath, StringComparison.OrdinalIgnoreCase) ? FileUtils.GetRelativePath(currentSourcePath, originalFileDir) : "";
                    finalTargetDirectory = string.IsNullOrEmpty(relPathToPreserve) ? targetCategoryFolderPath : Path.Combine(targetCategoryFolderPath, relPathToPreserve);
                }
                Directory.CreateDirectory(finalTargetDirectory);

                if (shouldRenameFiles)
                {
                    suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                        extractedText, fileInfo.Name, providerForClassification, modelName, cancellationToken).ConfigureAwait(false);
                    _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                    if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);
                }
                return await FinalizeMoveAndRename(filePath, fileInfo, finalTargetDirectory, destinationBasePath, suggestedNewBaseName, shouldRenameFiles, wasRenamed, hadDetailedSubfolder).ConfigureAwait(false);
            }
            else // Het is een afbeelding
            {
                _logger.Log($"INFO: Verwerken als afbeelding: {fileInfo.Name}");
                string imageDestinationFolder = Path.Combine(destinationBasePath, ApplicationSettings.DefaultImageFolderName);
                Directory.CreateDirectory(imageDestinationFolder);

                if (shouldRenameFiles)
                {
                    // De _imageAnalysisService is al geconfigureerd aan het begin van OrganizeFilesAsync
                    // op basis van de algemene uiSelectedProviderName.
                    if (uiSelectedProviderName.Contains("Azure AI Vision"))
                    {
                        suggestedNewBaseName = await _imageAnalysisService.SuggestImageNameAzureAsync(
                            filePath, fileInfo.Name, cancellationToken).ConfigureAwait(false);
                    }
                    else if (uiSelectedProviderName.Contains("OpenAI GPT-4 Vision") || (uiSelectedProviderName == "OpenAI (openai.com)" && (modelName.Contains("gpt-4o") || modelName.Contains("vision"))))
                    {
                        suggestedNewBaseName = await _imageAnalysisService.SuggestImageNameOpenAiAsync(
                            filePath, fileInfo.Name, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.Log($"WAARSCHUWING: Geen ondersteunde Image Analysis provider actief voor '{uiSelectedProviderName}'. Kan afbeelding '{fileInfo.Name}' niet hernoemen via AI.");
                    }
                    _totalTokensUsed += _imageAnalysisService.LastCallSimulatedTokensUsed;
                    if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);
                }
                return await FinalizeMoveAndRename(filePath, fileInfo, imageDestinationFolder, destinationBasePath, suggestedNewBaseName, shouldRenameFiles, wasRenamed, false).ConfigureAwait(false);
            }
        }

        private async Task<(bool processed, bool moved, bool hadSubfolder, bool renamed)> FinalizeMoveAndRename(
            string currentFilePath, FileInfo fileInfo, string finalTargetDirectory, string destinationBasePath,
            string suggestedNewBaseName, bool shouldRename, bool initialWasRenamedState, bool hadDetailedSubfolder)
        {
            string newFileName = fileInfo.Name;
            bool wasActuallyRenamed = initialWasRenamedState;

            if (shouldRename)
            {
                if (!string.IsNullOrWhiteSpace(suggestedNewBaseName))
                {
                    string tempNewFileName = FileUtils.SanitizeFileName(suggestedNewBaseName + fileInfo.Extension);
                    string baseNameWithoutExt = Path.GetFileNameWithoutExtension(tempNewFileName);
                    string extension = Path.GetExtension(tempNewFileName);
                    if (baseNameWithoutExt.Length > ApplicationSettings.MaxFilenameLength)
                    {
                        baseNameWithoutExt = baseNameWithoutExt.Substring(0, ApplicationSettings.MaxFilenameLength);
                        tempNewFileName = baseNameWithoutExt + extension;
                    }

                    if (!string.IsNullOrWhiteSpace(tempNewFileName) && tempNewFileName != fileInfo.Name)
                    {
                        newFileName = tempNewFileName;
                        wasActuallyRenamed = true;
                        _logger.Log($"INFO: AI suggereerde nieuwe bestandsnaam: '{newFileName}' voor '{fileInfo.Name}'");
                    }
                    else { _logger.Log($"INFO: AI-suggestie voor bestandsnaam was niet bruikbaar/gelijk aan origineel voor '{fileInfo.Name}'."); }
                }
                else { _logger.Log($"INFO: AI gaf geen suggestie voor bestandsnaam voor '{fileInfo.Name}'."); }
            }

            string destinationFilePath = Path.Combine(finalTargetDirectory, newFileName);

            if (System.IO.File.Exists(destinationFilePath))
            {
                if (destinationFilePath.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase) &&
                    newFileName.Equals(fileInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Log($"INFO: Bestand '{fileInfo.Name}' is al op de doel locatie en wordt niet hernoemd. Geen verplaatsing nodig.");
                    return (true, false, hadDetailedSubfolder, wasActuallyRenamed);
                }

                string baseNameConflict = Path.GetFileNameWithoutExtension(newFileName);
                string extensionConflict = Path.GetExtension(newFileName);
                int counter = 1;
                string uniqueDestinationFilePath = destinationFilePath;
                while (System.IO.File.Exists(uniqueDestinationFilePath) && !uniqueDestinationFilePath.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    uniqueDestinationFilePath = Path.Combine(finalTargetDirectory, $"{baseNameConflict}_{counter}{extensionConflict}");
                    counter++;
                }
                if (System.IO.File.Exists(uniqueDestinationFilePath) && !uniqueDestinationFilePath.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Log($"FOUT: Kon geen unieke bestandsnaam genereren voor '{newFileName}' in '{finalTargetDirectory}'. Bestand overgeslagen.");
                    return (false, false, false, false);
                }
                if (!destinationFilePath.Equals(uniqueDestinationFilePath))
                {
                    _logger.Log($"INFO: Doelbestand '{newFileName}' bestaat al. Hernoemd naar '{Path.GetFileName(uniqueDestinationFilePath)}' om conflict te voorkomen.");
                    newFileName = Path.GetFileName(uniqueDestinationFilePath);
                    destinationFilePath = uniqueDestinationFilePath;
                    wasActuallyRenamed = true;
                }
            }

            if (destinationFilePath.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Log($"INFO: Doelpad voor '{fileInfo.Name}' is identiek aan bronpad. Geen verplaatsing nodig.");
                return (true, false, hadDetailedSubfolder, wasActuallyRenamed);
            }

            try
            {
                System.IO.File.Move(currentFilePath, destinationFilePath);
                _logger.Log($"OK: Origineel '{fileInfo.Name}' verplaatst/hernoemd naar '{FileUtils.GetRelativePath(destinationBasePath, destinationFilePath)}'");
                return (true, true, hadDetailedSubfolder, wasActuallyRenamed);
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 183)
            {
                _logger.Log($"FOUT: Kan '{fileInfo.Name}' niet verplaatsen naar '{Path.GetFileName(destinationFilePath)}'. Bestand bestaat al: {ioEx.Message}");
                return (false, false, false, false);
            }
            catch (Exception moveEx)
            {
                _logger.Log($"FOUT: Kon '{fileInfo.Name}' niet verplaatsen naar '{Path.GetFileName(destinationFilePath)}': {moveEx.Message}");
                return (false, false, false, false);
            }
        }

        private IAiProvider GetAiProvider(string apiKey, string providerName, string azureEndpoint, string modelName)
        {
            // Deze methode retourneert alleen IAiProvider voor tekst-gebaseerde taken.
            // Vision providers worden geconfigureerd in ImageAnalysisService.
            if (providerName.Contains("Azure AI Vision") || providerName.Contains("OpenAI GPT-4 Vision"))
            {
                _logger.Log($"INFO: GetAiProvider aangeroepen voor Vision provider '{providerName}'. Deze wordt apart afgehandeld. Retourneer null voor IAiProvider.");
                return null;
            }

            try
            {
                switch (providerName)
                {
                    case "Gemini (Google)":
                        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key voor Gemini is vereist.");
                        return new GeminiAiProvider(apiKey, _httpClient);
                    case "OpenAI (openai.com)":
                        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key voor OpenAI is vereist.");
                        // Constructor OpenAiProvider(apiKey) of OpenAiProvider(apiKey, modelName)
                        // Voor nu (apiKey) aannemend, modelName wordt doorgegeven aan GetTextCompletionAsync
                        return new OpenAiProvider(apiKey);
                    case "Azure OpenAI":
                        if (string.IsNullOrWhiteSpace(azureEndpoint)) throw new ArgumentException("Azure Endpoint voor Azure OpenAI is vereist.");
                        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key voor Azure OpenAI is vereist.");
                        // modelName is hier de deployment name
                        return new AzureOpenAiProvider(azureEndpoint,apiKey);
                    case "Lokaal ONNX-model":
                        if (string.IsNullOrEmpty(SelectedOnnxModelPath) || !System.IO.File.Exists(SelectedOnnxModelPath))
                        {
                            _logger.Log("FOUT: Geen geldig ONNX-model geselecteerd of pad is incorrect.");
                            return null;
                        }
                        return new OnnxRobBERTProvider(_logger, SelectedOnnxModelPath, SelectedOnnxVocabPath);
                    default:
                        _logger.Log($"FOUT: Onbekende Tekst AI-provider geselecteerd: '{providerName}'.");
                        return null;
                }
            }
            catch (ArgumentException argEx) { _logger.Log($"FOUT bij initialiseren Tekst AI Provider '{providerName}': {argEx.Message}"); return null; }
            catch (Exception ex) { _logger.Log($"ALGEMENE FOUT bij initialiseren Tekst AI Provider '{providerName}': {ex.Message}\nStackTrace: {ex.StackTrace}"); return null; }
        }
    }
}