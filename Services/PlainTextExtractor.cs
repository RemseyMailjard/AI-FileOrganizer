using AI_FileOrganizer.Utils; // Voor ILogger
using System;
using System.Collections.Immutable; // Je gebruikt ImmutableHashSet, wat .NET Standard 1.3+ is, prima voor .NET Framework 4.7.2+ / .NET Core
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
    public sealed class PlainTextExtractor : ITextExtractor // Veronderstelt dat ITextExtractor ergens is gedefinieerd
    {
        private static readonly ImmutableHashSet<string> _supportedExtensions =
            ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, ".txt", ".md");

        private readonly ILogger _logger;

        public PlainTextExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
                // C# 7.3: Traditionele using statement
                using (FileStream fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite, // laat andere processen lezen
                    bufferSize: 4096,
                    useAsync: true))
                {
                    // Detecteer BOM → correcte encoding
                    // Gebruik de herstelde DetectEncoding methode
                    Encoding encoding = DetectEncodingInternal(fs) ?? Encoding.UTF8;
                    _logger.Log($"INFO: Gedetecteerde/gebruikte encoding voor {Path.GetFileName(filePath)}: {encoding.EncodingName}");


                    // C# 7.3: Traditionele using statement
                    // De StreamReader zal fs sluiten wanneer reader wordt gedisposed,
                    // omdat 'leaveOpen' false is (default).
                    using (StreamReader reader = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: false)) // detectEncodingFromByteOrderMarks is false omdat we het al doen
                    {
                        // Zorg ervoor dat de streampositie gereset is na BOM-detectie
                        if (fs.CanSeek)
                        {
                            fs.Seek(0, SeekOrigin.Begin);
                        }

                        string text = await reader.ReadToEndAsync().ConfigureAwait(false);
                        return text.Trim();
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Log($"🚫  Toegang geweigerd tot '{filePath}': {ex.Message}");
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 32 || (ex.HResult & 0xFFFF) == 33) // sharing violation (32) or lock violation (33)
            {
                _logger.Log($"🔒  Bestand vergrendeld of in gebruik: '{filePath}'. Fout: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Log($"❌  Onbekende fout bij lezen '{filePath}': {ex.Message}");
            }

            return string.Empty;
        }

        /*---------------------------------
         *  Hulp: simpele BOM-detector (C# 7.3 compatibel)
         *--------------------------------*/
        // Maak deze private static zoals de originele uitgecommentarieerde versie
        private static Encoding DetectEncodingInternal(FileStream fs)
        {
            if (fs == null) return null; // of throw ArgumentNullException
            if (!fs.CanRead) return null; // Kan niet lezen

            long originalPosition = 0;
            if (fs.CanSeek)
            {
                originalPosition = fs.Position; // Sla huidige positie op
            }
            else
            {
                // Als de stream niet seekable is, kunnen we de positie niet resetten.
                // BOM detectie aan het begin van een niet-seekable stream is lastig
                // zonder de stream te consumeren. Voor nu, return null in dit geval.
                // Je zou kunnen overwegen om een kleine buffer te lezen en die later
                // opnieuw te gebruiken als je de stream verder verwerkt.
             //   _logger.Log($"WAARSCHUWING: Stream voor {fs.Name} is niet seekable. Kan BOM niet betrouwbaar detecteren zonder te consumeren.");
                return null;
            }


            byte[] bom = new byte[4]; // Buffer voor BOM
            int read = 0;
            try
            {
                // Ga naar het begin van de stream voor BOM-detectie
                fs.Seek(0, SeekOrigin.Begin);
                read = fs.Read(bom, 0, bom.Length);
            }
            catch (Exception ex)
            {
           //     _logger.Log($"FOUT bij lezen BOM voor {fs.Name}: {ex.Message}");
                // Probeer positie te herstellen, ook al is er een fout
                if (fs.CanSeek)
                {
                    try { fs.Seek(originalPosition, SeekOrigin.Begin); } catch { /* negeer seek fout hier */ }
                }
                return null; // Kon BOM niet lezen
            }


            Encoding result = null;
            if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                result = Encoding.UTF8;
            }
            else if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
            {
                // Check for UTF-32 LE BOM (FF FE 00 00)
                if (read >= 4 && bom[2] == 0x00 && bom[3] == 0x00)
                {
                    result = Encoding.UTF32; // UTF-32 LE
                }
                else
                {
                    result = Encoding.Unicode; // UTF-16 LE
                }
            }
            else if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
            {
                result = Encoding.BigEndianUnicode; // UTF-16 BE
            }
            // UTF-32 BE BOM (00 00 FE FF)
            else if (read >= 4 && bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF)
            {
                // Er is geen standaard Encoding.UTF32BE, maar je zou een new UTF32Encoding(true, true) kunnen gebruiken.
                // Voor nu laten we het bij null, wat resulteert in UTF8 fallback.
                // result = new UTF32Encoding(true, true); // UTF-32 BE
                result = null; // Of behandel als onbekend -> fallback naar UTF8
            }
            // Geen BOM gedetecteerd of niet genoeg bytes gelezen
            // result blijft null, wat later wordt geïnterpreteerd als UTF8 fallback.

            // Reset de stream naar zijn oorspronkelijke positie (of naar begin als BOM is gevonden en stream gereset moet worden voor reader)
            // De StreamReader zal vanaf het begin lezen, dus de reset hier moet naar 0 zijn als BOM is gebruikt.
            // Echter, de caller (ExtractAsync) doet ook een seek(0) voor de reader.
            // Het is beter om hier te resetten naar de *originalPosition* als de BOM niet bruikbaar was,
            // en de caller de uiteindelijke positionering te laten doen.
            // Voor nu, resetten we altijd naar het begin, aangezien de reader toch vanaf daar begint.
            // Of beter nog, de DetectEncoding methode zou de stream niet moeten resetten, maar de bytes gewoon
            // aan de caller teruggeven of de reader direct configureren.
            // De huidige aanroep `DetectEncodingInternal(fs) ?? Encoding.UTF8;` en dan `new StreamReader(fs, encoding... fs.Seek(0...)`
            // betekent dat de streampositie na DetectEncodingInternal relevant is.
            // Laten we de streampositie herstellen naar de *oorspronkelijke* positie,
            // zodat de caller kan beslissen wat te doen.
            // De `StreamReader` in `ExtractAsync` zal dan de positie zelf beheren (vanaf begin na onze `fs.Seek(0)` daar).
            if (fs.CanSeek)
            {
                try
                {
                    fs.Seek(originalPosition, SeekOrigin.Begin);
                }
                catch (Exception ex)
                {
            //        _logger.Log($"FOUT bij resetten stream positie in DetectEncodingInternal voor {fs.Name}: {ex.Message}");
                }
            }

            return result;
        }
        // Interface die PlainTextExtractor implementeert (als voorbeeld)
        public interface ITextExtractor
        {
            bool CanExtract(string filePath);
            string Extract(string filePath);
            Task<string> ExtractAsync(string filePath, CancellationToken cancellationToken = default);
        }
    }
}