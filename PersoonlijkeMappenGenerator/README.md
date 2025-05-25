# 🗂️ Persoonlijke Mappenstructuur Generator

Maak in één klik een overzichtelijke, aanpasbare folderstructuur voor je **persoonlijke administratie**!  
Deze tool is ideaal voor iedereen die zijn digitale documenten wil organiseren – thuis, als professional, of als trainer/coach.

---

## 📸 Voorbeelden & Screenshots

Hieronder zie je hoe de Persoonlijke Mappenstructuur Generator en de alternatieven eruitzien in de praktijk:

### 🖥️ Standaard mappenstructuur (.EXE/.BAT output)
![Standaard mappenstructuur](https://github.com/RemseyMailjard/PersoonlijkeMappenGenerator/raw/main/PersoonlijkeMappenStructuurGenerator.gif)
<sup>Automatisch aangemaakte folderstructuur op je Desktop.</sup>

### ⚙️ Command-line interface (Batch Script)
![CLI gebruik](https://github.com/RemseyMailjard/PersoonlijkeMappenGenerator/raw/main/screenshot_cli.png)
<sup>Zo ziet de tool eruit bij het starten van de batch-versie.</sup>

### 🤖 AI File Organizer (Windows-app)
![AI Organizer GUI](https://github.com/RemseyMailjard/AI-FileOrganizer2/raw/nieuwStructuur/screenshot_ai_fileorganizer.png)
<sup>Interface en voorbeeld van de AI File Organizer-applicatie.</sup>

---

## 🚀 Snelstart: Welke versie past bij jou?

- **Windows .EXE (Aanbevolen)**
  - Direct klaar voor gebruik, geen installatie nodig.
  - 👉 [Download de laatste versie (.exe) via Releases](https://github.com/RemseyMailjard/PersoonlijkeMappenGenerator/releases)

- **.BAT Script (Voor gevorderden)**
  - Biedt flexibiliteit en kan worden aangepast. Voor de meest uitgebreide aanpasbaarheid via `map_structuur.txt`, zorg dat je een versie van het script gebruikt die dit ondersteunt (zoals de functionaliteit beschreven voor de .EXE, of het `test2.bat` script in de repository).
  - 👉 [Download het .bat script (PersoonlijkeMappenStructuurGenerator.bat)](https://github.com/RemseyMailjard/PersoonlijkeMappenGenerator/blob/main/PersoonlijkeMappenStructuurGenerator.bat)
  - ℹ️ *Indien je de `map_structuur.txt` wilt gebruiken met een .BAT script, controleer of het script deze functionaliteit heeft. Het `test2.bat` script in de repository demonstreert deze geavanceerde werking.*

- **AI File Organizer (Pro, API-key vereist)**
  - Laat bestanden sorteren, hernoemen en classificeren met AI.
  - 👉 [Bekijk op GitHub](https://github.com/RemseyMailjard/AI-FileOrganizer2/tree/nieuwStructuur)
  - 👉 [Download direct als .zip](https://github.com/RemseyMailjard/AI-FileOrganizer2/raw/nieuwStructuur/AIFileOrganizerLinkedIn.zip)
  - ℹ️ *OpenAI API key vereist voor volledige functionaliteit.*

- **File Organizer GPT (Interactief via ChatGPT)**
  - Laat je mappenstructuur samenstellen via een AI-chat, geen technische kennis nodig.
  - 👉 [Probeer direct in ChatGPT (File Organizer GPT)](https://chatgpt.com/g/g-6831bc2bcfd08191867ae093ab9f6de3-file-organizer)
  - ℹ️ *Gratis versie van ChatGPT is beperkt in berichten en uploads; Plus-abonnement aanbevolen.*

---

## 📁 Wat doet deze tool?

- Vraagt naar een **hoofdlocatie** (standaard: `Desktop\Persoonlijke Administratie`) en een **doeljaar**.
- De `.exe`-versie en sommige `.bat`-versies (zoals `test2.bat` in deze repository) gebruiken een flexibel `map_structuur.txt` bestand om jouw eigen mappenstructuur te bepalen (zie onderaan).
- Maakt een thematisch complete folderstructuur voor administratie, o.a.:
    - Financiën
    - Belastingen
    - Verzekeringen
    - Woning
    - Gezondheid
    - ...en meer!
- Plaatst submappen per jaartal voor eenvoudig archiveren.
- Logt alle acties in een logbestand (`%TEMP%\PersoonlijkeMappenStructuurGenerator_log.txt`) zodat je altijd ziet wat er is aangemaakt.

---

## ▶️ Installeren & Gebruiken (.EXE Versie - Aanbevolen)

1. Download `PersoonlijkeMappenGenerator.exe` uit de [Releases sectie](https://github.com/RemseyMailjard/PersoonlijkeMappenGenerator/releases).
2. Dubbelklik op het bestand om te starten.
3. Volg de instructies om de locatie en het jaartal te kiezen. De standaardstructuur wordt automatisch toegepast.

**Eigen structuur gebruiken?**
1. Download (of maak) een `map_structuur.txt` bestand. Een voorbeeld vind je in deze repository.
2. Plaats zowel het `.exe` bestand als je `map_structuur.txt` bestand in dezelfde map.
3. Start de tool. De structuur uit het tekstbestand wordt gebruikt.

---

## 🔧 Structuur aanpassen (`map_structuur.txt`)

- Bewerk of maak eenvoudig je eigen structuur in het tekstbestand:
    - Elke regel = een submap (relatief aan de hoofdmap)
    - Gebruik `/` als padscheidingsteken.
    - `!TARGET_YEAR!` en `!PREVIOUS_YEAR!` voor jaartal-plaatsing.
    - Regels beginnend met `#` of `rem` en lege regels worden genegeerd.

---

## 🧑‍💻 Gebruik (.BAT Script Versie)

1. Download een `.bat` script, bijvoorbeeld `PersoonlijkeMappenStructuurGenerator.bat`. Je vindt ook `map_structuur.txt` in de repository.
2. **Om `map_structuur.txt` te gebruiken (aanbevolen voor volledige flexibiliteit):**
    a. Zorg ervoor dat het `.bat` script dat je gebruikt `map_structuur.txt` ondersteunt. De `.exe`-versie doet dit, en het `test2.bat`-script in deze repository is een voorbeeld van een `.bat` script met deze functionaliteit.
    b. Plaats het `.bat` script en `map_structuur.txt` in dezelfde map.
    c. Pas de gewenste folderstructuur aan in `map_structuur.txt`.
3. **Indien het gebruikte `.bat` script `map_structuur.txt` niet ondersteunt, of als je `map_structuur.txt` niet in dezelfde map plaatst:**
    - Het script zal een ingebouwde (mogelijk beperktere) mappenstructuur aanmaken.
4. Dubbelklik op het `.bat` bestand of start het via de command prompt.
5. Volg de instructies. De gemaakte mappen en eventuele meldingen worden gelogd in `%TEMP%\PersoonlijkeMappenStructuurGenerator_log.txt`.

---

## 📦 Bestanden in deze Repository

- **`README.md`**: Dit informatiedocument.
- **`map_structuur.txt`**: Het sjabloonbestand dat je kunt aanpassen om je eigen mappenstructuur te definiëren. Wordt gebruikt door de `.exe`-versie en door scripts die deze functionaliteit implementeren (zie `test2.bat` voor een voorbeeldimplementatie).
- **`PersoonlijkeMappenStructuurGenerator.bat`**: Het primaire batch-script. Afhankelijk van de versie/update-status, kan de ondersteuning voor `map_structuur.txt` variëren.
- **`test2.bat`**: Een alternatief/ontwikkel batch-script dat uitgebreide ondersteuning voor `map_structuur.txt` demonstreert en een fallback naar een interne structuur heeft.
- **`Persoonlijke Administratie.zip`**: Een .zip-bestand, mogelijk met een voorbeeld van een gegenereerde structuur of een oudere distributie. Voor de laatste `.exe`-versie, zie de [Releases sectie](https://github.com/RemseyMailjard/PersoonlijkeMappenGenerator/releases).

---

## ✨ Geavanceerd: Bestandsorganisatie met AI

Wil je meer dan alleen mappenstructuren aanmaken?  
Gebruik dan **AI File Organizer** voor automatisch sorteren, hernoemen en categoriseren van al je documenten met behulp van AI.

- Analyseert bestandsinhoud (PDF, DOCX, TXT, MD)
- Classificeert en verplaatst automatisch naar relevante categorieën
- AI-voorstellen voor bestandsnamen en submappen
- Ondersteuning voor Google Gemini, OpenAI en Azure OpenAI

👉 [Bekijk op GitHub](https://github.com/RemseyMailjard/AI-FileOrganizer2/tree/nieuwStructuur)  
👉 [Direct downloaden (.zip)](https://github.com/RemseyMailjard/AI-FileOrganizer2/raw/nieuwStructuur/AIFileOrganizerLinkedIn.zip)

---

## 🤖 Alternatief: File Organizer GPT

Wil je via een interactieve chat en zonder technische kennis je mappenstructuur organiseren?  
Gebruik dan mijn **File Organizer GPT** (gebaseerd op ChatGPT):

👉 [Start direct met de File Organizer GPT](https://chatgpt.com/g/g-6831bc2bcfd08191867ae093ab9f6de3-file-organizer)

> ℹ️ De gratis versie van ChatGPT staat minder berichten en uploads toe. Voor langere sessies en meer uploads wordt **ChatGPT Plus** aanbevolen.

---

## 📌 Voor wie is dit bedoeld?

- Iedereen die digitale documenten en administratie overzichtelijk wil organiseren
- Personen die een goede start willen maken met hun eigen mappenopzet
- Trainers, coaches en docenten die een praktisch voorbeeld zoeken voor digitale organisatie

---

## 📜 Licentie

Dit project is beschikbaar onder de [MIT License](LICENSE).

---

## ✍️ Auteur

- **Remsey Mailjard**
    - IT-trainer | Power Platform ontwikkelaar
    - [remsey.nl](https://remsey.nl) • [LinkedIn](https://nl.linkedin.com/in/remseymailjard) • [GitHub](https://github.com/RemseyMailjard)

---

## 💬 Feedback & Contact

Heb je vragen, feedback of suggesties voor verbetering?  
Laat het weten via een GitHub issue of stuur me een bericht via LinkedIn – ik hoor graag van je!
