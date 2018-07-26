using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void update()
        {
            this.UserBox.ItemsSource = new[]
            {
                new Person{Username="Username1", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username2", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username3", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username4", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username5", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username6", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username7", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username8", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username9", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username10", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username11", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username12", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username13", ImageData=this.LoadImage("img.jpg") },
                new Person{Username="Username14", ImageData=this.LoadImage("img.jpg") }
            };

        }
        private BitmapImage LoadImage(string filename)
        {
            return new BitmapImage(new Uri("pack://application:,,,/" + filename));
        }

        private void Condividi_Click(object sender, RoutedEventArgs e)
        {
           MessageBox.Show("Ciao");
        }

        private void Annulla_Click(object sender, RoutedEventArgs e)
        {
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
           // update();
            Console.Write(e);
        }
    }
}
