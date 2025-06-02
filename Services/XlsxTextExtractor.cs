// AI_FileOrganizer/Services/XlsxTextExtractor.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet; // Voor Cell, Row, SheetData, SharedStringTable, etc.
using DocumentFormat.OpenXml;         // Voor OpenXmlPackageException
using AI_FileOrganizer.Utils;       // Voor ILogger

namespace AI_FileOrganizer.Services
{
    public class XlsxTextExtractor : ITextExtractor
    {
        private readonly ILogger _logger;

        public XlsxTextExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanExtract(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
        }

        public string Extract(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.Log($"WAARSCHUWING: Bestand niet gevonden voor XLSX-extractie: '{Path.GetFileName(filePath)}'.");
                    return string.Empty;
                }

                using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filePath, false)) // Read-only
                {
                    WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
                    if (workbookPart == null) return string.Empty;

                    // Haal de Shared String Table op, als die bestaat
                    SharedStringTablePart sstPart = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                    SharedStringTable sst = null;
                    if (sstPart != null)
                    {
                        sst = sstPart.SharedStringTable;
                    }

                    // Loop door alle werkbladen in het werkboek
                    foreach (Sheet sheet in workbookPart.Workbook.Sheets.Elements<Sheet>())
                    {
                        WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                        SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

                        if (sheetData != null)
                        {
                            foreach (Row row in sheetData.Elements<Row>())
                            {
                                foreach (Cell cell in row.Elements<Cell>())
                                {
                                    string cellValue = GetCellValue(cell, sst);
                                    if (!string.IsNullOrWhiteSpace(cellValue))
                                    {
                                        sb.Append(cellValue).Append(" "); // Voeg celwaarde toe met een spatie
                                    }
                                }
                                if (row.Elements<Cell>().Any()) // Alleen een newline als de rij cellen had
                                {
                                    sb.AppendLine(); // Nieuwe regel na elke rij met data
                                }
                            }
                        }
                        // Optioneel: Voeg een scheiding toe tussen werkbladen
                        // sb.AppendLine($"\n--- Werkblad: {sheet.Name?.Value ?? "Onbekend"} ---\n");
                    }
                }
            }
            catch (OpenXmlPackageException oxmlEx)
            {
                _logger.Log($"FOUT: Beschadigd XLSX-bestand '{Path.GetFileName(filePath)}' bij extractie: {oxmlEx.Message}");
                return string.Empty;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.Log($"FOUT: Toegang geweigerd tot XLSX-bestand '{Path.GetFileName(filePath)}': {uaEx.Message}");
                return string.Empty;
            }
            catch (IOException ioEx) when ((ioEx.HResult & 0xFFFF) == 32 || (ioEx.HResult & 0xFFFF) == 33)
            {
                _logger.Log($"WAARSCHUWING: Bestand '{Path.GetFileName(filePath)}' is vergrendeld en kan niet worden gelezen voor XLSX-extractie.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Log($"FOUT: Onbekende fout bij XLSX-extractie van '{Path.GetFileName(filePath)}': {ex.Message}\nStackTrace: {ex.StackTrace}");
                return string.Empty;
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Haalt de waarde van een cel op, rekening houdend met het celtype en de Shared String Table.
        /// </summary>
        private string GetCellValue(Cell cell, SharedStringTable sst)
        {
            if (cell == null || cell.CellValue == null)
            {
                return string.Empty;
            }

            string value = cell.CellValue.InnerText;

            // Als het celtype 'SharedString' is, zoek de waarde op in de SharedStringTable.
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (sst != null && int.TryParse(value, out int ssid))
                {
                    // SharedStringItem kan een Text element of RichText elementen bevatten
                    var ssi = sst.ChildElements[ssid] as SharedStringItem;
                    if (ssi != null)
                    {
                        if (ssi.Text != null) return ssi.Text.Text;

                        // Als het RichText is, concateneer alle Text delen
                        if (ssi.Text != null)
                        {
                            StringBuilder rtSb = new StringBuilder();
                            foreach (var rtRun in ssi.Text.Elements<Run>())
                            {
                                if (rtRun.Text != null) rtSb.Append(rtRun.Text.Text);
                            }
                            return rtSb.ToString();
                        }
                    }
                }
                return string.Empty; // Kon shared string niet vinden
            }
            // Voor booleans, geef "WAAR" of "ONWAAR" (of true/false)
            else if (cell.DataType != null && cell.DataType.Value == CellValues.Boolean)
            {
                return value == "1" ? "WAAR" : "ONWAAR"; // Of true/false
            }
            // Voor datums, probeer te parsen (Excel slaat datums op als getallen)
            // Dit is complexer omdat je de nummeringsbasis (1900 of 1904) moet weten.
            // Voor nu laten we de numerieke waarde staan, of je kunt een poging wagen met DateTime.FromOADate.
            // else if (cell.DataType != null && cell.DataType.Value == CellValues.Date) // DataType is niet altijd gezet voor datums
            // else if (cell.StyleIndex != null) // Datums hebben vaak een specifieke stijl
            // {
            //     // Echte datumconversie is lastig zonder de style sheet en number formats.
            //     // Voorbeeld met FromOADate, maar dit kan onjuist zijn zonder meer context.
            //     if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double oaDate))
            //     {
            //         try { return DateTime.FromOADate(oaDate).ToShortDateString(); }
            //         catch { /* Val terug naar de numerieke waarde */ }
            //     }
            // }

            return value; // Numerieke, inline string, error, etc.
        }
    }
}