# AI File Organizer 🚀

![AI File Organizer Screenshot (GIF Demo)](https://github.com/RemseyMailjard/PersoonlijkeMappenGenerator/raw/main/PersoonlijkeMappenStructuurGenerator.gif)

Een slimme, gebruiksvriendelijke applicatie om je digitale documenten **automatisch te organiseren en te hernoemen** met behulp van AI (Gemini, OpenAI, Azure OpenAI).

---

## 📋 Functies

- **AI-gestuurde classificatie:** Bestanden worden automatisch ingedeeld in logische (sub)categorieën.
- **AI-voorstellen voor submappen en bestandsnamen:** Hernoemopties en slimme structuur op basis van inhoud.
- **Ondersteuning voor Gemini, OpenAI en Azure OpenAI.**
- **Gebruiksvriendelijke interface met drag & drop.**
- **Live voortgang en uitgebreide logging.**
- **Annuleren van processen mogelijk.**

---

## 📌 Vereisten

- Windows 10 of hoger
- .NET Framework 4.8 (installatie wordt automatisch aangeboden)
- Geldige API-key voor Gemini (Google), OpenAI of Azure OpenAI
- **Billing (facturatie) moet zijn ingeschakeld** bij je AI-provider voor het gebruik van de AI-modellen
- Internetverbinding

---

## 📦 Installatie

1. Download de installer uit de [`installer`](installer) folder:  
   [AIFileOrganizerSetup.exe](installer/AIFileOrganizerSetup.exe)
2. Dubbelklik op het installatiebestand en volg de installatie-wizard.
3. Start de applicatie via het bureaublad of Startmenu.

> **Let op:** Tijdens installatie kan om administratorrechten worden gevraagd.

---

## 🗝️ API-key instellen

1. Start **AI File Organizer**.
2. Vul je API-key in (voor Gemini, OpenAI, of Azure).
3. Voor Azure OpenAI: vul ook het endpoint in.
4. Selecteer het gewenste AI-model.
5. Sla de instellingen op.

> **Let op:**  
> Bij veel AI-providers (zoals Google Gemini en OpenAI) moet je **billing/tegoed activeren** om de API's te gebruiken, ook bij gratis of lage volumes.

Zie [installatie-informatie.txt](installer/installatie-informatie.txt) voor stapsgewijze uitleg over het aanvragen en gebruiken van je API-key.

---

## ⚡ Gebruik

1. **Selecteer een bronmap** met te ordenen bestanden.
2. **Kies een doelmap** voor de georganiseerde output.
3. (Optioneel) Zet aan of bestanden automatisch worden hernoemd door AI.
4. Klik op **Start** en volg de voortgang in de applicatie.
5. Bekijk het logboek of sla dit op als tekstbestand.

---

## 📂 Ondersteunde bestandstypen

- PDF (`.pdf`)
- Word-documenten (`.docx`)
- Tekstbestanden (`.txt`, `.md`)

---

## 🗃️ Mapcategorieën

Bestanden worden automatisch gecategoriseerd in onder andere:

- Financiën
- Belastingen
- Verzekeringen
- Woning
- Gezondheid & Medisch
- Familie & Kinderen
- Voertuigen
- Persoonlijke documenten
- Hobbies & interesses
- Carrière / Werk
- Bedrijfsadministratie
- Reizen & vakanties
- Overig

De applicatie creëert automatisch relevante submappen als de AI extra details detecteert.

---

## 📑 Projectstructuur (voor ontwikkelaars)

- `installer/` — Setup, licentie, info-bestanden
- `bin/Release/` — Gebouwde applicatie
- `src/` — Broncode
- `README.md`, `LICENSE.txt`, etc.

---

## 🛠️ Gebruikte componenten

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

Veel plezier met organiseren! 🎉

© 2025 Remsey Mailjard | AI File Organizer
