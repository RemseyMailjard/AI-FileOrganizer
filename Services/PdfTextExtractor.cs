// AI_FileOrganizer2/Services/PdfTextExtractor.cs
using System;
using System.IO;
using System.Linq;
using System.Text; // Voor StringBuilder
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter; // Voor DocstrumBoundingBoxes
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector; // Voor UnsupervisedReadingOrderDetector
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor; // Voor NearestNeighbourWordExtractor
using UglyToad.PdfPig.Core;

using AI_FileOrganizer2.Utils; // Voor ILogger

namespace AI_FileOrganizer2.Services
{
    public class PdfTextExtractor : ITextExtractor
    {
        private readonly ILogger _logger;

        public PdfTextExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanExtract(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        public string Extract(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    _logger.Log($"WAARSCHUWING: Bestand niet gevonden voor PDF-extractie: '{filePath}'.");
                    return string.Empty;
                }

                // ReaderOptions instellen voor lenient parsing
                var options = new ParsingOptions { UseLenientParsing = true };

                using (var document = PdfDocument.Open(filePath, options))
                {
                    if (document.NumberOfPages == 0)
                    {
                        _logger.Log($"WAARSCHUWING: PDF-bestand '{filePath}' bevat geen pagina's.");
                        return string.Empty;
                    }

                    var fullText = new StringBuilder();

                    foreach (var page in document.GetPages())
                    {
                        var letters = page.Letters;
                        if (letters == null || !letters.Any())
                        {
                            _logger.Log($"INFO: Geen letters op pagina {page.Number} van '{filePath}'. Val terug op page.Text.");
                            fullText.AppendLine(page.Text?.Trim());
                            continue;
                        }

                        // Woordextractie
                        var wordExtractor = NearestNeighbourWordExtractor.Instance;
                        var words = wordExtractor.GetWords(letters);

                        if (words == null || !words.Any())
                        {
                            _logger.Log($"INFO: Geen woorden op pagina {page.Number} van '{filePath}'. Val terug op page.Text.");
                            fullText.AppendLine(page.Text?.Trim());
                            continue;
                        }

                        // Paginasegmentatie
                        var pageSegmenter = DocstrumBoundingBoxes.Instance;
                        var textBlocks = pageSegmenter.GetBlocks(words);

                        if (textBlocks == null || !textBlocks.Any())
                        {
                            _logger.Log($"INFO: Geen tekstblokken op pagina {page.Number} van '{filePath}'. Val terug op page.Text.");
                            fullText.AppendLine(page.Text?.Trim());
                            continue;
                        }

                        // Leesvolgorde bepalen
                        var readingOrderDetector = UnsupervisedReadingOrderDetector.Instance;
                        var orderedTextBlocks = readingOrderDetector.Get(textBlocks);

                        if (orderedTextBlocks == null || !orderedTextBlocks.Any())
                        {
                            _logger.Log($"INFO: Geen leesvolgorde gevonden op pagina {page.Number} van '{filePath}'. Val terug op page.Text.");
                            fullText.AppendLine(page.Text?.Trim());
                            continue;
                        }

                        foreach (var block in orderedTextBlocks)
                        {
                            if (!string.IsNullOrWhiteSpace(block.Text))
                                fullText.AppendLine(block.Text.Trim());
                        }
                    }

                    var result = fullText.ToString().Trim();
                    if (string.IsNullOrEmpty(result))
                        _logger.Log($"WAARSCHUWING: Geen tekst geëxtraheerd uit '{filePath}'.");
                    return result;
                }
            }
            catch (PdfDocumentFormatException pdfEx)
            {
                _logger.Log($"FOUT: Beschadigd PDF-bestand '{Path.GetFileName(filePath)}': {pdfEx.Message}");
                return string.Empty;
            }

         
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.Log($"FOUT: Geen toegang tot PDF-bestand '{filePath}': {uaEx.Message}");
                return string.Empty;
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 32) // ERROR_SHARING_VIOLATION
            {
                _logger.Log($"WAARSCHUWING: Bestand '{filePath}' is vergrendeld.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Onbekende fout bij extractie van '{filePath}': {ex.Message}");
                return string.Empty;
            }
        }

    }
}