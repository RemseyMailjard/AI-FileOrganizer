using AI_FileOrganizer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AI_FileOrganizer.Services
{
    public class TextExtractionService
    {
        private readonly ILogger _logger;
        private readonly List<ITextExtractor> _extractors; // Collection of specific extractors

        public TextExtractionService(ILogger logger, IEnumerable<ITextExtractor> extractors)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _extractors = extractors?.ToList() ?? throw new ArgumentNullException(nameof(extractors));

            if (!_extractors.Any())
            {
                _logger.Log("WAARSCHUWING: TextExtractionService geïnitialiseerd zonder ITextExtractors.");
            }
        }

        public string ExtractText(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.Log("WAARSCHUWING: Geen bestandspad opgegeven voor tekstextractie.");
                return string.Empty;
            }

            if (!File.Exists(filePath))
            {
                _logger.Log($"WAARSCHUWING: Bestand niet gevonden voor tekstextractie: '{Path.GetFileName(filePath)}'.");
                return string.Empty;
            }

            // Find the correct extractor for the file extension
            foreach (var extractor in _extractors)
            {
                if (extractor.CanExtract(filePath))
                {
                    _logger.Log($"INFO: Extraheren van tekst uit '{Path.GetFileName(filePath)}' met {extractor.GetType().Name}.");
                    return extractor.Extract(filePath);
                }
            }

            _logger.Log($"WAARSCHUWING: Geen geschikte tekstextractor gevonden voor '{Path.GetFileName(filePath)}'.");
            return string.Empty;
        }

        // The previous LogMessage method was unnecessary as _logger is directly available.
    }
}