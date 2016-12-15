using Akeeba.Unarchiver;
using Akeeba.Unarchiver.EventArgs;
using System;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace Akeeba.extractCLI
{
    class extractCLIProgram
    {
        static void Main(string[] args)
        {
            ResourceManager text = Resources.Language.ResourceManager;

            using (Unarchiver.Unarchiver extractor = Unarchiver.Unarchiver.CreateForFile(@"C:\Apache24\htdocs\backups\test.zip"))
            {
                try
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
                                extractor.Test(token);
                            }
                        }, token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default
                    );

                    t.Wait(token);
                }
                catch (Exception e)
                {
                    Console.WriteLine(text.GetString("ERR_HEADER"));
                    Console.WriteLine(e.Message);
                }
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
