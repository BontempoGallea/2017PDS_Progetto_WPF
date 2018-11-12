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
        private Array selectedItems;
        private int NumberOfSelectedItems;

        public ProgressBarWindow(string filepath, Person user)
        {
            InitializeComponent();

            this._file = System.IO.Path.GetFileName(filepath);
            this._user = user;

            this.Title = "Inviando " + this._file + " a " + this._user.Username;
        }

        public ProgressBarWindow(string filepath, IList selectedItems)
        {
            
            InitializeComponent();

            this._file = System.IO.Path.GetFileName(filepath);
            selectedItems.CopyTo(this.selectedItems, 0);
            this.NumberOfSelectedItems = selectedItems.Count;
            this.Title = "Inviando " + this._file;

        }

        protected override void OnClosed(EventArgs e)
        {
            SharedVariables.Annulla = false;
            base.OnClosed(e);
        }

        // Callback che ho detto di chiamare quando il contenuto della finestra ha completato il rendering. E' specificato nello XAML
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork; // Callback per il worker thread
            worker.ProgressChanged += Worker_ProgressChanged; // Callback che userà il worker per riferire il progresso all'UI thread
            worker.RunWorkerAsync(); // Lancio come async e in background di modo che possa lo stesso interferire con l'interfaccia grafica senza bloccarla (questo vuol dire che potrò usare il tasto annulla)
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            SharedVariables.numberOfDestination = this.NumberOfSelectedItems;
            SharedVariables.Uploaded = 0;
            try
            {
                foreach (Person user in this.selectedItems)
                {
                    FtpClient client = new FtpClient(SharedVariables.Luh.Admin.GetAuthString(), "", (sender as BackgroundWorker));
                    client.Upload(SharedVariables.PathSend, user.GetIp().ToString());
                    var i = 0;
                }
            }
            catch(Exception ex)
            {

                Console.WriteLine(ex.Message);
                MessageBox.Show(ex.Message);
                MainWindow.UpdateUsers(SharedVariables.getOnline().Values);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.Close();
                }));
                
            }
           
        }

        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            long filesize= e.UserState == null ? 0 : (long) e.UserState;
            double downloadSpeed = FtpClient.ShowInterfaceSpeedAndQueue(); // bytes per second
            long remainingtosend = filesize - SharedVariables.Uploaded;
            long remainingTime = (remainingtosend * 1000) / (long) (downloadSpeed/8);

            pbStatus.Value = e.ProgressPercentage; // E' la variabile per accedere a cosa mi è stato passato dal worker. Se avessi mandato ad esempio sempre 2, la progress bar si sarebbe piantata su 2 e basta

            TimeSpan timeLeft = new TimeSpan(0, 0, 0, (int) remainingTime);

            this.Time.Text = "Tempo residuo: " + timeLeft.ToString() +".\nVelocità " + String.Format("{0:0.0}", downloadSpeed/(8*1024*1024)) +" MByte/s.";

            if (pbStatus.Value == 100)
            {
                MessageBox.Show("File inviato correttamente");
                this.Close();
            } // Chiudo quando ho finito
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                SharedVariables.Annulla = true;
                foreach (Person item in this.selectedItems)
                {
                    var cred = item.Username.Split(' ');
                    var p = new Person();
                    SharedVariables.Luh.Users.TryGetValue(item.GetHash(), out p);
                    if (p.IsOnline())
                    {
                        FtpClient client = new FtpClient(item.GetAuthString(), "", null);
                        client.Remove(SharedVariables.PathSend, p.GetIp().ToString());
                    }
                    else
                    {
                        MessageBox.Show("Non posso annullare l'invio del file per \"" + item.Name + "\" perché non è più online.");
                    }
                }
            }
            catch(Exception ex)
            {

            }

            this.Close();
            SharedVariables.Annulla = false;
        }
    }
}
