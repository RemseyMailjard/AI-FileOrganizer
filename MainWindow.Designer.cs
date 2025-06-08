namespace AI_FileOrganizer
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
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
            this.rootTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPageConfig = new System.Windows.Forms.TabPage();
            this.tlpConfig = new System.Windows.Forms.TableLayoutPanel();
            this.tabPageOrganizeLog = new System.Windows.Forms.TabPage();
            this.tlpOrganizeLog = new System.Windows.Forms.TableLayoutPanel();
            this.btnSuggestSubfolders = new System.Windows.Forms.Button();
            this.rootTableLayoutPanel.SuspendLayout();
            this.tabControlMain.SuspendLayout();
            this.tabPageConfig.SuspendLayout();
            this.tlpConfig.SuspendLayout();
            this.tabPageOrganizeLog.SuspendLayout();
            this.tlpOrganizeLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblProvider
            // 
            this.lblProvider.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblProvider.AutoSize = true;
            this.lblProvider.Location = new System.Drawing.Point(3, 7);
            this.lblProvider.Name = "lblProvider";
            this.lblProvider.Size = new System.Drawing.Size(61, 16);
            this.lblProvider.TabIndex = 0;
            this.lblProvider.Text = "Provider:";
            // 
            // cmbProviderSelection
            // 
            this.cmbProviderSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpConfig.SetColumnSpan(this.cmbProviderSelection, 2);
            this.cmbProviderSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProviderSelection.FormattingEnabled = true;
            this.cmbProviderSelection.Items.AddRange(new object[] {
            "Gemini (Google)",
            "OpenAI (openai.com)",
            "Azure OpenAI",
            "Lokaal ONNX-model"});
            this.cmbProviderSelection.Location = new System.Drawing.Point(153, 3);
            this.cmbProviderSelection.Name = "cmbProviderSelection";
            this.cmbProviderSelection.Size = new System.Drawing.Size(918, 24);
            this.cmbProviderSelection.TabIndex = 1;
            this.cmbProviderSelection.SelectedIndexChanged += new System.EventHandler(this.cmbProviderSelection_SelectedIndexChanged);
            // 
            // lblApiKey
            // 
            this.lblApiKey.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(3, 37);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(57, 16);
            this.lblApiKey.TabIndex = 2;
            this.lblApiKey.Text = "API Key:";
            // 
            // txtApiKey
            // 
            this.txtApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpConfig.SetColumnSpan(this.txtApiKey, 2);
            this.txtApiKey.Location = new System.Drawing.Point(153, 34);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(918, 22);
            this.txtApiKey.TabIndex = 3;
            this.txtApiKey.UseSystemPasswordChar = true;
            // 
            // lblAzureEndpoint
            // 
            this.lblAzureEndpoint.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblAzureEndpoint.AutoSize = true;
            this.lblAzureEndpoint.Location = new System.Drawing.Point(3, 67);
            this.lblAzureEndpoint.Name = "lblAzureEndpoint";
            this.lblAzureEndpoint.Size = new System.Drawing.Size(100, 16);
            this.lblAzureEndpoint.TabIndex = 4;
            this.lblAzureEndpoint.Text = "Azure Endpoint:";
            this.lblAzureEndpoint.Visible = false;
            // 
            // txtAzureEndpoint
            // 
            this.txtAzureEndpoint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpConfig.SetColumnSpan(this.txtAzureEndpoint, 2);
            this.txtAzureEndpoint.Location = new System.Drawing.Point(153, 64);
            this.txtAzureEndpoint.Name = "txtAzureEndpoint";
            this.txtAzureEndpoint.Size = new System.Drawing.Size(918, 22);
            this.txtAzureEndpoint.TabIndex = 5;
            this.txtAzureEndpoint.Visible = false;
            // 
            // lblSourceFolder
            // 
            this.lblSourceFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSourceFolder.AutoSize = true;
            this.lblSourceFolder.Location = new System.Drawing.Point(3, 7);
            this.lblSourceFolder.Name = "lblSourceFolder";
            this.lblSourceFolder.Size = new System.Drawing.Size(95, 16);
            this.lblSourceFolder.TabIndex = 6;
            this.lblSourceFolder.Text = "Source Folder:";
            // 
            // txtSourceFolder
            // 
            this.txtSourceFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSourceFolder.Location = new System.Drawing.Point(153, 4);
            this.txtSourceFolder.Name = "txtSourceFolder";
            this.txtSourceFolder.Size = new System.Drawing.Size(788, 22);
            this.txtSourceFolder.TabIndex = 7;
            // 
            // btnSelectSourceFolder
            // 
            this.btnSelectSourceFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectSourceFolder.Location = new System.Drawing.Point(947, 3);
            this.btnSelectSourceFolder.Name = "btnSelectSourceFolder";
            this.btnSelectSourceFolder.Size = new System.Drawing.Size(124, 23);
            this.btnSelectSourceFolder.TabIndex = 8;
            this.btnSelectSourceFolder.Text = "Select Source";
            this.btnSelectSourceFolder.UseVisualStyleBackColor = true;
            this.btnSelectSourceFolder.Click += new System.EventHandler(this.btnSelectSourceFolder_Click);
            // 
            // lblDestinationFolder
            // 
            this.lblDestinationFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDestinationFolder.AutoSize = true;
            this.lblDestinationFolder.Location = new System.Drawing.Point(3, 37);
            this.lblDestinationFolder.Name = "lblDestinationFolder";
            this.lblDestinationFolder.Size = new System.Drawing.Size(119, 16);
            this.lblDestinationFolder.TabIndex = 9;
            this.lblDestinationFolder.Text = "Destination Folder:";
            // 
            // txtDestinationFolder
            // 
            this.txtDestinationFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestinationFolder.Location = new System.Drawing.Point(153, 34);
            this.txtDestinationFolder.Name = "txtDestinationFolder";
            this.txtDestinationFolder.Size = new System.Drawing.Size(788, 22);
            this.txtDestinationFolder.TabIndex = 10;
            // 
            // btnSelectDestinationFolder
            // 
            this.btnSelectDestinationFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectDestinationFolder.Location = new System.Drawing.Point(947, 33);
            this.btnSelectDestinationFolder.Name = "btnSelectDestinationFolder";
            this.btnSelectDestinationFolder.Size = new System.Drawing.Size(124, 23);
            this.btnSelectDestinationFolder.TabIndex = 11;
            this.btnSelectDestinationFolder.Text = "Select Destination";
            this.btnSelectDestinationFolder.UseVisualStyleBackColor = true;
            this.btnSelectDestinationFolder.Click += new System.EventHandler(this.btnSelectDestinationFolder_Click);
            // 
            // lblModel
            // 
            this.lblModel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblModel.AutoSize = true;
            this.lblModel.Location = new System.Drawing.Point(3, 97);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(48, 16);
            this.lblModel.TabIndex = 12;
            this.lblModel.Text = "Model:";
            // 
            // cmbModelSelection
            // 
            this.cmbModelSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpConfig.SetColumnSpan(this.cmbModelSelection, 2);
            this.cmbModelSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModelSelection.FormattingEnabled = true;
            this.cmbModelSelection.Location = new System.Drawing.Point(153, 93);
            this.cmbModelSelection.Name = "cmbModelSelection";
            this.cmbModelSelection.Size = new System.Drawing.Size(918, 24);
            this.cmbModelSelection.TabIndex = 13;
            this.cmbModelSelection.SelectedIndexChanged += new System.EventHandler(this.cmbModelSelection_SelectedIndexChanged);
            // 
            // chkRenameFiles
            // 
            this.chkRenameFiles.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkRenameFiles.AutoSize = true;
            this.tlpOrganizeLog.SetColumnSpan(this.chkRenameFiles, 3);
            this.chkRenameFiles.Location = new System.Drawing.Point(3, 65);
            this.chkRenameFiles.Name = "chkRenameFiles";
            this.chkRenameFiles.Size = new System.Drawing.Size(213, 20);
            this.chkRenameFiles.TabIndex = 14;
            this.chkRenameFiles.Text = "Bestandsnamen AI hernoemen";
            this.chkRenameFiles.UseVisualStyleBackColor = true;
            // 
            // btnStartOrganization
            // 
            this.btnStartOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartOrganization.Location = new System.Drawing.Point(3, 93);
            this.btnStartOrganization.Name = "btnStartOrganization";
            this.btnStartOrganization.Size = new System.Drawing.Size(144, 34);
            this.btnStartOrganization.TabIndex = 15;
            this.btnStartOrganization.Text = "Start Organisatie";
            this.btnStartOrganization.UseVisualStyleBackColor = true;
            this.btnStartOrganization.Click += new System.EventHandler(this.btnStartOrganization_Click);
            // 
            // btnStopOrganization
            // 
            this.btnStopOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStopOrganization.Location = new System.Drawing.Point(153, 93);
            this.btnStopOrganization.Name = "btnStopOrganization";
            this.btnStopOrganization.Size = new System.Drawing.Size(788, 34);
            this.btnStopOrganization.TabIndex = 16;
            this.btnStopOrganization.Text = "Stop Organisatie";
            this.btnStopOrganization.UseVisualStyleBackColor = true;
            this.btnStopOrganization.Click += new System.EventHandler(this.btnStopOrganization_Click);
            // 
            // btnSaveLog
            // 
            this.btnSaveLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveLog.Location = new System.Drawing.Point(947, 93);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new System.Drawing.Size(124, 34);
            this.btnSaveLog.TabIndex = 17;
            this.btnSaveLog.Text = "Log Opslaan";
            this.btnSaveLog.UseVisualStyleBackColor = true;
            this.btnSaveLog.Click += new System.EventHandler(this.btnSaveLog_Click);
            // 
            // btnRenameSingleFile
            // 
            this.tlpOrganizeLog.SetColumnSpan(this.btnRenameSingleFile, 2);
            this.btnRenameSingleFile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRenameSingleFile.Location = new System.Drawing.Point(3, 133);
            this.btnRenameSingleFile.Name = "btnRenameSingleFile";
            this.btnRenameSingleFile.Size = new System.Drawing.Size(938, 34);
            this.btnRenameSingleFile.TabIndex = 18;
            this.btnRenameSingleFile.Text = "Bestandsnaam hernoemen van 1 bestand";
            this.btnRenameSingleFile.UseVisualStyleBackColor = true;
            this.btnRenameSingleFile.Click += new System.EventHandler(this.btnRenameSingleFile_Click);
            // 
            // btnGenerateStandardFolders
            // 
            this.btnGenerateStandardFolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnGenerateStandardFolders.Location = new System.Drawing.Point(947, 133);
            this.btnGenerateStandardFolders.Name = "btnGenerateStandardFolders";
            this.btnGenerateStandardFolders.Size = new System.Drawing.Size(124, 34);
            this.btnGenerateStandardFolders.TabIndex = 23;
            this.btnGenerateStandardFolders.Text = "Maak Standaard Mappen";
            this.btnGenerateStandardFolders.UseVisualStyleBackColor = true;
            this.btnGenerateStandardFolders.Click += new System.EventHandler(this.btnGenerateStandardFolders_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpOrganizeLog.SetColumnSpan(this.progressBar1, 2);
            this.progressBar1.Location = new System.Drawing.Point(3, 214);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(938, 16);
            this.progressBar1.TabIndex = 19;
            this.progressBar1.Visible = false;
            // 
            // lblTokensUsed
            // 
            this.lblTokensUsed.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblTokensUsed.AutoSize = true;
            this.lblTokensUsed.Location = new System.Drawing.Point(954, 214);
            this.lblTokensUsed.Name = "lblTokensUsed";
            this.lblTokensUsed.Size = new System.Drawing.Size(117, 16);
            this.lblTokensUsed.TabIndex = 20;
            this.lblTokensUsed.Text = "Tokens gebruikt: 0";
            // 
            // rtbLog
            // 
            this.tlpOrganizeLog.SetColumnSpan(this.rtbLog, 3);
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.Location = new System.Drawing.Point(3, 238);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(1068, 255);
            this.rtbLog.TabIndex = 21;
            this.rtbLog.Text = "";
            // 
            // linkLabelAuthor
            // 
            this.linkLabelAuthor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabelAuthor.AutoSize = true;
            this.linkLabelAuthor.Location = new System.Drawing.Point(926, 547);
            this.linkLabelAuthor.Name = "linkLabelAuthor";
            this.linkLabelAuthor.Size = new System.Drawing.Size(165, 16);
            this.linkLabelAuthor.TabIndex = 22;
            this.linkLabelAuthor.TabStop = true;
            this.linkLabelAuthor.Text = "Made by Remsey Mailjard";
            this.linkLabelAuthor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.linkLabelAuthor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelAuthor_LinkClicked);
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.ColumnCount = 1;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.Controls.Add(this.tabControlMain, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.linkLabelAuthor, 0, 1);
            this.rootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 2;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(1094, 563);
            this.rootTableLayoutPanel.TabIndex = 0;
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabPageConfig);
            this.tabControlMain.Controls.Add(this.tabPageOrganizeLog);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(3, 3);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1088, 531);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabPageConfig
            // 
            this.tabPageConfig.Controls.Add(this.tlpConfig);
            this.tabPageConfig.Location = new System.Drawing.Point(4, 25);
            this.tabPageConfig.Name = "tabPageConfig";
            this.tabPageConfig.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageConfig.Size = new System.Drawing.Size(1080, 502);
            this.tabPageConfig.TabIndex = 0;
            this.tabPageConfig.Text = "Configuratie";
            this.tabPageConfig.UseVisualStyleBackColor = true;
            // 
            // tlpConfig
            // 
            this.tlpConfig.ColumnCount = 3;
            this.tlpConfig.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpConfig.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpConfig.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpConfig.Controls.Add(this.lblProvider, 0, 0);
            this.tlpConfig.Controls.Add(this.cmbProviderSelection, 1, 0);
            this.tlpConfig.Controls.Add(this.lblApiKey, 0, 1);
            this.tlpConfig.Controls.Add(this.txtApiKey, 1, 1);
            this.tlpConfig.Controls.Add(this.lblAzureEndpoint, 0, 2);
            this.tlpConfig.Controls.Add(this.txtAzureEndpoint, 1, 2);
            this.tlpConfig.Controls.Add(this.lblModel, 0, 3);
            this.tlpConfig.Controls.Add(this.cmbModelSelection, 1, 3);
            this.tlpConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpConfig.Location = new System.Drawing.Point(3, 3);
            this.tlpConfig.Name = "tlpConfig";
            this.tlpConfig.RowCount = 5;
            this.tlpConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpConfig.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpConfig.Size = new System.Drawing.Size(1074, 496);
            this.tlpConfig.TabIndex = 0;
            // 
            // tabPageOrganizeLog
            // 
            this.tabPageOrganizeLog.Controls.Add(this.tlpOrganizeLog);
            this.tabPageOrganizeLog.Location = new System.Drawing.Point(4, 25);
            this.tabPageOrganizeLog.Name = "tabPageOrganizeLog";
            this.tabPageOrganizeLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOrganizeLog.Size = new System.Drawing.Size(1080, 502);
            this.tabPageOrganizeLog.TabIndex = 1;
            this.tabPageOrganizeLog.Text = "Bestanden Ordenen & Log";
            this.tabPageOrganizeLog.UseVisualStyleBackColor = true;
            // 
            // tlpOrganizeLog
            // 
            this.tlpOrganizeLog.ColumnCount = 3;
            this.tlpOrganizeLog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlpOrganizeLog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpOrganizeLog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.tlpOrganizeLog.Controls.Add(this.lblSourceFolder, 0, 0);
            this.tlpOrganizeLog.Controls.Add(this.txtSourceFolder, 1, 0);
            this.tlpOrganizeLog.Controls.Add(this.btnSelectSourceFolder, 2, 0);
            this.tlpOrganizeLog.Controls.Add(this.lblDestinationFolder, 0, 1);
            this.tlpOrganizeLog.Controls.Add(this.txtDestinationFolder, 1, 1);
            this.tlpOrganizeLog.Controls.Add(this.btnSelectDestinationFolder, 2, 1);
            this.tlpOrganizeLog.Controls.Add(this.chkRenameFiles, 0, 2);
            this.tlpOrganizeLog.Controls.Add(this.btnStartOrganization, 0, 3);
            this.tlpOrganizeLog.Controls.Add(this.btnStopOrganization, 1, 3);
            this.tlpOrganizeLog.Controls.Add(this.btnSaveLog, 2, 3);
            this.tlpOrganizeLog.Controls.Add(this.btnRenameSingleFile, 0, 4);
            this.tlpOrganizeLog.Controls.Add(this.btnGenerateStandardFolders, 2, 4);
            this.tlpOrganizeLog.Controls.Add(this.btnSuggestSubfolders, 2, 5);
            this.tlpOrganizeLog.Controls.Add(this.progressBar1, 0, 6);
            this.tlpOrganizeLog.Controls.Add(this.lblTokensUsed, 2, 6);
            this.tlpOrganizeLog.Controls.Add(this.rtbLog, 0, 7);
            this.tlpOrganizeLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpOrganizeLog.Location = new System.Drawing.Point(3, 3);
            this.tlpOrganizeLog.Name = "tlpOrganizeLog";
            this.tlpOrganizeLog.RowCount = 8;
            this.tlpOrganizeLog.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpOrganizeLog.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpOrganizeLog.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tlpOrganizeLog.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tlpOrganizeLog.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tlpOrganizeLog.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tlpOrganizeLog.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tlpOrganizeLog.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpOrganizeLog.Size = new System.Drawing.Size(1074, 496);
            this.tlpOrganizeLog.TabIndex = 0;
            // 
            // btnSuggestSubfolders
            // 
            this.btnSuggestSubfolders.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSuggestSubfolders.Location = new System.Drawing.Point(947, 173);
            this.btnSuggestSubfolders.Name = "btnSuggestSubfolders";
            this.btnSuggestSubfolders.Size = new System.Drawing.Size(124, 34);
            this.btnSuggestSubfolders.TabIndex = 24;
            this.btnSuggestSubfolders.Text = "Suggestie Subfolders";
            this.btnSuggestSubfolders.UseVisualStyleBackColor = true;
            this.btnSuggestSubfolders.Click += new System.EventHandler(this.btnSuggestSubfolders_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1094, 563);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainWindow";
            this.Text = "AI File Organizer - Remsey Mailjard";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.rootTableLayoutPanel.PerformLayout();
            this.tabControlMain.ResumeLayout(false);
            this.tabPageConfig.ResumeLayout(false);
            this.tlpConfig.ResumeLayout(false);
            this.tlpConfig.PerformLayout();
            this.tabPageOrganizeLog.ResumeLayout(false);
            this.tlpOrganizeLog.ResumeLayout(false);
            this.tlpOrganizeLog.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

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
        private System.Windows.Forms.Button btnGenerateStandardFolders;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblTokensUsed;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.LinkLabel linkLabelAuthor;

        private System.Windows.Forms.TableLayoutPanel rootTableLayoutPanel;
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPageConfig;
        private System.Windows.Forms.TableLayoutPanel tlpConfig;
        private System.Windows.Forms.TabPage tabPageOrganizeLog;
        private System.Windows.Forms.TableLayoutPanel tlpOrganizeLog;
        private System.Windows.Forms.Button btnSuggestSubfolders; // Nieuwe knop declaratie
    }
}