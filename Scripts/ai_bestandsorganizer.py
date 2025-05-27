
import os
import shutil
import openai
from docx import Document
from PyPDF2 import PdfReader

# 🔑 Zet je eigen OpenAI API-key hieronder
openai.api_key = "YOUR_OPENAI_API_KEY"

# 📁 Mapinstellingen
source_folder = r"C:\Gebruiker\Downloads"  # Pas aan
destination_base = r"C:\Gebruiker\Documenten\AI-mappen"  # Pas aan

# 📚 Extractie van tekst uit diverse bestandsformaten
def extract_text(file_path):
    try:
        if file_path.endswith('.pdf'):
            with open(file_path, 'rb') as f:
                reader = PdfReader(f)
                return " ".join(page.extract_text() or "" for page in reader.pages)
        elif file_path.endswith('.docx'):
            doc = Document(file_path)
            return " ".join(p.text for p in doc.paragraphs)
        elif file_path.endswith(('.txt', '.md')):
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                return f.read()
    except Exception as e:
        print(f"Fout bij lezen van bestand {file_path}: {e}")
        return ""

# 🤖 Vraag aan GPT: welke map past het best?
def classify_text_with_gpt(text):
    prompt = f"""
Je bent een AI-organizer die bestanden indeelt in één van de volgende categorieën:

1. Financiën
2. Belastingen
3. Verzekeringen
4. Woning
5. Gezondheid en Medisch
6. Familie en Kinderen
7. Voertuigen
8. Persoonlijke Documenten
9. Hobbies en interesses
10. Carrière en Professionele Ontwikkeling
11. Bedrijfsadministratie
12. Reizen en vakanties

Geef alleen de categorie terug, zonder toelichting. Hier is een voorbeeldtekst:

{text[:1000]}
"""

    response = openai.ChatCompletion.create(
        model="gpt-4",
        messages=[{"role": "user", "content": prompt}],
        temperature=0
    )
    return response['choices'][0]['message']['content'].strip()

# 🔄 Verwerk bestanden
def organize_files():
    for filename in os.listdir(source_folder):
        if filename.endswith(('.pdf', '.docx', '.txt', '.md')):
            full_path = os.path.join(source_folder, filename)
            text = extract_text(full_path)
            category = classify_text_with_gpt(text)
            target_folder = os.path.join(destination_base, f"{list(folder_mapping.keys()).index(category)+1}. {category}")

            if not os.path.exists(target_folder):
                os.makedirs(target_folder)

            shutil.move(full_path, os.path.join(target_folder, filename))
            print(f"✅ {filename} verplaatst naar {target_folder}")

# ✅ Mappenmapping
folder_mapping = {
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
    "Reizen en vakanties": "12. Reizen en vakanties"
}

organize_files()
