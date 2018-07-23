using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RadioButton = System.Windows.Controls.RadioButton;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private bool automatic;
        public Settings()
        {
            InitializeComponent();
            this.SalvaModifiche.IsEnabled = false;
            if (SharedVariables.PathSave != null)
                this.DestinationPath.Text = SharedVariables.PathSave;
            if (SharedVariables.AutomaticSave == true)
                this.Si.IsChecked = true;
            else
            {
                this.No.IsChecked = true;
            }
        }

        private BitmapImage LoadImage(string filename)
        {
            return new BitmapImage(new Uri("pack://application:,,,/" + filename));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void browseButton_Click(object sender, EventArgs e)
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
            automatic = b1.Content.Equals("Si");
            this.SalvaModifiche.IsEnabled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SalvaModifiche_OnClick(object sender, RoutedEventArgs e)
        {
            SharedVariables.AutomaticSave = automatic;
            SharedVariables.PathSave = this.DestinationPath.Text;
            this.Close();
            
        }

        private void Annulla_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
