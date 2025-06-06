﻿// AI_FileOrganizer/Services/DocxTextExtractor.cs
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml; // Voor OpenXmlPackageException
using AI_FileOrganizer.Utils; // Voor ILogger

namespace AI_FileOrganizer.Services
{
    public class DocxTextExtractor : ITextExtractor
    {
        private readonly ILogger _logger;

        public DocxTextExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanExtract(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".docx", StringComparison.OrdinalIgnoreCase);
        }

        public string Extract(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.Log($"WAARSCHUWING: Bestand niet gevonden voor DOCX-extractie: '{Path.GetFileName(filePath)}'.");
                    return string.Empty;
                }

                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false)) // Openen in read-only modus
                {
                    Body body = wordDoc.MainDocumentPart?.Document?.Body;
                    if (body != null)
                    {
                        // Gebruik Environment.NewLine voor consistentie en trim elke paragraaf
                        return string.Join(Environment.NewLine, body.Elements<Paragraph>().Select(p => p.InnerText.Trim())).Trim();
                    }
                }
            }
            catch (OpenXmlPackageException oxmlEx)
            {
                _logger.Log($"FOUT: Beschadigd DOCX-bestand '{Path.GetFileName(filePath)}' bij extractie: {oxmlEx.Message}");
                return string.Empty;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.Log($"FOUT: Toegang geweigerd tot DOCX-bestand '{Path.GetFileName(filePath)}': {uaEx.Message}");
                return string.Empty;
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 32) // ERROR_SHARING_VIOLATION
            {
                _logger.Log($"WAARSCHUWING: Bestand '{Path.GetFileName(filePath)}' is vergrendeld en kan niet worden gelezen voor DOCX-extractie.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Onbekende fout bij DOCX-extractie van '{Path.GetFileName(filePath)}': {ex.Message}");
                return string.Empty;
            }
            return string.Empty; // In geval van een onverwachte codepad zonder return
        }
    }
}