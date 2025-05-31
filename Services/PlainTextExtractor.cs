using AI_FileOrganizer.Utils;
using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AI_FileOrganizer.Services
{
    /// <summary>
    /// Extractor for plain-text files such as .txt and .md.
    /// Detects BOM → kiest juiste encoding, is thread-safe
    /// en biedt zowel sync als async API.
    /// </summary>
    public sealed class PlainTextExtractor : ITextExtractor
    {
        private static readonly ImmutableHashSet<string> _supportedExtensions =
            ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, ".txt", ".md");

        private readonly ILogger _logger;

        public PlainTextExtractor(ILogger logger) =>
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /*-------------------------------------------------
         *  ITextExtractor — sync wrapper rond async versie
         *------------------------------------------------*/
        public bool CanExtract(string filePath) =>
            !string.IsNullOrWhiteSpace(filePath)
            && _supportedExtensions.Contains(Path.GetExtension(filePath));

        public string Extract(string filePath) =>
            ExtractAsync(filePath, CancellationToken.None).GetAwaiter().GetResult();

        /*-----------------------
         *  Async implementatie
         *----------------------*/
        public async Task<string> ExtractAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                _logger.Log($"⚠️  Bestand niet gevonden: '{filePath}'.");
                return string.Empty;
            }

            try
            {
                using var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite, // laat andere processen lezen
                    bufferSize: 4096,
                    useAsync: true);

                // Detecteer BOM → correcte encoding
                var encoding = DetectEncoding(fs) ?? Encoding.UTF8;

                using var reader = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: false);

                var text = await reader.ReadToEndAsync().ConfigureAwait(false);
                return text.Trim();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Log($"🚫  Toegang geweigerd tot '{filePath}': {ex.Message}");
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 32) // sharing violation
            {
                _logger.Log($"🔒  Bestand vergrendeld: '{filePath}'.");
            }
            catch (Exception ex)
            {
                _logger.Log($"❌  Onbekende fout bij lezen '{filePath}': {ex.Message}");
            }

            return string.Empty;
        }

        /*---------------------------------
         *  Hulp: simpele BOM-detector
         *--------------------------------*/
        private static Encoding? DetectEncoding(FileStream fs)
        {
            Span<byte> bom = stackalloc byte[4];
            int read = fs.Read(bom);
            fs.Seek(0, SeekOrigin.Begin); // reset

            return read switch
            {
                >= 3 when bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF => Encoding.UTF8,
                >= 2 when bom[0] == 0xFF && bom[1] == 0xFE => Encoding.Unicode,       // UTF-16 LE
                >= 2 when bom[0] == 0xFE && bom[1] == 0xFF => Encoding.BigEndianUnicode, // UTF-16 BE
                _ => null
            };
        }
    }
}
