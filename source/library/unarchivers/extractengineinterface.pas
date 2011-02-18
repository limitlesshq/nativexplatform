unit extractengineinterface;
{<
 Native Archive Extraction libraries
 Copyright (c)2008-2009 Nicholas K. Dionysopoulos

 Base extraction engine interface
}

{$H+}

{$IFNDEF fpc}
	{$DEFINE WINDOWS}
{$ENDIF}

interface

uses
  Classes, SysUtils
  {$IFNDEF WINDOWS},baseunix,Unix{$ENDIF};

type

{ Ennumeration of errors which can occur during the extraction of the archives }
TJPAExtractionErrors = (
	JPERR_NONE = 0,						// (No error has occured)
	JPERR_CANTREADARCHIVE,				// The selected archive could not be opened
	JPERR_ENDOFARCHIVE,					// Unexpected end of archive
    JPERR_READERROR,					// Could not read data from the archive.
    JPERR_INVALIDARCHIVE,				// Invalid archive format.
    JPERR_INVALIDHEADER,				// Invalid file entry header
    JPERR_FOLDERCREATION,				// Could not create folder
    JPERR_FILECREATION,					// Could not create file
    JPERR_DEFLATEEXCEPTION,				// A decompression error occured while extracting the archive
    JPERR_MISCEXCEPTION,				// An unknown error occured while extracting the archive
    JPERR_IGNORED,						// An error occured, but it was ignored
    JPERR_NOEOCD,						// No End-Of-Central-Directory found (ZIP files)
    JPERR_UNKNOWNCOMPRESSION			// Unknown compression type
);

{ Archive type enumeration }
TArchiveType = (jpatJPA = 0, jpatZIP, jpatJPS, jpatUnknown);

{ Extraction process status enumeration }
TExtractionStatus = (jpesIdle, jpesRunning, jpesFinished, jpesError);

{ Archive information }
TArchiveInformation = record
    ArchiveType             : TArchiveType;	//< Type of archive
    FileCount               : LongWord;		//< Number of contained files and directories
    ArchiveSize             : LongWord;		//< Size of the archive (including any archive headers), in bytes
    UncompressedSize        : LongWord;		//< Uncompressed size of all files, in bytes
    CompressedSize          : LongWord;		//< Compressed size of all files (excluding any archive headers), in bytes
end;

{ Extraction progress }
TExtractionProgress = record
    FilePosition            : LongWord;		//< Position in archive file, in bytes from archive start
    RunningCompressed       : LongWord;		//< Amount of compressed data read, in bytes
    RunningUncompressed     : LongWord;      //< Bytes written to disk
    Status                  : TExtractionStatus; //< Status of the extraction process
    ErrorType				: TJPAExtractionErrors; //< Type of the last error occured
    ErrorMessage            : String;		//< Last error message
end;

{ Information on last extracted file or folder }
TLastEntityInformation = record
    StoredName              : string;       //< Relative path stored in archive
    AbsoluteName            : string;       //< Calculated absolute name (where it was stored on the user's filesystem)
    CompressedSize          : LongWord;      //< Size of data section read from archive, in bytes
    UncompressedSize        : LongWord;      //< Size of extracted data written to disk, in bytes
end;

{ An entry indicating a single part of a multi-part archive }
TSpannedArchive = record
    Name          : string;
    StartOffset   : LongWord;
    EndOffset     : LongWord;
end;

{ A dynamic array holding info on multi-part archive's parts }
TSpannedArchiveArray = array of TSpannedArchive;

TDataWriter = class
	procedure       MakeDirRecursive(Dir: String); virtual; abstract;
    procedure		StartFile(FileName: String; RelativePath: String = ''); virtual; abstract;
    procedure		StopFile(); virtual; abstract;
	procedure		WriteData(var Buffer; Count: LongInt); virtual; abstract;
    function		mkSymLink( oldname, newname: string ): LongInt; virtual; abstract;
end;

{ The default data writer, simply writes files to disk }

{ TDefaultDataWriter }

TDefaultDataWriter = class(TDataWriter)
protected
	outFile:		File of byte; //< This is where we write to
    function		Last(What: String; Where: String): Integer; //< Used by MakeDirRecursive
public
	procedure       MakeDirRecursive(Dir: String); override; //< Recursively make a directory
    procedure		StartFile(FileName: String; RelativePath: String = '');  override; //< Signal that we've began writing to a new file
    procedure		StopFile();  override; //< Close a file we were writing to
	procedure		WriteData(var Buffer; Count: LongInt);  override; //< Write some data to the file
    function		mkSymLink( oldname, newname: string ): LongInt;  override; //< Make a symbolic link, an alias to fpSymLink
end;

{ An array of two bytes, used for returning the ZLib compressed data header }
TTwoBytesArray = array[0..1] of Byte;

{ Ancestor to all extraction classes, implementing utility functions }

{ TExtractionEngine }

TExtractionEngine = class
    protected
        fldArchiveInformation: TArchiveInformation; //< Internal storage for archive information
        fldProgress:    TExtractionProgress;
		strFilepath:	string; //< Full path to archive
		strTargetPath:	string; //< Full path to extraction source
		f:				File of Byte; //< Used for archive file I/O
		Offset:			LongWord; //< Position from archive's start, in bytes

        FListMode:		Boolean; //< When true it doesn't extract the archive contents
        FSkipErrors:	Boolean; //< When true it doesn't stop on extraction errors (e.g. unwritable files)

        FDataWriter:	TDataWriter; //< The Data Writer to use

        PartsArray:     TSpannedArchiveArray; // An array holding the archive's parts
        CurrentPart:    Integer; // The current part number of a multi-part archive
        procedure       scanMultipartArchives(); // Scans for the existence of multi-part archives
        function        getOffset: LongWord; // Returns the offset relative to the start of the first part
        function        getNextPart(): Boolean; // Open the next archive and update CurrentPart and f
        function        getEOF( Local: Boolean = true ): Boolean;
		function        skipToOffset(NewOffset: LongWord): Boolean; // Skips to the requested offset (relative to the start of the first part)
		procedure		OpenPart( partNo: Integer ); // Opens the specified part file

        function 		getZLibHeader() : TTwoBytesArray; //< Returns the standard ZLib header for Deflated data streams
        function		getPathName(strRelativePath: string): string; // Gets an absolute path based on a stored relative path
    public
        constructor     Create( ArchivePath, TargetPath: string ); virtual;
        destructor		Destroy; override;

        procedure   	ReadHeader(); virtual; abstract;
		function		ExtractNext: TLastEntityInformation; virtual; abstract;

        procedure		setDataWriter( var newWriter: TDataWriter );

		property        ArchiveInformation: TArchiveInformation read fldArchiveInformation;
        property        Progress: TExtractionProgress read fldProgress;
        property		ListMode: Boolean read FListMode write FListMode;
        property		SkipErrors: Boolean read FSkipErrors write FSkipErrors;
end;

resourcestring
    JPE_ERROR_CANTREADARCHIVE	= 'The selected archive could not be opened';
    JPE_ERROR_ENDOFARCHIVE      = 'Unexpected end of archive';
    JPE_ERROR_READERROR         = 'Could not read data from the archive.';
    JPE_ERROR_INVALIDARCHIVE    = 'Invalid archive format.';
    JPE_ERROR_INVALIDHEADER     = 'Invalid file entry header.';
    JPE_ERROR_FOLDERCREATION	= 'Could not create folder for file %s';
    JPE_ERROR_FILECREATION      = 'Could not create file %s';
    JPE_ERROR_DEFLATEEXCEPTION  = 'A decompression error occured while extracting the archive.';
    JPE_ERROR_MISCEXCEPTION     = 'An unknown error occured while extracting the archive.';
    JPE_ERROR_IGNORED			= 'An error occured, but it was ignored.';
    JPE_ERROR_NOEOCD            = 'The archive seems to be truncated or of an invalid format.';
	JPE_ERROR_UNKNOWNCOMPRESSION= 'We encountered an unknown compression type. Probably this ZIP archive was not created by Akeeba Backup/JoomlaPack?';

implementation

uses StrUtils;

constructor TExtractionEngine.Create( ArchivePath, TargetPath: string );
begin
	// Initialize options
	FSkipErrors:=False;
    FListMode:=False;

    // Create a default data writer
    FDataWriter := TDefaultDataWriter.Create;

	Self.strFilepath := ArchivePath;
	if(RightStr(TargetPath,1) = PathDelim) then
		TargetPath := LeftStr(TargetPath, Length(TargetPath)-1 );
	Self.strTargetPath := TargetPath;

    with Self.Progress do
    begin
        FilePosition:=0;
        Status:=jpesIdle;
        RunningCompressed:=0;
        RunningUncompressed:=0;
        ErrorMessage:='';
    end;

    // Scan for multi-part archives
    scanMultipartArchives;
    // Initialize
    if not skipToOffset(0) then
    begin
        Self.fldProgress.Status :=jpesError;
        Self.fldProgress.ErrorMessage:=JPE_ERROR_CANTREADARCHIVE;
        Self.fldProgress.ErrorType:=JPERR_CANTREADARCHIVE ;
    end;
end;

destructor TExtractionEngine.Destroy;
begin
	CloseFile(Self.f);
    FreeAndNil(FDataWriter);
	inherited;
end;

procedure TExtractionEngine.OpenPart(partNo: Integer);
begin
	{$I-}
	try
		Close(f);
	except
	on E: Exception do;
	end;
	{$I+}
	CurrentPart := partNo;
	Assign(f, PartsArray[partNo].Name);
	FileMode := fmOpenRead;
	Reset(f);
end;

function TExtractionEngine.getZLibHeader() : TTwoBytesArray;
var
  header: cardinal;
  level_flags: cardinal;
  temp: TTwoBytesArray;
const
  DEFLATE_ID : Byte = 8;
  WINDOW_LEN : Byte = 15;
  COMP_LEVEL : Byte = 1;
begin
  header := (DEFLATE_ID + ((WINDOW_LEN-8) shl 4)) shl 8;
  level_flags := (COMP_LEVEL-1) shr 1;
  if (level_flags > 3) then
    level_flags := 3;
  header := header or (level_flags shl 6);
  Inc(header, 31 - (header mod 31));
  temp[0] := Byte(header shr 8);
  temp[1] := Byte(header and $ff);
  getZLibHeader := temp;
end;

function TExtractionEngine.getEOF(Local: Boolean): Boolean;
begin
	if (not local) and ( CurrentPart = Length(PartsArray) - 1 ) then
		if FileSize(f) = FilePos(f) then
		begin
			Result := true;
			Exit;
		end;

	if(Local) then
		Result := Eof(f) or (IOResult <> 0)
	else
		Result := ( Eof(f) or (IOResult <> 0) )and ( CurrentPart >= Length(PartsArray) );
end;

function TExtractionEngine.getNextPart(): Boolean;
begin
    if(CurrentPart > (Length(PartsArray) - 1)) then
    begin
        with Self.Progress do
        begin
            Status:=jpesError;
            ErrorMessage:=JPE_ERROR_ENDOFARCHIVE;
            ErrorType:=JPERR_ENDOFARCHIVE;
        end;
        Result := False;
    end
    else
    begin
        Result := skipToOffset( PartsArray[CurrentPart].EndOffset + 1 );
    end;

end;

function TExtractionEngine.getOffset: LongWord;
var
    LocalOffset     : LongWord;
begin
    LocalOffset := FilePos(f);
    Result := LocalOffset + PartsArray[Self.CurrentPart].StartOffset;
end;

function TExtractionEngine.getPathName(strRelativePath: string): string; // Gets an absolute path based on a stored relative path
const
	invalidChars : Array[0..6] of Char = ':*?"<>|';
var
	i : Integer;
begin
{$IFDEF WINDOWS}
	// Do we have an invalid name?
    strRelativePath := Trim(strRelativePath);
    for i := 0 to 6 do
        if Pos(invalidChars[i], strRelativePath) > 0 then
        begin
        	raise Exception.Create('Invalid Windows file name '+strRelativePath);
            Result := '';
            Exit;
        end;

	// Translate directory separators
   strRelativePath := StringReplace(strRelativePath, '/', '\', [rfReplaceAll] );
{$ENDIF}
	// For some reason, I get an extra character at the end :s
    strRelativePath:=LeftStr(strRelativePath, Length(strRelativePath) - 1);
	if RightStr(strRelativePath, 1) = PathDelim then
   	begin
		strRelativePath := LeftStr(strRelativePath, Length(strRelativePath) - 1 );
  		FDataWriter.MakeDirRecursive(self.strTargetPath+PathDelim+strRelativePath); // A directory was found
	end
	else
	begin
		FDataWriter.MakeDirRecursive(self.strTargetPath+PathDelim+ExtractFilePath(strRelativePath));
	end;
    Result := self.strTargetPath+PathDelim+strRelativePath;
end;

procedure TExtractionEngine.setDataWriter(var newWriter: TDataWriter);
begin
	FDataWriter := newWriter;
end;

procedure TExtractionEngine.scanMultipartArchives();
var
    strBaseFilename     : string;
    strBaseExtension    : string;
    strExtension        : string;
    strFilename         : string;
    TotalSize           : LongInt;
    ThisFileSize        : LongWord;
    aFile               : File of Byte;
    i                   : Integer;
    found               : Boolean;
begin
    // Get the base extension
    strBaseExtension := ExtractFileExt(strFilepath);
    strBaseFilename  := Copy(strFilepath, 0, Length(strFilepath) - Length(strBaseExtension));

    // Try to find all parts
    i := 0; found := true; TotalSize := -1;
    SetLength(PartsArray, 0);
    while Found do
    begin
        Inc(i);
        strExtension := Copy(strBaseExtension, 0, 2) + Format('%.2u', [i]);
        strFilename := strBaseFilename + strExtension;
        if(not FileExists(strFilename)) then
        begin
            Found := false;
            strFilename := strFilepath;
        end;
        AssignFile(aFile, strFilename);
        FileMode := fmOpenRead;
        Reset(aFile);
        ThisFileSize := FileSize(aFile);
        CloseFile(aFile);

        SetLength(PartsArray, i);
        PartsArray[i-1].Name := strFilename;
        PartsArray[i-1].StartOffset := TotalSize + 1;
        PartsArray[i-1].EndOffset := PartsArray[i-1].StartOffset + ThisFileSize - 1;
        TotalSize := PartsArray[i-1].EndOffset;
    end;

    fldArchiveInformation.ArchiveSize := TotalSize;
end;

function TExtractionEngine.skipToOffset(NewOffset: LongWord): Boolean;
var
    Found           : Boolean;
    Count           : Integer;
    RelativeOffset  : LongWord;
begin
    Found := False;
    Count := -1;

    // Find the correct part
    while (not Found) and (Count < (Length(PartsArray)-1) ) do
    begin
        Inc(Count);
		Found := (PartsArray[Count].StartOffset <= NewOffset) and
                 (PartsArray[Count].EndOffset >= NewOffset);
    end;

    if(CurrentPart <> Count) then
    begin
        OpenPart(Count);
    end;

    // Make sure we found the offset
    if not Found then
    begin
        Result := False;
        Exit;
	end;

	// Calculate the relative offset
	RelativeOffset := NewOffset - PartsArray[CurrentPart].StartOffset;
	if(RelativeOffset > 0) then
	begin
		{$I+}
		Seek(f, RelativeOffset);
		{$I-}
		if IOResult <> 0 then
			if CurrentPart = Count then
				CurrentPart := -1;
	end
	else
	begin
		{$I-}
		AssignFile(f, PartsArray[CurrentPart].Name);
		FileMode := fmOpenRead;
		Reset(f);
		{$I+}
		Result := (IOResult = 0);
		Exit;
	end;


	// Do we have the correct part set?
	if not(CurrentPart = Count) then
	begin
		// No, set it and mark that we should open the file pointer
		try
			if CurrentPart <> -1 then
				CloseFile(f);
		except
			on E: Exception do;
		end;
		CurrentPart := Count;

		{$I-}
		AssignFile(f, PartsArray[CurrentPart].Name);
		FileMode := fmOpenRead;
		Reset(f);
		{$I+}
		if IOResult <> 0 then
		begin
			Result := false;
			Exit;
		end;

		// Calculate the relative offset
		RelativeOffset := NewOffset - PartsArray[CurrentPart].StartOffset;
		{$I-}
		Seek(f, RelativeOffset);
		{$I+}
	end;

	Result := (IOResult = 0);
end;

{ TDefaultDataWriter }

procedure TDefaultDataWriter.MakeDirRecursive(Dir: String);
var
  PrevDir : String;
  Ind     : Integer;
begin
{$IFDEF WINDOWS}
{
  // Fix paths not begining with a drive letter or UNC path
  if Copy(Dir,2,1) <> ':' then
     if (Copy(Dir,3,1) <> '\') and not (Copy(Dir,1,2) = '\\') then
        if Copy(Dir,1,1) = '\' then
           Dir := 'C:'+Dir
        else
           Dir := 'C:\'+Dir
     else
        Dir := 'C:'+Dir;
}
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

procedure TDefaultDataWriter.StartFile(FileName: String; RelativePath: String = '');
begin
	try
	AssignFile(Self.outFile, FileName);
    FileMode := fmOpenWrite;
    Rewrite(Self.outFile);
    except
        on E: Exception do;
    end;
end;

procedure TDefaultDataWriter.StopFile();
begin
    try
		CloseFile(Self.outFile);
    except
    	on E: Exception do;
    end;
end;

procedure TDefaultDataWriter.WriteData(var Buffer; Count: LongInt);
begin
	BlockWrite(Self.outFile, Buffer, Count);
end;

function TDefaultDataWriter.mkSymLink(oldname, newname: string): LongInt;
begin
	Result := 0;
	{$IFDEF fpc}
	{$IFNDEF WINDOWS}
	Result := fpSymLink( PChar(newname), PChar(oldname) );
    {$ENDIF}
    {$ENDIF}
end;

function TDefaultDataWriter.Last(What: String; Where: String): Integer;
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