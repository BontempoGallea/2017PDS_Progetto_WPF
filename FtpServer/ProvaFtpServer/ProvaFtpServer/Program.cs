using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProvaFtpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            FtpServer server = new FtpServer();

            server.Start();

            while(Console.ReadLine() != "close") { }

            server.Stop();
        }
    }
}
