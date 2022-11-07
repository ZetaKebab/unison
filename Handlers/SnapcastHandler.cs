using System;
using System.Diagnostics;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace unison
{
    public class SnapcastHandler
    {
        private readonly Process _snapcast = new();
        public bool HasStarted { get; private set; }

        public void OnConnectionChanged(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.snapcast_startup)
            {
                MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
                if (mpd.IsConnected())
                    LaunchOrExit();
            }
        }

        private void HandleExit(object sender, EventArgs e)
        {
            _snapcast.Kill();
            HasStarted = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateInterface();
            });
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
            });
        }

        public void LaunchOrExit(bool ForceExit = false)
        {
            if (!HasStarted && !ForceExit)
            {
                MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];

                _snapcast.StartInfo.FileName = Properties.Settings.Default.snapcast_path + @"\snapclient.exe";
                _snapcast.StartInfo.Arguments = $"--host {mpd._ipAddress}";
                _snapcast.StartInfo.CreateNoWindow = !Properties.Settings.Default.snapcast_window;
                _snapcast.EnableRaisingEvents = true;
                _snapcast.Exited += new EventHandler(HandleExit);

                try
                {
                    _snapcast.Start();
                }
                catch (Exception err)
                {
                    MessageBox.Show($"[{Resources.Resources.Snapcast_Popup1}]\n" +
                                    $"{Resources.Resources.Snapcast_Popup2} {err.Message}\n\n" +
                                    $"{Resources.Resources.Snapcast_Popup3} {Properties.Settings.Default.snapcast_path}\n" +
                                    $"{Resources.Resources.Snapcast_Popup4}",
                                    "unison", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                HasStarted = true;
            }
            else if (HasStarted)
            {
                _snapcast.Kill();
                HasStarted = false;
            }

            if (!ForceExit)
                UpdateInterface();
        }
    }
}