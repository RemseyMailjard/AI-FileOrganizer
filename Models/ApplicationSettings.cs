// AI_FileOrganizer/Models/ApplicationSettings.cs
using System;
using System.Collections.Generic;

namespace AI_FileOrganizer.Models
{
    public static class ApplicationSettings
    {
        // --- Algemene Limieten ---
        public const int MaxTextLengthForLlm = 30000;       // Max karakters voor LLM input (tekst-gebaseerd)
        public const int MinSubfolderNameLength = 3;       // Min lengte voor AI-gegenereerde submapnamen
        public const int MaxSubfolderNameLength = 70;      // Max lengte voor AI-gegenereerde submapnamen
        public const int MaxFilenameLength = 100;          // Max lengte voor AI-gegenereerde bestandsnamen (zonder extensie)

        // --- Gedrag Instellingen ---
        /// <summary>
        /// Bepaalt of gedetailleerde submappen (bijv. "Facturen/Januari 2024") moeten worden gebruikt voor documenten.
        /// </summary>
        public static bool UseDetailedSubfolders { get; set; } = true;

        /// <summary>
        /// Bepaalt of documenten die in de fallback categorie ('Overig') vallen, ook daadwerkelijk verplaatst moeten worden.
        /// </summary>
        public static bool OrganizeFallbackCategoryIfNoMatch { get; set; } = true;

        /// <summary>
        /// Optioneel: Specificeert een standaard AI-provider die gebruikt moet worden voor documenten als de
        /// primair geselecteerde provider niet geschikt is voor tekstverwerking (bijv. als een image provider is gekozen
        /// tijdens een batch operatie die ook documenten bevat).
        /// Laat leeg of null als je geen default fallback provider wilt.
        /// Mogelijke waarden: "Gemini (Google)", "OpenAI (openai.com)", "Azure OpenAI", "Lokaal ONNX-model".
        /// </summary>
        public static string DefaultProviderForDocumentsIfNotSpecified { get; set; } = "Lokaal ONNX-model"; // Voorbeeld default

        // --- Bestandstypen ---
        /// <summary>
        /// Extensies voor documenten die tekstextractie en categorisatie/hernoeming ondergaan.
        /// </summary>
        public static readonly HashSet<string> DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".txt", ".md", ".pptx", ".xlsx", ".xls", ".rtf", ".odt"
        };

        /// <summary>
        /// Extensies voor afbeeldingen die visuele analyse ondergaan voor naamsuggesties.
        /// </summary>
        public static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".tiff", ".tif", ".heic", ".avif"
        };

        /// <summary>
        /// Retourneert een gecombineerde set van alle ondersteunde extensies (documenten en afbeeldingen).
        /// </summary>
        public static HashSet<string> GetAllSupportedExtensions()
        {
            var allExtensions = new HashSet<string>(DocumentExtensions, StringComparer.OrdinalIgnoreCase);
            allExtensions.UnionWith(ImageExtensions);
            return allExtensions;
        }

        // --- Categorieën en Mappen voor Documenten ---
        /// <summary>
        /// Mapping van AI-gegenereerde categorienamen naar daadwerkelijke mapnamen.
        /// Keys zijn de namen die de AI retourneert (vergelijking is case-insensitive).
        /// Values zijn de mapnamen die worden aangemaakt.
        /// </summary>
        public static readonly Dictionary<string, string> FolderCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Financiën", "1. Financieel" },
            { "Belastingen", "2. Belastingzaken" },
            { "Verzekeringen", "3. Verzekeringen en Polissen" },
            { "Woning", "4. Huis en Wonen" },
            { "Gezondheid en Medisch", "5. Gezondheid en Zorg" },
            { "Familie en Kinderen", "6. Gezin en Familie" },
            { "Voertuigen", "7. Vervoer en Voertuigen" },
            { "Persoonlijke Documenten", "8. Identiteit en Persoonlijk" },
            { "Hobbies en interesses", "9. Vrije Tijd en Hobby's" },
            { "Carrière en Professionele Ontwikkeling", "10. Werk en Loopbaan" },
            { "Bedrijfsadministratie", "11. Zakelijke Administratie" },
            { "Reizen en vakanties", "12. Vakanties en Reizen" },
            { "Studie en Opleiding", "13. Studie en Opleiding" },
            { "Juridisch", "14. Juridische Documenten" }
            // De AiClassificationService.DEFAULT_FALLBACK_CATEGORY ("Overig") wordt hieronder gemapt
            // via FallbackCategoryKey en FallbackFolderName.
        };

        /// <summary>
        /// De sleutel (categorienaam) die de AI (AiClassificationService) retourneert
        /// voor de fallback categorie. Dit moet consistent zijn.
        /// </summary>
        public const string FallbackCategoryKey = "Overig";

        /// <summary>
        /// De daadwerkelijke mapnaam die gebruikt wordt voor de fallback categorie.
        /// </summary>
        public static string FallbackFolderName => $"99. {FallbackCategoryKey}"; // Bijv. "99. Overig"

        /// <summary>
        /// De standaardmapnaam (binnen de hoofddoelmap) waarin afbeeldingen worden geplaatst
        /// als ze geen verdere categorisatie ondergaan (wat momenteel het geval is).
        /// </summary>
        public const string DefaultImageFolderName = "Afbeeldingen";
    }
}