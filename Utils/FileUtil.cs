using System.Linq;
using System.Text.RegularExpressions;

namespace AI_FileOrganizer2.Utils
{
    public static class FileUtils
    {
        public static string SanitizeFileName(string proposedFullName)
        {
            if (string.IsNullOrWhiteSpace(proposedFullName)) return "";
            // Verwijder ongeldige karakters voor bestandsnamen
            string name = Regex.Replace(proposedFullName, @"[<>:""/\\|?*\x00-\x1F]", "_");
            // Trim spaties en punten aan begin/einde
            name = name.Trim('.', ' ');
            // Meerdere spaties/underscores vervangen door een enkele
            name = Regex.Replace(name, @"\s+", " ").Trim();
            name = Regex.Replace(name, @"_+", "_").Trim('_');
            // Limiteer tot 100 karakters (veilig voor filesysteem)
            if (name.Length > 100) name = name.Substring(0, 100);

            // Zorg ervoor dat de naam niet alleen underscores of spaties is geworden
            if (string.IsNullOrWhiteSpace(name.Replace("_", "")))
                return ""; // Of "Ongeldige Naam"

            return name;
        }

        public static string SanitizeFolderOrFileName(string naam)
        {
            if (string.IsNullOrWhiteSpace(naam)) return "";
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            var clean = new string(naam.Where(c => !invalidChars.Contains(c)).ToArray());
            clean = clean.Replace(".", "").Replace(",", "").Trim();
            return clean;
        }
    }
}
