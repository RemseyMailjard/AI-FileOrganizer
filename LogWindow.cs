using System;
using System.Windows.Forms;

namespace AI_FileOrganizer
{
    public partial class LogWindow : Form
    {
        public LogWindow()
        {
            InitializeComponent();
        }

        public void SetLog(string log)
        {
            this.tbLog.Text = log;
        }

        private void tbLog_DoubleClick(object sender, EventArgs e)
        {
            this.tbLog.SelectAll();
        }
    }
}
