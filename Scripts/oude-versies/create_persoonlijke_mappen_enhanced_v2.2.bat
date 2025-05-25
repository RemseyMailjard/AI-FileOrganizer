@echo off
setlocal enabledelayedexpansion

rem ================================================================================
rem  create_persoonlijke_mappen_enhanced.bat
rem  Versie: 2.2
rem  Maakt een configureerbare persoonlijke mappen­structuur aan op basis van een extern TXT-bestand.
rem  Functionaliteiten:
rem  - Leest mappenstructuur uit een TXT-bestand.
rem  - Gebruiker kan root-locatie kiezen.
rem  - Gebruiker kan jaartal voor mappen specificeren.
rem  - Controleert of mappen al bestaan voordat ze worden aangemaakt.
rem  - Gedetailleerde logging van acties.
rem  - Foutafhandeling bij het aanmaken van mappen.
rem  - Duidelijke bevestiging na gebruikersinteractie.
rem ================================================================================

:: ----- CONFIGURATIE & INITIALISATIE ---------------------------------------------
set "SCRIPT_VERSION=2.2"
set "LOGFILE=%TEMP%\create_persoonlijke_mappen_log.txt"
set "DEFAULT_ROOT_NAME=Persoonlijke Administratie"
set "STRUCTURE_FILE_NAME=map_structuur.txt"
set "STRUCTURE_FILE_PATH=%~dp0%STRUCTURE_FILE_NAME%" rem %~dp0 is de map van het huidige script

:: Initialiseer logbestand (overschrijft oud logbestand bij elke run)
echo. > "%LOGFILE%"
call :logMessage "================================================================================"
call :logMessage "Script create_persoonlijke_mappen_enhanced.bat (Versie: !SCRIPT_VERSION!) gestart."
call :logMessage "================================================================================"
echo.

:: ----- 0. CONTROLEER BESTAAN STRUCTUURBESTAND -----------------------------------
call :logMessage "[CHECK] Controleren of structuurbestand '%STRUCTURE_FILE_PATH%' bestaat."
if not exist "%STRUCTURE_FILE_PATH%" (
    echo !!! FOUT: Het structuurbestand "%STRUCTURE_FILE_NAME%" kon niet worden gevonden in:
    echo   %~dp0
    echo.
    echo Zorg ervoor dat dit bestand bestaat en in dezelfde map staat als het script,
    echo of pas de variabele 'STRUCTURE_FILE_PATH' in het script aan.
    echo Script stopt.
    call :logMessage "[FATAL] Structuurbestand '%STRUCTURE_FILE_PATH%' niet gevonden. Script afgebroken."
    goto EndScriptEarly
)
call :logMessage "[INFO] Structuurbestand '%STRUCTURE_FILE_PATH%' gevonden."
echo Structuurbestand "%STRUCTURE_FILE_NAME%" wordt gebruikt.
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
    if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
    if "%ROOT:~-1%"=="/" set "ROOT=%ROOT:~0,-1%"
    call :logMessage "[CONFIG] Door gebruiker gekozen rootlocatie: %ROOT%"
)
echo.
echo Gekozen locatie voor de mappenstructuur:
echo   "%ROOT%"
echo.

:: ----- 2. BEPAAL DOELJAAR EN VORIG JAAR -----------------------------------------
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
echo  Gebaseerd op structuur uit: "%STRUCTURE_FILE_NAME%"
echo ================================================================================
echo Controleer bovenstaande gegevens.
echo.
pause
echo.
echo OK. Bezig met het voorbereiden en aanmaken van de mappenstructuur...
call :logMessage "[USER_CONFIRMED] Gebruiker heeft bevestigd na controle van instellingen."
call :logMessage "[ACTION] Starten met aanmaken mappenstructuur vanuit '%STRUCTURE_FILE_PATH%'."
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

:: ----- 5. LEES STRUCTUURBESTAND EN MAAK MAPPEN AAN --------------------------------
call :logMessage "[INFO] Start aanmaken van submappenstructuur vanuit '%STRUCTURE_FILE_PATH%'."
echo Mappen worden aangemaakt op basis van "%STRUCTURE_FILE_NAME%":
echo.

for /F "usebackq eol=# tokens=* delims=" %%L in ("%STRUCTURE_FILE_PATH%") do (
    set "raw_line=%%L"
    rem Skip 'rem' comments as well
    echo "!raw_line!" | findstr /I /B /C:"rem " >nul
    if not errorlevel 1 (
        call :logMessage "[SKIP_COMMENT_REM] Regel overgeslagen (REM comment): !raw_line!"
        goto :next_line
    )
    
    rem Trim leading/trailing whitespace (optional, but good for robustness)
    for /f "tokens=* delims= " %%T in ("!raw_line!") do set "trimmed_line=%%T"
    
    if defined trimmed_line if not "!trimmed_line!"=="" (
        set "folder_to_create=!trimmed_line!"
        call :logMessage "[PARSE] Verwerken regel: !folder_to_create!"
        call :createDir "%ROOT%\!folder_to_create!"
    ) else (
        call :logMessage "[SKIP_EMPTY] Lege regel overgeslagen."
    )
    :next_line
)

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

rem De delayed expansion !TARGET_YEAR! en !PREVIOUS_YEAR! binnen FOLDER_PATH wordt hier afgehandeld.
echo %LOG_PREFIX% [CHECK] Bezig met map: "!FOLDER_PATH!" >> "%LOGFILE%"
echo   Bezig met map: "!FOLDER_PATH!"

if not exist "!FOLDER_PATH!" (
    mkdir "!FOLDER_PATH!"
    if not errorlevel 1 (
        echo     [OK] Map succesvol aangemaakt.
        echo %LOG_PREFIX% [SUCCESS] Map aangemaakt: "!FOLDER_PATH!" >> "%LOGFILE%"
    ) else (
        set "MKDIR_ERROR=!errorlevel!"
        echo     [FOUT!] Kon map NIET aanmaken! (Windows Error: !MKDIR_ERROR!)
        echo %LOG_PREFIX% [ERROR] Kon map NIET aanmaken: "!FOLDER_PATH!" (Windows Error: !MKDIR_ERROR!) >> "%LOGFILE%"
    )
) else (
    echo     [INFO] Map bestaat al, geen actie nodig.
    echo %LOG_PREFIX% [INFO] Map bestaat al: "!FOLDER_PATH!" >> "%LOGFILE%"
)
echo. >> "%LOGFILE%"
goto :eof


:logMessage
rem Parameter %1: Boodschap om te loggen.
echo [%DATE% %TIME%] %~1 >> "%LOGFILE%"
goto :eof

:EndScriptEarly
call :logMessage "Script voortijdig beëindigd vanwege fout."
call :logMessage "================================================================================"
echo.
endlocal
pause
exit /b 1

:EndScript
call :logMessage "Script succesvol beëindigd."
call :logMessage "================================================================================"
echo.
endlocal
pause
exit /b 0