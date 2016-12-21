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

using Akeeba.Unarchiver;
using Akeeba.Unarchiver.Encrypt;
using Akeeba.Unarchiver.EventArgs;
using System;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Akeeba.extractCLI
{
    class extractCLIProgram
    {
        static void Main(string[] args)
        {
            ResourceManager text = Resources.Language.ResourceManager;

            try
            {
                using (Unarchiver.Unarchiver extractor = Unarchiver.Unarchiver.CreateForFile(@"C:\Apache24\htdocs\backups\test.jps", "test"))
                {
                    // Attach event subscribers
                    extractor.ProgressEvent += OnProgressHandler;
                    extractor.EntityEvent += onEntityHandler;

                    CancellationTokenSource cts = new CancellationTokenSource();
                    var token = cts.Token;

                    Task t = Task.Factory.StartNew(
                        () =>
                        {
                            if (extractor != null)
                            {
                                extractor.Scan(token);
                            }
                        }, token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default
                    );

                    t.Wait(token);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(text.GetString("ERR_HEADER"));
                Console.WriteLine(e.Message);
            }
        }

        private static void OnProgressHandler(object sender, ProgressEventArgs e)
        {
            ResourceManager text = Akeeba.extractCLI.Resources.Language.ResourceManager;

            switch (e.Progress.Status)
            {
                case ExtractionStatus.Error:
                    Console.WriteLine(text.GetString("ERR_HEADER"));
                    Console.WriteLine(e.Progress.LastException.Message);
                    Console.WriteLine(e.Progress.LastException.StackTrace);
                    break;

                case ExtractionStatus.Running:
                    Console.WriteLine($"[File position {e.Progress.FilePosition, 0}]");
                    break;

                case ExtractionStatus.Finished:
                    Console.WriteLine(text.GetString("LBL_STATUS_FINISHED"));
                    break;

                case ExtractionStatus.Idle:
                    Console.WriteLine(text.GetString("LBL_STATUS_IDLE"));
                    break;
            }
        }

        private static void onEntityHandler(object sender, EntityEventArgs a)
        {
            Console.WriteLine(a.Information.StoredName);
        }
    }
}
