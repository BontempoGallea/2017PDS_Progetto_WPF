using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using RadioButton = System.Windows.Controls.RadioButton;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private bool Automatic;
        public string Name2 { get; set; }
        public string Surname { get; set; }
        public string SavePath { get; set; }
        public bool AutomaticSave { get; set; }
        public bool NotAutomaticSave { get; set; }

        public Settings()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Name2 = SharedVariables.Luh.Admin.Name;
            this.Surname = SharedVariables.Luh.Admin.Surname;

            this.SalvaModifiche.IsEnabled = false;
            this.SavePath = (SharedVariables.PathSave != null) ? SharedVariables.PathSave : null;

            this.AutomaticSave = SharedVariables.AutomaticSave;
            this.NotAutomaticSave = !this.AutomaticSave;
           
        }

        private BitmapImage LoadImage(string filename)
        {
            return new BitmapImage(new Uri("pack://application:,,,/" + filename));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
           
            
            // Creo un thread che andrà ad aprire un form per selezionare la cartella scelta dall'utente

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                DialogResult dr = fbd.ShowDialog();
                if (dr == System.Windows.Forms.DialogResult.OK)
                {
                    this.DestinationPath.Text = fbd.SelectedPath;
                    this.SalvaModifiche.IsEnabled = true;
                }
            }
        }


        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var b1=  sender as RadioButton;
            Automatic = b1.Content.Equals("Si");
            this.SalvaModifiche.IsEnabled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SalvaModifiche_OnClick(object sender, RoutedEventArgs e)
        {
            SharedVariables.AutomaticSave = Automatic;
            SharedVariables.PathSave = this.DestinationPath.Text;
            SharedVariables.Luh.Admin.Name = this.Name2;
            SharedVariables.Luh.Admin.Surname = this.Surname;
           

            this.Close();
            
        }

        private void Annulla_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
