using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Task taskclient, taskserver, pipeThread;
        public static RegistryKey key;
        public static bool exists = false; // Flag per vedere se ci sono altre istanze dello stesso progetto
        private Server server;
        private static Client client;
       // public Window BWindow;

        public static void ClientEntry(string name)
       {
           client.EntryPoint(name);
       }

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
                System.Windows.Application.Current.Shutdown();
            }

            if (SharedVariables.CloseEverything) return;
            // Pipe thread per ascoltare
            pipeThread = Task.Run(() => Listen());

            // Codice per l'aggiunta dell'opzione al context menu di Windows
            key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Classes\\*\\Shell\\Condividi in LAN");
            key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Classes\\*\\Shell\\Condividi in LAN\\command");
            key.SetValue("", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"" + " \"%1\"");
           
         

           
            // Creo la classe client che verrà fatta girare nel rispettivo thread
            client = new Client();

            // Creo la classe server che verrà fatta girare nel rispettivo thread
            server = new Server();
            taskserver = Task.Run((() => server.EntryPoint()));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
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
                string result = Encoding.ASCII.GetString(buffer).TrimEnd();
                result = result.Replace("\0", String.Empty);
              // result = "C:\\Users\\bitri\\Desktop\\ciao";
                Console.WriteLine("[Server]: Risultato ottenuto: " + result + "\t");
                if (!(result.CompareTo(string.Empty) == 0))
                {
                    SharedVariables.PathSend = result;
                    SharedVariables.W.Dispatcher.Invoke(new Action(() =>
                    {
                        AppCondivisione.MainWindow m2 = new MainWindow(SharedVariables.Luh.getList().Values) {Visibility = Visibility.Visible};
                        m2.Show();
                    }));
                   
                }
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