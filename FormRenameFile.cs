using System;
using System.IO;
using System.Linq; // This using statement is explicitly needed for .Any() and .Contains()
using System.Windows.Forms;

namespace AI_FileOrganizer2
{
    public partial class FormRenameFile : Form
    {
        public string NewFileName { get; private set; }
        public bool SkipFile { get; private set; } = false;

        public FormRenameFile(string originalFileName, string suggestedFileName)
        {
            InitializeComponent(); // This call links to the FormRenameFile.Designer.cs file
            lblOriginalFileName.Text = originalFileName;
            txtSuggestedFileName.Text = suggestedFileName;
            NewFileName = suggestedFileName; // Default value
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string newName = txtSuggestedFileName.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("De nieuwe bestandsnaam mag niet leeg zijn.", "Ongeldige naam", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Basis validatie: verwijder ongeldige karakters voor bestandsnamen
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (newName.Any(c => invalidChars.Contains(c)))
            {
                MessageBox.Show($"De nieuwe bestandsnaam bevat ongeldige karakters. Vermijd: {string.Join("", invalidChars)}", "Ongeldige naam", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            NewFileName = newName;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnSkip_Click(object sender, EventArgs e)
        {
            SkipFile = true;
            this.DialogResult = DialogResult.OK; // Gebruik OK om aan te geven dat een beslissing is genomen (overslaan)
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void FormRenameFile_Load(object sender, EventArgs e)
        {
            // Any specific load logic for the form
        }
    }
}