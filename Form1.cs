using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http; // Still needed for _httpClient
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json; // May not be strictly needed in Form1 anymore if only AI service uses it
using UglyToad.PdfPig; // For PDF extraction example
using DocumentFormat.OpenXml.Packaging; // For DOCX extraction example
using DocumentFormat.OpenXml.Wordprocessing; // For DOCX extraction example
using System.Diagnostics;
using DocumentFormat.OpenXml.Vml.Office; // Possibly not needed, check if used elsewhere


using AI_FileOrganizer2.Services; // Ensure this is present
using AI_FileOrganizer2.Utils; // Ensure this is present
using Microsoft.Extensions.Logging; // Ensure this is present
using ILogger = AI_FileOrganizer2.Utils.ILogger;
using Microsoft.WindowsAPICodePack.Dialogs; // Alias to avoid conflict with Microsoft.Extensions.Logging.ILogger
using Microsoft.WindowsAPICodePack.Dialogs;
namespace AI_FileOrganizer2
{
    public partial class Form1 : Form
    {

        private const int MAX_TEXT_LENGTH_FOR_LLM = 8000;
        private const int MIN_SUBFOLDER_NAME_LENGTH = 3; // Kept as it's a validation rule in Form1
        private const int MAX_SUBFOLDER_NAME_LENGTH = 50; // Kept as it's a validation rule in Form1
        private const int MAX_FILENAME_LENGTH = 100; // Maximale lengte voor AI-gegenereerde bestandsnaam
        private ILogger _logger;

        private readonly string[] SUPPORTED_EXTENSIONS = { ".pdf", ".docx", ".txt", ".md" };

        private readonly Dictionary<string, string> FOLDER_CATEGORIES = new Dictionary<string, string>
        {
            { "Financiën", "1. Financiën" },
            { "Belastingen", "2. Belastingen" },
            { "Verzekeringen", "3. Verzekeringen" },
            { "Woning", "4. Woning" },
            { "Gezondheid en Medisch", "5. Gezondheid en Medisch" },
            { "Familie en Kinderen", "6. Familie en Kinderen" },
            { "Voertuigen", "7. Voertuigen" },
            { "Persoonlijke Documenten", "8. Persoonlijke Documenten" },
            { "Hobbies en interesses", "9. Hobbies en interesses" },
            { "Carrière en Professionele Ontwikkeling", "10. Carrière en Professionele Ontwikkeling" },
            { "Bedrijfsadministratie", "11. Bedrijfsadministratie" },
            { "Reizen en vakanties", "12. Reizen en vakanties" }
        };

        private const string FALLBACK_CATEGORY_NAME = "Overig";
        private string FALLBACK_FOLDER_NAME => $"0. {FALLBACK_CATEGORY_NAME}";

        // HttpClient is still needed here as it's passed to GeminiAiProvider
        private static readonly HttpClient _httpClient = new HttpClient();
        private long _totalTokensUsed = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly AiClassificationService _aiService = new AiClassificationService();


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            _logger = new UiLogger(rtbLog); 
            if (cmbProviderSelection.Items.Count == 0)
            {
                cmbProviderSelection.Items.AddRange(new object[]
                {
                    "Gemini (Google)",
                    "OpenAI (openai.com)",
                    "Azure OpenAI"
                });
            }

            cmbProviderSelection.SelectedIndexChanged -= cmbProviderSelection_SelectedIndexChanged; // Prevent double attach
            cmbProviderSelection.SelectedIndexChanged += cmbProviderSelection_SelectedIndexChanged;
            cmbProviderSelection.SelectedIndex = 0; // Will trigger and set models

            txtApiKey.Text = "YOUR_GOOGLE_API_KEY_HERE";
            SetupApiKeyPlaceholder(txtApiKey, "YOUR_GOOGLE_API_KEY_HERE");
            txtApiKey.UseSystemPasswordChar = true;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            txtSourceFolder.Text = Path.Combine(desktopPath, "AI Organizer Bronmap");
            txtDestinationFolder.Text = Path.Combine(documentsPath, "AI Organizer Resultaat");

            lblTokensUsed.Text = "Tokens gebruikt: 0";
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Visible = false;
            btnStopOrganization.Enabled = false;
            btnSaveLog.Enabled = false;
            chkRenameFiles.Checked = false; // Default: not checked

            // Always start with Azure fields hidden
            lblAzureEndpoint.Visible = false;
            txtAzureEndpoint.Visible = false;
        }


        private void cmbProviderSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbModelSelection.Items.Clear();

            string provider = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            if (provider == "Gemini (Google)")
            {
                cmbModelSelection.Items.AddRange(new object[]
                {
                    "gemini-1.5-pro-latest",
                    "gemini-1.5-flash-latest",
                    "gemini-1.0-pro-latest",
                    "gemini-pro",
                    "gemini-2.5-pro-preview-05-06",
                    "gemini-2.5-flash-preview-04-17",
                    "gemini-2.0-flash-001",
                    "gemini-2.0-flash-lite-001"
                });
                lblApiKey.Text = "Google API Key:";
                // Hide Azure endpoint fields if present
                lblAzureEndpoint.Visible = false;
                txtAzureEndpoint.Visible = false;
            }
            else if (provider == "OpenAI (openai.com)")
            {
                cmbModelSelection.Items.AddRange(new object[]
                {
                    "gpt-4o",
                    "gpt-4-turbo",
                    "gpt-4",
                    "gpt-3.5-turbo",
                    "gpt-3.5-turbo-0125",
                    "gpt-3.5-turbo-0613"
                });
                lblApiKey.Text = "OpenAI API Key:";
                lblAzureEndpoint.Visible = false;
                txtAzureEndpoint.Visible = false;
            }
            else if (provider == "Azure OpenAI")
            {
                cmbModelSelection.Items.AddRange(new object[]
                {
                    "YOUR-AZURE-DEPLOYMENT-NAME"
                });
                lblApiKey.Text = "Azure OpenAI API Key:";
                lblAzureEndpoint.Visible = true;
                txtAzureEndpoint.Visible = true;
            }
            cmbModelSelection.SelectedIndex = 0;
        }

        private void SetupApiKeyPlaceholder(TextBox textBox, string placeholderText)
        {
            textBox.GotFocus -= RemoveApiKeyPlaceholderInternal;
            textBox.LostFocus -= AddApiKeyPlaceholderInternal;

            textBox.Tag = placeholderText;

            if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.Text == placeholderText)
            {
                textBox.Text = placeholderText;
                textBox.ForeColor = System.Drawing.Color.Gray;
            }
            else
            {
                textBox.ForeColor = System.Drawing.Color.Black;
            }

            textBox.GotFocus += RemoveApiKeyPlaceholderInternal;
            textBox.LostFocus += AddApiKeyPlaceholderInternal;
        }

        private void RemoveApiKeyPlaceholderInternal(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string placeholderText = textBox.Tag?.ToString();
            if (textBox.Text == placeholderText)
            {
                textBox.Text = "";
                textBox.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void AddApiKeyPlaceholderInternal(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string placeholderText = textBox.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholderText;
                textBox.ForeColor = System.Drawing.Color.Gray;
            }
        }

        private void btnSelectSourceFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true; // Essentieel: maakt het een map-selectie dialoogvenster
                dialog.Title = "Selecteer de bronmap met bestanden (inclusief submappen)";

                // Stel de initiële map in als de huidige tekst in het tekstveld, indien geldig
                if (Directory.Exists(txtSourceFolder.Text))
                {
                    dialog.InitialDirectory = txtSourceFolder.Text;
                }
                else
                {
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Val terug naar bureaublad
                }
                dialog.RestoreDirectory = true; // Onthoud de laatst geopende map

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtSourceFolder.Text = dialog.FileName; // FileName bevat hier de geselecteerde map
                }
            }
        }

        private void btnSelectDestinationFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true; // Essentieel: maakt het een map-selectie dialoogvenster
                dialog.Title = "Selecteer de doelmap voor geordende bestanden";
                dialog.EnsurePathExists = true; // Zorgt dat de map bestaat als je de naam in typt

                // Stel de initiële map in als de huidige tekst in het tekstveld, indien geldig
                if (Directory.Exists(txtDestinationFolder.Text))
                {
                    dialog.InitialDirectory = txtDestinationFolder.Text;
                }
                else
                {
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Val terug naar Mijn Documenten
                }
                dialog.RestoreDirectory = true; // Onthoud de laatst geopende map

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtDestinationFolder.Text = dialog.FileName; // FileName bevat hier de geselecteerde map
                }
            }
        }

        private async void btnStartOrganization_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
            SetUiEnabled(false);
            btnStopOrganization.Enabled = true;
            btnSaveLog.Enabled = false;

            string apiKey = txtApiKey.Text;
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == txtApiKey.Tag.ToString())
            {
                _logger.Log("FOUT: Gelieve een geldige API Key in te vullen.");
                SetUiEnabled(true); btnStopOrganization.Enabled = false; return;
            }

            if (!Directory.Exists(txtSourceFolder.Text))
            {
                _logger.Log($"FOUT: Bronmap '{txtSourceFolder.Text}' niet gevonden.");
                SetUiEnabled(true); btnStopOrganization.Enabled = false; return;
            }

            if (!Directory.Exists(txtDestinationFolder.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtDestinationFolder.Text);
                    _logger.Log($"[MAP] Basisdoelmap '{txtDestinationFolder.Text}' aangemaakt.");
                }
                catch (Exception ex)
                {
                    _logger.Log($"FOUT: Fout bij aanmaken basisdoelmap '{txtDestinationFolder.Text}': {ex.Message}");
                    SetUiEnabled(true); btnStopOrganization.Enabled = false; return;
                }
            }

            _logger.Log($"Starten met organiseren van bestanden uit: {txtSourceFolder.Text} (inclusief submappen)");
            _logger.Log($"Gebruikt model: {cmbModelSelection.SelectedItem}");
            if (chkRenameFiles.Checked)
            {
                _logger.Log("Bestandsnamen worden hernoemd met AI-suggesties.");
            }

            _totalTokensUsed = 0;
            UpdateTokensUsedLabel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await OrganizeFilesAsync(txtSourceFolder.Text, txtDestinationFolder.Text, apiKey, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.Log("\nOrganisatie geannuleerd door gebruiker.");
            }
            catch (Exception ex)
            {
                _logger.Log($"KRITIEKE FOUT tijdens organisatie: {ex.Message}");
            }
            finally
            {
                _logger.Log("\nOrganisatie voltooid!");
                SetUiEnabled(true);
                btnStopOrganization.Enabled = false;
                btnSaveLog.Enabled = true;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void btnStopOrganization_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _logger.Log("Annulering aangevraagd...");
            btnStopOrganization.Enabled = false;
        }

        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonSaveFileDialog()) // Gebruik CommonSaveFileDialog
            {
                // Voeg filters toe op de modernere manier
                dialog.Filters.Add(new CommonFileDialogFilter("Tekstbestanden", "*.txt"));
                dialog.DefaultExtension = "txt"; // Stel de standaardextensie in

                dialog.Title = "Sla logbestand op";
                // DE OPLOSSING: Gebruik DefaultFileName in plaats van FileName
                dialog.DefaultFileName = $"AI_Organizer_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                // Optioneel: Stel de initiële map in
                // Gebruik de doelmap als uitgangspunt, of Mijn Documenten als die niet bestaat
                if (Directory.Exists(txtDestinationFolder.Text))
                {
                    dialog.InitialDirectory = txtDestinationFolder.Text;
                }
                else
                {
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
                dialog.RestoreDirectory = true; // Onthoud de laatst geopende map

                // Toon het dialoogvenster
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    try
                    {
                        // dialog.FileName bevat het volledige pad inclusief de gekozen bestandsnaam
                        File.WriteAllText(dialog.FileName, rtbLog.Text);
                        MessageBox.Show("Logbestand succesvol opgeslagen.", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Fout bij opslaan: {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void SetUiEnabled(bool enabled)
        {
            txtApiKey.Enabled = enabled;
            txtSourceFolder.Enabled = enabled;
            btnSelectSourceFolder.Enabled = enabled;
            txtDestinationFolder.Enabled = enabled;
            btnSelectDestinationFolder.Enabled = enabled;
            cmbModelSelection.Enabled = enabled;
            cmbProviderSelection.Enabled = enabled; // Enable/disable provider selection too
            txtAzureEndpoint.Enabled = enabled; // Enable/disable Azure endpoint field
            btnStartOrganization.Enabled = enabled;
            btnSaveLog.Enabled = enabled;
            linkLabelAuthor.Enabled = enabled;
            chkRenameFiles.Enabled = enabled;
        }

        private void UpdateTokensUsedLabel()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateTokensUsedLabel));
                return;
            }
            lblTokensUsed.Text = $"Tokens gebruikt: {_totalTokensUsed}";
        }

        private async Task OrganizeFilesAsync(string sourcePath, string destinationBasePath, string apiKey, CancellationToken cancellationToken)
        {
            int processedFiles = 0;
            int movedFiles = 0;
            int filesWithSubfolders = 0;
            int renamedFiles = 0;

            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "gemini-1.5-pro-latest";
            bool shouldRenameFiles = chkRenameFiles.Checked;
            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "Gemini (Google)"; // Renamed to avoid conflict with `provider` parameter type.
            string azureEndpoint = txtAzureEndpoint?.Text;

            // Instantiate the correct AI provider based on selection
            IAiProvider currentAiProvider = null;
            try
            {
                switch (providerName)
                {
                    case "Gemini (Google)":
                        currentAiProvider = new GeminiAiProvider(apiKey, _httpClient);
                        break;
                    case "OpenAI (openai.com)":
                        currentAiProvider = new OpenAiProvider(apiKey);
                        break;
                    case "Azure OpenAI":
                        currentAiProvider = new AzureOpenAiProvider(azureEndpoint, apiKey);
                        break;
                    default:
                        _logger.Log($"FOUT: Onbekende AI-provider geselecteerd: {providerName}. Annuleer organisatie.");
                        return; // Stop the organization
                }
            }
            catch (ArgumentException ex) // Catch specific exceptions from provider constructors
            {
                _logger.Log($"FOUT: Configuratieprobleem voor AI-provider '{providerName}': {ex.Message}. Annuleer organisatie.");
                return;
            }


            var allFiles = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                    .Where(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLower()))
                                    .ToList();

            progressBar1.Maximum = allFiles.Count;
            progressBar1.Value = 0;
            progressBar1.Visible = true;

            foreach (string filePath in allFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    progressBar1.Visible = false;
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var fileInfo = new FileInfo(filePath);

                processedFiles++;
                _logger.Log($"\n[BESTAND] Verwerken van: {fileInfo.Name} (locatie: {Path.GetDirectoryName(filePath)})");

                // In a real application, you'd extract text from the file here.
                // For this example, we'll use the file path as a placeholder,
                // but you should replace this with actual text extraction logic.
                string extractedText = "Dummy text for demonstration. In a real app, extract content from: " + fileInfo.FullName;
                // TODO: Implement actual text extraction based on file type
                // Example for PDF:
                // try
                // {
                //     using (PdfDocument document = PdfDocument.Open(filePath))
                //     {
                //         extractedText = string.Join(Environment.NewLine, document.GetPages().Select(p => p.Text));
                //     }
                // }
                // catch (Exception ex)
                // {
                //     _logger.Log($"WAARSCHUWING: Kon geen tekst extraheren uit PDF {fileInfo.Name}: {ex.Message}");
                //     extractedText = fileInfo.Name; // Or use file name/path as fallback context
                // }
                // Example for DOCX:
                // else if (fileInfo.Extension.ToLower() == ".docx")
                // {
                //     try
                //     {
                //         using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                //         {
                //             extractedText = wordDoc.MainDocumentPart.Document.Body.InnerText;
                //         }
                //     }
                //     catch (Exception ex)
                //     {
                //         _logger.Log($"WAARSCHUWING: Kon geen tekst extraheren uit DOCX {fileInfo.Name}: {ex.Message}");
                //         extractedText = fileInfo.Name; // Fallback
                //     }
                // }
                // Example for TXT/MD:
                // else if (fileInfo.Extension.ToLower() == ".txt" || fileInfo.Extension.ToLower() == ".md")
                // {
                //     try
                //     {
                //         extractedText = File.ReadAllText(filePath);
                //     }
                //     catch (Exception ex)
                //     {
                //         _logger.Log($"WAARSCHUWING: Kon geen tekst extraheren uit TXT/MD {fileInfo.Name}: {ex.Message}");
                //         extractedText = fileInfo.Name; // Fallback
                //     }
                // }


                if (string.IsNullOrWhiteSpace(extractedText) || extractedText.StartsWith("Dummy text")) // Check for actual extracted content vs. placeholder
                {
                    _logger.Log($"INFO: Geen zinvolle tekst geëxtraheerd uit {fileInfo.Name}. Bestand wordt behandeld met bestandsnaam context.");
                    // For now, let's proceed with a default fallback to avoid stopping
                    extractedText = fileInfo.Name; // Use file name as a minimal context
                }

                // If text is too long for LLM, truncate it
                if (extractedText.Length > MAX_TEXT_LENGTH_FOR_LLM)
                {
                    _logger.Log($"WAARSCHUWING: Tekstlengte voor '{fileInfo.Name}' overschrijdt {MAX_TEXT_LENGTH_FOR_LLM} tekens. Tekst wordt afgekapt.");
                    extractedText = extractedText.Substring(0, MAX_TEXT_LENGTH_FOR_LLM);
                }


                // All AI calls should now use the 'currentAiProvider' instance
                string llmCategoryChoice = await _aiService.ClassifyCategoryAsync(
                    extractedText,
                    FOLDER_CATEGORIES.Keys.ToList(),
                    providerName,        // Dit is de 'string provider' parameter
                    apiKey,              // Dit is de 'string apiKey' parameter
                    currentAiProvider,   // Dit is de 'IAiProvider aiProvider' parameter
                    selectedModel,       // Dit is de 'string modelName' parameter
                    cancellationToken    // Dit is de 'CancellationToken cancellationToken' parameter
                );

                if (!string.IsNullOrWhiteSpace(llmCategoryChoice))
                {
                    string targetCategoryFolderName = FOLDER_CATEGORIES.ContainsKey(llmCategoryChoice)
                        ? FOLDER_CATEGORIES[llmCategoryChoice]
                        : FALLBACK_FOLDER_NAME;
                    string targetCategoryFolderPath = Path.Combine(destinationBasePath, targetCategoryFolderName);
                    Directory.CreateDirectory(targetCategoryFolderPath);

                    string finalDestinationFolderPath = targetCategoryFolderPath;

                    _logger.Log($"INFO: Poging tot genereren submapnaam voor '{fileInfo.Name}'...");

                    string subfolderNameSuggestion = await _aiService.SuggestSubfolderNameAsync(
                        extractedText,
                        fileInfo.Name,
                        currentAiProvider, // Pass the IAiProvider instance
                        selectedModel,
                        cancellationToken
                    );

                    if (!string.IsNullOrWhiteSpace(subfolderNameSuggestion))
                    {
                        // Sanitize and validate subfolder name
                        subfolderNameSuggestion = FileUtils.SanitizeFolderOrFileName(subfolderNameSuggestion);
                        if (subfolderNameSuggestion.Length < MIN_SUBFOLDER_NAME_LENGTH || subfolderNameSuggestion.Length > MAX_SUBFOLDER_NAME_LENGTH)
                        {
                            _logger.Log($"WAARSCHUWING: AI-gegenereerde submapnaam '{subfolderNameSuggestion}' is ongeldig (lengte). Wordt niet gebruikt.");
                            subfolderNameSuggestion = null; // Invalidate the suggestion
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(subfolderNameSuggestion))
                    {
                        string targetSubfolderPath = Path.Combine(targetCategoryFolderPath, subfolderNameSuggestion);
                        finalDestinationFolderPath = targetSubfolderPath;
                        _logger.Log($"INFO: AI suggereerde submap: '{subfolderNameSuggestion}'");
                        filesWithSubfolders++;
                    }
                    else
                    {
                        _logger.Log($"INFO: Geen geschikte submapnaam gegenereerd. Bestand komt direct in categorie '{targetCategoryFolderName}'.");
                    }

                    try
                    {
                        // Reconstruct the relative path from the original source structure
                        string relativePathFromSource = GetRelativePath(sourcePath, Path.GetDirectoryName(filePath));
                        // This ensures that if files were already in subfolders in the source, those subfolders are recreated under the new category/subfolder
                        string finalTargetDirectory = Path.Combine(finalDestinationFolderPath, relativePathFromSource);
                        Directory.CreateDirectory(finalTargetDirectory);

                        string newFileName = fileInfo.Name; // Standaard de originele naam

                        if (shouldRenameFiles)
                        {
                            _logger.Log($"INFO: AI-bestandsnaam genereren voor '{fileInfo.Name}'...");

                            string suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                                extractedText,
                                fileInfo.Name,
                                currentAiProvider, // Pass the IAiProvider instance
                                selectedModel,
                                cancellationToken
                            );

                            // Voeg een dialoogvenster in om de naam te bevestigen/wijzigen
                            // Pass only the suggested *base name* and let the form manage the extension initially
                            using (var renameForm = new FormRenameFile(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension))
                            {
                                if (renameForm.ShowDialog() == DialogResult.OK)
                                {
                                    if (renameForm.SkipFile)
                                    {
                                        _logger.Log($"INFO: Gebruiker koos om '{fileInfo.Name}' niet te hernoemen. Bestand wordt verplaatst met originele naam.");
                                        // newFileName remains fileInfo.Name
                                    }
                                    else
                                    {
                                        string proposedFullName = renameForm.NewFileName;
                                        string proposedBaseName = Path.GetFileNameWithoutExtension(proposedFullName);
                                        string proposedExtension = Path.GetExtension(proposedFullName);

                                        // Ensure the file extension is preserved or correctly handled
                                        if (string.IsNullOrEmpty(proposedExtension))
                                        {
                                            proposedFullName = proposedBaseName + fileInfo.Extension; // Add original extension if user removed it
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
                                        if (baseNameWithoutExt.Length > MAX_FILENAME_LENGTH)
                                        {
                                            baseNameWithoutExt = baseNameWithoutExt.Substring(0, MAX_FILENAME_LENGTH);
                                            newFileName = baseNameWithoutExt + extension;
                                            _logger.Log($"WAARSCHUWING: Nieuwe bestandsnaam te lang. Afgekapt naar '{newFileName}'.");
                                        }


                                        if (newFileName != fileInfo.Name)
                                        {
                                            _logger.Log($"INFO: '{fileInfo.Name}' wordt hernoemd naar '{newFileName}'.");
                                            renamedFiles++;
                                        }
                                        else
                                        {
                                            _logger.Log($"INFO: AI-suggestie voor '{fileInfo.Name}' was gelijk aan origineel of ongeldig na opschonen, niet hernoemd.");
                                        }
                                    }
                                }
                                else
                                {
                                    // User cancelled the rename dialog
                                    _logger.Log($"INFO: Hernoem-actie voor '{fileInfo.Name}' geannuleerd door gebruiker. Bestand wordt overgeslagen.");
                                    progressBar1.Increment(1);
                                    continue; // Skip to the next file
                                }
                            }
                        }

                        string destinationFilePath = Path.Combine(finalTargetDirectory, newFileName);

                        // Handle existing file names in destination folder
                        if (File.Exists(destinationFilePath))
                        {
                            string baseName = Path.GetFileNameWithoutExtension(newFileName);
                            string extension = Path.GetExtension(newFileName);
                            int counter = 1;
                            string uniqueDestinationFilePath = destinationFilePath; // Initialize
                            while (File.Exists(uniqueDestinationFilePath))
                            {
                                uniqueDestinationFilePath = Path.Combine(finalTargetDirectory, $"{baseName}_{counter}{extension}");
                                counter++;
                            }
                            _logger.Log($"INFO: Bestand '{newFileName}' bestaat al op doel. Hernoemd naar '{Path.GetFileName(uniqueDestinationFilePath)}' om conflict te voorkomen.");
                            destinationFilePath = uniqueDestinationFilePath;
                        }

                        File.Move(filePath, destinationFilePath);

                        _logger.Log($"OK: '{fileInfo.Name}' verplaatst naar '{GetRelativePath(destinationBasePath, destinationFilePath)}'");
                        movedFiles++;
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"FOUT: Fout bij verplaatsen/aanmaken map of hernoemen voor {fileInfo.Name}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.Log($"WAARSCHUWING: Kon '{fileInfo.Name}' niet classificeren met AI (retourneerde leeg of None). Wordt niet verplaatst.");
                }
                progressBar1.Increment(1);
            }

            progressBar1.Visible = false;

            _logger.Log($"\nTotaal aantal bestanden bekeken (met ondersteunde extensie): {processedFiles}");
            _logger.Log($"Aantal bestanden succesvol verplaatst: {movedFiles}");
            _logger.Log($"Aantal bestanden geplaatst in een AI-gegenereerde submap: {filesWithSubfolders}");
            _logger.Log($"Aantal bestanden hernoemd: {renamedFiles}");
        }


        private string GetRelativePath(string basePath, string fullPath)
        {
            string baseWithSeparator = AppendDirectorySeparatorChar(basePath);
            Uri baseUri = new Uri(baseWithSeparator);
            Uri fullUri = new Uri(fullPath);

            // MakeRelativeUri will return a URI representing the relative path
            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            // Convert the URI path (which uses '/') to the system's directory separator
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private string AppendDirectorySeparatorChar(string path)
        {
            if (!string.IsNullOrEmpty(path) && !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        private void linkLabelAuthor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string url = "https://www.linkedin.com/in/remseymailjard/";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kan link niet openen: {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}