using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace AppCondivisione
{
    class ListUserHandler
    {
        /*
         * Questa è la classe che si occupa di gestire la lista degli utenti attivi nella nostra LAN.  
        */
        private Person _Admin; // Utente sul quale sta girando l'applicazione
        private Dictionary<string, Person> _Users; // Lista degli utenti attivi dai quali ho ricevuto l'online
        private Dictionary<string, Person> _SelectedUsers = new Dictionary<string, Person>();
        private List<ListViewItem> _SelectedList = new List<ListViewItem>();

        public ListUserHandler()
        {
            /*
             * Costruttore della classe ListUserHandler
            */
            try
            {
                _Users = new Dictionary<string, Person>(); //creo una dictionary di persone
                _Admin = new Person(GetLocalIPAddress().Replace(".","-"), "", true, GetLocalIPAddress(), 21,0); //imposto admin
            }
            catch (Exception e) { }
        }

        public Person Admin
        {
            get { return this._Admin; }
            set { this._Admin = value; }
        }

        public Dictionary<string, Person> Users
        {
            get { return this._Users; }
        }

        internal void ResetTimer(string v)
        {
            _Users.TryGetValue(v, out Person a); // Prova a ottenere la persona alla tale chiave v
            a.Reset(); // Fa il reset della persona
        }

        internal bool IsPresent(string v)
        {
            return _Users.ContainsKey(v); // Ritorna un bool per indicare se la lista di persone contiene un valore con la dadta chiave
        }

        public void AddUser(Person p)
        {
            /*
             * Funzione per aggiungere un utente alla lista degli user.
             * Prima di aggiungere, controllo se la tale persona non fosse già stata inserita nella
             * collection degli users.
            */
            if (!_Users.ContainsKey(p.Surname + p.Name))
            {
                _Users.Add(p.Surname + p.Name, p);
            }
        }
        public void RemoveUser(Person p)
        {
            /*
             * Funzione per aggiungere un utente alla lista degli user.
             * Prima di aggiungere, controllo se la tale persona non fosse già stata inserita nella
             * collection degli users.
            */
            if (_Users.ContainsKey(p.Surname + p.Name))
            {
                _Users.Remove(p.Surname + p.Name);
            }
        }

        public static string GetLocalIPAddress()
        {
            /*
             * Funzione per trovare il mio indirizzo IPv4
             */
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine(ip.ToString() + " " + ip.ToString().StartsWith("10"));

                    if(ip.ToString().StartsWith("10") == false)
                    {
                        Console.WriteLine("Returning: " + ip.ToString());
                        return ip.ToString();
                    }
                }
            }
            throw new Exception("indirizzo non trovato");
        }
    }
}