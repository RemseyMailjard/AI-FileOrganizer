using System.Collections.Generic;

namespace AI_FileOrganizer.Models
{
    public static class ApplicationSettings
    {
        public const int MaxTextLengthForLlm = 30000;
        public const int MinSubfolderNameLength = 3;
        public const int MaxSubfolderNameLength = 50;
        public const int MaxFilenameLength = 100; // Maximum length for AI-generated filename
        public static bool UseDetailedSubfolders { get; set; } = true; // Voorbeeld: standaard aan
        public static bool OrganizeFallbackCategoryIfNoMatch { get; set; } = true; // Voorbeeld: standaard aan


        public static readonly string[] SupportedExtensions = { ".pdf", ".docx", ".txt", ".md" , ".pptx" };

        public static readonly Dictionary<string, string> FolderCategories = new Dictionary<string, string>
            {
                { "Financiën", "1. Financieel" },
                { "Belastingen", "2. Belastingzaken" },
                { "Verzekeringen", "3. Verzekeringen en Polissen" },
                { "Woning", "4. Huis en Wonen" },
                { "Gezondheid en Medisch", "5. Gezondheid en Zorg" },
                { "Familie en Kinderen", "6. Gezin en Familie" },
                { "Voertuigen", "7. Vervoer en Voertuigen" },
                { "Persoonlijke Documenten", "8. Identiteit en Documenten" },
                { "Hobbies en interesses", "9. Vrije Tijd en Hobby's" },
                { "Carrière en Professionele Ontwikkeling", "10. Werk en Loopbaan" },
                { "Bedrijfsadministratie", "11. Zakelijke Administratie" },
                { "Reizen en vakanties", "12. Vakanties en Reizen" }
            };

        public const string FallbackCategoryName = "Diversen";

        public static string FallbackFolderName => $"0. {FallbackCategoryName}";
    }
}