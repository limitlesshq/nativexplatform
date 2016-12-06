using Akeeba.Unarchiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akeeba.extractCLI
{
    class extractCLIProgram
    {
        static void Main(string[] args)
        {
            using (Unarchiver.Unarchiver extractor = Unarchiver.Unarchiver.createFor(@"C:\Apache24\htdocs\backups\test.jpa"))
            {
                // TODO Write some code
            }
        }
    }
}
