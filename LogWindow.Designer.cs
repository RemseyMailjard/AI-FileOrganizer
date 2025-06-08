namespace AI_FileOrganizer
{
    partial class LogWindow
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox tbLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tbLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // tbLog
            // 
            this.tbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbLog.Location = new System.Drawing.Point(0, 0);
            this.tbLog.Multiline = true;
            this.tbLog.ReadOnly = true;
            this.tbLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbLog.Name = "tbLog";
            this.tbLog.Size = new System.Drawing.Size(800, 450);
            this.tbLog.TabIndex = 0;
            this.tbLog.DoubleClick += new System.EventHandler(this.tbLog_DoubleClick);
            // 
            // LogWindow
            // 
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tbLog);
            this.Name = "LogWindow";
            this.Text = "Volledig logoverzicht";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
