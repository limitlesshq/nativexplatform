unit engineunjps;
{<
 Native Archive Extraction libraries
 Copyright (c)2008-2010 Nicholas K. Dionysopoulos

 JPS extraction engine
}

interface

uses
  extractengineinterface, AKAESCtr;

type
	TJPSHeader = record
		Signature:			array[0..2] of AnsiChar;
        MajorVersion:		Byte;
        MinorVersion:		Byte;
        SpannedArchive:		Byte;
        ExtraHeaderLength:	Word;
	end;

    TJPSEOAHeader = record
    	Signature:			array[0..2] of AnsiChar;
        NumParts:			Word;
        FileCount:			Longint;
        UncompSize:			Longint;
        CompSize:			Longint;
    end;

    TJPSEntityDescHeader = record
    	Signature:			array[0..2] of AnsiChar;
        EncSize:			Word;
        DecSize:			Word;
    end;

    TJPSEntityDescBlock = record
    	PathLength:			Word;
        Filename:			String;
        EntityType:			Byte;
        CompressionType:	Byte;
        UncompSize:			Longint;
        Permissions:		Longint;
        FileModTime:		Longint;
    end;

	TUnJPS = class(TExtractionEngine)
    private
        Decryptor:	TAkeebaAES;
    public
        constructor Create( ArchivePath, TargetPath: string ); override;
        destructor	Destroy; override;

        procedure	ReadHeader(); override;
        function	ExtractNext(): TLastEntityInformation; override;
        procedure	setPassword(password: string);
	end;

implementation

uses
	sysutils, Classes,
	{$IFDEF fpc}
		zstream // FreePascal - Use ZStream unit
		{$IFNDEF WINDOWS}, BaseUnix{$ENDIF} // FreePascal on Linux - use BaseUnix
	{$ELSE}
		zlib // Delphi on Windows - Use ZLib unit
	{$ENDIF}
    , math;

procedure TUnJPS.ReadHeader;
var
	JPSHeader:	TJPSHeader;
    EOAHeader:	TJPSEOAHeader;
	ExtraHeaders: Array[0..10000] of Byte;
begin
	// Try opening the first part
	try
		OpenPart(0);
    except
		on E: Exception do;
	end;

    // Read and check the signature
	BlockRead(Self.f, JPSHeader.Signature, SizeOf(JPSHeader.Signature));
	if JPSHeader.Signature <> 'JPS' then
	begin
		Self.fldProgress.Status:=jpesError;
		Self.fldProgress.ErrorMessage:=JPE_ERROR_INVALIDARCHIVE;
		Self.fldProgress.ErrorType:=JPERR_INVALIDARCHIVE;
		Exit;
	end;

    // Read the rest of the header
	try
		BlockRead(Self.f, JPSHeader.MajorVersion, SizeOf(JPSHeader.MajorVersion));
		BlockRead(Self.f, JPSHeader.MinorVersion, SizeOf(JPSHeader.MinorVersion));
		BlockRead(Self.f, JPSHeader.SpannedArchive, SizeOf(JPSHeader.SpannedArchive));
		BlockRead(Self.f, JPSHeader.ExtraHeaderLength, SizeOf(JPSHeader.ExtraHeaderLength));
		Self.Offset := getOffset;
	except
		On Exception do begin
		Self.fldProgress.Status:=jpesError;
		Self.fldProgress.ErrorMessage:=JPE_ERROR_CANTREADHEADER;
        Self.fldProgress.ErrorType:=JPERR_READERROR;
		Exit;
		end;
	end;

    // Skip over extra fields
	if JPSHeader.ExtraHeaderLength > 0 then
	begin
		try
			BlockRead(f, ExtraHeaders, JPSHeader.ExtraHeaderLength);
			Offset := getOffset;
		except
			on E: Exception do;
		end;
	end;

    // Open the last part and skip to the last 17 bytes
    try
    	skipToOffset( PartsArray[Length(PartsArray)-1].EndOffset - 16 );
    except
		on E: Exception do
        begin
		Self.fldProgress.Status:=jpesError;
		Self.fldProgress.ErrorMessage:=JPE_ERROR_NOEOARECORD;
        Self.fldProgress.ErrorType:=JPERR_READERROR;
		Exit;
        end;
	end;

    // Read the End Of Archive header signature
	BlockRead(Self.f, EOAHeader.Signature, SizeOf(EOAHeader.Signature));
	if EOAHeader.Signature <> 'JPE' then
	begin
		Self.fldProgress.Status:=jpesError;
		Self.fldProgress.ErrorMessage:=JPE_ERROR_INVALIDARCHIVE;
		Self.fldProgress.ErrorType:=JPERR_INVALIDARCHIVE;
		Exit;
	end;

    // Read the rest of the EOA header
	try
		BlockRead(Self.f, EOAHeader.NumParts, SizeOf(EOAHeader.NumParts));
		BlockRead(Self.f, EOAHeader.FileCount, SizeOf(EOAHeader.FileCount));
		BlockRead(Self.f, EOAHeader.UncompSize, SizeOf(EOAHeader.UncompSize));
		BlockRead(Self.f, EOAHeader.CompSize, SizeOf(EOAHeader.CompSize));
	except
		On Exception do begin
		Self.fldProgress.Status:=jpesError;
		Self.fldProgress.ErrorMessage:=JPE_ERROR_EOAMISSINGENTRIES;
        Self.fldProgress.ErrorType:=JPERR_READERROR;
		Exit;
		end;
	end;

    // Return to the original offset after the archive header
    skipToOffset(Offset);

	with Self.fldArchiveInformation do
	begin
		FileCount:=EOAHeader.FileCount;
		CompressedSize:=EOAHeader.CompSize;
		UncompressedSize:=EOAHeader.UncompSize;
		ArchiveType:=jpatJPS;
		ArchiveSize:=PartsArray[Length(PartsArray)-1].EndOffset;
	end;

	with Self.fldProgress do
    begin
		Status:=jpesIdle;
		try
			FilePosition:=Offset;
		except
        	on E: Exception do;
        end;
    end;
end;

procedure TUnJPS.setPassword(password: string);
begin
	Decryptor.AESSetPassword(password);
end;

constructor TUnJPS.Create(ArchivePath, TargetPath: string);
begin
  inherited;
  Decryptor := TAkeebaAES.Create;
end;

destructor TUnJPS.Destroy;
begin
  Decryptor.Free;
  inherited;
end;

function TUnJPS.Extractnext(): TLastEntityInformation;
const
    MAX_BUFFER_SIZE = 1048576;
var
    // Entity Description Block Header
	EDBH			: TJPSEntityDescHeader;
    // Encrypted/decrypted streams
    tBuf			: array of Byte;
    inputStream,
    EStream,
    UStream			: TMemoryStream;
	// Components of the JPA Entity Header
	PathLength		: Word;
    PathBuffer		: array[0..MAX_BUFFER_SIZE] of AnsiChar;
	StoredPathBin	: PAnsiChar;
	StoredPath		: String;
    PStoredPath		: PWideChar;
	EntityType		: Byte;
	CmpType			: Byte;
	SizeCmp			: LongWord;
	SizeUncmp		: LongWord;
	Permissions		: LongWord;

    writtenBytes	: LongWord;
    encSize,
    decSize			: LongWord;

	bytesLeft		: LongWord;
	stringBuffer	: string;
    thisReadSize,
	actualReadSize	: LongWord;					// How many bytes were actually read
	outPath			: string;					// Output full path and file name

	extractingStream: TDecompressionStream;		// Decompresses data on the fly
	tempHeader		: TTwoBytesArray;			// A copy of ZLib header

    strLinkTarget	: String;
    tempString		: AnsiString;

    i				: Integer;
begin
	Self.fldProgress.Status := jpesRunning;
    Self.fldProgress.ErrorType := JPERR_NONE;
    Self.fldProgress.ErrorMessage := '';

	if self.Offset >= (PartsArray[ Length(PartsArray)-1 ].EndOffset - 17) then
	begin
    	fldProgress.Status:=jpesFinished;
        fldProgress.ErrorType:=JPERR_NONE;
        fldProgress.ErrorMessage:='';
        Result.AbsoluteName:='';
        Result.StoredName:='';
        Result.UncompressedSize:=0;
        Result.CompressedSize:=0;
        exit;
 	end;

    try
		Self.skipToOffset(Self.Offset);

        // Read the Entity Description Block Header
    	BlockRead(Self.f, EDBH.Signature, sizeOf(EDBH.Signature));
        if( EDBH.Signature <> 'JPF' ) then
        begin
            Self.fldProgress.Status:=jpesError;
            Self.fldProgress.ErrorMessage:=JPE_ERROR_INVALIDARCHIVE;
			Self.fldProgress.ErrorType:=JPERR_INVALIDARCHIVE;
            Exit;
        end;
    	BlockRead(Self.f, EDBH.EncSize, SizeOf(EDBH.EncSize));
    	BlockRead(Self.f, EDBH.DecSize, SizeOf(EDBH.DecSize));

        // Read encrypted data in a memory stream
    	SetLength(tempString, EDBH.EncSize);
    	BlockRead(Self.f, tempString[1], EDBH.EncSize);
        EStream := TMemoryStream.Create;
        EStream.Write(tempString[1], EDBH.EncSize);

        // Decrypt the stream
        UStream := Decryptor.AESDecryptCBC(EStream);
        UStream.Seek(0, soFromBeginning);
        EStream.Free;

        // Read the Entity Description Block Data
        UStream.Read(PathLength, SizeOf(PathLength));

    	//SetLength(tempString, PathLength);
        //UStream.Read(tempString[1], PathLength);

        StoredPathBin := PathBuffer;
        for i := 0 to MAX_BUFFER_SIZE do
            PathBuffer[i] := #0;
        UStream.Read(StoredPathBin^, PathLength);
        StoredPath := Trim(StoredPathBin);

    	UStream.Read(EntityType, SizeOf(EntityType));
    	UStream.Read(CmpType, SizeOf(CmpType));
    	UStream.Read(SizeUncmp, SizeOf(SizeUncmp));
    	UStream.Read(Permissions, SizeOf(Permissions));

        UStream.Free;
    except
        On Exception do begin
        Self.fldProgress.Status:=jpesError;
        Self.fldProgress.ErrorMessage:=JPE_ERROR_CANTREADFHEADER;
        Self.fldProgress.ErrorType:=JPERR_READERROR;
        Exit;
        end;
    end;

    // Initialize
    Self.fldProgress.ErrorType := JPERR_NONE;
    SizeCmp := 0;

    // Recursively makes the directories and returns the target file path
    if not FListMode then
    begin
        try
        	StoredPath := Trim(StoredPath);
            outPath := Self.getPathName(StoredPath);
        except
            On E: Exception do;
        end;
    end;

    // Catch special case: 0 bytes file
    if( (SizeUncmp = 0) and (EntityType = 1) and (not FListMode) and (fldProgress.ErrorType = JPERR_NONE)) then
    begin
    	FDataWriter.StartFile(outPath, TrimRight(StoredPath));
        FDataWriter.StopFile;
    end
    else
	case EntityType of
		0: // Directory
      	begin
			// Do nothing more
		end;

        1: // Regular file
		begin
        	writtenBytes := 0;
            if( (not FListMode) and (fldProgress.ErrorType = JPERR_NONE) ) then
            begin
                try
                    FDataWriter.StartFile(outPath, TrimRight(StoredPath));
                except
                    On E: Exception do
                        if not FSkipErrors then	begin
                            Self.fldProgress.Status:=jpesError;
                            Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FILECREATION, [StoredPath]);
                            Self.fldProgress.ErrorType:=JPERR_FILECREATION;
                            Exit;
                        end
                        else
                            fldProgress.ErrorType := JPERR_IGNORED;
                end;
            end;

            while writtenBytes < SizeUncmp do
            begin
                // Read the Data Chunk Block header
                BlockRead(Self.f, encSize, SizeOf(encSize));
                tempString := IntToStr(getOffset) + ' -- ' + IntToStr(encSize);
                //OutputDebugString( PChar(tempString) );
                BlockRead(Self.f, decSize, SizeOf(decSize));
                Inc(SizeCmp, decSize);

                // Read encrypted data in a memory stream
                SetLength(tempString, encSize);
                bytesLeft := encSize;
                actualReadSize := 0;
                EStream := TMemoryStream.Create;

				while bytesLeft > 0 do
                begin
                    try
                        BlockRead(Self.f, tempString[1], bytesLeft, actualReadSize);
                        EStream.Write(tempString[1], actualReadSize);
                    except
                        On E: Exception do
                        begin
                            Self.fldProgress.Status:=jpesError;
                            Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
                            Self.fldProgress.ErrorType:=JPERR_READERROR;
                            Exit;
                        end;
                    end;
                    bytesLeft := bytesLeft - actualReadSize;

                    // Try to detect an end of part
                    if( getEOF(True) and ( bytesLeft > 0)  ) then
                    begin
                        // This part reached to an end, or is the file corrupt?
                        if not getEOF(false) then
                        begin
                            // Let's go to the next part
                            if not getNextPart then
                            begin
                                Self.fldProgress.Status := jpesError;
                                Self.fldProgress.ErrorMessage := JPE_ERROR_NONEXTPART;
                                Self.fldProgress.ErrorType:=JPERR_READERROR;
                                Exit;
                            end;
                        end
                        else
                        begin
                            // Nope, it's a premature end of archive
                            Self.fldProgress.Status := jpesError;
                            Self.fldProgress.ErrorMessage := JPE_ERROR_PREMATURE;
                            Self.fldProgress.ErrorType:=JPERR_READERROR;
                            Exit;
                        end;
                    end;
                end;

                // Decrypt the stream
                UStream := Decryptor.AESDecryptCBC(EStream);
                EStream.Free;

                case CmpType of
                	0:
                   	begin
                        // No compression, write to file
                        SetLength(tBuf, decSize);
                        UStream.Read(tBuf, decSize);
                        if( (not FListMode) and (fldProgress.ErrorType = JPERR_NONE) ) then
                        	FDataWriter.WriteData(tBuf, decSize);
                        UStream.free;
                        Inc(writtenBytes, decSize);
                    end;

                	1:
                   	begin
                    	// Compressed data. Uncompress and write to file.
                        (*
                        tempHeader := getZLibHeader; // Get a standard ZLib header
                        inputStream := TMemoryStream.Create;
                        inputStream.SetSize(decSize + 2);
                        inputStream.Write(tempHeader, 2);
                        inputStream.CopyFrom(UStream, decSize);
                        inputStream.Seek(0,soFromBeginning);

                        UStream.Free;
                        extractingStream := TDecompressionStream.Create(inputStream);
                        *)

                        extractingStream := TDecompressionStream.Create(UStream, true);

                        // Make enough space
                        SetLength(stringBuffer, SizeUncmp);
                        try
                            repeat
                            	thisReadSize := extractingStream.Read(PChar(stringBuffer)^, min(SizeUncmp - writtenBytes, 1048756));
                                if(thisReadSize > 0) then
                                	Inc(writtenBytes, thisReadSize);
                                try
                                	if( (not FListMode) and (fldProgress.ErrorType = JPERR_NONE)) then
                                    	FDataWriter.WriteData(PChar(stringBuffer)^, thisReadSize);
                                except
                                    On E: Exception do
                                        if not FSkipErrors then
                                        begin
                                            Self.fldProgress.Status:=jpesError;
                                            Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FILECREATION, [outPath]);
                                            Self.fldProgress.ErrorType:=JPERR_FILECREATION;
                                            FDataWriter.StopFile();
                                            Exit;
                                        end
                                        else
                                            fldProgress.ErrorType := JPERR_IGNORED;
                                end;
                            until thisReadSize = 0;
                        except
                            On E: Exception do
                            begin
                                Self.fldProgress.Status:=jpesError;
                                Self.fldProgress.ErrorMessage:=JPE_ERROR_DEFLATEEXCEPTION;
                                Self.fldProgress.ErrorType:=JPERR_DEFLATEEXCEPTION;
                                FDataWriter.StopFile();
                                Exit;
                            end;
                        end;

                        extractingStream.Free;
                        //inputStream.Free;
                        UStream.Free;
                    end;
                end;
            end;
            if( (not FListMode) and (fldProgress.ErrorType = JPERR_NONE) ) then
                FDataWriter.stopFile;

        end;

        2: // Symbolic link handling for *NIX Operating Systems (e.g. Linux).
           // NB! Only UNCOMPRESSED contents are expected! Otherwise, hell will
   		   // happen...
        begin
			actualReadSize := 0;
            strLinkTarget:='';
            writtenBytes := 0;

            while writtenBytes < SizeUncmp do
            begin
                // Read the Data Chunk Block header
                BlockRead(Self.f, encSize, SizeOf(encSize));
                BlockRead(Self.f, decSize, SizeOf(decSize));
                Inc(SizeCmp, decSize);

                // Read encrypted data in a memory stream
                SetLength(tempString, encSize);
                bytesLeft := encSize;
                actualReadSize := 0;
                EStream := TMemoryStream.Create;
                EStream.SetSize(encSize);

				while bytesLeft > 0 do
                begin
                    try
                        BlockRead(Self.f, tempString[1], bytesLeft, actualReadSize);
                        EStream.Write(tempString[1], actualReadSize);
                    except
                        On E: Exception do
                        begin
                            Self.fldProgress.Status:=jpesError;
                            Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
                            Self.fldProgress.ErrorType:=JPERR_READERROR;
                            Exit;
                        end;
                    end;
                    bytesLeft := LongWord(bytesLeft) - actualReadSize;

                    // Try to detect an end of part
                    if( getEOF(True) and ( bytesLeft > 0)  ) then
                    begin
                        // This part reached to an end, or is the file corrupt?
                        if not getEOF(false) then
                        begin
                            // Let's go to the next part
                            if not getNextPart then
                            begin
                                Self.fldProgress.Status := jpesError;
                                Self.fldProgress.ErrorMessage := JPE_ERROR_NONEXTPART;
                                Self.fldProgress.ErrorType:=JPERR_READERROR;
                                Exit;
                            end;
                        end
                        else
                        begin
                            // Nope, it's a premature end of archive
                            Self.fldProgress.Status := jpesError;
                            Self.fldProgress.ErrorMessage := JPE_ERROR_PREMATURE;
                            Self.fldProgress.ErrorType:=JPERR_READERROR;
                            Exit;
                        end;
                    end;
                end;

                // Decrypt the stream
                UStream := Decryptor.AESDecryptCBC(EStream);
                EStream.Free;

                // No compression, write to file
                SetLength(tempString, decSize);
                UStream.Read(tempString[1], decSize);
                strLinkTarget := strLinkTarget + tempString;
                UStream.free;
                Inc(writtenBytes, decSize);
            end;

            if FDataWriter.mkSymLink( PChar(strLinkTarget), PChar(outPath) ) <> 0 then
            begin
                Self.fldProgress.Status:=jpesError;
                Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
                Self.fldProgress.ErrorType:=JPERR_READERROR;
                Exit;
            end;
		end;
   end;

	Self.Offset := getOffset;

    with Self.fldProgress do
    begin
        Status:=jpesRunning;
        RunningCompressed := RunningCompressed + SizeCmp;
        RunningUncompressed := RunningUncompressed + SizeUncmp;
    end;

    with Result do
    begin
        StoredName:=StoredPath;
        AbsoluteName:=outPath;
        UncompressedSize:=SizeUncmp;
        CompressedSize:=SizeCmp;
    end;

	if(FilePos(f) >= FileSize(f)) then
        Self.fldProgress.Status:=jpesFinished;

    Self.fldProgress.ErrorType:=JPERR_NONE;

    Exit;
end;

end.