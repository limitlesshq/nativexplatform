#define MyAppName "Akeeba eXtract Wizard"
#define MyAppVerName "Akeeba eXtract Wizard 3.5"
#define MyAppVersion "3.5"
#define MyAppPublisher "Akeeba Developers"
#define MyAppURL "http://www.akeebabackup.com/"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{C5A52C02-1618-47DB-8A92-559DE29048EC}
AppName={#MyAppName}
AppVerName={#MyAppVerName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\Akeeba
DefaultGroupName=Akeeba
OutputDir=..\..\Output
OutputBaseFilename=SetupAkeebaExtractWizard
;SetupIconFile=..\..\Output\extract.exe
Compression=lzma/ultra
SolidCompression=true
InternalCompressLevel=ultra
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (c) 2008-2016 Nicholas K. Dionysopoulos / {#MyAppPublisher}
ShowLanguageDialog=no

[Languages]
Name: english; MessagesFile: compiler:Default.isl

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Files]
Source: ..\..\Output\extract.exe; DestDir: {app}; Flags: ignoreversion

[Icons]
Name: {group}\Akeeba eXtract Wizard; Filename: {app}\extract.exe; WorkingDir: {userdocs}; IconFilename: {app}\extract.exe
Name: {group}\{cm:UninstallProgram,{#MyAppName}}; Filename: {uninstallexe}
Name: {commondesktop}\Akeeba eXtract Wizard; Filename: {app}\extract.exe; IconFilename: {app}\extract.exe; Tasks: desktopicon
Name: {userappdata}\Microsoft\Internet Explorer\Quick Launch\Akeeba eXtract Wizard; Filename: {app}\extract.exe; IconFilename: {app}\extract.exe; Tasks: quicklaunchicon

[Run]
Filename: {app}\extract.exe; Description: "{cm:LaunchProgram,""Akeeba eXtract Wizard""}"; Flags: nowait postinstall skipifsilent
