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
            this.lblApiKey = new System.Windows.Forms.Label();
            this.txtApiKey = new System.Windows.Forms.TextBox();
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
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblTokensUsed = new System.Windows.Forms.Label();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.linkLabelAuthor = new System.Windows.Forms.LinkLabel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblApiKey
            // 
            this.lblApiKey.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(2, 3);
            this.lblApiKey.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(85, 13);
            this.lblApiKey.TabIndex = 0;
            this.lblApiKey.Text = "Google API Key:";
            // 
            // txtApiKey
            // 
            this.txtApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.txtApiKey, 2);
            this.txtApiKey.Location = new System.Drawing.Point(100, 2);
            this.txtApiKey.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(476, 20);
            this.txtApiKey.TabIndex = 1;
            this.txtApiKey.UseSystemPasswordChar = true;
            // 
            // lblSourceFolder
            // 
            this.lblSourceFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSourceFolder.AutoSize = true;
            this.lblSourceFolder.Location = new System.Drawing.Point(2, 25);
            this.lblSourceFolder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblSourceFolder.Name = "lblSourceFolder";
            this.lblSourceFolder.Size = new System.Drawing.Size(76, 13);
            this.lblSourceFolder.TabIndex = 2;
            this.lblSourceFolder.Text = "Source Folder:";
            // 
            // txtSourceFolder
            // 
            this.txtSourceFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSourceFolder.Location = new System.Drawing.Point(100, 22);
            this.txtSourceFolder.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtSourceFolder.Name = "txtSourceFolder";
            this.txtSourceFolder.ReadOnly = true;
            this.txtSourceFolder.Size = new System.Drawing.Size(397, 20);
            this.txtSourceFolder.TabIndex = 3;
            // 
            // btnSelectSourceFolder
            // 
            this.btnSelectSourceFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSelectSourceFolder.Location = new System.Drawing.Point(501, 22);
            this.btnSelectSourceFolder.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnSelectSourceFolder.Name = "btnSelectSourceFolder";
            this.btnSelectSourceFolder.Size = new System.Drawing.Size(75, 19);
            this.btnSelectSourceFolder.TabIndex = 4;
            this.btnSelectSourceFolder.Text = "Select Source";
            this.btnSelectSourceFolder.UseVisualStyleBackColor = true;
            this.btnSelectSourceFolder.Click += new System.EventHandler(this.btnSelectSourceFolder_Click);
            // 
            // lblDestinationFolder
            // 
            this.lblDestinationFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDestinationFolder.AutoSize = true;
            this.lblDestinationFolder.Location = new System.Drawing.Point(2, 43);
            this.lblDestinationFolder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblDestinationFolder.Name = "lblDestinationFolder";
            this.lblDestinationFolder.Size = new System.Drawing.Size(63, 23);
            this.lblDestinationFolder.TabIndex = 5;
            this.lblDestinationFolder.Text = "Destination Folder:";
            // 
            // txtDestinationFolder
            // 
            this.txtDestinationFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestinationFolder.Location = new System.Drawing.Point(100, 45);
            this.txtDestinationFolder.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.txtDestinationFolder.Name = "txtDestinationFolder";
            this.txtDestinationFolder.ReadOnly = true;
            this.txtDestinationFolder.Size = new System.Drawing.Size(397, 20);
            this.txtDestinationFolder.TabIndex = 6;
            // 
            // btnSelectDestinationFolder
            // 
            this.btnSelectDestinationFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSelectDestinationFolder.Location = new System.Drawing.Point(501, 45);
            this.btnSelectDestinationFolder.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnSelectDestinationFolder.Name = "btnSelectDestinationFolder";
            this.btnSelectDestinationFolder.Size = new System.Drawing.Size(75, 19);
            this.btnSelectDestinationFolder.TabIndex = 7;
            this.btnSelectDestinationFolder.Text = "Select Destination";
            this.btnSelectDestinationFolder.UseVisualStyleBackColor = true;
            this.btnSelectDestinationFolder.Click += new System.EventHandler(this.btnSelectDestinationFolder_Click);
            // 
            // lblModel
            // 
            this.lblModel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblModel.AutoSize = true;
            this.lblModel.Location = new System.Drawing.Point(2, 71);
            this.lblModel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(39, 13);
            this.lblModel.TabIndex = 8;
            this.lblModel.Text = "Model:";
            // 
            // cmbModelSelection
            // 
            this.cmbModelSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.cmbModelSelection, 2);
            this.cmbModelSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModelSelection.FormattingEnabled = true;
            this.cmbModelSelection.Location = new System.Drawing.Point(100, 68);
            this.cmbModelSelection.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.cmbModelSelection.Name = "cmbModelSelection";
            this.cmbModelSelection.Size = new System.Drawing.Size(476, 21);
            this.cmbModelSelection.TabIndex = 9;
            // 
            // chkRenameFiles
            // 
            this.chkRenameFiles.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkRenameFiles.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.chkRenameFiles, 3);
            this.chkRenameFiles.Location = new System.Drawing.Point(2, 91);
            this.chkRenameFiles.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.chkRenameFiles.Name = "chkRenameFiles";
            this.chkRenameFiles.Size = new System.Drawing.Size(171, 16);
            this.chkRenameFiles.TabIndex = 10;
            this.chkRenameFiles.Text = "Bestandsnamen AI hernoemen";
            this.chkRenameFiles.UseVisualStyleBackColor = true;
            // 
            // btnStartOrganization
            // 
            this.btnStartOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartOrganization.Location = new System.Drawing.Point(2, 111);
            this.btnStartOrganization.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnStartOrganization.Name = "btnStartOrganization";
            this.btnStartOrganization.Size = new System.Drawing.Size(94, 24);
            this.btnStartOrganization.TabIndex = 11;
            this.btnStartOrganization.Text = "Start";
            this.btnStartOrganization.UseVisualStyleBackColor = true;
            this.btnStartOrganization.Click += new System.EventHandler(this.btnStartOrganization_Click);
            // 
            // btnStopOrganization
            // 
            this.btnStopOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStopOrganization.Location = new System.Drawing.Point(100, 111);
            this.btnStopOrganization.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnStopOrganization.Name = "btnStopOrganization";
            this.btnStopOrganization.Size = new System.Drawing.Size(397, 24);
            this.btnStopOrganization.TabIndex = 15;
            this.btnStopOrganization.Text = "Stop";
            this.btnStopOrganization.UseVisualStyleBackColor = true;
            this.btnStopOrganization.Click += new System.EventHandler(this.btnStopOrganization_Click);
            // 
            // btnSaveLog
            // 
            this.btnSaveLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveLog.Location = new System.Drawing.Point(501, 111);
            this.btnSaveLog.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new System.Drawing.Size(75, 24);
            this.btnSaveLog.TabIndex = 16;
            this.btnSaveLog.Text = "Log Opslaan";
            this.btnSaveLog.UseVisualStyleBackColor = true;
            this.btnSaveLog.Click += new System.EventHandler(this.btnSaveLog_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.progressBar1, 2);
            this.progressBar1.Location = new System.Drawing.Point(2, 139);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(495, 15);
            this.progressBar1.TabIndex = 12;
            this.progressBar1.Visible = false;
            // 
            // lblTokensUsed
            // 
            this.lblTokensUsed.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblTokensUsed.AutoSize = true;
            this.lblTokensUsed.Location = new System.Drawing.Point(519, 137);
            this.lblTokensUsed.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblTokensUsed.Name = "lblTokensUsed";
            this.lblTokensUsed.Size = new System.Drawing.Size(57, 20);
            this.lblTokensUsed.TabIndex = 13;
            this.lblTokensUsed.Text = "Tokens gebruikt: 0";
            this.lblTokensUsed.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // rtbLog
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.rtbLog, 3);
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.Location = new System.Drawing.Point(2, 159);
            this.rtbLog.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(574, 168);
            this.rtbLog.TabIndex = 14;
            this.rtbLog.Text = "";
            // 
            // linkLabelAuthor
            // 
            this.linkLabelAuthor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabelAuthor.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.linkLabelAuthor, 3);
            this.linkLabelAuthor.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.linkLabelAuthor.Location = new System.Drawing.Point(387, 329);
            this.linkLabelAuthor.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.linkLabelAuthor.Name = "linkLabelAuthor";
            this.linkLabelAuthor.Size = new System.Drawing.Size(189, 20);
            this.linkLabelAuthor.TabIndex = 17;
            this.linkLabelAuthor.TabStop = true;
            this.linkLabelAuthor.Text = "Made by Remsey Mailjard";
            this.linkLabelAuthor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.linkLabelAuthor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelAuthor_LinkClicked);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 17.01428F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 69.45899F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13.52673F));
            this.tableLayoutPanel1.Controls.Add(this.lblApiKey, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtApiKey, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.lblSourceFolder, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtSourceFolder, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.btnSelectSourceFolder, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblDestinationFolder, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.txtDestinationFolder, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.btnSelectDestinationFolder, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.lblModel, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.cmbModelSelection, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.chkRenameFiles, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.btnStartOrganization, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.btnStopOrganization, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.btnSaveLog, 2, 5);
            this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.lblTokensUsed, 2, 6);
            this.tableLayoutPanel1.Controls.Add(this.rtbLog, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.linkLabelAuthor, 0, 8);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 9;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(578, 349);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 349);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form1";
            this.Text = "AI File Organizer - Remsey Mailjard";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblApiKey;
        private System.Windows.Forms.TextBox txtApiKey;
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
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblTokensUsed;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.LinkLabel linkLabelAuthor;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}