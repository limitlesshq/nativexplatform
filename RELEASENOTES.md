# Release highlights
 
**Critical bug extacting JPS archives**. Only empty, 0 byte files were being written to disk.

For more information and documentation please [consult the documentation Wiki](https://github.com/akeeba/nativexplatform/wiki).
 
This is a bugfix release of Akeeba eXtract Wizard written in C# targeting the .NET and Mono framework versions 4.5 and later.

# Requirements

Akeeba Portable Tools will run on any Windows, Linux or macOS computer on which you can run .NET Framework 4.5 or later; or Mono Framework 4.5 or later. Practically this means:
- Windows Vista or later
- Linux support varies by distribution. We believe Ubuntu Linux 14.04 or later should be supported.
- Mac OS X 10.7 (Lion) or later, macOS Sierra or later

The applications should run out of the box on Windows 10 and later. On earlier versions of Windows you will need to install the [.NET Framework 4.5](https://www.microsoft.com/en-us/download/details.aspx?id=30653) first.

On Linux and macOS you will need to install the [Mono framework 4.5 or later](http://www.mono-project.com/download/). In case your Linux distributions offers separate package for Mono Framework and Gtk# please remember to install _both_.

# GUI and CLI

Akeeba eXtract Wizard is the graphical wizard interface that you probably want to use. It's available in the following files:
- `AkeebaPortableToolsSetup.exe` (Windows) - Just run the installer to install it
- `extract-wizard-linux-*.zip` (Linux) - Extract to a folder and run the ExtractWizardGtk.exe with Mono Framework (or from a Terminal with `monoExtractWizardGtk.exe`
- `extract-wizard-macos-*.zip` (macOS)- Extract to a folder and double click on the application. You may drag it to your Applications folder for easier launch in the future.

There is also a command line (CLI) version which is available on the following packages
- `AkeebaPortableToolsSetup.exe` (Windows) - Just run the installer to install it
- `extract-cli-macos-linux-*.zip` (Linux and macOS) - Extract to a folder and add it to your `$PATH` for quick access.

# Tip

If you're on Windows you may want to disable your Antivirus or Windows Defender when extracting backup archives. The extraction will be much faster.

Alternatively, you can set up a directory exception in Windows Defender / your antivirus: exclude the folder where you are extracting the backup archive to make the extraction faster. If you (also) exclude your local server's web root you'll see that your local Joomla!, WordPress, Drupal, Magento, PrestaShop, etc sites now run very fast.

The reason is simple: antivirus software, like Windows Defender, will scan each and every file being accessed. While this is desirable for random files, it's not a good idea for local sites which need to access hundreds of files on every page load or for extracting a site which consists of several thousands of small files.

# Changelog

* Critical: Only empty files are created when extracting a JPS file
