# 🗂️ Persoonlijke Mappenstructuur Generator

Deze tool creëert automatisch een overzichtelijke en configureerbare mappenstructuur voor **persoonlijke administratie**. Het helpt je om digitale documenten georganiseerd te bewaren, gericht op particulieren. De structuur wordt gedefinieerd in een `map_structuur.txt` bestand, waardoor je deze eenvoudig kunt aanpassen aan je eigen behoeften.

**In deze repository vind je:**
*   Een direct bruikbare **.EXE versie** (aanbevolen voor de meeste gebruikers).
*   De originele **.BAT script versie** voor degenen die de code willen inzien of aanpassen.

## 📁 Wat doet deze tool?

*   Vraagt naar een **hoofdlocatie** (standaard `Desktop\Persoonlijke Administratie`) en een **doeljaar**.
*   Leest de gewenste mappenstructuur uit het meegeleverde `map_structuur.txt` bestand.
*   Creëert de mappen, waarbij placeholders zoals `!TARGET_YEAR!` en `!PREVIOUS_YEAR!` worden vervangen.
*   Genereert een uitgebreide set aan thematische mappen, bijvoorbeeld voor:
    *   Financiën
    *   Belastingen
    *   Verzekeringen
    *   Woning
    *   Gezondheid
    *   ...en vele andere categorieën.
*   Ondersteunt submappen met jaartallen voor eenvoudig archiveren.
*   Bevat een logbestand voor het traceren van de uitgevoerde acties.

## ▶️ Gebruik (.EXE Versie - Aanbevolen)

1.  Download `PersoonlijkeMappenGenerator.exe` en `map_structuur.txt` uit de [Releases sectie](https://github.com/GEBRUIKERSNAAM/PersoonlijkeMappenGenerator/releases) (of de hoofdmap van deze repository).
2.  **Plaats beide bestanden (`.exe` en `.txt`) in dezelfde map.**
3.  Dubbelklik op `PersoonlijkeMappenGenerator.exe`.
4.  Volg de instructies op het scherm om de locatie en het jaartal te kiezen.
5.  De mappenstructuur wordt aangemaakt volgens de definities in `map_structuur.txt`.

> ℹ️ De .EXE is gemaakt voor Windows en vereist geen installatie. Zorg ervoor dat `map_structuur.txt` aanwezig is in dezelfde map als de .EXE.

## 🔧 Structuur Aanpassen (`map_structuur.txt`)

Je kunt de mappenstructuur volledig naar wens aanpassen door het `map_structuur.txt` bestand te bewerken:
*   Elke regel definieert een map (relatief aan de hoofdmap).
*   Gebruik `/` als padscheidingsteken.
*   Gebruik `!TARGET_YEAR!` voor het gekozen jaartal en `!PREVIOUS_YEAR!` voor het jaar daarvoor.
*   Regels beginnend met `#` of `rem` worden genegeerd.

## 🧑‍💻 Gebruik (.BAT Script Versie - Voor gevorderden)

1.  Download `PersoonlijkeMappenStructuurGenerator.bat` en `map_structuur.txt`.
2.  **Plaats beide bestanden in dezelfde map.**
3.  Dubbelklik op het `.bat` bestand of voer het uit via de command prompt.
4.  Volg de instructies.

## 📷 Screenshot (Voorbeeld van de standaardstructuur)

> Voeg hier een afbeelding toe van (een deel van) de aangemaakte mappenstructuur voor visuele duidelijkheid.

## 📌 Voor wie is dit bedoeld?

*   Iedereen die zijn/haar digitale documenten en administratie overzichtelijk wil organiseren.
*   Personen die een startpunt zoeken voor een goede mappenopzet voor thuisadministratie.
*   Trainers en coaches die digitale vaardigheden aanleren en een praktisch voorbeeld willen geven.

## 💡 Tips

*   Pas `map_structuur.txt` aan om de structuur perfect op jouw situatie af te stemmen.
*   Bewaar de tool en `map_structuur.txt` op een handige plek voor jaarlijks gebruik.

## 📜 Licentie

Dit project is beschikbaar onder de [MIT License](LICENSE).

## ✍️ Auteur

*   **Remsey Mailjard**
    *   IT-trainer | Power Platform ontwikkelaar
    *   [remsey.nl](https://remsey.nl) • [LinkedIn](https://nl.linkedin.com/in/remseymailjard) • [GitHub](https://github.com/RemseyMailjard)