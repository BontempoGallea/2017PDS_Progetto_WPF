using System;
using System.ComponentModel;
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

        public ProgressBarWindow(string filepath, Person user)
        {
            InitializeComponent();

            this._file = System.IO.Path.GetFileName(filepath);
            this._user = user;

            this.Title = "Inviando " + this._file + " a " + this._user.Username;
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
            FtpClient client = new FtpClient(this._user.Username, "", (sender as BackgroundWorker));
            client.Upload(SharedVariables.PathSend, this._user.GetIp().ToString());

            for (int i = 0; i <= 100; i++)
            {
                (sender as BackgroundWorker).ReportProgress(i);
                Thread.Sleep(100); // Sleep perché altrimenti va velocissimo
            }
        }

        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbStatus.Value = e.ProgressPercentage; // E' la variabile per accedere a cosa mi è stato passato dal worker. Se avessi mandato ad esempio sempre 2, la progress bar si sarebbe piantata su 2 e basta
            if(pbStatus.Value == 100)
            {
                MessageBox.Show("File inviato correttamente");
                this.Close();
            } // Chiudo quando ho finito
        }
    }
}
