@echo off
setlocal enabledelayedexpansion

rem ================================================================================
rem  create_persoonlijke_mappen_enhanced.bat
rem  Versie: 2.1
rem  Maakt een configureerbare persoonlijke mappen­structuur aan.
rem  Functionaliteiten:
rem  - Gebruiker kan root-locatie kiezen.
rem  - Gebruiker kan jaartal voor mappen specificeren.
rem  - Controleert of mappen al bestaan voordat ze worden aangemaakt.
rem  - Gedetailleerde logging van acties.
rem  - Foutafhandeling bij het aanmaken van mappen.
rem  - Duidelijke bevestiging na gebruikersinteractie.
rem ================================================================================

:: ----- CONFIGURATIE & INITIALISATIE ---------------------------------------------
set "SCRIPT_VERSION=2.1"
set "LOGFILE=%TEMP%\create_persoonlijke_mappen_log.txt"
set "DEFAULT_ROOT_NAME=Persoonlijke Administratie"

:: Initialiseer logbestand (overschrijft oud logbestand bij elke run)
echo. > "%LOGFILE%"
call :logMessage "================================================================================"
call :logMessage "Script create_persoonlijke_mappen_enhanced.bat (Versie: !SCRIPT_VERSION!) gestart."
call :logMessage "================================================================================"
echo.

:: ----- 1. BEPAAL ROOT LOCATIE ---------------------------------------------------
set "DEFAULT_ROOT_PATH=%USERPROFILE%\Desktop\%DEFAULT_ROOT_NAME%"
echo Standaard locatie voor de hoofdmap is:
echo   "%DEFAULT_ROOT_PATH%"
echo.
set "USER_CHOSEN_ROOT="
set /p "USER_CHOSEN_ROOT=Voer een andere volledige locatie in, of druk Enter voor de standaard: "

if "!USER_CHOSEN_ROOT!"=="" (
    set "ROOT=%DEFAULT_ROOT_PATH%"
    call :logMessage "[CONFIG] Standaard rootlocatie gekozen: %ROOT%"
) else (
    set "ROOT=%USER_CHOSEN_ROOT%"
    if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%" rem Verwijder afsluitende backslash indien aanwezig
    if "%ROOT:~-1%"=="/" set "ROOT=%ROOT:~0,-1%" rem Verwijder afsluitende forward slash indien aanwezig
    call :logMessage "[CONFIG] Door gebruiker gekozen rootlocatie: %ROOT%"
)
echo.
echo Gekozen locatie voor de mappenstructuur:
echo   "%ROOT%"
echo.

:: ----- 2. BEPAAL DOELJAAR EN VORIG JAAR -----------------------------------------
:: Haal huidig jaar op (robuuste methode)
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set "datetime=%%I"
set "CURRENT_YEAR=%datetime:~0,4%"
set "TARGET_YEAR=%CURRENT_YEAR%"

call :logMessage "[CONFIG] Huidig systeemjaar gedetecteerd: %CURRENT_YEAR%"
echo Het huidige jaar is: !CURRENT_YEAR!
set /p "INPUT_YEAR=Voor welk jaar moeten de mappen worden aangemaakt? (Standaard: !CURRENT_YEAR!): "

if defined INPUT_YEAR if "!INPUT_YEAR!" NEQ "" (
    echo !INPUT_YEAR! | findstr /r /c:"^[1-9][0-9][0-9][0-9]$" >nul
    if not errorlevel 1 (
        set "TARGET_YEAR=!INPUT_YEAR!"
        call :logMessage "[CONFIG] Door gebruiker gekozen doeljaar: !TARGET_YEAR!"
    ) else (
        echo Ongeldig jaartal ingevoerd. "!CURRENT_YEAR!" wordt gebruikt.
        call :logMessage "[CONFIG] Ongeldig jaartal ingevoerd (!INPUT_YEAR!). Standaardjaar (!CURRENT_YEAR!) wordt gebruikt."
    )
) else (
    call :logMessage "[CONFIG] Standaard doeljaar gekozen: !TARGET_YEAR!"
)

set /a "PREVIOUS_YEAR = TARGET_YEAR - 1"
call :logMessage "[CONFIG] Vorig jaar (voor archief) berekend: !PREVIOUS_YEAR!"
echo.
echo Mappen worden aangemaakt voor het jaar: !TARGET_YEAR!
echo Archiefmappen worden aangemaakt voor het jaar: !PREVIOUS_YEAR!
echo.

:: ----- 3. INFORMATIE VOOR DE GEBRUIKER EN BEVESTIGING ----------------------------
echo ================================================================================
echo  Persoonlijke mappenstructuur wordt nu aangemaakt in:
echo      "%ROOT%"
echo  Voor het jaar: !TARGET_YEAR! (Archief: !PREVIOUS_YEAR!)
echo ================================================================================
echo Controleer bovenstaande gegevens.
echo.
pause
echo.
echo OK. Bezig met het voorbereiden en aanmaken van de mappenstructuur...
call :logMessage "[USER_CONFIRMED] Gebruiker heeft bevestigd na controle van instellingen."
call :logMessage "[ACTION] Starten met aanmaken mappenstructuur."
echo Een ogenblik geduld.
echo.

:: ----- 4. MAAK HOOFDMAP AAN EN CONTROLEER ---------------------------------------
call :logMessage "[ACTION] Poging tot aanmaken hoofdmap: %ROOT%"
call :createDir "%ROOT%"
if not exist "%ROOT%" (
    echo.
    echo !!! KRITIEKE FOUT: Kon de hoofdmap "%ROOT%" niet aanmaken. !!!
    echo Controleer het pad en uw schrijfrechten. Het script stopt.
    call :logMessage "[FATAL] Kon hoofdmap '%ROOT%' niet aanmaken. Script afgebroken."
    goto EndScript
)
call :logMessage "[SUCCESS] Hoofdmap '%ROOT%' succesvol aangemaakt of bestond al."
echo.

:: ----- 5. DEFINIEER EN MAAK MAPPENSTRUCTUUR AAN --------------------------------
call :logMessage "[INFO] Start aanmaken van submappenstructuur."

:: --- Categorieën ---
call :createDir "%ROOT%\1. Financien"
call :createDir "%ROOT%\1. Financien\Bankafschriften"
call :createDir "%ROOT%\1. Financien\Bankafschriften\!TARGET_YEAR!"
call :createDir "%ROOT%\1. Financien\Spaarrekeningen"
call :createDir "%ROOT%\1. Financien\Beleggingen"

call :createDir "%ROOT%\2. Belastingen"
call :createDir "%ROOT%\2. Belastingen\Aangiften Inkomstenbelasting"
call :createDir "%ROOT%\2. Belastingen\Aangiften Inkomstenbelasting\!TARGET_YEAR!"
call :createDir "%ROOT%\2. Belastingen\Correspondentie Belastingdienst"
call :createDir "%ROOT%\2. Belastingen\Correspondentie Belastingdienst\!TARGET_YEAR!"

call :createDir "%ROOT%\3. Verzekeringen"
call :createDir "%ROOT%\3. Verzekeringen\Zorgverzekering"
call :createDir "%ROOT%\3. Verzekeringen\Inboedel Opstal"
call :createDir "%ROOT%\3. Verzekeringen\Autoverzekering"
call :createDir "%ROOT%\3. Verzekeringen\Overig"

call :createDir "%ROOT%\4. Woning"
call :createDir "%ROOT%\4. Woning\Hypotheek of Huur"
call :createDir "%ROOT%\4. Woning\Nutsvoorzieningen"
call :createDir "%ROOT%\4. Woning\Onderhoud"
call :createDir "%ROOT%\4. Woning\Inrichting"

call :createDir "%ROOT%\5. Gezondheid"
call :createDir "%ROOT%\5. Gezondheid\Medische dossiers"
call :createDir "%ROOT%\5. Gezondheid\Recepten en medicijnen"
call :createDir "%ROOT%\5. Gezondheid\Zorgclaims"

call :createDir "%ROOT%\6. Voertuigen"
call :createDir "%ROOT%\6. Voertuigen\Onderhoudsrecords"
call :createDir "%ROOT%\6. Voertuigen\Verzekeringen"
call :createDir "%ROOT%\6. Voertuigen\Registratie Belasting"

call :createDir "%ROOT%\7. Carriere"
call :createDir "%ROOT%\7. Carriere\CV"
call :createDir "%ROOT%\7. Carriere\Certificaten"
call :createDir "%ROOT%\7. Carriere\Sollicitaties"

call :createDir "%ROOT%\8. Reizen"
call :createDir "%ROOT%\8. Reizen\!TARGET_YEAR! Andalusie"

call :createDir "%ROOT%\9. Hobby"
call :createDir "%ROOT%\9. Hobby\Gezondheid en fitness"
call :createDir "%ROOT%\9. Hobby\Recepten"
call :createDir "%ROOT%\9. Hobby\YouTube concepten"

call :createDir "%ROOT%\10. Familie en kinderen"
call :createDir "%ROOT%\10. Familie en kinderen\School en onderwijs"
call :createDir "%ROOT%\10. Familie en kinderen\Activiteiten en vakanties"
call :createDir "%ROOT%\10. Familie en kinderen\Overige documenten"

call :createDir "%ROOT%\11. Digitale bezittingen"
call :createDir "%ROOT%\11. Digitale bezittingen\Wachtwoordkluis"
call :createDir "%ROOT%\11. Digitale bezittingen\2FA backups"
call :createDir "%ROOT%\11. Digitale bezittingen\Software licenties"

call :createDir "%ROOT%\12. Abonnementen en lidmaatschappen"
call :createDir "%ROOT%\12. Abonnementen en lidmaatschappen\Streaming"
call :createDir "%ROOT%\12. Abonnementen en lidmaatschappen\Sportclub"
call :createDir "%ROOT%\12. Abonnementen en lidmaatschappen\Overige abonnementen"

call :createDir "%ROOT%\13. Foto en video"
call :createDir "%ROOT%\13. Foto en video\!TARGET_YEAR!"
call :createDir "%ROOT%\13. Foto en video\!TARGET_YEAR!\06 Graduatie"

call :createDir "%ROOT%\14. Opleiding"
call :createDir "%ROOT%\14. Opleiding\Cursussen"
call :createDir "%ROOT%\14. Opleiding\Studie materiaal"
call :createDir "%ROOT%\14. Opleiding\Certificaten"

call :createDir "%ROOT%\15. Juridisch"
call :createDir "%ROOT%\15. Juridisch\Contracten"
call :createDir "%ROOT%\15. Juridisch\Boetes"
call :createDir "%ROOT%\15. Juridisch\Officiele correspondentie"

call :createDir "%ROOT%\16. Nalatenschap"
call :createDir "%ROOT%\16. Nalatenschap\Testament"
call :createDir "%ROOT%\16. Nalatenschap\Levenstestament"
call :createDir "%ROOT%\16. Nalatenschap\Uitvaartwensen"

call :createDir "%ROOT%\17. Noodinformatie"
call :createDir "%ROOT%\17. Noodinformatie\Paspoort scans"
call :createDir "%ROOT%\17. Noodinformatie\ICE contacten"
call :createDir "%ROOT%\17. Noodinformatie\Medische alert"

call :createDir "%ROOT%\18. Huisinventaris"
call :createDir "%ROOT%\18. Huisinventaris\Fotos"
call :createDir "%ROOT%\18. Huisinventaris\Aankoopbewijzen"

call :createDir "%ROOT%\19. Persoonlijke projecten"
call :createDir "%ROOT%\19. Persoonlijke projecten\DIY plannen"
call :createDir "%ROOT%\19. Persoonlijke projecten\Side hustle ideeen"

call :createDir "%ROOT%\20. Huisdieren"
call :createDir "%ROOT%\20. Huisdieren\Dierenpaspoorten"
call :createDir "%ROOT%\20. Huisdieren\Dierenarts"
call :createDir "%ROOT%\20. Huisdieren\Verzekeringen"

call :createDir "%ROOT%\99. Archief"
call :createDir "%ROOT%\99. Archief\!PREVIOUS_YEAR!"
call :createDir "%ROOT%\99. Archief\!PREVIOUS_YEAR!\Oude projecten"

call :logMessage "[INFO] Aanmaken van submappenstructuur voltooid."
echo.
echo ********************************************************************************
echo *** KLAAR! De mappenstructuur operatie is voltooid.
echo *** Controleer de console output hierboven voor eventuele specifieke meldingen.
echo *** Een gedetailleerd logbestand is aangemaakt/bijgewerkt in:
echo ***   %LOGFILE%
echo ********************************************************************************
echo.

goto EndScript

:: ----- SUBROUTINES --------------------------------------------------------------

:createDir
rem Parameter %1: Volledig pad naar de map die aangemaakt moet worden.
set "FOLDER_PATH=%~1"
set "LOG_PREFIX=[%DATE% %TIME%]"

echo %LOG_PREFIX% [CHECK] Bezig met map: "%FOLDER_PATH%" >> "%LOGFILE%"
echo   Bezig met map: "%FOLDER_PATH%"

if not exist "%FOLDER_PATH%" (
    mkdir "%FOLDER_PATH%"
    if not errorlevel 1 (
        echo     [OK] Map succesvol aangemaakt.
        echo %LOG_PREFIX% [SUCCESS] Map aangemaakt: "%FOLDER_PATH%" >> "%LOGFILE%"
    ) else (
        set "MKDIR_ERROR=%errorlevel%"
        echo     [FOUT!] Kon map NIET aanmaken! (Windows Error: !MKDIR_ERROR!)
        echo %LOG_PREFIX% [ERROR] Kon map NIET aanmaken: "%FOLDER_PATH%" (Windows Error: !MKDIR_ERROR!) >> "%LOGFILE%"
    )
) else (
    echo     [INFO] Map bestaat al, geen actie nodig.
    echo %LOG_PREFIX% [INFO] Map bestaat al: "%FOLDER_PATH%" >> "%LOGFILE%"
)
echo. >> "%LOGFILE%" rem Extra witregel in log voor leesbaarheid
goto :eof


:logMessage
rem Parameter %1: Boodschap om te loggen.
echo [%DATE% %TIME%] %~1 >> "%LOGFILE%"
goto :eof


:EndScript
call :logMessage "Script beëindigd."
call :logMessage "================================================================================"
echo.
endlocal
pause
exit /b