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
using System.Windows.Forms;

namespace AI_FileOrganizer.Services
{
    public class FileOrganizerService
    {
        private readonly ILogger _logger;
        private readonly AiClassificationService _aiService;
        private readonly TextExtractionService _textExtractionService;
        private readonly CredentialStorageService _credentialStorageService;
        private readonly HttpClient _httpClient;

        // Events for UI updates
        public event Action<int, int> ProgressChanged;
        public event Action<long> TokensUsedUpdated;
        public event Func<string, string, Task<(DialogResult result, string newFileName, bool skipFile)>> RequestRenameFile;

        private long _totalTokensUsed = 0;

        // --- Nieuw: geef dit pad mee vanuit MainWindow aan OrganizeFilesAsync!
        public string SelectedOnnxModelPath { get; set; }
        public string SelectedOnnxVocabPath { get; set; } // Optioneel: als je vocab.txt gebruikt

        public FileOrganizerService(
            ILogger logger,
            AiClassificationService aiService,
            TextExtractionService textExtractionService,
            CredentialStorageService credentialStorageService,
            HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _textExtractionService = textExtractionService ?? throw new ArgumentNullException(nameof(textExtractionService));
            _credentialStorageService = credentialStorageService ?? throw new ArgumentNullException(nameof(credentialStorageService));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task OrganizeFilesAsync(
            string sourcePath,
            string destinationBasePath,
            string apiKey,
            string providerName,
            string modelName,
            string azureEndpoint,
            bool shouldRenameFiles,
            CancellationToken cancellationToken)
        {
            _totalTokensUsed = 0;
            TokensUsedUpdated?.Invoke(_totalTokensUsed);

            IAiProvider currentAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint, modelName);
            if (currentAiProvider == null) return;

            // Save API key (niet opslaan voor lokaal ONNX-model)
            if (providerName != "Lokaal ONNX-model")
                _credentialStorageService.SaveApiKey(providerName, apiKey, azureEndpoint);

            if (!Directory.Exists(destinationBasePath))
            {
                try
                {
                    Directory.CreateDirectory(destinationBasePath);
                    _logger.Log($"[MAP] Basisdoelmap '{destinationBasePath}' aangemaakt.");
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT: Fout bij aanmaken basisdoelmap '{destinationBasePath}': {ex.Message}");
                    return;
                }
            }

            var allFiles = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                    .Where(f => ApplicationSettings.SupportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                                    .ToList();

            int processedCount = 0, movedFiles = 0, filesWithSubfolders = 0, renamedFiles = 0;
            ProgressChanged?.Invoke(0, allFiles.Count);

            foreach (string filePath in allFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(filePath);
                processedCount++;
                _logger.Log($"\n[BESTAND] Verwerken van: {fileInfo.Name} (locatie: {Path.GetDirectoryName(filePath)})");

                try
                {
                    (bool processed, bool moved, bool hadSubfolder, bool renamed) = await ProcessAndMoveSingleFileInternalAsync(
                        filePath,
                        fileInfo,
                        sourcePath,
                        destinationBasePath,
                        currentAiProvider,
                        modelName,
                        shouldRenameFiles,
                        cancellationToken);

                    if (processed)
                    {
                        if (moved) movedFiles++;
                        if (hadSubfolder) filesWithSubfolders++;
                        if (renamed) renamedFiles++;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT: Fout bij verwerken van {fileInfo.Name}: {ex.Message}");
                }
                finally
                {
                    ProgressChanged?.Invoke(processedCount, allFiles.Count);
                }
            }

            _logger.Log($"\nTotaal aantal bestanden bekeken (met ondersteunde extensie): {processedCount}");
            _logger.Log($"Aantal bestanden succesvol verplaatst: {movedFiles}");
            _logger.Log($"Aantal bestanden geplaatst in een AI-gegenereerde submap: {filesWithSubfolders}");
            _logger.Log($"Aantal bestanden hernoemd: {renamedFiles}");
        }

        public async Task RenameSingleFileInteractiveAsync(
            string filePath, string apiKey, string providerName, string modelName, string azureEndpoint, CancellationToken cancellationToken)
        {
            _totalTokensUsed = 0;
            TokensUsedUpdated?.Invoke(_totalTokensUsed);

            IAiProvider currentAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint, modelName);
            if (currentAiProvider == null) return;

            if (providerName != "Lokaal ONNX-model")
                _credentialStorageService.SaveApiKey(providerName, apiKey, azureEndpoint);

            if (!File.Exists(filePath))
            {
                _logger.Log($"FOUT: Bestand niet gevonden voor hernoemen: '{Path.GetFileName(filePath)}'.");
                return;
            }

            FileInfo fileInfo = new FileInfo(filePath);
            _logger.Log($"\n[BESTAND] Voorbereiden van hernoemen voor: {fileInfo.Name}");

            try
            {
                string extractedText = _textExtractionService.ExtractText(filePath);
                _logger.Log($"Extractedtext: '{extractedText}'...");
                if (string.IsNullOrWhiteSpace(extractedText)) extractedText = fileInfo.Name;
                if (extractedText.Length > ApplicationSettings.MaxTextLengthForLlm)
                    extractedText = extractedText.Substring(0, ApplicationSettings.MaxTextLengthForLlm);

                _logger.Log($"INFO: AI-bestandsnaam genereren voor '{fileInfo.Name}'...");
                string suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                    extractedText,
                    fileInfo.Name,
                    currentAiProvider,
                    modelName,
                    cancellationToken
                );
                _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                TokensUsedUpdated?.Invoke(_totalTokensUsed);

                if (RequestRenameFile == null)
                {
                    _logger.Log("FOUT: UI callback voor hernoemen is niet ingesteld. Kan bestand niet interactief hernoemen.");
                    return;
                }

                var (dialogResult, returnedFileName, skipFile) = await RequestRenameFile.Invoke(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension);

                if (dialogResult == DialogResult.OK)
                {
                    if (skipFile)
                    {
                        _logger.Log($"INFO: Gebruiker koos om '{fileInfo.Name}' niet te hernoemen. Geen actie ondernomen.");
                    }
                    else
                    {
                        string proposedFullName = returnedFileName;
                        string proposedBaseName = Path.GetFileNameWithoutExtension(proposedFullName);
                        string proposedExtension = Path.GetExtension(proposedFullName);

                        if (string.IsNullOrEmpty(proposedExtension))
                        {
                            proposedFullName = proposedBaseName + fileInfo.Extension;
                        }
                        else if (proposedExtension.ToLower() != fileInfo.Extension.ToLower())
                        {
                            _logger.Log($"WAARSCHUWING: Bestandsnaam '{proposedFullName}' heeft afwijkende extensie. Originele extensie '{fileInfo.Extension}' behouden.");
                            proposedFullName = proposedBaseName + fileInfo.Extension;
                        }

                        string newFileName = FileUtils.SanitizeFileName(proposedFullName);

                        string baseNameWithoutExt = Path.GetFileNameWithoutExtension(newFileName);
                        string extension = Path.GetExtension(newFileName);
                        if (baseNameWithoutExt.Length > ApplicationSettings.MaxFilenameLength)
                        {
                            baseNameWithoutExt = baseNameWithoutExt.Substring(0, ApplicationSettings.MaxFilenameLength);
                            newFileName = baseNameWithoutExt + extension;
                            _logger.Log($"WAARSCHUWING: Nieuwe bestandsnaam te lang. Afgekapt naar '{newFileName}'.");
                        }

                        if (newFileName != fileInfo.Name)
                        {
                            string destinationFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);

                            if (File.Exists(destinationFilePath))
                            {
                                string baseNameConflict = Path.GetFileNameWithoutExtension(newFileName);
                                string extensionConflict = Path.GetExtension(newFileName);
                                int counter = 1;
                                string uniqueDestinationFilePath = destinationFilePath;
                                while (File.Exists(uniqueDestinationFilePath))
                                {
                                    uniqueDestinationFilePath = Path.Combine(Path.GetDirectoryName(filePath), $"{baseNameConflict}_{counter}{extensionConflict}");
                                    counter++;
                                }
                                _logger.Log($"INFO: Bestand '{newFileName}' bestaat al. Hernoemd naar '{Path.GetFileName(uniqueDestinationFilePath)}' om conflict te voorkomen.");
                                destinationFilePath = uniqueDestinationFilePath;
                            }

                            File.Move(filePath, destinationFilePath);
                            _logger.Log($"OK: '{fileInfo.Name}' hernoemd naar '{Path.GetFileName(destinationFilePath)}'.");
                        }
                        else
                        {
                            _logger.Log($"INFO: AI-suggestie was gelijk aan origineel of ongeldig na opschonen. '{fileInfo.Name}' niet hernoemd.");
                        }
                    }
                }
                else
                {
                    _logger.Log($"INFO: Hernoem-actie voor '{fileInfo.Name}' geannuleerd door gebruiker. Geen actie ondernomen.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Hernoem-actie geannuleerd.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Fout bij hernoemen van {fileInfo.Name}: {ex.Message}");
            }
            finally
            {
                _logger.Log("\nEnkel bestand hernoemen voltooid.");
            }
        }

        /// <summary>
        /// Helper method to process a single file: extract, classify, suggest subfolder/filename, and move.
        /// Returns (wasProcessed, wasMoved, hadSubfolder, wasRenamed)
        /// </summary>
        private async Task<(bool processed, bool moved, bool hadSubfolder, bool renamed)> ProcessAndMoveSingleFileInternalAsync(
            string filePath,
            FileInfo fileInfo,
            string sourcePath,
            string destinationBasePath,
            IAiProvider currentAiProvider,
            string modelName,
            bool shouldRenameFiles,
            CancellationToken cancellationToken)
        {
            // 1. Tekst extractie
            string extractedText = _textExtractionService.ExtractText(filePath);
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.Log($"INFO: Geen zinvolle tekst geëxtraheerd uit {fileInfo.Name}. Bestand wordt behandeld met bestandsnaam context.");
                extractedText = fileInfo.Name; // Fallback op bestandsnaam
            }
            if (extractedText.Length > ApplicationSettings.MaxTextLengthForLlm)
            {
                extractedText = extractedText.Substring(0, ApplicationSettings.MaxTextLengthForLlm);
                _logger.Log($"WAARSCHUWING: Tekstlengte voor '{fileInfo.Name}' overschrijdt {ApplicationSettings.MaxTextLengthForLlm} tekens. Tekst wordt afgekapt.");
            }

            // 2. Classificatie (categorie)
            string llmCategoryChoice = await _aiService.ClassifyCategoryAsync(
                extractedText,
                fileInfo.Name,
                ApplicationSettings.FolderCategories.Keys.ToList(),
                currentAiProvider,
                modelName,
                cancellationToken
            );
            _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
            TokensUsedUpdated?.Invoke(_totalTokensUsed);

            if (string.IsNullOrWhiteSpace(llmCategoryChoice))
            {
                _logger.Log($"WAARSCHUWING: Kon '{fileInfo.Name}' niet classificeren. Bestand wordt niet verplaatst.");
                return (false, false, false, false);
            }

            // 3. Categorie folder bepalen
            string targetCategoryFolderName = ApplicationSettings.FolderCategories.ContainsKey(llmCategoryChoice)
                ? ApplicationSettings.FolderCategories[llmCategoryChoice]
                : ApplicationSettings.FallbackFolderName;
            string targetCategoryFolderPath = Path.Combine(destinationBasePath, targetCategoryFolderName);
            Directory.CreateDirectory(targetCategoryFolderPath);

            // 4. Submap suggereren
            string subfolderNameSuggestion = await _aiService.SuggestSubfolderNameAsync(
                extractedText,
                fileInfo.Name,
                currentAiProvider,
                modelName,
                cancellationToken
            );
            _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
            TokensUsedUpdated?.Invoke(_totalTokensUsed);

            bool hadSubfolder = false;
            string aiSuggestedSubfolderName = null;
            if (!string.IsNullOrWhiteSpace(subfolderNameSuggestion))
            {
                subfolderNameSuggestion = FileUtils.SanitizeFolderOrFileName(subfolderNameSuggestion);
                if (subfolderNameSuggestion.Length >= ApplicationSettings.MinSubfolderNameLength &&
                    subfolderNameSuggestion.Length <= ApplicationSettings.MaxSubfolderNameLength)
                {
                    aiSuggestedSubfolderName = subfolderNameSuggestion;
                    _logger.Log($"INFO: AI suggereerde submap: '{aiSuggestedSubfolderName}'");
                    hadSubfolder = true;
                }
                else
                {
                    _logger.Log($"WAARSCHUWING: Ongeldige lengte AI-submapnaam '{subfolderNameSuggestion}'. Geen submap gebruikt.");
                }
            }

            // 5. Bepaal doelmap
            string finalTargetDirectory;
            if (hadSubfolder)
            {
                finalTargetDirectory = Path.Combine(targetCategoryFolderPath, aiSuggestedSubfolderName);
            }
            else
            {
                // Behoud originele relatieve padstructuur
                string relativePathFromSource = FileUtils.GetRelativePath(sourcePath, Path.GetDirectoryName(filePath));
                finalTargetDirectory = Path.Combine(targetCategoryFolderPath, relativePathFromSource);
            }
            Directory.CreateDirectory(finalTargetDirectory);

            // 6. Bestandsnaam AI-voorstel
            string newFileName = fileInfo.Name;
            bool wasRenamed = false;

            if (shouldRenameFiles)
            {
                string suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                    extractedText,
                    fileInfo.Name,
                    currentAiProvider,
                    modelName,
                    cancellationToken
                );
                _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                TokensUsedUpdated?.Invoke(_totalTokensUsed);

                if (RequestRenameFile == null)
                {
                    newFileName = FileUtils.SanitizeFileName(suggestedNewBaseName + fileInfo.Extension);
                    string baseNameWithoutExt = Path.GetFileNameWithoutExtension(newFileName);
                    string extension = Path.GetExtension(newFileName);
                    if (baseNameWithoutExt.Length > ApplicationSettings.MaxFilenameLength)
                    {
                        baseNameWithoutExt = baseNameWithoutExt.Substring(0, ApplicationSettings.MaxFilenameLength);
                        newFileName = baseNameWithoutExt + extension;
                    }
                    if (newFileName != fileInfo.Name) wasRenamed = true;
                }
                else
                {
                    var (dialogResult, returnedFileName, skipFile) = await RequestRenameFile.Invoke(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension);
                    if (dialogResult == DialogResult.OK)
                    {
                        if (!skipFile)
                        {
                            string proposedFullName = returnedFileName;
                            string proposedBaseName = Path.GetFileNameWithoutExtension(proposedFullName);
                            string proposedExtension = Path.GetExtension(proposedFullName);
                            if (string.IsNullOrEmpty(proposedExtension))
                            {
                                proposedFullName = proposedBaseName + fileInfo.Extension;
                            }
                            else if (proposedExtension.ToLower() != fileInfo.Extension.ToLower())
                            {
                                proposedFullName = proposedBaseName + fileInfo.Extension;
                            }
                            newFileName = FileUtils.SanitizeFileName(proposedFullName);
                            string baseNameWithoutExt = Path.GetFileNameWithoutExtension(newFileName);
                            string extension = Path.GetExtension(newFileName);
                            if (baseNameWithoutExt.Length > ApplicationSettings.MaxFilenameLength)
                            {
                                baseNameWithoutExt = baseNameWithoutExt.Substring(0, ApplicationSettings.MaxFilenameLength);
                                newFileName = baseNameWithoutExt + extension;
                            }
                            if (newFileName != fileInfo.Name) wasRenamed = true;
                        }
                    }
                    else
                    {
                        // User cancel, skip file
                        _logger.Log($"INFO: Hernoem-actie voor '{fileInfo.Name}' geannuleerd. Bestand wordt overgeslagen.");
                        return (false, false, false, false);
                    }
                }
            }

            // 7. Bestemmingspad en conflictcheck
            string destinationFilePath = Path.Combine(finalTargetDirectory, newFileName);
            if (File.Exists(destinationFilePath))
            {
                string baseName = Path.GetFileNameWithoutExtension(newFileName);
                string extension = Path.GetExtension(newFileName);
                int counter = 1;
                string uniqueDestinationFilePath = destinationFilePath;
                while (File.Exists(uniqueDestinationFilePath))
                {
                    uniqueDestinationFilePath = Path.Combine(finalTargetDirectory, $"{baseName}_{counter}{extension}");
                    counter++;
                }
                _logger.Log($"INFO: Bestand '{newFileName}' bestaat al op doel. Hernoemd naar '{Path.GetFileName(uniqueDestinationFilePath)}' om conflict te voorkomen.");
                destinationFilePath = uniqueDestinationFilePath;
            }

            File.Move(filePath, destinationFilePath);
            _logger.Log($"OK: '{fileInfo.Name}' verplaatst naar '{FileUtils.GetRelativePath(destinationBasePath, destinationFilePath)}'");
            return (true, true, hadSubfolder, wasRenamed);
        }

        /// <summary>
        /// Factory method to get the correct AI provider based on selection.
        /// </summary>
        private IAiProvider GetAiProvider(string apiKey, string providerName, string azureEndpoint, string modelName)
        {
            try
            {
                switch (providerName)
                {
                    case "Gemini (Google)":
                        return new GeminiAiProvider(apiKey, _httpClient);
                    case "OpenAI (openai.com)":
                        return new OpenAiProvider(apiKey);
                    case "Azure OpenAI":
                        return new AzureOpenAiProvider(azureEndpoint, apiKey);
                    case "Lokaal ONNX-model":
                        // Check for required ONNX path!
                        if (string.IsNullOrEmpty(SelectedOnnxModelPath) || !File.Exists(SelectedOnnxModelPath))
                        {
                            _logger.Log("FOUT: Geen geldig ONNX-model geselecteerd. Kies eerst een ONNX-modelbestand.");
                            return null;
                        }
                        // Optioneel: pass vocab path indien nodig
                        return new OnnxRobBERTProvider(SelectedOnnxModelPath, SelectedOnnxVocabPath); // Of alleen model path als vocab niet vereist is
                    default:
                        _logger.Log($"FOUT: Onbekende AI-provider geselecteerd: '{providerName}'.");
                        return null;
                }
            }
            catch (ArgumentException argEx)
            {
                _logger.Log($"FOUT bij initialiseren AI Provider '{providerName}': {argEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Log($"ALGEMENE FOUT bij initialiseren AI Provider '{providerName}': {ex.Message}");
                return null;
            }

        }
    }
}
