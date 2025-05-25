@echo off
setlocal enabledelayedexpansion

rem ==============================================================
rem  create_persoonlijke_mappen_advanced.bat
rem  Maakt een flexibele, configureerbare persoonlijke mappenstructuur aan.
rem  - Jaar en locatie naar keuze
rem  - Structuur in folders.txt
rem  - Logging, foutafhandeling en samenvatting
rem ==============================================================

set "SCRIPT_VERSION=2.0"
set "DEFAULT_ROOT=%USERPROFILE%\Desktop\Persoonlijke Administratie"
set "FOLDER_FILE=folders.txt"
set "LOGFILE=%TEMP%\persoonlijke_mappen_log.txt"

:: --------- ROOT-PAD EN JAAR VRAGEN ----------------------------

echo.
echo =============================================================
echo  Persoonlijke mappenstructuur wordt nu aangemaakt!
echo =============================================================
echo.
echo Standaard locatie is: "%DEFAULT_ROOT%"
set /p "ROOT=Wil je een andere locatie? (laat leeg voor standaard): "
if "!ROOT!"=="" set "ROOT=%DEFAULT_ROOT%"

:: Controle op backslash
if "!ROOT:~-1!"=="\" set "ROOT=!ROOT:~0,-1!"
if "!ROOT:~-1!"=="/" set "ROOT=!ROOT:~0,-1!"

:: ---- JAAR VRAAG ----------------------------------------------
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set "datetime=%%I"
set "JAAR=!datetime:~0,4!"
set /p "JAAR_INPUT=Voor welk jaar? (standaard: !JAAR!): "
if not "!JAAR_INPUT!"=="" set "JAAR=!JAAR_INPUT!"
set /a "VORIG_JAAR=JAAR-1"

echo.
echo Mapstructuur wordt aangemaakt in: "!ROOT!"
echo Voor jaar: !JAAR!, archief voor: !VORIG_JAAR!
echo.
pause

:: ---- CONTROLEREN OF FOLDER_FILE BESTAAT ----------------------
if not exist "!FOLDER_FILE!" (
    echo Fout: Structuurbestand "!FOLDER_FILE!" ontbreekt!
    exit /b 1
)

:: ---- LOG INITIALISEREN ---------------------------------------
echo. > "!LOGFILE!"
echo [START] Scriptversie %SCRIPT_VERSION% gestart op %DATE% %TIME% >> "!LOGFILE!"

:: ---- HOOFDMAP AANMAKEN ---------------------------------------
call :createDir "!ROOT!"

:: ---- STRUCTUUR AANMAKEN --------------------------------------
set /a "nieuw=0"
set /a "bestond=0"
for /f "usebackq delims=" %%F in ("!FOLDER_FILE!") do (
    set "regel=%%F"
    if "!regel!"=="" goto :skip
    set "regel=!regel:[JAAR]=!JAAR!!"
    set "regel=!regel:[VORIG_JAAR]=!VORIG_JAAR!!"
    set "PAD=!ROOT!\!regel!"
    call :createDir "!PAD!" && set /a nieuw+=1 || set /a bestond+=1
    :skip
)

:: ---- SAMENVATTING EN EINDE -----------------------------------
echo.
echo ================== SAMENVATTING ==================
echo Nieuwe mappen aangemaakt: !nieuw!
echo Mappen bestonden al:      !bestond!
echo Logbestand: !LOGFILE!
echo =================================================
echo.
start notepad "!LOGFILE!"
endlocal
pause
exit /b

:: ==== SUBROUTINES ==================================

:createDir
rem %1 = pad
if "%~1"=="" exit /b 1
if not exist "%~1" (
    mkdir "%~1%" >nul 2>&1
    if errorlevel 1 (
        echo [FOUT] Kon niet aanmaken: %~1
        echo [ERROR] Kon niet aanmaken: %~1 >> "!LOGFILE!"
        exit /b 1
    )
    echo [OK] Aangemaakt: %~1
    echo [OK] Aangemaakt: %~1 >> "!LOGFILE!"
    exit /b 0
) else (
    echo [INFO] Bestond al: %~1 >> "!LOGFILE!"
    exit /b 1
)
goto :eof
