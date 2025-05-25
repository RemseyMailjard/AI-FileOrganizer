@echo off
setlocal EnableDelayedExpansion

rem ============================================================================
rem  create_structuur_via_folders_txt.bat
rem  Versie: 1.0
rem  Leest folderstructuur uit folders.txt en vervangt {YEAR} en {PREVIOUS_YEAR}
rem ============================================================================

set "DEFAULT_ROOT=%USERPROFILE%\Desktop\Persoonlijke Administratie"
set /p "USER_ROOT=Voer root-locatie in (of druk Enter voor standaard: %DEFAULT_ROOT%): "
if "%USER_ROOT%"=="" set "USER_ROOT=%DEFAULT_ROOT%"

set /p "TARGET_YEAR=Voor welk jaar wil je mappen aanmaken? (bijv. 2025): "
if "%TARGET_YEAR%"=="" set "TARGET_YEAR=%DATE:~6,4%"

set /a PREVIOUS_YEAR=%TARGET_YEAR%-1

echo.
echo Rootlocatie: %USER_ROOT%
echo Jaar:        %TARGET_YEAR%
echo Archiefjaar: %PREVIOUS_YEAR%
echo.
pause

if not exist "folders.txt" (
    echo FOUT: folders.txt niet gevonden in deze map.
    pause
    exit /b 1
)

for /f "usebackq tokens=* delims=" %%L in ("folders.txt") do (
    set "line=%%L"
    if not "!line!"=="" (
        echo !line! | findstr /b /c:"#">nul && (
            rem Sla commentaar over
        ) else (
            set "folder=!line:{YEAR}=%TARGET_YEAR%!"
            set "folder=!folder:{PREVIOUS_YEAR}=%PREVIOUS_YEAR%!"
            set "path=%USER_ROOT%\!folder!"
            if not exist "!path!" (
                mkdir "!path!" 2>nul && (
                    echo [Aangemaakt] !path!
                ) || (
                    echo [FOUT] Kon !path! niet maken
                )
            ) else (
                echo [Bestaat]   !path!
            )
        )
    )
)

echo.
echo Klaar! Structuur aangemaakt of bijgewerkt.
pause
endlocal
