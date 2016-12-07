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
                extractor = Unarchiver.Unarchiver.createFor(@"C:\Apache24\htdocs\backups\test.jpa");

                // Attach event subscribers
                extractor.progressEvent += onProgress;
                extractor.entityEvent += onEntity;

                CancellationTokenSource cts = new CancellationTokenSource();
                var token = cts.Token;

                Task t = Task.Factory.StartNew(
                    () =>
                    {
                        extractor.scan(token);
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

            switch (e.progress.status)
            {
                case extractionStatus.error:
                    Console.WriteLine(Text.GetString("ERR_HEADER"));
                    Console.WriteLine(e.progress.exception.Message);
                    Console.WriteLine(e.progress.exception.StackTrace);
                    break;

                case extractionStatus.running:
                    Console.WriteLine(string.Format("[File position {0,0}]", e.progress.filePosition));
                    break;

                case extractionStatus.finished:
                    Console.WriteLine(Text.GetString("LBL_STATUS_FINISHED"));
                    break;

                case extractionStatus.idle:
                    Console.WriteLine(Text.GetString("LBL_STATUS_IDLE"));
                    break;
            }
        }

        private static void onEntity(object sender, EntityEventArgs a)
        {
            Console.WriteLine(a.information.storedName);
        }
    }
}
