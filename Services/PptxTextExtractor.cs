// AI_FileOrganizer/Services/PptxTextExtractor.cs
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation; // Voor Slide, TextBody
using Drawing = DocumentFormat.OpenXml.Drawing; // Alias voor Drawing namespace
using DocumentFormat.OpenXml; // Voor OpenXmlPackageException
using AI_FileOrganizer.Utils; // Voor ILogger
using System.Text; // Voor StringBuilder

namespace AI_FileOrganizer.Services
{
    public class PptxTextExtractor : ITextExtractor // Implementeer dezelfde interface
    {
        private readonly ILogger _logger;

        public PptxTextExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanExtract(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pptx", StringComparison.OrdinalIgnoreCase);
        }

        public string Extract(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.Log($"WAARSCHUWING: Bestand niet gevonden voor PPTX-extractie: '{Path.GetFileName(filePath)}'.");
                    return string.Empty;
                }

                // Open de presentatie in read-only modus
                using (PresentationDocument presentationDocument = PresentationDocument.Open(filePath, false))
                {
                    if (presentationDocument.PresentationPart?.Presentation?.SlideIdList == null)
                    {
                        _logger.Log($"INFO: Geen slides gevonden in PPTX-bestand: '{Path.GetFileName(filePath)}'.");
                        return string.Empty;
                    }

                    // Loop door elke slide in de presentatie
                    foreach (SlideId slideId in presentationDocument.PresentationPart.Presentation.SlideIdList.Elements<SlideId>())
                    {
                        // Haal de SlidePart op via de relatie ID
                        SlidePart slidePart = (SlidePart)presentationDocument.PresentationPart.GetPartById(slideId.RelationshipId);

                        if (slidePart?.Slide != null)
                        {
                            // Loop door alle Drawing.Paragraph elementen op de slide
                            // Tekst in PPTX kan in verschillende shapes en placeholders zitten.
                            // We zoeken naar Drawing.Paragraph elementen binnen TextBody elementen.
                            foreach (var textBody in slidePart.Slide.Descendants<TextBody>())
                            {
                                foreach (var paragraph in textBody.Elements<Drawing.Paragraph>())
                                {
                                    // Concateneer de tekst van alle Drawing.Run elementen binnen de paragraaf
                                    foreach (var run in paragraph.Elements<Drawing.Run>())
                                    {
                                        sb.Append(run.Text?.Text);
                                    }
                                    // Voeg een spatie toe na elke paragraaf (of newline als je dat prefereert)
                                    // Een newline kan de leesbaarheid voor de AI soms verbeteren.
                                    sb.AppendLine(); // GebruikAppendLine voor een nieuwe regel per paragraaf
                                }
                            }

                            // Haal ook tekst uit notities (speaker notes) als die er zijn
                            NotesSlidePart notesSlidePart = slidePart.NotesSlidePart;
                            if (notesSlidePart?.NotesSlide != null)
                            {
                                foreach (var textBody in notesSlidePart.NotesSlide.Descendants<TextBody>())
                                {
                                    foreach (var paragraph in textBody.Elements<Drawing.Paragraph>())
                                    {
                                        foreach (var run in paragraph.Elements<Drawing.Run>())
                                        {
                                            sb.Append(run.Text?.Text);
                                        }
                                        sb.AppendLine();
                                    }
                                }
                            }
                        }
                        // Voeg een duidelijke scheiding toe tussen slides, bijv. een paar nieuwe regels.
                        // Dit kan de AI helpen de context per slide beter te begrijpen.
                        // sb.AppendLine("\n--- Volgende Slide ---\n"); // Optioneel
                    }
                }
            }
            catch (OpenXmlPackageException oxmlEx)
            {
                _logger.Log($"FOUT: Beschadigd PPTX-bestand '{Path.GetFileName(filePath)}' bij extractie: {oxmlEx.Message}");
                return string.Empty;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.Log($"FOUT: Toegang geweigerd tot PPTX-bestand '{Path.GetFileName(filePath)}': {uaEx.Message}");
                return string.Empty;
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 32 || (ioEx.HResult & 0xFFFF) == 33) // ERROR_SHARING_VIOLATION or ERROR_LOCK_VIOLATION
            {
                _logger.Log($"WAARSCHUWING: Bestand '{Path.GetFileName(filePath)}' is vergrendeld en kan niet worden gelezen voor PPTX-extractie.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Onbekende fout bij PPTX-extractie van '{Path.GetFileName(filePath)}': {ex.Message}\nStackTrace: {ex.StackTrace}");
                return string.Empty;
            }

            return sb.ToString().Trim();
        }
    }
}