using System.Windows;
using System.Windows.Input;
using System.Reflection;
using System.ComponentModel;
using System;

namespace unison
{
    public class SystrayViewModel : INotifyPropertyChanged
    {
        public static string GetAppText => "unison v" + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public static ICommand ShowWindowCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                Window AppWindow = Application.Current.MainWindow;
                AppWindow.Show();
                AppWindow.Activate();

                if (AppWindow.WindowState == WindowState.Minimized)
                    AppWindow.WindowState = WindowState.Normal;
            },
            CanExecuteFunc = () => true
        };

        public static ICommand ExitApplicationCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                Application.Current.Shutdown();
            },
            CanExecuteFunc = () => true
        };

        public static string SnapcastText
        {
            get
            {
                SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
                return snapcast.Started ? "Stop Snapcast" : "Start Snapcast";
            }
        }

        public ICommand Snapcast
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => OnPropertyChanged("SnapcastText")));
                        ((MainWindow)Application.Current.MainWindow).Snapcast_Clicked(null, null);
                    },
                    CanExecuteFunc = () => true
                };
            }
        }

        public ICommand Settings
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () =>
                    {
                        ((MainWindow)Application.Current.MainWindow).Settings_Clicked(null, null);
                    },
                    CanExecuteFunc = () => true
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}