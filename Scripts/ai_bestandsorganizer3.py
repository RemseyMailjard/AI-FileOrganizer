import os
import shutil
import google.generativeai as genai
from google.api_core import exceptions as google_exceptions # For more specific error handling
from docx import Document
from PyPDF2 import PdfReader
from pathlib import Path
from typing import List, Dict, Optional

# --- Configuratie ---
# 🔑 Zet je eigen Google API-key hieronder
# WAARSCHUWING: Het hardcoden van API-keys wordt over het algemeen afgeraden
# voor scripts die gedeeld of in versiebeheer geplaatst worden.
# Zorg ervoor dat je dit script veilig bewaart als je hier je key invult.
GOOGLE_API_KEY = "YOUR_GOOGLE_API_KEY_HERE"  # VERVANG DIT!
if GOOGLE_API_KEY == "YOUR_GOOGLE_API_KEY_HERE" or not GOOGLE_API_KEY:
    print("Kritieke fout: GOOGLE_API_KEY is niet ingesteld in het script.")
    print("Vervang 'YOUR_GOOGLE_API_KEY_HERE' met je daadwerkelijke API-key.")
    exit(1)

try:
    genai.configure(api_key=GOOGLE_API_KEY)
except Exception as e:
    print(f"FOUT: Kan Google Generative AI niet configureren: {e}")
    exit(1)

# Mapinstellingen (gebruik Path voor platformonafhankelijkheid)
# Pas deze paden aan naar jouw situatie
SOURCE_FOLDER = Path(r"C:\Users\Remse\Desktop\Demo")  # Voorbeeld Windows
# SOURCE_FOLDER = Path.home() / "Downloads" # Voorbeeld platformonafhankelijk
DESTINATION_BASE_FOLDER = Path(r"C:\Users\Remse\Desktop\Demo\AI-mappen")  # Voorbeeld Windows
# DESTINATION_BASE_FOLDER = Path.home() / "Documenten" / "AI-mappen" # Voorbeeld

# Categorieën en hun mapnamen
FOLDER_CATEGORIES: Dict[str, str] = {
    "Financiën": "1. Financiën",
    "Belastingen": "2. Belastingen",
    "Verzekeringen": "3. Verzekeringen",
    "Woning": "4. Woning",
    "Gezondheid en Medisch": "5. Gezondheid en Medisch",
    "Familie en Kinderen": "6. Familie en Kinderen",
    "Voertuigen": "7. Voertuigen",
    "Persoonlijke Documenten": "8. Persoonlijke Documenten",
    "Hobbies en interesses": "9. Hobbies en interesses",
    "Carrière en Professionele Ontwikkeling": "10. Carrière en Professionele Ontwikkeling",
    "Bedrijfsadministratie": "11. Bedrijfsadministratie",
    "Reizen en vakanties": "12. Reizen en vakanties",
    # Voeg hier eventueel meer categorieën toe
}
FALLBACK_CATEGORY_NAME = "Overig"  # Categorie voor niet-herkende items
FALLBACK_FOLDER_NAME = f"0. {FALLBACK_CATEGORY_NAME}"

# Gemini Model instellingen
GEMINI_MODEL_NAME = "gemini-1.5-pro-preview-0506"
MAX_TEXT_LENGTH_FOR_LLM = 4000  # Aantal karakters om naar LLM te sturen (Gemini Pro has larger context)
                                # Still, keep it reasonable for classification tasks to save tokens and improve focus.

# Ondersteunde bestandsextensies
SUPPORTED_EXTENSIONS = ('.pdf', '.docx', '.txt', '.md')

# --- Functies ---

def extract_text(file_path: Path) -> str:
    """
    Extraheert tekst uit ondersteunde bestandsformaten.
    Retourneert lege string bij fouten of niet-ondersteunde types.
    """
    text = ""
    try:
        if file_path.suffix.lower() == '.pdf':
            with open(file_path, 'rb') as f:
                reader = PdfReader(f)
                for page in reader.pages:
                    page_text = page.extract_text()
                    if page_text:
                        text += page_text + " "
        elif file_path.suffix.lower() == '.docx':
            doc = Document(file_path)
            text = " ".join(p.text for p in doc.paragraphs if p.text)
        elif file_path.suffix.lower() in ('.txt', '.md'):
            with open(file_path, 'r', encoding='utf-8', errors='replace') as f:
                text = f.read()
        else:
            print(f"INFO: Niet-ondersteund bestandstype overgeslagen: {file_path.name}")

    except Exception as e:
        print(f"WAARSCHUWING: Fout bij lezen van bestand {file_path.name}: {e}")
    return text.strip()

def classify_text_with_gemini(text_to_classify: str, categories: List[str]) -> Optional[str]:
    """
    Vraagt aan Gemini om de tekst in een van de gegeven categorieën in te delen.
    Retourneert de categorienaam of None bij een fout.
    """
    if not text_to_classify:
        print("INFO: Geen tekst om te classificeren.")
        return None

    try:
        model = genai.GenerativeModel(GEMINI_MODEL_NAME)
    except Exception as e:
        print(f"FOUT: Kan Gemini model ('{GEMINI_MODEL_NAME}') niet initialiseren: {e}")
        return None

    category_list_for_prompt = "\n".join(f"- {cat}" for cat in categories)

    prompt = f"""
Je bent een AI-assistent gespecialiseerd in het organiseren van documenten.
Jouw taak is om de volgende tekst te analyseren en te bepalen in welke van de onderstaande categorieën deze het beste past.

Beschikbare categorieën:
{category_list_for_prompt}
- {FALLBACK_CATEGORY_NAME} (gebruik deze als geen andere categorie duidelijk past)

Geef ALLEEN de naam van de gekozen categorie terug, exact zoals deze in de lijst staat. Geen extra uitleg, nummers of opmaak.

Tekstfragment om te classificeren:
---
{text_to_classify[:MAX_TEXT_LENGTH_FOR_LLM]}
---

Categorie:"""

    try:
        generation_config = genai.types.GenerationConfig(
            temperature=0.0,
            max_output_tokens=50  # Max length for the category name
        )
        # For Gemini, safety_settings can be important. Default might be strict.
        # For this task, less likely to trigger, but good to be aware.
        # safety_settings = [
        #     {"category": "HARM_CATEGORY_HARASSMENT", "threshold": "BLOCK_NONE"},
        #     {"category": "HARM_CATEGORY_HATE_SPEECH", "threshold": "BLOCK_NONE"},
        #     {"category": "HARM_CATEGORY_SEXUALLY_EXPLICIT", "threshold": "BLOCK_NONE"},
        #     {"category": "HARM_CATEGORY_DANGEROUS_CONTENT", "threshold": "BLOCK_NONE"},
        # ]

        response = model.generate_content(
            prompt,
            generation_config=generation_config,
            # safety_settings=safety_settings # Uncomment if needed
        )

        # Check for blocks or empty response
        if not response.candidates or response.candidates[0].finish_reason != genai.types.Candidate.FinishReason.STOP:
            block_reason = response.prompt_feedback.block_reason if response.prompt_feedback else "Unknown"
            finish_reason_val = response.candidates[0].finish_reason if response.candidates else "No candidates"
            print(f"WAARSCHUWING: Gemini response mogelijk geblokkeerd of incompleet. Block reason: {block_reason}, Finish Reason: {finish_reason_val}")
            if response.text:
                 print(f"   Ontvangen tekst (ondanks block/incomplete): '{response.text.strip()}'")
            return FALLBACK_CATEGORY_NAME # Or None, depending on how strict you want to be

        chosen_category = response.text.strip()

        valid_categories = categories + [FALLBACK_CATEGORY_NAME]
        if chosen_category in valid_categories:
            return chosen_category
        else:
            print(f"WAARSCHUWING: Gemini retourneerde een onbekende of lege categorie: '{chosen_category}'. Gebruikt fallback.")
            # Attempt to match if Gemini added extra text or slightly misspelled
            for valid_cat in valid_categories:
                if valid_cat.lower() in chosen_category.lower() or chosen_category.lower() in valid_cat.lower():
                    print(f"INFO: Mogelijk bedoelde Gemini: '{valid_cat}'. Gebruikt deze.")
                    return valid_cat
            return FALLBACK_CATEGORY_NAME

    except google_exceptions.InvalidArgument as e:
        print(f"FOUT: Google API Fout (Ongeldig Argument) - Mogelijk probleem met prompt of API key permissies: {e}")
    except google_exceptions.PermissionDenied as e:
        print(f"FOUT: Google API Fout (Toegang Geweigerd) - Controleer API key en model toegang: {e}")
    except google_exceptions.ResourceExhausted as e:
        print(f"FOUT: Google API Fout (Quota Overschreden): {e}")
    except google_exceptions.GoogleAPIError as e:
        print(f"FOUT: Algemene Google API Fout: {e}")
    except Exception as e:
        print(f"FOUT: Onverwachte fout tijdens Gemini-classificatie: {e}")
    return None


def organize_files():
    """
    Doorloopt bestanden in de bronmap, classificeert ze en verplaatst ze.
    """
    if not SOURCE_FOLDER.exists() or not SOURCE_FOLDER.is_dir():
        print(f"FOUT: Bronmap '{SOURCE_FOLDER}' niet gevonden of is geen map.")
        return
    if not DESTINATION_BASE_FOLDER.exists():
        try:
            DESTINATION_BASE_FOLDER.mkdir(parents=True, exist_ok=True)
            print(f"[MAP] Basisdoelmap '{DESTINATION_BASE_FOLDER}' aangemaakt.")
        except OSError as e:
            print(f"FOUT: Fout bij aanmaken basisdoelmap '{DESTINATION_BASE_FOLDER}': {e}")
            return

    print(f"Starten met organiseren van bestanden uit: {SOURCE_FOLDER}")
    print(f"Gebruikt Gemini model: {GEMINI_MODEL_NAME}")

    processed_files = 0
    moved_files = 0

    for item_path in SOURCE_FOLDER.iterdir():
        if item_path.is_file() and item_path.suffix.lower() in SUPPORTED_EXTENSIONS:
            processed_files += 1
            print(f"\n[BESTAND] Verwerken van: {item_path.name}")

            extracted_text = extract_text(item_path)
            if not extracted_text:
                print(f"INFO: Geen tekst geëxtraheerd uit {item_path.name}. Bestand wordt overgeslagen.")
                continue

            llm_category_choice = classify_text_with_gemini(extracted_text, list(FOLDER_CATEGORIES.keys()))

            if llm_category_choice:
                target_folder_name = FOLDER_CATEGORIES.get(llm_category_choice, FALLBACK_FOLDER_NAME)
                target_folder_path = DESTINATION_BASE_FOLDER / target_folder_name

                try:
                    target_folder_path.mkdir(parents=True, exist_ok=True)
                    destination_file_path = target_folder_path / item_path.name

                    if destination_file_path.exists():
                        base, ext = item_path.stem, item_path.suffix
                        counter = 1
                        while destination_file_path.exists():
                            destination_file_path = target_folder_path / f"{base}_{counter}{ext}"
                            counter += 1
                        print(f"INFO: Bestand {item_path.name} bestaat al. Hernoemd naar {destination_file_path.name}")

                    shutil.move(str(item_path), str(destination_file_path))
                    print(f"OK: '{item_path.name}' verplaatst naar '{target_folder_path.relative_to(DESTINATION_BASE_FOLDER)}'")
                    moved_files +=1
                except OSError as e:
                    print(f"FOUT: Fout bij verplaatsen/aanmaken map voor {item_path.name}: {e}")
                except Exception as e:
                    print(f"FOUT: Onverwachte fout bij verwerken {item_path.name}: {e}")
            else:
                print(f"WAARSCHUWING: Kon '{item_path.name}' niet classificeren met Gemini. Wordt niet verplaatst.")
        elif item_path.is_file():
             print(f"INFO: Bestandstype van '{item_path.name}' niet ondersteund. Overgeslagen.")

    print(f"\nOrganisatie voltooid!")
    print(f"Totaal aantal bestanden bekeken (met ondersteunde extensie): {processed_files}")
    print(f"Aantal bestanden succesvol verplaatst: {moved_files}")

# --- Hoofd Uitvoering ---
if __name__ == "__main__":
    organize_files()