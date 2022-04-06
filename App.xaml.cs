using System.Globalization;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace unison
{
    public partial class App : Application
    {
        private TaskbarIcon _systray;
        private HotkeyHandler _hotkeys;
        private SnapcastHandler _snapcast;
        private MPDHandler _mpd;

        protected override void OnStartup(StartupEventArgs e)
        {
            //debug language
            //unison.Resources.Resources.Culture = CultureInfo.GetCultureInfo("fr-FR");

            base.OnStartup(e);

            _mpd = new MPDHandler();
            Current.Properties["mpd"] = _mpd;

            _hotkeys = new HotkeyHandler();
            Current.Properties["hotkeys"] = _hotkeys;

            _snapcast = new SnapcastHandler();
            Current.Properties["snapcast"] = _snapcast;

            Current.MainWindow = new MainWindow();

            _systray = (TaskbarIcon)FindResource("SystrayTaskbar");
            Current.Properties["systray"] = _systray;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _systray.Dispose();
            _snapcast.LaunchOrExit(true);
            _hotkeys.RemoveHotkeys();
            base.OnExit(e);
        }
    }
}
