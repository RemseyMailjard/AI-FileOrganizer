using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http; // Nodig voor _httpClient (voor GeminiAiProvider)
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// Externe bibliotheken voor modernere dialoogvensters (.NET Framework 4.8)
using Microsoft.WindowsAPICodePack.Dialogs;

// Jouw eigen services en utilities
using AI_FileOrganizer2.Services; // Voor IAiProvider, AiClassificationService, TextExtractionService
using AI_FileOrganizer2.Utils; // Voor ILogger (jouw custom logger)

// Logger afhankelijkheden (voor ILogger alias)
using ILogger = AI_FileOrganizer2.Utils.ILogger; // Alias om conflict met Microsoft.Extensions.Logging.ILogger te voorkomen


namespace AI_FileOrganizer2
{
    public partial class Form1 : Form
    {
        // Constanten voor LLM-parameters en validatie
        private const int MAX_TEXT_LENGTH_FOR_LLM = 8000;
        private const int MIN_SUBFOLDER_NAME_LENGTH = 3;
        private const int MAX_SUBFOLDER_NAME_LENGTH = 50;
        private const int MAX_FILENAME_LENGTH = 100; // Maximale lengte voor AI-gegenereerde bestandsnaam
        private readonly AiClassificationService _aiService;
        private readonly TextExtractionService _textExtractionService; // Zorg dat deze hier staat

        // Logger instantie voor UI-updates
        private ILogger _logger;

        // Ondersteunde bestandsextensies voor tekstextractie
        private readonly string[] SUPPORTED_EXTENSIONS = { ".pdf", ".docx", ".txt", ".md" };

        // Mappen categorieën voor organisatie
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

        // HttpClient is statisch voor hergebruik en efficiëntie bij API-aanroepen (vooral voor Gemini)
        private static readonly HttpClient _httpClient = new HttpClient();
        private long _totalTokensUsed = 0;
        private CancellationTokenSource _cancellationTokenSource;

        // Services die via Dependency Injection (DI) zouden gaan, maar hier handmatig geïnitialiseerd



        public Form1()
        {
            InitializeComponent();

            // Initialiseer de logger EERST, want andere services hebben deze nodig
            _logger = new UiLogger(rtbLog);

            // Initialiseer de services, geef de logger mee waar nodig
       

            _aiService = new AiClassificationService(_logger);
            _textExtractionService = new TextExtractionService(_logger);


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialisatie van de logger is nu in de constructor, dus deze regel is overbodig.
            // _logger = new UiLogger(rtbLog);

            // Zorg ervoor dat provider-selectie is ingesteld (aangenomen in designer)
            if (cmbProviderSelection.Items.Count == 0)
            {
                cmbProviderSelection.Items.AddRange(new object[]
                {
                    "Gemini (Google)",
                    "OpenAI (openai.com)",
                    "Azure OpenAI"
                });
            }

            // Voorkom dubbele event-handlers en voeg toe
            cmbProviderSelection.SelectedIndexChanged -= cmbProviderSelection_SelectedIndexChanged;
            cmbProviderSelection.SelectedIndexChanged += cmbProviderSelection_SelectedIndexChanged;
            cmbProviderSelection.SelectedIndex = 0; // Trigger selectie om modellen in te stellen

            // Standaard API-sleutel en placeholder instellen
            txtApiKey.Text = "YOUR_GOOGLE_API_KEY_HERE";
            SetupApiKeyPlaceholder(txtApiKey, "YOUR_GOOGLE_API_KEY_HERE");
            txtApiKey.UseSystemPasswordChar = true; // Verberg de API-sleutel

            // Standaard map-paden instellen
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            txtSourceFolder.Text = Path.Combine(desktopPath, "AI Organizer Bronmap");
            txtDestinationFolder.Text = Path.Combine(documentsPath, "AI Organizer Resultaat");

            // UI-elementen initialiseren
            lblTokensUsed.Text = "Tokens gebruikt: 0";
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Style = ProgressBarStyle.Continuous; // Voortgangsbalk toont continue voortgang
            progressBar1.Visible = false; // Verberg de voortgangsbalk initieel
            btnStopOrganization.Enabled = false; // Stop-knop is initieel uitgeschakeld
            btnSaveLog.Enabled = false; // Opslaan-knop is initieel uitgeschakeld
            chkRenameFiles.Checked = false; // Bestanden standaard niet hernoemen

            // Azure velden initieel verbergen
            lblAzureEndpoint.Visible = false;
            txtAzureEndpoint.Visible = false;
        }

        /// <summary>
        /// Update de beschikbare modellen op basis van de geselecteerde AI-provider.
        /// </summary>
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
                    "YOUR-AZURE-DEPLOYMENT-NAME" // Azure deployment names are custom
                });
                lblApiKey.Text = "Azure OpenAI API Key:";
                lblAzureEndpoint.Visible = true;
                txtAzureEndpoint.Visible = true;
            }
            cmbModelSelection.SelectedIndex = 0; // Selecteer altijd het eerste item in de lijst
        }

        /// <summary>
        /// Stelt een placeholdertekst in voor een TextBox en beheert de kleur.
        /// </summary>
        private void SetupApiKeyPlaceholder(TextBox textBox, string placeholderText)
        {
            textBox.GotFocus -= RemoveApiKeyPlaceholderInternal;
            textBox.LostFocus -= AddApiKeyPlaceholderInternal;

            textBox.Tag = placeholderText; // Sla de placeholder op in de Tag-eigenschap

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

        /// <summary>
        /// Verwijdert de placeholdertekst wanneer de TextBox focus krijgt.
        /// </summary>
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

        /// <summary>
        /// Voegt de placeholdertekst toe wanneer de TextBox focus verliest en leeg is.
        /// </summary>
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

        /// <summary>
        /// Handelt het klikken op de "Bronmap selecteren" knop af, met een modern dialoogvenster.
        /// </summary>
        private void btnSelectSourceFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true; // Cruciaal: maakt dit een map-selectie dialoogvenster
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
                    txtSourceFolder.Text = dialog.FileName; // FileName bevat hier het volledige pad naar de geselecteerde map
                }
            }
        }

        /// <summary>
        /// Handelt het klikken op de "Doelmap selecteren" knop af, met een modern dialoogvenster.
        /// </summary>
        private void btnSelectDestinationFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true; // Cruciaal: maakt dit een map-selectie dialoogvenster
                dialog.Title = "Selecteer de doelmap voor geordende bestanden";
                dialog.EnsurePathExists = true; // Zorgt dat de map bestaat als je de naam in typt, of helpt met aanmaken

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
                    txtDestinationFolder.Text = dialog.FileName; // FileName bevat hier het volledige pad naar de geselecteerde map
                }
            }
        }

        /// <summary>
        /// Start het organisatieproces van bestanden.
        /// </summary>
        private async void btnStartOrganization_Click(object sender, EventArgs e)
        {
            rtbLog.Clear(); // Maak het logvenster leeg
            SetUiEnabled(false); // Schakel UI-elementen uit tijdens verwerking
            btnStopOrganization.Enabled = true; // Schakel de stop-knop in
            btnSaveLog.Enabled = false; // Schakel de opslaan-log-knop uit

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
            UpdateTokensUsedLabel(); // Reset en update de token-teller
            _cancellationTokenSource = new CancellationTokenSource(); // Initialiseer CancellationTokenSource

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
                SetUiEnabled(true); // Schakel UI-elementen weer in
                btnStopOrganization.Enabled = false; // Schakel de stop-knop uit
                btnSaveLog.Enabled = true; // Schakel de opslaan-log-knop in
                _cancellationTokenSource.Dispose(); // Ruim de CancellationTokenSource op
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Annuleert het lopende organisatieproces.
        /// </summary>
        private void btnStopOrganization_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel(); // Vraag om annulering
            _logger.Log("Annulering aangevraagd...");
            btnStopOrganization.Enabled = false; // Schakel de stop-knop uit om dubbelklikken te voorkomen
        }

        /// <summary>
        /// Slaat de inhoud van het logvenster op in een tekstbestand, met een modern dialoogvenster.
        /// </summary>
        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonSaveFileDialog()) // Gebruik CommonSaveFileDialog voor een moderne UI
            {
                // Voeg filters toe op de modernere manier
                dialog.Filters.Add(new CommonFileDialogFilter("Tekstbestanden", "*.txt"));
                dialog.DefaultExtension = "txt"; // Stel de standaardextensie in

                dialog.Title = "Sla logbestand op";
                // Gebruik DefaultFileName om de voorgestelde bestandsnaam in te stellen
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

        /// <summary>
        /// Schakelt de gebruikersinterface-elementen in of uit.
        /// </summary>
        /// <param name="enabled">True om in te schakelen, False om uit te schakelen.</param>
        private void SetUiEnabled(bool enabled)
        {
            txtApiKey.Enabled = enabled;
            txtSourceFolder.Enabled = enabled;
            btnSelectSourceFolder.Enabled = enabled;
            txtDestinationFolder.Enabled = enabled;
            btnSelectDestinationFolder.Enabled = enabled;
            cmbModelSelection.Enabled = enabled;
            cmbProviderSelection.Enabled = enabled;
            txtAzureEndpoint.Enabled = enabled;
            btnStartOrganization.Enabled = enabled;
            // btnSaveLog.Enabled = enabled; // Deze wordt apart beheerd (aangezet in finally van OrganizeFilesAsync)
            linkLabelAuthor.Enabled = enabled;
            chkRenameFiles.Enabled = enabled;
        }

        /// <summary>
        /// Werkt het label voor het aantal gebruikte tokens bij.
        /// </summary>
        private void UpdateTokensUsedLabel()
        {
            if (InvokeRequired)
            {
                // BeginInvoke om thread-safe updates naar de UI toe te staan
                BeginInvoke(new Action(UpdateTokensUsedLabel));
                return;
            }
            lblTokensUsed.Text = $"Tokens gebruikt: {_totalTokensUsed}";
        }

        /// <summary>
        /// De hoofdlogica voor het organiseren van bestanden.
        /// </summary>
        private async Task OrganizeFilesAsync(string sourcePath, string destinationBasePath, string apiKey, CancellationToken cancellationToken)
        {
            int processedFiles = 0;
            int movedFiles = 0;
            int filesWithSubfolders = 0;
            int renamedFiles = 0;

            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "gemini-1.5-pro-latest";
            bool shouldRenameFiles = chkRenameFiles.Checked;
            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "Gemini (Google)";
            string azureEndpoint = txtAzureEndpoint?.Text;

            // Instantieer de juiste AI provider op basis van de gebruikersselectie
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
                        return; // Stop de organisatie als provider onbekend is
                }
            }
            catch (ArgumentException ex) // Vang specifieke exceptions van provider constructors (bijv. ongeldige Azure endpoint)
            {
                _logger.Log($"FOUT: Configuratieprobleem voor AI-provider '{providerName}': {ex.Message}. Annuleer organisatie.");
                return;
            }


            // Verzamel alle ondersteunde bestanden in de bronmap en submappen
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
                    cancellationToken.ThrowIfCancellationRequested(); // Gooi uitzondering om de lus te beëindigen
                }

                var fileInfo = new FileInfo(filePath);

                processedFiles++;
                _logger.Log($"\n[BESTAND] Verwerken van: {fileInfo.Name} (locatie: {Path.GetDirectoryName(filePath)})");

                // *** Hier wordt de TextExtractionService gebruikt ***
                string extractedText = _textExtractionService.ExtractText(filePath); // Gebruik de geoptimaliseerde ExtractText methode

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    _logger.Log($"INFO: Geen zinvolle tekst geëxtraheerd uit {fileInfo.Name}. Bestand wordt behandeld met bestandsnaam context (als fallback).");
                    extractedText = fileInfo.Name; // Gebruik bestandsnaam als minimale context
                }


                // Tronqueer tekst als deze te lang is voor de LLM
                if (extractedText.Length > MAX_TEXT_LENGTH_FOR_LLM)
                {
                    _logger.Log($"WAARSCHUWING: Tekstlengte voor '{fileInfo.Name}' overschrijdt {MAX_TEXT_LENGTH_FOR_LLM} tekens. Tekst wordt afgekapt.");
                    extractedText = extractedText.Substring(0, MAX_TEXT_LENGTH_FOR_LLM);
                }

                string llmCategoryChoice = await _aiService.ClassifyCategoryAsync(
    extractedText,
    FOLDER_CATEGORIES.Keys.ToList(),

    currentAiProvider, // Wordt gebruikt om de AI-call uit te voeren
    selectedModel,
    cancellationToken
);


                if (!string.IsNullOrWhiteSpace(llmCategoryChoice))
                {
                    string targetCategoryFolderName = FOLDER_CATEGORIES.ContainsKey(llmCategoryChoice)
                        ? FOLDER_CATEGORIES[llmCategoryChoice]
                        : FALLBACK_FOLDER_NAME;
                    string targetCategoryFolderPath = Path.Combine(destinationBasePath, targetCategoryFolderName);
                    Directory.CreateDirectory(targetCategoryFolderPath); // Zorg dat de categoriemap bestaat

                    string finalDestinationFolderPath = targetCategoryFolderPath;

                    _logger.Log($"INFO: Poging tot genereren submapnaam voor '{fileInfo.Name}'...");

                    // *** Correcte aanroep naar AiClassificationService.SuggestSubfolderNameAsync ***
                    string subfolderNameSuggestion = await _aiService.SuggestSubfolderNameAsync(
                        extractedText,
                        fileInfo.Name,
                        currentAiProvider, // De IAiProvider instantie
                        selectedModel,
                        cancellationToken
                    );

                    if (!string.IsNullOrWhiteSpace(subfolderNameSuggestion))
                    {
                        // Sanitize en valideer de voorgestelde submapnaam
                        subfolderNameSuggestion = FileUtils.SanitizeFolderOrFileName(subfolderNameSuggestion);
                        if (subfolderNameSuggestion.Length < MIN_SUBFOLDER_NAME_LENGTH || subfolderNameSuggestion.Length > MAX_SUBFOLDER_NAME_LENGTH)
                        {
                            _logger.Log($"WAARSCHUWING: AI-gegenereerde submapnaam '{subfolderNameSuggestion}' is ongeldig (lengte). Wordt niet gebruikt.");
                            subfolderNameSuggestion = null; // Ongeldige suggestie
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
                        // Herbouw het relatieve pad van de originele bronstructuur
                        string relativePathFromSource = GetRelativePath(sourcePath, Path.GetDirectoryName(filePath));
                        // Dit zorgt ervoor dat als bestanden al in submappen stonden in de bron, die submappen worden gerecreëerd onder de nieuwe categorie/submap
                        string finalTargetDirectory = Path.Combine(finalDestinationFolderPath, relativePathFromSource);
                        Directory.CreateDirectory(finalTargetDirectory); // Zorg dat de volledige doelmapstructuur bestaat

                        string newFileName = fileInfo.Name; // Standaard de originele naam
                        if (_aiService == null)
                        {
                            _logger.Log("FOUT: _aiService is niet geïnitialiseerd.");
                            SetUiEnabled(true); return;
                        }
                        if (currentAiProvider == null)
                        {
                            _logger.Log("FOUT: currentAiProvider is niet geïnitialiseerd.");
                            SetUiEnabled(true); return;
                        }
                        if (string.IsNullOrWhiteSpace(selectedModel))
                        {
                            _logger.Log("FOUT: selectedModel is leeg of null.");
                            SetUiEnabled(true); return;
                        }
                        if (_cancellationTokenSource == null)
                        {
                            _logger.Log("FOUT: _cancellationTokenSource is null.");
                            SetUiEnabled(true); return;
                        }
                        if (shouldRenameFiles)
                        {
                            _logger.Log($"INFO: AI-bestandsnaam genereren voor '{fileInfo.Name}'...");

                            // *** Correcte aanroep naar AiClassificationService.SuggestFileNameAsync ***

                            if (_aiService == null)
                            {
                                _logger.Log("FOUT: _aiService is null in btnRenameSingleFile_Click.");
                                SetUiEnabled(true);
                                return;
                            }

                            string suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                                extractedText,
                                fileInfo.Name,
                                currentAiProvider, // De IAiProvider instantie
                                selectedModel,
                                cancellationToken
                            );

                            // Toon dialoogvenster om naam te bevestigen/wijzigen
                            using (var renameForm = new FormRenameFile(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension))
                            {
                                if (renameForm.ShowDialog() == DialogResult.OK)
                                {
                                    if (renameForm.SkipFile)
                                    {
                                        _logger.Log($"INFO: Gebruiker koos om '{fileInfo.Name}' niet te hernoemen. Bestand wordt verplaatst met originele naam.");
                                        // newFileName blijft de originele naam
                                    }
                                    else
                                    {
                                        string proposedFullName = renameForm.NewFileName;
                                        string proposedBaseName = Path.GetFileNameWithoutExtension(proposedFullName);
                                        string proposedExtension = Path.GetExtension(proposedFullName);

                                        // Zorg ervoor dat de extensie behouden blijft of correct wordt afgehandeld
                                        if (string.IsNullOrEmpty(proposedExtension))
                                        {
                                            proposedFullName = proposedBaseName + fileInfo.Extension; // Voeg originele extensie toe als gebruiker deze verwijderde
                                        }
                                        else if (proposedExtension.ToLower() != fileInfo.Extension.ToLower())
                                        {
                                            _logger.Log($"WAARSCHUWING: Bestandsnaam '{proposedFullName}' heeft afwijkende extensie. Originele extensie '{fileInfo.Extension}' wordt behouden.");
                                            proposedFullName = proposedBaseName + fileInfo.Extension; // Forceer originele extensie
                                        }

                                        newFileName = FileUtils.SanitizeFileName(proposedFullName); // Sanitize de bevestigde nieuwe naam

                                        // Pas maximale lengtebeperking toe
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
                                    // Gebruiker annuleerde het hernoem-dialoogvenster
                                    _logger.Log($"INFO: Hernoem-actie voor '{fileInfo.Name}' geannuleerd door gebruiker. Bestand wordt overgeslagen.");
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
                            string uniqueDestinationFilePath = destinationFilePath;
                            while (File.Exists(uniqueDestinationFilePath))
                            {
                                uniqueDestinationFilePath = Path.Combine(finalTargetDirectory, $"{baseName}_{counter}{extension}");
                                counter++;
                            }
                            _logger.Log($"INFO: Bestand '{newFileName}' bestaat al op doel. Hernoemd naar '{Path.GetFileName(uniqueDestinationFilePath)}' om conflict te voorkomen.");
                            destinationFilePath = uniqueDestinationFilePath;
                        }

                        // Verplaats het bestand
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
                progressBar1.Increment(1); // Verhoog de voortgangsbalk voor elk verwerkt bestand
            }

            progressBar1.Visible = false; // Verberg de voortgangsbalk na voltooiing

            // Samenvattingslogberichten
            _logger.Log($"\nTotaal aantal bestanden bekeken (met ondersteunde extensie): {processedFiles}");
            _logger.Log($"Aantal bestanden succesvol verplaatst: {movedFiles}");
            _logger.Log($"Aantal bestanden geplaatst in een AI-gegenereerde submap: {filesWithSubfolders}");
            _logger.Log($"Aantal bestanden hernoemd: {renamedFiles}");
        }

        /// <summary>
        /// Berekent het relatieve pad van een volledig pad ten opzichte van een basispad.
        /// </summary>
        private string GetRelativePath(string basePath, string fullPath)
        {
            string baseWithSeparator = AppendDirectorySeparatorChar(basePath);
            Uri baseUri = new Uri(baseWithSeparator);
            Uri fullUri = new Uri(fullPath);

            // MakeRelativeUri geeft een URI terug die het relatieve pad vertegenwoordigt
            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            // Converteer het URI-pad (dat '/' gebruikt) naar de directory-scheider van het systeem
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Zorgt ervoor dat een pad eindigt met een directory-scheidingsteken.
        /// </summary>
        private string AppendDirectorySeparatorChar(string path)
        {
            if (!string.IsNullOrEmpty(path) && !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        /// <summary>
        /// Opent de LinkedIn-pagina van de auteur in de standaardbrowser.
        /// </summary>
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
            SetUiEnabled(false); // Schakel UI uit tijdens verwerking
            btnStopOrganization.Enabled = false; // Stop-knop is niet relevant voor single file rename
            btnSaveLog.Enabled = false; // Log-opslaan is niet direct relevant tot het klaar is

            string apiKey = txtApiKey.Text;
            if  (string.IsNullOrWhiteSpace(apiKey) || (txtApiKey.Tag != null && apiKey == txtApiKey.Tag.ToString()))

                {
                    _logger.Log("FOUT: Gelieve een geldige API Key in te vullen.");
                SetUiEnabled(true); return;
            }

            string selectedModel = cmbModelSelection.SelectedItem?.ToString() ?? "gemini-1.5-pro-latest";
            string providerName = cmbProviderSelection.SelectedItem?.ToString() ?? "Gemini (Google)";
            string azureEndpoint = txtAzureEndpoint?.Text;

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
                        _logger.Log($"FOUT: Onbekende AI-provider geselecteerd: {providerName}. Kan bestand niet hernoemen.");
                        SetUiEnabled(true); return;
                }
            }
            catch (ArgumentException ex)
            {
                _logger.Log($"FOUT: Configuratieprobleem voor AI-provider '{providerName}': {ex.Message}. Kan bestand niet hernoemen.");
                SetUiEnabled(true); return;
            }

            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = false; // Selecteer een bestand, geen map
                dialog.EnsureFileExists = true; // Zorg dat het bestand bestaat
                dialog.Multiselect = false; // Alleen één bestand tegelijk
                dialog.Title = "Selecteer een bestand om te hernoemen";

                // Voeg filters toe voor ondersteunde bestandstypen
                dialog.Filters.Add(new CommonFileDialogFilter("Ondersteunde bestanden", "*.pdf;*.docx;*.txt;*.md"));
                dialog.Filters.Add(new CommonFileDialogFilter("PDF Bestanden", "*.pdf"));
                dialog.Filters.Add(new CommonFileDialogFilter("Word Documenten", "*.docx"));
                dialog.Filters.Add(new CommonFileDialogFilter("Tekst Bestanden", "*.txt;*.md"));
                dialog.Filters.Add(new CommonFileDialogFilter("Alle Bestanden", "*.*"));

                dialog.InitialDirectory = txtSourceFolder.Text; // Begin in de bronmap
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string filePath = dialog.FileName;
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
                        if (extractedText.Length > MAX_TEXT_LENGTH_FOR_LLM)
                        {
                            _logger.Log($"WAARSCHUWING: Tekstlengte voor '{fileInfo.Name}' overschrijdt {MAX_TEXT_LENGTH_FOR_LLM} tekens. Tekst wordt afgekapt.");
                            extractedText = extractedText.Substring(0, MAX_TEXT_LENGTH_FOR_LLM);
                        }

                        _logger.Log($"INFO: AI-bestandsnaam genereren voor '{fileInfo.Name}'...");
                        string suggestedNewBaseName = await _aiService.SuggestFileNameAsync(
                            extractedText,
                            fileInfo.Name,
                            currentAiProvider,
                            selectedModel,
                            _cancellationTokenSource.Token // Gebruik de bestaande CancellationTokenSource
                        );

                        string newFileName = fileInfo.Name; // Standaard de originele naam

                        using (var renameForm = new FormRenameFile(fileInfo.Name, suggestedNewBaseName + fileInfo.Extension))
                        {
                            if (renameForm.ShowDialog() == DialogResult.OK)
                            {
                                if (renameForm.SkipFile)
                                {
                                    _logger.Log($"INFO: Gebruiker koos om '{fileInfo.Name}' niet te hernoemen. Geen actie ondernomen.");
                                }
                                else
                                {
                                    string proposedFullName = renameForm.NewFileName;
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

                                    newFileName = FileUtils.SanitizeFileName(proposedFullName);

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
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Log("Hernoem-actie geannuleerd.");
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"FOUT: Fout bij hernoemen van {fileInfo.Name}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.Log("Bestandselectie geannuleerd. Geen bestand hernoemd.");
                }
            }
            _logger.Log("\nEnkel bestand hernoemen voltooid.");
            SetUiEnabled(true); // Schakel UI weer in
            btnSaveLog.Enabled = true; // Log opslaan weer mogelijk
        }
    }
}