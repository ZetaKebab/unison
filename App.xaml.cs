using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using unison.Handlers;

namespace unison
{
    public partial class App : Application
    {
        private TaskbarIcon _systray;
        private HotkeyHandler _hotkeys;
        private SnapcastHandler _snapcast;
        private ShuffleHandler _shuffle;
        private MPDHandler _mpd;
        private UpdateHandler _updater;

        protected override void OnStartup(StartupEventArgs e)
        {
            //debug language
            //unison.Resources.Resources.Culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            //unison.Resources.Resources.Culture = System.Globalization.CultureInfo.GetCultureInfo("es-ES");


            base.OnStartup(e);

            _mpd = new MPDHandler();
            Current.Properties["mpd"] = _mpd;

            _hotkeys = new HotkeyHandler();
            Current.Properties["hotkeys"] = _hotkeys;

            _snapcast = new SnapcastHandler();
            Current.Properties["snapcast"] = _snapcast;

            _shuffle = new ShuffleHandler();
            Current.Properties["shuffle"] = _shuffle;
            
            _updater = new UpdateHandler();
            Current.Properties["updater"] = _updater;

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
