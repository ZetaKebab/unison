using System;
using System.Diagnostics;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

// todo:
//
// * finish correct refresh of mpd
// * show mpd version
// * change volume offset
// * fix window resize
// * show covers

namespace unison
{
    public partial class App : Application
    {
        private TaskbarIcon Systray;
        private HotkeyHandler Hotkeys;
        private SnapcastHandler Snapcast;
        private MPDHandler MPD;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Snapcast = new SnapcastHandler();
            Current.Properties["snapcast"] = Snapcast;

            MPD = new MPDHandler();
            Current.Properties["mpd"] = MPD;

            Hotkeys = new HotkeyHandler();
            Current.Properties["hotkeys"] = Hotkeys;

            Current.MainWindow = new MainWindow();

            Systray = (TaskbarIcon)FindResource("SystrayTaskbar");
            Current.Properties["systray"] = Systray;

            MPD.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Systray.Dispose();
            Snapcast.Stop();
            Hotkeys.RemoveHotKeys();
            base.OnExit(e);
        }
    }
}
