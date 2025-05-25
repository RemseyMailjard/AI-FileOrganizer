using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace PersoonlijkeMappenGenerator
{
    public static class BatchOrExeRunner
    {
        /// <summary>
        /// Start een batchbestand of exe-bestand met optionele command-line argumenten.
        /// </summary>
        /// <param name="filePath">Volledig pad naar het .exe of .bat bestand</param>
        /// <param name="arguments">Optionele command-line argumenten (standaard leeg)</param>
        /// <returns>true als gestart, false als niet gevonden of fout</returns>
        public static bool StartBatchOrExe(string filePath, string arguments = "")
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Het opgegeven bestand bestaat niet:\n" + filePath,
                                "Bestand niet gevonden", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = true, // Zet op false als je stdout wil opvangen
                    WorkingDirectory = Path.GetDirectoryName(filePath)
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fout bij het starten van het bestand:\n" + ex.Message,
                                "Startfout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
