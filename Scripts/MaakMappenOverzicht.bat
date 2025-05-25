@echo off
rem ==========================================================
rem  dump_folder_structure.bat
rem  Schrijft de mappenstructuur van een opgegeven folder
rem  of de standaard Desktopmap van de gebruiker
rem ==========================================================

setlocal enabledelayedexpansion

:: Automatisch de Desktopmap van de ingelogde gebruiker bepalen
set "DEFAULT_SRC=%USERPROFILE%\Desktop"
set "OUT=%~dp0folder_structure.txt"

echo.
echo ==========================================================
echo  FOLDER STRUCTURE EXPORTER
echo ==========================================================
echo.
echo Standaard locatie is: "!DEFAULT_SRC!"
set /p "USE_DEFAULT=Wil je deze gebruiken? (J/N): "

if /I "!USE_DEFAULT!"=="J" (
    set "SRC=!DEFAULT_SRC!"
) else (
    set /p "SRC=Voer het volledige pad in van de folder die je wilt analyseren: "
)

if not exist "!SRC!" (
    echo.
    echo ❌ ERROR: De opgegeven folder "!SRC!" bestaat niet.
    pause
    exit /b 1
)

echo.
echo 🔍 Exporteren van de mappenstructuur van: "!SRC!"
tree "!SRC!" /A > "!OUT!"

echo.
echo ✅ Folderstructuur opgeslagen in: "!OUT!"
pause
