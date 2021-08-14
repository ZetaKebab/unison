using System.Windows;
using System.Windows.Input;
using System.Reflection;
using System.ComponentModel;
using System;

namespace unison
{
    public class SystrayViewModel : INotifyPropertyChanged
    {
        public string GetAppText => "unison v" + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public ICommand ShowWindowCommand => new DelegateCommand
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

        public ICommand ExitApplicationCommand => new DelegateCommand
        {
            CommandAction = () =>
            {
                Application.Current.Shutdown();
            },
            CanExecuteFunc = () => true
        };

        public string SnapcastText
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
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}