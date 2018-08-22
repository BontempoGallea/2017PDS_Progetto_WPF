﻿using System;
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
        private Person admin; // Utente sul quale sta girando l'applicazione
        private Dictionary<string, Person> users; // Lista degli utenti attivi dai quali ho ricevuto l'online
        private int lastRefresh; // Lunghezza della lista, l'ultima volta che ho fatto refresh
        private Dictionary<string, Person> selectedUsers = new Dictionary<string, Person>();
       // private MetroFramework.Controls.MetroTile btn; // Bottone per selezionare il tale utente
        //private List<MetroFramework.Controls.MetroTile> listBTN = new List<MetroFramework.Controls.MetroTile>();
        private List<ListViewItem> selectedList = new List<ListViewItem>();

        // Persone di test
        //private Person test1;
        //private Person test2;

        public ListUserHandler()
        {
            /*
             * Costruttore della classe ListUserHandler
            */
            try
            {
                users = new Dictionary<string, Person>(); //creo una dictionary di persone
                lastRefresh = -1;
                //string name = System.DirectoryServices.AccountManagement.UserPrincipal.Current.DisplayName; // Nome dell'utente che ha effettuato l'accesso
                admin = new Person(getLocalIPAddress().Replace(".","-"), "", "online", getLocalIPAddress(), "21"); //imposto admin
            }
            catch (Exception e) { }
            // Persone aggiunte per test
            //test1 = new Person("Mario", "Rossi", "online", getLocalIPAddress(), "5000");
            //test2 = new Person("Luca", "Verdi", "online", getLocalIPAddress(), "1650");
            //addUser(test1);
            //addUser(test2);
        }

        

        public Dictionary<String, Person> getList() { return users; } // Ritorna la lista di persone

        public string getAdminState()
        {
            /*
             * Funzione per ritornare lo stato dell'admin
            */
            return this.admin.getState();
        }

        internal void resetTimer(string v)
        {
            Person a;
            users.TryGetValue(v, out a); // Prova a ottenere la persona alla tale chiave v
            a.reset(); // Fa il reset della persona
        }

        internal bool isPresent(string v)
        {
            return users.ContainsKey(v); // Ritorna un bool per indicare se la lista di persone contiene un valore con la dadta chiave
        }

        private void changeState2_Click(object sender, EventArgs e)
        {
            /*
            //funzione che aggiunge/rimuove dalla lista dei selezionati 
            MetroFramework.Controls.MetroTile changeState = sender as MetroFramework.Controls.MetroTile;
            if (changeState.Style == MetroFramework.MetroColorStyle.Green)
            {
                selectedList.Add(changeState);
                changeState.Style = MetroFramework.MetroColorStyle.Blue;
            }
            else
            {
                selectedList.Remove(changeState);
                changeState.Style = MetroFramework.MetroColorStyle.Green;
            }
            */
        }

        public void condividiButtonClick()
        {
            /*
             * Funzione che gestisce gli eventi di quando si clicca il pulsante per la condivisione
            */
            /*
            
            if (selectedList.Count > 0) // Se lista dei selezionati è > 0
            {
                SendFile sd = new SendFile(); // Apro la finestra della barra di avanzamento
                sd.StartPosition = FormStartPosition.CenterScreen;
                sd.Show();

                foreach (MetroFramework.Controls.MetroTile m in selectedList)
                {
                    Thread clientThread = new Thread(() => Program.client.EntryPoint(m.Name)) { Name = "clientThread" }; // Per ogni bottone selezionato creo un thread
                    clientThread.Start();
                    clientThread.Join();

                    sd.progressBar.Value += (100 / selectedList.Count);
                    sd.progressBar.Text = sd.progressBar.Value.ToString() + "%";
                }
            }
            else
            {
                MessageBox.Show("Non ha selezionato nessun utente!");
            }
           */
        }

        public void addUser(Person p)
        {
            /*
             * Funzione per aggiungere un utente alla lista degli user.
             * Prima di aggiungere, controllo se la tale persona non fosse già stata inserita nella
             * collection degli users.
            */
            if (!users.ContainsKey(p.getSurname() + p.getName()))
            {
                users.Add(p.getSurname() + p.getName(), p);
            }
        }
        public void RemoveUser(Person p)
        {
            /*
             * Funzione per aggiungere un utente alla lista degli user.
             * Prima di aggiungere, controllo se la tale persona non fosse già stata inserita nella
             * collection degli users.
            */
            if (users.ContainsKey(p.getSurname() + p.getName()))
            {
                users.Remove(p.getSurname() + p.getName());
            }
        }

        public Person getAdmin()
        {
            return admin;
        }

        public void changeAdminState(string s)
        {
            /*
             * Funzione per settare lo stato dell'admin
            */
            this.admin.setState(s);
           // Program.ac.state.Text = s;
        }

        public static string getLocalIPAddress()
        {
            /*
             * Funzione per trovare il mio indirizzo IPv4
             */
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("10."))
                {
                    return ip.ToString();
                }
            }
            throw new Exception("indirizzo non trovato");
        }
    }
}