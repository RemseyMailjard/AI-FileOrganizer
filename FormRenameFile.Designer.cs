using System.Linq; // Although not strictly needed for designer code, it was in your original.

namespace AI_FileOrganizer
{
    partial class FormRenameFile
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
            this.lblOriginalFileName = new System.Windows.Forms.Label();
            this.txtSuggestedFileName = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnSkip = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // lblOriginalFileName
            //
            this.lblOriginalFileName.AutoSize = true;
            this.lblOriginalFileName.Location = new System.Drawing.Point(145, 23);
            this.lblOriginalFileName.Name = "lblOriginalFileName";
            this.lblOriginalFileName.Size = new System.Drawing.Size(150, 16);
            this.lblOriginalFileName.TabIndex = 0;
            this.lblOriginalFileName.Text = "[Oorspronkelijke Naam]";
            //
            // txtSuggestedFileName
            //
            this.txtSuggestedFileName.Location = new System.Drawing.Point(148, 62);
            this.txtSuggestedFileName.Name = "txtSuggestedFileName";
            this.txtSuggestedFileName.Size = new System.Drawing.Size(300, 22);
            this.txtSuggestedFileName.TabIndex = 1;
            //
            // btnSave
            //
            this.btnSave.Location = new System.Drawing.Point(148, 105);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(95, 30);
            this.btnSave.TabIndex = 2;
            this.btnSave.Text = "Opslaan";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            //
            // btnSkip
            //
            this.btnSkip.Location = new System.Drawing.Point(249, 105);
            this.btnSkip.Name = "btnSkip";
            this.btnSkip.Size = new System.Drawing.Size(95, 30);
            this.btnSkip.TabIndex = 3;
            this.btnSkip.Text = "Overslaan";
            this.btnSkip.UseVisualStyleBackColor = true;
            this.btnSkip.Click += new System.EventHandler(this.btnSkip_Click);
            //
            // btnCancel
            //
            this.btnCancel.Location = new System.Drawing.Point(350, 105);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(98, 30);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Annuleren";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 16);
            this.label1.TabIndex = 5;
            this.label1.Text = "Originele naam:";
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(129, 16);
            this.label2.TabIndex = 6;
            this.label2.Text = "Voorgestelde naam:";
            //
            // FormRenameFile
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(477, 158);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSkip);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtSuggestedFileName);
            this.Controls.Add(this.lblOriginalFileName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormRenameFile";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Bestandsnaam Aanpassen";
            this.Load += new System.EventHandler(this.FormRenameFile_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblOriginalFileName;
        private System.Windows.Forms.TextBox txtSuggestedFileName;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnSkip;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}