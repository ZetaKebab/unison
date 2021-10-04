using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace unison
{
    public partial class Settings : Window
    {
        public static string GetVersion => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public static string GetLicense
        {
            get
            {
                try
                {
                    StreamReader Reader = new("LICENSE");
                    string file = "";
                    file += Reader.ReadToEnd();
                    return file;
                }
                catch (IOException e)
                {
                    return e.Message;
                }
            }
        }

        public Settings()
        {
            InitHwnd();
            InitializeComponent();
            DataContext = this;

            WindowState = WindowState.Minimized;

            MpdHost.Text = Properties.Settings.Default.mpd_host;
            MpdPort.Text = Properties.Settings.Default.mpd_port.ToString();
            //MpdPassword.Text = Properties.Settings.Default.mpd_password;
            SnapcastStartup.IsChecked = Properties.Settings.Default.snapcast_startup;
            SnapcastWindow.IsChecked = Properties.Settings.Default.snapcast_window;
            SnapcastPath.Text = Properties.Settings.Default.snapcast_path;
            SnapcastPort.Text = Properties.Settings.Default.snapcast_port.ToString();
            VolumeOffset.Text = Properties.Settings.Default.volume_offset.ToString();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo psi = new(e.Uri.AbsoluteUri);
            psi.UseShellExecute = true;
            Process.Start(psi);
            e.Handled = true;
        }

        public void UpdateConnectionStatus()
        {
            MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
            if (mpd.IsConnected())
                ConnectionStatus.Text = $"{unison.Resources.Resources.Settings_ConnectionStatusConnected} {mpd.GetVersion()}.";
            else
                ConnectionStatus.Text = unison.Resources.Resources.Settings_ConnectionStatusOffline;
        }

        private void MPDConnect_Clicked(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            ConnectionStatus.Text = unison.Resources.Resources.Settings_ConnectionStatusConnecting;
            MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
            mpd.Connect();
        }

        private void SnapcastReset_Clicked(object sender, RoutedEventArgs e)
        {
            SnapcastPath.Text = (string)Application.Current.FindResource("snapcastPath");
            SnapcastPort.Text = (string)Application.Current.FindResource("snapcastPort");
        }

        public void SaveSettings()
        {
            Properties.Settings.Default.mpd_host = MpdHost.Text;
            Properties.Settings.Default.mpd_port = int.Parse(MpdPort.Text, CultureInfo.InvariantCulture);
            //Properties.Settings.Default.mpd_password = MpdPassword.Text;
            Properties.Settings.Default.snapcast_startup = (bool)SnapcastStartup.IsChecked;
            Properties.Settings.Default.snapcast_window = (bool)SnapcastWindow.IsChecked;
            Properties.Settings.Default.snapcast_path = SnapcastPath.Text;
            Properties.Settings.Default.snapcast_port = int.Parse(SnapcastPort.Text, CultureInfo.InvariantCulture);
            Properties.Settings.Default.volume_offset = int.Parse(VolumeOffset.Text, CultureInfo.InvariantCulture);
            Properties.Settings.Default.Save();
        }

        private void ConnectHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                MPDConnect_Clicked(null, null);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            SaveSettings();
            WindowState = WindowState.Minimized;
            Hide();
        }

        public void InitHwnd()
        {
            WindowInteropHelper helper = new(this);
            helper.EnsureHandle();
        }
    }
}