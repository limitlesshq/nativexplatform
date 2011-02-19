#define MyAppName "Akeeba SiteDiff"
#define MyAppVerName "Akeeba SiteDiff 3.1"
#define MyAppVersion "3.1"
#define MyAppPublisher "Akeeba Developers"
#define MyAppURL "http://www.akeebabackup.com/"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{0537C77C-E98A-4009-98FB-BAFA10768DFF}
AppName={#MyAppName}
AppVerName={#MyAppVerName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\Akeeba
DefaultGroupName=Akeeba
OutputDir=..\Output
OutputBaseFilename=SetupAkeebaSiteDiff
SetupIconFile=..\Assets\usable\ico\SiteDiff.ico
Compression=lzma/ultra
SolidCompression=true
InternalCompressLevel=ultra
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (c) 2008-2010 Nicholas K. Dionysopoulos / {#MyAppPublisher}
ShowLanguageDialog=no

[Languages]
Name: english; MessagesFile: compiler:Default.isl

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked
Name: quicklaunchicon; Description: {cm:CreateQuickLaunchIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Files]
Source: ..\Output\AkeebaSiteDiff.exe; DestDir: {app}; Flags: ignoreversion
Source: ..\Output\languages\sitediff-en.ini; DestDir: {app}\languages
Source: ..\Output\languages\sitediff-el.ini; DestDir: {app}\languages

[Icons]
Name: {group}\Akeeba SiteDiff; Filename: {app}\AkeebaSiteDiff.exe; WorkingDir: {userdocs}; IconFilename: {app}\AkeebaSiteDiff.exe; IconIndex: 0
Name: {group}\{cm:UninstallProgram,{#MyAppName}}; Filename: {uninstallexe}; Components: ; Tasks: 
Name: {commondesktop}\Akeeba SiteDiff; Filename: {app}\AkeebaSiteDiff.exe; IconFilename: {app}\AkeebaSiteDiff.exe; IconIndex: 0; Tasks: desktopicon
Name: {userappdata}\Microsoft\Internet Explorer\Quick Launch\Akeeba SiteDiff; Filename: {app}\AkeebaSiteDiff.exe; IconFilename: {app}\AkeebaSiteDiff.exe; IconIndex: 0; Tasks: quicklaunchicon

[Run]
Filename: {app}\AkeebaSiteDiff.exe; Description: "{cm:LaunchProgram,""Akeeba SiteDiff""}"; Flags: nowait postinstall skipifsilent

[Dirs]
Name: {app}\languages
