using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace AppCondivisione
{
    class Client
    {
        /*
         * Classe che gestirà le tasks del client
        */
        public void EntryPoint(string user)
        {
            // Ottengo indirizzo ip e porta della persona a cui voglio inviare il file
            var cred = user.Split(' ');
            var p = new Person();
            SharedVariables.Luh.getList().TryGetValue(cred[1] + cred[0], out p);
            if (p.isOnline())
                SendFileTo(p.getIp().ToString(), p.getPort().ToString());
            else
                MessageBox.Show("La persona a cui vuoi inviare non è più online!");
        }

        private static bool IsDir(string fileName)
        {
            return (File.GetAttributes(fileName) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        private static void SendFileTo(string ip, string port)
        {

            try
            {
                var fileName = SharedVariables.PathSend;
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                // Crea un TCP socket.
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Connette il socket all'endpoint remoto
                client.Connect(ipEndPoint);
                // Manda fileName all'host remoto
                if (IsDir(fileName))
                {
                    var richiesta = String.Format(SharedVariables.Luh.getAdmin().getName() + "," + fileName + ",cartella", Environment.NewLine); // Stringa per avvisare chi sono, se lui mi accetta io mando il file
                    SendHeader(richiesta, client);
                    if (String.Compare(ReceiveResponse(client), "ok", StringComparison.Ordinal) == 0)
                    {
                        
                        var zipPath = fileName + ".zip";
                        ZipFile.CreateFromDirectory(fileName, zipPath);
                        SendFileOnNet(client, "", "", zipPath);
                        File.Delete(zipPath);
                        var ansbyte = new byte[1024];
                        client.Receive(ansbyte);
                        SendHeader(SharedVariables.Annulla ? "annulla" : "fine", client);
                    }
                }
                else
                {
                    var richiesta = String.Format(SharedVariables.Luh.getAdmin().getName() + "," + fileName + ",file", Environment.NewLine); // Stringa per avvisare chi sono, se lui mi accetta io mando il file
                    SendHeader(richiesta, client);
                    // Creo prebuffer e postbuffer per scrivere all'inizio e alla fine del file
                    if (String.Compare(ReceiveResponse(client), "ok", StringComparison.Ordinal) == 0)
                    {
                        SendFileOnNet(client, "", "", fileName);
                        var ansbyte = new byte[1024];
                        client.Receive(ansbyte);
                        SendHeader(SharedVariables.Annulla ? "annulla" : "fine", client);
                    }
                }
                client.Shutdown(SocketShutdown.Both);
                client.Close();
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

        private static void SendFileOnNet(Socket client, string pre, string post, string zipPath)
        {
            var string1 = String.Format(pre); // Modifico qua se voglio aggiungere qualcosa prima del file
            var preBuf = Encoding.ASCII.GetBytes(string1);
            var string2 = String.Format(post); // Modifico qua se voglio aggiungere qualcosa dopo il file
            var postBuf = Encoding.ASCII.GetBytes(string2);
            client.SendFile(zipPath, preBuf, postBuf, TransmitFileOptions.UseDefaultWorkerThread);
        }

        private static string ReceiveResponse(Socket client)
        {
            var ansbyte = new byte[1024];
            client.ReceiveBufferSize = 1024;
            client.Receive(ansbyte);
            return Encoding.ASCII.GetString(ansbyte).Replace("\0",String.Empty);
        }

        private static void SendHeader(string richiesta, Socket client)
        {
            var richbyte = Encoding.ASCII.GetBytes(richiesta);
            client.SendBufferSize = richbyte.Length;
            client.ReceiveBufferSize = 1024;
            client.Send(richbyte);
        }
    }
}
