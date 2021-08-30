using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;

namespace unison
{
    public class SnapcastHandler
    {
        private readonly Process _snapcast = new();
        public bool Started { get; private set; }

        public SnapcastHandler()
        {
        }

        public void OnConnectionChanged(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.snapcast_startup)
            {
                var mpd = (MPDHandler)Application.Current.Properties["mpd"];
                if (mpd._connected)
                    Start();
            }
        }

        public void UpdateInterface()
        {
            TaskbarIcon Systray = (TaskbarIcon)Application.Current.Properties["systray"];
            SystrayViewModel DataContext = Systray.DataContext as SystrayViewModel;
            DataContext.OnPropertyChanged("SnapcastText");

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow MainWin = (MainWindow)Application.Current.MainWindow;
                MainWin.OnSnapcastChanged();
            }, DispatcherPriority.ContextIdle);
        }

        public void Start()
        {
            if (!Started)
            {
                _snapcast.StartInfo.FileName = Properties.Settings.Default.snapcast_path + @"\snapclient.exe";
                _snapcast.StartInfo.Arguments = $"--host {Properties.Settings.Default.mpd_host}";
                _snapcast.StartInfo.CreateNoWindow = true;
                try
                {
                    _snapcast.Start();
                }
                catch (Exception err)
                {
                    MessageBox.Show($"[Snapcast error]\nInvalid path: {err.Message}\n\nCurrent path: {Properties.Settings.Default.snapcast_path}\nYou can reset it in the settings if needed.",
                                    "unison", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.WriteLine(err.Message);
                    return;
                }
                Started = true;
                UpdateInterface();
            }
            else
            {
                _snapcast.Kill();
                Started = false;
                UpdateInterface();
            }
        }

        public void Stop()
        {
            if (Started)
            {
                _snapcast.Kill();
                Started = false;
                UpdateInterface();
            }
        }
    }
}