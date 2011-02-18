unit engineunzip;
{<
 Native Archive Extraction libraries
 Copyright (c)2008-2009 Nicholas K. Dionysopoulos

 ZIP extraction engine
}
interface

uses
  extractengineinterface;

type
	TUnZIP = class(TExtractionEngine)
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
    	{$DEFINE WINDOWS}
		StrUtils, // Delphi on Windows - Use StrUtils and ZLib unit
		zlib
	{$ENDIF};

type
    // ZIP End of Central Directory record
    TZIPEOCDRecord = record
        Signature:              LongWord;
        DiskNumber:             Word;
        CDDisk:                 Word;
        DiskCDEntries:          Word;
        NumFilesInCD:           Word;
        CDLength:               LongWord;
        CDOffset:               LongWord;
        CommentLength:          Word;
    end;

    // ZIP Central Directory File Header -- 46 bytes
    TZIPCDFileHeader = record
        Signature:              LongWord;
        VersionMadeBy:          Word;
        VersionToExtract:       Word;
        Flags:                  Word;
        CompressionMethod:      Word;
        LastModTime:            Word;
        LastModDate:            Word;
        CRC32:                  LongWord;
        CompSize:               LongWord;
        UncompSize:             LongWord;
        FileNameLength:         Word;
        ExtraFieldLength:       Word;
        FileCommentLength:      Word;
        DiskNumberStart:        Word;
        InternalFileAttributes: Word;
        ExternalFileAttributes: LongWord;
        RelativeOffset:         Word;
    end;

    // ZIP Local File Header -- 30 bytes + variable
    TZIPLocalFileHeader = record
        Signature:              LongWord;
        VersionToExtract:       Word;
        Flags:                  Word;
        CompressionMethod:      Word;
        LastModTime:            Word;
        LastModDate:            Word;
        CRC32:                  LongWord;
        CompSize:               LongWord;
        UncompSize:             LongWord;
        FileNameLength:         Word;
        ExtraFieldLength:       Word;
        Filename:               string;
    end;

// It has to locate the ZIP Central Directory and extract some information
procedure TUnZIP.ReadHeader;
    function isEOCD(aBuffer: array of Byte): boolean;
    // Checks if the first four bytes of a buffer indicate the End of Central Directory
    begin
        if (aBuffer[0]=$50) and (aBuffer[1]=$4b) and (aBuffer[2]=$05) and (aBuffer[3]=$06) then
            result := true
        else
            result := false;
    end;

    function ReadNextHeader(): TZIPCDFileHeader;
    var
        CurOffset:  LongInt;
    begin
        BlockRead(Self.f, Result.Signature, 4);
        BlockRead(Self.f, Result.VersionMadeBy, 2);
        BlockRead(Self.f, Result.VersionToExtract, 2);
        BlockRead(Self.f, Result.Flags, 2);
        BlockRead(Self.f, Result.CompressionMethod, 2);
        BlockRead(Self.f, Result.LastModTime, 2);
        BlockRead(Self.f, Result.LastModDate, 2);
        BlockRead(Self.f, Result.CRC32, 4);
        BlockRead(Self.f, Result.CompSize, 4);
        BlockRead(Self.f, Result.UncompSize, 4);
        BlockRead(Self.f, Result.FileNameLength, 2);
        BlockRead(Self.f, Result.ExtraFieldLength, 2);
        BlockRead(Self.f, Result.FileCommentLength, 2);
        BlockRead(Self.f, Result.DiskNumberStart, 2);
        BlockRead(Self.f, Result.InternalFileAttributes, 2);
        BlockRead(Self.f, Result.ExternalFileAttributes, 4);
        BlockRead(Self.f, Result.RelativeOffset, 4);

        // Skip filename, extra field and comment
        CurOffset := filepos(Self.f);
        CurOffset := CurOffset + result.FileNameLength + result.ExtraFieldLength + result.FileCommentLength;
        Seek(Self.f, CurOffset);
    end;

var
    tempBuffer:                 Array[0..21] of Byte;
    // Used in EOCD searching
    localOffset:                LongWord;
    zipSize:                    LongWord;
    foundEOCD:                  boolean;
    // Used in reading the EOCD Record
    EOCD:                       TZIPEOCDRecord;
    // Used in scanning of the CD
    ThisHeader:                 TZIPCDFileHeader;
    i:                          Integer;
begin
    // The EOCD record is 22 to infinity bytes long. Its first 22 bytes are a
    // pre-defined data record, whereas the rest are the ZIP file comment.
    // In order to determine its location relative to the archive's EOF I chose
    // to implement an inneficient backwards sliding window algorithm. We start
    // by reading the last 22 bytes of the archive. If the header is not found,
    // we keep sliding backwards, one byte at a time until we either locate the
    // header or reach the BOF. The latter case means we don't have a valid
    // archive. This shouldn't happen, unless the archive was truncated in
    // transit.
    // Start with the last part
    OpenPart( Length(PartsArray) - 1 );

    zipSize := FileSize(Self.f);
    localOffset := zipSize - 22;
    foundEOCD := false;
    try
		while not (foundEOCD or (localOffset = 0)) do
        begin
            Seek(Self.F, localOffset);
            BlockRead(Self.f, tempBuffer, Length(tempBuffer));
            foundEOCD := isEOCD(tempBuffer);
            if not foundEOCD then Dec(localOffset, 1);
            if (localOffset = 0) and (CurrentPart = 0) then
            begin
                OpenPart(CurrentPart - 1);
                localOffset := FileSize(f) - 22;
            end;
        end;
    except
        On Exception do begin
        Self.fldProgress.Status:=jpesError;
        Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
        Self.fldProgress.ErrorType:=JPERR_READERROR;
        Exit;
        end;
    end;

    // Handle EOCD not found
    if not foundEOCD then
    begin
        Self.fldProgress.ErrorMessage := JPE_ERROR_NOEOCD;
        Self.fldProgress.ErrorType:=JPERR_NOEOCD;
        Self.fldProgress.Status := jpesError;
        Exit;
    end;

    // Being here, means we found the End Of Central Directory.
    // Let's read it, then!
    Seek(Self.f, localOffset);
    try
        BlockRead(Self.f, EOCD.Signature, 4);
        BlockRead(Self.f, EOCD.DiskNumber, 2);
        BlockRead(Self.f, EOCD.CDDisk, 2);
        BlockRead(Self.f, EOCD.DiskCDEntries , 2);
        BlockRead(Self.f, EOCD.NumFilesInCD , 2);
        BlockRead(Self.f, EOCD.CDLength , 4);
        BlockRead(Self.f, EOCD.CDOffset , 4);
        BlockRead(Self.f, EOCD.CommentLength , 2);
    except
        On Exception do begin
        Self.fldProgress.Status:=jpesError;
        Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
        Self.fldProgress.ErrorType:=JPERR_READERROR;
        Exit;
        end;
    end;

    // Update archive information we already know of
    self.fldArchiveInformation.ArchiveType := jpatZIP;
    self.fldArchiveInformation.FileCount := eocd.NumFilesInCD;
    self.fldArchiveInformation.UncompressedSize := 0;
    self.fldArchiveInformation.CompressedSize := 0;

    // Read the central directory entries and calculate comp./uncomp. file sizes
    Seek(Self.f, EOCD.CDOffset);
    for i:=1 to EOCD.NumFilesInCD do
    begin
    	try
            ThisHeader := ReadNextHeader;
            Inc(self.fldArchiveInformation.UncompressedSize, ThisHeader.UncompSize);
            Inc(self.fldArchiveInformation.CompressedSize, ThisHeader.CompSize);
            if Eof(f) then OpenPart( CurrentPart + 1 );
		except
			on Exception do begin
			Self.fldProgress.Status:=jpesError;
			Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
            Self.fldProgress.ErrorType:=JPERR_READERROR;
			Exit;
			end;
		end;
	end;

    OpenPart(0);
    // ... and finally we're done!
end;

function TUnZIP.Extractnext(): TLastEntityInformation;
    function isLFH(var aBuffer: array of Byte): boolean;
    // Checks if the first four bytes of a buffer indicate the End of Central Directory
    begin
        if (aBuffer[0]=$50) and (aBuffer[1]=$4b) and (aBuffer[2]=$03) and (aBuffer[3]=$04) then
            result := true
        else
            result := false;
    end;

    function isMultiPartHeader(var aBuffer: array of Byte) : Boolean;
    // Checks if a header is the mark of a multipart archive
    begin
        if (aBuffer[0]=$50) and (aBuffer[1]=$4b) and (aBuffer[2]=$07) and (aBuffer[3]=$08) then
            result := true
        else if (aBuffer[0]=$50) and (aBuffer[1]=$4b) and (aBuffer[2]=$30) and (aBuffer[3]=$30) then
            result := true
        else
            result := false;
    end;
const
    MAX_BUFFER_SIZE = 1048576;
var
    FileHeader      : TZIPLocalFileHeader;

    PathBuffer		: array[0..MAX_BUFFER_SIZE] of AnsiChar;
	StoredPathBin	: PAnsiChar;
	StoredPath		: WideString;
    PStoredPath		: PWideChar;

	bytesLeft		: LongInt;
	nextRead		: LongWord;
	tempBuffer		: array[0..MAX_BUFFER_SIZE] of Byte;	// A temporary buffer
	stringBuffer	: string;
	actualReadSize	: LongWord;					// How many bytes were actually read
	outPath			: string;					// Output full path and file name

	extractingStream: TDecompressionStream;		// Decompresses data on the fly
	inputStream		: TMemoryStream;			// Holds the compressed data
	tempHeader		: TTwoBytesArray;			// A copy of ZLib header

	strLinkTarget	: String;
	isSymlink		: Boolean;

    i               : Integer;

    offsetAfterHeader	: LongWord;
    offsetAfterBlock	: LongWord;
label
	IgnoredError;
begin
	Self.fldProgress.Status := jpesRunning;
    Self.fldProgress.ErrorType := JPERR_NONE;
    Self.fldProgress.ErrorMessage := '';

    try
        Self.skipToOffset(Self.Offset);
        // Read the header
        BlockRead(Self.f, tempBuffer, 4);
        // Is this a split archive marker?
        if isMultiPartHeader(tempBuffer) then
            // Skip over this and re-read the header
            BlockRead(Self.f, tempBuffer, 4);

        if not isLFH(tempBuffer) then
        begin
            // This is not a local file. are we done yet?
            if fldProgress.RunningUncompressed >= fldArchiveInformation.UncompressedSize then
                // yes, we are done indeed
                fldProgress.Status := jpesFinished
            else
            begin
                // no, this is an invalid archive
                fldProgress.Status := jpesError;
                fldProgress.ErrorMessage := JPE_ERROR_INVALIDARCHIVE;
                Self.fldProgress.ErrorType:=JPERR_INVALIDARCHIVE;
            end;
            Exit;
        end;
        BlockRead(Self.f, FileHeader.VersionToExtract, 2);
        BlockRead(Self.f, FileHeader.Flags, 2);
        BlockRead(Self.f, FileHeader.CompressionMethod, 2);
        BlockRead(Self.f, FileHeader.LastModTime, 2);
        BlockRead(Self.f, FileHeader.LastModDate, 2);
        BlockRead(Self.f, FileHeader.CRC32, 4);
        BlockRead(Self.f, FileHeader.CompSize, 4);
        BlockRead(Self.f, FileHeader.UncompSize, 4);
        BlockRead(Self.f, FileHeader.FileNameLength, 2);
        BlockRead(Self.f, FileHeader.ExtraFieldLength, 2);

        // Read filename
        StoredPathBin := PathBuffer;
        SetLength(StoredPath, FileHeader.FileNameLength+1);
        PStoredPath := PWideChar(StoredPath);
        BlockRead(Self.f, StoredPathBin^, FileHeader.FileNameLength);
        Utf8ToUnicode( PStoredPath, StoredPathBin, FileHeader.FileNameLength+1 );

        FileHeader.Filename := StoredPath;

        {
        SetLength(StoredPathBin, FileHeader.FileNameLength);
    	BlockRead(Self.f, tempBuffer, length(StoredPathBin));
        FileHeader.Filename:='';
    	for i := 0 to FileHeader.FileNameLength-1 do
    	begin
    		FileHeader.Filename := FileHeader.Filename + chr(tempBuffer[i]);
    	end;
        }

        // Skip extra field
        if FileHeader.ExtraFieldLength > 0 then
            BlockRead(Self.f, tempBuffer, FileHeader.ExtraFieldLength);
    except
        On Exception do
        	if not FSkipErrors then begin
            	Self.fldProgress.Status:=jpesError;
            	Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
            	Self.fldProgress.ErrorType:=JPERR_READERROR;
            	Exit;
			end
			else
				fldProgress.ErrorType := JPERR_IGNORED;
    end;

    // Fetch the current offset
    offsetAfterHeader:=getOffset;
	// Calculate the offset we'll be in after reading the file data
    offsetAfterBlock:=offsetAfterHeader + FileHeader.CompSize;

    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

    // If it is the list mode, we don't have to deal with writing to files
    if FListMode then
    begin
    	Self.Offset := offsetAfterBlock; // set the current offset past this block
        with Self.fldProgress do
        begin
            Status:=jpesRunning; // indicate we're still running
            ErrorType:=JPERR_NONE;
            ErrorMessage:='';
            RunningCompressed := RunningCompressed + FileHeader.CompSize;
            RunningUncompressed := RunningUncompressed + FileHeader.UncompSize;
        end;
        with Result do
        begin
        	// return current file info
            StoredName:=FileHeader.Filename;
            AbsoluteName:=FileHeader.Filename;
            UncompressedSize:=FileHeader.UncompSize;
            CompressedSize:=FileHeader.CompSize;
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
    	outPath := Self.getPathName(FileHeader.Filename);
    except
        On Exception do
        	if not FSkipErrors then
         	begin
        		Self.fldProgress.Status:=jpesError;
        		Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FOLDERCREATION, [FileHeader.Filename]);
        		Self.fldProgress.ErrorType:=JPERR_FOLDERCREATION;
        		Exit;
        	end
            else
            	Self.fldProgress.ErrorType := JPERR_IGNORED;
    end;
    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

    // Do nothing more for a directory (last character is slash and size is 0)
    if not( (RightStr(FileHeader.Filename,1)='/') and (FileHeader.UncompSize=0) ) then
    begin
        case FileHeader.CompressionMethod of
            0: // Stored file
                begin
                    // Mark symlinks on platforms which support them (*NIX variants, e.g. Linux)
                    isSymlink := FileHeader.VersionToExtract = $030a;
                    strLinkTarget:='';
					if not isSymlink then begin
                        try
                        	FDataWriter.StartFile(outPath, TrimRight(FileHeader.Filename));
                        except
    	                    On Exception do
                            	if not FSkipErrors then
                         		begin
                                	Self.fldProgress.Status:=jpesError;
                                	Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FILECREATION, [FileHeader.Filename]);
                                	Self.fldProgress.ErrorType:=JPERR_FILECREATION;
                                	Exit;
                            	end
                                else
            						Self.fldProgress.ErrorType := JPERR_IGNORED;
                        end;
                        if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;
                    end; // if not symlink

  					bytesLeft := FileHeader.CompSize;
					actualReadSize := 0;

					while bytesLeft > 0 do
					begin
     					if bytesLeft > Length(tempBuffer) then
							nextRead := Length(tempBuffer)
                        else
                        	nextRead := bytesLeft;
                        try
                        	BlockRead(Self.f, tempBuffer, nextRead, actualReadSize);
                            if isSymlink and (actualReadSize > 0) then
                            	for i := 0 to actualReadSize - 1 do
                                	strLinkTarget:=strLinkTarget + chr(tempBuffer[i]);
                        except
                            On Exception do
                            	if not FSkipErrors then
                            	begin
                                	Self.fldProgress.Status:=jpesError;
                                	Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
                                	Self.fldProgress.ErrorType:=JPERR_READERROR;
                                	Exit;
                            	end
                                else
                                	Self.fldProgress.ErrorType := JPERR_IGNORED;
                        end;
                        if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

                        bytesLeft := LongWord(bytesLeft) - actualReadSize;
                        if not isSymlink then try
                        	FDataWriter.WriteData(tempBuffer, actualReadSize);
                        except
                            On Exception do
                            	if not FSkipErrors then
                             	begin
                                	Self.fldProgress.Status:=jpesError;
                                	Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FILECREATION, [FileHeader.Filename]);
                                	Self.fldProgress.ErrorType:=JPERR_FILECREATION;
                            		Exit;
                            	end
                                else
                                	Self.fldProgress.ErrorType := JPERR_IGNORED;
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
                                    Self.fldProgress.ErrorMessage := JPE_ERROR_READERROR;
                                    Self.fldProgress.ErrorType:=JPERR_READERROR;
                                    Exit;
                                end;
                            end
                            else
                            begin
                                // Nope, it's a premature end of archive
                                Self.fldProgress.Status := jpesError;
                                Self.fldProgress.ErrorMessage := JPE_ERROR_READERROR;
                                Self.fldProgress.ErrorType:=JPERR_READERROR;
                                Exit;
                            end;
                        end;
					end; // while
					{$IFNDEF WINDOWS}
					{$IFDEF fpc}
					if isSymlink then
					begin
	                    if FDataWriter.mkSymLink( PChar(strLinkTarget), PChar(outPath) ) <> 0 then
							if not FSkipErrors then begin
								Self.fldProgress.Status:=jpesError;
								Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
								Self.fldProgress.ErrorType:=JPERR_READERROR;
								Exit;
							end
							else
								fldProgress.ErrorType := JPERR_IGNORED;
						if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;
					end // isSymlink
					else
						FDataWriter.StopFile();
					{$ENDIF} // if FreePascal
					{$ELSE} // is Windows --->
					FDataWriter.StopFile;
					{$ENDIF} // if Windows
				end;

			8: // Deflated
				begin
					// Load compressed data
					inputStream := TMemoryStream.Create;
					tempHeader := getZLibHeader; // Get a standard ZLib header
					actualReadSize := inputStream.Write(tempHeader, 2);
					actualReadSize := 0;

                    SetLength(stringBuffer, FileHeader.CompSize);
  					bytesLeft := FileHeader.CompSize;
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
                            On Exception do
                            	if not FSkipErrors then
                             	begin
                                	Self.fldProgress.Status:=jpesError;
                                	Self.fldProgress.ErrorMessage:=JPE_ERROR_READERROR;
                                	Self.fldProgress.ErrorType:=JPERR_READERROR;
                                	Exit;
                            	end
                                else
                                	Self.fldProgress.ErrorType := JPERR_IGNORED;
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
                                    Self.fldProgress.ErrorMessage := JPE_ERROR_READERROR;
                                    Self.fldProgress.ErrorType:=JPERR_READERROR;
                                    Exit;
                                end;
                            end
                            else
                            begin
                                // Nope, it's a premature end of archive
                                Self.fldProgress.Status := jpesError;
                                Self.fldProgress.ErrorMessage := JPE_ERROR_READERROR;
                                Self.fldProgress.ErrorType:=JPERR_READERROR;
                                Exit;
                            end;
                        end;
					end;

                    // Uncompress and write to target file
                    try
                    	FDataWriter.StartFile(outPath, TrimRight(FileHeader.Filename));
                    except
                        On Exception do
                        	if not FSkipErrors then
                         	begin
                            	Self.fldProgress.Status:=jpesError;
                            	Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FILECREATION, [FileHeader.Filename]);
                            	Self.fldProgress.ErrorType:=JPERR_FILECREATION;
                            	Exit;
                        	end
                            else
                            	Self.fldProgress.ErrorType := JPERR_IGNORED;
                    end;
                    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

					inputStream.Seek(0, 0 );
                    extractingStream := Tdecompressionstream.Create(inputStream);

                    SetLength(stringBuffer, FileHeader.UncompSize);
                    try
                        actualReadSize := extractingStream.Read(PChar(stringBuffer)^, FileHeader.UncompSize);
                    except
                        On Exception do
                        	if not FSkipErrors then
                         	begin
                            	Self.fldProgress.Status:=jpesError;
                            	Self.fldProgress.ErrorMessage:=JPE_ERROR_DEFLATEEXCEPTION;
                            	Self.fldProgress.ErrorType:=JPERR_DEFLATEEXCEPTION;
                                FDataWriter.StopFile();
                            	Exit;
                        	end
                            else
                            	Self.fldProgress.ErrorType := JPERR_IGNORED;
                    end;
                    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

					if not (actualReadSize = FileHeader.UncompSize) then
						if not FSkipErrors then
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
                        On Exception do
                        	if not FSkipErrors then
                        	begin
                            	Self.fldProgress.Status:=jpesError;
                            	Self.fldProgress.ErrorMessage:=Format(JPE_ERROR_FILECREATION, [FileHeader.Filename]);
                            	Self.fldProgress.ErrorType:=JPERR_FILECREATION;
                            	FDataWriter.StopFile();
                            	Exit;
                        	end
                            else
                            	Self.fldProgress.ErrorType := JPERR_IGNORED;
                    end;
                    if (Self.fldProgress.ErrorType = JPERR_IGNORED) then goto IgnoredError;

                    extractingStream.Free;
                    inputStream.Free;
                    FDataWriter.StopFile();
                end;

            else // Can't handle this, sorry!
                begin
                    fldProgress.Status := jpesError;
                    fldProgress.ErrorMessage := JPE_ERROR_UNKNOWNCOMPRESSION;
                    Self.fldProgress.ErrorType:=JPERR_UNKNOWNCOMPRESSION;
                end;
        end;
    end;

    // Update offset
    Self.Offset := getOffset;

    // TODO: Check for extra file data

    // Update results
    with Self.fldProgress do
    begin
        Status:=jpesRunning;
        RunningCompressed := RunningCompressed + FileHeader.CompSize;
        RunningUncompressed := RunningUncompressed + FileHeader.UncompSize;
    end;

    with Result do
    begin
        StoredName:=FileHeader.Filename;
        AbsoluteName:=outPath;
        UncompressedSize:=FileHeader.UncompSize;
        CompressedSize:=FileHeader.CompSize;
    end;

	if(FilePos(f) >= FileSize(f)) then
        Self.fldProgress.Status:=jpesFinished;

    Exit;

// This is a jump point which catches ignorable errors and skips the offending file block
IgnoredError:
	Self.Offset := offsetAfterBlock; // set the current offset past this block

    // TODO: Check for extra data field

    with Self.fldProgress do
    begin
        Status:=jpesRunning; // indicate we're still running
        ErrorType:=JPERR_IGNORED;
        ErrorMessage:=JPE_ERROR_IGNORED;
        RunningCompressed := RunningCompressed + FileHeader.CompSize;
        RunningUncompressed := RunningUncompressed + FileHeader.UncompSize;
    end;
    with Result do
    begin
    	// return current file info
        StoredName:=FileHeader.Filename;
        AbsoluteName:=outPath;
        UncompressedSize:=FileHeader.UncompSize;
        CompressedSize:=FileHeader.CompSize;
    end;
    // If we're past the end of file, mark ourselves as done...
    try
    	skipToOffset(Self.Offset);
    	if(FilePos(f) >= FileSize(f)) then
            Self.fldProgress.Status:=jpesFinished;
    except
    	on E: Exception do Self.fldProgress.Status:=jpesFinished;
    end;

    Exit;
end;

end.