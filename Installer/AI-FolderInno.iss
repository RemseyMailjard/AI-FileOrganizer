; ------------------------------------------------------------
; Inno Setup-script voor AI-FileOrganizer
; ------------------------------------------------------------
; Plaats dit bestand in:  Installer\AI-FolderInno.iss
; De oplossing wordt verondersteld te bouwen naar  bin\Release\
; ------------------------------------------------------------

#define MyAppName "AI-FileOrganizer"

; ── Versie kan vanuit CI worden geïnjecteerd:  /dMyAppVersion=1.8.0 ──
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif

#define MyAppPublisher "Skills4-IT"
#define MyAppURL       "https://www.aibuddies.nl"
#define MyAppExeName   "AI-FileOrganizer.exe"

[Setup]
; Unieke GUID - LET OP: sluitende accolade toegevoegd
AppId={{FE221BE4-56B8-4FD7-AD00-615E3278F31E}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

; Installer-metadata
LicenseFile="Installer\MIT License (MIT-licentie).txt"
InfoBeforeFile="Installer\installatie-informatie.txt"

; CI-vriendelijke instellingen
OutputDir=.
OutputBaseFilename=AI-FileOrganizerSetup
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DisableProgramGroupPage=yes

[Languages]
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; \
      GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Neem de volledige Release-map op—incl. submappen en DLL’s
; (Het script zit in Installer\, daarom één map omhoog)
Source: "..\bin\Release\*"; DestDir: "{app}"; \
       Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; \
      Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
          Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
          Flags: nowait postinstall skipifsilent
