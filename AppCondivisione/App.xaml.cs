using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using Microsoft.Win32;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Task client, server, pipeThread;
        public static RegistryKey key;
        public static bool exists = false; // Flag per vedere se ci sono altre istanze dello stesso progetto
       

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Controllo che non esista nessuna istanza dello stesso processo
            exists = Process
                    .GetProcessesByName(System.IO
                                        .Path
                                        .GetFileNameWithoutExtension(
                                            System.Reflection.Assembly.GetEntryAssembly().Location)
                                                                        ).Length > 1;

            if (exists)
            {
                MessageBox.Show("C'è già un altro processo che va");
                Console.WriteLine("Argomenti arrivati: " + e.Args[0]);
                Send(e.Args[0]);
                SharedVariables.CloseEverything = true;
            }

            if (!SharedVariables.CloseEverything)
            {


                // Pipe thread per ascoltare
                pipeThread = Task.Run(() => Listen());

                // Codice per l'aggiunta dell'opzione al context menu di Windows
                key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Classes\\*\\Shell\\Condividi in LAN");
                key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Classes\\*\\Shell\\Condividi in LAN\\command");
                key.SetValue("", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"" + " \"%1\"");
            }
        }

        public static void Listen()
        {
            try
            {
                NamedPipeServerStream pipeServer = new NamedPipeServerStream("MyPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                pipeServer.BeginWaitForConnection(new AsyncCallback(AsynWaitCallBack), pipeServer);
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId.ToString() + " [Server]: Ho iniziato ad ascoltare...");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void AsynWaitCallBack(IAsyncResult iar)
        {
            try
            {
                NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;
                Console.WriteLine("[Server] Finito di ricevere la risposta, chiudo...");
                pipeServer.EndWaitForConnection(iar);

                byte[] buffer = new byte[255];
                pipeServer.Read(buffer, 0, buffer.Length);
                string result = Encoding.ASCII.GetString(buffer);
                Console.WriteLine("[Server]: Risultato ottenuto: " + result + "\t");
                if (!(result.CompareTo(string.Empty) == 0))
                    SharedVariables.PathSend = result;
                pipeServer.Close();
                pipeServer = null;
                if (!SharedVariables.CloseEverything)
                {
                    pipeServer = new NamedPipeServerStream("MyPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    pipeServer.BeginWaitForConnection(new AsyncCallback(AsynWaitCallBack), pipeServer);
                    Console.WriteLine("[Server]: Ho iniziato ad ascoltare...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void Send(string path)
        {
            try
            {
                NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "MyPipe", PipeDirection.Out, PipeOptions.Asynchronous);
                pipeClient.Connect(5000);
                Console.WriteLine("[Client] Connesso!");
                byte[] buffer = Encoding.ASCII.GetBytes(path);
                pipeClient.Write(buffer, 0, buffer.Length);
                Console.WriteLine("[Client] Ho mandato questo: " + Encoding.ASCII.GetString(buffer));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void AsynSendCallBack(IAsyncResult iar)
        {
            try
            {
                Console.WriteLine("[Client] Non sono riuscito a connettermi...");
                NamedPipeClientStream pipeClient = (NamedPipeClientStream)iar.AsyncState;
                pipeClient.EndWrite(iar);
                pipeClient.Flush();
                pipeClient.Close();
                pipeClient.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}