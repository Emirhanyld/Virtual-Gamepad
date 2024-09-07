using System.Windows;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Exceptions;
using Nefarius.ViGEm.Client.Targets.Xbox360.Exceptions;
using System.Diagnostics;

namespace Virtual_Gamepad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViGEmClient? client;
        private IXbox360Controller? xboxController;
        private GamepadWindow? gamepadWindow;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            gamepadWindow?.Close();
            xboxController?.Disconnect();
            client?.Dispose();

            base.OnClosed(e);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new ViGEmClient();
                xboxController = client.CreateXbox360Controller();
                xboxController.AutoSubmitReport = false;
                xboxController.Connect();
            }
            catch (VigemBusNotFoundException)
            {
                MessageBox.Show("ViGEm Bus Driver is not found.\nPlease install the driver.");
            }
            catch (Exception)
            {
                MessageBox.Show("An unexpected error has occurred\nPlease try again or restart the application");
                xboxController?.Disconnect();
                xboxController = null;
                client?.Dispose();
                client = null;
                return;
            }

            if (xboxController == null)
                return;
            gamepadWindow = new GamepadWindow(xboxController);
            gamepadWindow.Show();

            Start.Content = "Stop";
            Start.Click -= Start_Click;
            Start.Click += Stop_Click;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            gamepadWindow?.Close();
            xboxController?.Disconnect();
            client?.Dispose();

            gamepadWindow = null;
            xboxController = null;
            client = null;

            Start.Content = "Start";
            Start.Click -= Stop_Click;
            Start.Click += Start_Click;
        }
    }
}