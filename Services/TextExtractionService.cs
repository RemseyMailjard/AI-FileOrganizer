using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace AI_FileOrganizer2.Services
{
    public class TextExtractionService
    {
        private string ExtractText(string filePath)
        {
            string text = "";
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".pdf")
                {
                    using (PdfDocument document = PdfDocument.Open(filePath))
                    {
                        if (document.NumberOfPages == 0) return "";
                        foreach (var page in document.GetPages())
                        {
                            text += page.Text;
                        }
                    }
                }
                else if (extension == ".docx")
                {
                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
                    {
                        Body body = wordDoc.MainDocumentPart?.Document?.Body;
                        if (body != null)
                        {
                            text = string.Join(" ", body.Elements<Paragraph>().Select(p => p.InnerText));
                        }
                    }
                }
                else if (extension == ".txt" || extension == ".md")
                {
                    text = File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"WAARSCHUWING: Algemene fout bij lezen van bestand {Path.GetFileName(filePath)}: {ex.Message}");
            }
            return text.Trim();
        }

        private void LogMessage(string v)
        {
            throw new NotImplementedException();
        }
    }
}
