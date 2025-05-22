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
            this.btnStartOrganization = new System.Windows.Forms.Button();
            this.btnStopOrganization = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblTokensUsed = new System.Windows.Forms.Label();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.linkLabelAuthor = new System.Windows.Forms.LinkLabel(); // NIEUW
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblApiKey
            // 
            this.lblApiKey.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(3, 8);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(91, 16);
            this.lblApiKey.TabIndex = 0;
            this.lblApiKey.Text = "Google API Key:";
            // 
            // txtApiKey
            // 
            this.txtApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.txtApiKey, 2);
            this.txtApiKey.Location = new System.Drawing.Point(100, 5);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(667, 22);
            this.txtApiKey.TabIndex = 1;
            this.txtApiKey.UseSystemPasswordChar = true;
            // 
            // lblSourceFolder
            // 
            this.lblSourceFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblSourceFolder.AutoSize = true;
            this.lblSourceFolder.Location = new System.Drawing.Point(3, 39);
            this.lblSourceFolder.Name = "lblSourceFolder";
            this.lblSourceFolder.Size = new System.Drawing.Size(91, 16);
            this.lblSourceFolder.TabIndex = 2;
            this.lblSourceFolder.Text = "Source Folder:";
            // 
            // txtSourceFolder
            // 
            this.txtSourceFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSourceFolder.Location = new System.Drawing.Point(100, 36);
            this.txtSourceFolder.Name = "txtSourceFolder";
            this.txtSourceFolder.ReadOnly = true;
            this.txtSourceFolder.Size = new System.Drawing.Size(567, 22);
            this.txtSourceFolder.TabIndex = 3;
            // 
            // btnSelectSourceFolder
            // 
            this.btnSelectSourceFolder.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnSelectSourceFolder.Location = new System.Drawing.Point(673, 33);
            this.btnSelectSourceFolder.Name = "btnSelectSourceFolder";
            this.btnSelectSourceFolder.Size = new System.Drawing.Size(94, 29);
            this.btnSelectSourceFolder.TabIndex = 4;
            this.btnSelectSourceFolder.Text = "Select Source";
            this.btnSelectSourceFolder.UseVisualStyleBackColor = true;
            this.btnSelectSourceFolder.Click += new System.EventHandler(this.btnSelectSourceFolder_Click);
            // 
            // lblDestinationFolder
            // 
            this.lblDestinationFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDestinationFolder.AutoSize = true;
            this.lblDestinationFolder.Location = new System.Drawing.Point(3, 70);
            this.lblDestinationFolder.Name = "lblDestinationFolder";
            this.lblDestinationFolder.Size = new System.Drawing.Size(117, 16);
            this.lblDestinationFolder.TabIndex = 5;
            this.lblDestinationFolder.Text = "Destination Folder:";
            // 
            // txtDestinationFolder
            // 
            this.txtDestinationFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestinationFolder.Location = new System.Drawing.Point(100, 67);
            this.txtDestinationFolder.Name = "txtDestinationFolder";
            this.txtDestinationFolder.ReadOnly = true;
            this.txtDestinationFolder.Size = new System.Drawing.Size(567, 22);
            this.txtDestinationFolder.TabIndex = 6;
            // 
            // btnSelectDestinationFolder
            // 
            this.btnSelectDestinationFolder.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnSelectDestinationFolder.Location = new System.Drawing.Point(673, 64);
            this.btnSelectDestinationFolder.Name = "btnSelectDestinationFolder";
            this.btnSelectDestinationFolder.Size = new System.Drawing.Size(94, 29);
            this.btnSelectDestinationFolder.TabIndex = 7;
            this.btnSelectDestinationFolder.Text = "Select Destination";
            this.btnSelectDestinationFolder.UseVisualStyleBackColor = true;
            this.btnSelectDestinationFolder.Click += new System.EventHandler(this.btnSelectDestinationFolder_Click);
            // 
            // lblModel
            // 
            this.lblModel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblModel.AutoSize = true;
            this.lblModel.Location = new System.Drawing.Point(3, 101);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(48, 16);
            this.lblModel.TabIndex = 8;
            this.lblModel.Text = "Model:";
            // 
            // cmbModelSelection
            // 
            this.cmbModelSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.cmbModelSelection, 2);
            this.cmbModelSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbModelSelection.FormattingEnabled = true;
            this.cmbModelSelection.Location = new System.Drawing.Point(100, 99);
            this.cmbModelSelection.Name = "cmbModelSelection";
            this.cmbModelSelection.Size = new System.Drawing.Size(667, 24);
            this.cmbModelSelection.TabIndex = 9;
            // 
            // btnStartOrganization
            // 
            this.btnStartOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStartOrganization.Location = new System.Drawing.Point(3, 128);
            this.btnStartOrganization.Name = "btnStartOrganization";
            this.btnStartOrganization.Size = new System.Drawing.Size(91, 30);
            this.btnStartOrganization.TabIndex = 10;
            this.btnStartOrganization.Text = "Start";
            this.btnStartOrganization.UseVisualStyleBackColor = true;
            this.btnStartOrganization.Click += new System.EventHandler(this.btnStartOrganization_Click);
            // 
            // btnStopOrganization
            // 
            this.btnStopOrganization.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStopOrganization.Location = new System.Drawing.Point(100, 128);
            this.btnStopOrganization.Name = "btnStopOrganization";
            this.btnStopOrganization.Size = new System.Drawing.Size(567, 30);
            this.btnStopOrganization.TabIndex = 14;
            this.btnStopOrganization.Text = "Stop";
            this.btnStopOrganization.UseVisualStyleBackColor = true;
            this.btnStopOrganization.Click += new System.EventHandler(this.btnStopOrganization_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.progressBar1, 2);
            this.progressBar1.Location = new System.Drawing.Point(3, 162);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(664, 19);
            this.progressBar1.TabIndex = 11;
            this.progressBar1.Visible = false;
            // 
            // lblTokensUsed
            // 
            this.lblTokensUsed.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblTokensUsed.AutoSize = true;
            this.lblTokensUsed.Location = new System.Drawing.Point(673, 163);
            this.lblTokensUsed.Name = "lblTokensUsed";
            this.lblTokensUsed.Size = new System.Drawing.Size(100, 16);
            this.lblTokensUsed.TabIndex = 12;
            this.lblTokensUsed.Text = "Tokens gebruikt: 0";
            this.lblTokensUsed.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // rtbLog
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.rtbLog, 3);
            this.rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLog.Location = new System.Drawing.Point(3, 187);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(764, 215); // Aangepaste hoogte
            this.rtbLog.TabIndex = 13;
            this.rtbLog.Text = "";
            // 
            // linkLabelAuthor
            // 
            this.linkLabelAuthor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabelAuthor.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.linkLabelAuthor, 3);
            this.linkLabelAuthor.Location = new System.Drawing.Point(601, 409); // Pas positie aan indien nodig
            this.linkLabelAuthor.Name = "linkLabelAuthor";
            this.linkLabelAuthor.Size = new System.Drawing.Size(166, 16);
            this.linkLabelAuthor.TabIndex = 15;
            this.linkLabelAuthor.TabStop = true;
            this.linkLabelAuthor.Text = "Gemaakt door Remsey Mailjard";
            linkLabelAuthor.Links.Add(13, 17, "https://www.linkedin.com/in/remseymailjard/");
            this.linkLabelAuthor.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.linkLabelAuthor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelAuthor_LinkClicked);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 13F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 72F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
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
            this.tableLayoutPanel1.Controls.Add(this.btnStartOrganization, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.btnStopOrganization, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.lblTokensUsed, 2, 5);
            this.tableLayoutPanel1.Controls.Add(this.rtbLog, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.linkLabelAuthor, 0, 7); // NIEUW: LinkLabel in laatste rij
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 8; // Aangepast: nu 8 rijen
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 33F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F)); // NIEUW: Rij voor LinkLabel
            this.tableLayoutPanel1.Size = new System.Drawing.Size(770, 430);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(770, 430);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Form1";
            this.Text = "AI File Organizer";
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
        private System.Windows.Forms.Button btnStartOrganization;
        private System.Windows.Forms.Button btnStopOrganization;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblTokensUsed;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.LinkLabel linkLabelAuthor; // NIEUW
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}