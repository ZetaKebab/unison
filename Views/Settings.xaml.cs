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
        private string defaultSnapcastPath = "snapclient_0.25.0-1_win64";
        private string defaultSnapcastPort = "1704";

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
            MpdPassword.Text = null; //Properties.Settings.Default.mpd_password;
            SnapcastStartup.IsChecked = Properties.Settings.Default.snapcast_startup;
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
            if (mpd._connected)
            {
                ConnectionStatus.Text = "Connected to MPD " + mpd.GetVersion() + ".";
                ConnectButton.IsEnabled = false;
            }
        }

        private void MPDConnect_Clicked(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
            mpd.Start();
            UpdateConnectionStatus();
        }

        private void SnapcastReset_Clicked(object sender, RoutedEventArgs e)
        {
            SnapcastPath.Text = defaultSnapcastPath;
            SnapcastPort.Text = defaultSnapcastPort;
        }

        public void SaveSettings()
        {
            Properties.Settings.Default.mpd_host = MpdHost.Text;
            Properties.Settings.Default.mpd_port = int.Parse(MpdPort.Text, CultureInfo.InvariantCulture);
            Properties.Settings.Default.mpd_password = null;//MpdPassword.Text;
            Properties.Settings.Default.snapcast_startup = (bool)SnapcastStartup.IsChecked;
            Properties.Settings.Default.snapcast_path = SnapcastPath.Text;
            Properties.Settings.Default.snapcast_port = int.Parse(SnapcastPort.Text, CultureInfo.InvariantCulture);
            Properties.Settings.Default.volume_offset = int.Parse(VolumeOffset.Text, CultureInfo.InvariantCulture);
            Properties.Settings.Default.Save();
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