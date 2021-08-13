using System.Windows;
using System.Windows.Input;
using System.Reflection;
using System.ComponentModel;
using System;
using System.Windows.Threading;

namespace unison
{
    public class NotifyIconViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer timer;

        public NotifyIconViewModel()
        {
            //timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTimerTick, Application.Current.Dispatcher);
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            //fire a property change event for the timestamp
            //Application.Current.Dispatcher.BeginInvoke(new Action(() => OnPropertyChanged("SnapcastText")));
        }

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
                //Application.Current.Dispatcher.BeginInvoke(new Action(() => OnPropertyChanged("SnapcastText")));
                SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
                return snapcast.Started ? "Stop Snapcast" : "Start Snapcast";
            }
        }

        public ICommand Snapcast
        {
            get
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => OnPropertyChanged("SnapcastText")));
                NotifyPropertyChanged("SnapcastText");
                return new DelegateCommand
                {
                    CommandAction = () => ((MainWindow)Application.Current.MainWindow).Snapcast_Clicked(null, null),
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

        public string GetSnapcastText
        {
            get
            {
                /*if (Application.Current.MainWindow != null)
                {
                    SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
                    return snapcast.Started ? "Stop Snapcast" : "Start Snapcast";
                }
                return "not initialized";*/
                return SnapcastText;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
