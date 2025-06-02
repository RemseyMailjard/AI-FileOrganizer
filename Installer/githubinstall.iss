; ------------------------------------------------------------
; Inno Setup-script voor AI-FileOrganizer
; ------------------------------------------------------------
#define MyAppName "AI-FileOrganizer"

; Versie-injectie via CLI:  iscc ... /dMyAppVersion=1.8.0
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif

#define MyAppPublisher "Skills4-IT"
#define MyAppURL       "https://www.aibuddies.nl"
#define MyAppExeName   "AI-FileOrganizer.exe"

; ──────────────────────────────────────────────────────────────
[Setup]
AppId={{FE221BE4-56B8-4FD7-AD00-615E3278F31E}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

OutputDir=.
OutputBaseFilename=AI-FileOrganizerSetup
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

LicenseFile="Installer\MIT License (MIT-licentie).txt"
InfoBeforeFile="Installer\installatie-informatie.txt"

[Files]
; Neem alles uit bin\Release\ mee
Source: "..\bin\Release\*"; DestDir: "{app}";
       Flags: recursesubdirs createallsubdirs ignoreversion

[Languages]
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
