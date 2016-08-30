unit main;
{<
 Akeeba eXtract Wizard
 Copyright (c)2008-2016 Nicholas K. Dionysopoulos
 Licensed under the GNU General Public Licence version 3, or any later version published by the Free Software Foundation
}

{$mode objfpc}{$H+}

interface

uses
    Classes, SysUtils, FileUtil, Forms, Controls, Graphics, Dialogs, StdCtrls,
    EditBtn, ComCtrls, Buttons, ExtractEngineInterface
;

{$IFDEF WINDOWS}
type
    TTaskBarProgressState = (tbpsNone, tbpsIndeterminate, tbpsNormal, tbpsError, tbpsPaused);
{$ENDIF}

type

    { TFormMain }

    TFormMain = class(TForm)
        BitBtn1:     TBitBtn;
        Button1:     TButton;
        chkIgnore:   TCheckBox;
        chkDryRun:   TCheckBox;
        edFolderName: TDirectoryEdit;
        edArchiveName: TFileNameEdit;
        edPassword:  TEdit;
        groupProgress: TGroupBox;
        groupOptions: TGroupBox;
        Label1:      TLabel;
        Label2:      TLabel;
        Label3:      TLabel;
        Label4:      TLabel;
        lblFilename: TLabel;
        pbProgress:  TProgressBar;
        procedure BitBtn1Click(Sender: TObject);
        procedure Button1Click(Sender: TObject);
        procedure edArchiveNameAcceptFileName(Sender: TObject; var Value: String);
    private
        { private declarations }
        function ExistsAndIsReadable: Boolean;
        function guessArchiveType: TArchiveType;
        procedure MakeDirRecursive(Dir: String);
        function Last(What, Where: String): Integer;
        {$IFDEF WINDOWS}
        procedure SetProgressState(const AState: TTaskBarProgressState);
        procedure SetProgressValue(const ACurrent, AMax: UInt64);
        {$ENDIF}
    public
        { public declarations }
    end;

var
    FormMain: TFormMain;

implementation

uses
    EngineUnJPA, EngineUnZIP, EngineUnJPS, AkAESCTR, LCLIntf
    {$IFDEF WINDOWS}
    ,Windows, win32int, InterfaceBase, ComObj
    {$ENDIF}
    ;

{$R *.lfm}

{$IFDEF WINDOWS}
const
  TASKBAR_CID: TGUID = '{56FDF344-FD6D-11d0-958A-006097C9A090}';

const
  TBPF_NOPROGRESS = 0;
  TBPF_INDETERMINATE = 1;
  TBPF_NORMAL = 2;
  TBPF_ERROR = 4;
  TBPF_PAUSED = 8;

type
  { Definition for Windows 7 ITaskBarList3 }
  ITaskBarList3 = interface(IUnknown)
  ['{EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF}']
    procedure HrInit(); stdcall;
    procedure AddTab(hwnd: THandle); stdcall;
    procedure DeleteTab(hwnd: THandle); stdcall;
    procedure ActivateTab(hwnd: THandle); stdcall;
    procedure SetActiveAlt(hwnd: THandle); stdcall;

    procedure MarkFullscreenWindow(hwnd: THandle; fFullscreen: Boolean); stdcall;

    procedure SetProgressValue(hwnd: THandle; ullCompleted: UInt64; ullTotal: UInt64); stdcall;
    procedure SetProgressState(hwnd: THandle; tbpFlags: Cardinal); stdcall;

    procedure RegisterTab(hwnd: THandle; hwndMDI: THandle); stdcall;
    procedure UnregisterTab(hwndTab: THandle); stdcall;
    procedure SetTabOrder(hwndTab: THandle; hwndInsertBefore: THandle); stdcall;
    procedure SetTabActive(hwndTab: THandle; hwndMDI: THandle; tbatFlags: Cardinal); stdcall;
    procedure ThumbBarAddButtons(hwnd: THandle; cButtons: Cardinal; pButtons: Pointer); stdcall;
    procedure ThumbBarUpdateButtons(hwnd: THandle; cButtons: Cardinal; pButtons: Pointer); stdcall;
    procedure ThumbBarSetImageList(hwnd: THandle; himl: THandle); stdcall;
    procedure SetOverlayIcon(hwnd: THandle; hIcon: THandle; pszDescription: PChar); stdcall;
    procedure SetThumbnailTooltip(hwnd: THandle; pszDescription: PChar); stdcall;
    procedure SetThumbnailClip(hwnd: THandle; var prcClip: TRect); stdcall;
  end;

var
  { Global variable storing the COM interface }
  GlobalTaskBarInterface: ITaskBarList3;

{$ENDIF}

const
    strPaypalURL: String =
        'https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=D9CFZ4H35NFWW';

{ TFormMain }

procedure TFormMain.edArchiveNameAcceptFileName(Sender: TObject; var Value: String);
begin
    if (trim(Value) <> '') then
        edFolderName.Text := ExtractFileNameWithoutExt(Value);
end;

procedure TFormMain.Button1Click(Sender: TObject);
var
    ArchiveType: TArchiveType;
    Unarchiver: TExtractionEngine;
    LastInfo: TLastEntityInformation;
    PercentageDone: Integer;
begin
    Button1.Enabled   := False;
    chkDryRun.Enabled := False;
    chkIgnore.Enabled := False;

    // Check if archive exists
    if not Self.ExistsAndIsReadable() then
    begin
        MessageDlg('The specified archive does not exist or is not readable',
            mtError, [mbOK], 0);
        Button1.Enabled   := True;
        chkDryRun.Enabled := True;
        chkIgnore.Enabled := True;
        ;
        Exit;
    end;

    // Guess archive type
    ArchiveType := Self.guessArchiveType();

    // Check for valid archive type
    if (ArchiveType = jpatUnknown) then
    begin
        MessageDlg('Unknown archive type', mtError, [mbOK], 0);
        Button1.Enabled   := True;
        chkDryRun.Enabled := True;
        chkIgnore.Enabled := True;
        ;
        Exit;
    end;

    // Check that we have an output directory
    if edFolderName.Text = '' then
    begin
        MessageDlg('No output directory specified', mtError, [mbOK], 0);
        Button1.Enabled   := True;
        chkDryRun.Enabled := True;
        chkIgnore.Enabled := True;
        ;
        Exit;
    end;

    // Does the output directory exist?
    if not DirectoryExists(edFolderName.Text) then
        Self.MakeDirRecursive(edFolderName.Text)// Try to create this directory recursively
    ;
    // Recheck if directory exists. If it doesn't exist, we're out of luck :(
    if not DirectoryExists(edFolderName.Text) then
    begin
        MessageDlg('The output directory does not exist and can not be created',
            mtError, [mbOK], 0);
        Button1.Enabled   := True;
        chkDryRun.Enabled := True;
        chkIgnore.Enabled := True;
        ;
        Exit;
    end;

    // Intanciate archiver
    case ArchiveType of
        jpatJPA:
            Unarchiver := TUnJPA.Create(Self.edArchiveName.Text, Self.edFolderName.Text);

        jpatZIP:
            Unarchiver := TUnZIP.Create(Self.edArchiveName.Text, Self.edFolderName.Text);

        jpatJPS:
        begin
            Unarchiver := TUnJPS.Create(Self.edArchiveName.Text, Self.edFolderName.Text);
            (Unarchiver as TUnJPS).setPassword(edPassword.Text);
        end;
    end;

    // Check for any errors during object creation
    if Unarchiver.Progress.Status <> jpesIdle then
    begin
        MessageDlg(Unarchiver.Progress.ErrorMessage, mtWarning, [mbOK], 0);
        Button1.Enabled   := True;
        chkDryRun.Enabled := True;
        chkIgnore.Enabled := True;
        Exit;
    end;

    // Set error ingore
    Unarchiver.SkipErrors := chkIgnore.Checked;

    // Set list-only mode
    Unarchiver.ListMode := chkDryRun.Checked;

    // Try to read header
    Unarchiver.ReadHeader();
    if Unarchiver.Progress.Status <> jpesIdle then
    begin
        MessageDlg(Unarchiver.Progress.ErrorMessage, mtWarning, [mbOK], 0);
        Button1.Enabled   := True;
        chkDryRun.Enabled := True;
        chkIgnore.Enabled := True;
        Exit;
    end;

    // Update progress bar
    {$IFDEF WINDOWS}
    SetProgressState(tbpsNormal);
    SetProgressValue(0, 100);
    {$ENDIF}
    pbProgress.Max      := 100;
    pbProgress.Position := 0;

    // Loop through the archive...
    repeat
        LastInfo := Unarchiver.ExtractNext;
        if Unarchiver.Progress.Status = jpesError then
        begin
            Application.ProcessMessages;
            SetProgressState(tbpsError);
            MessageDlg(Unarchiver.Progress.ErrorMessage, mtWarning, [mbOK], 0);
            SetProgressState(tbpsNone);
            pbProgress.Position := 0;
            {$IFDEF WINDOWS}
            SetProgressValue(0, 100);
            {$ENDIF}
        end
        else
        begin
            lblFilename.Caption := LastInfo.StoredName;
            PercentageDone      := trunc(100 * Unarchiver.Progress.RunningUncompressed /
                Unarchiver.ArchiveInformation.UncompressedSize);
            if (PercentageDone < 0) then
                PercentageDone := 0
            else if (PercentageDone > 100) then
                PercentageDone  := 100;
            pbProgress.Position := PercentageDone;
            {$IFDEF WINDOWS}
            SetProgressValue(PercentageDone, 100);
            {$ENDIF}
        end;
        Application.ProcessMessages;
    until (Unarchiver.Progress.Status <> jpesRunning);

    if (Unarchiver.Progress.Status = jpesFinished) then
    begin
        pbProgress.Position := 100;
        {$IFDEF WINDOWS}
        SetProgressValue(100, 100);
        {$ENDIF}
        MessageDlg('Your archive was successfully extracted', mtInformation, [mbOK], 0);
    end;

    pbProgress.Position := 0;
    {$IFDEF WINDOWS}
    SetProgressValue(0, 100);
    SetProgressState(tbpsNone);
    {$ENDIF}

    lblFilename.Caption := '';

    Button1.Enabled   := True;
    chkDryRun.Enabled := True;
    chkIgnore.Enabled := True;

    (Unarchiver as TExtractionEngine).Free;
end;

procedure TFormMain.BitBtn1Click(Sender: TObject);
begin
    openURL(strPaypalURL);
end;

function TFormMain.ExistsAndIsReadable: Boolean;
begin
    if (edArchiveName.Text = '') then
        Result := False
    else if not FileExistsUTF8(edArchiveName.Text) then
        Result := False
    else if not FileIsReadable(edArchiveName.Text) then
        Result := False
    else
        Result := True;
end;

function TFormMain.guessArchiveType: TArchiveType;
begin
    if (UpperCase(ExtractFileExt(edArchiveName.Text)) = '.JPA') then
        Result := jpatJPA
    else if (UpperCase(ExtractFileExt(edArchiveName.Text)) = '.ZIP') then
        Result := jpatZIP
    else if (UpperCase(ExtractFileExt(edArchiveName.Text)) = '.JPS') then
        Result := jpatJPS
    else
        Result := jpatUnknown;
end;

procedure TFormMain.MakeDirRecursive(Dir: String);
var
    PrevDir: String;
    Ind: Integer;
begin
{$IFDEF WIN32}
    // Fix paths not begining with a drive letter or UNC path
    if Copy(Dir, 2, 1) <> ':' then
        if (Copy(Dir, 3, 1) <> '\') and not (Copy(Dir, 1, 2) = '\\') then
            if Copy(Dir, 1, 1) = '\' then
                Dir := 'C:' + Dir
            else
                Dir := 'C:\' + Dir
        else
            Dir := 'C:' + Dir;
{$ENDIF}

    if not DirectoryExists(Dir) then
    begin
        // if directory don't exist, get name of the previous directory

        Ind     := Self.Last(PathDelim, Dir);         //  Position of the last '\'
        PrevDir := Copy(Dir, 1, Ind - 1);    //  Previous directory

        // if previous directoy don't exist,
        // it's passed to this procedure - this is recursively...
        if not DirectoryExists(PrevDir) then
            Self.MakeDirRecursive(PrevDir);

        // In thats point, the previous directory must be exist.
        // So, the actual directory (in "Dir" variable) will be created.
        CreateDir(Dir);
    end;
end;

function TFormMain.Last(What, Where: String): Integer;
var
    Ind: Integer;
begin
    Result := 0;

    for Ind := (Length(Where) - Length(What) + 1) downto 1 do
        if Copy(Where, Ind, Length(What)) = What then
        begin
            Result := Ind;
            Break;
        end;
end;

{$IFDEF WINDOWS}
procedure TFormMain.SetProgressState(const AState: TTaskBarProgressState);
const
  Flags: array[TTaskBarProgressState] of Cardinal = (0, 1, 2, 4, 8);
begin
  if GlobalTaskBarInterface <> nil then
    GlobalTaskBarInterface.SetProgressState(TWin32WidgetSet(WidgetSet).AppHandle, Flags[AState]);
end;

procedure TFormMain.SetProgressValue(const ACurrent, AMax: UInt64);
begin
  if GlobalTaskBarInterface <> nil then
    GlobalTaskBarInterface.SetProgressValue(TWin32WidgetSet(WidgetSet).AppHandle, ACurrent, AMax);
end;

procedure InitializeAPI();
var
  Unk: IInterface;

begin
  { Make sure that COM is initialized }
  CoInitializeEx(nil, 0);

  try
    { Obtain an IUnknown }
    Unk := CreateComObject(TASKBAR_CID);

    if Unk = nil then
      Exit;

    { Cast to the required interface }
    GlobalTaskBarInterface := Unk as ITaskBarList3;

    { Initialize }
    GlobalTaskBarInterface.HrInit();
  except
    GlobalTaskBarInterface := nil;
  end;
end;
{$ENDIF}

initialization

  {$IFDEF WINDOWS}
  { Initialize the Windows 7 taskbar API }
  InitializeAPI();
  {$ENDIF}

finalization

  {$IFDEF WINDOWS}
  { Force interface release }
  GlobalTaskBarInterface := nil;
  {$ENDIF}

end.
