﻿//-----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Akeeba Ltd">
// Copyright (c) 2006-2017  Nicholas K. Dionysopoulos / Akeeba Ltd
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using CommandLine;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Akeeba eXtract CLI")]
[assembly: AssemblyDescription("Extract backup archives made with Akeeba Backup from the CLI")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Akeeba Ltd")]
[assembly: AssemblyProduct("Akeeba Backup Portable Tools")]
[assembly: AssemblyCopyright("Copyright (c) 2006-2017 Nicholas K. Dionysopoulos / Akeeba Ltd")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// CommandLineParser parameters
[assembly: AssemblyLicense(
	           "This is free software. You may redistribute copies of it under the terms of",
	           "the MIT License <http://www.opensource.org/licenses/mit-license.php>.")]
[assembly: AssemblyUsage(
	           @"Usage:    extractCLI archive [targetFolder] [options]",
	           "",
	           @"Examples: extractCLI archive.jpa C:\Target\Folder",
	           @"          extractCLI C:\Foo\archive.zip -t",
	           @"          extractCLI archive.jps C:\Target\Folder -p myPassword")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("a1787806-b40e-41e2-97d4-e08d79a52097")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("4.0.2.*")]
[assembly: AssemblyFileVersion("4.0.2.*")]
[assembly: NeutralResourcesLanguage("en-US")]
