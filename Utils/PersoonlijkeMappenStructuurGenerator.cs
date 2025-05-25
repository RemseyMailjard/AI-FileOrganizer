using System;
using System.IO;
using System.Windows.Forms;

namespace AI_FileOrganizer2.Utils
{
    public class PersoonlijkeMappenStructuurGenerator
    {
        public string LogFile { get; private set; }
        public int FoldersCreated { get; private set; }
        public int FoldersExisted { get; private set; }
        public int FoldersFailed { get; private set; }
        public string ScriptVersion { get; private set; } = "1.5.3";

        public PersoonlijkeMappenStructuurGenerator()
        {
            LogFile = Path.Combine(Path.GetTempPath(), "PersoonlijkeMappenStructuurGenerator_log.txt");
        }

        public void Start()
        {
            FoldersCreated = 0;
            FoldersExisted = 0;
            FoldersFailed = 0;

            File.WriteAllText(LogFile, ""); // Leeg logbestand
            LogMessage($"Script gestart (v{ScriptVersion}).");

            // 1. Kies root directory
            string defaultRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Persoonlijke Administratie");
            string root = PromptForFolder(defaultRoot);

            // 2. Bepaal jaartal
            int currentYear = DateTime.Now.Year;
            int targetYear = PromptForYearWithMessageBox(currentYear);
            int previousYear = targetYear - 1;

            LogMessage($"Doeljaar ingesteld op: {targetYear}, archiefjaar: {previousYear}");

            // 3. Bevestiging
            string summary = $"Mappenstructuur wordt aangemaakt in:\n    {root}\nVoor het jaar: {targetYear}\nArchief voor: {previousYear}\n\nKlik OK om te starten, of Annuleren om af te breken.";
            if (MessageBox.Show(summary, "Bevestigen", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.Cancel)
                return;

            LogMessage($"Start mapcreatie: Locatie={root}, Jaar={targetYear}.");
            CreateFolder(root);

            if (!Directory.Exists(root))
            {
                MessageBox.Show("Hoofdmap kon niet worden aangemaakt: " + root, "FOUT", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage("FOUT: Hoofdmap niet aangemaakt.");
                return;
            }

            // 4. Mappenstructuur aanmaken (zoals batch)
            CreateFolder($"{root}\\1. Financien");
            CreateFolder($"{root}\\1. Financien\\Bankafschriften\\{targetYear}");
            CreateFolder($"{root}\\1. Financien\\Spaarrekeningen");
            CreateFolder($"{root}\\1. Financien\\Beleggingen");

            CreateFolder($"{root}\\2. Belastingen");
            CreateFolder($"{root}\\2. Belastingen\\Aangiften Inkomstenbelasting\\{targetYear}");
            CreateFolder($"{root}\\2. Belastingen\\Correspondentie Belastingdienst\\{targetYear}");

            CreateFolder($"{root}\\3. Verzekeringen");
            CreateFolder($"{root}\\3. Verzekeringen\\Zorgverzekering");
            CreateFolder($"{root}\\3. Verzekeringen\\Inboedel Opstal");
            CreateFolder($"{root}\\3. Verzekeringen\\Autoverzekering");
            CreateFolder($"{root}\\3. Verzekeringen\\Overig");

            CreateFolder($"{root}\\4. Woning");
            CreateFolder($"{root}\\4. Woning\\Hypotheek of Huur");
            CreateFolder($"{root}\\4. Woning\\Nutsvoorzieningen");
            CreateFolder($"{root}\\4. Woning\\Onderhoud");
            CreateFolder($"{root}\\4. Woning\\Inrichting");

            CreateFolder($"{root}\\5. Gezondheid");
            CreateFolder($"{root}\\5. Gezondheid\\Medische dossiers");
            CreateFolder($"{root}\\5. Gezondheid\\Recepten en medicijnen");
            CreateFolder($"{root}\\5. Gezondheid\\Zorgclaims");

            CreateFolder($"{root}\\6. Voertuigen");
            CreateFolder($"{root}\\6. Voertuigen\\Onderhoudsrecords");
            CreateFolder($"{root}\\6. Voertuigen\\Verzekeringen");
            CreateFolder($"{root}\\6. Voertuigen\\Registratie Belasting");

            CreateFolder($"{root}\\7. Carriere");
            CreateFolder($"{root}\\7. Carriere\\CV");
            CreateFolder($"{root}\\7. Carriere\\Certificaten");
            CreateFolder($"{root}\\7. Carriere\\Sollicitaties");

            CreateFolder($"{root}\\8. Reizen");
            CreateFolder($"{root}\\8. Reizen\\{targetYear} Andalusie");
            CreateFolder($"{root}\\8. Reizen\\Vooruitblik Volgend Jaar");

            CreateFolder($"{root}\\9. Hobby");
            CreateFolder($"{root}\\9. Hobby\\Gezondheid en fitness");
            CreateFolder($"{root}\\9. Hobby\\Recepten");
            CreateFolder($"{root}\\9. Hobby\\YouTube concepten");

            CreateFolder($"{root}\\10. Familie en kinderen");
            CreateFolder($"{root}\\10. Familie en kinderen\\School en onderwijs");
            CreateFolder($"{root}\\10. Familie en kinderen\\Activiteiten en vakanties");
            CreateFolder($"{root}\\10. Familie en kinderen\\Overige documenten");

            CreateFolder($"{root}\\11. Digitale bezittingen");
            CreateFolder($"{root}\\11. Digitale bezittingen\\Wachtwoordkluis");
            CreateFolder($"{root}\\11. Digitale bezittingen\\2FA backups");
            CreateFolder($"{root}\\11. Digitale bezittingen\\Software licenties");

            CreateFolder($"{root}\\12. Abonnementen en lidmaatschappen");
            CreateFolder($"{root}\\12. Abonnementen en lidmaatschappen\\Streaming");
            CreateFolder($"{root}\\12. Abonnementen en lidmaatschappen\\Sportclub");
            CreateFolder($"{root}\\12. Abonnementen en lidmaatschappen\\Overige abonnementen");

            CreateFolder($"{root}\\13. Foto en video");
            CreateFolder($"{root}\\13. Foto en video\\{targetYear}");
            CreateFolder($"{root}\\13. Foto en video\\{targetYear}\\06 Graduatie");

            CreateFolder($"{root}\\14. Opleiding");
            CreateFolder($"{root}\\14. Opleiding\\Cursussen");
            CreateFolder($"{root}\\14. Opleiding\\Studie materiaal");
            CreateFolder($"{root}\\14. Opleiding\\Certificaten");

            CreateFolder($"{root}\\15. Juridisch");
            CreateFolder($"{root}\\15. Juridisch\\Contracten");
            CreateFolder($"{root}\\15. Juridisch\\Boetes");
            CreateFolder($"{root}\\15. Juridisch\\Officiele correspondentie");

            CreateFolder($"{root}\\16. Nalatenschap");
            CreateFolder($"{root}\\16. Nalatenschap\\Testament");
            CreateFolder($"{root}\\16. Nalatenschap\\Levenstestament");
            CreateFolder($"{root}\\16. Nalatenschap\\Uitvaartwensen");

            CreateFolder($"{root}\\17. Noodinformatie");
            CreateFolder($"{root}\\17. Noodinformatie\\Paspoort scans");
            CreateFolder($"{root}\\17. Noodinformatie\\ICE contacten");
            CreateFolder($"{root}\\17. Noodinformatie\\Medische alert");

            CreateFolder($"{root}\\18. Huisinventaris");
            CreateFolder($"{root}\\18. Huisinventaris\\Fotos");
            CreateFolder($"{root}\\18. Huisinventaris\\Aankoopbewijzen");

            CreateFolder($"{root}\\19. Persoonlijke projecten");
            CreateFolder($"{root}\\19. Persoonlijke projecten\\DIY plannen");
            CreateFolder($"{root}\\19. Persoonlijke projecten\\Side hustle ideeen");

            CreateFolder($"{root}\\20. Huisdieren");
            CreateFolder($"{root}\\20. Huisdieren\\Dierenpaspoorten");
            CreateFolder($"{root}\\20. Huisdieren\\Dierenarts");
            CreateFolder($"{root}\\20. Huisdieren\\Verzekeringen");

            CreateFolder($"{root}\\99. Archief");
            CreateFolder($"{root}\\99. Archief\\{previousYear}");
            CreateFolder($"{root}\\99. Archief\\{previousYear}\\Oude projecten");

            // 5. Resultaat tonen
            string resultMsg = $"SUCCES! Alle mappen zijn succesvol verwerkt.\nGemaakt: {FoldersCreated}, Bestonden al: {FoldersExisted}, Mislukt: {FoldersFailed}.\n\nLogbestand: {LogFile}";
            MessageBox.Show(resultMsg, "Resultaat", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LogMessage("Script succesvol beëindigd.");
        }

        private string PromptForFolder(string defaultRoot)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Kies hoofdmap voor je Persoonlijke Administratie:";
                dialog.SelectedPath = defaultRoot;
                if (dialog.ShowDialog() == DialogResult.OK)
                    return dialog.SelectedPath;
                else
                    return defaultRoot;
            }
        }

        /// <summary>
        /// Toont eerst een MessageBox voor keuze, daarna eventueel een input-dialog voor een aangepast jaartal.
        /// </summary>
        private int PromptForYearWithMessageBox(int currentYear)
        {
            var result = MessageBox.Show(
                $"Wil je de mappenstructuur aanmaken voor het huidige jaar ({currentYear})?\n\nKies 'Nee' om een ander jaar op te geven.",
                "Doeljaar kiezen",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes || result == DialogResult.Cancel)
                return currentYear;

            // Gebruiker wil een ander jaar: toon een input-dialog
            string yearInput = ShowYearInputDialog(currentYear.ToString());
            if (string.IsNullOrWhiteSpace(yearInput))
                return currentYear;

            if (int.TryParse(yearInput, out int jaar) && jaar >= 1000 && jaar <= 9999)
                return jaar;
            else
            {
                MessageBox.Show($"Ongeldige invoer ('{yearInput}'). Standaard jaar ({currentYear}) wordt gebruikt.", "Let op", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return currentYear;
            }
        }

        // Eenvoudige jaar-invul dialog met TextBox
        private string ShowYearInputDialog(string defaultYear)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                Text = "Voer jaartal in",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            };
            Label textLabel = new Label() { Left = 20, Top = 20, Text = "Geef het gewenste jaartal:", Width = 240 };
            TextBox inputBox = new TextBox() { Left = 20, Top = 50, Width = 240, Text = defaultYear };
            Button confirmation = new Button() { Text = "OK", Left = 100, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text : defaultYear;
        }

        private void CreateFolder(string folder)
        {
            try
            {
                if (Directory.Exists(folder))
                {
                    LogMessage("BESTAAT AL: " + folder);
                    FoldersExisted++;
                }
                else
                {
                    Directory.CreateDirectory(folder);
                    LogMessage("AANGEMAAKT: " + folder);
                    FoldersCreated++;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"FOUT: Kon niet aanmaken: {folder} ({ex.Message})");
                FoldersFailed++;
            }
        }

        private void LogMessage(string msg)
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now}] {msg}{Environment.NewLine}");
        }
    }
}
