import os
import re # Voor het opschonen van mapnamen
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
GOOGLE_API_KEY = "AIzaSyDKbr0af4HYPKDYJfDBn8UisDpLW8nNj5w" # VERVANG DIT MET JE ECHTE API KEY!
if GOOGLE_API_KEY == "YOUR_GOOGLE_API_KEY_HERE" or GOOGLE_API_KEY == "AI" or not GOOGLE_API_KEY:
    print("Kritieke fout: GOOGLE_API_KEY is niet ingesteld of is nog de placeholder in het script.")
    print("Vervang deze met je daadwerkelijke API-key.")
    exit(1)

try:
    genai.configure(api_key=GOOGLE_API_KEY)
except Exception as e:
    print(f"FOUT: Kan Google Generative AI niet configureren: {e}")
    exit(1)

# Mapinstellingen
SOURCE_FOLDER = Path(r"C:\Users\Remse\Documents\AI-mappen334\99._Overig")
DESTINATION_BASE_FOLDER = Path(r"C:\Users\Remse\Desktop\Demo\AI-mappen")

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
}
FALLBACK_CATEGORY_NAME = "Overig"
FALLBACK_FOLDER_NAME = f"0. {FALLBACK_CATEGORY_NAME}"

GEMINI_MODEL_NAME = "gemini-2.5-pro-preview-05-06"
MAX_TEXT_LENGTH_FOR_LLM = 8000 # Lengte voor classificatie
MAX_TEXT_LENGTH_FOR_SUBFOLDER_NAME = 2000 # Kan korter zijn voor submapnaam
SUPPORTED_EXTENSIONS = ('.pdf', '.docx', '.txt', '.md')
MIN_SUBFOLDER_NAME_LENGTH = 3 # Minimale lengte voor een acceptabele submapnaam
MAX_SUBFOLDER_NAME_LENGTH = 50 # Maximale lengte voor een submapnaam

def extract_text(file_path: Path) -> str:
    text = ""
    try:
        if file_path.suffix.lower() == '.pdf':
            with open(file_path, 'rb') as f:
                reader = PdfReader(f)
                if not reader.pages or len(reader.pages) == 0:
                    print(f"WAARSCHUWING: PDF-bestand {file_path.name} bevat geen pagina's of kon niet correct gelezen worden.")
                    return ""
                for page_num, page in enumerate(reader.pages):
                    try:
                        page_text = page.extract_text()
                        if page_text:
                            text += page_text + " "
                    except Exception as page_e:
                        print(f"WAARSCHUWING: Fout bij extraheren tekst van pagina {page_num + 1} in {file_path.name}: {page_e}")
        elif file_path.suffix.lower() == '.docx':
            doc = Document(file_path)
            text = " ".join(p.text for p in doc.paragraphs if p.text)
        elif file_path.suffix.lower() in ('.txt', '.md'):
            with open(file_path, 'r', encoding='utf-8', errors='replace') as f:
                text = f.read()
        else:
            print(f"INFO: Niet-ondersteund bestandstype overgeslagen: {file_path.name}")
    except Exception as e:
        print(f"WAARSCHUWING: Algemene fout bij lezen van bestand {file_path.name}: {e}")
    return text.strip()

def sanitize_folder_name(name: str) -> str:
    """Schoont een voorgestelde mapnaam op en maakt deze geldig."""
    if not name:
        return ""
    # Verwijder ongeldige tekens voor mapnamen (Windows, Linux, macOS)
    name = re.sub(r'[<>:"/\\|?*\x00-\x1F]', '_', name)
    # Verwijder voorloop- en naloopspaties en punten
    name = name.strip('. ')
    # Beperk de lengte
    name = name[:MAX_SUBFOLDER_NAME_LENGTH]
    # Vervang meerdere spaties/underscores door een enkele
    name = re.sub(r'\s+', ' ', name).strip()
    name = re.sub(r'_+', '_', name).strip('_')
    return name

def _call_gemini_api(prompt: str, model_name: str, max_tokens: int = 50, temperature: float = 0.0) -> Optional[str]:
    """Helper functie om Gemini API aan te roepen en response te verwerken."""
    try:
        model = genai.GenerativeModel(model_name)
        generation_config = genai.types.GenerationConfig(
            temperature=temperature,
            max_output_tokens=max_tokens
        )
        response = model.generate_content(
            prompt,
            generation_config=generation_config,
        )

        candidate_available = response.candidates and len(response.candidates) > 0
        
        if not candidate_available:
            block_reason_str = "Onbekend (geen kandidaten)"
            if response.prompt_feedback and response.prompt_feedback.block_reason:
                block_reason_obj = response.prompt_feedback.block_reason
                block_reason_str = block_reason_obj.name if hasattr(block_reason_obj, 'name') else str(block_reason_obj)
            print(f"WAARSCHUWING: Gemini response (model: {model_name}) bevat geen kandidaten. Block reason: {block_reason_str}")
            return None

        first_candidate = response.candidates[0]
        current_finish_reason = first_candidate.finish_reason
        is_normal_stop = hasattr(current_finish_reason, 'name') and current_finish_reason.name == 'STOP'

        extracted_text_from_response = ""
        if hasattr(first_candidate.content, 'parts') and first_candidate.content.parts:
            extracted_text_from_response = "".join(part.text for part in first_candidate.content.parts if hasattr(part, 'text')).strip()
        elif response.text: # Fallback
             extracted_text_from_response = response.text.strip()

        if not is_normal_stop:
            block_reason_str = "Niet van toepassing (geen block)"
            if response.prompt_feedback and response.prompt_feedback.block_reason:
                block_reason_obj = response.prompt_feedback.block_reason
                block_reason_str = block_reason_obj.name if hasattr(block_reason_obj, 'name') else str(block_reason_obj)
            finish_reason_str = current_finish_reason.name if hasattr(current_finish_reason, 'name') else str(current_finish_reason)
            print(f"WAARSCHUWING: Gemini response (model: {model_name}) niet normaal gestopt. Block reason: {block_reason_str}, Finish Reason: {finish_reason_str}")
            if extracted_text_from_response:
                 print(f"   Ontvangen tekst (ondanks ongebruikelijke stop): '{extracted_text_from_response}'")
            return None # Of de deels geëxtraheerde tekst als je dat wilt

        if not extracted_text_from_response:
            print(f"WAARSCHUWING: Gemini (model: {model_name}) retourneerde een lege response na normale stop.")
            return None
        
        return extracted_text_from_response

    except google_exceptions.InvalidArgument as e:
        print(f"FOUT: Google API Fout (Ongeldig Argument) - Model: '{model_name}': {e}")
    except google_exceptions.PermissionDenied as e:
        print(f"FOUT: Google API Fout (Toegang Geweigerd) - Model: '{model_name}': {e}")
    except google_exceptions.ResourceExhausted as e:
        print(f"FOUT: Google API Fout (Quota Overschreden) - Model: '{model_name}': {e}")
    except google_exceptions.GoogleAPIError as e:
        print(f"FOUT: Algemene Google API Fout - Model: '{model_name}': {e}")
    except Exception as e:
        print(f"FOUT: Onverwachte fout tijdens Gemini-aanroep (model: {model_name}): {e}")
    return None


def classify_text_with_gemini(text_to_classify: str, categories: List[str]) -> Optional[str]:
    if not text_to_classify:
        print("INFO: Geen tekst om te classificeren.")
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

    chosen_category = _call_gemini_api(prompt, GEMINI_MODEL_NAME, max_tokens=50, temperature=0.0)

    if not chosen_category:
        print(f"WAARSCHUWING: Gemini kon geen categorie bepalen. Gebruikt fallback.")
        return FALLBACK_CATEGORY_NAME

    valid_categories = categories + [FALLBACK_CATEGORY_NAME]
    if chosen_category in valid_categories:
        return chosen_category
    else:
        print(f"WAARSCHUWING: Gemini retourneerde een onbekende categorie: '{chosen_category}'. Poging tot fuzzy matching.")
        for valid_cat in valid_categories:
            if valid_cat.lower() in chosen_category.lower() or chosen_category.lower() in valid_cat.lower():
                print(f"INFO: Mogelijk bedoelde Gemini: '{valid_cat}'. Gebruikt deze.")
                return valid_cat
        print(f"INFO: Geen match gevonden met fuzzy matching voor '{chosen_category}'. Gebruikt fallback.")
        return FALLBACK_CATEGORY_NAME

def generate_subfolder_name_with_gemini(text_to_analyze: str, original_filename: str) -> Optional[str]:
    if not text_to_analyze:
        print("INFO: Geen tekst om submapnaam te genereren.")
        return None

    prompt = f"""
Je bent een AI-assistent die helpt bij het organiseren van bestanden.
Analyseer de volgende tekst van een document (oorspronkelijke bestandsnaam: "{original_filename}") en stel een KORTE, BESCHRIJVENDE submapnaam voor (maximaal 5 woorden).
Deze submapnaam moet het hoofdonderwerp of de essentie van het document samenvatten.
Voorbeelden van goede submapnamen: "Belastingaangifte 2023", "Hypotheekofferte Rabobank", "Notulen vergadering Project X", "CV Jan Jansen".
Vermijd generieke namen zoals "Document", "Bestand", "Info" of simpelweg een datum zonder context.
Geef ALLEEN de voorgestelde submapnaam terug, zonder extra uitleg of opmaak.

Tekstfragment:
---
{text_to_analyze[:MAX_TEXT_LENGTH_FOR_SUBFOLDER_NAME]}
---

Voorgestelde submapnaam:"""

    suggested_name = _call_gemini_api(prompt, GEMINI_MODEL_NAME, max_tokens=20, temperature=0.2) # Iets meer creativiteit toegestaan

    if suggested_name:
        sanitized_name = sanitize_folder_name(suggested_name)
        # Voeg extra controles toe om te generieke of te korte namen te voorkomen
        if len(sanitized_name) < MIN_SUBFOLDER_NAME_LENGTH or sanitized_name.lower() in ["document", "bestand", "info", "overig", "algemeen"]:
            print(f"INFO: Gemini suggereerde een te generieke/korte submapnaam: '{suggested_name}' (gesaneerd: '{sanitized_name}'). Wordt niet gebruikt.")
            return None
        return sanitized_name
    return None


def organize_files():
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
    files_with_subfolders = 0

    for item_path in SOURCE_FOLDER.iterdir():
        if item_path.is_file() and item_path.suffix.lower() in SUPPORTED_EXTENSIONS:
            processed_files += 1
            print(f"\n[BESTAND] Verwerken van: {item_path.name}")

            extracted_text = extract_text(item_path)
            if not extracted_text:
                print(f"INFO: Geen tekst geëxtraheerd uit {item_path.name}. Bestand wordt overgeslagen.")
                # Optioneel: verplaats naar fallback zonder classificatie
                # target_category_folder_path = DESTINATION_BASE_FOLDER / FALLBACK_FOLDER_NAME
                # target_category_folder_path.mkdir(parents=True, exist_ok=True)
                # try:
                #     shutil.move(str(item_path), str(target_category_folder_path / item_path.name))
                #     print(f"INFO: '{item_path.name}' verplaatst naar '{FALLBACK_FOLDER_NAME}' wegens geen extraheerbare tekst.")
                # except Exception as move_err:
                #     print(f"FOUT: bij verplaatsen van {item_path.name} naar fallback: {move_err}")
                continue

            llm_category_choice = classify_text_with_gemini(extracted_text, list(FOLDER_CATEGORIES.keys()))

            if llm_category_choice:
                target_category_folder_name = FOLDER_CATEGORIES.get(llm_category_choice, FALLBACK_FOLDER_NAME)
                target_category_folder_path = DESTINATION_BASE_FOLDER / target_category_folder_name
                target_category_folder_path.mkdir(parents=True, exist_ok=True) # Maak categoriemap

                final_destination_folder_path = target_category_folder_path
                
                # Probeer een submapnaam te genereren
                print(f"INFO: Poging tot genereren submapnaam voor '{item_path.name}'...")
                subfolder_name_suggestion = generate_subfolder_name_with_gemini(extracted_text, item_path.name)

                if subfolder_name_suggestion:
                    target_subfolder_path = target_category_folder_path / subfolder_name_suggestion
                    final_destination_folder_path = target_subfolder_path # Update de eindbestemming
                    print(f"INFO: Gemini suggereerde submap: '{subfolder_name_suggestion}'")
                    files_with_subfolders += 1
                else:
                    print(f"INFO: Geen geschikte submapnaam gegenereerd. Bestand komt direct in categorie '{target_category_folder_name}'.")

                try:
                    final_destination_folder_path.mkdir(parents=True, exist_ok=True) # Maak (sub)map indien nodig
                    destination_file_path = final_destination_folder_path / item_path.name

                    if destination_file_path.exists():
                        base, ext = item_path.stem, item_path.suffix
                        counter = 1
                        while destination_file_path.exists():
                            destination_file_path = final_destination_folder_path / f"{base}_{counter}{ext}"
                            counter += 1
                        print(f"INFO: Bestand {item_path.name} bestaat al op doel. Hernoemd naar {destination_file_path.name}")

                    shutil.move(str(item_path), str(destination_file_path))
                    relative_path = destination_file_path.relative_to(DESTINATION_BASE_FOLDER)
                    print(f"OK: '{item_path.name}' verplaatst naar '{relative_path}'")
                    moved_files +=1
                except OSError as e:
                    print(f"FOUT: Fout bij verplaatsen/aanmaken map voor {item_path.name}: {e}")
                except Exception as e:
                    print(f"FOUT: Onverwachte fout bij verwerken {item_path.name}: {e}")
            else:
                print(f"WAARSCHUWING: Kon '{item_path.name}' niet classificeren met Gemini (retourneerde None). Wordt niet verplaatst.")
        elif item_path.is_file():
             print(f"INFO: Bestandstype van '{item_path.name}' niet ondersteund. Overgeslagen.")

    print(f"\nOrganisatie voltooid!")
    print(f"Totaal aantal bestanden bekeken (met ondersteunde extensie): {processed_files}")
    print(f"Aantal bestanden succesvol verplaatst: {moved_files}")
    print(f"Aantal bestanden geplaatst in een AI-gegenereerde submap: {files_with_subfolders}")


if __name__ == "__main__":
    organize_files()