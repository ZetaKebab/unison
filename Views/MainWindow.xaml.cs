using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace unison
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public readonly Settings SettingsWindow = new Settings();

        private readonly MPDHandler mpd;

        DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitHwnd();
            InitializeComponent();

            WindowState = WindowState.Minimized;

            mpd = (MPDHandler)Application.Current.Properties["mpd"];

            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (mpd.GetCurrentSong() == null)
                return;

            CurrentTime.Text = FormatSeconds(mpd._currentTime);
            TimeSlider.Value = mpd._currentTime / mpd.GetCurrentSong().Time * 100;
        }

        public void UpdateButton(ref Border border, bool b)
        {
            border.Style = b ? (Style)Resources["SelectedButton"] : (Style)Resources["UnselectedButton"];
        }

        public string FormatSeconds(int time)
        {
            TimeSpan timespan = TimeSpan.FromSeconds(time);
            return timespan.ToString(@"mm\:ss");
        }
        public string FormatSeconds(double time)
        {
            TimeSpan timespan = TimeSpan.FromSeconds(time);
            return timespan.ToString(@"mm\:ss");
        }

        public void OnConnectionChanged(object sender, EventArgs e)
        {
            Connection.Text = (mpd._connected ? "✔️" : "❌") + $"{Properties.Settings.Default.mpd_host}:{Properties.Settings.Default.mpd_port}";
            SettingsWindow.UpdateConnectionStatus();
            if (mpd._connected)
                Snapcast.IsEnabled = true;
        }

        public void OnSongChanged(object sender, EventArgs e)
        {
            if (mpd.GetCurrentSong() == null)
                return;

            if (!mpd.GetCurrentSong().HasName)
                SongTitle.Text = mpd.GetCurrentSong().Title;
            else
                SongTitle.Text = mpd.GetCurrentSong().Title;
            SongTitle.ToolTip = mpd.GetCurrentSong().Path;
            SongArtist.Text = mpd.GetCurrentSong().Artist;
            SongAlbum.Text = mpd.GetCurrentSong().Album;
            SongGenre.Text = mpd.GetCurrentSong().Genre;

            if (mpd.GetCurrentSong().Date != null)
                SongAlbum.Text += $" ({ mpd.GetCurrentSong().Date})";
            Format.Text = mpd.GetCurrentSong().Path.Substring(mpd.GetCurrentSong().Path.LastIndexOf(".") + 1);

            EndTime.Text = FormatSeconds(mpd.GetCurrentSong().Time);
        }

        public void OnStatusChanged(object sender, EventArgs e)
        {
            if (mpd.GetStatus() == null)
                return;

            if (VolumeSlider.Value != mpd._currentVolume)
            {
                VolumeSlider.Value = mpd._currentVolume;
                VolumeSlider.ToolTip = mpd._currentVolume;
            }

            if (mpd.IsPlaying())
                PlayPause.Text = "\xedb4";
            else
                PlayPause.Text = "\xedb5";

            UpdateButton(ref BorderRandom, mpd._currentRandom);
            UpdateButton(ref BorderRepeat, mpd._currentRepeat);
            UpdateButton(ref BorderSingle, mpd._currentSingle);
            UpdateButton(ref BorderConsume, mpd._currentConsume);
        }

        public void OnCoverChanged(object sender, EventArgs e)
        {
            if (mpd.GetCover() == null)
            {
                NoCover.Visibility = Visibility.Visible;
                Cover.Visibility = Visibility.Collapsed;
            }
            else if (Cover.Source != mpd.GetCover())
            {
                Cover.Source = mpd.GetCover();
                Cover.Visibility = Visibility.Visible;
                NoCover.Visibility = Visibility.Collapsed;
            }
        }

        public void OnSnapcastChanged()
        {
            SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
            if (snapcast.Started)
                SnapcastText.Text = "Stop Snapcast";
            else
                SnapcastText.Text = "Start Snapcast";
        }

        public void Pause_Clicked(object sender, RoutedEventArgs e) => mpd.PlayPause();
        public void Previous_Clicked(object sender, RoutedEventArgs e) =>  mpd.Prev();
        public void Next_Clicked(object sender, RoutedEventArgs e) => mpd.Next();

        public void Random_Clicked(object sender, RoutedEventArgs e) => mpd.Random();
        public void Repeat_Clicked(object sender, RoutedEventArgs e) => mpd.Repeat();
        public void Single_Clicked(object sender, RoutedEventArgs e) => mpd.Single();
        public void Consume_Clicked(object sender, RoutedEventArgs e) => mpd.Consume();
        public void ChangeVolume(int value) => mpd.SetVolume(value);

        public void Snapcast_Clicked(object sender, RoutedEventArgs e)
        {
            SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
            if (!snapcast.Started)
                snapcast.Start();
            else
                snapcast.Stop();
        }

        public void Settings_Clicked(object sender, RoutedEventArgs e)
        {
            SettingsWindow.Show();
            SettingsWindow.Activate();

            if (SettingsWindow.WindowState == WindowState.Minimized)
                SettingsWindow.WindowState = WindowState.Normal;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
        }

        private void TimeSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            timer.Stop();
        }

        private void TimeSlider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            Slider slider = (Slider)sender;

            double SongPercentage = slider.Value;
            double SongTime = mpd._totalTime;
            double SeekTime = SongPercentage / 100 * SongTime;

            mpd.SetTime(SeekTime);
            timer.Start();
        }

        private void VolumeSlider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            Slider slider = (Slider)sender;
            mpd.SetVolume((int)slider.Value);
            slider.ToolTip = mpd._currentVolume;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HotkeyHandler hk = (HotkeyHandler)Application.Current.Properties["hotkeys"];
            hk.Activate(this);
        }

        public void InitHwnd()
        {
            WindowInteropHelper helper = new(this);
            helper.EnsureHandle();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}