#define MyAppName "Akeeba Remote Control"
#define MyAppVerName "Akeeba Remote Control 2.5"
#define MyAppVersion "2.5"
#define MyAppPublisher "Akeeba Developers"
#define MyAppURL "http://www.akeebabackup.com/"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{DDF3FBA6-6566-47D9-A721-C99E3A794400}
AppName={#MyAppName}
AppVerName={#MyAppVerName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\Akeeba
DefaultGroupName=Akeeba
OutputDir=..\Output
OutputBaseFilename=SetupAkeebaRemoteControl
SetupIconFile=..\Assets\usable\ico\remote.ico
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
Source: ..\Output\AkeebaRemoteControl.exe; DestDir: {app}; Flags: ignoreversion
Source: ..\Output\libeay32.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Output\sqlite3.dll; DestDir: {app}; Flags: ignoreversion
Source: ..\Output\ssleay32.dll; DestDir: {app}; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: ..\Output\languages\dutch.ini; DestDir: {app}\languages
Source: ..\Output\languages\english.ini; DestDir: {app}\languages
Source: ..\Output\languages\french.ini; DestDir: {app}\languages
Source: ..\Output\languages\german.ini; DestDir: {app}\languages
Source: ..\Output\languages\greek.ini; DestDir: {app}\languages
Source: ..\Output\languages\russian.ini; DestDir: {app}\languages

[Icons]
Name: {group}\Remote; Filename: {app}\AkeebaRemoteControl.exe; WorkingDir: {userdocs}; IconFilename: {app}\AkeebaRemoteControl.exe; IconIndex: 0
Name: {group}\{cm:UninstallProgram,{#MyAppName}}; Filename: {uninstallexe}
Name: {commondesktop}\Akeeba Remote Control; Filename: {app}\AkeebaRemoteControl.exe; IconFilename: {app}\AkeebaRemoteControl.exe; IconIndex: 0; Tasks: desktopicon
Name: {userappdata}\Microsoft\Internet Explorer\Quick Launch\Akeeba Remote Control; Filename: {app}\AkeebaRemoteControl.exe; IconFilename: {app}\JoomlaPackRemote.exe; IconIndex: 0; Tasks: quicklaunchicon

[Run]
Filename: {app}\AkeebaRemoteControl.exe; Description: "{cm:LaunchProgram,""Akeeba Remote Control""}"; Flags: nowait postinstall skipifsilent

[Dirs]
Name: {app}\languages
