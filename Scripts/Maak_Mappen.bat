@echo off
setlocal enabledelayedexpansion

rem ================================================================================
rem  create_persoonlijke_mappen_verbeterd.bat
rem  Versie: 1.3 (Verbeterde flow na jaarinvoer en foutafhandeling)
rem ================================================================================

:: ----- CONFIGURATIE & INITIALISATIE ---------------------------------------------
set "LOGFILE=%TEMP%\create_persoonlijke_mappen_simple_log.txt"
set "SCRIPT_SUCCESSFUL=true" rem Standaard aanname, wordt false bij fouten

:: Initialiseer logbestand
echo. > "%LOGFILE%"
call :logMessage "Script create_persoonlijke_mappen_verbeterd.bat (v1.3) gestart."

:: ----- ROOT-PAD (bureaublad) ----------------------------------------------------
set "ROOT=%USERPROFILE%\Desktop\Persoonlijke Administratie"

:: ----- JAAR INSTELLING ----------------------------------------------------------
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set "datetime=%%I"
set "CURRENT_YEAR=%datetime:~0,4%"
set "TARGET_YEAR=%CURRENT_YEAR%"
set "PREVIOUS_YEAR_ARCHIVE=%datetime:~0,4%"
set /a "PREVIOUS_YEAR_ARCHIVE-=1"

echo.
set "INPUT_YEAR=" rem Reset variabele voor de zekerheid
set /p "INPUT_YEAR=Voor welk jaar moeten de mappen worden aangemaakt? (Standaard: !CURRENT_YEAR!): "
if defined INPUT_YEAR if "!INPUT_YEAR!" NEQ "" (
    echo !INPUT_YEAR! | findstr /r /c:"^[1-9][0-9][0-9][0-9]$" >nul
    if not errorlevel 1 (
        set "TARGET_YEAR=!INPUT_YEAR!"
        set /a "PREVIOUS_YEAR_ARCHIVE = TARGET_YEAR - 1"
        call :logMessage "Door gebruiker gekozen doeljaar: !TARGET_YEAR!. Archiefjaar: !PREVIOUS_YEAR_ARCHIVE!"
    ) else (
        echo Ongeldig jaartal ingevoerd. !CURRENT_YEAR! (en archief !PREVIOUS_YEAR_ARCHIVE!) wordt gebruikt.
        call :logMessage "Ongeldig jaartal. Standaardjaar (!CURRENT_YEAR!) en archief (!PREVIOUS_YEAR_ARCHIVE!) gebruikt."
    )
) else (
    call :logMessage "Standaard doeljaar (!CURRENT_YEAR!) en archief (!PREVIOUS_YEAR_ARCHIVE!) gebruikt."
)

echo.
echo ==============================================================
echo  Persoonlijke mappenstructuur wordt nu aangemaakt in:
echo      "%ROOT%"
echo  Voor het jaar: !TARGET_YEAR!
echo ==============================================================
echo Controleer de bovenstaande gegevens.
pause
echo.
echo OK. Starten met het aanmaken van de mappen...
echo.

:: ----- HOOFDMAP AANMAKEN EN CONTROLEREN ----------------------------------------
call :logMessage "Controleren/aanmaken hoofdmap: %ROOT%"
call :createSingleDir "%ROOT%"
if not exist "%ROOT%" (
    echo.
    echo !!! KRITIEKE FOUT: Kon de hoofdmap "%ROOT%" niet aanmaken. !!!
    echo Controleer het pad en uw schrijfrechten. Script stopt.
    call :logMessage "FATAL: Kon hoofdmap '%ROOT%' niet aanmaken."
    set "SCRIPT_SUCCESSFUL=false"
    goto EndScriptProcessing
)
call :logMessage "Hoofdmap succesvol verwerkt."
echo.

:: ----- MAPPENSTRUCTUUR AANMAKEN ------------------------------------------------
call :logMessage "Start aanmaken submappenstructuur."

:: ---------- 1. Financiën --------------------------------------
call :createSingleDir "%ROOT%\1. Financien"
call :createSingleDir "%ROOT%\1. Financien\Bankafschriften"
call :createSingleDir "%ROOT%\1. Financien\Bankafschriften\!TARGET_YEAR!"
call :createSingleDir "%ROOT%\1. Financien\Spaarrekeningen"
call :createSingleDir "%ROOT%\1. Financien\Beleggingen"

:: ... (ALLE ANDERE MAP CREATIE CALLS BLIJVEN HIER IDENTIEK) ...
:: ---------- 2. Belastingen ------------------------------------
call :createSingleDir "%ROOT%\2. Belastingen"
call :createSingleDir "%ROOT%\2. Belastingen\Aangiften Inkomstenbelasting"
call :createSingleDir "%ROOT%\2. Belastingen\Aangiften Inkomstenbelasting\!TARGET_YEAR!"
call :createSingleDir "%ROOT%\2. Belastingen\Correspondentie Belastingdienst"
call :createSingleDir "%ROOT%\2. Belastingen\Correspondentie Belastingdienst\!TARGET_YEAR!"

:: ---------- 3. Verzekeringen ----------------------------------
call :createSingleDir "%ROOT%\3. Verzekeringen"
call :createSingleDir "%ROOT%\3. Verzekeringen\Zorgverzekering"
call :createSingleDir "%ROOT%\3. Verzekeringen\Inboedel Opstal"
call :createSingleDir "%ROOT%\3. Verzekeringen\Autoverzekering"
call :createSingleDir "%ROOT%\3. Verzekeringen\Overig"

:: ---------- 4. Woning -----------------------------------------
call :createSingleDir "%ROOT%\4. Woning"
call :createSingleDir "%ROOT%\4. Woning\Hypotheek of Huur"
call :createSingleDir "%ROOT%\4. Woning\Nutsvoorzieningen"
call :createSingleDir "%ROOT%\4. Woning\Onderhoud"
call :createSingleDir "%ROOT%\4. Woning\Inrichting"

:: ---------- 5. Gezondheid -------------------------------------
call :createSingleDir "%ROOT%\5. Gezondheid"
call :createSingleDir "%ROOT%\5. Gezondheid\Medische dossiers"
call :createSingleDir "%ROOT%\5. Gezondheid\Recepten en medicijnen"
call :createSingleDir "%ROOT%\5. Gezondheid\Zorgclaims"

:: ---------- 6. Voertuigen -------------------------------------
call :createSingleDir "%ROOT%\6. Voertuigen"
call :createSingleDir "%ROOT%\6. Voertuigen\Onderhoudsrecords"
call :createSingleDir "%ROOT%\6. Voertuigen\Verzekeringen"
call :createSingleDir "%ROOT%\6. Voertuigen\Registratie Belasting"

:: ---------- 7. Carrière ---------------------------------------
call :createSingleDir "%ROOT%\7. Carriere"
call :createSingleDir "%ROOT%\7. Carriere\CV"
call :createSingleDir "%ROOT%\7. Carriere\Certificaten"
call :createSingleDir "%ROOT%\7. Carriere\Sollicitaties"

:: ---------- 8. Reizen -----------------------------------------
call :createSingleDir "%ROOT%\8. Reizen"
call :createSingleDir "%ROOT%\8. Reizen\!TARGET_YEAR! Andalusie"

:: ---------- 9. Hobby ------------------------------------------
call :createSingleDir "%ROOT%\9. Hobby"
call :createSingleDir "%ROOT%\9. Hobby\Gezondheid en fitness"
call :createSingleDir "%ROOT%\9. Hobby\Recepten"
call :createSingleDir "%ROOT%\9. Hobby\YouTube concepten"

:: ---------- 10. Familie en kinderen ---------------------------
call :createSingleDir "%ROOT%\10. Familie en kinderen"
call :createSingleDir "%ROOT%\10. Familie en kinderen\School en onderwijs"
call :createSingleDir "%ROOT%\10. Familie en kinderen\Activiteiten en vakanties"
call :createSingleDir "%ROOT%\10. Familie en kinderen\Overige documenten"

:: ---------- 11. Digitale bezittingen --------------------------
call :createSingleDir "%ROOT%\11. Digitale bezittingen"
call :createSingleDir "%ROOT%\11. Digitale bezittingen\Wachtwoordkluis"
call :createSingleDir "%ROOT%\11. Digitale bezittingen\2FA backups"
call :createSingleDir "%ROOT%\11. Digitale bezittingen\Software licenties"

:: ---------- 12. Abonnementen en lidmaatschappen ---------------
call :createSingleDir "%ROOT%\12. Abonnementen en lidmaatschappen"
call :createSingleDir "%ROOT%\12. Abonnementen en lidmaatschappen\Streaming"
call :createSingleDir "%ROOT%\12. Abonnementen en lidmaatschappen\Sportclub"
call :createSingleDir "%ROOT%\12. Abonnementen en lidmaatschappen\Overige abonnementen"

:: ---------- 13. Foto en video ---------------------------------
call :createSingleDir "%ROOT%\13. Foto en video"
call :createSingleDir "%ROOT%\13. Foto en video\!TARGET_YEAR!"
call :createSingleDir "%ROOT%\13. Foto en video\!TARGET_YEAR!\06 Graduatie"

:: ---------- 14. Opleiding -------------------------------------
call :createSingleDir "%ROOT%\14. Opleiding"
call :createSingleDir "%ROOT%\14. Opleiding\Cursussen"
call :createSingleDir "%ROOT%\14. Opleiding\Studie materiaal"
call :createSingleDir "%ROOT%\14. Opleiding\Certificaten"

:: ---------- 15. Juridisch -------------------------------------
call :createSingleDir "%ROOT%\15. Juridisch"
call :createSingleDir "%ROOT%\15. Juridisch\Contracten"
call :createSingleDir "%ROOT%\15. Juridisch\Boetes"
call :createSingleDir "%ROOT%\15. Juridisch\Officiele correspondentie"

:: ---------- 16. Nalatenschap ----------------------------------
call :createSingleDir "%ROOT%\16. Nalatenschap"
call :createSingleDir "%ROOT%\16. Nalatenschap\Testament"
call :createSingleDir "%ROOT%\16. Nalatenschap\Levenstestament"
call :createSingleDir "%ROOT%\16. Nalatenschap\Uitvaartwensen"

:: ---------- 17. Noodinformatie --------------------------------
call :createSingleDir "%ROOT%\17. Noodinformatie"
call :createSingleDir "%ROOT%\17. Noodinformatie\Paspoort scans"
call :createSingleDir "%ROOT%\17. Noodinformatie\ICE contacten"
call :createSingleDir "%ROOT%\17. Noodinformatie\Medische alert"

:: ---------- 18. Huisinventaris --------------------------------
call :createSingleDir "%ROOT%\18. Huisinventaris"
call :createSingleDir "%ROOT%\18. Huisinventaris\Fotos"
call :createSingleDir "%ROOT%\18. Huisinventaris\Aankoopbewijzen"

:: ---------- 19. Persoonlijke projecten ------------------------
call :createSingleDir "%ROOT%\19. Persoonlijke projecten"
call :createSingleDir "%ROOT%\19. Persoonlijke projecten\DIY plannen"
call :createSingleDir "%ROOT%\19. Persoonlijke projecten\Side hustle ideeen"

:: ---------- 20. Huisdieren ------------------------------------
call :createSingleDir "%ROOT%\20. Huisdieren"
call :createSingleDir "%ROOT%\20. Huisdieren\Dierenpaspoorten"
call :createSingleDir "%ROOT%\20. Huisdieren\Dierenarts"
call :createSingleDir "%ROOT%\20. Huisdieren\Verzekeringen"

:: ---------- 99. Archief ---------------------------------------
call :createSingleDir "%ROOT%\99. Archief"
call :createSingleDir "%ROOT%\99. Archief\!PREVIOUS_YEAR_ARCHIVE!"
call :createSingleDir "%ROOT%\99. Archief\!PREVIOUS_YEAR_ARCHIVE!\Oude projecten"

call :logMessage "Aanmaken submappenstructuur voltooid."
goto EndScriptProcessing

:: ----- SUBROUTINES --------------------------------------------------------------

:createSingleDir
rem Parameter %1: Volledig pad naar de map die aangemaakt moet worden.
set "FOLDER_TO_MAKE=%~1"
echo.
echo   Verwerken map: "!FOLDER_TO_MAKE!"

if not exist "!FOLDER_TO_MAKE!" (
    mkdir "!FOLDER_TO_MAKE!"
    if not errorlevel 1 (
        echo     [OK] Map succesvol aangemaakt.
        call :logMessage "AANGEMAAKT: !FOLDER_TO_MAKE!"
    ) else (
        set "MKDIR_ERROR_LEVEL=!errorlevel!"
        echo     [FOUT!] Kon map NIET aanmaken! (Errorlevel: !MKDIR_ERROR_LEVEL!)
        call :logMessage "FOUT bij aanmaken: !FOLDER_TO_MAKE! (Error: !MKDIR_ERROR_LEVEL!)"
        set "SCRIPT_SUCCESSFUL=false" rem Markeer dat er iets misging
    )
) else (
    echo     [INFO] Map bestaat al, geen actie nodig.
    call :logMessage "BESTAAT AL: !FOLDER_TO_MAKE!"
)
goto :eof

:logMessage
rem Parameter %1: Boodschap om te loggen.
echo [%DATE% %TIME%] %~1 >> "%LOGFILE%"
goto :eof

:EndScriptProcessing
rem Dit label wordt aangeroepen na de mapverwerking of bij een fatale fout.
echo.
if "!SCRIPT_SUCCESSFUL!"=="true" (
    echo *****************************************************************
    echo ***                                                         ***
    echo ***    S U C C E S !                                        ***
    echo ***                                                         ***
    echo ***    Alle persoonlijke mappen zijn succesvol verwerkt     ***
    echo ***    (aangemaakt of bestonden al) in:                     ***
    echo ***    "%ROOT%"
    echo ***                                                         ***
    echo *****************************************************************
    call :logMessage "Script succesvol beëindigd. Alle mappen verwerkt."
) else (
    echo *****************************************************************
    echo ***                                                         ***
    echo ***    LET OP: Er zijn problemen opgetreden!                 ***
    echo ***                                                         ***
    echo ***    Mogelijk zijn niet alle mappen (correct) aangemaakt. ***
    echo ***    Controleer de output hierboven en het logbestand:    ***
    echo ***    "%LOGFILE%"
    echo ***                                                         ***
    echo *****************************************************************
    call :logMessage "Script beëindigd met problemen tijdens mapcreatie."
)
echo.
echo Druk op een willekeurige toets om af te sluiten...
pause >nul
endlocal
exit /b