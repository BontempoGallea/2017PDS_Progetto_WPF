using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.Win32;

namespace AppCondivisione
{
    class Server
    {
        private const int SenderPort = 16000;
        private static readonly UdpClient ClientUdp = new UdpClient(SenderPort);
        private static Thread _branchUdp;
        private static Thread _branchTcp;
        private static Thread _talkUdp;
        private static Thread _listenerUdp;
        private static SaveFileDialog datifile = new SaveFileDialog();

        public void EntryPoint()
        {
            _branchUdp = new Thread(EntryUdp);
            _branchUdp.Start();
            _branchTcp = new Thread(EntryTcp);
            _branchTcp.Start();
        }

        public void EntryUdp()
        {
            _talkUdp = new Thread(EntryTalk);
            _talkUdp.Start();
            _listenerUdp = new Thread(EntryListen);
            _listenerUdp.Start();
            
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
            Console.WriteLine("Chiudo il branch UDP per il messaggio");
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
                    SharedVariables.Luh.ResetTimer(cred[2] + cred[1]); // Se presente resetto il timer della persona
                    SharedVariables.Luh.Users[cred[2] + cred[1]].setImage(int.Parse(cred[6]));
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
                    SharedVariables.Luh.Users.Remove(cred[2] + cred[1]);
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