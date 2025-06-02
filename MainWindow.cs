using AI_FileOrganizer.Models;
using AI_FileOrganizer.Services;
using AI_FileOrganizer.Utils;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq; // Toegevoegd voor .Contains etc.
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ILogger = AI_FileOrganizer.Utils.ILogger; // Alias om conflicten te voorkomen

namespace AI_FileOrganizer
{
    public partial class MainWindow : Form
    {
        private readonly ILogger _logger;
        private static readonly HttpClient _httpClient = new HttpClient(); // Static HttpClient wordt aanbevolen
        private long _totalTokensUsed = 0;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly FileOrganizerService _fileOrganizerService;
        private readonly AiClassificationService _aiService;
        private readonly TextExtractionService _textExtractionService;
        private readonly CredentialStorageService _credentialStorageService;
        private readonly ImageAnalysisService _imageAnalysisService; // <<< TOEGEVOEGD FIELD

        public string SelectedOnnxModelPath { get; private set; }
        public string SelectedOnnxVocabPath { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            // Voor ExcelDataReader (indien gebruikt in XlsTextExtractor)
            // System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // Eenmalig bij app start

            _logger = new UiLogger(rtbLog);
            _aiService = new AiClassificationService(_logger);
            _credentialStorageService = new CredentialStorageService(_logger);
            _imageAnalysisService = new ImageAnalysisService(_logger, _httpClient); // <<< INITIALISEER HIER

            _textExtractionService = new TextExtractionService(
                _logger,
                new List<ITextExtractor>
                {
                    new PdfTextExtractor(_logger),    // Veronderstelt dat deze bestaat
                    new DocxTextExtractor(_logger),
                    new PptxTextExtractor(_logger),   // <<< TOEGEVOEGD
                    new XlsxTextExtractor(_logger),   // <<< TOEGEVOEGD

                    new PlainTextExtractor(_logger)
                    // Voeg hier andere text extractors toe indien nodig
                }
            );

            _fileOrganizerService = new FileOrganizerService(
                _logger,
                _aiService,
                _textExtractionService,
                _credentialStorageService,
                _httpClient,
                _imageAnalysisService // <<< GEEF MEE AAN CONSTRUCTOR
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
            // In MainWindow.cs, constructor

            _fileOrganizerService.RequestRenameFile += async (originalName, suggestedName) =>
            {
                // Deze event handler wordt mogelijk aangeroepen vanaf een achtergrondthread 
                // door de FileOrganizerService. We moeten de UI interactie (het tonen van de dialoog)
                // op de UI-thread uitvoeren.

                var tcs = new TaskCompletionSource<(DialogResult result, string newFileName, bool skipFile)>();

                // Gebruik BeginInvoke om de actie op de UI-thread te plaatsen zonder de huidige thread te blokkeren.
                // Control.InvokeRequired is hier niet strikt nodig omdat BeginInvoke al thread-safe is
                // en de actie in de wachtrij van de UI-thread plaatst.
                if (this.IsDisposed || !this.IsHandleCreated) // Voorkom aanroepen als form al weg is
                {
                    _logger.Log("WAARSCHUWING: RequestRenameFile aangeroepen terwijl MainWindow niet beschikbaar is. Annuleer hernoemactie.");
                    tcs.SetResult((DialogResult.Cancel, originalName, true)); // Annuleer en skip
                }
                else
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (this.IsDisposed) // Dubbelcheck
                            {
                                tcs.TrySetResult((DialogResult.Cancel, originalName, true));
                                return;
                            }

                            using (var renameForm = new FormRenameFile(originalName, suggestedName))
                            {
                                // Zorg ervoor dat ShowDialog de owner meekrijgt voor correct modaal gedrag
                                // en om te voorkomen dat de dialoog achter het hoofdvenster verdwijnt.
                                DialogResult result = renameForm.ShowDialog(this);
                                tcs.TrySetResult((result, renameForm.NewFileName, renameForm.SkipFile));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log($"FOUT in RequestRenameFile UI thread: {ex.Message}");
                            tcs.TrySetException(ex); // Geef de exceptie door aan de wachtende Task
                        }
                    }));
                }

                return await tcs.Task; // Wacht asynchroon op het resultaat van de UI interactie
            };

            // _cancellationTokenSource wordt nu per actie aangemaakt.
            // TestOnnxRobBERTProvider();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (cmbProviderSelection.Items.Count == 0)
            {
                cmbProviderSelection.Items.AddRange(new object[] {
                    "Gemini (Google)",        // Tekst LLM
                    "OpenAI (openai.com)",    // Tekst LLM (kan ook GPT-4o voor vision zijn)
                    "Azure OpenAI",           // Tekst LLM (kan ook Vision model deployment zijn)
                    "OpenAI GPT-4 Vision",    // Specifiek voor afbeeldingen
                    "Azure AI Vision",        // Specifiek voor afbeeldingen
                    "Lokaal ONNX-model"       // Tekst (RobBERT)
                });
            }
            cmbProviderSelection.SelectedIndexChanged -= cmbProviderSelection_SelectedIndexChanged;
            cmbProviderSelection.SelectedIndexChanged += cmbProviderSelection_SelectedIndexChanged;

            cmbModelSelection.SelectedIndexChanged -= cmbModelSelection_SelectedIndexChanged;
            cmbModelSelection.SelectedIndexChanged += cmbModelSelection_SelectedIndexChanged;

            // Selecteer een standaard provider
            cmbProviderSelection.SelectedItem = "Gemini (Google)"; // Of een andere default
            if (cmbProviderSelection.SelectedIndex == -1 && cmbProviderSelection.Items.Count > 0)
            {
                cmbProviderSelection.SelectedIndex = 0;
            }


            // txtApiKey.Text = "YOUR_GOOGLE_API_KEY_HERE"; // Dit wordt nu door LoadApiKeyForSelectedProvider gedaan
            SetupApiKeyPlaceholder(txtApiKey, GetDefaultApiKeyPlaceholder(cmbProviderSelection.SelectedItem?.ToString() ?? ""));
            txtApiKey.UseSystemPasswordChar = true;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            txtSourceFolder.Text = Path.Combine(desktopPath, "AI Organizer Bronmap"); // Voorbeeld paden
            txtDestinationFolder.Text = Path.Combine(documentsPath, "AI Organizer Resultaat");

            lblTokensUsed.Text = "Tokens/Transacties: 0"; // Algemener label
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Visible = false;
            btnStopOrganization.Enabled = false;
            btnSaveLog.Enabled = false;
            chkRenameFiles.Checked = true; // Standaard aan voor hernoemen
            lblAzureEndpoint.Visible = false;
            txtAzureEndpoint.Visible = false;

            SelectedOnnxModelPath = null;
            SelectedOnnxVocabPath = null;

            LoadApiKeyForSelectedProvider(); // Laad API key na instellen default provider
        }

        private void LoadApiKeyForSelectedProvider()
        {
            string selectedProviderName = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            (string loadedApiKey, string loadedAzureEndpoint) = _credentialStorageService.GetApiKey(selectedProviderName);

            // Algemene logica voor alle providers behalve ONNX
            if (selectedProviderName == "Lokaal ONNX-model")
            {
                txtApiKey.Text = "";
                txtApiKey.Enabled = false;
                txtApiKey.ForeColor = System.Drawing.Color.Gray;
                txtApiKey.Tag = null; // Placeholder is niet relevant
                txtApiKey.UseSystemPasswordChar = false;
            }
            else
            {
                txtApiKey.Enabled = true;
                txtApiKey.UseSystemPasswordChar = true;
                string placeholder = GetDefaultApiKeyPlaceholder(selectedProviderName);
                SetupApiKeyPlaceholder(txtApiKey, placeholder); // Zorgt voor correcte kleur en placeholder

                if (!string.IsNullOrWhiteSpace(loadedApiKey) && loadedApiKey != placeholder)
                {
                    txtApiKey.Text = loadedApiKey;
                    txtApiKey.ForeColor = System.Drawing.Color.Black;
                }
            }

            // Specifieke logica voor Azure providers (tekst of vision)
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
                txtAzureEndpoint.Text = ""; // Leegmaken en verbergen als niet Azure
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
            if (string.IsNullOrWhiteSpace(providerName) || providerName == "Lokaal ONNX-model") return ""; // Geen placeholder voor ONNX
            return "YOUR_API_KEY_HERE";
        }


        private void cmbProviderSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbModelSelection.Items.Clear();
            string provider = cmbProviderSelection.SelectedItem?.ToString() ?? "";

            SelectedOnnxModelPath = null;
            SelectedOnnxVocabPath = null;
            cmbModelSelection.Enabled = true; // Standaard aan, tenzij ONNX en geen model gekozen
            lblApiKey.Text = "API Key:"; // Algemeen label
            lblModel.Text = "Model / Deployment:"; // Algemeen label

            if (provider == "Gemini (Google)")
            {
                cmbModelSelection.Items.AddRange(new object[] {
                    "gemini-1.5-pro-latest", "gemini-1.5-flash-latest", "gemini-pro"
                    // Voeg meer toe indien relevant, "gemini-2.0-flash-lite" lijkt verouderd
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
                cmbModelSelection.Items.AddRange(new object[] { "YOUR-AZURE-DEPLOYMENT-NAME" }); // Gebruiker moet dit invullen
                lblApiKey.Text = "Azure OpenAI API Key:";
                lblModel.Text = "Deployment Name:";
            }
            else if (provider == "OpenAI GPT-4 Vision")
            {
                cmbModelSelection.Items.AddRange(new object[] { "gpt-4o", "gpt-4-vision-preview" }); // gpt-4o is nieuwer en beter
                lblApiKey.Text = "OpenAI API Key (Vision):";
            }
            else if (provider == "Azure AI Vision")
            {
                // Azure AI Vision (service) heeft geen "modellen" op dezelfde manier als LLMs.
                // De API versie en features bepalen wat je gebruikt. Je kunt dit veld leeg laten of "Standaard" tonen.
                cmbModelSelection.Items.Add("Standaard (via API versie)");
                cmbModelSelection.Enabled = false; // Geen modelkeuze nodig voor standaard Vision API
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
                // Als er geen modellen zijn voor een cloud provider (behalve Azure AI Vision waar het niet strict nodig is)
                cmbModelSelection.Items.Add("Geen modellen beschikbaar voor deze provider");
                cmbModelSelection.SelectedIndex = 0;
                cmbModelSelection.Enabled = false;
            }

            LoadApiKeyForSelectedProvider(); // Laadt keys en stelt endpoint zichtbaarheid in
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
                        cmbModelSelection.Items.Clear(); // Verwijder "Kies..."
                        cmbModelSelection.Items.Add(System.IO.Path.GetFileName(SelectedOnnxModelPath));
                        cmbModelSelection.SelectedIndex = 0;

                        if (MessageBox.Show("Heeft dit ONNX-model een bijbehorend vocab.txt bestand nodig?",
                                            "Vocabulaire Selecteren", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            using (OpenFileDialog vocabDialog = new OpenFileDialog())
                            {
                                vocabDialog.Filter = "Tekstbestanden (*.txt)|*.txt|Alle bestanden (*.*)|*.*";
                                vocabDialog.Title = "Selecteer het vocab.txt bestand";
                                vocabDialog.InitialDirectory = Path.GetDirectoryName(SelectedOnnxModelPath); // Start in dezelfde map
                                if (vocabDialog.ShowDialog() == DialogResult.OK)
                                {
                                    SelectedOnnxVocabPath = vocabDialog.FileName;
                                }
                                else { SelectedOnnxVocabPath = null; }
                            }
                        }
                        else { SelectedOnnxVocabPath = null; }
                    }
                    else // Gebruiker annuleerde ONNX model selectie
                    {
                        SelectedOnnxModelPath = null;
                        SelectedOnnxVocabPath = null;
                        // Reset naar "Kies..." als er geen model is of selectie mislukt
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
            // Verwijder eerst oude handlers om duplicatie te voorkomen
            textBox.GotFocus -= RemoveApiKeyPlaceholderInternal;
            textBox.LostFocus -= AddApiKeyPlaceholderInternal;

            textBox.Tag = placeholderText; // Sla placeholder op in Tag

            if (string.IsNullOrWhiteSpace(textBox.Text) || textBox.Text == placeholderText)
            {
                textBox.Text = placeholderText;
                textBox.ForeColor = System.Drawing.Color.Gray;
            }
            else
            {
                textBox.ForeColor = System.Drawing.Color.Black; // Zwarte tekst als er al iets is ingevuld
            }

            // Voeg handlers opnieuw toe
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
                dialog.EnsurePathExists = true; // Zorgt ervoor dat de map gemaakt kan worden als hij niet bestaat, of checkt of hij bestaat
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
            progressBar1.Value = 0; // Reset progress bar
            progressBar1.Visible = true;

            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "";
            string apiKey = (txtApiKey.Tag?.ToString() == txtApiKey.Text) ? "" : txtApiKey.Text; // Alleen API key als niet placeholder
            string azureEndpoint = (txtAzureEndpoint.Tag?.ToString() == txtAzureEndpoint.Text) ? "" : txtAzureEndpoint.Text;

            _fileOrganizerService.SelectedOnnxModelPath = SelectedOnnxModelPath;
            _fileOrganizerService.SelectedOnnxVocabPath = SelectedOnnxVocabPath;

            // Validatie voor API keys / Endpoints gebaseerd op provider
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
                _cancellationTokenSource?.Dispose(); // Zorg dat het gedisposed wordt
                _cancellationTokenSource = null;
            }
        }

        private void btnStopOrganization_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _logger.Log("Annulering aangevraagd...");
            btnStopOrganization.Enabled = false; // Direct uitschakelen
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
                        System.IO.File.WriteAllText(dialog.FileName, rtbLog.Text); // Gebruik System.IO.File
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
            // Vereenvoudigde logica voor het enablen/disablen van UI elementen
            txtSourceFolder.Enabled = enabled;
            btnSelectSourceFolder.Enabled = enabled;
            txtDestinationFolder.Enabled = enabled;
            btnSelectDestinationFolder.Enabled = enabled;
            cmbProviderSelection.Enabled = enabled;
            cmbModelSelection.Enabled = enabled; // Logica voor enablen/disablen zit nu meer in cmbProviderSelection_SelectedIndexChanged
            btnStartOrganization.Enabled = enabled;
            btnRenameSingleFile.Enabled = enabled;
            chkRenameFiles.Enabled = enabled;
            btnGenerateStandardFolders.Enabled = enabled; // Voeg deze toe als je die hebt

            string selectedProvider = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            txtApiKey.Enabled = enabled && selectedProvider != "Lokaal ONNX-model";
            txtAzureEndpoint.Enabled = enabled && selectedProvider.Contains("Azure");

            // Stop knop is apart beheerd
            // btnStopOrganization.Enabled = !enabled; // Alleen aan als proces loopt
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

        // btnRenameSingleFile_Click is al geüpdatet in het vorige antwoord om onderscheid te maken
        // tussen documenten en afbeeldingen en de juiste service aan te roepen.
        // De hierboven staande versie is degene die je providede, zorg dat die de logica bevat
        // die we eerder bespraken. Ik zal hem hier nogmaals opnemen met de laatste correcties.

        // In MainWindow.cs

        private async void btnRenameSingleFile_Click(object sender, EventArgs e) // 'e' is hier de EventArgs
        {
            rtbLog.Clear();
            SetUiEnabled(false);
            btnStopOrganization.Enabled = false; // Stop knop is niet relevant voor een enkele, snelle actie
            btnSaveLog.Enabled = false;
            progressBar1.Visible = false;

            // Haal API key op, maar alleen als het niet de placeholder tekst is
            string apiKey = (txtApiKey.Tag is string placeholderApi && txtApiKey.Text == placeholderApi) ? "" : txtApiKey.Text;
            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? ""; // Kan leeg zijn als niet van toepassing
                                                                                     // Haal Azure endpoint op, maar alleen als het niet de placeholder tekst is
            string azureEndpoint = (txtAzureEndpoint.Tag is string placeholderAzure && txtAzureEndpoint.Text == placeholderAzure) ? "" : txtAzureEndpoint.Text;

            // Zet ONNX-paden, relevant als _fileOrganizerService een ONNX-operatie zou doen
            _fileOrganizerService.SelectedOnnxModelPath = SelectedOnnxModelPath;
            _fileOrganizerService.SelectedOnnxVocabPath = SelectedOnnxVocabPath;

            // --- Provider/API Key validatie ---
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
            // Geldt voor zowel tekst als vision OpenAI/Gemini providers die een API key nodig hebben
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
                SetUiEnabled(true); // Schakel UI weer in
                return;
            }

            _cancellationTokenSource?.Dispose(); // Gooi oude weg als die bestond
            _cancellationTokenSource = new CancellationTokenSource();

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = false;
                dialog.EnsureFileExists = true;
                dialog.Multiselect = false;
                dialog.Title = "Selecteer een bestand om te hernoemen";

                // Gebruik de correcte ApplicationSettings properties
                var docExtStr = string.Join(";", ApplicationSettings.DocumentExtensions.Select(ext => $"*{ext}"));
                var imgExtStr = string.Join(";", ApplicationSettings.ImageExtensions.Select(ext => $"*{ext}"));

                dialog.Filters.Add(new CommonFileDialogFilter("Documenten & Afbeeldingen", $"{docExtStr};{imgExtStr}"));
                dialog.Filters.Add(new CommonFileDialogFilter("Documenten", docExtStr));
                dialog.Filters.Add(new CommonFileDialogFilter("Afbeeldingen", imgExtStr));
                // Je kunt meer specifieke filters toevoegen als je wilt:
                // dialog.Filters.Add(new CommonFileDialogFilter("PDF Bestanden", "*.pdf"));
                // dialog.Filters.Add(new CommonFileDialogFilter("Word Documenten", "*.docx"));
                // dialog.Filters.Add(new CommonFileDialogFilter("PNG Afbeeldingen", "*.png"));
                // dialog.Filters.Add(new CommonFileDialogFilter("JPEG Afbeeldingen", "*.jpg;*.jpeg"));
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
                    catch (Exception ex) // <<< CORRECTIE: 'e' hernoemd naar 'ex'
                    {
                        _logger.Log($"KRITIEKE FOUT tijdens enkel bestand hernoemen: {ex.Message}\n{ex.StackTrace}");
                    }
                }
                else
                {
                    _logger.Log("Bestandselectie geannuleerd. Geen bestand hernoemd.");
                }
            } // Einde using (var dialog...)

            SetUiEnabled(true);
            btnSaveLog.Enabled = true; // Log kan nu opgeslagen worden

            // Dispose CancellationTokenSource als het nog bestaat en niet null is
            // Dit gebeurt nu per actie, dus het zou altijd moeten bestaan tenzij er een eerdere exception was.
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null; // Zet naar null om aan te geven dat er geen actieve token meer is
        }
        private void btnGenerateStandardFolders_Click(object sender, EventArgs e)
        {
            try
            {
                var generator = new PersoonlijkeMappenStructuurGenerator(); // Zorg dat namespace correct is
                generator.Start();
                _logger.Log("INFO: Standaard persoonlijke mappenstructuur generatie gestart/voltooid (zie console/output voor details).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Er is een fout opgetreden bij het aanmaken van de mappenstructuur:\n\n" + ex.Message, "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.Log($"FOUT bij genereren mappenstructuur: {ex.Message}");
            }
        }

        private void lblSourceFolder_Click(object sender, EventArgs e)
        {
            // Placeholder - kan verwijderd worden als niet gebruikt
        }
    }
}