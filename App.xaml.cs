using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace unison
{
    public partial class App : Application
    {
        private TaskbarIcon Systray;
        private HotkeyHandler Hotkeys;
        private SnapcastHandler Snapcast;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Hotkeys = new HotkeyHandler();
            Current.Properties["hotkeys"] = Hotkeys;

            Snapcast = new SnapcastHandler();
            Current.Properties["snapcast"] = Snapcast;

            Current.MainWindow = new MainWindow();

            Systray = (TaskbarIcon)FindResource("SystrayTaskbar");
            Current.Properties["notify"] = Systray;
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
