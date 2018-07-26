using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using System.Collections.Generic;

namespace AppCondivisione
{
    /**
     * Specifiche RFC per il protocollo FTP:
     *      1) The commands begin with a command code followed by an argument field. 
     *      2) The command codes are four or fewer alphabetic characters. 
     *      3) Upper and lower case alphabetic characters are to be treated identically. 
     *      4) The argument field consists of a variable length character string ending with the character sequence <CRLF>.
     *      
     * Section 5.3.1 defines the syntax for all commands.
     * 
     * Section 4.2, FTP Replies, details how the server should respond to each command.
     *      An FTP reply consists of a three digit number ... followed by some text. 
     *      A reply is defined to contain the 3-digit code, followed by Space <SP>, 
     *      followed by one line of text, and terminated by the Telnet end-of-line code.
     *      
     * Esempio:
     *      The server might send 200 Command OK. 
     *      The FTP client will know that the code 200 means success, and doesn't care about the text. 
     *      The text is meant for the human on the other end.
    **/
    class FtpServer
    {
        private TcpListener _listener;

        ILog _log = LogManager.GetLogger(typeof(FtpServer));

        private bool _disposed = false;
        private bool _listening = false;

        private List<ClientConnection> _activeConnections;

        private IPEndPoint _localEndPoint;

        public FtpServer()
            : this(IPAddress.Any, 21)
        {
        }

        public FtpServer(IPAddress ipAddress, int port)
        {
            _localEndPoint = new IPEndPoint(ipAddress, port);
        }

        public void Start()
        {
            this._listener = new TcpListener(_localEndPoint);
            this._listening = true;
            this._listener.Start();
            this._activeConnections = new List<ClientConnection>();
            // Questa è una chiamata NON bloccante. Viene inserita nella coda una callback a HandleAcceptTcpClient la quale verrà chiamata in caso di connessione
            this._listener.BeginAcceptTcpClient(HandleAcceptTcpClient, _listener);
            Console.WriteLine("[FtpServer] Started on TCP port 21");
        }

        public void Stop()
        {
            if(this._listener != null)
            {
                this._listening = false;
                this._listener.Stop();

                this._listener = null;

                Console.WriteLine("[FtpServer] Stopped listening...");
            }
        }

        /**
         * Funzione che viene chiamata quando avviene una connessione
         * @params result = dove verrà salvato il risultato delle chiamata
         **/
        public void HandleAcceptTcpClient(IAsyncResult result)
        {
            if (_listening)
            {
                // Devo rimettermi ad ascoltare per nuove connessioni
                this._listener.BeginAcceptTcpClient(HandleAcceptTcpClient, this._listener);

                TcpClient client = this._listener.EndAcceptTcpClient(result);

                ClientConnection connection = new ClientConnection(client);

                this._activeConnections.Add(connection);

                ThreadPool.QueueUserWorkItem(connection.HandleClient, client);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /**
         * Funzione per rilasciare correttamente tutte le connessioni, di modo da non chiudere il server
         * e lasciare qualcosa in sospeso.
        **/ 
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();

                    foreach (ClientConnection conn in _activeConnections)
                    {
                        conn.Dispose();
                    }
                }
            }

            _disposed = true;
        }
    }
}