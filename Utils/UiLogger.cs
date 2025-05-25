using System.Windows.Forms;

namespace AI_FileOrganizer.Utils
{
    /// <summary>
    /// Logger die logt naar een RichTextBox in een WinForms UI.
    /// </summary>
    public class UiLogger : ILogger
    {
        private readonly RichTextBox _logBox;

        public UiLogger(RichTextBox logBox)
        {
            _logBox = logBox;
        }

        public void Log(string message)
        {
            // Zorg dat je op de juiste thread zit voor UI-updates
            if (_logBox.InvokeRequired)
            {
                _logBox.BeginInvoke(new MethodInvoker(() => Log(message)));
            }
            else
            {
                _logBox.AppendText(message + System.Environment.NewLine);
                _logBox.ScrollToCaret();
            }
        }
    }
}
