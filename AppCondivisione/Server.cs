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
            try
            {
                _branchUdp = new Thread(EntryUdp);
                _branchUdp.Start();
                _branchTcp = new Thread(EntryTcp);
                _branchTcp.Start();
            }
            catch (ArgumentException e) { }
            catch (ThreadStateException e) { }
            catch (OutOfMemoryException e) { }
            catch (InvalidOperationException e) { }
        }

        public void EntryUdp()
        {
            Console.WriteLine("Branch UDP entrato!");
            try
            {
                _talkUdp = new Thread(EntryTalk);
                _talkUdp.Start();
                _listenerUdp = new Thread(EntryListen);
                _listenerUdp.Start();
            }
            catch (ArgumentException e) { }
            catch (ThreadStateException e) { }
            catch (OutOfMemoryException e) { }
            catch (InvalidOperationException e) { }
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
                    SharedVariables.Luh.Admin = new Person(ListUserHandler.GetLocalIPAddress().Replace(".","-"), "", true, ListUserHandler.GetLocalIPAddress(), 21);
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
            try
            {
                while (!done && !SharedVariables.CloseEverything)
                {
                    if (ClientUdp.Available <= 0) continue;
                    var bytes = ClientUdp.Receive(ref ipEp); // Buffer
                    var cred = Encoding.ASCII.GetString(bytes, 0, bytes.Length).Split(','); // Converto in stringhe
                    if(!cred[0].Equals("pds")) continue;
                    if (SharedVariables.Luh.IsPresent(cred[2] + cred[1]) && String.Compare(cred[3], "online", StringComparison.Ordinal) == 0)
                    {

                        // Controllo che la persona è gia presente nella lista e lo stato inviatomi sia ONLINE
                        SharedVariables.Luh.ResetTimer(cred[2] + cred[1]); // Se presente resetto il timer della persona
                        done = true; // Ricezione completata
                    }
                    else if(!SharedVariables.Luh.IsPresent(cred[2] + cred[1]) && String.Compare(cred[3], "online", StringComparison.Ordinal) == 0)
                    {
                        Person p = new Person(cred[1], cred[2], cred[3] == "online", cred[4], int.Parse(cred[5])); //creo una nuova persona
                        //TODO: da rimettere...tolto solo per debug
                        //if (p.isEqual(SharedVariables.Luh.getAdmin()) ||
                        //    String.Compare(cred[2], "offline", StringComparison.Ordinal) == 0) continue;
                        SharedVariables.Luh.AddUser(p);//inserisco nella lista delle persone
                        done = true;//ricezione completata
                    }
                    else if (SharedVariables.Luh.IsPresent(cred[2] + cred[1]) && String.Compare(cred[3], "offline", StringComparison.Ordinal) == 0) {
                        SharedVariables.Luh.Users.Remove(cred[2] + cred[1]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /*
         * Sezione del tamo TCP dove si elencano le funzioni usate dal server per ricevere files.
        */
        public void EntryTcp()
        {
            while (SharedVariables.Luh.Admin == null) { }
            FtpServer server = new FtpServer(SharedVariables.Luh.Admin.GetIp(), SharedVariables.Luh.Admin.Port);
            server.Start();
            while (SharedVariables.Luh.Admin.IsOnline() && !SharedVariables.CloseEverything) ;
            server.Stop();
        }
    }
}