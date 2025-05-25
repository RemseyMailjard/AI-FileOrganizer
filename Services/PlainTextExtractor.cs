// AI_FileOrganizer/Services/PlainTextExtractor.cs
using System;
using System.IO;
using AI_FileOrganizer.Utils; // Voor ILogger

namespace AI_FileOrganizer.Services
{
    public class PlainTextExtractor : ITextExtractor
    {
        private readonly ILogger _logger;

        public PlainTextExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanExtract(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".txt" || extension == ".md";
        }

        public string Extract(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.Log($"WAARSCHUWING: Bestand niet gevonden voor TXT/MD-extractie: '{Path.GetFileName(filePath)}'.");
                    return string.Empty;
                }
                return File.ReadAllText(filePath).Trim();
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.Log($"FOUT: Toegang geweigerd tot TXT/MD-bestand '{Path.GetFileName(filePath)}': {uaEx.Message}");
                return string.Empty;
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 32) // ERROR_SHARING_VIOLATION
            {
                _logger.Log($"WAARSCHUWING: Bestand '{Path.GetFileName(filePath)}' is vergrendeld en kan niet worden gelezen voor TXT/MD-extractie.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Onbekende fout bij TXT/MD-extractie van '{Path.GetFileName(filePath)}': {ex.Message}");
                return string.Empty;
            }
        }
    }
}