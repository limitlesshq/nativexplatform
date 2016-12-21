//-----------------------------------------------------------------------
// <copyright file="extractCLIProgram.cs" company="Akeeba Ltd">
// Copyright (c) 2006-2016  Nicholas K. Dionysopoulos / Akeeba Ltd
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using Akeeba.extractCLI.Resources;
using Akeeba.Unarchiver;
using Akeeba.Unarchiver.DataWriter;
using Akeeba.Unarchiver.EventArgs;
using CommandLine;

namespace Akeeba.extractCLI
{
    class ExtractCliProgram
    {
	    private static Options _options;

        static int Main(string[] args)
        {
	        // Get a reference to the language file
            ResourceManager text = Language.ResourceManager;

	        // Parse the command line options
	        _options = new Options();

	        if (!Parser.Default.ParseArguments(args, _options))
	        {
		        Console.WriteLine(_options.GetUsage());

		        return 255;
	        }

	        // Make sure an archive file is given
	        if (_options.ArchiveFile == null)
	        {
		        Console.WriteLine(_options.GetUsage());

		        return 255;
	        }

	        // Display a banner
	        if (!_options.Silent)
	        {
		        Console.WriteLine("Akeeba eXtract CLI v. " + Assembly.GetCallingAssembly().GetName().Version);
		        Console.WriteLine($"Copyright (c)2006-{DateTime.Now.Year} Nicholas K. Dionysopoulos / Akeeba Ltd");
		        Console.WriteLine("-------------------------------------------------------------------------------");
		        Console.WriteLine("This is free software. You may redistribute copies of it under the terms of");
		        Console.WriteLine("the MIT License <http://www.opensource.org/licenses/mit-license.php>.");
		        Console.WriteLine("");
	        }

	        // If no output directory is given we assume the current workign directory
	        if (_options.TargetFolder == null)
	        {
		        _options.TargetFolder = Directory.GetCurrentDirectory();
	        }

            try
            {
	            // Make sure the file exists
	            if (!File.Exists(_options.ArchiveFile))
	            {
		            throw new FileNotFoundException(String.Format(text.GetString("ERR_NOT_FOUND"), _options.ArchiveFile));
	            }

	            // Make sure the file can be read
	            try
	            {
		            using (FileStream stream = File.Open(_options.ArchiveFile, FileMode.Open, FileAccess.Read))
		            {
		            }
	            }
	            catch
	            {
		            throw new FileNotFoundException(String.Format(text.GetString("ERR_CANNOT_READ"), _options.ArchiveFile));
	            }

	            // Try to extract
                using (Unarchiver.Unarchiver extractor = Unarchiver.Unarchiver.CreateForFile(_options.ArchiveFile, _options.Password))
                {
                    // Attach event subscribers
                    extractor.ProgressEvent += OnProgressHandler;
                    extractor.EntityEvent += onEntityHandler;

	                // Create a cancelation token (it's required by the unarchiver)
                    CancellationTokenSource cts = new CancellationTokenSource();
                    var token = cts.Token;

                    Task t = Task.Factory.StartNew(
                        () =>
                        {
	                        if (extractor == null)
	                        {
		                        throw new Exception("Internal state consistency violation: extractor object is null");
	                        }

	                        // Get the appropriate writer
	                        IDataWriter writer = new NullWriter();

	                        if (!_options.Test)
	                        {
		                        writer = new DirectFileWriter(_options.TargetFolder);
	                        }

	                        // Test the extraction
	                        extractor.Extract(token, writer);
                        }, token,
		                TaskCreationOptions.None,
						TaskScheduler.Default
                    );

                    t.Wait(token);
                }
            }
            catch (Exception e)
            {
	            Exception targetException = (e.InnerException == null) ? e : e.InnerException;

	            if (!_options.Silent || _options.Verbose)
	            {
		            Console.WriteLine("");
		            Console.WriteLine(text.GetString("ERR_HEADER"));
		            Console.WriteLine("");
		            Console.WriteLine(targetException.Message);
	            }

	            if (_options.Verbose)
	            {
		            Console.WriteLine("");
		            Console.WriteLine("Stack trace:");
		            Console.WriteLine("");
		            Console.WriteLine(targetException.StackTrace);
	            }

				return 250;
            }

	        return 0;
        }

        private static void OnProgressHandler(object sender, ProgressEventArgs e)
        {
            ResourceManager text = Language.ResourceManager;

            switch (e.Progress.Status)
            {
                case ExtractionStatus.Error:
	                if (!_options.Silent || _options.Verbose)
	                {
		                Console.WriteLine(text.GetString("ERR_HEADER"));
		                Console.WriteLine(e.Progress.LastException.Message);
		                Console.WriteLine(e.Progress.LastException.StackTrace);
	                }
	                break;

                case ExtractionStatus.Running:
	                if (_options.Verbose)
	                {
		                Console.WriteLine($"[File position {e.Progress.FilePosition, 0}]");
	                }
                    break;

                case ExtractionStatus.Finished:
	                if (!_options.Silent)
	                {
		                Console.WriteLine(text.GetString("LBL_STATUS_FINISHED"));
	                }
                    break;

                case ExtractionStatus.Idle:
	                if (!_options.Silent)
	                {
		                Console.WriteLine(text.GetString("LBL_STATUS_IDLE"));
	                }
                    break;
            }
        }

        private static void onEntityHandler(object sender, EntityEventArgs a)
        {
	        if (!_options.Silent)
	        {
		        Console.WriteLine(a.Information.StoredName);
	        }
        }
    }
}
