using System;
using System.Windows;

namespace AppCondivisione
{
    /// <summary>
    /// Logica di interazione per NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        public string Alert { get; set; }

        public NotificationWindow (string alert)
        {
            InitializeComponent();
            DataContext = this;
            this.Alert = alert;

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                // Per posizionare in basso a destra (circa) la finestra
                var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice; // Addattamento per il device
                var corner = transform.Transform(new Point(workingArea.Right, workingArea.Bottom)); // Dove lo vado a piazzare

                this.Left = corner.X - this.ActualWidth - 50;
                this.Top = corner.Y - this.ActualHeight;
            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
