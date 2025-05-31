using AI_FileOrganizer.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AI_FileOrganizer.Services
{
    /// <summary>
    /// Central service that delegates text-extraction to concrete <see cref="ITextExtractor"/> implementations.
    /// Thread-safe, lazy-initialised and ready for dependency-injection.
    /// </summary>
    public sealed class TextExtractionService
    {
        private readonly ILogger _logger;
        private readonly ImmutableArray<ITextExtractor> _extractors;

        public TextExtractionService(
            ILogger logger,
            IEnumerable<ITextExtractor> extractors)
        {
            _logger     = logger   ?? throw new ArgumentNullException(nameof(logger));
            _extractors = (extractors ?? throw new ArgumentNullException(nameof(extractors)))
                          .ToImmutableArray();

            if (_extractors.IsEmpty)
                _logger.Log("⚠️  TextExtractionService geïnitiseerd zonder extractors.");
        }

        /// <summary>
        /// Synchronous convenience wrapper rond <see cref="ExtractTextAsync"/>.
        /// </summary>
        public string ExtractText(string filePath) =>
            ExtractTextAsync(filePath, CancellationToken.None).GetAwaiter().GetResult();

        /// <summary>
        /// Extracts text asynchronously.  
        /// Returns an empty string when extraction fails instead of throwing,
        /// zodat de bovenlaag zelf kan beslissen wat ermee te doen.
        /// </summary>
        public async Task<string> ExtractTextAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.Log("⚠️  Leeg bestandspad ontvangen.");
                return string.Empty;
            }
            if (!File.Exists(filePath))
            {
                _logger.Log($"⚠️  Bestand niet gevonden: '{Path.GetFileName(filePath)}'.");
                return string.Empty;
            }

            var extractor = _extractors.FirstOrDefault(e => e.CanExtract(filePath));
            if (extractor is null)
            {
                _logger.Log($"⚠️  Geen extractor voor extensie '{Path.GetExtension(filePath)}'.");
                return string.Empty;
            }

            _logger.Log($"ℹ️  Extractie uit '{Path.GetFileName(filePath)}' met {extractor.GetType().Name}.");

            try
            {
                // Sommige extractors zijn I/O-bound; maak ze async-vriendelijk met Task.Run
                return await Task.Run(() => extractor.Extract(filePath), cancellationToken)
                                 .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Log($"❌  Fout bij extractie: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
