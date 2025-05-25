@echo off
rem ==============================================================
rem  create_persoonlijke_mappen.bat
rem  Maakt een complete persoonlijke mappen­structuur op het
rem  bureaublad van de huidige gebruiker (geen ZZP/ondernemersmappen)
rem ==============================================================

:: ----- ROOT-PAD (bureaublad) ----------------------------------
set "ROOT=%USERPROFILE%\Desktop\Persoonlijke Administratie"

echo.
echo ==============================================================
echo  Persoonlijke mappenstructuur wordt nu aangemaakt in:
echo      "%ROOT%"
echo ==============================================================

:: ---------- 1. Financiën --------------------------------------
mkdir "%ROOT%\1. Financien\Bankafschriften\2025"
mkdir "%ROOT%\1. Financien\Spaarrekeningen"
mkdir "%ROOT%\1. Financien\Beleggingen"

:: ---------- 2. Belastingen ------------------------------------
mkdir "%ROOT%\2. Belastingen\Aangiften Inkomstenbelasting\2025"
mkdir "%ROOT%\2. Belastingen\Correspondentie Belastingdienst\2025"

:: ---------- 3. Verzekeringen ----------------------------------
mkdir "%ROOT%\3. Verzekeringen\Zorgverzekering"
mkdir "%ROOT%\3. Verzekeringen\Inboedel Opstal"
mkdir "%ROOT%\3. Verzekeringen\Autoverzekering"
mkdir "%ROOT%\3. Verzekeringen\Overig"

:: ---------- 4. Woning -----------------------------------------
mkdir "%ROOT%\4. Woning\Hypotheek of Huur"
mkdir "%ROOT%\4. Woning\Nutsvoorzieningen"
mkdir "%ROOT%\4. Woning\Onderhoud"
mkdir "%ROOT%\4. Woning\Inrichting"

:: ---------- 5. Gezondheid -------------------------------------
mkdir "%ROOT%\5. Gezondheid\Medische dossiers"
mkdir "%ROOT%\5. Gezondheid\Recepten en medicijnen"
mkdir "%ROOT%\5. Gezondheid\Zorgclaims"

:: ---------- 6. Voertuigen -------------------------------------
mkdir "%ROOT%\6. Voertuigen\Onderhoudsrecords"
mkdir "%ROOT%\6. Voertuigen\Verzekeringen"
mkdir "%ROOT%\6. Voertuigen\Registratie Belasting"

:: ---------- 7. Carrière ---------------------------------------
mkdir "%ROOT%\7. Carriere\CV"
mkdir "%ROOT%\7. Carriere\Certificaten"
mkdir "%ROOT%\7. Carriere\Sollicitaties"

:: ---------- 8. Reizen -----------------------------------------
mkdir "%ROOT%\8. Reizen\2025 Andalusie"

:: ---------- 9. Hobby ------------------------------------------
mkdir "%ROOT%\9. Hobby\Gezondheid en fitness"
mkdir "%ROOT%\9. Hobby\Recepten"
mkdir "%ROOT%\9. Hobby\YouTube concepten"

:: ---------- 10. Familie en kinderen ---------------------------
mkdir "%ROOT%\10. Familie en kinderen\School en onderwijs"
mkdir "%ROOT%\10. Familie en kinderen\Activiteiten en vakanties"
mkdir "%ROOT%\10. Familie en kinderen\Overige documenten"

:: ---------- 11. Digitale bezittingen --------------------------
mkdir "%ROOT%\11. Digitale bezittingen\Wachtwoordkluis"
mkdir "%ROOT%\11. Digitale bezittingen\2FA backups"
mkdir "%ROOT%\11. Digitale bezittingen\Software licenties"

:: ---------- 12. Abonnementen en lidmaatschappen ---------------
mkdir "%ROOT%\12. Abonnementen en lidmaatschappen\Streaming"
mkdir "%ROOT%\12. Abonnementen en lidmaatschappen\Sportclub"
mkdir "%ROOT%\12. Abonnementen en lidmaatschappen\Overige abonnementen"

:: ---------- 13. Foto en video ---------------------------------
mkdir "%ROOT%\13. Foto en video\2025\06 Graduatie"

:: ---------- 14. Opleiding -------------------------------------
mkdir "%ROOT%\14. Opleiding\Cursussen"
mkdir "%ROOT%\14. Opleiding\Studie materiaal"
mkdir "%ROOT%\14. Opleiding\Certificaten"

:: ---------- 15. Juridisch -------------------------------------
mkdir "%ROOT%\15. Juridisch\Contracten"
mkdir "%ROOT%\15. Juridisch\Boetes"
mkdir "%ROOT%\15. Juridisch\Officiele correspondentie"

:: ---------- 16. Nalatenschap ----------------------------------
mkdir "%ROOT%\16. Nalatenschap\Testament"
mkdir "%ROOT%\16. Nalatenschap\Levenstestament"
mkdir "%ROOT%\16. Nalatenschap\Uitvaartwensen"

:: ---------- 17. Noodinformatie --------------------------------
mkdir "%ROOT%\17. Noodinformatie\Paspoort scans"
mkdir "%ROOT%\17. Noodinformatie\ICE contacten"
mkdir "%ROOT%\17. Noodinformatie\Medische alert"

:: ---------- 18. Huisinventaris --------------------------------
mkdir "%ROOT%\18. Huisinventaris\Fotos"
mkdir "%ROOT%\18. Huisinventaris\Aankoopbewijzen"

:: ---------- 19. Persoonlijke projecten ------------------------
mkdir "%ROOT%\19. Persoonlijke projecten\DIY plannen"
mkdir "%ROOT%\19. Persoonlijke projecten\Side hustle ideeen"

:: ---------- 20. Huisdieren ------------------------------------
mkdir "%ROOT%\20. Huisdieren\Dierenpaspoorten"
mkdir "%ROOT%\20. Huisdieren\Dierenarts"
mkdir "%ROOT%\20. Huisdieren\Verzekeringen"

:: ---------- 99. Archief ---------------------------------------
mkdir "%ROOT%\99. Archief\2024\Oude projecten"

echo.
echo *****************************************************************
echo *** SUCCES! Alle mappen zijn succesvol aangemaakt op het bureaublad.
echo *****************************************************************
echo.
set /p exitKey=Druk op X en op Enter om af te sluiten... 
