; ---------- Bovenin ----------
#define MyAppName      "AI-FileOrganizer"

; Versie injecteren vanuit de CLI (/dMyAppVersion=1.7.0)
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif

#define MyAppPublisher "Skills4-IT"
#define MyAppURL       "https://www.aibuddies.nl"
#define MyAppExeName   "AI-FileOrganizer.exe"

; ---------- Setup ----------
[Setup]
AppId={{FE221BE4-56B8-4FD7-AD00-615E3278F31E}}     ; gefixt: ‘{{’ → ‘{’  &  sluit-}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
OutputDir=.
OutputBaseFilename=AI-FileOrganizerSetup
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
SolidCompression=yes
WizardStyle=modern
LicenseFile=Installer\MIT License (MIT-licentie).txt
InfoBeforeFile=Installer\installatie-informatie.txt
; (…rest ongewijzigd…)

; ---------- Bestanden ----------
[Files]
; Pak heel de Release-map mee in één regel:
Source: "..\bin\Release\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
; Wil je wél handmatig filteren? Gebruik ‘_Prepend’ helpers om herhaling te schrappen.

; ---------- Taal ----------
[Languages]
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
