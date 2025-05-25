@echo off
setlocal EnableDelayedExpansion

rem ================================================================================
rem  create_persoonlijke_mappen_enhanced.bat
rem  Versie: 2.2
rem  Verbeterde batchscript voor persoonlijke mappenstructuur
rem ================================================================================

:: ---------- CONFIG & INIT --------------------------------------------------------
set "SCRIPT_VERSION=2.2"
set "LOGFILE=%TEMP%\create_persoonlijke_mappen_log.txt"
set "DEFAULT_ROOT_NAME=Persoonlijke Administratie"

rem Initialiseer logbestand
echo. > "%LOGFILE%"
call :logMessage "================================================================================"
call :logMessage "Script create_persoonlijke_mappen_enhanced.bat (Versie: !SCRIPT_VERSION!) gestart."
call :logMessage "================================================================================"
echo.

:: ---------- ROOT LOCATIE ---------------------------------------------------------
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

    echo !ROOT! | findstr /r /c:"[<>:\"/\\|?*]" >nul
    if !errorlevel! equ 0 (
        echo [FOUT] Ongeldige tekens in pad. Script afgebroken.
        call :logMessage "[ERROR] Ongeldig pad ingevoerd: !ROOT!"
        goto EndScript
    )
    call :logMessage "[CONFIG] Door gebruiker gekozen rootlocatie: %ROOT%"
)

echo.
echo Gekozen locatie voor de mappenstructuur:
echo   "%ROOT%"
echo.

:: ---------- JAAR & ARCHIEF -------------------------------------------------------
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set "datetime=%%I"
set "CURRENT_YEAR=%datetime:~0,4%"
set "TARGET_YEAR=%CURRENT_YEAR%"

call :logMessage "[CONFIG] Huidig systeemjaar gedetecteerd: %CURRENT_YEAR%"
echo Het huidige jaar is: !CURRENT_YEAR!
set /p "INPUT_YEAR=Voor welk jaar moeten de mappen worden aangemaakt? (Standaard: !CURRENT_YEAR!): "

if defined INPUT_YEAR if "!INPUT_YEAR!" NEQ "" (
    echo !INPUT_YEAR! | findstr /r /c:"^[1-9][0-9][0-9][0-9]$" >nul
    if !errorlevel! equ 0 (
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

:: ---------- BEVESTIGING ----------------------------------------------------------
echo ================================================================================
echo  Persoonlijke mappenstructuur wordt nu aangemaakt in:
echo      "%ROOT%"
echo  Voor het jaar: !TARGET_YEAR! (Archief: !PREVIOUS_YEAR!)
echo ================================================================================
pause
echo.
call :logMessage "[USER_CONFIRMED] Gebruiker heeft bevestigd na controle van instellingen."

:: ---------- MAAK HOOFDMAP ---------------------------------------------------------
if not exist "!ROOT!" (
    mkdir "!ROOT!"
    if !errorlevel! neq 0 (
        echo [FOUT] Kon hoofdmap niet aanmaken: "!ROOT!"
        call :logMessage "[FATAL] Kon hoofdmap niet aanmaken: !ROOT!"
        goto EndScript
    )
    call :logMessage "[SUCCESS] Hoofdmap aangemaakt: !ROOT!"
) else (
    call :logMessage "[INFO] Hoofdmap bestaat al: !ROOT!"
)

echo OK. Bezig met aanmaken van de mappenstructuur...
call :logMessage "[ACTION] Start submappenstructuur."

:: ---------- SUBMAPPEN ------------------------------------------------------------
rem Voorbeeld — voeg hier alle mappen zoals eerder gedefinieerd toe
call :createDir "%ROOT%\1. Financien\Bankafschriften\!TARGET_YEAR!"
call :createDir "%ROOT%\1. Financien\Spaarrekeningen"
call :createDir "%ROOT%\99. Archief\!PREVIOUS_YEAR!\Oude projecten"
:: Voeg al je overige `call :createDir ...` hier toe

echo.
echo ********************************************************************************
echo *** KLAAR! De mappenstructuur is aangemaakt.
echo *** Bekijk logbestand in:
echo ***   %LOGFILE%
echo ********************************************************************************
echo.

goto EndScript

:: ---------- SUBROUTINES ----------------------------------------------------------

:createDir
set "FOLDER_PATH=%~1"
call :logMessage "[CHECK] Map: %FOLDER_PATH%"
echo Bezig met: %FOLDER_PATH%

if not exist "%FOLDER_PATH%" (
    mkdir "%FOLDER_PATH%" 2>nul
    if !errorlevel! equ 0 (
        echo   [OK] Aangemaakt
        call :logMessage "[SUCCESS] Gemaakt: %FOLDER_PATH%"
    ) else (
        echo   [FOUT] Kon map niet maken!
        call :logMessage "[ERROR] Mislukt: %FOLDER_PATH%"
    )
) else (
    echo   [INFO] Bestond al
    call :logMessage "[INFO] Bestond al: %FOLDER_PATH%"
)
goto :eof

:logMessage
echo [%DATE% %TIME%] %~1 >> "%LOGFILE%"
goto :eof

:EndScript
call :logMessage "Script beëindigd."
call :logMessage "================================================================================"
echo.
endlocal
pause
exit /b
