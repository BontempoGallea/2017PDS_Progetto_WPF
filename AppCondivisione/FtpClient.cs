﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace AppCondivisione
{
    class FtpClient
    {
        private WebClient _client;
        private NetworkCredential _credentials;
        private BackgroundWorker _worker;
        public bool Annulla;

        public FtpClient(string username, string password, BackgroundWorker backgroundWorker)
        {
            Annulla = false;
            this._credentials = new NetworkCredential { UserName = username, Password = password };
            this._client = new WebClient
            {
                Proxy = null,
                Credentials = this._credentials
            };
            this._worker = backgroundWorker;

        }

        public void Upload(string pathname, string address)
        {
            string finalPath = pathname;
            string method = WebRequestMethods.Ftp.UploadFile;
            
            if (this.IsDir(pathname))
            {
                method = WebRequestMethods.Ftp.UploadFileWithUniqueName;
                finalPath = MakeZip(pathname);
            }
            
            FileInfo fileInf = new FileInfo(finalPath);
            try
            {
                this.UploadFile(fileInf, address, method);
                if (this.IsDir(pathname))
                {
                    File.Delete(finalPath);
                }
            }catch(Exception ex)
            {
                if (this.IsDir(pathname))
                {
                    File.Delete(finalPath);
                }
                throw new Exception(ex.Message);
            }
        }

        private string MakeZip(string pathname)
        {
            var zipPath = pathname + ".zip";
            zipPath = this.GetUniqueName(zipPath);
            ZipFile.CreateFromDirectory(pathname, zipPath);
            return zipPath;
        }

        private string GetUniqueName(string pathname)
        {
            var _numberAutoSaved = 0;
            var vett2 = pathname.Split('.');
            while (File.Exists(pathname) || Directory.Exists(pathname))
            {
                _numberAutoSaved++;
                pathname = vett2[0] + "(" + _numberAutoSaved + ")" + "." + vett2[1];
            }

            return pathname;
        }

        public void UploadFile(FileInfo fileInf, string address, string method)
        {
            Console.WriteLine(fileInf);
            string uri = "ftp://" + address + "/" + fileInf.Name;
            FtpWebRequest reqFTP;

            // Creo una richiesta FTP a partire dall'uri che ho appena creato
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));

            // Credenziali
            reqFTP.Credentials = this._credentials;
            
            // Per default c'è il keepalive, che fa sì che la connessione di controllo non venga chiusa dopo l'esecuzione
            reqFTP.KeepAlive = false;

            // Specifico il comando, in questo caso UPLOAD
            reqFTP.Method = method;

            // Trasferiamo in binario perché usiamo FTP
            reqFTP.UseBinary = true;

            // Dimensione del file che carichiamo
            reqFTP.ContentLength = fileInf.Length;
            
            // Ho messo 4KB a caso
            int buffLength = 4096;
            byte[] buff = new byte[buffLength];
            int contentLen;
            // Apro lo stream per leggere il file da caricare
            FileStream fs = fileInf.OpenRead();
            Stream strm;
            try
            {
                // Stream lato server
                strm = reqFTP.GetRequestStream();

            }catch(Exception ex)
            {

                fs.Close();
                throw new Exception("Il destinatario ha rifiutato la richiesta di ricezione del file. Connessione chiusa.");
            }
            // Leggo 4KB alla votla
            // Ciclo fino a che non ho finito
            var Uploaded = 0;
            while ((contentLen = fs.Read(buff, 0, buffLength))!=0 && Annulla == false && SharedVariables.CloseEverything== false)
            {
                // Sposta il contenuto dallo stream del file allo stream FTP e voilà
                strm.Write(buff, 0, contentLen);
                var speed=ShowInterfaceSpeedAndQueue()/8;
               
                Uploaded += contentLen;
              
                
                this._worker.ReportProgress((int)((Uploaded * 100) / (fileInf.Length)), fileInf.Length);
                
            }
            // Chiudo tutto
            if (Annulla || SharedVariables.CloseEverything)
            {
                fs.Close();
                reqFTP.Abort();
                _client.Dispose();
                strm.Close();
                throw new Exception("Connessione abortita. Invio del file \"" + fileInf.Name + "\" annullato.");
            }

            strm.Close();
            fs.Close();
        }

        public static long ShowInterfaceSpeedAndQueue()
        {
   
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                IPv4InterfaceStatistics stats = adapter.GetIPv4Statistics();
                if (adapter.Speed > 0)
                {
                    return adapter.Speed;
                }
            }
            return 0;
            
        }

        private bool IsDir(string fileName)
        {
            return (File.GetAttributes(fileName) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        public void Remove(string pathname, string address)
        {
            string finalPath = pathname;
            string method = WebRequestMethods.Ftp.DeleteFile;

            if (this.IsDir(pathname))
            {
                method = WebRequestMethods.Ftp.RemoveDirectory;

            }
            FileInfo fileInf = new FileInfo(finalPath);
            this.RemoveFile(fileInf, address, method);
        }

        public void RemoveFile(FileInfo fileInf, string address, string method)
        {
            Console.WriteLine(fileInf);
            string uri = "ftp://" + address + "/" + fileInf.Name;
            FtpWebRequest reqFTP;

            // Creo una richiesta FTP a partire dall'uri che ho appena creato
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));

            // Credenziali
            reqFTP.Credentials = this._credentials;

            // Per default c'è il keepalive, che fa sì che la connessione di controllo non venga chiusa dopo l'esecuzione
            reqFTP.KeepAlive = false;

            // Specifico il comando, in questo caso UPLOAD
            reqFTP.Method = method;

            // Trasferiamo in binario perché usiamo FTP
            reqFTP.UseBinary = true;
            try{
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                response.Close();
            }catch(Exception e)
            {
                //il server ha chiuso la connessione.. non serve fare niente
                Console.WriteLine(e);

            }
        }
    }

}
