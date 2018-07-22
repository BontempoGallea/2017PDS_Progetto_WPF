using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Compression;
using System.Windows;
using AppCondivisione;
using Microsoft.Win32;

namespace ApplicazioneCondivisione
{
    class Server
    {
        /*
         * Classe che gestirà le tasks del client
        */
        private const int SenderPort = 16000;
        private static readonly UdpClient ClientUdp = new UdpClient(SenderPort);
        private static Thread _branchUdp;
        private static Thread _branchTcp;
        private static Thread _talkUdp;
        private static Thread _listenerUdp;
        private int _numberAutoSaved;
        private static SaveFileDialog datifile = new SaveFileDialog();
        public void EntryPoint()
        {
            try
            {
                _branchUdp = new Thread(EntryUdp);
                _branchUdp.Start();
                _branchTcp = new Thread(EntryTcp);
                _branchTcp.SetApartmentState(ApartmentState.STA);
                _branchTcp.Start();
            }
            catch (ArgumentException e) { }
            catch (ThreadStateException e) { }
            catch (OutOfMemoryException e) { }
            catch (InvalidOperationException e) { }
        }

        public void EntryUdp()
        {
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
            while (!Program.closeEverything)
            {
                while (Program.luh.getAdmin() == null) { }
                // Mando pacchetti broadcast ogni 5s, SOLO SE sono ONLINE
                if (String.Compare(Program.luh.getAdmin().getState(), "online", StringComparison.Ordinal) == 0)//da fare un lock
                    BroadcastMessage(Program.luh.getAdmin().getString());
                Thread.Sleep(5000);
            }
        }

        static void BroadcastMessage(string message)
        {
            IPEndPoint ipEp = new IPEndPoint(IPAddress.Broadcast, SenderPort);

            try
            {
                ClientUdp.Send(Encoding.ASCII.GetBytes(message), Encoding.ASCII.GetBytes(message).Length, ipEp);
                //Console.WriteLine("Multicast data sent.....");
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
            while (!Program.closeEverything)
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
                while (!done && !Program.closeEverything)
                {
                    if (ClientUdp.Available <= 0) continue;
                    var bytes = ClientUdp.Receive(ref ipEp); // Buffer
                    var cred = Encoding.ASCII.GetString(bytes, 0, bytes.Length).Split(','); // Converto in stringhe
                    if (Program.luh.isPresent(cred[1] + cred[0]) && String.Compare(cred[2], "offline", StringComparison.Ordinal) != 0)
                    {
                        // Controllo che la persona è gia presente nella lista e lo stato inviatomi sia ONLINE
                        Program.luh.resetTimer(cred[1] + cred[0]); // Se presente resetto il timer della persona
                        done = true; // Ricezione completata
                    }
                    else // Se non è gia presente
                    {
                        Person p = new Person(cred[0], cred[1], cred[2], cred[3], cred[4]); //creo una nuova persona
                        if (!p.isEqual(Program.luh.getAdmin()) && String.Compare(cred[2], "offline", StringComparison.Ordinal) != 0) //se non è uguale all'amministratore
                        {
                            Program.luh.addUser(p);//inserisco nella lista delle persone
                            done = true;//ricezione completata
                        }
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
            while (Program.luh.getAdmin() == null) { }
            while (Program.luh.getAdmin().isOnline() && !Program.closeEverything)
                ReceiveFile();
        }

        public void ReceiveFile()
        {
            var bufferfile = new byte[1024];

            try
            {
                var listener = new TcpListener(Program.luh.getAdmin().getIp(), Program.luh.getAdmin().getPort());// Imposto tcplistener con le credenziali della persona
                listener.Start(); // Inizio ascolto
                Thread.Sleep(2000);
                while (!Program.closeEverything)
                {
                    if (!listener.Pending()) // Se non c'è nessuno che vuole inviarmi nulla, continuo col prossimo ciclo
                        continue;
                    AcceptRemoteConnection(bufferfile, datifile, listener);
                }
            }
            catch (ArgumentNullException e) { }
            catch (EncoderFallbackException e) { }
            catch (ArgumentException e) { }
            catch (SocketException e) { }
            catch (ObjectDisposedException e) { }
            catch (System.Security.SecurityException e) { }
            catch (FileNotFoundException e) { }
            catch (InvalidOperationException e) { }
            catch (DirectoryNotFoundException e) { }
            catch (PathTooLongException e) { }
            catch (IOException e) { }
            catch (UnauthorizedAccessException e) { }
        }

        private void AcceptRemoteConnection(byte[] bufferfile, SaveFileDialog datifile, TcpListener listener)
        {
            using (var client = listener.AcceptTcpClient()) //accetto tcpclient
            { // Bloccante
              //ricevo pacchetto di informazione
                var buf = new byte[1024];

                client.GetStream().Read(buf, 0, 1024);//leggo in buf 1024 byte dallo stream del client
                var vet = Encoding.ASCII.GetString(buf).Split(',');
                var admin = vet[0];
                var nomeFile = Path.GetFileName(vet[1]);
                var tipo = vet[2];
                //
                byte[] autorizzo;
                if (String.Compare(tipo, "cartella", StringComparison.Ordinal) == 0)
                {

                    autorizzo = !Program.automaticSave ? ShowMessageBox(nomeFile, admin) : Encoding.ASCII.GetBytes("ok");
                    client.GetStream().Write(autorizzo, 0, autorizzo.Length);
                    SetName(nomeFile);

                    // datifile.Filter = " text |*.txt";
                    if (!Program.automaticSave)
                        datifile.ShowDialog();
                    else
                        _numberAutoSaved++;
                    const string temp = "./temp.zip";
                    SendFile(bufferfile, datifile, client, temp);
                   // ZipFile.ExtractToDirectory(temp, datifile.FileName);
                    File.Delete(temp);
                }
                else if (String.Compare(tipo, "file", StringComparison.Ordinal) == 0)
                {
                    autorizzo = !Program.automaticSave ? ShowMessageBox(nomeFile, admin) : Encoding.ASCII.GetBytes("ok");
                    client.GetStream().Write(autorizzo, 0, autorizzo.Length);
                    SetName(nomeFile);
                    // datifile.Filter = " text |*.txt";
                    if (!Program.automaticSave)
                        datifile.ShowDialog();
                    else
                        _numberAutoSaved++;
                    SendFile(bufferfile, datifile, client, datifile.FileName);
                }
                client.Close();
            }
        }

        private void SetName(string nomeFile)
        {
            if (nomeFile != null)
            {
                var vett2 = nomeFile.Split('.');
                _numberAutoSaved = 0;
                datifile.FileName = Program.pathSave + @"\" + nomeFile;
                while (File.Exists(datifile.FileName))
                {
                    _numberAutoSaved++;
                    datifile.FileName = Program.pathSave + @"\" + vett2[0] + "(" + _numberAutoSaved + ")" + "." + vett2[1];
                }
            }
            datifile.InitialDirectory = Program.pathSave;
        }

        private static void SendFile(byte[] bufferfile, SaveFileDialog datifile, TcpClient client, string temp)
        {
            byte[] buf;
            using (var stream = client.GetStream()) // flusso di dati
            using (var output = File.Create(temp)) // file di output
            {
                // Leggo il file a pezzi da 1KB

                int bytesRead;
                while ((bytesRead = stream.Read(bufferfile, 0, 1024)) > 0)
                {
                    output.Write(bufferfile, 0, bytesRead);
                    if (bytesRead < 1024)
                        break;
                }

                buf = Encoding.ASCII.GetBytes("fine?");
                client.GetStream().Write(buf, 0, 2);
                buf = new byte[1024];
                client.GetStream().Read(buf, 0, 1024);
                var risposta = Encoding.ASCII.GetString(buf);
                if (String.Compare(risposta, "annulla", StringComparison.Ordinal) == 0)
                {
                    File.Delete(datifile.FileName);
                }
            }
        }

        private static byte[] ShowMessageBox(string nomeFile, string admin)
        {
            /*
            switch (MessageBox.Show(admin + @"sta tentando di inviarti il file", nomeFile, MessageBoxButtons.YesNo))
            {
                case DialogResult.No:
                    return Encoding.ASCII.GetBytes("no");
                case DialogResult.Yes:
                    return Encoding.ASCII.GetBytes("ok");
            }
            */
            throw new Exception();
        }
        
    }
}