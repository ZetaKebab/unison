using System.Windows;
using System.Windows.Input;
using System.Reflection;
using System.ComponentModel;
using System;

namespace unison
{
    public class SystrayViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
            CommandAction = () => Application.Current.Shutdown(),
            CanExecuteFunc = () => true
        };

        public string SnapcastText
        {
            get
            {
                SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
                return snapcast.HasStarted ? unison.Resources.Resources.StopSnapcast : unison.Resources.Resources.StartSnapcast;
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

                        SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
                        snapcast.LaunchOrExit();
                    },
                    CanExecuteFunc = () => true
                };
            }
        }

        public ICommand Radios
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () => ((MainWindow)Application.Current.MainWindow).Radios_Clicked(null, null),
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
                    CommandAction = () => ((MainWindow)Application.Current.MainWindow).Settings_Clicked(null, null),
                    CanExecuteFunc = () => true
                };
            }
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}