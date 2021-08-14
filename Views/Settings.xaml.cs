using System.ComponentModel;
using System.Diagnostics;
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
            MpdPassword.Text = Properties.Settings.Default.mpd_password;
            SnapcastStartup.IsChecked = Properties.Settings.Default.snapcast_startup;
            SnapcastPath.Text = Properties.Settings.Default.snapcast_path;
            SnapcastPort.Text = Properties.Settings.Default.snapcast_port.ToString();
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

        private void SnapcastReset_Clicked(object sender, RoutedEventArgs e)
        {
            SnapcastPath.Text = defaultSnapcastPath;
            SnapcastPort.Text = defaultSnapcastPort;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            Properties.Settings.Default.mpd_host = MpdHost.Text;
            Properties.Settings.Default.mpd_port = int.Parse(MpdPort.Text);
            Properties.Settings.Default.mpd_password = MpdPassword.Text;
            Properties.Settings.Default.snapcast_startup = (bool)SnapcastStartup.IsChecked;
            Properties.Settings.Default.snapcast_path = SnapcastPath.Text;
            Properties.Settings.Default.snapcast_port = int.Parse(SnapcastPort.Text);
            Properties.Settings.Default.Save();

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