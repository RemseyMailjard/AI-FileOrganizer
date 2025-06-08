using AI_FileOrganizer.Models;
using AI_FileOrganizer.Services;
using AI_FileOrganizer.Utils;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ILogger = AI_FileOrganizer.Utils.ILogger;

namespace AI_FileOrganizer
{
    public partial class MainWindow : Form
    {
        private readonly ILogger _logger;
        private static readonly HttpClient _httpClient = new HttpClient();
        private long _totalTokensUsed = 0;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly FileOrganizerService _fileOrganizerService;
        private readonly AiClassificationService _aiService;
        private readonly TextExtractionService _textExtractionService;
        private readonly CredentialStorageService _credentialStorageService;
        private readonly ImageAnalysisService _imageAnalysisService;
        private LogWindow _logWindow;


        public string SelectedOnnxModelPath { get; private set; }
        public string SelectedOnnxVocabPath { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            _logger = new UiLogger(rtbLog);
            _aiService = new AiClassificationService(_logger);
            _credentialStorageService = new CredentialStorageService(_logger);
            _imageAnalysisService = new ImageAnalysisService(_logger, _httpClient);

            _textExtractionService = new TextExtractionService(
                _logger,
                new List<ITextExtractor>
                {
                    new PdfTextExtractor(_logger),
                    new DocxTextExtractor(_logger),
                    new PptxTextExtractor(_logger),
                    new XlsxTextExtractor(_logger),
                    new PlainTextExtractor(_logger)
                }
            );

            _fileOrganizerService = new FileOrganizerService(
                _logger,
                _aiService,
                _textExtractionService,
                _credentialStorageService,
                _httpClient,
                _imageAnalysisService
            );

            _fileOrganizerService.ProgressChanged += (current, total) =>
            {
                if (progressBar1.InvokeRequired)
                    progressBar1.BeginInvoke(new Action(() => { progressBar1.Maximum = total; progressBar1.Value = current; }));
                else
                {
                    progressBar1.Maximum = total;
                    progressBar1.Value = current;
                }
            };
            _fileOrganizerService.TokensUsedUpdated += (tokens) =>
            {
                _totalTokensUsed = tokens;
                UpdateTokensUsedLabel();
            };

            // AANPASSING VOOR C# 7.3: Gebruik System.Tuple
            _fileOrganizerService.RequestRenameFile += async (originalName, suggestedName) =>
            {
                // TaskCompletionSource met System.Tuple
                var tcs = new TaskCompletionSource<Tuple<DialogResult, string, bool>>();

                if (this.IsDisposed || !this.IsHandleCreated)
                {
                    _logger.Log("WAARSCHUWING: RequestRenameFile aangeroepen terwijl MainWindow niet beschikbaar is. Annuleer hernoemactie.");
                    // Gebruik Tuple.Create voor System.Tuple
                    tcs.SetResult(Tuple.Create(DialogResult.Cancel, originalName, true));
                }
                else
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (this.IsDisposed)
                            {
                                // Gebruik Tuple.Create voor System.Tuple
                                tcs.TrySetResult(Tuple.Create(DialogResult.Cancel, originalName, true));
                                return;
                            }

                            using (var renameForm = new FormRenameFile(originalName, suggestedName))
                            {
                                DialogResult result = renameForm.ShowDialog(this);
                                // Gebruik Tuple.Create voor System.Tuple
                                tcs.TrySetResult(Tuple.Create(result, renameForm.NewFileName, renameForm.SkipFile));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log($"FOUT in RequestRenameFile UI thread: {ex.Message}");
                            tcs.TrySetException(ex);
                        }
                    }));
                }
                // De return type van tcs.Task is nu Task<Tuple<DialogResult, string, bool>>,
                // wat overeenkomt met het delegate type.
                return await tcs.Task;
            };

            // _cancellationTokenSource wordt nu per actie aangemaakt.
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (cmbProviderSelection.Items.Count == 0)
            {
                cmbProviderSelection.Items.AddRange(new object[] {
                    "Gemini (Google)",
                    "OpenAI (openai.com)",
                    "Azure OpenAI",
                    "OpenAI GPT-4 Vision",
                    "Azure AI Vision",
                    "Lokaal ONNX-model"
                });
            }
            cmbProviderSelection.SelectedIndexChanged -= cmbProviderSelection_SelectedIndexChanged;
            cmbProviderSelection.SelectedIndexChanged += cmbProviderSelection_SelectedIndexChanged;

            cmbModelSelection.SelectedIndexChanged -= cmbModelSelection_SelectedIndexChanged;
            cmbModelSelection.SelectedIndexChanged += cmbModelSelection_SelectedIndexChanged;

            cmbProviderSelection.SelectedItem = "Gemini (Google)";
            if (cmbProviderSelection.SelectedIndex == -1 && cmbProviderSelection.Items.Count > 0)
            {
                cmbProviderSelection.SelectedIndex = 0;
            }

            SetupApiKeyPlaceholder(txtApiKey, GetDefaultApiKeyPlaceholder(cmbProviderSelection.SelectedItem?.ToString() ?? ""));
            txtApiKey.UseSystemPasswordChar = true;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            txtSourceFolder.Text = Path.Combine(desktopPath, "AI Organizer Bronmap");
            txtDestinationFolder.Text = Path.Combine(documentsPath, "AI Organizer Resultaat");

            lblTokensUsed.Text = "Tokens used: 0";
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Visible = false;
            btnStopOrganization.Enabled = false;
            btnSaveLog.Enabled = false;
            chkRenameFiles.Checked = true;
            lblAzureEndpoint.Visible = false;
            txtAzureEndpoint.Visible = false;

            SelectedOnnxModelPath = null;
            SelectedOnnxVocabPath = null;

            LoadApiKeyForSelectedProvider();
        }

        private void LoadApiKeyForSelectedProvider()
        {
            string selectedProviderName = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            Tuple<string, string> credentials = _credentialStorageService.GetApiKey(selectedProviderName); // Voor C# 7.3
            string loadedApiKey = credentials.Item1;
            string loadedAzureEndpoint = credentials.Item2;


            if (selectedProviderName == "Lokaal ONNX-model")
            {
                txtApiKey.Text = "";
                txtApiKey.Enabled = false;
                txtApiKey.ForeColor = System.Drawing.Color.Gray;
                txtApiKey.Tag = null;
                txtApiKey.UseSystemPasswordChar = false;
            }
            else
            {
                txtApiKey.Enabled = true;
                txtApiKey.UseSystemPasswordChar = true;
                string placeholder = GetDefaultApiKeyPlaceholder(selectedProviderName);
                SetupApiKeyPlaceholder(txtApiKey, placeholder);

                if (!string.IsNullOrWhiteSpace(loadedApiKey) && loadedApiKey != placeholder)
                {
                    txtApiKey.Text = loadedApiKey;
                    txtApiKey.ForeColor = System.Drawing.Color.Black;
                }
            }

            if (selectedProviderName.Contains("Azure"))
            {
                txtAzureEndpoint.Enabled = true;
                lblAzureEndpoint.Visible = true;
                txtAzureEndpoint.Visible = true;
                string azurePlaceholder = "YOUR_AZURE_ENDPOINT_HERE";
                SetupApiKeyPlaceholder(txtAzureEndpoint, azurePlaceholder);

                if (!string.IsNullOrWhiteSpace(loadedAzureEndpoint) && loadedAzureEndpoint != azurePlaceholder)
                {
                    txtAzureEndpoint.Text = loadedAzureEndpoint;
                    txtAzureEndpoint.ForeColor = System.Drawing.Color.Black;
                }
            }
            else
            {
                txtAzureEndpoint.Text = "";
                txtAzureEndpoint.Enabled = false;
                lblAzureEndpoint.Visible = false;
                txtAzureEndpoint.Visible = false;
                txtAzureEndpoint.Tag = null;
            }
        }

        private string GetDefaultApiKeyPlaceholder(string providerName)
        {
            if (providerName.Contains("Gemini")) return "YOUR_GOOGLE_API_KEY_HERE";
            if (providerName.Contains("OpenAI GPT-4 Vision")) return "YOUR_OPENAI_API_KEY_HERE (voor Vision)";
            if (providerName.Contains("OpenAI (openai.com)")) return "YOUR_OPENAI_API_KEY_HERE (voor Tekst)";
            if (providerName.Contains("Azure AI Vision")) return "YOUR_AZURE_VISION_API_KEY_HERE";
            if (providerName.Contains("Azure OpenAI")) return "YOUR_AZURE_OPENAI_API_KEY_HERE";
            if (string.IsNullOrWhiteSpace(providerName) || providerName == "Lokaal ONNX-model") return "";
            return "YOUR_API_KEY_HERE";
        }

        private void cmbProviderSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbModelSelection.Items.Clear();
            string provider = cmbProviderSelection.SelectedItem?.ToString() ?? "";

            SelectedOnnxModelPath = null;
            SelectedOnnxVocabPath = null;
            cmbModelSelection.Enabled = true;
            lblApiKey.Text = "API Key:";
            lblModel.Text = "Model / Deployment:";

            if (provider == "Gemini (Google)")
            {
                cmbModelSelection.Items.AddRange(new object[] {
                    "gemini-1.5-pro-latest", "gemini-1.5-flash-latest", "gemini-pro"
                });
                lblApiKey.Text = "Google API Key:";
            }
            else if (provider == "OpenAI (openai.com)")
            {
                cmbModelSelection.Items.AddRange(new object[] { "gpt-4o", "gpt-4-turbo", "gpt-3.5-turbo" });
                lblApiKey.Text = "OpenAI API Key:";
            }
            else if (provider == "Azure OpenAI")
            {
                cmbModelSelection.Items.AddRange(new object[] { "YOUR-AZURE-DEPLOYMENT-NAME" });
                lblApiKey.Text = "Azure OpenAI API Key:";
                lblModel.Text = "Deployment Name:";
            }
            else if (provider == "OpenAI GPT-4 Vision")
            {
                cmbModelSelection.Items.AddRange(new object[] { "gpt-4o", "gpt-4-vision-preview" });
                lblApiKey.Text = "OpenAI API Key (Vision):";
            }
            else if (provider == "Azure AI Vision")
            {
                cmbModelSelection.Items.Add("Standaard (via API versie)");
                cmbModelSelection.Enabled = false;
                lblApiKey.Text = "Azure Vision API Key:";
                lblModel.Text = "Model (N.v.t.):";
            }
            else if (provider == "Lokaal ONNX-model")
            {
                cmbModelSelection.Items.Add("Kies lokaal ONNX-model...");
                lblApiKey.Text = "Geen API Key nodig";
            }

            if (cmbModelSelection.Items.Count > 0)
            {
                cmbModelSelection.SelectedIndex = 0;
            }
            else if (provider != "Lokaal ONNX-model" && provider != "Azure AI Vision")
            {
                cmbModelSelection.Items.Add("Geen modellen beschikbaar voor deze provider");
                cmbModelSelection.SelectedIndex = 0;
                cmbModelSelection.Enabled = false;
            }

            LoadApiKeyForSelectedProvider();
        }

        private void cmbModelSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            string provider = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            string selectedModelItem = cmbModelSelection.SelectedItem?.ToString() ?? "";

            if (provider == "Lokaal ONNX-model" && selectedModelItem == "Kies lokaal ONNX-model...")
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "ONNX-modellen (*.onnx)|*.onnx";
                    ofd.Title = "Selecteer een ONNX-modelbestand";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        SelectedOnnxModelPath = ofd.FileName;
                        cmbModelSelection.Items.Clear();
                        cmbModelSelection.Items.Add(System.IO.Path.GetFileName(SelectedOnnxModelPath));
                        cmbModelSelection.SelectedIndex = 0;

                        if (MessageBox.Show("Heeft dit ONNX-model een bijbehorend vocab.txt bestand nodig?",
                                            "Vocabulaire Selecteren", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            using (OpenFileDialog vocabDialog = new OpenFileDialog())
                            {
                                vocabDialog.Filter = "Tekstbestanden (*.txt)|*.txt|Alle bestanden (*.*)|*.*";
                                vocabDialog.Title = "Selecteer het vocab.txt bestand";
                                vocabDialog.InitialDirectory = Path.GetDirectoryName(SelectedOnnxModelPath);
                                if (vocabDialog.ShowDialog() == DialogResult.OK)
                                {
                                    SelectedOnnxVocabPath = vocabDialog.FileName;
                                }
                                else { SelectedOnnxVocabPath = null; }
                            }
                        }
                        else { SelectedOnnxVocabPath = null; }
                    }
                    else
                    {
                        SelectedOnnxModelPath = null;
                        SelectedOnnxVocabPath = null;
                        if (!cmbModelSelection.Items.OfType<string>().Any(item => item.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase)))
                        {
                            cmbModelSelection.Items.Clear();
                            cmbModelSelection.Items.Add("Kies lokaal ONNX-model...");
                            cmbModelSelection.SelectedIndex = 0;
                        }
                    }
                }
            }
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
            if (textBox != null && textBox.Tag is string placeholderText)
            {
                if (textBox.Text == placeholderText)
                {
                    textBox.Text = "";
                    textBox.ForeColor = System.Drawing.Color.Black;
                }
            }
        }

        private void AddApiKeyPlaceholderInternal(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Tag is string placeholderText)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholderText;
                    textBox.ForeColor = System.Drawing.Color.Gray;
                }
            }
        }

        private void btnSelectSourceFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Selecteer de bronmap";
                try { dialog.InitialDirectory = Directory.Exists(txtSourceFolder.Text) ? txtSourceFolder.Text : Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
                catch { dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtSourceFolder.Text = dialog.FileName;
                }
            }
        }

        private void btnSelectDestinationFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Selecteer de doelmap";
                dialog.EnsurePathExists = true;
                try { dialog.InitialDirectory = Directory.Exists(txtDestinationFolder.Text) ? txtDestinationFolder.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
                catch { dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtDestinationFolder.Text = dialog.FileName;
                }
            }
        }

        private async void btnStartOrganization_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
            SetUiEnabled(false);
            btnStopOrganization.Enabled = true;
            btnSaveLog.Enabled = false;
            progressBar1.Value = 0;
            progressBar1.Visible = true;

            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "";
            string apiKey = (txtApiKey.Tag?.ToString() == txtApiKey.Text) ? "" : txtApiKey.Text;
            string azureEndpoint = (txtAzureEndpoint.Tag?.ToString() == txtAzureEndpoint.Text) ? "" : txtAzureEndpoint.Text;

            _fileOrganizerService.SelectedOnnxModelPath = SelectedOnnxModelPath;
            _fileOrganizerService.SelectedOnnxVocabPath = SelectedOnnxVocabPath;

            bool credentialsValid = true;
            if (providerName.Contains("Azure") && (string.IsNullOrWhiteSpace(azureEndpoint) || string.IsNullOrWhiteSpace(apiKey)))
            {
                _logger.Log("FOUT: Azure Endpoint en API Key zijn vereist voor de geselecteerde Azure provider.");
                credentialsValid = false;
            }
            else if ((providerName.Contains("Gemini") || providerName.Contains("OpenAI")) && string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.Log($"FOUT: API Key is vereist voor {providerName}.");
                credentialsValid = false;
            }
            else if (providerName == "Lokaal ONNX-model" && string.IsNullOrWhiteSpace(SelectedOnnxModelPath))
            {
                _logger.Log("FOUT: Selecteer een lokaal ONNX model bestand.");
                credentialsValid = false;
            }

            if (!credentialsValid)
            {
                SetUiEnabled(true); btnStopOrganization.Enabled = false; progressBar1.Visible = false; return;
            }

            if (!Directory.Exists(txtSourceFolder.Text))
            {
                _logger.Log($"FOUT: Bronmap '{txtSourceFolder.Text}' niet gevonden.");
                SetUiEnabled(true); btnStopOrganization.Enabled = false; progressBar1.Visible = false; return;
            }

            _logger.Log($"Starten met organiseren van bestanden uit: {txtSourceFolder.Text} (inclusief submappen)");
            _logger.Log($"Gebruikte provider: {providerName}, Model: {selectedModel}");
            if (chkRenameFiles.Checked) _logger.Log("Bestandsnamen worden hernoemd met AI-suggesties.");
            if (ApplicationSettings.UseDetailedSubfolders) _logger.Log("Gedetailleerde submappen worden gebruikt (indien van toepassing).");

            _totalTokensUsed = 0;
            UpdateTokensUsedLabel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await _fileOrganizerService.OrganizeFilesAsync(
                    txtSourceFolder.Text,
                    txtDestinationFolder.Text,
                    apiKey,
                    providerName,
                    selectedModel,
                    azureEndpoint,
                    chkRenameFiles.Checked,
                    _cancellationTokenSource.Token
                );
            }
            catch (OperationCanceledException) { _logger.Log("\nOrganisatie geannuleerd door gebruiker."); }
            catch (Exception ex) { _logger.Log($"KRITIEKE FOUT tijdens organisatie: {ex.Message}\n{ex.StackTrace}"); }
            finally
            {
                _logger.Log("\nOrganisatie proces beëindigd.");
                SetUiEnabled(true);
                btnStopOrganization.Enabled = false;
                btnSaveLog.Enabled = true;
                progressBar1.Visible = false;
                _cancellationTokenSource?.Dispose();
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
            using (var dialog = new CommonSaveFileDialog())
            {
                dialog.Filters.Add(new CommonFileDialogFilter("Tekstbestanden", "*.txt"));
                dialog.DefaultExtension = "txt";
                dialog.Title = "Sla logbestand op";
                dialog.DefaultFileName = $"AI_Organizer_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                try { dialog.InitialDirectory = Directory.Exists(txtDestinationFolder.Text) ? txtDestinationFolder.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
                catch { dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    try
                    {
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
            txtSourceFolder.Enabled = enabled;
            btnSelectSourceFolder.Enabled = enabled;
            txtDestinationFolder.Enabled = enabled;
            btnSelectDestinationFolder.Enabled = enabled;
            cmbProviderSelection.Enabled = enabled;
            cmbModelSelection.Enabled = enabled;
            btnStartOrganization.Enabled = enabled;
            btnRenameSingleFile.Enabled = enabled;
            chkRenameFiles.Enabled = enabled;
            btnGenerateStandardFolders.Enabled = enabled;

            string selectedProvider = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            txtApiKey.Enabled = enabled && selectedProvider != "Lokaal ONNX-model";
            txtAzureEndpoint.Enabled = enabled && selectedProvider.Contains("Azure");
        }

        private void UpdateTokensUsedLabel()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateTokensUsedLabel));
                return;
            }
            lblTokensUsed.Text = $"Tokens/Transacties: {_totalTokensUsed}";
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

        private async void btnRenameSingleFile_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
            SetUiEnabled(false);
            btnStopOrganization.Enabled = false;
            btnSaveLog.Enabled = false;
            progressBar1.Visible = false;

            string apiKey = (txtApiKey.Tag is string placeholderApi && txtApiKey.Text == placeholderApi) ? "" : txtApiKey.Text;
            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "";
            string azureEndpoint = (txtAzureEndpoint.Tag is string placeholderAzure && txtAzureEndpoint.Text == placeholderAzure) ? "" : txtAzureEndpoint.Text;

            _fileOrganizerService.SelectedOnnxModelPath = SelectedOnnxModelPath;
            _fileOrganizerService.SelectedOnnxVocabPath = SelectedOnnxVocabPath;

            bool credentialsValid = true;
            if (string.IsNullOrWhiteSpace(providerName))
            {
                _logger.Log("FOUT: Geen AI provider geselecteerd.");
                credentialsValid = false;
            }
            else if (providerName.Contains("Azure") && (string.IsNullOrWhiteSpace(azureEndpoint) || string.IsNullOrWhiteSpace(apiKey)))
            {
                _logger.Log("FOUT: Azure Endpoint en API Key zijn vereist voor de geselecteerde Azure provider.");
                credentialsValid = false;
            }
            else if ((providerName.Contains("Gemini") || providerName.Contains("OpenAI")) && string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.Log($"FOUT: API Key is vereist voor {providerName}.");
                credentialsValid = false;
            }
            else if (providerName == "Lokaal ONNX-model" && string.IsNullOrWhiteSpace(SelectedOnnxModelPath))
            {
                _logger.Log("FOUT: Selecteer een lokaal ONNX model bestand.");
                credentialsValid = false;
            }

            if (!credentialsValid)
            {
                SetUiEnabled(true);
                return;
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = false;
                dialog.EnsureFileExists = true;
                dialog.Multiselect = false;
                dialog.Title = "Selecteer een bestand om te hernoemen";

                var docExtStr = string.Join(";", ApplicationSettings.DocumentExtensions.Select(ext => $"*{ext}"));
                var imgExtStr = string.Join(";", ApplicationSettings.ImageExtensions.Select(ext => $"*{ext}"));

                dialog.Filters.Add(new CommonFileDialogFilter("Documenten & Afbeeldingen", $"{docExtStr};{imgExtStr}"));
                dialog.Filters.Add(new CommonFileDialogFilter("Documenten", docExtStr));
                dialog.Filters.Add(new CommonFileDialogFilter("Afbeeldingen", imgExtStr));
                dialog.Filters.Add(new CommonFileDialogFilter("Alle Bestanden", "*.*"));

                try
                {
                    dialog.InitialDirectory = Directory.Exists(txtSourceFolder.Text) ? txtSourceFolder.Text : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
                catch
                {
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string filePath = dialog.FileName;
                    _logger.Log($"INFO: Geselecteerd bestand voor hernoemen: {filePath}");
                    try
                    {
                        await _fileOrganizerService.RenameSingleFileInteractiveAsync(
                            filePath,
                            apiKey,
                            providerName,
                            selectedModel,
                            azureEndpoint,
                            _cancellationTokenSource.Token
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Log("Hernoem-actie geannuleerd door gebruiker.");
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"KRITIEKE FOUT tijdens enkel bestand hernoemen: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else
                {
                    _logger.Log("Bestandselectie geannuleerd. Geen bestand hernoemd.");
                }
            }

            SetUiEnabled(true);
            btnSaveLog.Enabled = true;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void btnGenerateStandardFolders_Click(object sender, EventArgs e)
        {
            try
            {
                // Veronderstel dat PersoonlijkeMappenStructuurGenerator in dezelfde namespace zit,
                // of voeg de juiste using toe.
                var generator = new PersoonlijkeMappenStructuurGenerator(); // Geef logger mee indien nodig
                generator.Start();
                _logger.Log("INFO: Standaard persoonlijke mappenstructuur generatie gestart/voltooid.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Er is een fout opgetreden bij het aanmaken van de mappenstructuur:\n\n" + ex.Message, "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.Log($"FOUT bij genereren mappenstructuur: {ex.Message}");
            }
        }

        // Zorg ervoor dat je bovenaan je .cs bestand hebt:
        // using System.IO;
        // using System.Threading;
        // using System.Threading.Tasks;
        // using System.Windows.Forms;
        // using AI_FileOrganizer.Services;    // Namespace van je services
        // using AI_FileOrganizer.Models;      // Voor IAiProvider etc.

        private async void btnSuggestSubfolders_Click(object sender, EventArgs e)
        {
            btnSuggestSubfolders.Enabled = false;
            rtbLog.AppendText("[INFO] Starten met genereren van gedetailleerde subfolder suggesties per bestand...\n");
            StringBuilder suggestionsBuilder = new StringBuilder();
            HashSet<string> uniqueSuggestedFullPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string destinationFolder = txtDestinationFolder.Text;
                if (string.IsNullOrWhiteSpace(destinationFolder))
                {
                    MessageBox.Show("Selecteer eerst een doelfolder (waar de 'classification.txt' verwacht wordt).", "Doelfolder vereist", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    rtbLog.AppendText("[FOUT] Geen doelfolder geselecteerd.\n");
                    return;
                }

                if (!Directory.Exists(destinationFolder))
                {
                    MessageBox.Show(string.Format("De doelfolder '{0}' bestaat niet.", destinationFolder), "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    rtbLog.AppendText(string.Format("[FOUT] Doelfolder '{0}' niet gevonden.\n", destinationFolder));
                    return;
                }

                string logFilePath = Path.Combine(destinationFolder, "classification.txt");

                if (!File.Exists(logFilePath))
                {
                    MessageBox.Show(string.Format("De logfile 'classification.txt' werd niet gevonden in:\n{0}", destinationFolder), "Bestand niet gevonden", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    rtbLog.AppendText(string.Format("[FOUT] 'classification.txt' niet gevonden in '{0}'.\n", destinationFolder));
                    return;
                }

                rtbLog.AppendText(string.Format("[INFO] Logfile '{0}' wordt gelezen...\n", logFilePath));
                string[] logLines = await Task.Run(() => File.ReadAllLines(logFilePath)); // Voor C# 7.3

                IAiProvider selectedProvider = GetSelectedAiProvider();
                if (selectedProvider == null) return;

                var aiService = new AiClassificationService(new UiLogger(rtbLog));
                string modelName = cmbModelSelection.Text;

                //if (string.IsNullOrWhiteSpace(modelName) && !(selectedProvider is LocalOnnxProvider)) // Pas LocalOnnxProvider aan
                //{
                //    MessageBox.Show("Selecteer een AI model.", "Configuratiefout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    rtbLog.AppendText("[FOUT] AI model niet geselecteerd.\n");
                //    return;
                //}

                rtbLog.AppendText("[INFO] Verwerken log entries voor gedetailleerde subfolder suggesties...\n");
                int processedCount = 0;

                foreach (string line in logLines)
                {
                    if (line.StartsWith("OK: Origineel '"))
                    {
                        // Parse de regel om originele bestandsnaam en nieuw pad (incl. hoofdcategorie) te krijgen
                        // Voorbeeld: OK: Origineel 'foo.txt' (van 'C:\source') verplaatst/hernoemd naar 'CategorieX\nieuw_foo.txt'
                        Match match = Regex.Match(line, @"Origineel '([^']*)'.*verplaatst/hernoemd naar '([^']*)'");
                        if (match.Success)
                        {
                            processedCount++;
                            string originalFilenameFromLog = match.Groups[1].Value;
                            string newPathFullRelative = match.Groups[2].Value; // Bijv. "10. Werk en Loopbaan/SubMap/Bestand.ext" of "10. Werk en Loopbaan/Bestand.ext"

                            string mainCategoryPath = Path.GetDirectoryName(newPathFullRelative); // Geeft "10. Werk en Loopbaan/SubMap" of "10. Werk en Loopbaan"
                            if (string.IsNullOrEmpty(mainCategoryPath)) // Kan gebeuren als bestand direct in root van destination staat (onwaarschijnlijk met jouw setup)
                            {
                                rtbLog.AppendText(string.Format("[WAARSCHUWING] Kon hoofdcategorie niet bepalen voor '{0}'. Overgeslagen.\n", originalFilenameFromLog));
                                continue;
                            }

                            // Voor `SuggestDetailedSubfolderAsync` hebben we de `determinedCategoryKey` nodig.
                            // Dit is de naam van de *hoofdcategorie map*, bijv. "10. Werk en Loopbaan".
                            // Als `mainCategoryPath` al een submap bevat (bijv. "Hoofdcategorie/AI_Submap"), willen we alleen "Hoofdcategorie".
                            // Echter, `SuggestDetailedSubfolderAsync` voegt `PluralizeDocumentType(documentType)` toe als een basis.
                            // We moeten de `determinedCategoryKey` consistent houden met wat `ApplicationSettings.DetailedSubfolderPrompts` verwacht.
                            // Laten we aannemen dat de EERSTE map in `newPathFullRelative` de `determinedCategoryKey` is.
                            string[] pathParts = newPathFullRelative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                            string determinedCategoryKeyFromLog = pathParts.Length > 0 ? pathParts[0] : null;

                            if (string.IsNullOrEmpty(determinedCategoryKeyFromLog))
                            {
                                rtbLog.AppendText(string.Format("[WAARSCHUWING] Kon hoofdcategorie niet bepalen uit pad '{0}' voor bestand '{1}'. Overgeslagen.\n", newPathFullRelative, originalFilenameFromLog));
                                continue;
                            }

                            rtbLog.AppendText(string.Format("[INFO] Vraag gedetailleerde subfolder aan voor: '{0}' (categorie: '{1}')...\n", originalFilenameFromLog, determinedCategoryKeyFromLog));

                            // Voor `textToAnalyze`: In een ideale wereld zouden we de tekst hebben.
                            // Voor nu geven we een lege string mee; `GetRelevantTextForAI` in je service zal dan fallbacken naar bestandsnaam.
                            string suggestedDetailedPathPart = await aiService.SuggestDetailedSubfolderAsync(
                                "", // textToAnalyze - leeg, service zal fallbacken op originalFilename
                                originalFilenameFromLog,
                                determinedCategoryKeyFromLog, // Dit moet de KEY zijn die ApplicationSettings verwacht, bijv. "Werk en Loopbaan"
                                selectedProvider,
                                modelName,
                                CancellationToken.None
                            );

                            if (!string.IsNullOrWhiteSpace(suggestedDetailedPathPart))
                            {
                                string fullSuggestedPath = Path.Combine(determinedCategoryKeyFromLog, suggestedDetailedPathPart);
                                if (uniqueSuggestedFullPaths.Add(fullSuggestedPath)) // Voeg toe aan HashSet om duplicaten te filteren
                                {
                                    suggestionsBuilder.AppendLine(fullSuggestedPath);
                                    rtbLog.AppendText(string.Format("  -> AI suggereerde gedetailleerd pad: '{0}'\n", fullSuggestedPath));
                                }
                            }
                            else
                            {
                                rtbLog.AppendText(string.Format("  -> AI gaf geen gedetailleerd subpad voor '{0}'.\n", originalFilenameFromLog));
                            }
                        }
                    }
                }

                if (processedCount == 0)
                {
                    rtbLog.AppendText("[INFO] Geen verwerkte bestanden gevonden in de logfile om suggesties voor te genereren.\n");
                    MessageBox.Show("Geen 'OK: Origineel...' regels gevonden in de logfile. Kan geen suggesties genereren.", "Log Leeg of Onjuist Formaat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string finalSuggestions = suggestionsBuilder.ToString();
                if (string.IsNullOrWhiteSpace(finalSuggestions))
                {
                    MessageBox.Show("De AI heeft geen gedetailleerde subfolder suggesties gegeven voor de bestanden in de log.", "Geen Suggesties", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    rtbLog.AppendText("[INFO] Geen gedetailleerde subfolder suggesties gegenereerd.\n");
                }
                else
                {
                    rtbLog.AppendText("\n--- Gecombineerde AI Subfolder Suggesties ---\n" + finalSuggestions + "\n");
                    MessageBox.Show("De AI heeft de volgende gecombineerde subfolderstructuur voorgesteld (unieke paden):\n\n" + finalSuggestions,
                                    "Suggestie voor Subfolders", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                rtbLog.AppendText(string.Format("[FOUT] Algemene fout bij suggereren van gedetailleerde subfolders: {0}\nStack Trace: {1}\n", ex.Message, ex.StackTrace));
                MessageBox.Show("Er is een fout opgetreden: " + ex.Message, "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSuggestSubfolders.Enabled = true;
                rtbLog.AppendText("[INFO] Verwerken gedetailleerde subfolder suggesties voltooid.\n");
            }
            
        }

        private IAiProvider GetSelectedAiProvider()
        {
            throw new NotImplementedException();
        }

        // De GetSelectedAiProvider methode blijft zoals eerder voorgesteld,
        // zorg ervoor dat de provider constructors en class namen correct zijn voor jouw project.
        // private IAiProvider GetSelectedAiProvider() { ... } (zie vorige antwoord)

        // Zorg ervoor dat de methode RequestSubfolderSuggestionFromLogAsync in AiClassificationService bestaat:
        // In AiClassificationService.cs
        // public async Task<string> RequestSubfolderSuggestionFromLogAsync(
        //     string prompt,
        //     IAiProvider aiProvider,
        //     string modelName,
        //     CancellationToken cancellationToken)
        // {
        //     this.LastCallSimulatedTokensUsed = 0;
        //     if (aiProvider == null) { /* ... error handling ... */ return "Fout: AI Provider niet beschikbaar."; }
        //     if (string.IsNullOrWhiteSpace(modelName)) { /* ... error handling ... */ return "Fout: Modelnaam niet opgegeven."; }
        //
        //     Tuple<string, long> aiResult = await CallAiProviderAsync(
        //         aiProvider,
        //         modelName,
        //         prompt,
        //         AiTaskSettings.SubfolderSuggestion, // Zorg dat dit bestaat!
        //         cancellationToken,
        //         "subfolder suggestie (log-based)");
        //
        //     this.LastCallSimulatedTokensUsed = aiResult.Item2;
        //     string rawCompletion = aiResult.Item1;
        //     if (string.IsNullOrWhiteSpace(rawCompletion)) { return string.Empty; }
        //     return rawCompletion.Trim();
        // }
    }
}