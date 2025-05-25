# AI File Organizer 🚀

![AI File Organizer Screenshot](https://via.placeholder.com/800x450?text=Plaats+hier+een+screenshot+van+de+UI)

Een slimme, gebruiksvriendelijke applicatie om je digitale documenten **automatisch te organiseren** en te hernoemen met behulp van AI (Gemini, OpenAI, Azure OpenAI).

---

## 📋 Functies

- **AI-gestuurde classificatie:** Bestanden worden automatisch ingedeeld in slimme categorieën en submappen.
- **AI-suggesties voor bestandsnamen:** Ontvang hernoem-voorstellen op basis van inhoud.
- **Ondersteuning voor Gemini, OpenAI en Azure OpenAI.**
- **Moderne en eenvoudige interface.**
- **Uitgebreide logging en voortgangsweergave.**
- **Drag & drop-bestanden en mapselectie.**

---

## 📌 Vereisten

- Windows 10 of hoger
- .NET Framework 4.8 (installatie wordt automatisch aangeboden)
- Geldige API-key voor Gemini (Google), OpenAI of Azure OpenAI
- Internetverbinding

---

## 📦 Installatie

1. Download de installer uit de [`installer`](installer) folder:  
   [AIFileOrganizerSetup.exe](installer/AIFileOrganizerSetup.exe)
2. Dubbelklik op de `.exe` en volg de installatie-wizard.
3. Start de applicatie via het bureaublad of Startmenu.

> **Let op:** Tijdens installatie kan om administratorrechten worden gevraagd.

---

## 🗝️ API-key instellen

1. Start **AI File Organizer**.
2. Vul je API-key in (voor Gemini, OpenAI, of Azure).
3. Voor Azure OpenAI: vul ook het endpoint in.
4. Selecteer het gewenste AI-model.

Zie [installatie-informatie.txt](installer/installatie-informatie.txt) voor hulp bij het aanvragen van je API-key.

---

## ⚡ Gebruik

1. **Selecteer een bronmap** met te ordenen bestanden.
2. **Kies een doelmap** voor de georganiseerde output.
3. (Optioneel) Zet aan of bestanden automatisch worden hernoemd door AI.
4. Klik op **Start** en volg de voortgang.
5. Controleer het logboek in de applicatie of sla het op als tekstbestand.

---

## 📂 Ondersteunde bestandstypen

- PDF (`.pdf`)
- Word (`.docx`)
- Tekst (`.txt`, `.md`)

---

## 🗃️ Mapcategorieën

Bestanden worden automatisch gecategoriseerd in o.a.:

- Financiën
- Belastingen
- Verzekeringen
- Woning
- Gezondheid/Medisch
- Familie/Kids
- Voertuigen
- Persoonlijke documenten
- Hobbies & interesses
- Carrière
- Bedrijfsadministratie
- Reizen/vakantie
- Overig

(De app creëert submappen als de AI relevante details detecteert.)

---

## 📑 Projectstructuur (voor ontwikkelaars)

- `installer/` — Setup, licentie, info-bestanden
- `bin/Release/` — Gebouwde applicatie
- `src/` — Broncode
- `README.md`, `LICENSE.txt`, etc.

---

## 🛠️ Credits & Componenten

Gebouwd met:
- PdfPig (PDF-extractie)
- DocumentFormat.OpenXml (Word-extractie)
- Microsoft.WindowsAPICodePack (moderne dialogs)
- Gemini, OpenAI, Azure.AI.OpenAI, Newtonsoft.Json

---

## 📞 Support & Feedback

- **LinkedIn:** [Remsey Mailjard](https://www.linkedin.com/in/remseymailjard/)
- **Website:** [remsey.nl](https://www.remsey.nl)

---

## 📄 Licentie

Open source onder de [MIT-licentie](installer/LICENSE.txt).

---

© 2025 Remsey Mailjard | AI File Organizer

Dit project is een Windows Forms-applicatie (.NET Framework 4.8) die is ontworpen om uw digitale documenten automatisch te organiseren met behulp van kunstmatige intelligentie. Het analyseert de inhoud van uw bestanden (PDF, DOCX, TXT, MD) en verplaatst ze naar vooraf gedefinieerde, logische categoriefolders, inclusief de mogelijkheid om AI-gegenereerde submappen en bestandsnamen voor te stellen.

## Inhoudsopgave

*   [Functies](#functies)
*   [Vereisten](#vereisten)
*   [Installatie](#installatie)
*   [API-sleutel instellen](#api-sleutel-instellen)
    *   [Google Gemini API-sleutel](#google-gemini-api-sleutel)
    *   [OpenAI API-sleutel](#openai-api-sleutel)
    *   [Azure OpenAI API-sleutel](#azure-openai-api-sleutel)
*   [Gebruik](#gebruik)
    *   [Stap 1: Applicatie starten](#stap-1-applicatie-starten)
    *   [Stap 2: API-provider en -model selecteren](#stap-2-api-provider-en--model-selecteren)
    *   [Stap 3: Mappen configureren](#stap-3-mappen-configureren)
    *   [Stap 4: Bestanden hernoemen (optioneel)](#stap-4-bestanden-hernoemen-optioneel)
    *   [Stap 5: Organisatie starten](#stap-5-organisatie-starten)
    *   [Stap 6: Voortgang en logboek](#stap-6-voortgang-en-logboek)
*   [Ondersteunde bestandstypen](#ondersteunde-bestandstypen)
*   [Voorgedefinieerde mapcategorieën](#voorgedefinieerde-mapcategorieën)
*   [Projectstructuur](#projectstructuur)
*   [Credits](#credits)

## Functies

*   **AI-gestuurde classificatie**: Automatische categorisatie van documenten in vooraf gedefinieerde mappen.
*   **Intelligente submap-suggesties**: De AI stelt beschrijvende submapnamen voor op basis van de inhoud.
*   **AI-gegenereerde bestandsnamen**: Mogelijkheid om bestandsnamen te hernoemen met AI-suggesties (met gebruikersbevestiging).
*   **Ondersteuning voor meerdere AI-providers**: Kies tussen Google Gemini, OpenAI (via openai.com) en Azure OpenAI.
*   **Robuuste tekstextractie**: Extraheert tekst uit PDF-, DOCX-, TXT- en MD-bestanden, inclusief verbeterde lay-outanalyse voor PDF's.
*   **Moderne UI-dialoogvensters**: Gebruikt moderne Windows-dialoogvensters voor map- en bestandsselectie/opslaan.
*   **Uitgebreide logging**: Gedetailleerde logboeken van het organisatieproces, direct zichtbaar in de UI en opslaanbaar naar een bestand.
*   **Annulering van processen**: Mogelijkheid om een lopend organisatieproces te stoppen.

## Vereisten

*   **Besturingssysteem**: Windows 10 of nieuwer.
*   **.NET Framework**: .NET Framework 4.8 Runtime geïnstalleerd.
*   **Internetverbinding**: Vereist voor communicatie met de AI-API's.
*   **API-sleutel**: Een geldige API-sleutel voor de gekozen AI-provider (Google Gemini, OpenAI of Azure OpenAI). Zie [API-sleutel instellen](#api-sleutel-instellen) voor instructies.

## Installatie

Dit project is een Visual Studio-oplossing. Volg deze stappen om het te installeren en te draaien:

1.  **Kloon de repository**:
    ```bash
    git clone https://github.com/remseymailjard/remseymailjard-ai-fileorganizer2.git
    cd remseymailjard-ai-fileorganizer2
    ```
2.  **Open in Visual Studio**: Open het `AI-FileOrganizer2.sln` bestand in Visual Studio (Visual Studio 2019 of nieuwer wordt aanbevolen voor .NET Framework 4.8 projecten).
3.  **Herstel NuGet-pakketten**: Visual Studio zou automatisch de benodigde NuGet-pakketten moeten herstellen bij het openen van de oplossing. Als dit niet gebeurt, klik dan met de rechtermuisknop op de oplossing in Solution Explorer en kies "Restore NuGet Packages".
    *   **Belangrijke NuGet-pakketten**: Dit project maakt gebruik van:
        *   `PdfPig` (voor PDF-extractie)
        *   `DocumentFormat.OpenXml` (voor DOCX-extractie)
        *   `Microsoft.WindowsAPICodePack.Shell` en `Microsoft.WindowsAPICodePack.Core` (voor moderne dialoogvensters)
        *   `Google.GenerativeAI` (voor Gemini API)
        *   `OpenAI` (voor OpenAI API)
        *   `Azure.AI.OpenAI` (voor Azure OpenAI API)
        *   `Newtonsoft.Json` (voor JSON-serialisatie/deserialisatie)
4.  **Bouw de oplossing**: Klik in Visual Studio op "Build" > "Build Solution" (of druk op `F6`).
5.  **Start de applicatie**: Nadat de build is voltooid, kunt u de applicatie starten door op `F5` te drukken of door te navigeren naar de `bin\Debug` (of `bin\Release`) map in uw projectdirectory en `AI-FileOrganizer2.exe` uit te voeren.

## API-sleutel instellen

U hebt een API-sleutel nodig van de door u gekozen AI-provider om de applicatie te kunnen gebruiken. De applicatie ondersteunt Google Gemini, OpenAI en Azure OpenAI.

**Beveiligingstip**: Bewaar uw API-sleutels altijd veilig en deel ze nooit met anderen.

### Google Gemini API-sleutel

1.  **Ga naar Google AI Studio**: Open uw webbrowser en ga naar [https://aistudio.google.com/](https://aistudio.google.com/).
2.  **Log in**: Log in met uw Google-account.
3.  **Navigeer naar API-sleutels**: Klik in het linkernavigatiemenu op "Get API key" (of "API keys" als u er al een heeft).
4.  **Maak een nieuwe API-sleutel aan**: Klik op "Create API key in new project" of "Create API key".
5.  **Kopieer de sleutel**: Kopieer de gegenereerde API-sleutel en plak deze in het `Google API Key:` veld in de applicatie.

### OpenAI API-sleutel

1.  **Ga naar het OpenAI-platform**: Open uw webbrowser en ga naar [https://platform.openai.com/](https://platform.openai.com/).
2.  **Log in**: Log in met uw OpenAI-account.
3.  **Navigeer naar API-sleutels**: Klik op uw profielpictogram (rechtsboven) en selecteer "View API keys".
4.  **Maak een nieuwe sleutel aan**: Klik op "Create new secret key". Geef de sleutel eventueel een naam voor herkenbaarheid.
5.  **Kopieer de sleutel**: Kopieer de **geheime** sleutel die wordt weergegeven. Deze wordt slechts één keer getoond. Plak deze in het `OpenAI API Key:` veld in de applicatie.

### Azure OpenAI API-sleutel

Azure OpenAI vereist een Azure-abonnement en de implementatie van een OpenAI-model in uw Azure-resource.

1.  **Meld u aan bij Azure Portal**: Ga naar [https://portal.azure.com/](https://portal.azure.com/) en log in.
2.  **Maak een Azure OpenAI Service-resource aan**:
    *   Zoek in de zoekbalk naar "Azure OpenAI".
    *   Klik op "Create Azure OpenAI".
    *   Volg de stappen om een nieuwe resource te maken (kies een abonnement, resourcegroep, regio en naam).
    *   Zorg ervoor dat u aanvraagt voor toegang tot de Azure OpenAI service, aangezien deze beperkt is.
3.  **Implementeer een model**:
    *   Navigeer naar uw zojuist gemaakte Azure OpenAI resource.
    *   Klik in het linkernavigatiemenu onder "Resource Management" op "Model deployments".
    *   Klik op "Manage deployments" om naar Azure OpenAI Studio te gaan.
    *   Klik in Azure OpenAI Studio op "Deployments" > "Create new deployment".
    *   Kies een model (bijv. `gpt-4o`, `gpt-35-turbo`) en geef het een "Deployment name" (bijv. `my-gpt4o-deployment`). Dit is de naam die u in de applicatie als "Model" selecteert.
4.  **Verzamel de API-sleutel en Endpoint**:
    *   Ga terug naar uw Azure OpenAI resource in Azure Portal.
    *   Klik in het linkernavigatiemenu onder "Resource Management" op "Keys and Endpoint".
    *   U ziet hier twee sleutels (KEY 1, KEY 2) en een Endpoint.
    *   **Kopieer één van de sleutels** (bijv. KEY 1) en plak deze in het `Azure OpenAI API Key:` veld in de applicatie.
    *   **Kopieer de Endpoint URL** en plak deze in het `Azure Endpoint:` veld in de applicatie.
    *   **Onthoud de "Deployment name"** die u heeft gekozen in stap 3. Dit is de waarde die u in het `Model:` dropdown-menu in de applicatie kiest wanneer u "Azure OpenAI" als provider selecteert.

## Gebruik

### Stap 1: Applicatie starten

Start `AI-FileOrganizer2.exe` vanuit de `bin\Debug` of `bin\Release` map.

### Stap 2: API-provider en -model selecteren

1.  **Provider selecteren**: Kies uw AI-provider uit het `Provider:` dropdown-menu (Gemini (Google), OpenAI (openai.com), of Azure OpenAI).
2.  **API Key en Endpoint (indien van toepassing)**: Vul uw API-sleutel in het `API Key:` veld. Als u Azure OpenAI hebt geselecteerd, vul dan ook de `Azure Endpoint:` URL in.
3.  **Model selecteren**: Kies het specifieke AI-model dat u wilt gebruiken uit het `Model:` dropdown-menu.

### Stap 3: Mappen configureren

1.  **Bronmap (`Source Folder`)**: Klik op "Select Source" om de map te kiezen die de bestanden bevat die u wilt organiseren. De applicatie scant alle submappen in deze bronmap.
2.  **Doelmap (`Destination Folder`)**: Klik op "Select Destination" om de map te kiezen waar de georganiseerde bestanden naartoe moeten worden verplaatst. De applicatie creëert hierin de categoriefolders en, indien gewenst, submappen.

### Stap 4: Bestanden hernoemen (optioneel)

*   Schakel het selectievakje `Bestandsnamen AI hernoemen` in als u wilt dat de AI suggesties doet voor nieuwe, meer beschrijvende bestandsnamen. Wanneer dit is ingeschakeld, krijgt u voor elk bestand een pop-up waarin u de AI-suggestie kunt accepteren, bewerken of overslaan.

### Stap 5: Organisatie starten

*   Klik op de knop **"Start"** om het organisatieproces te starten.

### Stap 6: Voortgang en logboek

*   **Voortgangsbalk**: Monitor de voortgang onderaan het venster.
*   **Logboek (`rtbLog`)**: Gedetailleerde informatie over elke stap van het proces (extractie, classificatie, verplaatsing, hernoeming) wordt in realtime weergegeven.
*   **"Stop" knop**: U kunt het proces op elk moment onderbreken door op de "Stop" knop te klikken.
*   **"Log Opslaan" knop**: Na voltooiing (of annulering) van het proces, kunt u het volledige logboek opslaan naar een tekstbestand via de "Log Opslaan" knop.

## Ondersteunde bestandstypen

De AI File Organizer kan tekst extraheren uit de volgende bestandstypen:

*   `.pdf` (Portable Document Format)
*   `.docx` (Microsoft Word Document)
*   `.txt` (Plain Text File)
*   `.md` (Markdown File)

## Voorgedefinieerde mapcategorieën

De applicatie probeert bestanden in een van de volgende hoofdcategorieën te plaatsen. Als geen duidelijke match wordt gevonden, wordt het bestand in de 'Overig' map geplaatst.

*   1. Financiën
*   2. Belastingen
*   3. Verzekeringen
*   4. Woning
*   5. Gezondheid en Medisch
*   6. Familie en Kinderen
*   7. Voertuigen
*   8. Persoonlijke Documenten
*   9. Hobbies en interesses
*   10. Carrière en Professionele Ontwikkeling
*   11. Bedrijfsadministratie
*   12. Reizen en vakanties
*   0. Overig (Fallback-categorie)

## Projectstructuur
