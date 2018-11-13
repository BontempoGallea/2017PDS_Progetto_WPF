using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Task taskserver, pipeThread;
        public static RegistryKey key;
        public static bool exists = false; // Flag per vedere se ci sono altre istanze dello stesso progetto
        private Server server;

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
                Console.WriteLine("[APP] Argomenti arrivati: " + e.Args[0]);
                Send(e.Args[0]);
                SharedVariables.CloseEverything = true;
                Environment.Exit(0);
            }
        
            if (SharedVariables.CloseEverything) return;

            // Carico le credenziali dell'admin
            this.LoadAdminCredentials();

            // Pipe thread per ascoltare
            pipeThread = Task.Run(() => Listen());

            // Codice per l'aggiunta dell'opzione al context menu di Windows
            key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Classes\\*\\Shell\\Condividi in LAN");
            key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Classes\\*\\Shell\\Condividi in LAN\\command");
            key.SetValue("", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"" + " \"%1\"");

            //stessa cosa per directory
            key = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\\Condividi in LAN");
            key = Registry.ClassesRoot.CreateSubKey(@"Directory\\shell\\Condividi in LAN\\command");
            key.SetValue("", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"" + " \"%1\"");

            //stessa cosa per directory
            key = Registry.ClassesRoot.CreateSubKey(@"*\shell\\Condividi in LAN");
            key = Registry.ClassesRoot.CreateSubKey(@"*\\shell\\Condividi in LAN\\command");
            key.SetValue("", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"" + " \"%1\"");

            // Creo la classe server che verrà fatta girare nel rispettivo thread

            server = new Server();
            taskserver = Task.Run((() => server.EntryPoint()));
        }

        private void LoadAdminCredentials()
        {
            try
            {
                using (StreamReader file = File.OpenText(System.Windows.Forms.Application.StartupPath + @"/Credentials.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    Credentials admin = (Credentials)serializer.Deserialize(file, typeof(Credentials));
                    SharedVariables.Luh.Admin = new Person(admin.Name, admin.Surname, admin.State, ListUserHandler.GetLocalIPAddress(), admin.Port, admin.ImageKey, admin.Rnd);
                    SharedVariables.AutomaticSave = admin.AutoSave;
                    
                    SharedVariables.PathSave = (admin.PathSave == null)? SharedVariables.PathSave: admin.PathSave;
                    Console.WriteLine("[APP] Admin: " + SharedVariables.Luh.Admin.Name);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("[APP] File non ancora stato creato!");
            }
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

                while (!SharedVariables.CloseEverything) { Thread.Sleep(1000); }

                if (SharedVariables.CloseEverything)
                {
                    Console.WriteLine("[Server] Chiuso tutto della named pipe.");
                    pipeServer.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void AsynWaitCallBack(IAsyncResult iar)
        {
            if(SharedVariables.CloseEverything)
            {
                Console.WriteLine("[Server] Chiuso tutto della asynwaitcallback.");
                return;
            }
            try
            {
                NamedPipeServerStream pipeServer = (NamedPipeServerStream) iar.AsyncState;
                Console.WriteLine("[Server] Finito di ricevere la risposta, chiudo...");
                pipeServer.EndWaitForConnection(iar);
                byte[] buffer = new byte[255];
                pipeServer.Read(buffer, 0, buffer.Length);
                string result = Encoding.ASCII.GetString(buffer).TrimEnd();
                result = result.Replace("\0", String.Empty);

                Console.WriteLine("[Server]: Risultato ottenuto: " + result + "\t");
                if (!(result.CompareTo(string.Empty) == 0))
                {
                    Dictionary<string, Person> values= new Dictionary<string, Person>();
                    SharedVariables.PathSend = result;
                    SharedVariables.W.Dispatcher.BeginInvoke(new Action(() =>
                    {

                        foreach (Person e in SharedVariables.Luh.Users.Values) {
                            if(e.IsOnline() &&  !e.IsOld())
                            {
                                values.Add(e.GetHash(),e);
                            }
                        }
                        SharedVariables.W.Update(values.Values, Visibility.Visible);
                    }));
                   
                }

                if (!SharedVariables.CloseEverything)
                {
                    pipeServer.Close();
                    pipeServer = null;
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