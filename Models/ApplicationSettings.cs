using System;
using System.Collections.Generic;
using System.Linq; // Nodig voor .Where in InitializeDetailedSubfolderPrompts (maar niet gebruikt in de huidige versie van die methode)

namespace AI_FileOrganizer.Models
{
    // 1. AiModelParams: Vervangen 'record' door 'class'
    public class AiModelParams
    {
        public int MaxTokens { get; private set; } // Properties kunnen private set hebben als ze niet extern gewijzigd mogen worden na constructie
        public float Temperature { get; private set; }

        public AiModelParams(int maxTokens, float temperature)
        {
            MaxTokens = maxTokens;
            Temperature = temperature;
        }
    }

    // 2. AiTaskSettings: Kan een static class blijven, 'new' expressies aangepast
    public static class AiTaskSettings
    {
        public static AiModelParams CategoryClassification { get; set; } = new AiModelParams(50, 0.0f);
        public static AiModelParams EffectiveFilename { get; set; } = new AiModelParams(25, 0.2f);
        public static AiModelParams DetailedSubfolderType { get; set; } = new AiModelParams(15, 0.1f);
        public static AiModelParams DetailedSubfolderName { get; set; } = new AiModelParams(30, 0.3f);
    }

    // 3. DetailedSubfolderPromptConfig: Kan een class blijven, 'new' expressies aangepast
    public class DetailedSubfolderPromptConfig
    {
        public Dictionary<string, string> DocumentTypeExamples { get; set; }
        public string DocumentTypeBasePrompt { get; set; }
        public Dictionary<Tuple<string, string>, string> SpecificSubfolderNamePrompts { get; set; } // Tuple voor C# 7.3

        public DetailedSubfolderPromptConfig() // Constructor om dictionaries te initialiseren
        {
            DocumentTypeExamples = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            DocumentTypeBasePrompt = @"Analyseer het document binnen de categorie '{category}'.
Identificeer het specifieke type document. Mogelijke types voor '{category}' zijn bijvoorbeeld:
{examples}
- Anders (geef een korte, specifieke beschrijving van maximaal 3 woorden als het niet past)

Documentinformatie:
<bestandsnaam>
{originalFilename}
</bestandsnaam>
{textContext}

Antwoord (alleen het type, bijv. 'Factuur' of 'Belastingaangifte'): ";
            // Gebruik Tuple voor C# 7.3 compatibele dictionary key
            SpecificSubfolderNamePrompts = new Dictionary<Tuple<string, string>, string>(new TupleStringStringComparer());
        }
    }

    // Custom comparer voor Tuple<string, string> als dictionary key (voor case-insensitivity op CategoryKey)
    public class TupleStringStringComparer : IEqualityComparer<Tuple<string, string>>
    {
        public bool Equals(Tuple<string, string> x, Tuple<string, string> y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1) && // CategoryKey (case-insensitive)
                   StringComparer.Ordinal.Equals(x.Item2, y.Item2);             // DocumentTypeKeyword (case-sensitive, of OrdinalIgnoreCase indien gewenst)
        }

        public int GetHashCode(Tuple<string, string> obj)
        {
            if (obj is null) return 0;
            int hashItem1 = obj.Item1 == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1);
            int hashItem2 = obj.Item2 == null ? 0 : StringComparer.Ordinal.GetHashCode(obj.Item2);
            return hashItem1 ^ hashItem2;
        }
    }


    public static class ApplicationSettings
    {
        public const int MaxTextLengthForLlm = 30000;
        public const int MinSubfolderNameLength = 3;
        public const int MaxSubfolderNameLength = 70;
        public const int MaxFilenameLength = 100;

        public static bool UseDetailedSubfolders { get; set; } = true;
        public static bool OrganizeFallbackCategoryIfNoMatch { get; set; } = true;
        public static string DefaultProviderForDocumentsIfNotSpecified { get; set; } = "Lokaal ONNX-model";

        public static readonly HashSet<string> DocumentExtensions;
        public static readonly HashSet<string> ImageExtensions;

        public static readonly Dictionary<string, string> FolderCategories;
        public const string FallbackCategoryKey = "Overig";
        public static string FallbackFolderName
        {
            get { return "99. " + FallbackCategoryKey; }
        }

        public const string DefaultImageFolderName = "Afbeeldingen";
        public static readonly Dictionary<string, List<string>> SubfolderStructure;
        public static readonly string[] GenericFilenameTerms;
        public static DetailedSubfolderPromptConfig DetailedSubfolderPrompts { get; private set; }
        public static Dictionary<string, string> DocumentTypePlurals { get; private set; }

        // Static constructor voor initialisatie van readonly fields en andere setup
        static ApplicationSettings()
        {
            DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".docx", ".txt", ".md", ".pptx", ".xlsx", ".xls", ".rtf", ".odt"
            };

            ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp", ".tiff", ".tif", ".heic", ".avif"
            };

            FolderCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
                { "Reizen en vakanties", "12. Vakanties en Reizen" }
            };

            SubfolderStructure = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "1. Financieel", new List<string> { "Bankafschriften", "Budgettering", "Inkomsten", "Uitgaven", "Leningen & Kredieten", "Jaaroverzichten" } },
                { "2. Belastingzaken", new List<string> { "Aangiftes", "Correspondentie Belastingdienst", "Voorlopige aanslagen", "Definitieve aanslagen", "Teruggaven & Betalingen" } },
                { "3. Verzekeringen en Polissen", new List<string> { "Zorgverzekering", "Woonverzekering", "Reisverzekering", "Autoverzekering", "Inboedelverzekering", "Overige verzekeringen", "Polisbladen" } },
                { "4. Huis en Wonen", new List<string> { "Huur-Koopcontract", "Hypotheekdocumenten", "Verbouwingen", "Energie & Nutsvoorzieningen", "WOZ-beschikkingen", "VVE-Gemeente" } },
                { "5. Gezondheid en Zorg", new List<string> { "Huisarts", "Specialisten", "Medicatieoverzicht", "Declaraties zorgverzekering", "Medische verslagen", "Vaccinaties" } },
                { "6. Gezin en Familie", new List<string> { "Geboorteakten", "Schooldocumenten", "Kinderopvang", "Zorgtoeslag & Kinderbijslag", "Oudercommunicatie", "Oppas & Logistiek" } },
                { "7. Vervoer en Voertuigen", new List<string> { "Kentekenbewijzen", "Verzekering", "APK & Onderhoud", "Boetes & Correspondentie", "Verkoop-Aankoopdocumenten" } },
                { "8. Identiteit en Persoonlijk", new List<string> { "Paspoorten & ID’s", "Diploma’s & Certificaten", "CV en sollicitaties", "Lidmaatschappen", "Notariële stukken", "Wachtwoorden (versleuteld)" } },
                { "9. Vrije Tijd en Hobby's", new List<string> { "Foto’s & Creaties", "Cursussen", "Muziek & Instrumenten", "Sport & Verenigingen", "Vrijwilligerswerk" } },
                { "10. Werk en Loopbaan", new List<string> { "Arbeidscontracten", "Salarisspecificaties", "Opleidingen", "Netwerk & Events", "Referenties" } },
                { "11. Zakelijke Administratie", new List<string> { "Facturen", "Bonnetjes", "BTW-aangifte", "Inkoop-Verkoop", "KvK & Registratie", "Contracten" } },
                { "12. Vakanties en Reizen", new List<string> { "Reispassen & Visa", "Vliegtickets & Boekingen", "Reisverzekeringen", "Reisverslagen & Foto’s", "Inpaklijsten", "Bestemmingen" } }
            };

            GenericFilenameTerms = new string[] {
                "document", "bestand", "info", "overig", "algemeen", "diversen", "factuur",
                "file", "information", "general", "various", "invoice", ""
            };

            DetailedSubfolderPrompts = new DetailedSubfolderPromptConfig(); // Initialiseer het object
            InitializeDetailedSubfolderPrompts();

            DocumentTypePlurals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            InitializeDocumentTypePlurals();
        }


        public static HashSet<string> GetAllSupportedExtensions()
        {
            HashSet<string> all = new HashSet<string>(DocumentExtensions, StringComparer.OrdinalIgnoreCase);
            all.UnionWith(ImageExtensions);
            return all;
        }

        private static void InitializeDocumentTypePlurals()
        {
            DocumentTypePlurals["Offerte"] = "Offertes";
            DocumentTypePlurals["Aangifte"] = "Aangiften";
            DocumentTypePlurals["Polis"] = "Polissen";
            DocumentTypePlurals["Brief"] = "Brieven";
            DocumentTypePlurals["Certificaat"] = "Certificaten";
            DocumentTypePlurals["Contract"] = "Contracten";
            DocumentTypePlurals["Rapport"] = "Rapporten";
            DocumentTypePlurals["Formulier"] = "Formulieren";
            DocumentTypePlurals["Overeenkomst"] = "Overeenkomsten";
        }

        private static void InitializeDetailedSubfolderPrompts()
        {
            var dsp = DetailedSubfolderPrompts; // Kortere alias

            // Stap 1: Voorbeelden van documenttypes per categorie
            dsp.DocumentTypeExamples["Financiën"] = "- Factuur, Bankafschrift, Offerte, Leningsovereenkomst, Jaaropgave, Onkostennota, Salarisspecificatie, Beleggingsoverzicht, Budget";
            dsp.DocumentTypeExamples["Belastingen"] = "- Belastingaangifte, Belastingaanslag, Voorlopige aanslag, Toeslagbeschikking, Bezwaarschrift";
            dsp.DocumentTypeExamples["Verzekeringen"] = "- Polisblad, Schadeclaim, Verzekeringsvoorwaarden, Opzegging, Offerte verzekering";
            dsp.DocumentTypeExamples["Woning"] = "- Huurcontract, Koopakte, Hypotheekofferte, Taxatierapport, VvE Documenten, Bouwtekening";
            dsp.DocumentTypeExamples["Gezondheid en Medisch"] = "- Doktersrekening, Recept, Medisch verslag, Verwijsbrief, Vaccinatiebewijs";
            dsp.DocumentTypeExamples["Persoonlijke Documenten"] = "- CV, Sollicitatiebrief, Diploma, Certificaat, Paspoortkopie, Rijbewijskopie, Geboorteakte";
            dsp.DocumentTypeExamples["Carrière en Professionele Ontwikkeling"] = "- Arbeidscontract, Salarisstrook, Getuigschrift, Trainingsmateriaal, Certificaat";
            dsp.DocumentTypeExamples["Bedrijfsadministratie"] = "- Uitgaande factuur, Inkomende factuur, Inkooporder, Contract, KvK-uittreksel, Jaarrekening";

            // Stap 2: Specifieke prompts - gebruik Tuple<string, string> als key
            dsp.SpecificSubfolderNamePrompts[Tuple.Create("Financiën", "factuur")] =
    $@"Het document is een '{{documentType}}' (categorie: {{category}}). 
Stel een submapnaam voor van maximaal 3-4 woorden. Focus op:
1. Type factuur (bijv. Verkoop, Inkoop, Credit) - als dit nog niet in '{{documentType}}' zit.
2. Maand en Jaar (bijv. Februari 2024, Q1 2023) of de leverancier/klant.

Voorbeelden:
- Verkoopfacturen Februari 2024
- Inkoop [Leverancier] Maart 2023
- Creditnota's Q2

Documentinformatie (gebruik dit voor context):
<bestandsnaam>
{{originalFilename}}
</bestandsnaam>
{{textContext}}

Antwoord (alleen de beschrijvende submapnaam, bijv. 'Verkoop Maart 2024' of 'Kosten KPN Januari'): ";

            dsp.SpecificSubfolderNamePrompts[Tuple.Create("Financiën", "bankafschrift")] =
    $@"Het document is een '{{documentType}}' (categorie: {{category}}). 
Stel een submapnaam voor die de bank en periode aanduidt.
Voorbeeld:
- ING Januari 2024
- Rabobank Q1 2023

Documentinformatie:
<bestandsnaam>
{{originalFilename}}
</bestandsnaam>
{{textContext}}

Antwoord (alleen de submapnaam, bijv. 'ING Maart 2024'): ";

            dsp.SpecificSubfolderNamePrompts[Tuple.Create("Financiën", "leningsovereenkomst")] =
    $@"Het document is een '{{documentType}}' (categorie: {{category}}).
Stel een submapnaam voor die de geldverstrekker of het doel van de lening aanduidt.
Voorbeeld:
- Hypotheek [Banknaam]
- Persoonlijke Lening [Doel]

Documentinformatie:
<bestandsnaam>
{{originalFilename}}
</bestandsnaam>
{{textContext}}

Antwoord (alleen de submapnaam): ";

            dsp.SpecificSubfolderNamePrompts[Tuple.Create("Belastingen", "aangifte")] =
    $@"Het document is een '{{documentType}}' (categorie: {{category}}). 
Stel een submapnaam voor die het type aangifte en het jaartal aanduidt.
Voorbeeld:
- IB 2023
- BTW Q3 2024

Documentinformatie:
<bestandsnaam>
{{originalFilename}}
</bestandsnaam>
{{textContext}}

Antwoord (alleen de submapnaam, bijv. 'IB 2023'): ";

            dsp.SpecificSubfolderNamePrompts[Tuple.Create("Belastingen", "aanslag")] =
    $@"Het document is een '{{documentType}}' (categorie: {{category}}). 
Stel een submapnaam voor die het type aanslag en het jaartal aanduidt.
Voorbeeld:
- Definitieve Aanslag IB 2022
- Voorlopige Aanslag ZVW 2023

Documentinformatie:
<bestandsnaam>
{{originalFilename}}
</bestandsnaam>
{{textContext}}

Antwoord (alleen de submapnaam): ";

            dsp.SpecificSubfolderNamePrompts[Tuple.Create("Verzekeringen", "polis")] =
    $@"Het document is een '{{documentType}}' (categorie: {{category}}). 
Stel een submapnaam voor die het type verzekering en eventueel de verzekeraar aanduidt.
Voorbeeld:
- Autoverzekering [Verzekeraar]
- Zorgpolis [Jaar]

Documentinformatie:
<bestandsnaam>
{{originalFilename}}
</bestandsnaam>
{{textContext}}

Antwoord (alleen de submapnaam): ";

            dsp.SpecificSubfolderNamePrompts[Tuple.Create("Carrière en Professionele Ontwikkeling", "certificaat")] =
    $@"Het document is een '{{documentType}}' (categorie: {{category}}).
Stel een submapnaam voor die de naam van het certificaat of de training kort beschrijft.
Voorbeeld:
- AZ-900 Microsoft Azure Fundamentals
- Scrum Master PSM I

Documentinformatie:
<bestandsnaam>
{{originalFilename}}
</bestandsnaam>
{{textContext}}

Antwoord (alleen de submapnaam): ";

            dsp.SpecificSubfolderNamePrompts[Tuple.Create("Carrière en Professionele Ontwikkeling", "salaris")] =
    $@"Het document is een '{{documentType}}' (categorie: {{category}}).
Stel een submapnaam voor die de maand en het jaar aanduidt.
Voorbeeld:
- Januari 2024
- December 2023

Documentinformatie:
<bestandsnaam>
{{originalFilename}}
</bestandsnaam>
{{textContext}}

Antwoord (alleen de submapnaam, bijv. 'Maart 2024'): ";
        }

        public static List<string> GetDefinedSubfoldersFor(string mainCategoryFolderName)
        {
            List<string> subfolders; // Declareer lokaal
            if (SubfolderStructure.TryGetValue(mainCategoryFolderName, out subfolders))
            {
                return subfolders;
            }
            return new List<string>();
        }
    }
}