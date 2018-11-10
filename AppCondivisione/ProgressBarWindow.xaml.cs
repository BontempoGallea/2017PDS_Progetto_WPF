using System;
using System.Collections;
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
        private IList selectedItems;
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
            this.selectedItems = selectedItems;
            this.Title = "Inviando " + this._file;

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
            SharedVariables.numberOfDestination = this.selectedItems.Count;
            SharedVariables.Uploaded = 0;
            try
            {
                foreach (Person user in this.selectedItems)
                {
                    FtpClient client = new FtpClient(SharedVariables.Luh.Admin.Username, "", (sender as BackgroundWorker));
                    client.Upload(SharedVariables.PathSend, user.GetIp().ToString());
                    var i = 0;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Qualcosa è andato storto");
                MessageBox.Show("Qualcosa è andato storto");
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
            long remainingTime = (filesize *1024- SharedVariables.Uploaded*1024) / (long)downloadSpeed;


            pbStatus.Value = e.ProgressPercentage; // E' la variabile per accedere a cosa mi è stato passato dal worker. Se avessi mandato ad esempio sempre 2, la progress bar si sarebbe piantata su 2 e basta
           
            this.Time.Text = "Tempo residuo: " + (remainingTime)+" Secondi. Velocità " +downloadSpeed/(8*1024*1024) +" MByte/s.";
            if (pbStatus.Value == 100)
            {
                MessageBox.Show("File inviato correttamente");
                this.Close();
            } // Chiudo quando ho finito
        }
    }
}
