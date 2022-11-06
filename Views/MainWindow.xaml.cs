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
        private readonly Radios _radiosWin;
        private readonly DispatcherTimer _timer;
        private readonly MPDHandler _mpd;

        public MainWindow()
        {
            InitHwnd();
            InitializeComponent();
            DefaultState(true);
            WindowState = WindowState.Minimized;

            _settingsWin = new Settings();
            _radiosWin = new Radios();
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
            if (_mpd.IsConnected())
            {
                ConnectionOkIcon.Visibility = Visibility.Visible;
                ConnectionFailIcon.Visibility = Visibility.Collapsed;

                Snapcast.IsEnabled = true;
                if (_radiosWin.IsConnected())
                    Radio.IsEnabled = true;
            }
            else
            {
                _timer.Stop();
                DefaultState(true);
                ConnectionOkIcon.Visibility = Visibility.Collapsed;
                ConnectionFailIcon.Visibility = Visibility.Visible;

                Snapcast.IsEnabled = false;
                Radio.IsEnabled = false;
            }
            _settingsWin.UpdateConnectionStatus();
            Connection.Text = $"{Properties.Settings.Default.mpd_host}:{Properties.Settings.Default.mpd_port}";
        }

        public void OnSongChanged(object sender, EventArgs e)
        {
            if (_mpd.GetCurrentSong() == null)
                return;

            if (_mpd.GetCurrentSong().HasTitle && _mpd.GetCurrentSong().Title.Length > 0)
                SongTitle.Text = _mpd.GetCurrentSong().Title;
            else if (_mpd.GetCurrentSong().HasName && _mpd.GetCurrentSong().Name.Length > 0)
                SongTitle.Text = _mpd.GetCurrentSong().Name;
            else if (_mpd.GetCurrentSong().Path != null)
            {
                int start = _mpd.GetCurrentSong().Path.LastIndexOf("/") + 1;
                int end = _mpd.GetCurrentSong().Path.LastIndexOf(".");
                if (start > 0 && end > 0 && end > start)
                    SongTitle.Text = _mpd.GetCurrentSong().Path.Substring(start, end - start);
            }

            SongTitle.ToolTip = _mpd.GetCurrentSong().Path;
            SongArtist.Text = _mpd.GetCurrentSong().Artist;
            SongAlbum.Text = _mpd.GetCurrentSong().Album;

            if (_mpd.GetCurrentSong().Date != null)
                SongAlbum.Text += $" ({_mpd.GetCurrentSong().Date.Split("-")[0]})";

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
                PlayPause.Text = (string)Application.Current.FindResource("playButton");
            else
            {
                PlayPause.Text = (string)Application.Current.FindResource("pauseButton");
                if (_mpd.GetStatus().State == MpcNET.MpdState.Stop)
                {
                    DefaultState();
                }
            }

            _mpd.QueryStats();
            _settingsWin.UpdateStats();
        }

        private void DefaultState(bool LostConnection = false)
        {
            SongTitle.Text = "";
            SongArtist.Text = "";
            SongAlbum.Text = "";
            SongGenre.Text = "";
            SongInfoDash.Text = "";
            SongFormat.Text = "";
            CurrentTime.Text = "";
            EndTime.Text = "";
            PlayPause.Text = (string)Application.Current.FindResource("pauseButton");
            TimeSlider.Value = 50;
            TimeSlider.IsEnabled = false;
            NoCover.Visibility = Visibility.Collapsed;
            Cover.Visibility = Visibility.Collapsed;
            RadioCover.Visibility = Visibility.Collapsed;

            if (LostConnection)
            {
                ConnectionOkIcon.Visibility = Visibility.Collapsed;
                ConnectionFailIcon.Visibility = Visibility.Visible;
            }
            Connection.Text = $"{Properties.Settings.Default.mpd_host}:{Properties.Settings.Default.mpd_port}";
        }

        public void OnCoverChanged(object sender, EventArgs e)
        {
            NoCover.Visibility = Visibility.Collapsed;
            Cover.Visibility = Visibility.Collapsed;
            RadioCover.Visibility = Visibility.Collapsed;

            if (_mpd.GetCurrentSong().Time == -1)
                RadioCover.Visibility = Visibility.Visible;
            else if (_mpd.GetCover() == null)
                NoCover.Visibility = Visibility.Visible;
            else if (Cover.Source != _mpd.GetCover())
            {
                Cover.Source = _mpd.GetCover();
                Cover.Visibility = Visibility.Visible;
            }
        }

        public void OnSnapcastChanged()
        {
            SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
            if (snapcast.HasStarted)
                SnapcastText.Text = unison.Resources.Resources.StopSnapcast; 
            else
                SnapcastText.Text = unison.Resources.Resources.StartSnapcast;
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

        public void Radios_Clicked(object sender, RoutedEventArgs e)
        {
            _radiosWin.Show();
            _radiosWin.Activate();

            if (_radiosWin.WindowState == WindowState.Minimized)
                _radiosWin.WindowState = WindowState.Normal;
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

        public void UpdateUpdateStatus(string version)
        {
            _settingsWin.UpdateUpdateStatus(version);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HotkeyHandler hk = (HotkeyHandler)Application.Current.Properties["hotkeys"];
            hk.Activate(this);
        }

        private void MouseDownClipboard(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                string CopyText = SongTitle.Text + " - " + SongArtist.Text + "\n";
                CopyText += SongAlbum.Text + "\n";
                CopyText += SongTitle.ToolTip;
                Clipboard.SetText(CopyText);
            }
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