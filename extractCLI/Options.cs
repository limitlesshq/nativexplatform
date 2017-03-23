//-----------------------------------------------------------------------
// <copyright file="Options.cs" company="Akeeba Ltd">
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

using System;
using System.Reflection;
using Akeeba.extractCLI.Resources;

namespace Akeeba.extractCLI
{
	using CommandLine;
	using CommandLine.Text;

	internal class Options
	{
		[ValueOption(0)]
		public string ArchiveFile { get; set; }

		[ValueOption(1)]
		public string TargetFolder { get; set; }

		[Option('p', "password", Required = false, HelpText = "Encryption password, for JPS archives")]
		public string Password { get; set; }

		[Option('t', "test", Required = false, HelpText = "Test the archive without writing anything to disk")]
		public bool Test { get; set; }

		[Option('s', "silent", Required = false, HelpText = "Encryption password, for JPS archives")]
		public bool Silent { get; set; }

		[Option('v', "verbose", Required = false, HelpText = "Enable verbose output, use when reporting issues")]
		public bool Verbose { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			var help = new HelpText {
				Heading = new HeadingInfo("Akeeba eXtract CLI", Assembly.GetCallingAssembly().GetName().Version.ToString()),
				Copyright = new CopyrightInfo("Nicholas K. Dionysopoulos / Akeeba Ltd", new int[]{2006, DateTime.Now.Year}),
				AdditionalNewLineAfterOption = true,
				AddDashesToOption = true,
				MaximumDisplayWidth = 79
			};
			help.AddPreOptionsLine("-------------------------------------------------------------------------------");
			help.AddPreOptionsLine("This is free software. You may redistribute copies of it under the terms of");
			help.AddPreOptionsLine("the MIT License <http://www.opensource.org/licenses/mit-license.php>.");
			help.AddPreOptionsLine("");
			help.AddPreOptionsLine(@"Usage:    extractCLI archive [targetFolder] [options]");
			help.AddPreOptionsLine("");
			help.AddPreOptionsLine(@"Examples: extractCLI archive.jpa C:\Target\Folder");
			help.AddPreOptionsLine(@"          extractCLI C:\Foo\archive.zip -t");
			help.AddPreOptionsLine(@"          extractCLI archive.jps C:\Target\Folder -p myPassword");
			help.AddOptions(this);
			return help;
		}
	}
}