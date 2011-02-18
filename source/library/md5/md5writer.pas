unit md5writer;

{$H+}

interface

uses
  Classes, SysUtils, md5,
  extractengineinterface;

type

TMD5Information = record
	Filepath	: string;
    MD5			: string;
end;

{ Event type for MD5 information receival }
TOnMD5Information = procedure(Info: TMD5Information) of object;

{ TMD5DataWriter }

TMD5DataWriter = class(TDataWriter)
	private
        FContext:			MD5Context;
    	FMD5Information:	TMD5Information;
		FOnMD5Information:	TOnMD5Information;
	public
    	procedure       MakeDirRecursive(Dir: String); override; //< Recursively make a directory
        procedure		StartFile(FileName: String; RelativePath: String = '');  override; //< Signal that we've began writing to a new file
        procedure		StopFile();  override; //< Close a file we were writing to
    	procedure		WriteData(var Buffer; Count: LongInt);  override; //< Write some data to the file
        function		mkSymLink( oldname, newname: string ): LongInt;  override; //< Make a symbolic link, an alias to fpSymLink

        property		OnMD5Information: TOnMD5Information read FOnMD5Information write FOnMD5Information;
        property		MD5Information: TMD5Information read FMD5Information;
    end;

implementation

{ TMD5DataWriter }

procedure TMD5DataWriter.MakeDirRecursive(Dir: String);
begin
	// Do nothing; we do not extract files using this engine
end;

procedure TMD5DataWriter.StartFile(FileName: String; RelativePath: String = '');
begin
	FMD5Information.Filepath := RelativePath;
    MD5Init(FContext);
end;

procedure TMD5DataWriter.StopFile();
var
	FileDigest				: MD5Digest;
begin
	MD5Final(FContext, FileDigest);
    FMD5Information.MD5:=MD5Print(FileDigest);
    if assigned(FOnMD5Information) then
    	FOnMD5Information(FMD5Information);
end;

procedure TMD5DataWriter.WriteData(var Buffer; Count: LongInt);
begin
	MD5Update(FContext, @Buffer, Count);
end;

function TMD5DataWriter.mkSymLink(oldname, newname: string): LongInt;
begin
	// Do nothing for symlinks; we don't care extracting them
    Result := 0;
end;

end.
