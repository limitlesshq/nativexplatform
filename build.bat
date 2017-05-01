REM Build script for Windows

REM Rebuild the Windows .EXE and the self-executing installer
MKDIR release
MSBuild /property:Platform="x86" /property:Configuration=Release /target:Clean;Rebuild

REM Copy the installer to the release directory
COPY AkeebaPortableToolsSetup\bin\Release\*.exe release
