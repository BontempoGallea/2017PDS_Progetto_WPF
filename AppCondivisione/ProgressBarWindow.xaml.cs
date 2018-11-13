using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Shapes;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per ProgressBarWindow.xaml
    /// </summary>
    public partial class ProgressBarWindow : Window
    {
        private string _file;
        private Person _user;
        private Person[] SelectedItems;
        private int NumberOfSelectedItems;
        private int AlreadySent = 1;
        private bool Annulla = false, Continue = false;
        private int Uploaded = 0;
        private FtpClient client;
        private string CurrentReceiver;

        public ProgressBarWindow(string filepath, Person user)
        {
            InitializeComponent();

            this._file = System.IO.Path.GetFileName(filepath);
            this._user = user;
            this.Annulla = false;
            this.Title = "Inviando " + this._file + " a " + this._user.Username;
        }

        public ProgressBarWindow(string filepath, IList selectedItems)
        {
            
            InitializeComponent();
            this.Annulla = false;

            this.NumberOfSelectedItems = selectedItems.Count;
            this.SelectedItems = new Person[this.NumberOfSelectedItems];
            this._file = System.IO.Path.GetFileName(filepath);
            selectedItems.CopyTo(this.SelectedItems, 0);
           
            this.Title = "Inviando " + this._file;

        }

        protected override void OnClosed(EventArgs e)
        {
          
            base.OnClosed(e);
        }

        // Callback che ho detto di chiamare quando il contenuto della finestra ha completato il rendering. E' specificato nello XAML
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork; // Callback per il worker thread
            worker.ProgressChanged += Worker_ProgressChanged; // Callback che userà il worker per riferire il progresso all'UI thread

            Thread.Sleep(2000);

            worker.RunWorkerAsync(); // Lancio come async e in background di modo che possa lo stesso interferire con l'interfaccia grafica senza bloccarla (questo vuol dire che potrò usare il tasto annulla)
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Uploaded = 0;
            
            foreach (Person user in this.SelectedItems)
            {
                if(client != null && this.client.Annulla == true)
                {
                    break;
                }

                this._user = user;
                try
                {
                    this.client = new FtpClient(SharedVariables.Luh.Admin.GetAuthString(), "", (sender as BackgroundWorker));
                    client.Upload(SharedVariables.PathSend, user.GetIp().ToString());
                    while(!Continue) { Thread.Sleep(500); }
                    this.Continue = false;
                    AlreadySent++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Errore connessione", MessageBoxButton.OK, MessageBoxImage.Error);
                    MainWindow.UpdateUsers(SharedVariables.getOnline().Values);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (this.AlreadySent >= this.NumberOfSelectedItems)
                        {
                            this.Close();
                        }
                    }));
                }
            }
            
           
        }

        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            long filesize= e.UserState == null ? 0 : (long) e.UserState;
            double downloadSpeed = FtpClient.ShowInterfaceSpeedAndQueue(); // bytes per second
            long a = (filesize * e.ProgressPercentage ) / 100;
            long b = filesize * (AlreadySent - 1);
            long c = filesize * NumberOfSelectedItems;
            long remainingtosend = c - (b + a);
            long remainingTime = (remainingtosend * 128) / (long) (downloadSpeed/8);

            pbStatus.Value = e.ProgressPercentage; // E' la variabile per accedere a cosa mi è stato passato dal worker. Se avessi mandato ad esempio sempre 2, la progress bar si sarebbe piantata su 2 e basta

            TimeSpan timeLeft = new TimeSpan(0, 0, 0, (int) remainingTime);
            this.AnnullaButton.IsEnabled = true;
            this.Time.Text = "Tempo residuo: " + timeLeft.ToString() +".\nVelocità " + String.Format("{0:0.0}", downloadSpeed/(8*1024*1024)) +" MByte/s.";

            if (pbStatus.Value == 100)
            {
                MessageBox.Show("File inviato correttamente a " + this._user.Username, "Risultato invio file", MessageBoxButton.OK, MessageBoxImage.Information);
                this.AnnullaButton.IsEnabled = false;
                this.Continue = true;
                if (this.AlreadySent >= this.NumberOfSelectedItems)
                {
                    this.Close();
                }
            } // Chiudo quando ho finito
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                this.client.Annulla = true;
                foreach (Person item in this.SelectedItems)
                {
                    var cred = item.Username.Split(' ');
                    var p = new Person();
                    SharedVariables.Luh.Users.TryGetValue(item.GetHash(), out p);
                    if (p.IsOnline())
                    {
                        FtpClient client = new FtpClient(SharedVariables.Luh.Admin.GetAuthString(), "", null);
                        client.Remove(SharedVariables.PathSend, p.GetIp().ToString());
                    }
                    else
                    {
                        MessageBox.Show("Non è possibile annullare l'invio del file a \"" + item.Name + "\", perché non è più online.","Avviso",MessageBoxButton.OK,MessageBoxImage.Warning);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message,"Errore connesione",MessageBoxButton.OK,MessageBoxImage.Error);
            }

            this.Close();
        }
    }
}
