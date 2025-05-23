// AI_FileOrganizer2/Services/ITextExtractor.cs
using System.IO;

namespace AI_FileOrganizer2.Services
{
    public interface ITextExtractor
    {
        /// <summary>
        /// Bepaalt of deze extractor de opgegeven bestandsextensie kan verwerken.
        /// </summary>
        /// <param name="filePath">Het pad naar het bestand.</param>
        /// <returns>True als de extractor het bestand kan verwerken, anders False.</returns>
        bool CanExtract(string filePath);

        /// <summary>
        /// Extrahert tekst uit het opgegeven bestand.
        /// </summary>
        /// <param name="filePath">Het volledige pad naar het bestand.</param>
        /// <returns>De geëxtraheerde tekst, of een lege string bij fouten of geen tekst.</returns>
        string Extract(string filePath);
    }
}