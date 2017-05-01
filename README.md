# Akeeba Portable Tools

Akeeba Backup and Akeeba Solo desktop tools.

This repository contains the desktop utilities which are meant to be used with Akeeba Backup / Akeeba Solo to make your
life easier. All of our tools are now written in C#, targetting .NET Framework 4.5 / Mono 4.5. Graphical interfaces are
built using WindowsForms for maximum cross-platform interoperability.

## License

Please consult [the license file](LICENSE.md) in this repository.

## Support for susbcribers only

Support for this software is only provided to subscribers of Akeeba Backup and Akeeba Solo and *only* through our site's
Support section.

Please note that Issues requesting support or for any other non-code reason in this repository will be closed without a
response. This is necessary to keep things tidy. Thank you for your understanding.

## Developers welcome

The GitHub reposiory is meant for use by developers only. If you are a developer and have a code-related observation,
suggestion or proposal you are welcome to submit an Issue or, better yet, a Pull Request with your code.

The repository comes as a single Solution. You can open it with most .NET IDEs such as Visual Studio, Xamarin Studio,
MonoDevelop or JetBrains Rider. Before trying to compile the solution please remember to install the NuGet packages.

The projects in this solution are as follows:
* **extractCLI** The command-line version of Akeeba eXtract
* **ExtractWizard** The GUI, WindowsForms version of Akeeba eXtract Wizard
* **Unarchiver** A library you can use to extract JPA, JPS and ZIP archives created with any Akeeba Backup version published since 2011.