// AI_FileOrganizer/Services/FileOrganizerService.cs
using AI_FileOrganizer.Models;
using AI_FileOrganizer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http; // Alleen als je het direct gebruikt, anders via AiProvider implementaties
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
        // HttpClient kan beter per provider geïnjecteerd worden of via een IHttpClientFactory
        private readonly HttpClient _httpClient;
        private readonly ImageAnalysisService _imageAnalysisService;

        public event Action<int, int> ProgressChanged;
        public event Action<long> TokensUsedUpdated;
        // Gebruik Tuple voor C# 7.3 compatibiliteit in het return type van de Func
        public event Func<string, string, Task<Tuple<DialogResult, string, bool>>> RequestRenameFile;


        private long _totalTokensUsed = 0;

        public string SelectedOnnxModelPath { get; set; }
        public string SelectedOnnxVocabPath { get; set; }

        public FileOrganizerService(
            ILogger logger,
            AiClassificationService aiService,
            TextExtractionService textExtractionService,
            CredentialStorageService credentialStorageService,
            HttpClient httpClient, // Overweeg IHttpClientFactory
            ImageAnalysisService imageAnalysisService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _textExtractionService = textExtractionService ?? throw new ArgumentNullException(nameof(textExtractionService));
            _credentialStorageService = credentialStorageService ?? throw new ArgumentNullException(nameof(credentialStorageService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient)); // Of injecteer IHttpClientFactory
            _imageAnalysisService = imageAnalysisService ?? throw new ArgumentNullException(nameof(imageAnalysisService));
        }

        public async Task OrganizeFilesAsync(
            string sourcePath, // Dit is de root map van waaruit bestanden gelezen worden
            string destinationBasePath, // Dit is de root map waar de georganiseerde structuur komt
            string apiKey,
            string providerName,
            string modelName,
            string azureEndpoint,
            bool shouldRenameFiles,
            CancellationToken cancellationToken)
        {
            _totalTokensUsed = 0;
            if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);

            bool isExplicitImageProvider = providerName.Contains("Azure AI Vision") || providerName.Contains("OpenAI GPT-4 Vision");
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
                }
            }
            else
            {
                currentTextAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint, modelName);
                if (currentTextAiProvider == null && providerName != "Lokaal ONNX-model") // ONNX kan nog steeds werken via AiClassificationService intern
                {
                    _logger.Log("FOUT: Kon Tekst AI Provider niet initialiseren. Organisatie gestopt voor documenten die deze provider vereisen.");
                    // Overweeg of je hier wilt returnen, of doorgaan voor afbeeldingen / ONNX
                }

                if (providerName == "OpenAI (openai.com)" && (modelName.Contains("gpt-4o") || modelName.Contains("vision")))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(apiKey)) _imageAnalysisService.ConfigureOpenAi(apiKey, modelName);
                        else _logger.Log("WAARSCHUWING: OpenAI (multi-purpose) provider geselecteerd, maar API key ontbreekt voor vision deel.");
                    }
                    catch (ArgumentException argEx) { _logger.Log($"FOUT bij configureren OpenAI voor vision: {argEx.Message}"); }
                }
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
                _logger.Log($"\n[BESTAND {processedCount}/{allFiles.Count}] Verwerken van: {fileInfo.Name} (locatie: {fileInfo.DirectoryName})");

                try
                {
                    bool currentFileIsImage = ApplicationSettings.ImageExtensions.Contains(fileInfo.Extension.ToLowerInvariant());

                    // De sourcePath parameter is de root van de input, niet de directe directory van het bestand
                    Tuple<bool, bool, bool, bool> resultTuple = await ProcessAndMoveSingleFileInternalAsync(
                        filePath, fileInfo, sourcePath, destinationBasePath,
                        currentTextAiProvider, modelName, shouldRenameFiles, cancellationToken,
                        currentFileIsImage, providerName
                        ).ConfigureAwait(false);

                    if (resultTuple.Item1) // processed
                    {
                        if (resultTuple.Item2) movedFiles++;
                        if (resultTuple.Item3) filesWithDetailedSubfolders++;
                        if (resultTuple.Item4) renamedFiles++;
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
            string providerName,
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
                        if (providerName == "OpenAI (openai.com)" && (modelName.Contains("gpt-4o") || modelName.Contains("vision")))
                        {
                            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("OpenAI API Key is vereist voor Vision features.");
                            _imageAnalysisService.ConfigureOpenAi(apiKey, modelName);
                            _logger.Log($"INFO: OpenAI (multi-purpose) geconfigureerd voor vision voor afbeelding.");
                        }
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

            if (!File.Exists(filePath))
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
                    _logger.Log($"INFO: Geëxtraheerde tekst (eerste 100 karakters): '{(extractedText != null && extractedText.Length > 100 ? extractedText.Substring(0, 100) + "..." : extractedText)}'...");
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

                Tuple<DialogResult, string, bool> renameResponse = await RequestRenameFile.Invoke(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension).ConfigureAwait(false);
                DialogResult dialogResult = renameResponse.Item1;
                string returnedFileName = renameResponse.Item2;
                bool skipFile = renameResponse.Item3;


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
                            destinationFilePath = GetUniqueFilePath(destinationFilePath, filePath); // Zorg voor uniek pad

                            if (!destinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Move(filePath, destinationFilePath);
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

        // HERSCHREVEN METHODE VOOR PADCONSTRUCTIE
        private async Task<Tuple<bool, bool, bool, bool>> ProcessAndMoveSingleFileInternalAsync(
            string currentFullFilePath, // Volledige pad naar het bronbestand
            FileInfo fileInfo,           // FileInfo object van het bronbestand
            string sourcePathRoot,       // De root input directory (bijv. C:\Users\Remse\Desktop\Persoonlijke Administratie244)
            string destinationBasePath,  // De root output directory (bijv. C:\Georganiseerde Documenten)
            IAiProvider currentTextAiProvider,
            string modelName,
            bool shouldRenameFiles,
            CancellationToken cancellationToken,
            bool isImageFileCurrent,
            string uiSelectedProviderName)
        {
            string extractedText = null;
            string suggestedNewBaseName = null; // Alleen de naam zonder extensie
            bool wasRenamed = false;
            bool hadDetailedSubfolder = false;

            // --- Padvariabelen ---
            string targetMainCategoryFolderByKey; // De key zoals "Financiën"
            string targetMainCategoryFolderName;  // De mapnaam zoals "1. Financieel"
            string detailedSubfolderRelativePath = null; // Relatief t.o.v. targetMainCategoryFolderName, bv. "Facturen\2024"
            string finalNewFileName = fileInfo.Name; // Start met de originele naam

            if (!isImageFileCurrent) // Het is een DOCUMENT
            {
                extractedText = _textExtractionService.ExtractText(currentFullFilePath);
                if (string.IsNullOrWhiteSpace(extractedText)) { extractedText = fileInfo.Name; _logger.Log($"INFO: Geen tekst geëxtraheerd uit {fileInfo.Name}. Gebruik bestandsnaam als context."); }
                if (extractedText.Length > ApplicationSettings.MaxTextLengthForLlm) { extractedText = extractedText.Substring(0, ApplicationSettings.MaxTextLengthForLlm); _logger.Log($"WAARSCHUWING: Tekst voor '{fileInfo.Name}' afgekapt."); }

                IAiProvider providerForClassification = currentTextAiProvider;
                if (providerForClassification == null && uiSelectedProviderName == "Lokaal ONNX-model")
                {
                    // Als ONNX de UI selectie is, maar currentTextAiProvider null is (omdat het geen text provider is),
                    // dan moet AiClassificationService intern de ONNX provider kunnen gebruiken.
                    // GetAiProvider zou al een OnnxRobBERTProvider moeten retourneren als "Lokaal ONNX-model" geselecteerd is.
                    // Als dat niet zo is, dan is er een logische fout in GetAiProvider of de aanroep ervan.
                    _logger.Log($"INFO: ONNX geselecteerd. AiClassificationService zal proberen ONNX te gebruiken indien geconfigureerd met embeddings.");
                    // Voor de zekerheid: als GetAiProvider de ONNX provider niet correct instelt, doe het hier:
                    if (currentTextAiProvider == null && !string.IsNullOrEmpty(SelectedOnnxModelPath))
                        providerForClassification = new OnnxRobBERTProvider(_logger, SelectedOnnxModelPath, SelectedOnnxVocabPath);

                }
                else if (providerForClassification == null)
                {
                    _logger.Log($"FOUT: Geen geschikte Text AI provider beschikbaar voor document '{fileInfo.Name}'. Kan niet classificeren/hernoemen.");
                    return Tuple.Create(false, false, false, false); // processed, moved, hadSubfolder, renamed
                }

                targetMainCategoryFolderByKey = await _aiService.ClassifyCategoryAsync(
                    extractedText, fileInfo.Name, ApplicationSettings.FolderCategories.Keys.ToList(),
                    providerForClassification, modelName, cancellationToken).ConfigureAwait(false);
                _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);

                if (string.IsNullOrWhiteSpace(targetMainCategoryFolderByKey) ||
                    (targetMainCategoryFolderByKey.Equals(ApplicationSettings.FallbackCategoryKey, StringComparison.OrdinalIgnoreCase) &&
                     !ApplicationSettings.OrganizeFallbackCategoryIfNoMatch))
                {
                    _logger.Log($"WAARSCHUWING: Document '{fileInfo.Name}' niet geclassificeerd of viel in '{ApplicationSettings.FallbackCategoryKey}' (fallback organisatie uit). Niet verplaatst.");
                    return Tuple.Create(false, false, false, false);
                }

                // Map de AI categorie key naar de daadwerkelijke mapnaam
                if (targetMainCategoryFolderByKey.Equals(ApplicationSettings.FallbackCategoryKey, StringComparison.OrdinalIgnoreCase))
                {
                    targetMainCategoryFolderName = ApplicationSettings.FallbackFolderName;
                }
                else if (!ApplicationSettings.FolderCategories.TryGetValue(targetMainCategoryFolderByKey, out targetMainCategoryFolderName))
                {
                    _logger.Log($"FOUT: Categorie key '{targetMainCategoryFolderByKey}' onbekend. Gebruik fallback map voor '{fileInfo.Name}'.");
                    targetMainCategoryFolderName = ApplicationSettings.FallbackFolderName;
                    targetMainCategoryFolderByKey = ApplicationSettings.FallbackCategoryKey; // Zorg dat key consistent is
                }

                if (ApplicationSettings.UseDetailedSubfolders)
                {
                    detailedSubfolderRelativePath = await _aiService.SuggestDetailedSubfolderAsync(
                        extractedText, fileInfo.Name, targetMainCategoryFolderByKey, // Geef de KEY mee
                        providerForClassification, modelName, cancellationToken).ConfigureAwait(false);
                    _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                    if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);
                    hadDetailedSubfolder = !string.IsNullOrWhiteSpace(detailedSubfolderRelativePath);
                }

                if (shouldRenameFiles)
                {
                    suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                        extractedText, fileInfo.Name, providerForClassification, modelName, cancellationToken).ConfigureAwait(false);
                    _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                    if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);
                }
            }
            else // Het is een AFBEELDING
            {
                _logger.Log($"INFO: Verwerken als afbeelding: {fileInfo.Name}");
                targetMainCategoryFolderName = ApplicationSettings.DefaultImageFolderName; // Afbeeldingen gaan naar een vaste map
                // Geen gedetailleerde submappen voor afbeeldingen in dit scenario (kan worden uitgebreid)
                hadDetailedSubfolder = false;

                if (shouldRenameFiles)
                {
                    // ImageAnalysisService is al geconfigureerd aan het begin van OrganizeFilesAsync
                    if (uiSelectedProviderName.Contains("Azure AI Vision"))
                    {
                        suggestedNewBaseName = await _imageAnalysisService.SuggestImageNameAzureAsync(
                            currentFullFilePath, fileInfo.Name, cancellationToken).ConfigureAwait(false);
                    }
                    else if (uiSelectedProviderName.Contains("OpenAI GPT-4 Vision") || (uiSelectedProviderName == "OpenAI (openai.com)" && (modelName.Contains("gpt-4o") || modelName.Contains("vision"))))
                    {
                        suggestedNewBaseName = await _imageAnalysisService.SuggestImageNameOpenAiAsync(
                            currentFullFilePath, fileInfo.Name, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.Log($"WAARSCHUWING: Geen ondersteunde Image Analysis provider actief voor '{uiSelectedProviderName}'. Kan afbeelding '{fileInfo.Name}' niet hernoemen via AI.");
                    }
                    _totalTokensUsed += _imageAnalysisService.LastCallSimulatedTokensUsed;
                    if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);
                }
            }

            // Hernoem logica (indien van toepassing)
            if (shouldRenameFiles && !string.IsNullOrWhiteSpace(suggestedNewBaseName))
            {
                string tempNewFileName = FileUtils.SanitizeFileName(suggestedNewBaseName + fileInfo.Extension);
                string baseNameOnly = Path.GetFileNameWithoutExtension(tempNewFileName);
                if (baseNameOnly.Length > ApplicationSettings.MaxFilenameLength)
                {
                    baseNameOnly = baseNameOnly.Substring(0, ApplicationSettings.MaxFilenameLength);
                    tempNewFileName = baseNameOnly + fileInfo.Extension;
                }

                if (!tempNewFileName.Equals(fileInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(baseNameOnly)) // Voorkom hernoemen naar alleen extensie
                {
                    finalNewFileName = tempNewFileName;
                    wasRenamed = true;
                    _logger.Log($"INFO: AI suggereerde nieuwe bestandsnaam: '{finalNewFileName}' voor '{fileInfo.Name}'");
                }
                else
                {
                    _logger.Log($"INFO: AI-suggestie voor bestandsnaam ('{suggestedNewBaseName}') was niet bruikbaar, gelijk aan origineel, of resulteerde in een ongeldige naam na opschonen voor '{fileInfo.Name}'.");
                }
            }


            // --- Constructie van het DEFINITIEVE DOELPAD ---
            string finalRelativePathToDestinationRoot; // Relatief t.o.v. destinationBasePath

            if (!string.IsNullOrWhiteSpace(detailedSubfolderRelativePath)) // Alleen voor documenten met AI subfolder
            {
                // Voorbeeld: targetMainCategoryFolderName = "2. Belastingzaken"
                //            detailedSubfolderRelativePath = "Belastingaangiften\IB 2021"
                //            finalNewFileName = "Aangifte 2021.pdf"
                // Resultaat: "2. Belastingzaken\Belastingaangiften\IB 2021\Aangifte 2021.pdf"
                finalRelativePathToDestinationRoot = Path.Combine(targetMainCategoryFolderName, detailedSubfolderRelativePath, finalNewFileName);
            }
            else // Documenten zonder AI subfolder, of afbeeldingen
            {
                // Voorbeeld (document): targetMainCategoryFolderName = "11. Zakelijke Administratie"
                //                     finalNewFileName = "Rapport.docx"
                // Resultaat: "11. Zakelijke Administratie\Rapport.docx"
                // Voorbeeld (afbeelding): targetMainCategoryFolderName = "Afbeeldingen"
                //                       finalNewFileName = "Vakantiefoto.jpg"
                // Resultaat: "Afbeeldingen\Vakantiefoto.jpg"
                finalRelativePathToDestinationRoot = Path.Combine(targetMainCategoryFolderName, finalNewFileName);
            }

            string fullDestinationPath = Path.Combine(destinationBasePath, finalRelativePathToDestinationRoot);
            // --- Einde padconstructie ---


            // Controleer of het bestand al op de juiste plek staat met de juiste naam
            if (currentFullFilePath.Equals(fullDestinationPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Log($"INFO: Bestand '{fileInfo.Name}' is al op de correcte doellocatie en heeft de correcte naam. Geen actie nodig.");
                return Tuple.Create(true, false, hadDetailedSubfolder, wasRenamed); // processed, !moved, hadSubfolder, wasRenamed
            }

            // Verplaats/hernoem bestand (met conflictresolutie)
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullDestinationPath));
                string finalPathToMove = GetUniqueFilePath(fullDestinationPath, currentFullFilePath); // currentFullFilePath is om eigen overschrijven te voorkomen

                if (!finalPathToMove.Equals(fullDestinationPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Log($"INFO: Doelpad '{Path.GetFileName(fullDestinationPath)}' bestaat of conflict. Gebruik uniek pad '{Path.GetFileName(finalPathToMove)}'.");
                    // Als de naam veranderd is door uniek maken, update wasRenamed
                    if (!Path.GetFileName(finalPathToMove).Equals(finalNewFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        wasRenamed = true; // Het is hernoemd, ook al was het alleen om uniek te maken
                    }
                }


                File.Move(currentFullFilePath, finalPathToMove);
                // Log het relatieve pad ten opzichte van de destinationBasePath
                _logger.Log($"OK: Origineel '{fileInfo.Name}' (van '{fileInfo.DirectoryName}') verplaatst/hernoemd naar '{FileUtils.GetRelativePath(destinationBasePath, finalPathToMove)}'");
                return Tuple.Create(true, true, hadDetailedSubfolder, wasRenamed); // processed, moved, hadSubfolder, wasRenamed
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 183 || (ioEx.HResult & 0xFFFF) == 0xB7) // 183: Cannot create a file when that file already exists. 0xB7 is ERROR_ALREADY_EXISTS
            {
                _logger.Log($"FOUT (IO): Kan '{fileInfo.Name}' niet verplaatsen. Bestand bestaat mogelijk al op doel en GetUniqueFilePath kon het niet oplossen, of ander IO probleem. Pad: '{fullDestinationPath}'. Fout: {ioEx.Message}");
                return Tuple.Create(true, false, false, false); // processed, !moved
            }
            catch (Exception moveEx)
            {
                _logger.Log($"FOUT: Kon '{fileInfo.Name}' niet verplaatsen naar '{Path.GetFileName(fullDestinationPath)}' in '{Path.GetDirectoryName(fullDestinationPath)}': {moveEx.Message}");
                return Tuple.Create(true, false, false, false); // processed, !moved
            }
        }

        // Helper methode om een uniek bestandspad te genereren
        private string GetUniqueFilePath(string targetPath, string originalSourcePath)
        {
            // Als het doelpad hetzelfde is als het bronpad, is er geen conflict met zichzelf.
            if (targetPath.Equals(originalSourcePath, StringComparison.OrdinalIgnoreCase))
            {
                return targetPath; // Geen wijziging nodig, het is hetzelfde bestand.
            }

            if (!File.Exists(targetPath))
            {
                return targetPath; // Pad is al uniek
            }

            string directory = Path.GetDirectoryName(targetPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(targetPath);
            string extension = Path.GetExtension(targetPath);
            int count = 1;
            string newFilePath;
            do
            {
                newFilePath = Path.Combine(directory, string.Format("{0}_{1}{2}", fileNameWithoutExtension, count++, extension));
            } while (File.Exists(newFilePath) && !newFilePath.Equals(originalSourcePath, StringComparison.OrdinalIgnoreCase));

            // Als de loop eindigt omdat newFilePath == originalSourcePath, betekent dit dat
            // we proberen te hernoemen naar een naam die, na toevoeging van _N, overeenkomt met het origineel.
            // Dit zou niet mogen gebeuren als het origineel niet al een _N had.
            // Als de unieke naam toch het originele bronpad is, retourneer dan het originele doelpad,
            // De aanroeper zal dan zien dat het niet verplaatst hoeft te worden.
            if (File.Exists(newFilePath) && newFilePath.Equals(originalSourcePath, StringComparison.OrdinalIgnoreCase))
            {
                // Dit scenario is lastig: het "unieke" pad is het bronpad zelf.
                // Het betekent dat het doelbestand (zonder _N) bestaat, en het unieke pad (_N) is het origineel.
                // We moeten dan het *originele* targetPath teruggeven en de Move-operatie zal falen (wat goed is, want het bestand bestaat al).
                // Of, als we *altijd* een _N willen als het doel (zonder _N) bestaat:
                // Als het doel (zonder _N) het bronpad is, is er geen hernoeming.
                // Als het doel (zonder _N) NIET het bronpad is, maar wel bestaat, dan is _N correct.
                return targetPath; // Laat de File.Move falen als targetPath (zonder _N) al bestaat en niet het bronbestand is.
                                   // Of forceer de _N versie, maar wees voorzichtig met het origineel.
                                   // Voor nu, als het unieke pad uiteindelijk het bronpad is, geef het originele target terug.
            }


            return newFilePath;
        }


        private IAiProvider GetAiProvider(string apiKey, string providerName, string azureEndpoint, string modelName)
        {
            if (providerName.Contains("Azure AI Vision") || providerName.Contains("OpenAI GPT-4 Vision"))
            {
                _logger.Log($"INFO: GetAiProvider aangeroepen voor expliciete Vision provider '{providerName}'. Deze wordt apart afgehandeld. Retourneer null voor IAiProvider (tekst).");
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
                        return new OpenAiProvider(apiKey); // Modelnaam wordt meegegeven aan GetTextCompletionAsync
                    case "Azure OpenAI":
                        if (string.IsNullOrWhiteSpace(azureEndpoint)) throw new ArgumentException("Azure Endpoint voor Azure OpenAI is vereist.");
                        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key voor Azure OpenAI is vereist.");
                        // modelName is hier de deployment name
                        return new AzureOpenAiProvider(azureEndpoint, apiKey); // Model/Deployment wordt intern in provider afgehandeld of meegegeven
                    case "Lokaal ONNX-model":
                        if (string.IsNullOrEmpty(SelectedOnnxModelPath) || !File.Exists(SelectedOnnxModelPath))
                        {
                            _logger.Log("FOUT: Geen geldig ONNX-model geselecteerd of pad is incorrect.");
                            return null;
                        }
                        // Vocab pad kan optioneel zijn afhankelijk van de provider implementatie
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