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
using System.Diagnostics; // Voor Process.Start

namespace AI_FileOrganizer2
{
    public partial class Form1 : Form
    {
        private const string GEMINI_BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const int MAX_TEXT_LENGTH_FOR_LLM = 8000;
        private const int MAX_TEXT_LENGTH_FOR_SUBFOLDER_NAME = 2000;
        private const int MIN_SUBFOLDER_NAME_LENGTH = 3;
        private const int MAX_SUBFOLDER_NAME_LENGTH = 50;

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

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtApiKey.Text = "YOUR_GOOGLE_API_KEY_HERE";
            txtSourceFolder.Text = @"C:\Users\Remse\Desktop\Demo\Source";
            txtDestinationFolder.Text = @"C:\Users\Remse\Desktop\Demo\AI-mappen";

            if (string.IsNullOrWhiteSpace(txtApiKey.Text) || txtApiKey.Text == "YOUR_GOOGLE_API_KEY_HERE")
            {
                txtApiKey.Text = "YOUR_GOOGLE_API_KEY_HERE";
                txtApiKey.ForeColor = System.Drawing.Color.Gray;
                txtApiKey.GotFocus += RemoveApiKeyPlaceholder;
                txtApiKey.LostFocus += AddApiKeyPlaceholder;
            }
            txtApiKey.UseSystemPasswordChar = true;

            cmbModelSelection.Items.AddRange(new object[] {
                "gemini-1.5-pro-latest",
                "gemini-1.0-pro-latest",
                "gemini-pro"
            });
            cmbModelSelection.SelectedIndex = 0;

            lblTokensUsed.Text = "Tokens gebruikt: 0";
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Visible = false;
            btnStopOrganization.Enabled = false;
        }

        private void RemoveApiKeyPlaceholder(object sender, EventArgs e)
        {
            if (txtApiKey.Text == "YOUR_GOOGLE_API_KEY_HERE")
            {
                txtApiKey.Text = "";
                txtApiKey.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void AddApiKeyPlaceholder(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtApiKey.Text))
            {
                txtApiKey.Text = "YOUR_GOOGLE_API_KEY_HERE";
                txtApiKey.ForeColor = System.Drawing.Color.Gray;
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

            if (txtApiKey.Text == "YOUR_GOOGLE_API_KEY_HERE" || string.IsNullOrWhiteSpace(txtApiKey.Text))
            {
                LogMessage("FOUT: Gelieve een geldige Google API Key in te vullen.");
                SetUiEnabled(true);
                btnStopOrganization.Enabled = false;
                return;
            }

            if (!Directory.Exists(txtSourceFolder.Text))
            {
                LogMessage($"FOUT: Bronmap '{txtSourceFolder.Text}' niet gevonden.");
                SetUiEnabled(true);
                btnStopOrganization.Enabled = false;
                return;
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
                    SetUiEnabled(true);
                    btnStopOrganization.Enabled = false;
                    return;
                }
            }

            LogMessage($"Starten met organiseren van bestanden uit: {txtSourceFolder.Text} (inclusief submappen)");
            LogMessage($"Gebruikt Gemini model: {cmbModelSelection.SelectedItem}");

            _totalTokensUsed = 0;
            UpdateTokensUsedLabel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await OrganizeFilesAsync(txtSourceFolder.Text, txtDestinationFolder.Text, txtApiKey.Text, _cancellationTokenSource.Token);
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

        private void SetUiEnabled(bool enabled)
        {
            txtApiKey.Enabled = enabled;
            txtSourceFolder.Enabled = enabled;
            btnSelectSourceFolder.Enabled = enabled;
            txtDestinationFolder.Enabled = enabled;
            btnSelectDestinationFolder.Enabled = enabled;
            cmbModelSelection.Enabled = enabled;
            btnStartOrganization.Enabled = enabled;
            linkLabelAuthor.Enabled = enabled; // Zorg dat de link ook enabled/disabled wordt
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

            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "gemini-1.5-pro-latest";

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

                    LogMessage($"INFO: Poging tot genereren submapnaam voor '{fileInfo.Name}'...");
                    string subfolderNameSuggestion = await GenerateSubfolderNameWithGeminiAsync(extractedText, fileInfo.Name, apiKey, selectedModel, cancellationToken);

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

                        string destinationFilePath = Path.Combine(finalTargetDirectory, fileInfo.Name);

                        if (File.Exists(destinationFilePath))
                        {
                            string baseName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                            string extension = fileInfo.Extension;
                            int counter = 1;
                            while (File.Exists(destinationFilePath))
                            {
                                destinationFilePath = Path.Combine(finalTargetDirectory, $"{baseName}_{counter}{extension}");
                                counter++;
                            }
                            LogMessage($"INFO: Bestand {fileInfo.Name} bestaat al op doel. Hernoemd naar {Path.GetFileName(destinationFilePath)}");
                        }

                        File.Move(filePath, destinationFilePath);

                        LogMessage($"OK: '{fileInfo.Name}' verplaatst naar '{GetRelativePath(destinationBasePath, destinationFilePath)}'");
                        movedFiles++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"FOUT: Fout bij verplaatsen/aanmaken map voor {fileInfo.Name}: {ex.Message}");
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
            name = Regex.Replace(name, @"[<>:""/\\|?*\x00-\x1F]", "_");
            name = name.Trim('.', ' ');
            name = name.Substring(0, Math.Min(name.Length, MAX_SUBFOLDER_NAME_LENGTH));
            name = Regex.Replace(name, @"\s+", " ").Trim();
            name = Regex.Replace(name, @"_+", "_").Trim('_');
            return name;
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
                    LogMessage($"FOUT: Gemini API: {response.StatusCode} - {errorMsg}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(jsonResponse);

                long inputTokens = result?.usageMetadata?.promptTokenCount ?? 0;
                long outputTokens = result?.usageMetadata?.candidatesTokenCount ?? 0;
                _totalTokensUsed += (inputTokens + outputTokens);
                UpdateTokensUsedLabel();

                string resultText = result?.candidates?[0]?.content?.parts?[0]?.text;
                if (string.IsNullOrWhiteSpace(resultText))
                {
                    LogMessage("WAARSCHUWING: Lege response van Gemini API.");
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

            if (validCategories.Contains(chosenCategory))
            {
                return chosenCategory;
            }
            else
            {
                LogMessage($"WAARSCHUWING: Gemini retourneerde een onbekende categorie: '{chosenCategory}'. Poging tot fuzzy matching.");
                foreach (var validCat in validCategories)
                {
                    if (validCat.ToLower().Contains(chosenCategory.ToLower()) || chosenCategory.ToLower().Contains(validCat.ToLower()))
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
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kan link niet openen: {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}