// AI_FileOrganizer/Services/FileOrganizerService.cs
using AI_FileOrganizer.Models; // Veronderstelt dat ApplicationSettings hier is
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
        private readonly TextExtractionService _textExtractionService; // Veronderstelt dat deze klasse bestaat
        private readonly CredentialStorageService _credentialStorageService; // Veronderstelt dat deze klasse bestaat
        private readonly HttpClient _httpClient;

        public event Action<int, int> ProgressChanged;
        public event Action<long> TokensUsedUpdated;
        public event Func<string, string, Task<(DialogResult result, string newFileName, bool skipFile)>> RequestRenameFile;

        private long _totalTokensUsed = 0;

        public string SelectedOnnxModelPath { get; set; }
        public string SelectedOnnxVocabPath { get; set; }

        public FileOrganizerService(
            ILogger logger,
            AiClassificationService aiService,
            TextExtractionService textExtractionService, // Zorg dat deze klasse bestaat en correct geïnjecteerd wordt
            CredentialStorageService credentialStorageService, // Zorg dat deze klasse bestaat
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
            if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);


            IAiProvider currentAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint, modelName);
            if (currentAiProvider == null)
            {
                _logger.Log("FOUT: Kon AI Provider niet initialiseren. Organisatie gestopt.");
                return;
            }

            if (providerName != "Lokaal ONNX-model" && !string.IsNullOrEmpty(apiKey)) // Alleen opslaan als er een API key is
            {
                _credentialStorageService.SaveApiKey(providerName, apiKey, azureEndpoint);
            }


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

            var allFiles = new List<string>();
            try
            {
                allFiles = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                   .Where(f => ApplicationSettings.SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                                   .ToList();
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Kon bestanden niet lezen uit bronmap '{sourcePath}': {ex.Message}");
                return;
            }


            int processedCount = 0, movedFiles = 0, filesWithSubfolders = 0, renamedFiles = 0;
            if (ProgressChanged != null) ProgressChanged.Invoke(0, allFiles.Count);


            foreach (string filePath in allFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Log("INFO: Organisatieproces geannuleerd door gebruiker.");
                    cancellationToken.ThrowIfCancellationRequested();
                }


                FileInfo fileInfo = new FileInfo(filePath);
                processedCount++;
                _logger.Log($"\n[BESTAND {processedCount}/{allFiles.Count}] Verwerken van: {fileInfo.Name} (locatie: {Path.GetDirectoryName(filePath)})");

                try
                {
                    var resultTuple = await ProcessAndMoveSingleFileInternalAsync(
                        filePath,
                        fileInfo,
                        sourcePath, // sourcePath wordt hier meegegeven
                        destinationBasePath,
                        currentAiProvider,
                        modelName,
                        shouldRenameFiles,
                        cancellationToken).ConfigureAwait(false);

                    if (resultTuple.processed)
                    {
                        if (resultTuple.moved) movedFiles++;
                        if (resultTuple.hadSubfolder) filesWithSubfolders++;
                        if (resultTuple.renamed) renamedFiles++;
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.Log("INFO: Verwerking van bestand geannuleerd.");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT: Onverwachte fout bij verwerken van {fileInfo.Name}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                }
                finally
                {
                    if (ProgressChanged != null) ProgressChanged.Invoke(processedCount, allFiles.Count);
                }
            }

            _logger.Log($"\n--- SAMENVATTING ---");
            _logger.Log($"Totaal aantal bestanden bekeken (met ondersteunde extensie): {processedCount}");
            _logger.Log($"Aantal bestanden succesvol verplaatst: {movedFiles}");
            _logger.Log($"Aantal bestanden hernoemd: {renamedFiles}");
            _logger.Log($"Totaal gesimuleerde tokens gebruikt: {_totalTokensUsed}");
            _logger.Log($"--- EINDE SAMENVATTING ---");
        }

        public async Task RenameSingleFileInteractiveAsync(
            string filePath, string apiKey, string providerName, string modelName, string azureEndpoint, CancellationToken cancellationToken)
        {
            _totalTokensUsed = 0;
            if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);


            IAiProvider currentAiProvider = GetAiProvider(apiKey, providerName, azureEndpoint, modelName);
            if (currentAiProvider == null) return;

            if (providerName != "Lokaal ONNX-model" && !string.IsNullOrEmpty(apiKey))
            {
                _credentialStorageService.SaveApiKey(providerName, apiKey, azureEndpoint);
            }


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
                _logger.Log($"INFO: Geëxtraheerde tekst (eerste 100 karakters): '{(extractedText?.Length > 100 ? extractedText.Substring(0, 100) : extractedText)}'...");
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
                ).ConfigureAwait(false);
                _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);


                if (RequestRenameFile == null)
                {
                    _logger.Log("FOUT: UI callback voor hernoemen is niet ingesteld. Kan bestand niet interactief hernoemen.");
                    return;
                }

                var (dialogResult, returnedFileName, skipFile) = await RequestRenameFile.Invoke(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension).ConfigureAwait(false);


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

                            if (File.Exists(destinationFilePath) && !destinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                string baseNameConflict = Path.GetFileNameWithoutExtension(newFileNameSanitized);
                                string extensionConflict = Path.GetExtension(newFileNameSanitized);
                                int counter = 1;
                                string uniqueDestinationFilePath = destinationFilePath;
                                while (File.Exists(uniqueDestinationFilePath))
                                {
                                    uniqueDestinationFilePath = Path.Combine(Path.GetDirectoryName(filePath), $"{baseNameConflict}_{counter}{extensionConflict}");
                                    counter++;
                                }
                                _logger.Log($"INFO: Bestand '{newFileNameSanitized}' bestaat al. Hernoemd naar '{Path.GetFileName(uniqueDestinationFilePath)}' om conflict te voorkomen.");
                                destinationFilePath = uniqueDestinationFilePath;
                            }

                            if (!destinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Move(filePath, destinationFilePath);
                                _logger.Log($"OK: '{fileInfo.Name}' hernoemd naar '{Path.GetFileName(destinationFilePath)}'.");
                            }
                            else
                            {
                                _logger.Log($"INFO: Doel bestandsnaam '{Path.GetFileName(destinationFilePath)}' is hetzelfde als origineel (mogelijk na opschonen of case-verschil). Niet hernoemd.");
                            }
                        }
                        else
                        {
                            _logger.Log($"INFO: AI-suggestie was gelijk aan origineel, leeg, of ongeldig na opschonen. '{fileInfo.Name}' niet hernoemd.");
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
                _logger.Log($"FOUT: Fout bij hernoemen van {fileInfo.Name}: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            finally
            {
                _logger.Log("\nEnkel bestand hernoemen voltooid.");
                if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);
            }
        }

        private async Task<(bool processed, bool moved, bool hadSubfolder, bool renamed)> ProcessAndMoveSingleFileInternalAsync(
            string filePath,
            FileInfo fileInfo,
            string currentSourcePath, // Hernoemd van sourcePath om verwarring te voorkomen met de parameter van de parent
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
                extractedText = fileInfo.Name;
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
            ).ConfigureAwait(false);
            _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
            if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);


            if (string.IsNullOrWhiteSpace(llmCategoryChoice) || llmCategoryChoice == "Overig")
            {
                _logger.Log($"WAARSCHUWING: Kon '{fileInfo.Name}' niet classificeren of viel in 'Overig'. Bestand wordt niet verplaatst.");
                return (false, false, false, false);
            }

            // 3. Categorie folder bepalen
            string targetCategoryFolderName;
            if (!ApplicationSettings.FolderCategories.TryGetValue(llmCategoryChoice, out targetCategoryFolderName))
            {
                targetCategoryFolderName = ApplicationSettings.FallbackFolderName;
            }
            string targetCategoryFolderPath = Path.Combine(destinationBasePath, targetCategoryFolderName);
            Directory.CreateDirectory(targetCategoryFolderPath);

            // 4. Submap suggereren - DEZE STAP WORDT OVERGESLAGEN
            bool hadSubfolder = false;

            // 5. Bepaal doelmap (geen AI submap, direct in categorie of behoud relatief pad)
            // *** HIER IS DE AANGEPASTE LOGICA ***
            string finalTargetDirectory;
            string originalFileDirectory = Path.GetDirectoryName(filePath);
            string normalizedSourcePath = currentSourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string relativePath = "";

            if (!originalFileDirectory.Equals(normalizedSourcePath, StringComparison.OrdinalIgnoreCase))
            {
                // Bestand staat in een submap van currentSourcePath
                relativePath = FileUtils.GetRelativePath(currentSourcePath, originalFileDirectory);
            }

            if (string.IsNullOrEmpty(relativePath))
            {
                finalTargetDirectory = targetCategoryFolderPath; // Plaats direct in de hoofdcategoriemap
            }
            else
            {
                finalTargetDirectory = Path.Combine(targetCategoryFolderPath, relativePath);
            }

            Directory.CreateDirectory(finalTargetDirectory);
            // *** EINDE AANGEPASTE LOGICA ***

            // 6. Bestandsnaam AI-voorstel
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
                ).ConfigureAwait(false);
                _totalTokensUsed += _aiService.LastCallSimulatedTokensUsed;
                if (TokensUsedUpdated != null) TokensUsedUpdated.Invoke(_totalTokensUsed);


                if (RequestRenameFile == null)
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
                            wasRenamed = true;
                            _logger.Log($"INFO: AI suggereerde nieuwe bestandsnaam: '{newFileName}'");
                        }
                        else
                        {
                            _logger.Log($"INFO: AI-suggestie voor bestandsnaam was niet bruikbaar of gelijk aan origineel. '{fileInfo.Name}' niet hernoemd.");
                        }
                    }
                    else
                    {
                        _logger.Log($"INFO: AI gaf geen suggestie voor bestandsnaam. '{fileInfo.Name}' niet hernoemd.");
                    }
                }
                else
                {
                    var (dialogResult, returnedFileName, skipFile) = await RequestRenameFile.Invoke(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension).ConfigureAwait(false);
                    if (dialogResult == DialogResult.OK)
                    {
                        if (skipFile)
                        {
                            _logger.Log($"INFO: Hernoem-actie voor '{fileInfo.Name}' overgeslagen door gebruiker. Bestand wordt niet verplaatst/hernoemd.");
                            return (false, false, false, false);
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
                            else if (!proposedExtension.Equals(fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.Log($"WAARSCHUWING: Hernoemde bestandsnaam '{proposedFullName}' heeft afwijkende extensie. Originele extensie '{fileInfo.Extension}' behouden.");
                                proposedFullName = proposedBaseName + fileInfo.Extension;
                            }

                            string tempNewFileName = FileUtils.SanitizeFileName(proposedFullName);
                            string baseNameWithoutExt = Path.GetFileNameWithoutExtension(tempNewFileName);
                            string extension = Path.GetExtension(tempNewFileName);
                            if (baseNameWithoutExt.Length > ApplicationSettings.MaxFilenameLength)
                            {
                                baseNameWithoutExt = baseNameWithoutExt.Substring(0, ApplicationSettings.MaxFilenameLength);
                                tempNewFileName = baseNameWithoutExt + extension;
                                _logger.Log($"WAARSCHUWING: Hernoemde bestandsnaam te lang. Afgekapt naar '{tempNewFileName}'.");
                            }

                            if (!string.IsNullOrWhiteSpace(tempNewFileName) && tempNewFileName != fileInfo.Name)
                            {
                                newFileName = tempNewFileName;
                                wasRenamed = true;
                            }
                        }
                    }
                    else
                    {
                        _logger.Log($"INFO: Hernoem-actie voor '{fileInfo.Name}' geannuleerd door gebruiker. Bestand wordt niet verplaatst/hernoemd.");
                        return (false, false, false, false);
                    }
                }
            }

            // 7. Bestemmingspad en conflictcheck
            string destinationFilePath = Path.Combine(finalTargetDirectory, newFileName);
            if (File.Exists(destinationFilePath))
            {
                if (destinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase) && newFileName.Equals(fileInfo.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Log($"INFO: Bestand '{fileInfo.Name}' is al op de doel locatie en wordt niet hernoemd. Geen verplaatsing nodig.");
                    return (true, false, false, false);
                }

                string baseName = Path.GetFileNameWithoutExtension(newFileName);
                string extension = Path.GetExtension(newFileName);
                int counter = 1;
                string uniqueDestinationFilePath = destinationFilePath;
                while (File.Exists(uniqueDestinationFilePath) && !uniqueDestinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    uniqueDestinationFilePath = Path.Combine(finalTargetDirectory, $"{baseName}_{counter}{extension}");
                    counter++;
                }

                if (File.Exists(uniqueDestinationFilePath) && !uniqueDestinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Log($"FOUT: Kon geen unieke bestandsnaam vinden voor '{newFileName}' in '{finalTargetDirectory}'. Bestand wordt overgeslagen.");
                    return (false, false, false, false);
                }

                if (!destinationFilePath.Equals(uniqueDestinationFilePath))
                {
                    _logger.Log($"INFO: Bestand '{newFileName}' bestaat al op doel. Hernoemd naar '{Path.GetFileName(uniqueDestinationFilePath)}' om conflict te voorkomen.");
                    destinationFilePath = uniqueDestinationFilePath;
                }
            }

            if (destinationFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Log($"INFO: Doelpad voor '{fileInfo.Name}' is hetzelfde als bronpad. Geen verplaatsing nodig.");
                return (true, false, hadSubfolder, wasRenamed);
            }

            try
            {
                File.Move(filePath, destinationFilePath);
                _logger.Log($"OK: '{fileInfo.Name}' verplaatst naar '{FileUtils.GetRelativePath(destinationBasePath, destinationFilePath)}'");
                return (true, true, hadSubfolder, wasRenamed);
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 183)
            {
                _logger.Log($"FOUT: Kan '{fileInfo.Name}' niet verplaatsen naar '{Path.GetFileName(destinationFilePath)}'. Bestand bestaat al (race condition?): {ioEx.Message}");
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
            try
            {
                switch (providerName)
                {
                    case "Gemini (Google)":
                        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key voor Gemini is vereist.");
                        return new GeminiAiProvider(apiKey, _httpClient);
                    case "OpenAI (openai.com)":
                        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key voor OpenAI is vereist.");
                        return new OpenAiProvider(apiKey);
                    case "Azure OpenAI":
                        if (string.IsNullOrWhiteSpace(azureEndpoint)) throw new ArgumentException("Azure Endpoint voor Azure OpenAI is vereist.");
                        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("API Key voor Azure OpenAI is vereist.");
                        return new AzureOpenAiProvider(azureEndpoint, apiKey);
                    case "Lokaal ONNX-model":
                        if (string.IsNullOrEmpty(SelectedOnnxModelPath) || !File.Exists(SelectedOnnxModelPath))
                        {
                            _logger.Log("FOUT: Geen geldig ONNX-model geselecteerd of pad is incorrect. Kies eerst een ONNX-modelbestand.");
                            return null;
                        }
                        return new OnnxRobBERTProvider(_logger, SelectedOnnxModelPath, SelectedOnnxVocabPath);
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
                _logger.Log($"ALGEMENE FOUT bij initialiseren AI Provider '{providerName}': {ex.Message}\nStackTrace: {ex.StackTrace}");
                return null;
            }
        }
    }
}