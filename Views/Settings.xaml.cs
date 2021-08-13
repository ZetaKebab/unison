using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace unison.Views
{
    public partial class Settings : Window
    {
        public string GetVersion => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public Settings()
        {
            InitializeComponent();
            DataContext = this;

            WindowState = WindowState.Minimized;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo psi = new(e.Uri.AbsoluteUri);
            psi.UseShellExecute = true;
            Process.Start(psi);
            e.Handled = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
        }

        public string GetLicense
        {
            get
            {
                try
                {
                    var Reader = new StreamReader("LICENSE");
                    string file = "";
                    file = file + Reader.ReadToEnd();
                    return file;
                }
                catch (IOException e)
                {
                    return e.Message;
                }
            }
        }
    }
}