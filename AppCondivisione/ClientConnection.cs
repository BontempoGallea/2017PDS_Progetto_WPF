using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using log4net;

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
        private string _currentDirectory;
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

            this._root = "/";
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
                while(!string.IsNullOrEmpty(line = this._controlReader.ReadLine()))
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
                            
                            // Permette di cambiare directory senza dovere inviare di nuovo le credenziali di accesso
                            case "CWD":
                                response = this.ChangeWorkingDirectory(arguments);
                                break;

                            // Caso speciale di CWD, risali di un livello nel tree
                            case "CDUP":
                                response = this.ChangeWorkingDirectory("..");
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

                            // Specifica la host port da usare per la data connection (trasferimento dati)
                            case "PORT":
                                response = Port(arguments);
                                break;

                            // Importante: dice al server di aprire una port e ascoltare su quella invece di connettersi direttamente al client. 
                            //Utile quando il client è protetto da firewall e non può riceve connessioni entranti 
                            case "PASV": 
                                response = this.Passive();
                                break;

                            // Logout
                            case "QUIT":
                                response = "221 Server closing control connection";
                                break;

                            // Specifica la file structure di quello che si vuole trasferire (ved. sopra)
                            case "STRU":
                                response = Structure(arguments);
                                break;
                                
                            // In che mode si vuole trasferire. Noi usiamo solo la stream, anche se potremmo usare anche la block... Si vedrà
                            case "MODE":
                                response = Mode(arguments);
                                break;

                            // Specifica il path name del file che è da essere rinominato
                            case "RNFR":
                                renameFrom = arguments;
                                response = "350 Requested file action pending further information";
                                break;

                            // Specifica il nuovo nome del file che è stato detto di rinominare nel RNFR
                            case "RNTO":
                                response = Rename(renameFrom, arguments);
                                break;

                            // Cancella il file specificato dal path name passato
                            case "DELE":
                                response = Delete(arguments);
                                break;

                            // Cancella la directory specificata
                            case "RMD":
                                response = RemoveDir(arguments);
                                break;

                            // Crea una directory con un path assoluto o una subdirectory della current working directory
                            case "MKD":
                                response = CreateDir(arguments);
                                break;

                            // Trasferisci una copia del file
                            case "RETR":
                                response = Retrieve(arguments);
                                break;

                            // Salva quello che ti viene inviato e se esiste già sostituiscilo
                            case "STOR":
                                response = Store(arguments);
                                break;

                            // Salva quello che ti viene inviato, ma con un nome che sia unico nella directory indicata
                            case "STOU":
                                response = StoreUnique();
                                break;

                            // Restituisci a chi te 'ha chiesta una lista di file o le informazioni di un file (dipende dal path specificato)
                            case "LIST":
                                response = List(arguments ?? _currentDirectory);
                                break;
                            
                            // Salva dei dati in un file e se quel file esiste già e contiene delle cose, allegacele in fondo
                            case "APPE":
                                response = Append(arguments);
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
                                response = "200 OK";
                                break;
                            case "NLST":
                            case "SITE":
                            case "STAT":
                            case "HELP":
                            case "SMNT":
                            case "REST":
                            case "ABOR":
                            default:
                                response = "502 Command not implemented";
                                break;
                        }
                    }

                    if(this._controlClient == null || !_controlClient.Connected)
                    {
                        break;
                    } else
                    {
                        this._controlWriter.WriteLine(response);
                        this._controlWriter.Flush();

                        if (response.StartsWith("221"))
                        {
                            break;
                        }
                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            Dispose();
        }

        private bool IsPathValid(string path)
        {
            return path.StartsWith(_root);
        }

        private string NormalizeFilename(string path)
        {
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

        private string Options(string arguments)
        {
            return "200 Looks good to me...";
        }

        private string User(string username)
        {
            _username = username;

            return "331 Username ok, need password";
        }

        private string ChangeWorkingDirectory(string pathname)
        {
            if (pathname == "/")
            {
                _currentDirectory = _root;
            }
            else
            {
                string newDir;

                if (pathname.StartsWith("/"))
                {
                    pathname = pathname.Substring(1).Replace('/', '\\');
                    newDir = Path.Combine(_root, pathname);
                }
                else
                {
                    pathname = pathname.Replace('/', '\\');
                    newDir = Path.Combine(_currentDirectory, pathname);
                }

                if (Directory.Exists(newDir))
                {
                    _currentDirectory = new DirectoryInfo(newDir).FullName;

                    if (!IsPathValid(_currentDirectory))
                    {
                        _currentDirectory = _root;
                    }
                }
                else
                {
                    _currentDirectory = _root;
                }
            }

            return "250 Changed to new directory";
        }

        private string Port(string hostPort)
        {
            _dataConnectionType = DataConnectionType.Active;

            string[] ipAndPort = hostPort.Split(',');

            byte[] ipAddress = new byte[4];
            byte[] port = new byte[2];

            for (int i = 0; i < 4; i++)
            {
                ipAddress[i] = Convert.ToByte(ipAndPort[i]);
            }

            for (int i = 4; i < 6; i++)
            {
                port[i - 4] = Convert.ToByte(ipAndPort[i]);
            }

            if (BitConverter.IsLittleEndian)
                Array.Reverse(port);

            _dataEndpoint = new IPEndPoint(new IPAddress(ipAddress), BitConverter.ToInt16(port, 0));

            return "200 Data Connection Established";
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

                return "250 Requested file action okay, completed";
            }

            return "550 File Not Found";
        }

        private string RemoveDir(string pathname)
        {
            pathname = NormalizeFilename(pathname);

            if (pathname != null)
            {
                if (Directory.Exists(pathname))
                {
                    Directory.Delete(pathname);
                }
                else
                {
                    return "550 Directory Not Found";
                }

                return "250 Requested file action okay, completed";
            }

            return "550 Directory Not Found";
        }

        private string CreateDir(string pathname)
        {
            pathname = NormalizeFilename(pathname);

            if (pathname != null)
            {
                if (!Directory.Exists(pathname))
                {
                    Directory.CreateDirectory(pathname);
                }
                else
                {
                    return "550 Directory already exists";
                }

                return "250 Requested file action okay, completed";
            }

            return "550 Directory Not Found";
        }

        private string FileModificationTime(string pathname)
        {
            pathname = NormalizeFilename(pathname);

            if (pathname != null)
            {
                if (File.Exists(pathname))
                {
                    return string.Format("213 {0}", File.GetLastWriteTime(pathname).ToString("yyyyMMddHHmmss.fff"));
                }
            }

            return "550 File Not Found";
        }

        private string FileSize(string pathname)
        {
            pathname = NormalizeFilename(pathname);

            if (pathname != null)
            {
                if (File.Exists(pathname))
                {
                    long length = 0;

                    using (FileStream fs = File.Open(pathname, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        length = fs.Length;
                    }

                    return string.Format("213 {0}", length);
                }
            }

            return "550 File Not Found";
        }

        private string Retrieve(string pathname)
        {
            pathname = NormalizeFilename(pathname);

            if (pathname != null)
            {
                if (File.Exists(pathname))
                {
                    var state = new DataConnectionOperation { Arguments = pathname, Operation = RetrieveOperation };

                    SetupDataConnectionOperation(state);

                    return string.Format("150 Opening {0} mode data transfer for RETR", _dataConnectionType);
                }
            }

            return "550 File Not Found";
        }

        private string Store(string pathname)
        {
            pathname = NormalizeFilename(pathname);

            if (pathname != null)
            {
                var state = new DataConnectionOperation { Arguments = pathname, Operation = StoreOperation };

                SetupDataConnectionOperation(state);

                return string.Format("150 Opening {0} mode data transfer for STOR", _dataConnectionType);
            }

            return "450 Requested file action not taken";
        }

        private string Append(string pathname)
        {
            pathname = NormalizeFilename(pathname);

            if (pathname != null)
            {
                var state = new DataConnectionOperation { Arguments = pathname, Operation = AppendOperation };

                SetupDataConnectionOperation(state);

                return string.Format("150 Opening {0} mode data transfer for APPE", _dataConnectionType);
            }

            return "450 Requested file action not taken";
        }

        private string StoreUnique()
        {
            string pathname = NormalizeFilename(new Guid().ToString());

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

        private string List(string pathname)
        {
            pathname = NormalizeFilename(pathname);

            if (pathname != null)
            {
                var state = new DataConnectionOperation { Arguments = pathname, Operation = ListOperation };

                SetupDataConnectionOperation(state);

                return string.Format("150 Opening {0} mode data transfer for LIST", _dataConnectionType);
            }

            return "450 Requested file action not taken";
        }

        private string Structure(string structure)
        {
            switch (structure)
            {
                case "F":
                    _fileStructureType = FileStructureType.File;
                    break;
                case "R":
                case "P":
                    return string.Format("504 STRU not implemented for \"{0}\"", structure);
                default:
                    return string.Format("501 Parameter {0} not recognized", structure);
            }

            return "200 Command OK";
        }

        private string Mode(string mode) // Ho scritto solo la STREAM MODE, anche se in realtà potrebbe anche esserci la block mode
        {
            if (mode.ToUpperInvariant() == "S")
            {
                return "200 OK";
            }
            else
            {
                return "504 Command not implemented for that parameter";
            }
        }

        private string Rename(string renameFrom, string renameTo)
        {
            if (string.IsNullOrWhiteSpace(renameFrom) || string.IsNullOrWhiteSpace(renameTo))
            {
                return "450 Requested file action not taken";
            }

            renameFrom = NormalizeFilename(renameFrom);
            renameTo = NormalizeFilename(renameTo);

            if (renameFrom != null && renameTo != null)
            {
                if (File.Exists(renameFrom))
                {
                    File.Move(renameFrom, renameTo);
                }
                else if (Directory.Exists(renameFrom))
                {
                    Directory.Move(renameFrom, renameTo);
                }
                else
                {
                    return "450 Requested file action not taken";
                }

                return "250 Requested file action okay, completed";
            }

            return "450 Requested file action not taken";
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

        private string RetrieveOperation(NetworkStream dataStream, string pathname)
        {
            long bytes = 0;

            using (FileStream fs = new FileStream(pathname, FileMode.Open, FileAccess.Read))
            {
                bytes = CopyStream(fs, dataStream);
            }

            return "226 Closing data connection, file transfer successful";
        }

        private string StoreOperation(NetworkStream dataStream, string pathname)
        {
            long bytes = 0;

            using (FileStream fs = new FileStream(pathname, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                bytes = CopyStream(dataStream, fs);
            }

            return "226 Closing data connection, file transfer successful";
        }

        private string AppendOperation(NetworkStream dataStream, string pathname)
        {
            long bytes = 0;

            using (FileStream fs = new FileStream(pathname, FileMode.Append, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan))
            {
                bytes = CopyStream(dataStream, fs);
            }

            return "226 Closing data connection, file transfer successful";
        }

        private string ListOperation(NetworkStream dataStream, string pathname)
        {
            StreamWriter dataWriter = new StreamWriter(dataStream, Encoding.ASCII);

            IEnumerable<string> directories = Directory.EnumerateDirectories(pathname);

            foreach (string dir in directories)
            {
                DirectoryInfo d = new DirectoryInfo(dir);

                string date = d.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180) ?
                    d.LastWriteTime.ToString("MMM dd  yyyy") :
                    d.LastWriteTime.ToString("MMM dd HH:mm");

                string line = string.Format("drwxr-xr-x    2 2003     2003     {0,8} {1} {2}", "4096", date, d.Name);

                dataWriter.WriteLine(line);
                dataWriter.Flush();
            }

            IEnumerable<string> files = Directory.EnumerateFiles(pathname);

            foreach (string file in files)
            {
                FileInfo f = new FileInfo(file);

                string date = f.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180) ?
                    f.LastWriteTime.ToString("MMM dd  yyyy") :
                    f.LastWriteTime.ToString("MMM dd HH:mm");

                string line = string.Format("-rw-r--r--    2 2003     2003     {0,8} {1} {2}", f.Length, date, f.Name);

                dataWriter.WriteLine(line);
                dataWriter.Flush();
            }

            return "226 Transfer complete";
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
