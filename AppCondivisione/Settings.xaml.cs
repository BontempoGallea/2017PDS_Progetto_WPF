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
        public String MyTitle { get; set; }
        public string NewName { get; set; }
        public string Surname { get; set; }
        public string SavePath { get; set; }
        public bool AutomaticSave { get; set; }
        public bool NotAutomaticSave { get; set; }
        public string ImagePath { get; set; }
        public int ImageKey { get; set; }

        public Settings()
        {
            InitializeComponent();

            this.DataContext = this;

            this.NewName = SharedVariables.Luh.Admin.Name;
            this.Surname = SharedVariables.Luh.Admin.Surname;
            this.ImagePath = SharedVariables.images[SharedVariables.Luh.Admin.KeyImage];
            this.SalvaModifiche.IsEnabled = false;
            this.SavePath = (SharedVariables.PathSave != null) ? SharedVariables.PathSave : null;
            this.AutomaticSave = SharedVariables.AutomaticSave;
            this.NotAutomaticSave = !this.AutomaticSave;
            this.MyTitle = "Preferenze di " + this.NewName + " " + this.Surname;
            this.HighlightCorrectProfilePicture();
        }

        private void HighlightCorrectProfilePicture()
        {
            var o = (System.Windows.Controls.MenuItem)this.FindName(SharedVariables.Luh.Admin.ImageName);
            o.Background = Brushes.PowderBlue;
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
            (sender as System.Windows.Controls.Button).ContextMenu.IsEnabled = true;
            (sender as System.Windows.Controls.Button).ContextMenu.PlacementTarget = (sender as System.Windows.Controls.Button);
            (sender as System.Windows.Controls.Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            (sender as System.Windows.Controls.Button).ContextMenu.IsOpen = true;
        }

        private void SalvaModifiche_OnClick(object sender, RoutedEventArgs e)
        {
            SharedVariables.AutomaticSave = Automatic;
            SharedVariables.PathSave = this.DestinationPath.Text;
            SharedVariables.Luh.Admin.Name = this.NewName;
            SharedVariables.Luh.Admin.Surname = this.Surname;
            SharedVariables.Luh.Admin.KeyImage = this.ImageKey;
            
            JsonSerializer jsonSerializer = new JsonSerializer();

            using (StreamWriter file = File.CreateText(System.Windows.Forms.Application.StartupPath + @"/Credentials.json"))
            {
                Credentials credentials = new Credentials()
                {
                    Name = SharedVariables.Luh.Admin.Name,
                    Surname = SharedVariables.Luh.Admin.Surname,
                    State = SharedVariables.Luh.Admin.State,
                    Username = SharedVariables.Luh.Admin.Surname,
                    ImageKey = SharedVariables.Luh.Admin.KeyImage,
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
            var b1 = sender as System.Windows.Controls.MenuItem;
            string key = b1.Header.ToString();
            var index = SharedVariables.keyimmages[key];
            this.ImagePath = SharedVariables.images[index];
            this.ImageKey = index;

            // Resetto quello di prima
            var item = (System.Windows.Controls.MenuItem) this.FindName(SharedVariables.Luh.Admin.ImageName);
            item.Background = Brushes.Transparent;

            ImageBrush imgBrush = new ImageBrush();
          
            imgBrush.ImageSource =new BitmapImage(new Uri(this.ImagePath));
            this.Ellipse.Fill = imgBrush;
            this.HighlightCorrectProfilePicture();
        }
    }
}
