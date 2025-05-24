using System.Collections.Generic;

namespace AI_FileOrganizer2.Models
{
    public static class ApplicationSettings
    {
        public const int MaxTextLengthForLlm = 8000;
        public const int MinSubfolderNameLength = 3;
        public const int MaxSubfolderNameLength = 50;
        public const int MaxFilenameLength = 100; // Maximum length for AI-generated filename

        public static readonly string[] SupportedExtensions = { ".pdf", ".docx", ".txt", ".md" };

        public static readonly Dictionary<string, string> FolderCategories = new Dictionary<string, string>
        {
            { "Financiën", "1. Financiën" },
            { "Belastingen", "2. Belastingen" },
            { "Verzekeringen", "3. Verzekeringen" },
            { "Woning", "4. Woning" },
            { "Gezondheid en Medisch", "5. Gezondheid en Medisch" },
            { "Familie en Kinderen", "6. Familie en Kinderen" },
            { "Voertuigen", "7. Voertuigen" },
            { "Persoonlijke Documenten", "8. Persoonlijke Documenten" },
            { "Hobbies en interesses", "9. Hobbies en interesses" },
            { "Carrière en Professionele Ontwikkeling", "10. Carrière en Professionele Ontwikkeling" },
            { "Bedrijfsadministratie", "11. Bedrijfsadministratie" },
            { "Reizen en vakanties", "12. Reizen en vakanties" }
        };

        public const string FallbackCategoryName = "Overig";
        public static string FallbackFolderName => $"0. {FallbackCategoryName}";
    }
}