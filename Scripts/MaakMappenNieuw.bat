@echo off
setlocal EnableDelayedExpansion

rem =================================================================================
rem  create_persoonlijke_mappen_enhanced.bat
rem  Versie: 2.2
rem  Geavanceerde tool voor persoonlijke mappenstructuur
rem  Ondersteunt root- en jaartalargumenten, UTF-8 log, validatie, structuur uit bestand
rem =================================================================================

:: ---------- CONFIGURATIE ----------------------------------------------------------
set "SCRIPT_VERSION=2.2"
set "DEFAULT_ROOT_NAME=Persoonlijke Administratie"
set "LOGFILE=%TEMP%\create_persoonlijke_mappen_log.txt"
set "FOLDER_FILE=folders.txt"

:: ---------- INITIALISATIE LOG -----------------------------------------------------
break> "%LOGFILE%"
powershell -Command "Set-Content -Encoding UTF8 -Path '%LOGFILE%' -Value '[INFO] Logbestand aangemaakt (UTF-8)'"

call :logMessage "================================================================================"
call :logMessage "Script versie !SCRIPT_VERSION! gestart op %DATE% %TIME%"
call :logMessage "================================================================================"
echo.

:: ---------- INVOER VAN ARGUMENTEN -------------------------------------------------
set "USER_CHOSEN_ROOT=%~1"
set "INPUT_YEAR=%~2"

:: ---------- ROOTLOCATIE VRAGEN ----------------------------------------------------
set "DEFAULT_ROOT_PATH=%USERPROFILE%\Desktop\%DEFAULT_ROOT_NAME%"
if not defined USER_CHOSEN_ROOT (
    echo Standaard rootlocatie is:
    echo   "%DEFAULT_ROOT_PATH%"
    set /p "USER_CHOSEN_ROOT=Voer een andere locatie in of druk Enter voor standaard: "
    if "!USER_CHOSEN_ROOT!"=="" set "USER_CHOSEN_ROOT=%DEFAULT_ROOT_PATH%"
)

call :validatePath "!USER_CHOSEN_ROOT!" || goto EndScript
set "ROOT=!USER_CHOSEN_ROOT!"
call :logMessage "[CONFIG] Gekozen rootlocatie: !ROOT!"

:: ---------- JAAR OPHALEN ----------------------------------------------------------
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set "datetime=%%I"
set "CURRENT_YEAR=%datetime:~0,4%"
if not defined INPUT_YEAR (
    echo Huidig jaar is: %CURRENT_YEAR%
    set /p "INPUT_YEAR=Voer jaartal in voor mappenstructuur (Enter voor huidig): "
    if "!INPUT_YEAR!"=="" set "INPUT_YEAR=%CURRENT_YEAR%"
)
echo !INPUT_YEAR! | findstr /r /c:"^[1-9][0-9][0-9][0-9]$" >nul || (
    echo Ongeldig jaartal ingevoerd. Standaardjaar %CURRENT_YEAR% wordt gebruikt.
    set "INPUT_YEAR=%CURRENT_YEAR%"
)
set "TARGET_YEAR=%INPUT_YEAR%"
set /a PREVIOUS_YEAR=TARGET_YEAR-1

call :logMessage "[CONFIG] Doeljaar: %TARGET_YEAR%, Archiefjaar: %PREVIOUS_YEAR%"

:: ---------- BEVESTIGING -----------------------------------------------------------
echo ------------------------------------------------------------------------------
echo Rootlocatie : !ROOT!
echo Jaar        : !TARGET_YEAR!
echo Archiefjaar: !PREVIOUS_YEAR!
echo ------------------------------------------------------------------------------
pause

:: ---------- HOOFDMAP MAKEN --------------------------------------------------------
call :createDir "!ROOT!" || (
    echo [FATAAL] Kan hoofdmap niet aanmaken. Controleer pad of rechten.
    goto EndScript
)

:: ---------- STRUCTUUR MAKEN -------------------------------------------------------
if exist "%FOLDER_FILE%" (
    call :logMessage "[INFO] Folders.txt gevonden. Structuur wordt uit bestand ingelezen."
    for /f "usebackq tokens=* delims=" %%F in ("%FOLDER_FILE%") do (
        call :createDir "!ROOT!\%%F"
    )
) else (
    call :logMessage "[INFO] Geen folders.txt gevonden. Standaardstructuur wordt gebruikt."

    call :createDir "!ROOT!\1. Financien\Bankafschriften\!TARGET_YEAR!"
    call :createDir "!ROOT!\1. Financien\Spaarrekeningen"
    call :createDir "!ROOT!\2. Belastingen\Aangiften Inkomstenbelasting\!TARGET_YEAR!"
    call :createDir "!ROOT!\3. Verzekeringen\Zorgverzekering"
    call :createDir "!ROOT!\4. Woning\Hypotheek of Huur"
    call :createDir "!ROOT!\5. Gezondheid\Medische dossiers"
    call :createDir "!ROOT!\6. Voertuigen\Onderhoudsrecords"
    call :createDir "!ROOT!\7. Carriere\CV"
    call :createDir "!ROOT!\8. Reizen\!TARGET_YEAR! Andalusie"
    call :createDir "!ROOT!\9. Hobby\YouTube concepten"
    call :createDir "!ROOT!\10. Familie en kinderen\Activiteiten en vakanties"
    call :createDir "!ROOT!\11. Digitale bezittingen\Software licenties"
    call :createDir "!ROOT!\12. Abonnementen en lidmaatschappen\Streaming"
    call :createDir "!ROOT!\13. Foto en video\!TARGET_YEAR!\06 Graduatie"
    call :createDir "!ROOT!\14. Opleiding\Certificaten"
    call :createDir "!ROOT!\15. Juridisch\Contracten"
    call :createDir "!ROOT!\16. Nalatenschap\Uitvaartwensen"
    call :createDir "!ROOT!\17. Noodinformatie\Paspoort scans"
    call :createDir "!ROOT!\18. Huisinventaris\Aankoopbewijzen"
    call :createDir "!ROOT!\19. Persoonlijke projecten\Side hustle ideeen"
    call :createDir "!ROOT!\20. Huisdieren\Dierenarts"
    call :createDir "!ROOT!\99. Archief\!PREVIOUS_YEAR!\Oude projecten"
)

echo ------------------------------------------------------------------------------
echo [✓] Structuur aangemaakt in: !ROOT!
echo [i] Logbestand: !LOGFILE!
echo ------------------------------------------------------------------------------
goto EndScript

:: ---------- SUBROUTINES ----------------------------------------------------------

:createDir
set "FOLDER_PATH=%~1"
echo [•] Maken: %FOLDER_PATH%
if not exist "!FOLDER_PATH!" (
    mkdir "!FOLDER_PATH!" 2>nul
    if !errorlevel! equ 0 (
        call :logMessage "[OK] Map gemaakt: !FOLDER_PATH!"
    ) else (
        call :logMessage "[ERROR] Fout bij maken: !FOLDER_PATH!"
        echo [FOUT] Kon map niet maken: !FOLDER_PATH!
    )
) else (
    call :logMessage "[INFO] Bestaat al: !FOLDER_PATH!"
)
goto :eof

:logMessage
echo [%DATE% %TIME%] %~1 >> "%LOGFILE%"
goto :eof

:validatePath
echo %~1 | findstr /r /c:"[<>:\"/\\|?*]" >nul
if %errorlevel% neq 0 (
    call :logMessage "[ERROR] Ongeldig pad: %~1"
    echo [FOUT] Ongeldig teken in pad: %~1
    exit /b 1
)
exit /b 0

:EndScript
call :logMessage "Script beëindigd op %DATE% %TIME%"
call :logMessage "================================================================================"
endlocal
pause
exit /b
