# Requirements

## Build Requirements

On Linux / macOS you must have the Mono SDK and its toolchain already installed. The easiest way to do that is by installing MonoDevelop or Xamarin Studio.

On Windows you must have the .NET Framework SDK and its toolchain already installed. The easiest way to do that is by installing Microsoft Visual Studio Community Edition.

In either case you must have the build toolchain (xbuild for Mono, MSBuild for Windows) in your path.

## Release Requirements

Publishing a release has further requirements. You will need to have the following tools:

* A command line environment.
* A PHP CLI binary in your path
* Command line Git executables
* Phing

You will also need the following path structure inside a folder on your system

* **PortableTools** This repository
* **buildfiles** [Akeeba Build Tools](https://github.com/akeeba/buildfiles)

# Build scripts

## Linux and macOS

The script `build.sh` will build the CLI and GUI tools for Linux / macOS and place them in ZIP files inside the `release` directory under the repository's root.

This script uses Mono Framework's `xbuild` toolchain to build everything and the command line `zip` tool to generate the backup archives. It also makes use of `grep` and `sed` to extract the version number from the solution.

> **IMPORTANT!** Do not run this from Bash on Windows. The generated Akeeba eXtract Wizard will be linked to the Windows version of Gtk# and won't work on Linux and macOS.

## Windows

The script `build.bat` will build the CLI and GUI tools for Windows. The executable installer will be also built and copied to the release directory.

This script uses Microsoft's `MSBuild` toolchain to build everything. Building the installer requires the [WiX Toolset](http://wixtoolset.org/). You only need MSBuild in your PATH. The WiX Toolchain integrates in MSBuild itself, there's no separate executable.

# Release process

> **NOTE** This section is for our internal reference.

## Environment Preparation

The best way to build and release is using Windows with an Ubuntu Linux VM. 

On Windows install [Microsoft Visual Studio](https://www.visualstudio.com/vs/community/) and [WiX Toolset](http://wixtoolset.org/). On Linux `sudo apt-get monodevelop` and let it handle the dependencies automatically.

Create a Shared Folder in VirtualBox to make the Windows' machines Projects directory available for read and write into the Ubuntu Linux VM. Make it permanent and automount.

## Release process

1. Open a CMD.EXE Command Prompt
1. Pull in all changes ```
git checkout development
git pull --all
```
1. Edit the Properties\AssemblyInfo.cs of all C# projects and update the version number.
1. Edit the Product.wxs (ExtractWizardSetup) and Bundle.wxs (AkeebaPortableToolsSetup) of the two installer projects and update the version number.
1. Edit the RELEASENOTES.md file with the new release notes. Remember to copy the information from CHANGELOG.md.
1. `set AKEEBAVERSION=1.2.3`  **IMPORTANT: Change the version number**
1. Pull all changes ```
git checkout master
git pull
git merge development
git push
git checkout development
git push
git tag %AKEEBAVERSION% -sm "Tagging %AKEEBAVERSION%"
git push --tags
```
1. Boot the Linux VM and open a Terminal: `cd /media/sf_Projects/akeeba/PortableTools && ./build.sh`
1. Open a CMD.EXE Command Prompt and ```
cd %USERPROFILE%\Projects\akeeba\PortableTools
build.bat
phing release -Dversion=%AKEEBAVERSION%
```

