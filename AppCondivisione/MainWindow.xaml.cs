using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        static ListView box;
        private System.Timers.Timer t;

        private void OnTimeElapse(object sender, ElapsedEventArgs e)
        {
            t.Stop();
            if (SharedVariables.W.Visibility == Visibility.Visible)
            {
                SharedVariables.W.Dispatcher.Invoke(new Action(() =>
                {
                    Dictionary<string, Person> values = new Dictionary<string, Person>();
                    foreach (Person pe in SharedVariables.Luh.Users.Values)
                    {
                        if (pe.IsOnline() && !pe.IsOld())
                        {
                            values.Add(pe.GetHash(), pe);
                        }
                    }
                    
                    SharedVariables.W.UserBox.ItemsSource = values.Values;
                    t.Start();
                }));
            }
            
        }

        public void Update(Dictionary<string, Person>.ValueCollection values, Visibility state)
        {
            SharedVariables.W.Title = "Invia " + SharedVariables.PathSend.Split('\\')[SharedVariables.PathSend.Split('\\').Length - 1] + " a...";
            SharedVariables.W.Visibility= state;
            SharedVariables.W.UserBox.ItemsSource = values;
            SharedVariables.W.t = new System.Timers.Timer(2000);
            SharedVariables.W.t.Elapsed += SharedVariables.W.OnTimeElapse;
            SharedVariables.W.t.AutoReset = true;
            SharedVariables.W.SetState();
            SharedVariables.W.t.Start();
        }

        private void SetState()
        {
            Button b = this.State;
            if (SharedVariables.Luh.Admin.State)
            {
                b.Background = Brushes.Green;
                b.Content = "Stato: Online";
                SharedVariables.Luh.Admin.State = true;
                
            }
            else
            {
                b.Background = Brushes.Gray;
                b.Content = "Stato: Offline";
                SharedVariables.Luh.Admin.State = false;
            }

        }

        static public void UpdateUsers(Dictionary<string, Person>.ValueCollection values)
        {
            SharedVariables.W.UserBox.Dispatcher.Invoke(new Action(() =>
            {
                SharedVariables.W.UserBox.ItemsSource = values;
            }));
               

        }

        public MainWindow()
        {
            this.WindowState = System.Windows.WindowState.Normal;
            InitializeComponent();
            SharedVariables.W = this;
            SharedVariables.W.SetState();
        }

        private BitmapImage LoadImage(string filename)
        {
            return new BitmapImage(new Uri("pack://application:,,,/" + filename));
        }
        public void RemovePerson(Person p)
        {
            if (this.UserBox.Items.Contains(p))
            {
                this.UserBox.Items.Remove(p);
            }
        }

        private void Condividi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.UserBox.SelectedItems.Count == 0)
                {
                    throw new Exception("Non hai selezionato nessun destinatario");
                }

                ProgressBarWindow pbw = new ProgressBarWindow(SharedVariables.PathSend, this.UserBox.SelectedItems);
                pbw.Show();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message,"Errore connessione",MessageBoxButton.OK,MessageBoxImage.Error);
            }
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
                    label.Foreground = (isSelected) ? Brushes.Black : Brushes.Green;
                }
                else
                {
                    var ellipse = el as Ellipse;
                    ellipse.Stroke = (isSelected) ? Brushes.LightGray : Brushes.Green;
                }
            }

            isSelected = !isSelected;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b.Content.Equals("Stato: Online"))
            {
                b.Background = Brushes.Gray;
                b.Content = "Stato: Offline";
                SharedVariables.Luh.Admin.State=false;
            }
            else
            {
                b.Background = Brushes.Green;
                b.Content = "Stato: Online";
                SharedVariables.Luh.Admin.State = true;
            }
            
            
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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SharedVariables.W.Visibility = Visibility.Hidden;
            e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
