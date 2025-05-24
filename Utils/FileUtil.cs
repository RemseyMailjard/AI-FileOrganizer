using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AI_FileOrganizer2.Utils
{
    public static class FileUtils
    {
        public static string SanitizeFileName(string proposedFullName)
        {
            if (string.IsNullOrWhiteSpace(proposedFullName)) return "";
            // Remove invalid characters for file names
            string name = Regex.Replace(proposedFullName, @"[<>:""/\\|?*\x00-\x1F]", "_");
            // Trim spaces and dots at the beginning/end
            name = name.Trim('.', ' ');
            // Replace multiple spaces/underscores with a single one
            name = Regex.Replace(name, @"\s+", " ").Trim();
            name = Regex.Replace(name, @"_+", "_").Trim('_');
            // Max length should ideally come from ApplicationSettings, but keeping it consistent with original Form1 constant for now.
            if (name.Length > 100) name = name.Substring(0, 100);

            // Ensure the name doesn't become empty or just underscores/spaces after sanitization
            if (string.IsNullOrWhiteSpace(name.Replace("_", "").Replace(" ", "")))
                return "OngeldigeNaam"; // Provide a fallback if it becomes totally unreadable

            return name;
        }

        public static string SanitizeFolderOrFileName(string naam)
        {
            if (string.IsNullOrWhiteSpace(naam)) return "";
            var invalidChars = Path.GetInvalidFileNameChars(); // Also covers some path chars
            // Union with invalid path chars for robustness for folders
            var invalidPathChars = Path.GetInvalidPathChars().Union(invalidChars).Distinct().ToArray();
            var clean = new string(naam.Where(c => !invalidPathChars.Contains(c)).ToArray());
            clean = clean.Replace(".", "").Replace(",", "").Trim(); // Remove common punctuation
            // Replace multiple spaces with single space, then trim again
            clean = Regex.Replace(clean, @"\s+", " ").Trim();
            // Remove trailing spaces, periods, or other common problematic characters for paths
            clean = clean.TrimEnd('.', ' ');

            if (string.IsNullOrWhiteSpace(clean))
                return "OngeldigeMapnaam";

            return clean;
        }

        /// <summary>
        /// Calculates the relative path of a full path relative to a base path.
        /// </summary>
        public static string GetRelativePath(string basePath, string fullPath)
        {
            string baseWithSeparator = AppendDirectorySeparatorChar(basePath);
            Uri baseUri = new Uri(baseWithSeparator);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Ensures a path ends with a directory separator character.
        /// </summary>
        public static string AppendDirectorySeparatorChar(string path)
        {
            if (!string.IsNullOrEmpty(path) && !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        public static string FallbackFolderNameFromFilename(string filename)
        {
            filename = filename.ToLowerInvariant();

            if (filename.Contains("factuur")) return "Factuur";
            if (filename.Contains("offerte")) return "Offerte";
            if (filename.Contains("polis") || filename.Contains("verzekering")) return "Polis";
            if (filename.Contains("cv") || filename.Contains("curriculum")) return "CV";
            if (filename.Contains("notulen")) return "Notulen";
            if (filename.Contains("handleiding") || filename.Contains("manual")) return "Handleiding";
            if (filename.Contains("rapport") || filename.Contains("report")) return "Rapport";
            if (filename.Contains("budget") || filename.Contains("begroting")) return "Budget";
            if (filename.Contains("jaaropgave") || filename.Contains("jaaroverzicht")) return "Jaaropgave";

            return null;
        }
     
    }
}