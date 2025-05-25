using AI_FileOrganizer.Models; // For ApplicationSettings
using AI_FileOrganizer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms; // Needed for DialogResult

namespace AI_FileOrganizer.Services
{
    public class FileOrganizerService
    {
        private readonly ILogger _logger;
        private readonly AiClassificationService _aiService;
        private readonly TextExtractionService _textExtractionService;
        private readonly CredentialStorageService _credentialStorageService;
        private readonly HttpClient _httpClient; // Shared HttpClient for AI providers

        // Events for UI updates
        public event Action<int, int> ProgressChanged; // (currentFileIndex, totalFiles)
        public event Action<long> TokensUsedUpdated; // (totalTokensUsed)
        // Callback for interactive rename. Returns (DialogResult, newFileName, skipFile)
        public event Func<string, string, Task<(DialogResult result, string newFileName, bool skipFile)>> RequestRenameFile;

        // Note: _totalTokensUsed tracking needs to be properly implemented within IAiProvider for accurate reporting.
        // For now, it's a placeholder.
        private long _totalTokensUsed = 0;

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

        /// <summary>
        /// Orchestrates the main file organization process.
        /// </summary>
        public async Task OrganizeFilesAsync(string sourcePath, string destinationBasePath, string apiKey, string providerName, string modelName, string azureEndpoint, bool shouldRenameFiles, CancellationToken cancellationToken)
        {
            _totalTokensUsed = 0; // Reset token counter for each new run
            TokensUsedUpdated?.Invoke(_totalTokensUsed);

            IAiProvider currentAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint);
            if (currentAiProvider == null) return; // Error already logged by GetAiProvider

            // Save API key using the credential storage service
            _credentialStorageService.SaveApiKey(providerName, apiKey, azureEndpoint);

            // Ensure destination base path exists
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

            int processedCount = 0;
            int movedFiles = 0;
            int filesWithSubfolders = 0;
            int renamedFiles = 0;

            ProgressChanged?.Invoke(0, allFiles.Count);

            foreach (string filePath in allFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested(); // Propagate cancellation
                }

                var fileInfo = new FileInfo(filePath);
                processedCount++;
                _logger.Log($"\n[BESTAND] Verwerken van: {fileInfo.Name} (locatie: {Path.GetDirectoryName(filePath)})");

                try
                {
                    var (processed, moved, hadSubfolder, renamed) = await ProcessAndMoveSingleFileInternalAsync(
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
                    throw; // Important: rethrow to be caught by Form1's main try-catch
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

        /// <summary>
        /// Handles interactive renaming of a single selected file.
        /// </summary>
        public async Task RenameSingleFileInteractiveAsync(string filePath, string apiKey, string providerName, string modelName, string azureEndpoint, CancellationToken cancellationToken)
        {
            _totalTokensUsed = 0; // Reset tokens for single operation
            TokensUsedUpdated?.Invoke(_totalTokensUsed);

            IAiProvider currentAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint);
            if (currentAiProvider == null) return; // Error already logged

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
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.Log($"INFO: Geen zinvolle tekst geëxtraheerd uit {fileInfo.Name}. Gebruik bestandsnaam als context.");
                    extractedText = fileInfo.Name;
                }
                if (extractedText.Length > ApplicationSettings.MaxTextLengthForLlm)
                {
                    _logger.Log($"WAARSCHUWING: Tekstlengte voor '{fileInfo.Name}' overschrijdt {ApplicationSettings.MaxTextLengthForLlm} tekens. Tekst wordt afgekapt.");
                    extractedText = extractedText.Substring(0, ApplicationSettings.MaxTextLengthForLlm);
                }

                _logger.Log($"INFO: AI-bestandsnaam genereren voor '{fileInfo.Name}'...");
                string suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                    extractedText,
                    fileInfo.Name,
                    currentAiProvider,
                    modelName,
                    cancellationToken
                );

                // Request UI interaction for rename
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
                throw; // Propagate cancellation
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
            string extractedText = _textExtractionService.ExtractText(filePath);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.Log($"INFO: Geen zinvolle tekst geëxtraheerd uit {fileInfo.Name}. Bestand wordt behandeld met bestandsnaam context (als fallback).");
                extractedText = fileInfo.Name; // Use filename as minimal context
            }

            if (extractedText.Length > ApplicationSettings.MaxTextLengthForLlm)
            {
                _logger.Log($"WAARSCHUWING: Tekstlengte voor '{fileInfo.Name}' overschrijdt {ApplicationSettings.MaxTextLengthForLlm} tekens. Tekst wordt afgekapt.");
                extractedText = extractedText.Substring(0, ApplicationSettings.MaxTextLengthForLlm);
            }

            string llmCategoryChoice = await _aiService.ClassifyCategoryAsync(
                extractedText,
                filePath,
                ApplicationSettings.FolderCategories.Keys.ToList(),
                currentAiProvider,
                modelName,
                cancellationToken
            );

            if (string.IsNullOrWhiteSpace(llmCategoryChoice))
            {
                _logger.Log($"WAARSCHUWING: Kon '{fileInfo.Name}' niet classificeren met AI (retourneerde leeg of None). Bestand wordt niet verplaatst.");
                return (false, false, false, false);
            }

            string targetCategoryFolderName = ApplicationSettings.FolderCategories.ContainsKey(llmCategoryChoice)
                ? ApplicationSettings.FolderCategories[llmCategoryChoice]
                : ApplicationSettings.FallbackFolderName;
            string targetCategoryFolderPath = Path.Combine(destinationBasePath, targetCategoryFolderName);
            Directory.CreateDirectory(targetCategoryFolderPath); // Ensure the category folder exists

            string aiSuggestedSubfolderName = null;
            bool hadSubfolder = false;

            _logger.Log($"INFO: Poging tot genereren submapnaam voor '{fileInfo.Name}'...");
            string subfolderNameSuggestion = await _aiService.SuggestSubfolderNameAsync(
                extractedText,
                fileInfo.Name,
                currentAiProvider,
                modelName,
                cancellationToken
            );

            if (!string.IsNullOrWhiteSpace(subfolderNameSuggestion))
            {
                subfolderNameSuggestion = FileUtils.SanitizeFolderOrFileName(subfolderNameSuggestion);
                if (subfolderNameSuggestion.Length < ApplicationSettings.MinSubfolderNameLength || subfolderNameSuggestion.Length > ApplicationSettings.MaxSubfolderNameLength)
                {
                    _logger.Log($"WAARSCHUWING: AI-gegenereerde submapnaam '{subfolderNameSuggestion}' is ongeldig (lengte). Wordt niet gebruikt.");
                    subfolderNameSuggestion = null; // Set to null if invalid to trigger fallback
                }
            }

            if (!string.IsNullOrWhiteSpace(subfolderNameSuggestion))
            {
                aiSuggestedSubfolderName = subfolderNameSuggestion; // Store the validated AI-suggested name
                _logger.Log($"INFO: AI suggereerde submap: '{aiSuggestedSubfolderName}'");
                hadSubfolder = true;
            }
            else
            {
                _logger.Log($"INFO: Geen geschikte submapnaam gegenereerd. Bestand komt direct in categorie '{targetCategoryFolderName}' of in originele submap structuur.");
            }

            string finalTargetDirectory;

            // AANGEPAST: Bepaal het uiteindelijke doelpad
            if (hadSubfolder)
            {
                // Als er een AI-gesuggereerde submap is, plaats het bestand DIRECT daarin.
                finalTargetDirectory = Path.Combine(targetCategoryFolderPath, aiSuggestedSubfolderName);
            }
            else
            {
                // Als er GEEN AI-gesuggereerde submap is, behoud dan de originele relatieve mapstructuur onder de categorie.
                string relativePathFromSource = FileUtils.GetRelativePath(sourcePath, Path.GetDirectoryName(filePath));
                finalTargetDirectory = Path.Combine(targetCategoryFolderPath, relativePathFromSource);
            }
            Directory.CreateDirectory(finalTargetDirectory); // Ensure the full target directory structure exists


            string newFileName = fileInfo.Name;
            bool wasRenamed = false;

            if (shouldRenameFiles)
            {
                _logger.Log($"INFO: AI-bestandsnaam genereren voor '{fileInfo.Name}'...");
                string suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                    extractedText,
                    fileInfo.Name,
                    currentAiProvider,
                    modelName,
                    cancellationToken
                );

                if (RequestRenameFile == null)
                {
                    _logger.Log("WAARSCHUWING: UI callback voor hernoemen is niet ingesteld. Kan bestand niet interactief hernoemen. Gebruik AI suggestie direct.");
                    newFileName = FileUtils.SanitizeFileName(suggestedNewBaseName + fileInfo.Extension);
                    string baseNameWithoutExt = Path.GetFileNameWithoutExtension(newFileName);
                    string extension = Path.GetExtension(newFileName);
                    if (baseNameWithoutExt.Length > ApplicationSettings.MaxFilenameLength)
                    {
                        baseNameWithoutExt = baseNameWithoutExt.Substring(0, ApplicationSettings.MaxFilenameLength);
                        newFileName = baseNameWithoutExt + extension;
                        _logger.Log($"WAARSCHUWING: Nieuwe bestandsnaam te lang. Afgekapt naar '{newFileName}'.");
                    }
                    if (newFileName != fileInfo.Name) wasRenamed = true;
                }
                else
                {
                    // Call the UI callback to show the rename form
                    var (dialogResult, returnedFileName, skipFile) = await RequestRenameFile.Invoke(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension);

                    if (dialogResult == DialogResult.OK)
                    {
                        if (skipFile)
                        {
                            _logger.Log($"INFO: Gebruiker koos om '{fileInfo.Name}' niet te hernoemen. Bestand wordt verplaatst met originele naam.");
                            // newFileName remains the original name
                        }
                        else
                        {
                            string proposedFullName = returnedFileName;
                            string proposedBaseName = Path.GetFileNameWithoutExtension(proposedFullName);
                            string proposedExtension = Path.GetExtension(proposedFullName);

                            // Ensure extension is preserved or handled correctly
                            if (string.IsNullOrEmpty(proposedExtension))
                            {
                                proposedFullName = proposedBaseName + fileInfo.Extension;
                            }
                            else if (proposedExtension.ToLower() != fileInfo.Extension.ToLower())
                            {
                                _logger.Log($"WAARSCHUWING: Bestandsnaam '{proposedFullName}' heeft afwijkende extensie. Originele extensie '{fileInfo.Extension}' wordt behouden.");
                                proposedFullName = proposedBaseName + fileInfo.Extension; // Force original extension
                            }

                            newFileName = FileUtils.SanitizeFileName(proposedFullName); // Sanitize the confirmed new name

                            // Apply max length constraint
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
                                _logger.Log($"INFO: '{fileInfo.Name}' wordt hernoemd naar '{newFileName}'.");
                                wasRenamed = true;
                            }
                            else
                            {
                                _logger.Log($"INFO: AI-suggestie voor '{fileInfo.Name}' was gelijk aan origineel of ongeldig na opschonen, niet hernoemd.");
                            }
                        }
                    }
                    else
                    {
                        // User cancelled the rename dialog, means skip this file for now
                        _logger.Log($"INFO: Hernoem-actie voor '{fileInfo.Name}' geannuleerd door gebruiker. Bestand wordt overgeslagen.");
                        return (false, false, false, false); // Return false for processed, so it's not counted as moved/renamed
                    }
                }
            }

            string destinationFilePath = Path.Combine(finalTargetDirectory, newFileName);

            // Handle existing file names in target directory
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
        private IAiProvider GetAiProvider(string apiKey, string providerName, string azureEndpoint)
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
                    default:
                        _logger.Log($"FOUT: Onbekende AI-provider geselecteerd: {providerName}. Kan actie niet uitvoeren.");
                        return null;
                }
            }
            catch (ArgumentException ex) // Catch specific exceptions from provider constructors (e.g., invalid Azure endpoint)
            {
                _logger.Log($"FOUT: Configuratieprobleem voor AI-provider '{providerName}': {ex.Message}. Kan actie niet uitvoeren.");
                return null;
            }
        }
    }
}