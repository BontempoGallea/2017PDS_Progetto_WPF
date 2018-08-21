using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FtpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            FtpClient client = new FtpClient();

            client.Upload(@"C: \Users\bitri\Documents\Documenti\reti2\euge", "127.0.0.1");
        }
    }
}
