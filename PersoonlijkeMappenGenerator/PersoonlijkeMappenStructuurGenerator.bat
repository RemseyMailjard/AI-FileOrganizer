@echo off
setlocal enabledelayedexpansion

rem ================================================================================
rem  PersoonlijkeMappenStructuurGenerator.bat
rem  Versie: 1.5.3 (Robuuste jaartalvalidatie)
rem  Maakt een persoonlijke mappenstructuur aan met logging en foutafhandeling.
rem ================================================================================

:: Welkomstbericht
echo.
echo ===============================
echo Gemaakt door Remsey Mailjard
echo LinkedIn: https://nl.linkedin.com/in/remseymailjard
echo ===============================
echo.

:: Initialisatie
set "SCRIPT_VERSION=1.5.3"
set "LOGFILE=%TEMP%\PersoonlijkeMappenStructuurGenerator_log.txt"
set "SCRIPT_SUCCESSFUL=true"
set /a FOLDERS_CREATED_COUNT=0
set /a FOLDERS_EXISTED_COUNT=0
set /a FOLDERS_FAILED_COUNT=0

:: Logbestand aanmaken
echo. > "%LOGFILE%"
call :logMessage "Script gestart (v%SCRIPT_VERSION%)."

:: Root-locatie bepalen
set "DEFAULT_ROOT=%USERPROFILE%\Desktop\Persoonlijke Administratie"
echo.
echo =============================================================
echo  Persoonlijke mappenstructuur wordt nu aangemaakt!
echo =============================================================
echo Standaard locatie is: "%DEFAULT_ROOT%"
set /p "ROOT=Wil je een andere locatie? (laat leeg voor standaard): "
if "!ROOT!"=="" set "ROOT=%DEFAULT_ROOT%"
if "!ROOT:~-1!"=="\" set "ROOT=!ROOT:~0,-1!"

:: Huidig jaar berekenen en standaard doeljaar instellen
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set "datetime=%%I"
set "CURRENT_YEAR=%datetime:~0,4%"
set "TARGET_YEAR=%CURRENT_YEAR%"
set /a "PREVIOUS_YEAR_ARCHIVE=TARGET_YEAR-1"
call :logMessage "Standaard doeljaar ingesteld op huidig jaar: !CURRENT_YEAR!, archiefjaar: !PREVIOUS_YEAR_ARCHIVE!"

:: Vraag gebruiker naar doeljaar met numerieke validatie
echo.
set "INPUT_YEAR="
set /p "INPUT_YEAR=Voor welk jaar moeten de mappen worden aangemaakt? (standaard: !CURRENT_YEAR!): "

if "!INPUT_YEAR!"=="" (
    call :logMessage "Geen jaartal ingevoerd. Standaard (!CURRENT_YEAR!) wordt gebruikt."
    goto AfterYearValidation
)

set /a TEST_YEAR=0 2>nul
set /a TEST_YEAR=!INPUT_YEAR! 2>nul
if errorlevel 1 (
    call :logMessage "Ongeldige invoer voor jaartal (niet numeriek): '!INPUT_YEAR!'."
    goto InvalidYearInput
)

if !TEST_YEAR! lss 1000 (
    call :logMessage "Ingevoerd jaartal '!INPUT_YEAR!' (!TEST_YEAR!) is te klein (minder dan 1000)."
    goto InvalidYearInput
)
if !TEST_YEAR! gtr 9999 (
    call :logMessage "Ingevoerd jaartal '!INPUT_YEAR!' (!TEST_YEAR!) is te groot (meer dan 9999)."
    goto InvalidYearInput
)

set "TARGET_YEAR=!TEST_YEAR!"
set /a "PREVIOUS_YEAR_ARCHIVE=TARGET_YEAR-1"
call :logMessage "Doeljaar door gebruiker ingesteld: !TARGET_YEAR!, archiefjaar: !PREVIOUS_YEAR_ARCHIVE!"
goto AfterYearValidation

:InvalidYearInput
echo Ongeldig jaartal ('!INPUT_YEAR!'). Standaard jaartal (!CURRENT_YEAR!) wordt gebruikt.
call :logMessage "Teruggevallen op standaardjaar (!CURRENT_YEAR!) na ongeldige input '!INPUT_YEAR!'."
set "TARGET_YEAR=%CURRENT_YEAR%"
set /a "PREVIOUS_YEAR_ARCHIVE=TARGET_YEAR-1"

:AfterYearValidation
:: Overzicht en bevestiging
echo.
echo =============================================================
echo Mappenstructuur wordt aangemaakt in:
echo    "!ROOT!"
echo Voor het jaar: !TARGET_YEAR!
echo Archief voor: !PREVIOUS_YEAR_ARCHIVE!
echo =============================================================
echo Druk op Enter om te starten, of sluit dit venster om te annuleren.
pause

:: Begin mappen aanmaken
call :logMessage "Start mapcreatie: Locatie=!ROOT!, Jaar=!TARGET_YEAR!."
call :createSingleDir "!ROOT!"
if not exist "!ROOT!" (
    echo [FOUT] Kan hoofdmap niet aanmaken: "!ROOT!"
    call :logMessage "FOUT: Hoofdmap niet aangemaakt."
    set "SCRIPT_SUCCESSFUL=false"
    goto EndScriptProcessing
)

:: ---------- 1. Financiën --------------------------------------
call :createSingleDir "!ROOT!\1. Financien"
call :createSingleDir "!ROOT!\1. Financien\Bankafschriften\!TARGET_YEAR!"
call :createSingleDir "!ROOT!\1. Financien\Spaarrekeningen"
call :createSingleDir "!ROOT!\1. Financien\Beleggingen"

:: ---------- 2. Belastingen ------------------------------------
call :createSingleDir "!ROOT!\2. Belastingen"
call :createSingleDir "!ROOT!\2. Belastingen\Aangiften Inkomstenbelasting\!TARGET_YEAR!"
call :createSingleDir "!ROOT!\2. Belastingen\Correspondentie Belastingdienst\!TARGET_YEAR!"

:: ---------- 3. Verzekeringen ----------------------------------
call :createSingleDir "!ROOT!\3. Verzekeringen"
call :createSingleDir "!ROOT!\3. Verzekeringen\Zorgverzekering"
call :createSingleDir "!ROOT!\3. Verzekeringen\Inboedel Opstal"
call :createSingleDir "!ROOT!\3. Verzekeringen\Autoverzekering"
call :createSingleDir "!ROOT!\3. Verzekeringen\Overig"

:: ---------- 4. Woning -----------------------------------------
call :createSingleDir "!ROOT!\4. Woning"
call :createSingleDir "!ROOT!\4. Woning\Hypotheek of Huur"
call :createSingleDir "!ROOT!\4. Woning\Nutsvoorzieningen"
call :createSingleDir "!ROOT!\4. Woning\Onderhoud"
call :createSingleDir "!ROOT!\4. Woning\Inrichting"

:: ---------- 5. Gezondheid -------------------------------------
call :createSingleDir "!ROOT!\5. Gezondheid"
call :createSingleDir "!ROOT!\5. Gezondheid\Medische dossiers"
call :createSingleDir "!ROOT!\5. Gezondheid\Recepten en medicijnen"
call :createSingleDir "!ROOT!\5. Gezondheid\Zorgclaims"

:: ---------- 6. Voertuigen -------------------------------------
call :createSingleDir "!ROOT!\6. Voertuigen"
call :createSingleDir "!ROOT!\6. Voertuigen\Onderhoudsrecords"
call :createSingleDir "!ROOT!\6. Voertuigen\Verzekeringen"
call :createSingleDir "!ROOT!\6. Voertuigen\Registratie Belasting"

:: ---------- 7. Carrière ---------------------------------------
call :createSingleDir "!ROOT!\7. Carriere"
call :createSingleDir "!ROOT!\7. Carriere\CV"
call :createSingleDir "!ROOT!\7. Carriere\Certificaten"
call :createSingleDir "!ROOT!\7. Carriere\Sollicitaties"

:: ---------- 8. Reizen -----------------------------------------
call :createSingleDir "!ROOT!\8. Reizen"
call :createSingleDir "!ROOT!\8. Reizen\!TARGET_YEAR! Andalusie"
call :createSingleDir "!ROOT!\8. Reizen\Vooruitblik Volgend Jaar"

:: ---------- 9. Hobby ------------------------------------------
call :createSingleDir "!ROOT!\9. Hobby"
call :createSingleDir "!ROOT!\9. Hobby\Gezondheid en fitness"
call :createSingleDir "!ROOT!\9. Hobby\Recepten"
call :createSingleDir "!ROOT!\9. Hobby\YouTube concepten"

:: ---------- 10. Familie en kinderen ---------------------------
call :createSingleDir "!ROOT!\10. Familie en kinderen"
call :createSingleDir "!ROOT!\10. Familie en kinderen\School en onderwijs"
call :createSingleDir "!ROOT!\10. Familie en kinderen\Activiteiten en vakanties"
call :createSingleDir "!ROOT!\10. Familie en kinderen\Overige documenten"

:: ---------- 11. Digitale bezittingen --------------------------
call :createSingleDir "!ROOT!\11. Digitale bezittingen"
call :createSingleDir "!ROOT!\11. Digitale bezittingen\Wachtwoordkluis"
call :createSingleDir "!ROOT!\11. Digitale bezittingen\2FA backups"
call :createSingleDir "!ROOT!\11. Digitale bezittingen\Software licenties"

:: ---------- 12. Abonnementen en lidmaatschappen ---------------
call :createSingleDir "!ROOT!\12. Abonnementen en lidmaatschappen"
call :createSingleDir "!ROOT!\12. Abonnementen en lidmaatschappen\Streaming"
call :createSingleDir "!ROOT!\12. Abonnementen en lidmaatschappen\Sportclub"
call :createSingleDir "!ROOT!\12. Abonnementen en lidmaatschappen\Overige abonnementen"

:: ---------- 13. Foto en video ---------------------------------
call :createSingleDir "!ROOT!\13. Foto en video"
call :createSingleDir "!ROOT!\13. Foto en video\!TARGET_YEAR!"
call :createSingleDir "!ROOT!\13. Foto en video\!TARGET_YEAR!\06 Graduatie"

:: ---------- 14. Opleiding -------------------------------------
call :createSingleDir "!ROOT!\14. Opleiding"
call :createSingleDir "!ROOT!\14. Opleiding\Cursussen"
call :createSingleDir "!ROOT!\14. Opleiding\Studie materiaal"
call :createSingleDir "!ROOT!\14. Opleiding\Certificaten"

:: ---------- 15. Juridisch -------------------------------------
call :createSingleDir "!ROOT!\15. Juridisch"
call :createSingleDir "!ROOT!\15. Juridisch\Contracten"
call :createSingleDir "!ROOT!\15. Juridisch\Boetes"
call :createSingleDir "!ROOT!\15. Juridisch\Officiele correspondentie"

:: ---------- 16. Nalatenschap ----------------------------------
call :createSingleDir "!ROOT!\16. Nalatenschap"
call :createSingleDir "!ROOT!\16. Nalatenschap\Testament"
call :createSingleDir "!ROOT!\16. Nalatenschap\Levenstestament"
call :createSingleDir "!ROOT!\16. Nalatenschap\Uitvaartwensen"

:: ---------- 17. Noodinformatie --------------------------------
call :createSingleDir "!ROOT!\17. Noodinformatie"
call :createSingleDir "!ROOT!\17. Noodinformatie\Paspoort scans"
call :createSingleDir "!ROOT!\17. Noodinformatie\ICE contacten"
call :createSingleDir "!ROOT!\17. Noodinformatie\Medische alert"

:: ---------- 18. Huisinventaris --------------------------------
call :createSingleDir "!ROOT!\18. Huisinventaris"
call :createSingleDir "!ROOT!\18. Huisinventaris\Fotos"
call :createSingleDir "!ROOT!\18. Huisinventaris\Aankoopbewijzen"

:: ---------- 19. Persoonlijke projecten ------------------------
call :createSingleDir "!ROOT!\19. Persoonlijke projecten"
call :createSingleDir "!ROOT!\19. Persoonlijke projecten\DIY plannen"
call :createSingleDir "!ROOT!\19. Persoonlijke projecten\Side hustle ideeen"

:: ---------- 20. Huisdieren ------------------------------------
call :createSingleDir "!ROOT!\20. Huisdieren"
call :createSingleDir "!ROOT!\20. Huisdieren\Dierenpaspoorten"
call :createSingleDir "!ROOT!\20. Huisdieren\Dierenarts"
call :createSingleDir "!ROOT!\20. Huisdieren\Verzekeringen"

:: ---------- 99. Archief ---------------------------------------
call :createSingleDir "!ROOT!\99. Archief"
call :createSingleDir "!ROOT!\99. Archief\!PREVIOUS_YEAR_ARCHIVE!"
call :createSingleDir "!ROOT!\99. Archief\!PREVIOUS_YEAR_ARCHIVE!\Oude projecten"

:EndScriptProcessing
echo.
if "!SCRIPT_SUCCESSFUL!"=="true" (
    echo *****************************************************************
    echo *** SUCCES! Alle mappen zijn succesvol verwerkt.            ***
    echo *****************************************************************
    call :logMessage "Script succesvol beëindigd."
) else (
    echo *****************************************************************
    echo *** LET OP: Er zijn fouten opgetreden. Controleer het logbestand: ***
    echo ***        "!LOGFILE!"                                       ***
    echo *****************************************************************
    call :logMessage "Script beëindigd met fouten."
)

echo.
echo Druk op een toets om af te sluiten...
pause >nul
endlocal
exit /b

:: -------------------------- SUBROUTINES --------------------------

:createSingleDir
rem Subroutine voor het aanmaken van één map met logging
set "FOLDER=%~1"
if not exist "%FOLDER%" (
    mkdir "%FOLDER%" && (
        echo [OK] %FOLDER%
        call :logMessage "AANGEMAAKT: %FOLDER%"
        set /a FOLDERS_CREATED_COUNT+=1
    ) || (
        echo [FOUT] Kon niet aanmaken: %FOLDER%
        call :logMessage "FOUT: Kon niet aanmaken: %FOLDER%"
        set "SCRIPT_SUCCESSFUL=false"
        set /a FOLDERS_FAILED_COUNT+=1
    )
) else (
    echo [INFO] Bestaat al: %FOLDER%
    call :logMessage "BESTAAT AL: %FOLDER%"
    set /a FOLDERS_EXISTED_COUNT+=1
)
goto :eof

:logMessage
rem Subroutine voor loggen van berichten
echo [%DATE% %TIME%] %~1 >> "%LOGFILE%"
goto :eof