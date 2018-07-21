using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
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

        public MainWindow()
        {
            InitializeComponent();

            this.UserBox.ItemsSource = new UserData[]
            {
                new UserData{Username="Username1", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username2", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username3", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username4", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username5", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username6", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username7", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username8", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username9", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username10", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username11", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username12", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username13", ImageData=this.LoadImage("img.jpg") },
                new UserData{Username="Username14", ImageData=this.LoadImage("img.jpg") }
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
    }
}
