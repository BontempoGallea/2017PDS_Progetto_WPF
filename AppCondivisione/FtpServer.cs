using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace AppCondivisione
{
    class FtpServer
    {
        private TcpListener _listener;

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
            if (this._listener != null)
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