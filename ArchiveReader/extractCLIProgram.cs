using Akeeba.Unarchiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Akeeba.extractCLI
{
    class extractCLIProgram
    {
        static void Main(string[] args)
        {
            Unarchiver.Unarchiver extractor = null;

            try
            {
                extractor = Unarchiver.Unarchiver.createFor(@"C:\Apache24\htdocs\backups\test.jpa");

                CancellationTokenSource cts = new CancellationTokenSource();
                var token = cts.Token;

                Task t = Task.Factory.StartNew(
                    () =>
                    {
                        extractor.scan(token);
                        Console.WriteLine("I am done, I think");
                    }, token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default
                    );

                t.Wait(token);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR:");
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

            Console.WriteLine("All done. Press ENTER");
            Console.ReadLine();
        }
    }
}
