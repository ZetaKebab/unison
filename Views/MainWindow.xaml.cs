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
    public partial class MainWindow : Window
    {
        private readonly Settings _settingsWin;
        private readonly DispatcherTimer _timer;
        private readonly MPDHandler _mpd;

        public MainWindow()
        {
            InitHwnd();
            InitializeComponent();
            WindowState = WindowState.Minimized;

            _settingsWin = new Settings();
            _timer = new DispatcherTimer();
            _mpd = (MPDHandler)Application.Current.Properties["mpd"];

            _timer.Interval = TimeSpan.FromSeconds(0.5);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mpd.GetCurrentSong() == null)
                return;

            CurrentTime.Text = FormatSeconds(_mpd.GetCurrentTime());
            TimeSlider.Value = _mpd.GetCurrentTime() / _mpd.GetCurrentSong().Time * 100;
        }

        public void OnConnectionChanged(object sender, EventArgs e)
        {
            Connection.Text = (_mpd.IsConnected() ? "✔️" : "❌") + $"{Properties.Settings.Default.mpd_host}:{Properties.Settings.Default.mpd_port}";
            _settingsWin.UpdateConnectionStatus();
            if (_mpd.IsConnected())
                Snapcast.IsEnabled = true;
        }

        public void OnSongChanged(object sender, EventArgs e)
        {
            if (_mpd.GetCurrentSong() == null)
                return;

            if (_mpd.GetCurrentSong().HasTitle && _mpd.GetCurrentSong().Title.Length > 0)
                SongTitle.Text = _mpd.GetCurrentSong().Title;
            else if (_mpd.GetCurrentSong().HasName && _mpd.GetCurrentSong().Name.Length > 0)
                SongTitle.Text = _mpd.GetCurrentSong().Name;
            else
            {
                int start = _mpd.GetCurrentSong().Path.LastIndexOf("/") + 1;
                int end = _mpd.GetCurrentSong().Path.LastIndexOf(".");
                SongTitle.Text = _mpd.GetCurrentSong().Path.Substring(start, end - start);
            }

            SongTitle.ToolTip = _mpd.GetCurrentSong().Path;
            SongArtist.Text = _mpd.GetCurrentSong().Artist;
            SongAlbum.Text = _mpd.GetCurrentSong().Album;

            if (_mpd.GetCurrentSong().Date != null)
                SongAlbum.Text += $" ({ _mpd.GetCurrentSong().Date})";

            SongGenre.Text = _mpd.GetCurrentSong().Genre;
            SongFormat.Text = _mpd.GetCurrentSong().Path.Substring(_mpd.GetCurrentSong().Path.LastIndexOf(".") + 1);
            if (SongGenre.Text.Length == 0 || SongFormat.Text.Length == 0)
                SongInfoDash.Text = "";
            else
                SongInfoDash.Text = " – ";

            TimeSlider.IsEnabled = true;
            if (_mpd.GetCurrentSong().Time == -1)
            {
                CurrentTime.Text = "";
                EndTime.Text = "";
                _timer.Stop();
                TimeSlider.Value = 50;
                TimeSlider.IsEnabled = false;
            }
            else
            {
                if (!_timer.IsEnabled)
                    _timer.Start();
                EndTime.Text = FormatSeconds(_mpd.GetCurrentSong().Time);
            }
        }

        public void OnStatusChanged(object sender, EventArgs e)
        {
            if (_mpd.GetStatus() == null)
                return;

            if (VolumeSlider.Value != _mpd.GetStatus().Volume)
            {
                VolumeSlider.Value = _mpd.GetStatus().Volume;
                VolumeSlider.ToolTip = _mpd.GetStatus().Volume;
            }

            UpdateButton(ref BorderRandom, _mpd.GetStatus().Random);
            UpdateButton(ref BorderRepeat, _mpd.GetStatus().Repeat);
            UpdateButton(ref BorderSingle, _mpd.GetStatus().Single);
            UpdateButton(ref BorderConsume, _mpd.GetStatus().Consume);

            if (_mpd.IsPlaying())
                PlayPause.Text = "\xedb4";
            else
            {
                PlayPause.Text = "\xedb5";
                if (_mpd.GetStatus().State == MpcNET.MpdState.Stop)
                {
                    DefaultState();
                }
            }
        }

        private void DefaultState()
        {
            SongTitle.Text = "";
            SongArtist.Text = "";
            SongAlbum.Text = "";
            SongGenre.Text = "";
            SongInfoDash.Text = "";
            SongFormat.Text = "";
            CurrentTime.Text = "";
            EndTime.Text = "";
            TimeSlider.Value = 50;
            TimeSlider.IsEnabled = false;
            NoCover.Visibility = Visibility.Visible;
            Cover.Visibility = Visibility.Collapsed;
        }

        public void OnCoverChanged(object sender, EventArgs e)
        {
            if (_mpd.GetCover() == null)
            {
                NoCover.Visibility = Visibility.Visible;
                Cover.Visibility = Visibility.Collapsed;
            }
            else if (Cover.Source != _mpd.GetCover())
            {
                Cover.Source = _mpd.GetCover();
                Cover.Visibility = Visibility.Visible;
                NoCover.Visibility = Visibility.Collapsed;
            }
        }

        public void OnSnapcastChanged()
        {
            SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
            if (snapcast.HasStarted)
                SnapcastText.Text = "Stop Snapcast";
            else
                SnapcastText.Text = "Start Snapcast";
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

        public void Pause_Clicked(object sender, RoutedEventArgs e) => _mpd.PlayPause();
        public void Previous_Clicked(object sender, RoutedEventArgs e) =>  _mpd.Prev();
        public void Next_Clicked(object sender, RoutedEventArgs e) => _mpd.Next();

        public void Random_Clicked(object sender, RoutedEventArgs e) => _mpd.Random();
        public void Repeat_Clicked(object sender, RoutedEventArgs e) => _mpd.Repeat();
        public void Single_Clicked(object sender, RoutedEventArgs e) => _mpd.Single();
        public void Consume_Clicked(object sender, RoutedEventArgs e) => _mpd.Consume();
        public void ChangeVolume(int value) => _mpd.SetVolume(value);

        public void Snapcast_Clicked(object sender, RoutedEventArgs e)
        {
            SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
            snapcast.LaunchOrExit();
        }

        public void Settings_Clicked(object sender, RoutedEventArgs e)
        {
            _settingsWin.Show();
            _settingsWin.Activate();

            if (_settingsWin.WindowState == WindowState.Minimized)
                _settingsWin.WindowState = WindowState.Normal;
        }

        private void TimeSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _timer.Stop();
        }

        private void TimeSlider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            Slider slider = (Slider)sender;

            double SongPercentage = slider.Value;
            double SongTime = _mpd.GetCurrentSong().Time;
            double SeekTime = SongPercentage / 100 * SongTime;

            _mpd.SetTime(SeekTime);
            _timer.Start();
        }

        private void VolumeSlider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            Slider slider = (Slider)sender;
            _mpd.SetVolume((int)slider.Value);
            slider.ToolTip = (int)slider.Value;
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
        }
    }
}