"""
streamlit_organizer_app.py
Run met:
    pip install streamlit google-generativeai python-docx PyPDF2
    streamlit run streamlit_organizer_app.py
"""

import streamlit as st
from pathlib import Path
import shutil
import google.generativeai as genai
from docx import Document
from PyPDF2 import PdfReader

# ---------- Basisconfiguratie ----------
DEFAULT_CATEGORIES = {
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
SUPPORTED_EXTENSIONS = (".pdf", ".docx", ".txt", ".md")
MAX_TEXT_LENGTH_FOR_LLM = 4000


# ---------- Hulpfuncties ----------
def extract_text(file_path: Path) -> str:
    """Haal ruwe tekst uit PDF, DOCX, TXT of MD."""
    try:
        if file_path.suffix.lower() == ".pdf":
            with open(file_path, "rb") as f:
                reader = PdfReader(f)
                return " ".join((page.extract_text() or "") for page in reader.pages)
        if file_path.suffix.lower() == ".docx":
            doc = Document(file_path)
            return " ".join(p.text for p in doc.paragraphs if p.text)
        if file_path.suffix.lower() in (".txt", ".md"):
            return file_path.read_text(encoding="utf-8", errors="ignore")
    except Exception as e:
        st.warning(f"❗ Fout bij lezen {file_path.name}: {e}")
    return ""


def classify_text(text: str, categories, api_key: str, model_name: str) -> str:
    """Gebruik Gemini om tekst in een categorie te plaatsen."""
    if not text:
        return FALLBACK_CATEGORY_NAME

    genai.configure(api_key=api_key)
    model = genai.GenerativeModel(model_name)

    prompt = (
        "Classificeer dit document in één van deze categorieën:\n"
        + "\n".join(f"- {c}" for c in categories)
        + f"\n- {FALLBACK_CATEGORY_NAME} (fallback)\n\n"
        + text[:MAX_TEXT_LENGTH_FOR_LLM]
    )

    try:
        resp = model.generate_content(
            prompt,
            generation_config=genai.types.GenerationConfig(
                temperature=0.0, max_output_tokens=20
            ),
        )
        answer = "".join(
            part.text for part in resp.candidates[0].content.parts if hasattr(part, "text")
        ).strip()
        return answer if answer in categories else FALLBACK_CATEGORY_NAME
    except Exception as e:
        st.warning(f"❗ Gemini-fout: {e}")
        return FALLBACK_CATEGORY_NAME


def organize_files(src: Path, dst_base: Path, api_key: str, model_name: str):
    processed = moved = 0
    log = []

    if not src.exists():
        log.append(f"Bronmap {src} bestaat niet.")
        return processed, moved, log

    dst_base.mkdir(parents=True, exist_ok=True)

    for file in src.iterdir():
        if not file.is_file() or file.suffix.lower() not in SUPPORTED_EXTENSIONS:
            continue

        processed += 1
        text = extract_text(file)
        category = classify_text(text, list(DEFAULT_CATEGORIES.keys()), api_key, model_name)
        target_name = DEFAULT_CATEGORIES.get(category, FALLBACK_FOLDER_NAME)
        target_dir = dst_base / target_name
        target_dir.mkdir(parents=True, exist_ok=True)

        new_path = target_dir / file.name
        counter = 1
        while new_path.exists():
            new_path = target_dir / f"{file.stem}_{counter}{file.suffix}"
            counter += 1

        shutil.move(str(file), str(new_path))
        moved += 1
        log.append(f"{file.name} → {target_name}")

    return processed, moved, log


# ---------- Streamlit UI ----------
st.title("📂 AI File Organizer")

with st.sidebar:
    st.header("Instellingen")
    api_key = st.text_input("Google API-key", type="password")
    model_name = st.text_input("Gemini-model", value="gemini-1.5-pro-latest")
    src_dir = st.text_input("Bronmap", value=str(Path.home() / "Downloads"))
    dst_dir = st.text_input(
        "Doelbasis­map", value=str(Path.home() / "Documents" / "AI-mappen")
    )
    run = st.button("🚀 Organiseer nu")

if run:
    if not api_key:
        st.error("Geef een geldige API-key op.")
    else:
        with st.spinner("Bestanden worden georganiseerd…"):
            n_proc, n_moved, report = organize_files(
                Path(src_dir), Path(dst_dir), api_key, model_name
            )
        st.success(f"Klaar! {n_moved}/{n_proc} bestanden verplaatst.")
        st.write("### Log")
        for entry in report:
            st.write("•", entry)
