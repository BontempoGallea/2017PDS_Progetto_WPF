using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCondivisione
{
    class Credentials
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Username { get; set; }
        public bool State { get; set; }
        public int ImageKey { get; set; }
        public int Port { get; set; }

        public Credentials() { }
    }
}
