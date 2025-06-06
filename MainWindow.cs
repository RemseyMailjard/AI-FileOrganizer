﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

using AI_FileOrganizer.Services;
using AI_FileOrganizer.Utils;
using AI_FileOrganizer.Models;
using ILogger = AI_FileOrganizer.Utils.ILogger;
using DocumentFormat.OpenXml.Spreadsheet;
using AI_FileOrganizer.Properties;

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

        public string SelectedOnnxModelPath { get; private set; }
        public string SelectedOnnxVocabPath { get; private set; } // Optioneel: indien vocab.txt vereist

        public MainWindow()
        {
            InitializeComponent();

            _logger = new UiLogger(rtbLog);
            _aiService = new AiClassificationService(_logger);
            _credentialStorageService = new CredentialStorageService(_logger);
            _textExtractionService = new TextExtractionService(
                _logger,
                new List<ITextExtractor>
                {
                    new PdfTextExtractor(_logger),
                    new DocxTextExtractor(_logger),
                    new PlainTextExtractor(_logger)
                }
            );

            _fileOrganizerService = new FileOrganizerService(
                _logger,
                _aiService,
                _textExtractionService,
                _credentialStorageService,
                _httpClient
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

            _fileOrganizerService.RequestRenameFile += (originalName, suggestedName) =>
            {
                return Invoke((Func<Task<(DialogResult, string, bool)>>)(() =>
                {
                    using (var renameForm = new FormRenameFile(originalName, suggestedName))
                    {
                        DialogResult result = renameForm.ShowDialog();
                        return Task.FromResult((result, renameForm.NewFileName, renameForm.SkipFile));
                    }
                })) as Task<(DialogResult, string, bool)>;
            };

            _cancellationTokenSource = new CancellationTokenSource();
         //   TestOnnxRobBERTProvider();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (cmbProviderSelection.Items.Count == 0)
            {
                cmbProviderSelection.Items.AddRange(new object[] {
                    "Gemini (Google)",
                    "OpenAI (openai.com)",
                    "Azure OpenAI",
                    "Lokaal ONNX-model"
                });
            }
            cmbProviderSelection.SelectedIndexChanged -= cmbProviderSelection_SelectedIndexChanged;
            cmbProviderSelection.SelectedIndexChanged += cmbProviderSelection_SelectedIndexChanged;

            cmbModelSelection.SelectedIndexChanged -= cmbModelSelection_SelectedIndexChanged;
            cmbModelSelection.SelectedIndexChanged += cmbModelSelection_SelectedIndexChanged;

            cmbProviderSelection.SelectedIndex = 0;

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
            chkRenameFiles.Checked = false;
            lblAzureEndpoint.Visible = false;
            txtAzureEndpoint.Visible = false;

            SelectedOnnxModelPath = null;
            SelectedOnnxVocabPath = null;

            LoadApiKeyForSelectedProvider();
        }

        private void LoadApiKeyForSelectedProvider()
        {
            string selectedProviderName = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            (string loadedApiKey, string loadedAzureEndpoint) = _credentialStorageService.GetApiKey(selectedProviderName);

            if (selectedProviderName == "Lokaal ONNX-model")
            {
                txtApiKey.Text = "";
                txtApiKey.Enabled = false;
                txtApiKey.ForeColor = System.Drawing.Color.Gray;
                txtApiKey.Tag = null;

                txtAzureEndpoint.Text = "";
                txtAzureEndpoint.Enabled = false;
                txtAzureEndpoint.ForeColor = System.Drawing.Color.Gray;
                txtAzureEndpoint.Tag = null;
                return;
            }

            txtApiKey.Enabled = true;
            if (!string.IsNullOrWhiteSpace(loadedApiKey))
            {
                txtApiKey.Text = loadedApiKey;
                txtApiKey.ForeColor = System.Drawing.Color.Black;
                txtApiKey.Tag = GetDefaultApiKeyPlaceholder(selectedProviderName);
            }
            else
            {
                txtApiKey.Text = GetDefaultApiKeyPlaceholder(selectedProviderName);
                txtApiKey.ForeColor = System.Drawing.Color.Gray;
                txtApiKey.Tag = GetDefaultApiKeyPlaceholder(selectedProviderName);
            }

            if (selectedProviderName == "Azure OpenAI")
            {
                txtAzureEndpoint.Enabled = true;
                if (!string.IsNullOrWhiteSpace(loadedAzureEndpoint))
                {
                    txtAzureEndpoint.Text = loadedAzureEndpoint;
                    txtAzureEndpoint.ForeColor = System.Drawing.Color.Black;
                    txtAzureEndpoint.Tag = "YOUR_AZURE_ENDPOINT_HERE";
                }
                else
                {
                    txtAzureEndpoint.Text = "YOUR_AZURE_ENDPOINT_HERE";
                    txtAzureEndpoint.ForeColor = System.Drawing.Color.Gray;
                    txtAzureEndpoint.Tag = "YOUR_AZURE_ENDPOINT_HERE";
                }
            }
            else
            {
                txtAzureEndpoint.Text = "";
                txtAzureEndpoint.ForeColor = System.Drawing.Color.Gray;
                txtAzureEndpoint.Tag = null;
                txtAzureEndpoint.Enabled = false;
            }
        }

        private string GetDefaultApiKeyPlaceholder(string providerName)
        {
            if (providerName.Contains("Gemini")) return "YOUR_GOOGLE_API_KEY_HERE";
            if (providerName.Contains("OpenAI (openai.com)")) return "YOUR_OPENAI_API_KEY_HERE";
            if (providerName.Contains("Azure OpenAI")) return "YOUR_AZURE_ENDPOINT_HERE";
            return "YOUR_API_KEY_HERE";
        }

        private void cmbProviderSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbModelSelection.Items.Clear();
            string provider = cmbProviderSelection.SelectedItem?.ToString() ?? "";

            // Reset ONNX-vocab/model-paths bij provider-wissel!
            SelectedOnnxModelPath = null;
            SelectedOnnxVocabPath = null;

            if (provider == "Gemini (Google)")
            {
                cmbModelSelection.Items.AddRange(new object[] {
                    "gemini-2.0-flash-lite", "gemini-1.5-pro-latest", "gemini-1.5-flash-latest",
                    "gemini-1.0-pro-latest", "gemini-pro", "gemini-2.5-pro-preview-05-06",
                    "gemini-2.5-flash-preview-04-17", "gemini-2.0-flash-001", "gemini-2.0-flash-lite-001" });
                lblApiKey.Text = "Google API Key:";
                lblAzureEndpoint.Visible = false;
                txtAzureEndpoint.Visible = false;
            }
            else if (provider == "OpenAI (openai.com)")
            {
                cmbModelSelection.Items.AddRange(new object[] {
                    "gpt-4o", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo",
                    "gpt-3.5-turbo-0125", "gpt-3.5-turbo-0613" });
                lblApiKey.Text = "OpenAI API Key:";
                lblAzureEndpoint.Visible = false;
                txtAzureEndpoint.Visible = false;
            }
            else if (provider == "Azure OpenAI")
            {
                cmbModelSelection.Items.AddRange(new object[] { "YOUR-AZURE-DEPLOYMENT-NAME" });
                lblApiKey.Text = "Azure OpenAI API Key:";
                lblAzureEndpoint.Visible = true;
                txtAzureEndpoint.Visible = true;
            }
            else if (provider == "Lokaal ONNX-model")
            {
                cmbModelSelection.Items.Add("Kies lokaal ONNX-model...");
                lblApiKey.Text = "Geen API Key nodig";
                lblAzureEndpoint.Visible = false;
                txtAzureEndpoint.Visible = false;
            }

            cmbModelSelection.SelectedIndex = 0;
            LoadApiKeyForSelectedProvider();
        }

        private void cmbModelSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            string provider = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "";

            if (provider == "Lokaal ONNX-model" && selectedModel == "Kies lokaal ONNX-model...")
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
                        // Vraag gebruiker om vocab
                        if (MessageBox.Show("Heeft dit ONNX-model een vocab.txt nodig?", "Vocab selecteren", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            using (OpenFileDialog vocabDialog = new OpenFileDialog())
                            {
                                vocabDialog.Filter = "Vocab (*.txt)|*.txt|Alle bestanden (*.*)|*.*";
                                vocabDialog.Title = "Selecteer vocab.txt";
                                if (vocabDialog.ShowDialog() == DialogResult.OK)
                                {
                                    SelectedOnnxVocabPath = vocabDialog.FileName;
                                }
                                else
                                {
                                    SelectedOnnxVocabPath = null;
                                }
                            }
                        }
                        else
                        {
                            SelectedOnnxVocabPath = null;
                        }
                    }
                    else
                    {
                        SelectedOnnxModelPath = null;
                        SelectedOnnxVocabPath = null;
                        cmbModelSelection.SelectedIndex = -1;
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
                dialog.IsFolderPicker = true;
                dialog.Title = "Selecteer de bronmap met bestanden (inclusief submappen)";
                dialog.InitialDirectory = Directory.Exists(txtSourceFolder.Text) ? txtSourceFolder.Text : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
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
                dialog.Title = "Selecteer de doelmap voor geordende bestanden";
                dialog.EnsurePathExists = true;
                dialog.InitialDirectory = Directory.Exists(txtDestinationFolder.Text) ? txtDestinationFolder.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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
            progressBar1.Visible = true;

            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "";
            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "";
            string apiKey = txtApiKey.Text;
            string azureEndpoint = txtAzureEndpoint.Text;

            // Zet ONNX-paths vóór elke organisatie-actie
            _fileOrganizerService.SelectedOnnxModelPath = SelectedOnnxModelPath;
            _fileOrganizerService.SelectedOnnxVocabPath = SelectedOnnxVocabPath;

            if (providerName != "Lokaal ONNX-model")
            {
                string currentApiKeyPlaceholder = txtApiKey.Tag?.ToString();
                if (string.IsNullOrWhiteSpace(apiKey) || apiKey == currentApiKeyPlaceholder)
                {
                    _logger.Log("FOUT: Gelieve een geldige API Key in te vullen.");
                    SetUiEnabled(true); btnStopOrganization.Enabled = false; progressBar1.Visible = false; return;
                }
            }

            if (!Directory.Exists(txtSourceFolder.Text))
            {
                _logger.Log($"FOUT: Bronmap '{txtSourceFolder.Text}' niet gevonden.");
                SetUiEnabled(true); btnStopOrganization.Enabled = false; progressBar1.Visible = false; return;
            }

            _logger.Log($"Starten met organiseren van bestanden uit: {txtSourceFolder.Text} (inclusief submappen)");
            _logger.Log($"Gebruikt model: {selectedModel}");
            if (chkRenameFiles.Checked)
            {
                _logger.Log("Bestandsnamen worden hernoemd met AI-suggesties.");
            }

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
                progressBar1.Visible = false;
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

         //   MessageBox.Show("sa", "ShareX", MessageBoxButtons.OK, MessageBoxIcon.Information);
            using (var dialog = new CommonSaveFileDialog())
            {
                dialog.Filters.Add(new CommonFileDialogFilter("Tekstbestanden", "*.txt"));
                dialog.DefaultExtension = "txt";
                dialog.Title = "Sla logbestand op";
                dialog.DefaultFileName = $"AI_Organizer_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                dialog.InitialDirectory = Directory.Exists(txtDestinationFolder.Text) ? txtDestinationFolder.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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
            txtApiKey.Enabled = enabled && cmbProviderSelection.SelectedItem?.ToString() != "Lokaal ONNX-model";
            txtSourceFolder.Enabled = enabled;
            btnSelectSourceFolder.Enabled = enabled;
            txtDestinationFolder.Enabled = enabled;
            btnSelectDestinationFolder.Enabled = enabled;
            cmbModelSelection.Enabled = enabled;
            cmbProviderSelection.Enabled = enabled;
            txtAzureEndpoint.Enabled = enabled && cmbProviderSelection.SelectedItem?.ToString() == "Azure OpenAI";
            btnStartOrganization.Enabled = enabled;
            btnRenameSingleFile.Enabled = enabled;
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

        private async void btnRenameSingleFile_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
            SetUiEnabled(false);
            btnStopOrganization.Enabled = false;
            btnSaveLog.Enabled = false;
            progressBar1.Visible = false;

            string apiKey = txtApiKey.Text;
            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "Gemini (Google)";
            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "gemini-1.5-pro-latest";
            string azureEndpoint = txtAzureEndpoint?.Text;

            // Zet ONNX-paths voor een enkele bestandsactie
            _fileOrganizerService.SelectedOnnxModelPath = SelectedOnnxModelPath;
            _fileOrganizerService.SelectedOnnxVocabPath = SelectedOnnxVocabPath;

            if (providerName != "Lokaal ONNX-model")
            {
                if (string.IsNullOrWhiteSpace(apiKey) || (txtApiKey.Tag != null && apiKey == txtApiKey.Tag.ToString()))
                {
                    _logger.Log("FOUT: Gelieve een geldige API Key in te vullen.");
                    SetUiEnabled(true); return;
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = false;
                dialog.EnsureFileExists = true;
                dialog.Multiselect = false;
                dialog.Title = "Selecteer een bestand om te hernoemen";
                dialog.Filters.Add(new CommonFileDialogFilter("Ondersteunde bestanden", "*.pdf;*.docx;*.txt;*.md"));
                dialog.Filters.Add(new CommonFileDialogFilter("PDF Bestanden", "*.pdf"));
                dialog.Filters.Add(new CommonFileDialogFilter("Word Documenten", "*.docx"));
                dialog.Filters.Add(new CommonFileDialogFilter("Tekst Bestanden", "*.txt;*.md"));
                dialog.Filters.Add(new CommonFileDialogFilter("Alle Bestanden", "*.*"));
                dialog.InitialDirectory = txtSourceFolder.Text;
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string filePath = dialog.FileName;
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
                        _logger.Log($"KRITIEKE FOUT tijdens enkel bestand hernoemen: {ex.Message}");
                    }
                }
                else
                {
                    _logger.Log("Bestandselectie geannuleerd. Geen bestand hernoemd.");
                }
            }
            SetUiEnabled(true);
            btnSaveLog.Enabled = true;
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        private void btnGenerateStandardFolders_Click(object sender, EventArgs e)
        {
            try
            {
                var generator = new AI_FileOrganizer.Utils.PersoonlijkeMappenStructuurGenerator();
                generator.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Er is een fout opgetreden bij het aanmaken van de mappenstructuur:\n\n" + ex.Message, "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lblSourceFolder_Click(object sender, EventArgs e)
        {
        }
    }
}
