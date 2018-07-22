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
using ApplicazioneCondivisione;
using Microsoft.Win32;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class Program : Application
    {
       
           // public static Client client;
            //public static Server server;
            public static Thread serverThread;
            public static Thread pipeThread;
            public static ListUserHandler luh;
            //public static ApplicazioneCondivisione ac;
        //public static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer(); // Inizializzo timer
        public static bool closeEverything = false; // Questo è il flag al quale i thread fanno riferimento per sapere se devono chiudere tutto o no
            public static RegistryKey key;
            public static bool exists = false; // Flag per vedere se ci sono altre istanze dello stesso progetto
            public static string pathSend = "C:\\Users\\host1\\Documents\\catia.zip"; // Path del file / della cartella da inviare
            public static string pathSave = @"C:\Users\" + Environment.UserName + @"\Downloads"; // Path di default per il salvataggio dei files in arrivo
            public static bool automaticSave = true; // True = non popparmi la finestra di accetazione quando mi arriva un file  
            public static bool AnnullaBoolean = false;
            /// <summary>
            /// Punto di ingresso principale dell'applicazione.
            /// </summary>
            [MTAThread]
            static void Main(string[] args)
            {
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
                    Console.WriteLine("Argomenti arrivati: " + args[0]);
                    Send(args[0]);
                    closeEverything = true;
                }

                if (!closeEverything)
                {
                 //   Application.EnableVisualStyles(); // Questa operazione deve essere fatta prima di inizializzare qualsiasi oggetto
                //    Application.SetCompatibleTextRenderingDefault(false);
                    luh = new ListUserHandler();

                    // Pipe thread per ascoltare
                    pipeThread = new Thread(Listen) { Name = "pipeThread" };
                    pipeThread.Start();

                    // Codice per l'aggiunta dell'opzione al context menu di Windows
                    key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Classes\\*\\Shell\\Condividi in LAN");
                    key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\\Classes\\*\\Shell\\Condividi in LAN\\command");
                    key.SetValue("", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"" + " \"%1\"");

                    // Creo la classe client che verrà fatta girare nel rispettivo thread
                  //  client = new Client();

                    // Creo la classe server che verrà fatta girare nel rispettivo thread
                //    server = new Server();
                //    serverThread = new Thread(server.EntryPoint) { Name = "serverThread" };
                    serverThread.Start();

                    // Avvio l'applicazione
                //    ac = new ApplicazioneCondivisione();
               //     Application.Run(ac);
                }
            }

            public static void Listen()
            {
                try
                {
                    NamedPipeServerStream pipeServer = new NamedPipeServerStream("MyPipe", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    pipeServer.BeginWaitForConnection(new AsyncCallback(AsynWaitCallBack), pipeServer);
                    Console.WriteLine("[Server]: Ho iniziato ad ascoltare...");

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
                        pathSend = result;
                    pipeServer.Close();
                    pipeServer = null;
                    if (!closeEverything)
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
