using Akeeba.Unarchiver;
using Akeeba.Unarchiver.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
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
            ResourceManager Text = Akeeba.extractCLI.Resources.Language.ResourceManager;
            Unarchiver.Unarchiver extractor = null;

            try
            {
                extractor = Unarchiver.Unarchiver.CreateForFile(@"C:\Apache24\htdocs\backups\test.jpa");

                // Attach event subscribers
                extractor.ProgressEvent += onProgress;
                extractor.EntityEvent += onEntity;

                CancellationTokenSource cts = new CancellationTokenSource();
                var token = cts.Token;

                Task t = Task.Factory.StartNew(
                    () =>
                    {
                        extractor.Scan(token);
                    }, token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                    );

                t.Wait(token);
            }
            catch (Exception e)
            {
                Console.WriteLine(Text.GetString("ERR_HEADER"));
                Console.WriteLine(e.Message);

                Console.ReadLine();
            }
            finally
            {
                if ((extractor != null) && (extractor is IDisposable))
                {
                    extractor.Dispose();
                }
            }

            Console.ReadLine();
        }

        private static void onProgress(object sender, ProgressEventArgs e)
        {
            ResourceManager Text = Akeeba.extractCLI.Resources.Language.ResourceManager;

            switch (e.Progress.Status)
            {
                case ExtractionStatus.Error:
                    Console.WriteLine(Text.GetString("ERR_HEADER"));
                    Console.WriteLine(e.Progress.LastException.Message);
                    Console.WriteLine(e.Progress.LastException.StackTrace);
                    break;

                case ExtractionStatus.Running:
                    Console.WriteLine(string.Format("[File position {0,0}]", e.Progress.FilePosition));
                    break;

                case ExtractionStatus.Finished:
                    Console.WriteLine(Text.GetString("LBL_STATUS_FINISHED"));
                    break;

                case ExtractionStatus.Idle:
                    Console.WriteLine(Text.GetString("LBL_STATUS_IDLE"));
                    break;
            }
        }

        private static void onEntity(object sender, EntityEventArgs a)
        {
            Console.WriteLine(a.Information.StoredName);
        }
    }
}
