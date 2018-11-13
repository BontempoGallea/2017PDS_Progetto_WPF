using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace AppCondivisione
{
    class Server
    {
        private const int SenderPort = 16000;
        private static readonly UdpClient ClientUdp = new UdpClient(SenderPort);
        private static Task _branchUdp;
        private static Task _branchTcp;
        private static Task _talkUdp;
        private static Task _listenerUdp;
        private static SaveFileDialog datifile = new SaveFileDialog();

        public void EntryPoint()
        {
            _branchUdp = Task.Run((() =>EntryUdp())); 
            _branchTcp = Task.Run((() => EntryTcp()));
           
        }

        public void EntryUdp()
        {
            _talkUdp = Task.Run((() => EntryTalk()));
            _listenerUdp = Task.Run((() => EntryListen())); 
        }

        /*
         * Sezione del ramo UDP dove sono elencate le funzioni che il server userà quando dovrà inviare pacchetti 
         * broadcast sulla LAN.
        */
        public void EntryTalk()
        {
            Console.WriteLine("Mando un messsaggio UDP!");
            while (!SharedVariables.CloseEverything)
            {
                if(SharedVariables.Luh.Admin == null)
                {
                    SharedVariables.Luh.Admin = new Person(ListUserHandler.GetLocalIPAddress().Replace(".","-"), "", true, ListUserHandler.GetLocalIPAddress(), 21,0);
                }

                Console.WriteLine(SharedVariables.Luh.Admin.ToString());

                // Mando pacchetti broadcast ogni 5s, SOLO SE sono ONLINE
                if (SharedVariables.Luh.Admin.State)
                {
                    BroadcastMessage("pds," + SharedVariables.Luh.Admin.GetString());
                }
                Thread.Sleep(5000);
            }
            Console.WriteLine("thread di segnalazione UDP chiuso");

        }

        static void BroadcastMessage(string message)
        {
            IPEndPoint ipEp = new IPEndPoint(IPAddress.Broadcast, SenderPort);

            try
            {
                ClientUdp.Send(Encoding.ASCII.GetBytes(message), Encoding.ASCII.GetBytes(message).Length, ipEp);
                Console.WriteLine("Multicast data sent. Message: << " + message + " >>");
            }
            catch (Exception e)
            {
                Console.WriteLine("" + e);
            }
        }

        /*
         * Sezione del ramo UDP che elenca le funzioni usate dal server per agire come receiver di pacchetti
        */
        public void EntryListen()
        {
            while (!SharedVariables.CloseEverything)
                ReceiveBroadcastMessages();
            Console.WriteLine("thread di ascolto UDP chiuso");
        }

        private static void ReceiveBroadcastMessages()
        {
            /*
             * Funzione per ricevere un messaggio in broadcast
            */
            var done = false; // Variabile per terminare la ricezione del pacchetto
            var ipEp = new IPEndPoint(IPAddress.Any, SenderPort); // Endpoint dal quale sto ricevendo dati, accetto qualsiasi indirizzo con la senderPort
            while (!done && !SharedVariables.CloseEverything)
            {
                if (ClientUdp.Available <= 0) continue;
                var bytes = ClientUdp.Receive(ref ipEp); // Buffer
                var cred = Encoding.ASCII.GetString(bytes, 0, bytes.Length).Split(','); // Converto in stringhe
                if(!cred[0].Equals("pds")) continue;

                Person newPerson = new Person(cred[1], cred[2], cred[3] == "online", cred[4], int.Parse(cred[5]), int.Parse(cred[6]));

                if (SharedVariables.Luh.IsPresent(newPerson) && String.Compare(cred[3], "online", StringComparison.Ordinal) == 0)
                {
                    // Controllo che la persona è gia presente nella lista e lo stato inviatomi sia ONLINE
                    SharedVariables.Luh.ResetTimer(newPerson.GetHash()); // Se presente resetto il timer della persona
                    SharedVariables.Luh.Users[newPerson.GetHash()].setImage(int.Parse(cred[6]));
                    done = true; // Ricezione completata
                }
                else if(!SharedVariables.Luh.IsPresent(newPerson) && String.Compare(cred[3], "online", StringComparison.Ordinal) == 0)
                {
                    Console.WriteLine("[****] Aggiunta nuova persona: " + newPerson.GetIp());
                    //TODO: da rimettere...tolto solo per debug
                    /*if (p.IsEqual(SharedVariables.Luh.Admin) ||
                        String.Compare(cred[2], "offline", StringComparison.Ordinal) == 0) continue;*/
                    SharedVariables.Luh.AddUser(newPerson);//inserisco nella lista delle persone
                    done = true; //ricezione completata
                }
                else if (SharedVariables.Luh.IsPresent(newPerson) && String.Compare(cred[3], "offline", StringComparison.Ordinal) == 0) {
                    SharedVariables.Luh.Users.Remove(newPerson.GetHash());
                }
            }
        }

        /*
         * Sezione del tamo TCP dove si elencano le funzioni usate dal server per ricevere files.
        */
        public void EntryTcp()
        {
            while (SharedVariables.Luh.Admin == null) { }
            FtpServer server = new FtpServer(SharedVariables.Luh.Admin.GetIp().MapToIPv4(), SharedVariables.Luh.Admin.Port);
            server.Start();
            while (SharedVariables.Luh.Admin.IsOnline() && !SharedVariables.CloseEverything) ;
            server.Stop();
        }
    }
}