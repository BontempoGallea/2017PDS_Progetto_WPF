using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
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
        public string NewName { get; set; }
        public string Surname { get; set; }
        public string SavePath { get; set; }
        public bool AutomaticSave { get; set; }
        public bool NotAutomaticSave { get; set; }
        public string ImagePath { get; set; }
        public int imagekey { get; set; }

        public Settings()
        {
            InitializeComponent();
            this.DataContext = this;
            this.NewName = SharedVariables.Luh.Admin.Name;
            this.Surname = SharedVariables.Luh.Admin.Surname;
            this.ImagePath = SharedVariables.images[SharedVariables.Luh.Admin.keyimage];
            this.SalvaModifiche.IsEnabled = false;
            this.SavePath = (SharedVariables.PathSave != null) ? SharedVariables.PathSave : null;
            this.AutomaticSave = SharedVariables.AutomaticSave;
            this.NotAutomaticSave = !this.AutomaticSave;
            foreach (String k in SharedVariables.keyimmages.Keys)
            {
                int value;
                SharedVariables.keyimmages.TryGetValue(k,out value);
                if ( value == SharedVariables.Luh.Admin.keyimage)
                {
                    var o = (System.Windows.Controls.RadioButton)this.FindName(k);
                    o.IsChecked = true;
                }
            }

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
            Console.WriteLine("[SETTINGS] Name: " + this.NewName);
            SharedVariables.Luh.Admin.Name = this.NewName;
            SharedVariables.Luh.Admin.Surname = this.Surname;
            SharedVariables.Luh.Admin.keyimage = this.imagekey;

            JsonSerializer jsonSerializer = new JsonSerializer();

            using (StreamWriter file = File.CreateText(System.Windows.Forms.Application.StartupPath + @"/Credentials.json"))
            {
                Credentials credentials = new Credentials()
                {
                    Name = SharedVariables.Luh.Admin.Name,
                    Surname = SharedVariables.Luh.Admin.Surname,
                    State = SharedVariables.Luh.Admin.State,
                    Username = SharedVariables.Luh.Admin.Surname,
                    ImageKey = SharedVariables.Luh.Admin.keyimage,
                    Port = SharedVariables.Luh.Admin.Port,
                    PathSave= this.DestinationPath.Text,
                    AutoSave = Automatic
            };
                jsonSerializer.Serialize(file, credentials);
            }

            this.Close();
            
        }

        private void Annulla_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ChangeImage(object sender, RoutedEventArgs e)
        {
            var b1 = sender as RadioButton;
            string key = b1.Content.ToString();
            var index = SharedVariables.keyimmages[key];
            this.ImagePath = SharedVariables.images[index];
            this.imagekey = index;

            ImageBrush imgBrush = new ImageBrush();
          
            imgBrush.ImageSource =new BitmapImage(new Uri(this.ImagePath));
            this.Ellipse.Fill = imgBrush;
        }
    }
}
