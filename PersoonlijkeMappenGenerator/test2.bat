@echo off
setlocal enabledelayedexpansion

rem ================================================================================
rem  PersoonlijkeMappenStructuurGenerator.bat
rem  Versie: 2.1 - ALTIJD correcte jaarvervanging + volledige categorieën fallback
rem  Gemaakt door Remsey Mailjard (https://nl.linkedin.com/in/remseymailjard)
rem ================================================================================

:: Welkomstbericht
echo.
echo ===============================
echo Gemaakt door Remsey Mailjard
echo LinkedIn: https://nl.linkedin.com/in/remseymailjard
echo ===============================
echo.

:: Initialisatie
set "SCRIPT_VERSION=2.1"
set "LOGFILE=%TEMP%\PersoonlijkeMappenStructuurGenerator_log.txt"
set "MAP_DEFINITION_FILE=%~dp0map_structuur.txt"
set "SCRIPT_SUCCESSFUL=true"
set /a FOLDERS_CREATED_COUNT=0
set /a FOLDERS_EXISTED_COUNT=0
set /a FOLDERS_FAILED_COUNT=0
set "USE_EXTERNAL_MAP_FILE=false"

:: Logbestand aanmaken
echo. > "%LOGFILE%"
call :logMessage "Script gestart (v%SCRIPT_VERSION%)."

:: Controle op externe structuur
if exist "!MAP_DEFINITION_FILE!" (
    set "USE_EXTERNAL_MAP_FILE=true"
    call :logMessage "Mappen definitiebestand gevonden: !MAP_DEFINITION_FILE!. Deze zal worden gebruikt."
) else (
    call :logMessage "Mappen definitiebestand (!MAP_DEFINITION_FILE!) niet gevonden. Standaard ingebouwde structuur zal worden gebruikt."
)

:: Root-locatie bepalen
set "DEFAULT_ROOT=%USERPROFILE%\Desktop\Persoonlijke Administratie"
echo.
echo =============================================================
echo  Persoonlijke mappenstructuur wordt nu aangemaakt!
if "!USE_EXTERNAL_MAP_FILE!"=="true" (
    echo  (Structuur gelezen uit: !MAP_DEFINITION_FILE!)
) else (
    echo  (Standaard ingebouwde structuur wordt gebruikt)
)
echo =============================================================
echo Standaard locatie is: "%DEFAULT_ROOT%"
set /p "ROOT=Wil je een andere locatie? (laat leeg voor standaard): "
if "!ROOT!"=="" set "ROOT=%DEFAULT_ROOT%"
if "!ROOT:~-1!"=="\" set "ROOT=!ROOT:~0,-1!"

:: Huidig jaar & vorig jaar bepalen
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set "datetime=%%I"
set "CURRENT_YEAR=%datetime:~0,4%"
set "TARGET_YEAR=!CURRENT_YEAR!"
set /a PREVIOUS_YEAR=!TARGET_YEAR!-1

:: Input van gebruiker: jaar
echo.
set "INPUT_YEAR="
set /p "INPUT_YEAR=Voor welk jaar moeten de mappen worden aangemaakt? (standaard: !CURRENT_YEAR!): "

:: Validatie en herberekening previous year
if "!INPUT_YEAR!"=="" (
    set "TARGET_YEAR=!CURRENT_YEAR!"
    set /a PREVIOUS_YEAR=!TARGET_YEAR!-1
) else (
    set /a TEST_YEAR=0 2>nul
    set /a TEST_YEAR=!INPUT_YEAR! 2>nul
    if errorlevel 1 (
        set "TARGET_YEAR=!CURRENT_YEAR!"
        set /a PREVIOUS_YEAR=!TARGET_YEAR!-1
    ) else if !TEST_YEAR! lss 1000 (
        set "TARGET_YEAR=!CURRENT_YEAR!"
        set /a PREVIOUS_YEAR=!TARGET_YEAR!-1
    ) else if !TEST_YEAR! gtr 9999 (
        set "TARGET_YEAR=!CURRENT_YEAR!"
        set /a PREVIOUS_YEAR=!TARGET_YEAR!-1
    ) else (
        set "TARGET_YEAR=!TEST_YEAR!"
        set /a PREVIOUS_YEAR=!TARGET_YEAR!-1
    )
)

echo [DEBUG] TARGET_YEAR=!TARGET_YEAR!  PREVIOUS_YEAR=!PREVIOUS_YEAR!
call :logMessage "In gebruik: TARGET_YEAR=!TARGET_YEAR!, PREVIOUS_YEAR=!PREVIOUS_YEAR!"

:: Overzicht & bevestiging
echo.
echo =============================================================
echo Mappenstructuur wordt aangemaakt in:
echo    "!ROOT!"
echo Voor het jaar: !TARGET_YEAR!
echo Archief voor: !PREVIOUS_YEAR!
echo =============================================================
echo Druk op Enter om te starten, of sluit dit venster om te annuleren.
pause

:: Hoofdmap aanmaken
call :createSingleDir "!ROOT!"
if not exist "!ROOT!" (
    echo [FOUT] Kan hoofdmap niet aanmaken: "!ROOT!"
    call :logMessage "FOUT: Hoofdmap niet aangemaakt."
    set "SCRIPT_SUCCESSFUL=false"
    goto EndScriptProcessing
)

:: ----------- STRUCTUUR OPBOUW -----------
if "!USE_EXTERNAL_MAP_FILE!"=="true" (
    call :logMessage "Verwerken van mappenstructuur uit !MAP_DEFINITION_FILE!..."
    for /f "usebackq delims=" %%L in ("!MAP_DEFINITION_FILE!") do (
        set "LINE=%%L"
        call :trim LINE
        set "SKIP_LINE="
        if not defined LINE set "SKIP_LINE=1"
        if "!LINE!"=="" set "SKIP_LINE=1"
        if "!LINE:~0,1!"=="#" set "SKIP_LINE=1"
        if /i "!LINE:~0,3!"=="rem" (
            set "CHECK_REM_SUFFIX=!LINE:~3!"
            if not defined CHECK_REM_SUFFIX set "SKIP_LINE=1"
            if "!CHECK_REM_SUFFIX:~0,1!"==" " set "SKIP_LINE=1"
            if "!CHECK_REM_SUFFIX!"=="" set "SKIP_LINE=1"
        )
        if not defined SKIP_LINE (
            set "PROCESSED_LINE=!LINE!"
            set "PROCESSED_LINE=!PROCESSED_LINE:!TARGET_YEAR!=!TARGET_YEAR!!"
            set "PROCESSED_LINE=!PROCESSED_LINE:!PREVIOUS_YEAR!=!PREVIOUS_YEAR!!"
            set "PROCESSED_LINE=!PROCESSED_LINE:/=\!"
            if defined PROCESSED_LINE (
                if "!PROCESSED_LINE:~0,1!"=="\" set "PROCESSED_LINE=!PROCESSED_LINE:~1!"
                if defined PROCESSED_LINE (
                    if "!PROCESSED_LINE:~-1!"=="\" set "PROCESSED_LINE=!PROCESSED_LINE:~0,-1!"
                )
            )
            if defined PROCESSED_LINE (
                if not "!PROCESSED_LINE!"=="" (
                    set "FULL_PATH=!ROOT!\!PROCESSED_LINE!"
                    call :createSingleDir "!FULL_PATH!"
                )
            )
        )
    )
    call :logMessage "Verwerking van !MAP_DEFINITION_FILE! voltooid."
) else (
    call :logMessage "Gebruik van standaard ingebouwde mappenstructuur..."
    rem ---- ALLE CATEGORIEËN, altijd met !TARGET_YEAR! en !PREVIOUS_YEAR! ----

    call :createSingleDir "!ROOT!\1. Financien"
    call :createSingleDir "!ROOT!\1. Financien\Bankafschriften"
    call :createSingleDir "!ROOT!\1. Financien\Bankafschriften\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\1. Financien\Creditcard Afschriften"
    call :createSingleDir "!ROOT!\1. Financien\Creditcard Afschriften\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\1. Financien\Spaarrekeningen"
    call :createSingleDir "!ROOT!\1. Financien\Beleggingen"
    call :createSingleDir "!ROOT!\1. Financien\Beleggingen\!TARGET_YEAR! Transacties"
    call :createSingleDir "!ROOT!\1. Financien\Beleggingen\Jaaroverzichten"
    call :createSingleDir "!ROOT!\1. Financien\Leningen en Kredieten"
    call :createSingleDir "!ROOT!\1. Financien\Budgetten en Uitgaven Tracking"
    call :createSingleDir "!ROOT!\1. Financien\Facturen Betaald"
    call :createSingleDir "!ROOT!\1. Financien\Facturen Betaald\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\1. Financien\Cryptocurrency (indien van toepassing)"

    call :createSingleDir "!ROOT!\2. Belastingen"
    call :createSingleDir "!ROOT!\2. Belastingen\Aangiften Inkomstenbelasting"
    call :createSingleDir "!ROOT!\2. Belastingen\Aangiften Inkomstenbelasting\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\2. Belastingen\Voorlopige Aanslagen"
    call :createSingleDir "!ROOT!\2. Belastingen\Voorlopige Aanslagen\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\2. Belastingen\Correspondentie Belastingdienst"
    call :createSingleDir "!ROOT!\2. Belastingen\Correspondentie Belastingdienst\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\2. Belastingen\Toeslagen (Zorg, Huur, etc.)"
    call :createSingleDir "!ROOT!\2. Belastingen\Toeslagen\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\2. Belastingen\Jaaropgaven Werkgevers"
    call :createSingleDir "!ROOT!\2. Belastingen\Jaaropgaven Werkgevers\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\2. Belastingen\Fiscale Documenten Banken en Beleggingen"

    call :createSingleDir "!ROOT!\3. Verzekeringen"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Zorgverzekering"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Zorgverzekering\!TARGET_YEAR! Polis en Declaraties"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Inboedel en Opstalverzekering"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Inboedel en Opstalverzekering\!TARGET_YEAR! Polis"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Aansprakelijkheidsverzekering (AVP)"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Aansprakelijkheidsverzekering (AVP)\!TARGET_YEAR! Polis"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Autoverzekering"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Autoverzekering\!TARGET_YEAR! Polis en Schadeclaims"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Reisverzekering"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Levensverzekering"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Uitvaartverzekering"
    call :createSingleDir "!ROOT!\3. Verzekeringen\Overige Verzekeringen"

    call :createSingleDir "!ROOT!\4. Woning"
    call :createSingleDir "!ROOT!\4. Woning\Koopakte of Huurcontract"
    call :createSingleDir "!ROOT!\4. Woning\Hypotheekdocumenten"
    call :createSingleDir "!ROOT!\4. Woning\Huuradministratie"
    call :createSingleDir "!ROOT!\4. Woning\Huuradministratie\!TARGET_YEAR! Betalingen"
    call :createSingleDir "!ROOT!\4. Woning\VvE Documenten (indien van toepassing)"
    call :createSingleDir "!ROOT!\4. Woning\Nutsvoorzieningen (Energie, Water, Internet)"
    call :createSingleDir "!ROOT!\4. Woning\Nutsvoorzieningen\!TARGET_YEAR! Facturen"
    call :createSingleDir "!ROOT!\4. Woning\Gemeentelijke Heffingen (OZB, Afval)"
    call :createSingleDir "!ROOT!\4. Woning\Gemeentelijke Heffingen\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\4. Woning\Onderhoud en Reparaties"
    call :createSingleDir "!ROOT!\4. Woning\Onderhoud en Reparaties\Facturen en Offertes"
    call :createSingleDir "!ROOT!\4. Woning\Verbouwingen"
    call :createSingleDir "!ROOT!\4. Woning\Inrichting en Apparatuur (Garanties, Handleidingen)"

    call :createSingleDir "!ROOT!\5. Gezondheid"
    call :createSingleDir "!ROOT!\5. Gezondheid\Medische dossiers (Huisarts, Specialist)"
    call :createSingleDir "!ROOT!\5. Gezondheid\Uitslagen Onderzoeken"
    call :createSingleDir "!ROOT!\5. Gezondheid\Recepten en Medicijnoverzichten"
    call :createSingleDir "!ROOT!\5. Gezondheid\Vaccinatiebewijzen"
    call :createSingleDir "!ROOT!\5. Gezondheid\Zorgdeclaraties Ingediend"
    call :createSingleDir "!ROOT!\5. Gezondheid\Fitness en Welzijn"

    call :createSingleDir "!ROOT!\6. Voertuigen"
    call :createSingleDir "!ROOT!\6. Voertuigen\Aankoopdocumenten"
    call :createSingleDir "!ROOT!\6. Voertuigen\Kentekenbewijzen"
    call :createSingleDir "!ROOT!\6. Voertuigen\Onderhoudsrecords en Facturen"
    call :createSingleDir "!ROOT!\6. Voertuigen\Onderhoudsrecords en Facturen\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\6. Voertuigen\APK Keuringsrapporten"
    call :createSingleDir "!ROOT!\6. Voertuigen\Verzekeringspolissen"
    call :createSingleDir "!ROOT!\6. Voertuigen\Wegenbelasting"
    call :createSingleDir "!ROOT!\6. Voertuigen\Schadeclaims"

    call :createSingleDir "!ROOT!\7. Carriere en Werk"
    call :createSingleDir "!ROOT!\7. Carriere en Werk\CV en Sollicitatiebrieven"
    call :createSingleDir "!ROOT!\7. Carriere en Werk\Arbeidscontracten"
    call :createSingleDir "!ROOT!\7. Carriere en Werk\Salarisstroken"
    call :createSingleDir "!ROOT!\7. Carriere en Werk\Salarisstroken\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\7. Carriere en Werk\Beoordelingen en Functioneringsgesprekken"
    call :createSingleDir "!ROOT!\7. Carriere en Werk\Certificaten en Diploma's"
    call :createSingleDir "!ROOT!\7. Carriere en Werk\Zakelijke Reiskosten Declaraties"
    call :createSingleDir "!ROOT!\7. Carriere en Werk\Pensioendocumenten"

    call :createSingleDir "!ROOT!\8. Reizen en Vakanties"
    call :createSingleDir "!ROOT!\8. Reizen en Vakanties\!TARGET_YEAR! Bestemming 1"
    call :createSingleDir "!ROOT!\8. Reizen en Vakanties\!TARGET_YEAR! Bestemming 2"
    call :createSingleDir "!ROOT!\8. Reizen en Vakanties\Paspoortkopieën en Visa"
    call :createSingleDir "!ROOT!\8. Reizen en Vakanties\Tickets en Boekingsbevestigingen"
    call :createSingleDir "!ROOT!\8. Reizen en Vakanties\Reisverzekeringsdocumenten"

    call :createSingleDir "!ROOT!\9. Hobby en Interesses"
    call :createSingleDir "!ROOT!\9. Hobby en Interesses\Sport en Fitness"
    call :createSingleDir "!ROOT!\9. Hobby en Interesses\Muziek"
    call :createSingleDir "!ROOT!\9. Hobby en Interesses\Lezen"
    call :createSingleDir "!ROOT!\9. Hobby en Interesses\Koken en Recepten"
    call :createSingleDir "!ROOT!\9. Hobby en Interesses\Fotografie"
    call :createSingleDir "!ROOT!\9. Hobby en Interesses\Creatieve Projecten"
    call :createSingleDir "!ROOT!\9. Hobby en Interesses\YouTube Concepten"

    call :createSingleDir "!ROOT!\10. Familie en Kinderen"
    call :createSingleDir "!ROOT!\10. Familie en Kinderen\Geboorteaktes"
    call :createSingleDir "!ROOT!\10. Familie en Kinderen\School en Onderwijs"
    call :createSingleDir "!ROOT!\10. Familie en Kinderen\School en Onderwijs\Kind 1\!TARGET_YEAR! Rapporten"
    call :createSingleDir "!ROOT!\10. Familie en Kinderen\Activiteiten en Vakanties"
    call :createSingleDir "!ROOT!\10. Familie en Kinderen\Medische Gegevens Kinderen"
    call :createSingleDir "!ROOT!\10. Familie en Kinderen\Belangrijke Correspondentie"

    call :createSingleDir "!ROOT!\11. Digitale Bezittingen"
    call :createSingleDir "!ROOT!\11. Digitale Bezittingen\Wachtwoordbeheer"
    call :createSingleDir "!ROOT!\11. Digitale Bezittingen\2FA Backup Codes"
    call :createSingleDir "!ROOT!\11. Digitale Bezittingen\Software Licenties en Sleutels"
    call :createSingleDir "!ROOT!\11. Digitale Bezittingen\Domeinnamen en Hosting"
    call :createSingleDir "!ROOT!\11. Digitale Bezittingen\Online Abonnementen Overzicht"
    call :createSingleDir "!ROOT!\11. Digitale Bezittingen\Backup Strategie en Logs"

    call :createSingleDir "!ROOT!\12. Abonnementen en Lidmaatschappen"
    call :createSingleDir "!ROOT!\12. Abonnementen en Lidmaatschappen\Streamingdiensten"
    call :createSingleDir "!ROOT!\12. Abonnementen en Lidmaatschappen\Kranten en Tijdschriften"
    call :createSingleDir "!ROOT!\12. Abonnementen en Lidmaatschappen\Sportclub en Verenigingen"
    call :createSingleDir "!ROOT!\12. Abonnementen en Lidmaatschappen\Software Abonnementen"
    call :createSingleDir "!ROOT!\12. Abonnementen en Lidmaatschappen\Overige Abonnementen en Contracten"

    call :createSingleDir "!ROOT!\13. Foto en Video"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\01 Januari"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\02 Februari"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\03 Maart"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\04 April"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\05 Mei"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\06 Juni"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\07 Juli"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\08 Augustus"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\09 September"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\10 Oktober"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\11 November"
    call :createSingleDir "!ROOT!\13. Foto en Video\!TARGET_YEAR!\12 December"
    call :createSingleDir "!ROOT!\13. Foto en Video\Speciale Gebeurtenissen"
    call :createSingleDir "!ROOT!\13. Foto en Video\Bewerkte Projecten"

    call :createSingleDir "!ROOT!\14. Opleiding en Studie"
    call :createSingleDir "!ROOT!\14. Opleiding en Studie\Diploma's en Certificaten"
    call :createSingleDir "!ROOT!\14. Opleiding en Studie\Cursusmateriaal"
    call :createSingleDir "!ROOT!\14. Opleiding en Studie\Aantekeningen"
    call :createSingleDir "!ROOT!\14. Opleiding en Studie\Projecten en Scripties"
    call :createSingleDir "!ROOT!\14. Opleiding en Studie\Inschrijvingsbewijzen"

    call :createSingleDir "!ROOT!\15. Juridisch"
    call :createSingleDir "!ROOT!\15. Juridisch\Contracten en Overeenkomsten"
    call :createSingleDir "!ROOT!\15. Juridisch\Juridische Correspondentie"
    call :createSingleDir "!ROOT!\15. Juridisch\Verkeersboetes en Afhandeling"
    call :createSingleDir "!ROOT!\15. Juridisch\Testament en Erfrecht"
    call :createSingleDir "!ROOT!\15. Juridisch\Officiële Documenten Overheid"

    call :createSingleDir "!ROOT!\16. Nalatenschap"
    call :createSingleDir "!ROOT!\16. Nalatenschap\Testament (kopie)"
    call :createSingleDir "!ROOT!\16. Nalatenschap\Levenstestament (kopie)"
    call :createSingleDir "!ROOT!\16. Nalatenschap\Wilsverklaringen"
    call :createSingleDir "!ROOT!\16. Nalatenschap\Uitvaartwensen en Polis"
    call :createSingleDir "!ROOT!\16. Nalatenschap\Lijst Belangrijke Contacten"
    call :createSingleDir "!ROOT!\16. Nalatenschap\Informatie Digitale Nalatenschap"

    call :createSingleDir "!ROOT!\17. Noodinformatie"
    call :createSingleDir "!ROOT!\17. Noodinformatie\Kopieën Paspoort, ID-kaart, Rijbewijs"
    call :createSingleDir "!ROOT!\17. Noodinformatie\ICE (In Case of Emergency) Contacten"
    call :createSingleDir "!ROOT!\17. Noodinformatie\Medische Alert Informatie"
    call :createSingleDir "!ROOT!\17. Noodinformatie\Bloedgroepkaart"

    call :createSingleDir "!ROOT!\18. Huisinventaris"
    call :createSingleDir "!ROOT!\18. Huisinventaris\Lijst van Bezittingen"
    call :createSingleDir "!ROOT!\18. Huisinventaris\Foto's en Video's van Bezittingen"
    call :createSingleDir "!ROOT!\18. Huisinventaris\Aankoopbewijzen en Garanties"
    call :createSingleDir "!ROOT!\18. Huisinventaris\Taxatierapporten"

    call :createSingleDir "!ROOT!\19. Persoonlijke Projecten"
    call :createSingleDir "!ROOT!\19. Persoonlijke Projecten\DIY Projecten"
    call :createSingleDir "!ROOT!\19. Persoonlijke Projecten\Schrijfprojecten"
    call :createSingleDir "!ROOT!\19. Persoonlijke Projecten\Side Hustle Ideeën en Planning"
    call :createSingleDir "!ROOT!\19. Persoonlijke Projecten\Onderzoek en Notities"

    call :createSingleDir "!ROOT!\20. Huisdieren"
    call :createSingleDir "!ROOT!\20. Huisdieren\Dierenpaspoorten en Registratie"
    call :createSingleDir "!ROOT!\20. Huisdieren\Vaccinatieboekjes"
    call :createSingleDir "!ROOT!\20. Huisdieren\Dierenartsrekeningen en Dossiers"
    call :createSingleDir "!ROOT!\20. Huisdieren\Verzekeringsdocumenten Huisdier"
    call :createSingleDir "!ROOT!\20. Huisdieren\Aankoop- of Adoptiepapieren"

    call :createSingleDir "!ROOT!\99. Archief\!PREVIOUS_YEAR!"
    call :createSingleDir "!ROOT!\99. Archief\!PREVIOUS_YEAR!\Afgeronde Projecten"
    call :createSingleDir "!ROOT!\99. Archief\!PREVIOUS_YEAR!\Oude Financiele Documenten"
    call :createSingleDir "!ROOT!\99. Archief\!PREVIOUS_YEAR!\Overige Gearchiveerde Items"
)

:EndScriptProcessing
if "!SCRIPT_SUCCESSFUL!"=="true" (
    echo.
    echo *****************************************************************
    echo *** SUCCES! Alle mappen zijn succesvol verwerkt.              ***
    echo *** Mappen gemaakt: !FOLDERS_CREATED_COUNT!, Bestonden al: !FOLDERS_EXISTED_COUNT!, Mislukt: !FOLDERS_FAILED_COUNT! ***
    echo *****************************************************************
    call :logMessage "Script succesvol beëindigd. Mappen gemaakt: !FOLDERS_CREATED_COUNT!, Bestonden al: !FOLDERS_EXISTED_COUNT!, Mislukt: !FOLDERS_FAILED_COUNT!."
) else (
    echo.
    echo *****************************************************************
    echo *** LET OP: Er zijn fouten opgetreden. Controleer het logbestand: ***
    echo ***        "!LOGFILE!"                                       ***
    echo *** Mappen gemaakt: !FOLDERS_CREATED_COUNT!, Bestonden al: !FOLDERS_EXISTED_COUNT!, Mislukt: !FOLDERS_FAILED_COUNT! ***
    echo *****************************************************************
    call :logMessage "Script beëindigd met fouten. Mappen gemaakt: !FOLDERS_CREATED_COUNT!, Bestonden al: !FOLDERS_EXISTED_COUNT!, Mislukt: !FOLDERS_FAILED_COUNT!."
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

:trim
rem Subroutine om leidende en volgende spaties van een variabele te verwijderen
setlocal
set "value=!%1!"
:trimLoopLead
if defined value if "%value:~0,1%"==" " (
    set "value=%value:~1%"
    goto trimLoopLead
)
:trimLoopTrail
if defined value if "%value:~-1%"==" " (
    set "value=%value:~0,-1%"
    goto trimLoopTrail
)
endlocal & set "%1=%value%"
goto :eof
