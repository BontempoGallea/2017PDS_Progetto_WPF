using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace AppCondivisione
{
    /**
     * L'interfaccia IDisposable mi offre un meccanismo per rilasciare correttamente, senza che io me ne preoccupi,
     * le risorse non gestite da GC.
     * 
     * Non ho copiato dalla documentazione tutti i codici di risposta perché era una rottura di cazzo, sono comunque simili a 
     * quelli dell'HTTP. Ho allegato il foglio dell'RFC nel progetto.
     * 
     * Non ho messo la parte per l'autenticazione con i certificati X.509 con il server perché non mi sembrava il caso.
     * 
     * Gran parte del codice l'hho ottenuto da un tutorial, che ho snellito lasciando i comandi che mi sembravano più intelligenti.
     * Questo server FTP è testabile anche con FileZilla, basta connettersi a 127.0.0.1 alla porta 21 con qualsiasi credenziali.
    **/
    class ClientConnection : IDisposable
    {
        private class DataConnectionOperation
        {
            public Func<NetworkStream, string, string> Operation { get; set; }
            public string Arguments { get; set; }
        }

        #region Copy Stream Implementations

        private static long CopyStream(Stream input, Stream output, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int count = 0;
            long total = 0;

            while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, count);
                total += count;
            }

            return total;
        }

        private static long CopyStreamAscii(Stream input, Stream output, int bufferSize)
        {
            char[] buffer = new char[bufferSize];
            int count = 0;
            long total = 0;

            using (StreamReader rdr = new StreamReader(input, Encoding.ASCII))
            {
                using (StreamWriter wtr = new StreamWriter(output, Encoding.ASCII))
                {
                    while ((count = rdr.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        wtr.Write(buffer, 0, count);
                        total += count;
                    }
                }
            }

            return total;
        }

        private long CopyStream(Stream input, Stream output)
        {
            Stream limitedStream = output; // new RateLimitingStream(output, 131072, 0.5);

            if (_connectionType == TransferType.Image)
            {
                return CopyStream(input, limitedStream, 4096);
            }
            else
            {
                return CopyStreamAscii(input, limitedStream, 4096);
            }
        }

        #endregion

        #region Enums

        private enum TransferType
        {
            Ascii, // Default, utilizzato principalmente per il trasferimento di file di testo
            Ebcdic, // Pensato per chi lo usa anche come rappresentazione Host, non ci interessa penso
            Image, // I dati sono inviato come una serie continua di bits raggruppati 8 a 8. E' inteso per un trasferimento più efficiente di file e dati binari
            Local, // Bisogna specificare come secondo parametro la size del pezzetto di dati che si può trasferire per volta
        }

        private enum FormatControlType // Secondo parametro per ASCII e EBCDIC
        {
            NonPrint, // Default da usare in caso non sia specificato il secondo parametro dopo il TYPE
            Telnet, // In pratica CRLF indica la fine di una linea qua
            CarriageControl, // Casino, per capire la struttura del file vengono usati caratteri di controllo di FORTRAN
        }

        private enum DataConnectionType
        {
            Passive,
            Active,
        }

        private enum FileStructureType
        {
            File, // Default. Non c'è una struttura precisa, il file è considerato una sequenza di byte
            Record, // Il file è una sequenza di record
            Page, // Il file è fatto di pagine indipendenti indicizzate
        }

        #endregion

        private bool _disposed = false;

        private string TAG;

        private TcpListener _passiveListener;

        private TcpClient _controlClient; // Control connection = connessione con il client per lo scambio di comandi
        private TcpClient _dataClient; // Data connection = connessione full duplex (in entrambi i sensi) che ha una specifica Mode e Type

        private NetworkStream _controlStream;
        private StreamReader _controlReader;
        private StreamWriter _controlWriter;

        private TransferType _connectionType = TransferType.Ascii;
        private FormatControlType _formatControlType = FormatControlType.NonPrint;
        private DataConnectionType _dataConnectionType = DataConnectionType.Active;
        private FileStructureType _fileStructureType = FileStructureType.File;

        private string _username;
        private string _root;
        private string _currentDirectory= SharedVariables.PathSave;
        private bool _isDirectoryFlag = false;
        private IPEndPoint _dataEndpoint;
        private IPEndPoint _remoteEndPoint;

        private string _clientIP;

        private List<string> _validCommands;

        /**
         * Inizializzazioni varie a partire da un client che ha richiesto una connessione
         * al nostro FTP server
        **/
        public ClientConnection(TcpClient client)
        {
            this._controlClient = client;

            this._controlStream = this._controlClient.GetStream();

            this._controlReader = new StreamReader(this._controlStream);
            this._controlWriter = new StreamWriter(this._controlStream);

            IPAddress localAddress = ((IPEndPoint)this._controlClient.Client.LocalEndPoint).Address;

            this._passiveListener = new TcpListener(localAddress, 0);
            this._passiveListener.Start();

            this._validCommands = new List<string>();

            this._root =SharedVariables.PathSave;
            this._currentDirectory = this._root;
        }

        /**
         * Qui accadono le vere magie, è la funzione che si preoccupa di capire qual è il comando
         * inviato dal client e agire di conseguenza.
        **/
        public void HandleClient(object obj)
        {
            this._remoteEndPoint = (IPEndPoint)_controlClient.Client.RemoteEndPoint;

            this._clientIP = _remoteEndPoint.Address.ToString();

            this._controlStream = _controlClient.GetStream();

            this._controlReader = new StreamReader(_controlStream);
            this._controlWriter = new StreamWriter(_controlStream);

            this._controlWriter.WriteLine("220 Ready!");
            this._controlWriter.Flush();
            this._validCommands.AddRange(new string[] { "AUTH", "USER", "PASS", "QUIT", "HELP", "NOOP" });
            string line;

            this._dataClient = new TcpClient();

            string renameFrom = null;

            try
            {
                while (!string.IsNullOrEmpty(line = this._controlReader.ReadLine()))
                {
                    string response = null;

                    string[] command = line.Split(' ');
                    string cmd = command[0].ToUpperInvariant();
                    string arguments = command.Length > 1 ? line.Substring(command[0].Length + 1) : null;

                    if (string.IsNullOrWhiteSpace(arguments))
                        arguments = null;

                    if (cmd != "RNTO")
                    {
                        renameFrom = null;
                    }

                    if (response == null)
                    {
                        //Console.WriteLine("[INIT] Comando ricevuto = " + cmd + " --- Arguments = " + arguments);
                        switch (cmd)
                        {
                            // Riceve l'identità dello user, necessaria per accedere al suo relativo file system
                            case "USER":
                                response = this.User(arguments);
                                break;

                            // Non poteva mancare ovviamente la password tra le credenziali
                            case "PASS":
                                response = this.Password(arguments);
                                break;

                            // Printa su quale directory sto attualmente lavorando
                            case "PWD":
                                response = PrintWorkingDirectory();
                                break;

                            // Specifica il representation type che verrà utilizzato per il trasferimento
                            case "TYPE":
                                string[] splitArgs = arguments.Split(' ');
                                response = this.Type(splitArgs[0], splitArgs.Length > 1 ? splitArgs[1] : null);
                                break;

                            // Importante: dice al server di aprire una port e ascoltare su quella invece di connettersi direttamente al client. 
                            //Utile quando il client è protetto da firewall e non può riceve connessioni entranti 
                            case "PASV":
                                response = this.Passive();
                                break;
                            case "RMD":
                                response = RemoveDir(arguments);
                                break;
                            // Logout
                            case "QUIT":
                                response = "221 Server closing control connection";
                                break;

                            // Salva quello che ti viene inviato e se esiste già sostituiscilo
                            case "STOR":
                                response = Store(arguments, false);
                                break;

                            case "STOU":
                                response = StoreUniqueReal();
                                break;

                            case "CWD":
                                response = ChangeWorkingDirectory(arguments);
                                break;

                            case "DELE":
                                response = Delete(arguments);
                                break;
                            // Reinizializza tutte le credenziali
                            case "REIN":
                                _username = null;
                                _passiveListener = null;
                                _dataClient = null;
                                response = "220 Service ready for new user";
                                break;

                            case "SYST":
                            case "NOOP":
                            case "ACCT":
                            case "ALLO":
                            case "OPTS":
                                response = "200 OK";
                                break;
                            case "NLST":
                            case "SITE":
                            case "STAT":
                            case "HELP":
                            case "SMNT":
                            case "REST":
                            case "ABOR":
                            case "APPE":
                            case "LIST":
                            case "MKD":

                            case "RETR":
                            case "RNTO":
                            case "RNFR":
                            case "MODE":
                            case "STRU":
                            case "PORT":
                            case "CDUP":
                            default:
                                response = "502 Command not implemented";
                                break;
                        }
                    }

                    if (this._controlClient == null || !_controlClient.Connected)
                    {
                        break;
                    }
                    else
                    {
                        this._controlWriter.WriteLine(response);
                        this._controlWriter.Flush();

                        if (response.StartsWith("221"))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                
            }

            Dispose();
        }

        private string RemoveDir(string pathname)
        {
            pathname = NormalizeFilename(pathname);
            if (pathname != null)
            {
                if (Directory.Exists(pathname))
                {
                    Directory.Delete(pathname, true);
                }
                else
                {
                    return "550 Directory Not Found";
                }
                return "250 requested directory action okay, completed";
            }
            else
            {
                return "550 Directory Not Found";
            }
        }

        private string Delete(string pathname)
        {
            pathname = NormalizeFilename(pathname);
            if (pathname != null)
            {
                if (File.Exists(pathname))
                {
                    File.Delete(pathname);
                }
                else
                {
                    return "550 File Not Found";
                }
                return "250 requested file action okay, completed";
            }
            else
            {
                return "550 File Not Found";
            }
        }

        private bool IsPathValid(string path)
        {
            return path.StartsWith(_root);
        }

        private string NormalizeFilename(string path)
        {
            Console.WriteLine(SharedVariables.PathSave);
            Console.WriteLine(TAG + "NORMALIZE --> " + path);
            if (path == null)
            {
                path = string.Empty;
            }

            if (path == "/")
            {
                return _root;
            }
            else if (path.StartsWith("/"))
            {
                path = new FileInfo(Path.Combine(_root, path.Substring(1))).FullName;
            }
            else
            {
                path = new FileInfo(Path.Combine(_currentDirectory, path)).FullName;
            }

            return IsPathValid(path) ? path : null;
        }

        #region FTP Commands


        private string User(string username)
        {
            _username = username;

            TAG = "[FtpServer@" + _username + "] ";

            string _userDirectory = NormalizeFilename(username);

            Console.WriteLine(TAG + "NEW USER ---> " + _userDirectory);

            if (!Directory.Exists(_userDirectory))
            {
                Directory.CreateDirectory(_userDirectory);
            }

            this._currentDirectory = _userDirectory;
            this._root = this._currentDirectory;

            return "331 Username ok, need password";
        }

        private string ChangeWorkingDirectory(string pathname)
        {
            this._currentDirectory = "/" + pathname;

            this._isDirectoryFlag = true;

            return "250 Changed to new directory";
        }



        private string Passive()
        {
            _dataConnectionType = DataConnectionType.Passive;

            IPAddress localIp = ((IPEndPoint)_controlClient.Client.LocalEndPoint).Address;

            _passiveListener = new TcpListener(localIp, 0);
            _passiveListener.Start();

            IPEndPoint passiveListenerEndpoint = (IPEndPoint)_passiveListener.LocalEndpoint;

            byte[] address = passiveListenerEndpoint.Address.GetAddressBytes();
            short port = (short)passiveListenerEndpoint.Port;

            byte[] portArray = BitConverter.GetBytes(port);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(portArray);

            return string.Format("227 Entering Passive Mode ({0},{1},{2},{3},{4},{5})", address[0], address[1], address[2], address[3], portArray[0], portArray[1]);
        }

        private string Type(string typeCode, string formatControl)
        {
            switch (typeCode.ToUpperInvariant())
            {
                case "A":
                    _connectionType = TransferType.Ascii;
                    break;
                case "I":
                    _connectionType = TransferType.Image;
                    break;
                default:
                    return "504 Command not implemented for that parameter";
            }

            if (!string.IsNullOrWhiteSpace(formatControl))
            {
                switch (formatControl.ToUpperInvariant())
                {
                    case "N":
                        _formatControlType = FormatControlType.NonPrint;
                        break;
                    default:
                        return "504 Command not implemented for that parameter";
                }
            }

            return string.Format("200 Type set to {0}", _connectionType);
        }


        private string Store(string pathname, bool isDirectory)
        {
            Console.WriteLine(TAG + "STORE --> " + pathname);

            pathname = NormalizeFilename(pathname);

            Console.WriteLine(TAG + "STORE --> " + pathname);

            if (pathname != null)
            {
                this.StoreUnique(pathname, isDirectory);

                return string.Format("150 Opening {0} mode data transfer for STOR", _dataConnectionType);
            }

            return "450 Requested file action not taken";
        }

        private string StoreUniqueReal()
        {
            string pathname = NormalizeFilename(_currentDirectory);

            Console.WriteLine(TAG + "STORE UNIQUE REAL ---> " + pathname);



            var state = new DataConnectionOperation { Arguments = pathname, Operation = StoreOperation };

            SetupDataConnectionOperation(state);

            return string.Format("150 Opening {0} mode data transfer for STOU", _dataConnectionType);
        }

        private string StoreUnique(string pathname, bool isDirectory)
        {



            var state = new DataConnectionOperation { Arguments = pathname, Operation = StoreOperation };

            SetupDataConnectionOperation(state);



            return string.Format("150 Opening {0} mode data transfer for STOU", _dataConnectionType);
        }

        private string Password(string password)
        {
            return "230 User logged in";
        }

        private string PrintWorkingDirectory()
        {
            string current = _currentDirectory.Replace(_root, string.Empty).Replace('\\', '/');

            if (current.Length == 0)
            {
                current = "/";
            }

            return string.Format("257 \"{0}\" is current directory.", current); ;
        }

        #endregion

        #region DataConnection Operations

        private void HandleAsyncResult(IAsyncResult result)
        {
            if (_dataConnectionType == DataConnectionType.Active)
            {
                _dataClient.EndConnect(result);
            }
            else
            {
                _dataClient = _passiveListener.EndAcceptTcpClient(result);
            }
        }

        private void SetupDataConnectionOperation(DataConnectionOperation state)
        {
            System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Vuoi ricevere il file?", "Vuoi ricevere il file?", System.Windows.Forms.MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                    if (_dataConnectionType == DataConnectionType.Active)
                    {
                        _dataClient = new TcpClient(_dataEndpoint.AddressFamily);
                        _dataClient.BeginConnect(_dataEndpoint.Address, _dataEndpoint.Port, DoDataConnectionOperation, state);
                    }
                    else
                    {
                        _passiveListener.BeginAcceptTcpClient(DoDataConnectionOperation, state);
                    }
            }
            else if (dialogResult == DialogResult.No)
            {
                //dataStream.Close();
            }

        }

        private void DoDataConnectionOperation(IAsyncResult result)
        {
            HandleAsyncResult(result);

            DataConnectionOperation op = result.AsyncState as DataConnectionOperation;

            string response;

            using (NetworkStream dataStream = _dataClient.GetStream())
            {
                response = op.Operation(dataStream, op.Arguments);
            }

            _dataClient.Close();
            _dataClient = null;

            _controlWriter.WriteLine(response);
            _controlWriter.Flush();
        }



        private string StoreOperation(NetworkStream dataStream, string pathname)
        {
            long bytes = 0;
            int _numberAutoSaved = 0;
            string folder = pathname.Split('.')[0];
            string fileName = pathname;

            var vett2 = pathname.Split('.');
            while (File.Exists(fileName) || Directory.Exists(folder.Split('.')[0]))
            {
                _numberAutoSaved++;
                var extension = (this._isDirectoryFlag) ? "" : "." + vett2[1];
                folder = vett2[0] + "(" + _numberAutoSaved + ")" + extension;
                if (!_isDirectoryFlag) { fileName = vett2[0] + "(" + _numberAutoSaved + ")" + extension; }
            }

            Console.WriteLine(TAG + "STORE OPERATION ASYNC ---> " + pathname + "   " + folder);
           
                using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
                {
                    bytes = CopyStream(dataStream, fs);
                }

                if (_isDirectoryFlag && File.Exists(fileName))
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(pathname, folder);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        dataStream.Close();

                    }
                    File.Delete(pathname);
                }
           
               

            return "226 Closing data connection, file transfer successful";
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_controlClient != null)
                    {
                        _controlClient.Close();
                    }

                    if (_dataClient != null)
                    {
                        _dataClient.Close();
                    }

                    if (_controlStream != null)
                    {
                        _controlStream.Close();
                    }

                    if (_controlReader != null)
                    {
                        _controlReader.Close();
                    }

                    if (_controlWriter != null)
                    {
                        _controlWriter.Close();
                    }
                }
            }

            _disposed = true;
        }

        #endregion
    }
}
