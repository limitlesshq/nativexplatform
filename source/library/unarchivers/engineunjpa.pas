unit engineunjpa;
{<
 Native Archive Extraction libraries
 Copyright (c)2008-2016 Nicholas K. Dionysopoulos
 Licensed under the GNU General Public Licence version 3, or any later version published by the Free Software Foundation

 JPA archive extraction
}

interface

uses
  extractengineinterface;
type
	TJPAHeader = record
		Signature:		array[0..2] of AnsiChar;
        HeaderLength:	Word;
        MajorVersion:	byte;
        MinorVersion:	byte;
        FileCount:		LongWord;
        Uncompressed:	LongWord;
        Compressed:		LongWord;
	end;

	TUnJPA = class(TExtractionEngine)
		public
			procedure	ReadHeader(); override;
			function	ExtractNext(): TLastEntityInformation; override;
	end;

implementation

uses
	sysutils, Classes,
	{$IFDEF fpc}
		zstream // FreePascal - Use ZStream unit
		{$IFNDEF WINDOWS}, BaseUnix{$ENDIF} // FreePascal on Linux - use BaseUnix
	{$ELSE}
		zlib // Delphi on Windows - Use ZLib unit
	{$ENDIF};

procedure TUnJPA.ReadHeader;
var
	JPAHeader:	TJPAHeader;
	ExtraHeaders: Array[0..10000] of Byte;
	ExtraLength: LongInt;
begin
	try
		OpenPart(0);
    except
		on E: Exception do;
	end;
	BlockRead(Self.f, JPAHeader.Signature, SizeOf(JPAHeader.Signature));
	if JPAHeader.Signature <> 'JPA' then
	begin
		Self.fldProgress.Status:=jpesError;
		Self.fldProgress.ErrorMessage:=JPE_ERROR_INVALIDARCHIVE;
		Self.fldProgress.ErrorType:=JPERR_INVALIDARCHIVE;
		Exit;
	end;

	try
		BlockRead(Self.f, JPAHeader.HeaderLength, SizeOf(JPAHeader.HeaderLength));
		BlockRead(Self.f, JPAHeader.MajorVersion, SizeOf(JPAHeader.MajorVersion));
		BlockRead(Self.f, JPAHeader.MinorVersion, SizeOf(JPAHeader.MinorVersion));
		BlockRead(Self.f, JPAHeader.FileCount, SizeOf(JPAHeader.FileCount));
		BlockRead(Self.f, JPAHeader.Uncompressed, SizeOf(JPAHeader.Uncompressed));
		BlockRead(Self.f, JPAHeader.Compressed, SizeOf(JPAHeader.Compressed));
		Self.Offset := getOffset;
	except
		On Exception do begin
		Self.fldProgress.Status:=jpesError;
		Self.fldProgress.ErrorMessage:=JPE_ERROR_CANTREADHEADER;
        Self.fldProgress.ErrorType:=JPERR_READERROR;
		Exit;
		end;
	end;

	if JPAHeader.HeaderLength > 19 then
	begin
		try
			ExtraLength := JPAHeader.HeaderLength - 19;
			BlockRead(f, ExtraHeaders, ExtraLength);
			Offset := getOffset;
		except
			on E: Exception do;
		end;
	end;

	with Self.fldArchiveInformation do
	begin
		FileCount:=JPAHeader.FileCount;
		CompressedSize:=JPAHeader.Compressed;
		UncompressedSize:=JPAHeader.Uncompressed;
		ArchiveType:=jpatJPA;
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

function TUnJPA.Extractnext(): TLastEntityInformation;
const
    MAX_BUFFER_SIZE = 1048576;
var
	// Components of the JPA Entity Header
	Signature		: Array[0..2] of AnsiChar;
	BlockLength		: Word;
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

	bytesLeft		: LongWord;
	nextRead		: LongWord;
	tempBuffer		: array[0..MAX_BUFFER_SIZE] of Byte;	// A temporary buffer
	stringBuffer	: string;
	actualReadSize	: LongWord;					// How many bytes were actually read
	outPath			: string;					// Output full path and file name

	extractingStream: TDecompressionStream;		// Decompresses data on the fly
	inputStream		: TMemoryStream;			// Holds the compressed data
	tempHeader		: TTwoBytesArray;			// A copy of ZLib header

    extraHeadersSize: Word;						// Size of (ignored) extra headers

    strLinkTarget	: String;

	i				: Integer;

    offsetAfterHeader,
    offsetAfterBlock  : LongWord;
label
	IgnoredError;
begin
	Self.fldProgress.Status := jpesRunning;
    Self.fldProgress.ErrorType := JPERR_NONE;
    Self.fldProgress.ErrorMessage := '';

	if self.Offset >= PartsArray[ Length(PartsArray)-1 ].EndOffset then
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
    	BlockRead(Self.f, Signature, sizeOf(Signature));
        if( Signature <> 'JPF' ) then
        begin
            Self.fldProgress.Status:=jpesError;
            Self.fldProgress.ErrorMessage:=JPE_ERROR_INVALIDARCHIVE;
			Self.fldProgress.ErrorType:=JPERR_INVALIDARCHIVE;
            Exit;
        end;
    	BlockRead(Self.f, BlockLength, SizeOf(BlockLength));
    	BlockRead(Self.f, PathLength, SizeOf(PathLength));

        StoredPathBin := PathBuffer;
        for i := 0 to MAX_BUFFER_SIZE do
            PathBuffer[i] := #0;
        BlockRead(Self.f, StoredPathBin^, PathLength);
        StoredPath := StoredPathBin;

    	BlockRead(Self.f, EntityType, SizeOf(EntityType));
    	BlockRead(Self.f, CmpType, SizeOf(CmpType));
    	BlockRead(Self.f, SizeCmp, SizeOf(SizeCmp));
    	BlockRead(Self.f, SizeUncmp, SizeOf(SizeUncmp));
    	BlockRead(Self.f, Permissions, SizeOf(Permissions));

        // Extra headers size: Total block length, minus the fixed part of the
        // header (21 bytes) and the variable Path Length
        extraHeadersSize := BlockLength - 21 - PathLength;
    	// If there are extra headers, skip them
        if(extraHeadersSize > 0) then
        	BlockRead(Self.f, tempBuffer, extraHeadersSize);
    except
        On Exception do begin
        Self.fldProgress.Status:=jpesError;
        Self.fldProgress.ErrorMessage:=JPE_ERROR_CANTREADFHEADER;
        Self.fldProgress.ErrorType:=JPERR_READERROR;
        Exit;
        end;
    end;

    // Fetch the current offset
    offsetAfterHeader:=getOffset;
	// Calculate the offset we'll be in after reading the file data
    offsetAfterBlock:=offsetAfterHeader + SizeCmp;

    // If it is the list mode, we don't have to deal with writing to files
    if FListMode then
    begin
    	Self.Offset := offsetAfterBlock; // set the current offset past this block
        with Self.fldProgress do
        begin
            Status:=jpesRunning; // indicate we're still running
            ErrorType:=JPERR_NONE;
            ErrorMessage:='';
            RunningCompressed := RunningCompressed + SizeCmp;
            RunningUncompressed := RunningUncompressed + SizeUncmp;
        end;
        with Result do
        begin
        	// return current file info
            StoredName:=StoredPath;
            AbsoluteName:=StoredPath;
            UncompressedSize:=SizeUncmp;
            CompressedSize:=SizeCmp;
        end;
        // If we're past the end of file, mark ourselves as done...
        try
        	if not skipToOffset(Self.Offset) then Self.fldProgress.Status:=jpesFinished;
        	if(FilePos(f) >= FileSize(f)) then
                Self.fldProgress.Status:=jpesFinished;
        except
        	on E: Exception do Self.fldProgress.Status:=jpesFinished;
        end;

        Exit;
    end;

    // Recursively makes the directories and returns the target file path
    try
    	outPath := Self.getPathName(StoredPath);
    except
        On E: Exception do
        	if not FSkipErrors then
         	begin
                Self.fldProgress.Status:=jpesError;
                Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FOLDERCREATION, [StoredPath]);
                Self.fldProgress.ErrorType:=JPERR_FOLDERCREATION;
                Exit;
        	end
            else
            	Self.fldProgress.ErrorType := JPERR_IGNORED;
    end;
    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

    // If that was a directory, we are done
	case EntityType of
		0: // Directory
      	begin
			// Do nothing more
		end;

        1:
		begin
			case CmpType of
            	0: // No compression
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
                    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

  					bytesLeft := SizeCmp;
					actualReadSize := 0;

                    SetLength(stringBuffer, MAX_BUFFER_SIZE);

					while bytesLeft > 0 do
					begin
     					if bytesLeft > Length(stringBuffer) then
							nextRead := Length(stringBuffer)
                        else
                        	nextRead := bytesLeft;
                        try
                        	BlockRead(Self.f, PChar(stringBuffer)^, nextRead, actualReadSize);
                        except
                            On E: Exception do
                            	if not FSkipErrors then begin
                                    Self.fldProgress.Status:=jpesError;
    								Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
                                	Self.fldProgress.ErrorType:=JPERR_READERROR;
                                    Exit;
                            	end
                                else
                                	fldProgress.ErrorType := JPERR_IGNORED;
                        end;
                        if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;
                        bytesLeft := LongWord(bytesLeft) - actualReadSize;
                        try
                        	FDataWriter.WriteData(PChar(stringBuffer)^, actualReadSize);
                        except
                            On E: Exception do
                            	if not FSkipErrors then begin
                                    Self.fldProgress.Status:=jpesError;
                                    Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FILECREATION, [StoredPath]);
                                    Self.fldProgress.ErrorType:=JPERR_FILECREATION;
                                	Exit;
                            	end
                                else
                                fldProgress.ErrorType := JPERR_IGNORED;
                        end;
                        if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

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
                    FDataWriter.StopFile();
                  end;

				1: // GZip compression
				begin
					// Load compressed data
                    inputStream := TMemoryStream.Create;
					tempHeader := getZLibHeader; // Get a standard ZLib header
					actualReadSize := inputStream.Write(tempHeader, 2);
					actualReadSize := 0;

					SetLength(stringBuffer, SizeCmp);
					bytesLeft := SizeCmp;
					actualReadSize := 0;

					while bytesLeft > 0 do
					begin
     					if bytesLeft > Length(stringBuffer) then
							nextRead := Length(stringBuffer)
                        else
                        	nextRead := bytesLeft;
                        try
                        	BlockRead(Self.f, PChar(stringBuffer)^, nextRead, actualReadSize);
                        except
                            On E: Exception do
                                if not FSkipErrors then begin
                                    Self.fldProgress.Status := jpesError;
    								Self.fldProgress.ErrorMessage := JPE_ERROR_READERROR + ' DEBUG To GZip efage poutso';
                                    Self.fldProgress.ErrorType:=JPERR_READERROR;
                                    Exit;
                                end
                                else
                                fldProgress.ErrorType := JPERR_IGNORED;
                        end;
                        if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;
                        bytesLeft := LongWord(bytesLeft) - actualReadSize;
                        inputStream.Write(PChar(stringBuffer)^, actualReadSize);

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

                    // Uncompress and write to target file
                    try
                    	FDataWriter.StartFile(outPath, TrimRight(StoredPath));
                    except
                        On E: Exception do
                        	if not FSkipErrors then begin
                                Self.fldProgress.Status:=jpesError;
                                Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FILECREATION, [outPath]);
                                Self.fldProgress.ErrorType:=JPERR_FILECREATION;
                                Exit;
                        	end
                            else
                            fldProgress.ErrorType := JPERR_IGNORED;
                    end;
                    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

                    inputStream.Seek(0, 0 );
                    extractingStream := Tdecompressionstream.Create(inputStream);

                    SetLength(stringBuffer, SizeUncmp);
                    try
                        actualReadSize := extractingStream.Read(PChar(stringBuffer)^, SizeUncmp);
                    except
                        On E: Exception do
                            if not FSkipErrors then	begin
                                Self.fldProgress.Status:=jpesError;
                                Self.fldProgress.ErrorMessage:=JPE_ERROR_DEFLATEEXCEPTION;
                                Self.fldProgress.ErrorType:=JPERR_DEFLATEEXCEPTION;
                                FDataWriter.StopFile();
                                Exit;
                            end
                            else
                            	fldProgress.ErrorType := JPERR_IGNORED;
                    end;
                    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

					if (actualReadSize <> SizeUncmp) then
						if (not FSkipErrors) then
						begin
							Self.fldProgress.Status:=jpesError;
							Self.fldProgress.ErrorMessage:=JPE_ERROR_DEFLATEEXCEPTION;
							Self.fldProgress.ErrorType:=JPERR_DEFLATEEXCEPTION;
                            FDataWriter.StopFile();
							Exit;
						end
						else
							goto IgnoredError;

                    try
						FDataWriter.WriteData(PChar(stringBuffer)^, actualReadSize);
						//FDataWriter.WriteData(stringBuffer, actualReadSize);
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
                    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

                    extractingStream.Free;
                    inputStream.Free;
                    FDataWriter.StopFile();
				end;
            end;
         end;

         2: // Symbolic link handling for *NIX Operating Systems (e.g. Linux).
			// NB! Only UNCOMPRESSED contents are expected! Otherwise, hell will
   			// happen...
         	begin
			bytesLeft := SizeCmp;
			actualReadSize := 0;
            strLinkTarget:='';

			while bytesLeft > 0 do
			begin
				if bytesLeft > Length(tempBuffer) then
					nextRead := Length(tempBuffer)
            	else
            		nextRead := bytesLeft;

				try
					BlockRead(Self.f, tempBuffer, nextRead, actualReadSize);
					if actualReadSize > 0 then
                		for i:=0 to actualReadSize - 1 do
                    		strLinkTarget := strLinkTarget + Chr(tempBuffer[i]);
				except
            		On E: Exception do
                	if not FSkipErrors then begin
                		Self.fldProgress.Status:=jpesError;
						Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR + ' DEBUG To symlink mou mesa gamw thn poutana moy gamw';
                    	Self.fldProgress.ErrorType:=JPERR_READERROR;
                    	Exit;
                	end
					else
						fldProgress.ErrorType := JPERR_IGNORED;
            	end;
				if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

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

            if FDataWriter.mkSymLink( PChar(strLinkTarget), PChar(outPath) ) <> 0 then
               	if not FSkipErrors then begin
               		Self.fldProgress.Status:=jpesError;
					Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR + ' DEBUG Phga na grapsw to symlink kai efaga palto - Apo '+strLinkTarget+' sto '+outPath;
                   	Self.fldProgress.ErrorType:=JPERR_READERROR;
                   	Exit;
               	end
				else
					fldProgress.ErrorType := JPERR_IGNORED;
			if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;
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

// This is a jump point which catches ignorable errors and skips the offending file block
IgnoredError:
	Self.Offset := offsetAfterBlock; // set the current offset past this block
    with Self.fldProgress do
    begin
        Status:=jpesRunning; // indicate we're still running
        ErrorType:=JPERR_IGNORED;
        ErrorMessage:=JPE_ERROR_IGNORED;
        RunningCompressed := RunningCompressed + SizeCmp;
        RunningUncompressed := RunningUncompressed + SizeUncmp;
    end;
    with Result do
    begin
    	// return current file info
        StoredName:=StoredPath;
        AbsoluteName:=outPath;
        UncompressedSize:=SizeUncmp;
        CompressedSize:=SizeCmp;
    end;
    // If we're past the end of file, mark ourselves as done...
    try
    	skipToOffset(Self.Offset);
    	if(FilePos(f) >= FileSize(f)) then
            Self.fldProgress.Status:=jpesFinished;
    except
    	on E: Exception do Self.fldProgress.Status:=jpesFinished;
    end;
end;

end.
