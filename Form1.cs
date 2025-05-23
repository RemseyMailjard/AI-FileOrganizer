using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;
using DocumentFormat.OpenXml.Vml.Office;
using OpenAI.Chat;
using OpenAI;
using Azure.AI.OpenAI;
using Azure;
using System.ClientModel;


namespace AI_FileOrganizer2
{
    public partial class Form1 : Form
    {
        private const string GEMINI_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const int MAX_TEXT_LENGTH_FOR_LLM = 8000;
        private const int MAX_TEXT_LENGTH_FOR_SUBFOLDER_NAME = 2000;
        private const int MIN_SUBFOLDER_NAME_LENGTH = 3;
        private const int MAX_SUBFOLDER_NAME_LENGTH = 50;
        private const int MAX_FILENAME_LENGTH = 100; // Maximale lengte voor AI-gegenereerde bestandsnaam

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

        private static readonly HttpClient _httpClient = new HttpClient();
        private long _totalTokensUsed = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private System.Windows.Forms.ComboBox cmbProviderSelection;
        private System.Windows.Forms.Label lblProvider;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtApiKey.Text = "YOUR_GOOGLE_API_KEY_HERE";

            SetupApiKeyPlaceholder(txtApiKey, "YOUR_GOOGLE_API_KEY_HERE");
            txtApiKey.UseSystemPasswordChar = true;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            txtSourceFolder.Text = Path.Combine(desktopPath, "AI Organizer Bronmap");
            txtDestinationFolder.Text = Path.Combine(documentsPath, "AI Organizer Resultaat");


            this.lblProvider = new System.Windows.Forms.Label();
            this.cmbProviderSelection = new System.Windows.Forms.ComboBox();
            // Add to your layout, set Items, and add a SelectionChanged event
            this.cmbProviderSelection.Items.AddRange(new object[] {
                "Gemini (Google)",
                "OpenAI (openai.com)",
                "Azure OpenAI"
            });
this.cmbProviderSelection.SelectedIndex = 0; // Default
    cmbModelSelection.Items.AddRange(new object[] {
                "gemini-1.5-pro-latest",
                "gemini-1.5-flash-latest",
                "gemini-1.0-pro-latest",
                "gemini-pro",
                "gemini-2.5-pro-preview-05-06",
                "gemini-2.5-flash-preview-04-17",
                "gemini-2.0-flash-001",
                "gemini-2.0-flash-lite-001",
            });
            cmbModelSelection.SelectedIndex = 0;

            lblTokensUsed.Text = "Tokens gebruikt: 0";
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Visible = false;
            btnStopOrganization.Enabled = false;
            btnSaveLog.Enabled = false;

            // Nieuw: Standaardinstelling voor hernoemen van bestanden
            chkRenameFiles.Checked = false; // Standaard niet aanvinken
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
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Selecteer de bronmap met bestanden (inclusief submappen)";
                fbd.ShowNewFolderButton = false;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtSourceFolder.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnSelectDestinationFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Selecteer de doelmap voor geordende bestanden";
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDestinationFolder.Text = fbd.SelectedPath;
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
                LogMessage("FOUT: Gelieve een geldige Google API Key in te vullen.");
                SetUiEnabled(true); btnStopOrganization.Enabled = false; return;
            }

            if (!Directory.Exists(txtSourceFolder.Text))
            {
                LogMessage($"FOUT: Bronmap '{txtSourceFolder.Text}' niet gevonden.");
                SetUiEnabled(true); btnStopOrganization.Enabled = false; return;
            }

            if (!Directory.Exists(txtDestinationFolder.Text))
            {
                try
                {
                    Directory.CreateDirectory(txtDestinationFolder.Text);
                    LogMessage($"[MAP] Basisdoelmap '{txtDestinationFolder.Text}' aangemaakt.");
                }
                catch (Exception ex)
                {
                    LogMessage($"FOUT: Fout bij aanmaken basisdoelmap '{txtDestinationFolder.Text}': {ex.Message}");
                    SetUiEnabled(true); btnStopOrganization.Enabled = false; return;
                }
            }

            LogMessage($"Starten met organiseren van bestanden uit: {txtSourceFolder.Text} (inclusief submappen)");
            LogMessage($"Gebruikt Gemini model: {cmbModelSelection.SelectedItem}");
            if (chkRenameFiles.Checked)
            {
                LogMessage("Bestandsnamen worden hernoemd met AI-suggesties.");
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
                LogMessage("\nOrganisatie geannuleerd door gebruiker.");
            }
            catch (Exception ex)
            {
                LogMessage($"KRITIEKE FOUT tijdens organisatie: {ex.Message}");
            }
            finally
            {
                LogMessage("\nOrganisatie voltooid!");
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
            LogMessage("Annulering aangevraagd...");
            btnStopOrganization.Enabled = false;
        }

        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Textbestanden (*.txt)|*.txt";
                saveFileDialog.Title = "Sla logbestand op";
                saveFileDialog.FileName = $"AI_Organizer_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveFileDialog.FileName, rtbLog.Text);
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
            btnStartOrganization.Enabled = enabled;
            btnSaveLog.Enabled = enabled; // Log opslaan kan ook als alles disabled is, maar wordt apart beheerd
            linkLabelAuthor.Enabled = enabled;
            chkRenameFiles.Enabled = enabled; // Nieuw: Checkbox ook enabled/disabled maken
        }

        private void LogMessage(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => LogMessage(message)));
                return;
            }
            rtbLog.AppendText(message + Environment.NewLine);
            rtbLog.ScrollToCaret();
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
            bool shouldRenameFiles = chkRenameFiles.Checked; // Lees de status van de checkbox

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
                LogMessage($"\n[BESTAND] Verwerken van: {fileInfo.Name} (locatie: {Path.GetDirectoryName(filePath)})");

                string extractedText = ExtractText(fileInfo.FullName);
                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    LogMessage($"INFO: Geen tekst geëxtraheerd uit {fileInfo.Name}. Bestand wordt overgeslagen.");
                    progressBar1.Increment(1);
                    continue;
                }

                string llmCategoryChoice = await ClassifyTextWithGeminiAsync(extractedText, FOLDER_CATEGORIES.Keys.ToList(), apiKey, selectedModel, cancellationToken);

                if (!string.IsNullOrWhiteSpace(llmCategoryChoice))
                {
                    string targetCategoryFolderName = FOLDER_CATEGORIES.ContainsKey(llmCategoryChoice)
                        ? FOLDER_CATEGORIES[llmCategoryChoice]
                        : FALLBACK_FOLDER_NAME;
                    string targetCategoryFolderPath = Path.Combine(destinationBasePath, targetCategoryFolderName);
                    Directory.CreateDirectory(targetCategoryFolderPath);

                    string finalDestinationFolderPath = targetCategoryFolderPath;

                    string subfolderNameSuggestion = null;
                    LogMessage($"INFO: Poging tot genereren submapnaam voor '{fileInfo.Name}'...");
                    subfolderNameSuggestion = await GenerateSubfolderNameWithGeminiAsync(extractedText, fileInfo.Name, apiKey, selectedModel, cancellationToken);

                    if (!string.IsNullOrWhiteSpace(subfolderNameSuggestion))
                    {
                        string targetSubfolderPath = Path.Combine(targetCategoryFolderPath, subfolderNameSuggestion);
                        finalDestinationFolderPath = targetSubfolderPath;
                        LogMessage($"INFO: Gemini suggereerde submap: '{subfolderNameSuggestion}'");
                        filesWithSubfolders++;
                    }
                    else
                    {
                        LogMessage($"INFO: Geen geschikte submapnaam gegenereerd. Bestand komt direct in categorie '{targetCategoryFolderName}'.");
                    }

                    try
                    {
                        string relativePathFromSource = GetRelativePath(sourcePath, Path.GetDirectoryName(filePath));
                        string finalTargetDirectory = Path.Combine(finalDestinationFolderPath, relativePathFromSource);
                        Directory.CreateDirectory(finalTargetDirectory);

                        string newFileName = fileInfo.Name; // Standaard de originele naam

                        if (shouldRenameFiles)
                        {
                            LogMessage($"INFO: AI-bestandsnaam genereren voor '{fileInfo.Name}'...");
                            string suggestedNewBaseName = await GenerateFileNameWithGeminiAsync(extractedText, fileInfo.Name, apiKey, selectedModel, cancellationToken);

                            // Voeg een dialoogvenster in om de naam te bevestigen/wijzigen
                            using (var renameForm = new FormRenameFile(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension))
                            {
                                if (renameForm.ShowDialog() == DialogResult.OK)
                                {
                                    if (renameForm.SkipFile)
                                    {
                                        LogMessage($"INFO: Gebruiker koos om '{fileInfo.Name}' niet te hernoemen.");
                                        // Blijf bij de originele naam voor dit bestand
                                    }
                                    else
                                    {
                                        // Zorg ervoor dat de extensie behouden blijft
                                        string proposedFullName = renameForm.NewFileName;
                                        string proposedBaseName = Path.GetFileNameWithoutExtension(proposedFullName);
                                        string proposedExtension = Path.GetExtension(proposedFullName);

                                        if (string.IsNullOrEmpty(proposedExtension))
                                        {
                                            // Als gebruiker extensie weghaalt, voeg originele extensie toe
                                            proposedFullName = proposedBaseName + fileInfo.Extension;
                                        }
                                        else if (proposedExtension.ToLower() != fileInfo.Extension.ToLower())
                                        {
                                            // Als gebruiker een ANDERE extensie invoert, waarschuw en behoud originele
                                            LogMessage($"WAARSCHUWING: Bestandsnaam '{proposedFullName}' heeft afwijkende extensie. Originele extensie '{fileInfo.Extension}' behouden.");
                                            proposedFullName = proposedBaseName + fileInfo.Extension;
                                        }

                                        newFileName = SanitizeFileName(proposedFullName); // Opschonen nieuwe naam
                                        if (newFileName != fileInfo.Name)
                                        {
                                            LogMessage($"INFO: '{fileInfo.Name}' wordt hernoemd naar '{newFileName}'.");
                                            renamedFiles++;
                                        }
                                        else
                                        {
                                            LogMessage($"INFO: AI-suggestie voor '{fileInfo.Name}' was gelijk aan origineel of ongeldig, niet hernoemd.");
                                        }
                                    }
                                }
                                else
                                {
                                    // Gebruiker heeft annuleren gekozen in de rename dialoog
                                    LogMessage($"INFO: Hernoem-actie voor '{fileInfo.Name}' geannuleerd door gebruiker. Bestand wordt niet hernoemd of verplaatst.");
                                    progressBar1.Increment(1);
                                    continue; // Ga naar het volgende bestand
                                }
                            }
                        }

                        string destinationFilePath = Path.Combine(finalTargetDirectory, newFileName);

                        // Afhandeling bestaande bestandsnamen in doelmap
                        if (File.Exists(destinationFilePath))
                        {
                            string baseName = Path.GetFileNameWithoutExtension(newFileName);
                            string extension = Path.GetExtension(newFileName);
                            int counter = 1;
                            while (File.Exists(destinationFilePath))
                            {
                                destinationFilePath = Path.Combine(finalTargetDirectory, $"{baseName}_{counter}{extension}");
                                counter++;
                            }
                            LogMessage($"INFO: Bestand '{newFileName}' bestaat al op doel. Hernoemd naar '{Path.GetFileName(destinationFilePath)}' om conflict te voorkomen.");
                        }

                        File.Move(filePath, destinationFilePath);

                        LogMessage($"OK: '{fileInfo.Name}' verplaatst naar '{GetRelativePath(destinationBasePath, destinationFilePath)}'");
                        movedFiles++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"FOUT: Fout bij verplaatsen/aanmaken map of hernoemen voor {fileInfo.Name}: {ex.Message}");
                    }
                }
                else
                {
                    LogMessage($"WAARSCHUWING: Kon '{fileInfo.Name}' niet classificeren met Gemini (retourneerde None). Wordt niet verplaatst.");
                }
                progressBar1.Increment(1);
            }

            progressBar1.Visible = false;

            LogMessage($"\nTotaal aantal bestanden bekeken (met ondersteunde extensie): {processedFiles}");
            LogMessage($"Aantal bestanden succesvol verplaatst: {movedFiles}");
            LogMessage($"Aantal bestanden geplaatst in een AI-gegenereerde submap: {filesWithSubfolders}");
            LogMessage($"Aantal bestanden hernoemd: {renamedFiles}");
        }

        private string SanitizeFileName(string proposedFullName)
        {
            throw new NotImplementedException();
        }

        private string ExtractText(string filePath)
        {
            string text = "";
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".pdf")
                {
                    using (PdfDocument document = PdfDocument.Open(filePath))
                    {
                        if (document.NumberOfPages == 0) return "";
                        foreach (var page in document.GetPages())
                        {
                            text += page.Text;
                        }
                    }
                }
                else if (extension == ".docx")
                {
                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                    {
                        Body body = wordDoc.MainDocumentPart?.Document?.Body;
                        if (body != null)
                        {
                            text = string.Join(" ", body.Elements<Paragraph>().Select(p => p.InnerText));
                        }
                    }
                }
                else if (extension == ".txt" || extension == ".md")
                {
                    text = File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"WAARSCHUWING: Algemene fout bij lezen van bestand {Path.GetFileName(filePath)}: {ex.Message}");
            }
            return text.Trim();
        }

        private string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            // Verwijder ongeldige karakters voor bestandsnamen
            name = Regex.Replace(name, @"[<>:""/\\|?*\x00-\x1F]", "_");
            // Trim spaties en punten aan begin/einde
            name = name.Trim('.', ' ');
            // Meerdere spaties/underscores vervangen door een enkele
            name = Regex.Replace(name, @"\s+", " ").Trim();
            name = Regex.Replace(name, @"_+", "_").Trim('_');

            // Zorg ervoor dat de naam niet alleen underscores of spaties is geworden
            if (string.IsNullOrWhiteSpace(name.Replace("_", "")))
            {
                return ""; // Of een placeholder zoals "Ongeldige Naam"
            }
            return name;
        }



        private async Task<string> CallOpenAIAsync(string prompt, string modelName, string apiKey, CancellationToken cancellationToken)
        {
            var client = new OpenAI.Chat.ChatClient(model: modelName, apiKey: apiKey);
            var messages = new List<OpenAI.Chat.ChatMessage>
    {
        new OpenAI.Chat.UserChatMessage(prompt)
    };

            // Await the chat completion and get the ClientResult<ChatCompletion>
            var completionResult = await client.CompleteChatAsync(messages, options: null, cancellationToken);

            // Get the actual ChatCompletion from the .Value property
            var chatCompletion = completionResult.Value;

            // Now you can access Content on the ChatCompletion object
            var firstContent = chatCompletion.Content.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstContent?.Text))
            {
                return firstContent.Text.Trim();
            }

            return null;
        }






        /// <summary>
        /// Classifies the input text into a category using the selected LLM provider and model.
        /// </summary>
        /// <param name="textToClassify">Text to be classified.</param>
        /// <param name="categories">List of category names.</param>
        /// <param name="apiKey">API key for the provider.</param>
        /// <param name="modelName">Model or deployment name.</param>
        /// <param name="provider">Provider name ("Gemini (Google)", "OpenAI (openai.com)", "Azure OpenAI").</param>
        /// <param name="azureEndpoint">Azure endpoint (only for Azure OpenAI).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The chosen category as a string.</returns>
        private async Task<string> ClassifyTextWithLlmAsync(
                                        string textToClassify,
                                        List<string> categories,
                                        string apiKey,
                                        string modelName,
                                        string provider,
                                        string azureEndpoint,
                                        CancellationToken cancellationToken)
                                    {
                                        // Fallback names from your constants
                                        const string FALLBACK_CATEGORY_NAME = "Overig";
                                        var validCategories = new List<string>(categories) { FALLBACK_CATEGORY_NAME };

                                        if (string.IsNullOrWhiteSpace(textToClassify))
                                            return null;

                                        // Prompt (same as your original Gemini one)
                                        var categoryListForPrompt = string.Join("\n", categories.Select(cat => $"- {cat}"));
                                        var prompt = $@"
                            Je bent een AI-assistent gespecialiseerd in het organiseren van documenten.
                            Jouw taak is om de volgende tekst te analyseren en te bepalen in welke van de onderstaande categorieën deze het beste past.

                            Beschikbare categorieën:
                            {categoryListForPrompt}
                            - {FALLBACK_CATEGORY_NAME} (gebruik deze als geen andere categorie duidelijk past)

                            Geef ALLEEN de naam van de gekozen categorie terug, exact zoals deze in de lijst staat. Geen extra uitleg, nummers of opmaak.

                            Tekstfragment om te classificeren:
                            ---
                            {textToClassify.Substring(0, Math.Min(textToClassify.Length, 8000))}
                            ---

                            Categorie:";

                    // --- LLM dispatch ---
                    string chosenCategory = null;

                    try
                    {
                        switch (provider)
                        {
                            case "Gemini (Google)":
                                chosenCategory = await CallGeminiApiAsync(prompt, modelName, 50, 0.0f, apiKey, cancellationToken);
                                break;
                            case "OpenAI (openai.com)":
                                chosenCategory = await CallOpenAIAsync(prompt, modelName, apiKey, cancellationToken);
                                break;
                            case "Azure OpenAI":
                                chosenCategory = await CallAzureOpenAIAsync(prompt, azureEndpoint, modelName, apiKey, cancellationToken);
                                break;
                            default:
                                LogMessage("FOUT: Onbekende provider geselecteerd in ClassifyTextWithLlmAsync.");
                                return FALLBACK_CATEGORY_NAME;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"FOUT tijdens LLM-aanroep: {ex.Message}");
                        return FALLBACK_CATEGORY_NAME;
                    }

                    if (string.IsNullOrWhiteSpace(chosenCategory))
                        return FALLBACK_CATEGORY_NAME;

                    // Clean and match the result
                    chosenCategory = chosenCategory.Trim();

                    if (validCategories.Contains(chosenCategory))
                        return chosenCategory;

                    // Fuzzy fallback: try to match even with minor differences
                    foreach (var validCat in validCategories)
                    {
                        if (validCat.ToLower().Contains(chosenCategory.ToLower()) || chosenCategory.ToLower().Contains(validCat.ToLower()))
                        {
                            LogMessage($"INFO: Fuzzy match: '{chosenCategory}' -> '{validCat}'");
                            return validCat;
                        }
                    }

                    LogMessage($"WAARSCHUWING: Kreeg onbekende categorie '{chosenCategory}'. Gebruik fallback.");
                    return FALLBACK_CATEGORY_NAME;
                }


     
        private async Task<string> CallGeminiApiAsync(string prompt, string modelName, int maxTokens, float temperature, string apiKey, CancellationToken cancellationToken)
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    maxOutputTokens = maxTokens,
                    temperature = temperature
                }
            };

            string jsonRequest = JsonConvert.SerializeObject(requestBody);
            string endpoint = $"{GEMINI_BASE_URL}{modelName}:generateContent?key={apiKey}";

            try
            {
                var httpContent = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, httpContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorJson = await response.Content.ReadAsStringAsync();
                    dynamic errorObj = null;
                    try { errorObj = JsonConvert.DeserializeObject(errorJson); } catch { }
                    string errorMsg = errorObj?.error?.message ?? "Onbekende API-fout";
                    LogMessage($"FOUT: Gemini API (HTTP {response.StatusCode}): {errorMsg}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(jsonResponse);

                long inputTokens = result?.usageMetadata?.promptTokenCount ?? 0;
                long outputTokens = result?.usageMetadata?.candidatesTokenCount ?? 0;
                _totalTokensUsed += (inputTokens + outputTokens);
                UpdateTokensUsedLabel();

                string resultText = result?.candidates?[0]?.content?.parts?[0]?.text;

                // ROBUUSTERE LOGGING VOOR LEGE RESPONSE
                if (string.IsNullOrWhiteSpace(resultText))
                {
                    string blockReason = result?.promptFeedback?.blockReason?.ToString(); // e.g., OTHER, SAFETY, RECITATION
                    string safetyRatingsJson = result?.promptFeedback?.safetyRatings != null ? JsonConvert.SerializeObject(result.promptFeedback.safetyRatings) : "Geen safety ratings.";
                    string fullResponse = jsonResponse;

                    string debugMessage = $"WAARSCHUWING: Lege response van Gemini API. ";
                    if (!string.IsNullOrWhiteSpace(blockReason))
                    {
                        debugMessage += $"Reden van blokkering: {blockReason}. ";
                    }
                    debugMessage += $"Safety Ratings: {safetyRatingsJson}. "; // Toont de JSON van safetyRatings
                    debugMessage += $"Volledige JSON: {fullResponse}";

                    LogMessage(debugMessage);
                    return null;
                }
                return resultText.Trim();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogMessage($"FOUT Gemini API: {ex.Message}");
                return null;
            }
        }

        private async Task<string> ClassifyTextWithGeminiAsync(string textToClassify, List<string> categories, string apiKey, string modelName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(textToClassify))
            {
                return null;
            }

            var categoryListForPrompt = string.Join("\n", categories.Select(cat => $"- {cat}"));
            var prompt = $@"
Je bent een AI-assistent gespecialiseerd in het organiseren van documenten.
Jouw taak is om de volgende tekst te analyseren en te bepalen in welke van de onderstaande categorieën deze het beste past.

Beschikbare categorieën:
{categoryListForPrompt}
- {FALLBACK_CATEGORY_NAME} (gebruik deze als geen andere categorie duidelijk past)

Geef ALLEEN de naam van de gekozen categorie terug, exact zoals deze in de lijst staat. Geen extra uitleg, nummers of opmaak.

Tekstfragment om te classificeren:
---
{textToClassify.Substring(0, Math.Min(textToClassify.Length, MAX_TEXT_LENGTH_FOR_LLM))}
---

Categorie:";

            string chosenCategory = await CallGeminiApiAsync(prompt, modelName, 50, 0.0f, apiKey, cancellationToken);

            if (string.IsNullOrWhiteSpace(chosenCategory))
            {
                return FALLBACK_CATEGORY_NAME;
            }

            var validCategories = categories.ToList();
            validCategories.Add(FALLBACK_CATEGORY_NAME);

            if (validCategories.Contains(chosenCategory.Trim()))
            {
                return chosenCategory.Trim();
            }
            else
            {
                LogMessage($"WAARSCHUWING: Gemini retourneerde een onbekende categorie: '{chosenCategory}'. Poging tot fuzzy matching.");
                foreach (var validCat in validCategories)
                {
                    if (validCat.ToLower().Contains(chosenCategory.ToLower().Trim()) || chosenCategory.ToLower().Trim().Contains(validCat.ToLower()))
                    {
                        LogMessage($"INFO: Mogelijk bedoelde Gemini: '{validCat}'. Gebruikt deze.");
                        return validCat;
                    }
                }
                return FALLBACK_CATEGORY_NAME;
            }
        }

        private async Task<string> GenerateSubfolderNameWithGeminiAsync(string textToAnalyze, string originalFilename, string apiKey, string modelName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(textToAnalyze))
            {
                return null;
            }

            var prompt = $@"
                    Je bent een AI-assistent die helpt bij het organiseren van bestanden.
                    Analyseer de volgende tekst van een document (oorspronkelijke bestandsnaam: ""{originalFilename}"") en stel een KORTE, BESCHRIJVENDE submapnaam voor (maximaal 5 woorden).
                    Deze submapnaam moet het hoofdonderwerp of de essentie van het document samenvatten.
                    Voorbeelden van goede submapnamen: ""Belastingaangifte 2023"", ""Hypotheekofferte Rabobank"", ""Notulen vergadering Project X"", ""CV Jan Jansen"".
                    Vermijd generieke namen zoals ""Document"", ""Bestand"", ""Info"" of simpelweg een datum zonder context.
                    Geef ALLEEN de voorgestelde submapnaam terug, zonder extra uitleg of opmaak.

                    Tekstfragment:
                    ---
                    {textToAnalyze.Substring(0, Math.Min(textToAnalyze.Length, MAX_TEXT_LENGTH_FOR_SUBFOLDER_NAME))}
                    ---

                    Voorgestelde submapnaam:";

            string suggestedName = await CallGeminiApiAsync(prompt, modelName, 20, 0.2f, apiKey, cancellationToken);

            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                string sanitizedName = SanitizeFolderName(suggestedName);
                if (string.IsNullOrWhiteSpace(sanitizedName) || sanitizedName.Length < MIN_SUBFOLDER_NAME_LENGTH ||
                    new[] { "document", "bestand", "info", "overig", "algemeen" }.Contains(sanitizedName.ToLower()))
                {
                    return null;
                }
                return sanitizedName;
            }
            return null;
        }

        // Nieuwe methode om bestandsnaam te genereren
        private async Task<string> GenerateFileNameWithGeminiAsync(string textToAnalyze, string originalFilename, string apiKey, string modelName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(textToAnalyze))
            {
                return Path.GetFileNameWithoutExtension(originalFilename); // Geef originele naam terug als er geen tekst is
            }

            var prompt = $@"
            Je bent een AI-assistent die helpt bij het organiseren van bestanden.
            Analyseer de volgende tekst van een document (oorspronkelijke bestandsnaam: ""{originalFilename}"") en stel een KORTE, BESCHRIJVENDE bestandsnaam voor (maximaal 10 woorden).
            Deze bestandsnaam moet het hoofdonderwerp of de essentie van het document samenvatten, zonder de bestandsextensie.
            Gebruik geen ongeldige karakters voor bestandsnamen.
            Voorbeelden van goede bestandsnamen: ""Jaarverslag 2023 Hypotheekofferte Rabobank"", ""Notulen Project X"", ""CV Jan Jansen"".
            Vermijd generieke namen zoals ""Document"", ""Bestand"", ""Info"", ""Factuur"" of simpelweg een datum zonder context.
            Geef ALLEEN de voorgestelde bestandsnaam terug, zonder extra uitleg of opmaak, en ZONDER extensie.";

            // Truncate the text to the maximum allowed length
            string truncatedText = textToAnalyze.Substring(0, Math.Min(textToAnalyze.Length, MAX_TEXT_LENGTH_FOR_SUBFOLDER_NAME));



            string suggestedName = await CallGeminiApiAsync(prompt, modelName, 30, 0.3f, apiKey, cancellationToken); // Iets hogere maxTokens en temperature voor bestandsnaam

            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                // Verwijder extra spaties en punten aan het einde van de naam
                string cleanedName = suggestedName.Trim('.', ' ');
                // Verwijder eventuele extensies die de AI er toch aan heeft geplakt
                cleanedName = Path.GetFileNameWithoutExtension(cleanedName);
                // Kort in als het te lang is, anders conflicteert het met OS limits
                cleanedName = cleanedName.Substring(0, Math.Min(cleanedName.Length, MAX_FILENAME_LENGTH));

                string sanitizedName = SanitizeFileName(cleanedName);

                if (string.IsNullOrWhiteSpace(sanitizedName) || sanitizedName.Length < MIN_SUBFOLDER_NAME_LENGTH ||
                    new[] { "document", "bestand", "info", "overig", "algemeen", "factuur" }.Contains(sanitizedName.ToLower())) // Voeg "factuur" toe aan generieke namen
                {
                    return Path.GetFileNameWithoutExtension(originalFilename); // Fallback naar originele naam
                }
                return sanitizedName;
            }
            return Path.GetFileNameWithoutExtension(originalFilename); // Fallback naar originele naam
        }

        private async Task<string> CallAzureOpenAIAsync(
                     string prompt,
                     string azureEndpoint,
                     string deploymentOrModelName,
                     string apiKey,
                     CancellationToken cancellationToken)
                        {
                            // Step 1: Create the client using the API key
                            var endpoint = new Uri(azureEndpoint);

                            // Note: For v2+ use AzureOpenAIClient instead of OpenAIClient
                            var azureClient = new AzureOpenAIClient(endpoint, new ApiKeyCredential(apiKey));

                            // Step 2: Get the chat client for your deployment (deploymentOrModelName)
                            var chatClient = azureClient.GetChatClient(deploymentOrModelName);

                            // Step 3: Prepare the messages (single-turn or multi-turn)
                            var messages = new List<ChatMessage>
                    {
                        new UserChatMessage(prompt)
                    };

            // Step 4: Get the chat completion (async)
            var completionResult = await chatClient.CompleteChatAsync(messages, options: null, cancellationToken);


            // Step 5: Extract the content from the result
            // The .Content is a list of one or more (for function/tool calls), usually just one
            var firstContent = completionResult.Value.Content;

            if (!string.IsNullOrWhiteSpace(firstContent?.ToString()))
            {
                return firstContent.ToString().Trim();
            }

            // Fallback: No answer
            return null;
        }


        private string GetRelativePath(string basePath, string fullPath)
        {
            string baseWithSeparator = AppendDirectorySeparatorChar(basePath);
            Uri baseUri = new Uri(baseWithSeparator);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

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