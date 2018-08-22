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
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isSelected = false;

        public MainWindow(Dictionary<string, Person>.ValueCollection values)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
            InitializeComponent();
            SharedVariables.W = this;
            this.UserBox.ItemsSource = values;
        }

        public MainWindow()
        {
            this.WindowState = System.Windows.WindowState.Minimized;
            InitializeComponent();
            SharedVariables.W = this;
        }

        private BitmapImage LoadImage(string filename)
        {
            return new BitmapImage(new Uri("pack://application:,,,/" + filename));
        }

        private void Condividi_Click(object sender, RoutedEventArgs e)
        {
            
            foreach (Person item in this.UserBox.SelectedItems)
            {  
                var cred =  item.Username.Split(' ');
                var p = new Person();
                SharedVariables.Luh.Users.TryGetValue(cred[1] + cred[0], out p);
                Console.WriteLine("Stato di " +p.Name + " = " + p.State);
                if (p.IsOnline())
                {
                    ProgressBarWindow pbw = new ProgressBarWindow(SharedVariables.PathSend, p);
                    pbw.Show();
                }
                else
                {
                    MessageBox.Show("La persona a cui vuoi inviare non è più online!");
                }
            } 
        }

        private void Annulla_Click(object sender, RoutedEventArgs e)
        {
            foreach (Person item in this.UserBox.SelectedItems)
            {
                var cred = item.Username.Split(' ');
                var p = new Person();
                SharedVariables.Luh.Users.TryGetValue(cred[1] + cred[0], out p);
                if (p.IsOnline())
                {
                    FtpClient client = new FtpClient(cred[1] + cred[0], "", null);
                    client.Remove(SharedVariables.PathSend, p.GetIp().ToString());
                }
                else
                {
                    MessageBox.Show("La persona a cui vuoi inviare non è più online!");
                }
            }
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }
        

        private void Image_Click(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as Grid;
            foreach(UIElement el in grid.Children)
            {
                if(Grid.GetRow(el) == 1)
                {
                    var label = el as Label;
                    label.Foreground = (isSelected) ? Brushes.Black : Brushes.Blue;
                }
                else
                {
                    var ellipse = el as Ellipse;
                    ellipse.Stroke = (isSelected) ? Brushes.LightGray : Brushes.Blue;
                }
            }

            isSelected = !isSelected;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;

            b.Background = (b.Content.Equals("Stato: Online")) ? Brushes.Gray : Brushes.Blue;
            b.Content = (b.Content.Equals("Stato: Online")) ? "Stato: Offline" : "Stato: Online";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Settings s= new Settings();
            s.Show();

        }

        private void Main_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
           Console.Write(e);
        }

    }
}
