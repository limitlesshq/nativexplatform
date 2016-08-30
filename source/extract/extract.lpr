program extract;
{<
 Akeeba eXtract Wizard
 Copyright (c)2008-2016 Nicholas K. Dionysopoulos
 Licensed under the GNU General Public Licence version 3, or any later version published by the Free Software Foundation
}

{$mode objfpc}{$H+}

uses {$IFDEF UNIX} {$IFDEF UseCThreads}
  cthreads, {$ENDIF} {$ENDIF}
  Interfaces, // this includes the LCL widgetset
  Forms,
  main,
  engineunjpa,
  engineunjps,
  engineunzip,
  extractengineinterface  { you can add units after this }
  ;

{$R *.res}

begin
  Application.Title := 'Akeeba eXtract Wizard';
  Application.Initialize;
  Application.CreateForm(TFormMain, FormMain);
  Application.Run;
end.

