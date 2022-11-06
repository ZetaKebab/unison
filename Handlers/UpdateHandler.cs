using System.Windows;
using AutoUpdaterDotNET;

namespace unison.Handlers
{
    internal class UpdateHandler
    {
        readonly string xmlFile = "https://raw.githubusercontent.com/ZetaKebab/unison/main/Installer/unison.xml";

        private bool _UpdateAvailable = false;
        public bool UpdateAvailable() => _UpdateAvailable;

        public UpdateHandler()
        {
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            Start();
        }

        public void Start()
        {
            AutoUpdater.Start(xmlFile);
        }

        private string CutVersionNumber(string number)
        {
            return number.Substring(0, number.LastIndexOf("."));
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.Error == null)
            {
                if (args.IsUpdateAvailable)
                {
                    _UpdateAvailable = true;
                    string number = CutVersionNumber(args.CurrentVersion);

                    MainWindow MainWin = (MainWindow)Application.Current.MainWindow;
                    MainWin.UpdateUpdateStatus(number);

                    MessageBoxResult Result = MessageBox.Show($"{unison.Resources.Resources.Update_Message1} {number}.\n{unison.Resources.Resources.Update_Message2}",
                                                                "unison", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (Result == MessageBoxResult.Yes)
                        AutoUpdater.DownloadUpdate(args);
                }
            }
        }
    }
}
