using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Diagnostics;
using System.Windows;

namespace unison
{
    public class SnapcastHandler
    {
        private readonly Process _snapcast = new();
        public bool Started { get; private set; }
        private string _snapcastPath;

        public SnapcastHandler()
        {
            // wip: this will have to be moved after the mpd connection, later on
            _snapcastPath = Properties.Settings.Default.snapcast_path;
            if (Properties.Settings.Default.snapcast_startup)
                Start();
        }

        private void UpdateSystray()
        {
            TaskbarIcon Systray = (TaskbarIcon)Application.Current.Properties["systray"];
            SystrayViewModel DataContext = Systray.DataContext as SystrayViewModel;
            DataContext.OnPropertyChanged("SnapcastText");
        }

        public void Start()
        {
            if (!Started)
            {
                _snapcast.StartInfo.FileName = _snapcastPath + @"\snapclient.exe";
                _snapcast.StartInfo.Arguments = $"--host {Properties.Settings.Default.mpd_host}";
                _snapcast.StartInfo.CreateNoWindow = true;
                try
                {
                    _snapcast.Start();
                }
                catch (Exception err)
                {
                    MessageBox.Show($"[Snapcast error]\nInvalid path: {err.Message}\n\nCurrent path: {_snapcastPath}\nYou can reset it in the settings if needed.",
                                    "unison", MessageBoxButton.OK, MessageBoxImage.Error);
                    Trace.WriteLine(err.Message);
                    return;
                }
                Started = true;
                UpdateSystray();
            }
            else
            {
                _snapcast.Kill();
                Started = false;
                UpdateSystray();
            }
        }

        public void Stop()
        {
            if (Started)
            {
                _snapcast.Kill();
                Started = false;
                UpdateSystray();
            }
        }
    }
}