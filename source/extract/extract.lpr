program extract;

{$mode objfpc}{$H+}

uses {$IFDEF UNIX} {$IFDEF UseCThreads}
  cthreads, {$ENDIF} {$ENDIF}
  Interfaces, // this includes the LCL widgetset
  Forms,
  main,
  AkAESCTR,
  engineunjpa,
  engineunjps,
  engineunzip,
  extractengineinterface { you can add units after this };

{$R *.res}

begin
  Application.Title := 'Akeeba eXtract Wizard';
  Application.Initialize;
  Application.CreateForm(TFormMain, FormMain);
  Application.Run;
end.

