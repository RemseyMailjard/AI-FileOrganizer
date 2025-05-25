namespace AI_FileOrganizer2
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.lblProvider = new System.Windows.Forms.Label();
            this.cmbProviderSelection = new System.Windows.Forms.ComboBox();
            this.lblApiKey = new System.Windows.Forms.Label();
            this.txtApiKey = new System.Windows.Forms.TextBox();
            this.lblAzureEndpoint = new System.Windows.Forms.Label();
            this.txtAzureEndpoint = new System.Windows.Forms.TextBox();
            this.lblSourceFolder = new System.Windows.Forms.Label();
            this.txtSourceFolder = new System.Windows.Forms.TextBox();
            this.btnSelectSourceFolder = new System.Windows.Forms.Button();
            this.lblDestinationFolder = new System.Windows.Forms.Label();
            this.txtDestinationFolder = new System.Windows.Forms.TextBox();
            this.btnSelectDestinationFolder = new System.Windows.Forms.Button();
            this.lblModel = new System.Windows.Forms.Label();
            this.cmbModelSelection = new System.Windows.Forms.ComboBox();
            this.chkRenameFiles = new System.Windows.Forms.CheckBox();
            this.btnStartOrganization = new System.Windows.Forms.Button();
            this.btnStopOrganization = new System.Windows.Forms.Button();
            this.btnSaveLog = new System.Windows.Forms.Button();
            this.btnRenameSingleFile = new System.Windows.Forms.Button();
            this.btnGenerateStandardFolders = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblTokensUsed = new System.Windows.Forms.Label();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.linkLabelAuthor = new System.Windows.Forms.LinkLabel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Controls.Add(this.lblProvider, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.cmbProviderSelection, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblApiKey, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtApiKey, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblAzureEndpoint, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.txtAzureEndpoint, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.lblSourceFolder, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.txtSourceFolder, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.btnSelectSourceFolder, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.lblDestinationFolder, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.txtDestinationFolder, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.btnSelectDestinationFolder, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.lblModel, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.cmbModelSelection, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.chkRenameFiles, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.btnStartOrganization, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.btnStopOrganization, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.btnSaveLog, 2, 7);
            this.tableLayoutPanel1.Controls.Add(this.btnRenameSingleFile, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this.btnGenerateStandardFolders, 2, 8);
            this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 9);
            this.tableLayoutPanel1.Controls.Add(this.lblTokensUsed, 2, 9);
            this.tableLayoutPanel1.Controls.Add(this.rtbLog, 0, 10);
            this.tableLayoutPanel1.Controls.Add(this.linkLabelAuthor, 0, 11);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 13;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 2F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(850, 480);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // lblProvider
            // 
            this.lblProvider.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblProvider.AutoSize = true;
            this.lblProvider.Location = new System.Drawing.Point(3, 6);
            this.lblProvider.Name = "lblProvider";
            this.lblProvider.Size = new System.Drawing.Size(61, 16);
            this.lblProvider.TabIndex = 0;
            this.lblProvider.Text = "Provider:";
            // 
            // cmbProviderSelection
            // 
            this.cmbProviderSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.cmbProviderSelection, 2);
            this.cmbProviderSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProviderSelection.Items.AddRange(new object[] {
            "Gemini (Google)",
            "OpenAI (openai.com)",
            "Azure OpenAI"});
            this.cmbProviderSelection.Location = new System.Drawing.Point(173, 3);
            this.cmbProviderSelection.Name = "cmbProviderSelection";
            this.cmbProviderSelection.Size = new System.Drawing.Size(674, 24);
            this.cmbProviderSelection.TabIndex = 1;
            this.cmbProviderSelection.SelectedIndexChanged += new System.EventHandler(this.cmbProviderSelection_SelectedIndexChanged);
            // 
            // lblApiKey
            // 
            this.lblApiKey.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(3, 34);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(105, 16);
            this.lblApiKey.TabIndex = 2;
            this.lblApiKey.Text = "Google API Key:";
            // 
            // txtApiKey
            // 
            this.txtApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.txtApiKey, 2);
            this.txtApiKey.Location = new System.Drawing.Point(173, 31);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(674, 22);
            this.txtApiKey.TabIndex = 3;
            this.txtApiKey.UseSystemPasswordChar = true;
            // 
            // lblAzureEndpoint
            // 
            this.lblAzureEndpoint.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAzureEndpoint.AutoSize = true;
            this.lblAzureEndpoint.Location = new System.Drawing.Point(3, 62);
            this.lblAzureEndpoint.Name = "lblAzureEndpoint";
            this.lblAzureEndpoint.Size = new System.Drawing.Size(100, 16);
            this.lblAzureEndpoint.TabIndex = 4;
            this.lblAzureEndpoint.Text = "Azure Endpoint:";
            this.lblAzureEndpoint.Visible = false;
            // 
            // txtAzureEndpoint
            // 
            this.txtAzureEndpoint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.txtAzureEndpoint, 2);
            this.txtAzureEndpoint.Location = new System.Drawing.Point(173, 59);
            this.txtAzureEndpoint.Name = "txtAzureEndpoint";
            this.txtAzureEndpoint.Size = new System.Drawing.Size(674, 22);
            this.txtAzureEndpoint.TabIndex = 5;
            this.txtAzureEndpoint.Visible = false;
            // 
            // lblSourceFolder
            // 
            this.lblSourceFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSourceFolder.AutoSize = true;
            this.lblSourceFolder.Location = new System.Drawing.Point(3, 90);
            this.lblSourceFolder.Name = "lblSourceFolder";
            this.lblSourceFolder.Size = new System.Drawing.Size(95, 16);
            this.lblSourceFolder.TabIndex = 6;
            this.lblSourceFolder.Text = "Source Folder:";
            // 
            // txtSourceFolder
            // 
            this.txtSourceFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSourceFolder.Location = new System.Drawing.Point(173, 87);
            this.txtSourceFolder.Name = "txtSourceFolder";
            this.txtSourceFolder.ReadOnly = true;
            this.txtSourceFolder.Size = new System.Drawing.Size(504, 22);
            this.txtSourceFolder.TabIndex = 7;
            // 
            // btnSelectSourceFolder
            // 
            this.btnSelectSourceFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnSelectSourceFolder.Location = new System.Drawing.Point(683, 87);
            this.btnSelectSourceFolder.Name = "btnSelectSourceFolder";
            this.btnSelectSourceFolder.Size = new System.Drawing.Size(75, 22);
            this.btnSelectSourceFolder.TabIndex = 8;
            this.btnSelectSourceFolder.Text = "Select Source";
            this.btnSelectSourceFolder.Click += new System.EventHandler(this.btnSelectSourceFolder_Click);
            // 
            // lblDestinationFolder
            // 
            this.lblDestinationFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDestinationFolder.AutoSize = true;
            this.lblDestinationFolder.Location = new System.Drawing.Point(3, 118);
            this.lblDestinationFolder.Name = "lblDestinationFolder";
            this.lblDestinationFolder.Size = new System.Drawing.Size(119, 16);
            this.lblDestinationFolder.TabIndex = 9;
            this.lblDestinationFolder.Text = "Destination Folder:";
            // 
            // txtDestinationFolder
            // 
            this.txtDestinationFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestinationFolder.Location = new System.Drawing.Point(173, 115);
            this.txtDestinationFolder.Name = "txtDestinationFolder";
            this.txtDestinationFolder.ReadOnly = true;
            this.txtDestinationFolder.Size = new System.Drawing.Size(504, 22);
            this.txtDestinationFolder.TabIndex = 10;
            // 
            // btnSelectDestinationFolder
            // 
            this.btnSelectDestinationFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnSelectDestinationFolder.Location = new System.Drawing.Point(683, 115);
            this.btnSelectDestinationFolder.Name = "btnSelectDestinationFolder";
            this.btnSelectDestinationFolder.Size = new System.Drawing.Size(75, 22);
            this.btnSelectDestinationFolder.TabIndex = 11;
            this.btnSelectDestinationFolder.Text = "Select Destination";
            this.btnSelectDestinationFolder.Click += new System.EventHandler(this.btnSelectDestinationFolder_Click);
            // 
            // lblModel
            // 
            this.lblModel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblModel.AutoSize = true;
            this.lblModel.Location = new System.Drawing.Point(3, 146);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(48, 16);
            this.lblModel.TabIndex = 12;
            this.lblModel.Text = "Model:";
            // 
            // cmbModelSelection
            // 
            this.cmbModelSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.cmbModelSelection, 2);
            this.cmbModelSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModelSelection.Location = new System.Drawing.Point(173, 143);
            this.cmbModelSelection.Name = "cmbModelSelection";
            this.cmbModelSelection.Size = new System.Drawing.Size(674, 24);
            this.cmbModelSelection.TabIndex = 13;
            // 
            // chkRenameFiles
            // 
            this.chkRenameFiles.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkRenameFiles.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.chkRenameFiles, 3);
            this.chkRenameFiles.Location = new System.Drawing.Point(3, 171);
            this.chkRenameFiles.Name = "chkRenameFiles";
            this.chkRenameFiles.Size = new System.Drawing.Size(213, 19);
            this.chkRenameFiles.TabIndex = 14;
            this.chkRenameFiles.Text = "Bestandsnamen AI hernoemen";
            // 
            // btnStartOrganization
            // 
            this.btnStartOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartOrganization.Location = new System.Drawing.Point(3, 196);
            this.btnStartOrganization.Name = "btnStartOrganization";
            this.btnStartOrganization.Size = new System.Drawing.Size(164, 30);
            this.btnStartOrganization.TabIndex = 15;
            this.btnStartOrganization.Text = "Start Organisatie";
            this.btnStartOrganization.Click += new System.EventHandler(this.btnStartOrganization_Click);
            // 
            // btnStopOrganization
            // 
            this.btnStopOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStopOrganization.Location = new System.Drawing.Point(173, 196);
            this.btnStopOrganization.Name = "btnStopOrganization";
            this.btnStopOrganization.Size = new System.Drawing.Size(504, 30);
            this.btnStopOrganization.TabIndex = 16;
            this.btnStopOrganization.Text = "Stop Organisatie";
            this.btnStopOrganization.Click += new System.EventHandler(this.btnStopOrganization_Click);
            // 
            // btnSaveLog
            // 
            this.btnSaveLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveLog.Location = new System.Drawing.Point(683, 196);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new System.Drawing.Size(164, 30);
            this.btnSaveLog.TabIndex = 17;
            this.btnSaveLog.Text = "Log Opslaan";
            this.btnSaveLog.Click += new System.EventHandler(this.btnSaveLog_Click);
            // 
            // btnRenameSingleFile
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.btnRenameSingleFile, 2);
            this.btnRenameSingleFile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRenameSingleFile.Location = new System.Drawing.Point(3, 232);
            this.btnRenameSingleFile.Name = "btnRenameSingleFile";
            this.btnRenameSingleFile.Size = new System.Drawing.Size(674, 30);
            this.btnRenameSingleFile.TabIndex = 18;
            this.btnRenameSingleFile.Text = "Hernoem Enkel Bestand met AI";
            this.btnRenameSingleFile.UseVisualStyleBackColor = true;
            this.btnRenameSingleFile.Click += new System.EventHandler(this.btnRenameSingleFile_Click);
            // 
            // btnGenerateStandardFolders
            // 
            this.btnGenerateStandardFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGenerateStandardFolders.Location = new System.Drawing.Point(683, 232);
            this.btnGenerateStandardFolders.Name = "btnGenerateStandardFolders";
            this.btnGenerateStandardFolders.Size = new System.Drawing.Size(164, 30);
            this.btnGenerateStandardFolders.TabIndex = 23;
            this.btnGenerateStandardFolders.Text = "Standaardfolderstructuur";
            this.btnGenerateStandardFolders.UseVisualStyleBackColor = true;
            this.btnGenerateStandardFolders.Click += new System.EventHandler(this.btnGenerateStandardFolders_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.progressBar1, 2);
            this.progressBar1.Location = new System.Drawing.Point(3, 268);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(674, 16);
            this.progressBar1.TabIndex = 19;
            this.progressBar1.Visible = false;
            // 
            // lblTokensUsed
            // 
            this.lblTokensUsed.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblTokensUsed.AutoSize = true;
            this.lblTokensUsed.Location = new System.Drawing.Point(730, 268);
            this.lblTokensUsed.Name = "lblTokensUsed";
            this.lblTokensUsed.Size = new System.Drawing.Size(117, 16);
            this.lblTokensUsed.TabIndex = 20;
            this.lblTokensUsed.Text = "Tokens gebruikt: 0";
            // 
            // rtbLog
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.rtbLog, 3);
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.Location = new System.Drawing.Point(3, 290);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(844, 159);
            this.rtbLog.TabIndex = 21;
            this.rtbLog.Text = "";
            // 
            // linkLabelAuthor
            // 
            this.linkLabelAuthor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabelAuthor.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.linkLabelAuthor, 3);
            this.linkLabelAuthor.Location = new System.Drawing.Point(682, 462);
            this.linkLabelAuthor.Name = "linkLabelAuthor";
            this.linkLabelAuthor.Size = new System.Drawing.Size(165, 16);
            this.linkLabelAuthor.TabIndex = 22;
            this.linkLabelAuthor.TabStop = true;
            this.linkLabelAuthor.Text = "Made by Remsey Mailjard";
            this.linkLabelAuthor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.linkLabelAuthor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelAuthor_LinkClicked);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(850, 480);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Form1";
            this.Text = "AI File Organizer - Remsey Mailjard";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lblProvider;
        private System.Windows.Forms.ComboBox cmbProviderSelection;
        private System.Windows.Forms.Label lblApiKey;
        private System.Windows.Forms.TextBox txtApiKey;
        private System.Windows.Forms.Label lblAzureEndpoint;
        private System.Windows.Forms.TextBox txtAzureEndpoint;
        private System.Windows.Forms.Label lblSourceFolder;
        private System.Windows.Forms.TextBox txtSourceFolder;
        private System.Windows.Forms.Button btnSelectSourceFolder;
        private System.Windows.Forms.Label lblDestinationFolder;
        private System.Windows.Forms.TextBox txtDestinationFolder;
        private System.Windows.Forms.Button btnSelectDestinationFolder;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.ComboBox cmbModelSelection;
        private System.Windows.Forms.CheckBox chkRenameFiles;
        private System.Windows.Forms.Button btnStartOrganization;
        private System.Windows.Forms.Button btnStopOrganization;
        private System.Windows.Forms.Button btnSaveLog;
        private System.Windows.Forms.Button btnRenameSingleFile;
        private System.Windows.Forms.Button btnGenerateStandardFolders; // TOEGEVOEGD!
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblTokensUsed;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.LinkLabel linkLabelAuthor;
    }
}
