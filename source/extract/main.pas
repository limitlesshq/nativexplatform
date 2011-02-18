unit main;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, FileUtil, Forms, Controls, Graphics, Dialogs, StdCtrls,
  EditBtn, ComCtrls,
  ExtractEngineInterface;

type

  { TFormMain }

  TFormMain = class(TForm)
   Button1: TButton;
   chkIgnore: TCheckBox;
   chkDryRun: TCheckBox;
   edFolderName: TDirectoryEdit;
   edArchiveName: TFileNameEdit;
   edPassword: TEdit;
   groupProgress: TGroupBox;
   groupOptions: TGroupBox;
   Label1: TLabel;
   Label2: TLabel;
   Label3: TLabel;
   lblFilename: TLabel;
   pbProgress: TProgressBar;
   procedure Button1Click(Sender: TObject);
   procedure edArchiveNameAcceptFileName(Sender: TObject; var Value: String);
  private
    { private declarations }
    function ExistsAndIsReadable: Boolean;
    function guessArchiveType: TArchiveType;
    procedure MakeDirRecursive(Dir: String);
    function Last(What, Where: String): Integer;
  public
    { public declarations }
  end; 

var
  FormMain: TFormMain;

implementation

uses
    EngineUnJPA, EngineUnZIP, EngineUnJPS, AkAESCTR;

{$R *.lfm}

{ TFormMain }

procedure TFormMain.edArchiveNameAcceptFileName(Sender: TObject;
 var Value: String);
begin
    if(trim(Value) <> '') then
    begin
        edFolderName.Text:= ExtractFileNameWithoutExt(Value);
    end;
end;

procedure TFormMain.Button1Click(Sender: TObject);
var
    ArchiveType:        TArchiveType;
    Unarchiver:         TExtractionEngine;
    LastInfo:           TLastEntityInformation;
begin
    // Check if archive exists
    if not Self.ExistsAndIsReadable() then
    begin
        MessageDlg('The specified archive does not exist or is not readable', mtError, [mbOK], 0);
        Exit;
    end;

    // Guess archive type
    ArchiveType:=Self.guessArchiveType();

    // Check for valid archive type
    if(ArchiveType = jpatUnknown) then
    begin
        MessageDlg('Unknown archive type', mtError, [mbOK], 0);
        Exit;
    end;

    // Check that we have an output directory
    if edFolderName.Text = '' then
    begin
        MessageDlg('No output directory specified', mtError, [mbOK], 0);
        Exit;
    end;

    // Does the output directory exist?
    if not DirectoryExists(edFolderName.Text) then
    begin
        // Try to create this directory recursively
        Self.MakeDirRecursive(edFolderName.Text);
    end;
    // Recheck if directory exists. If it doesn't exist, we're out of luck :(
    if not DirectoryExists(edFolderName.Text) then
    begin
        MessageDlg('The output directory does not exist and can not be created', mtError, [mbOK], 0);
        Exit;
    end;

    // Intanciate archiver
    case ArchiveType of
		jpatJPA:
			begin
			Unarchiver := TUnJPA.Create(Self.edArchiveName.Text, Self.edFolderName.Text);
			end;

        jpatZIP:
            begin
            Unarchiver := TUnZIP.Create(Self.edArchiveName.Text, Self.edFolderName.Text);
            end;

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
        Exit;
    end;

    // Update progress bar
	pbProgress.Max:=100;
	pbProgress.Position:=0;

    // Loop through the archive...
    repeat
	    LastInfo := Unarchiver.ExtractNext;
        if Unarchiver.Progress.Status = jpesError then
		begin
			Application.ProcessMessages;
			MessageDlg(Unarchiver.Progress.ErrorMessage, mtWarning, [mbOK], 0);
			pbProgress.Position:=0;
        end
        else
        begin
            lblFilename.Caption := LeftStr(LastInfo.StoredName,Length(LastInfo.StoredName)-1);
			pbProgress.Position:= trunc(100* Unarchiver.Progress.RunningUncompressed / Unarchiver.ArchiveInformation.UncompressedSize);
        end;
        Application.ProcessMessages;
    until (Unarchiver.Progress.Status <> jpesRunning);

    (Unarchiver as TExtractionEngine).Free;

    if(Unarchiver.Progress.Status = jpesFinished) then
    	MessageDlg('Your archive was successfully extracted', mtInformation, [mbOK], 0);

    pbProgress.Position:=0;
    lblFilename.Caption:='';
end;

function TFormMain.ExistsAndIsReadable: Boolean;
begin
     if(edArchiveName.Text = '') then
     	 Result := false
     else if not FileExistsUTF8(edArchiveName.Text) then
     	 Result := false
     else if not FileIsReadable(edArchiveName.Text) then
     	 Result := false
     else
     	 Result := true;
end;

function TFormMain.guessArchiveType: TArchiveType;
begin
	if( UpperCase(ExtractFileExt(edArchiveName.Text)) = '.JPA' ) then
		Result := jpatJPA
	else if( UpperCase(ExtractFileExt(edArchiveName.Text)) = '.ZIP' ) then
		Result := jpatZIP
	else if( UpperCase(ExtractFileExt(edArchiveName.Text)) = '.JPS' ) then
		Result := jpatJPS
	else
    	Result := jpatUnknown;
end;

procedure TFormMain.MakeDirRecursive(Dir: String);
var
  PrevDir : String;
  Ind     : Integer;
begin
{$IFDEF WIN32}
  // Fix paths not begining with a drive letter or UNC path
  if Copy(Dir,2,1) <> ':' then
     if (Copy(Dir,3,1) <> '\') and not (Copy(Dir,1,2) = '\\') then
        if Copy(Dir,1,1) = '\' then
           Dir := 'C:'+Dir
        else
           Dir := 'C:\'+Dir
     else
        Dir := 'C:'+Dir;
{$ENDIF}

  if not DirectoryExists(Dir) then begin
     // if directory don't exist, get name of the previous directory

     Ind     := Self.Last(PathDelim, Dir);         //  Position of the last '\'
     PrevDir := Copy(Dir, 1, Ind-1);    //  Previous directory

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
	Ind : Integer;
begin
	Result := 0;

	for Ind := (Length(Where)-Length(What)+1) downto 1 do
		if Copy(Where, Ind, Length(What)) = What then begin
			Result := Ind;
			Break;
		end;
end;

end.
