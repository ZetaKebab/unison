using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MPDCtrl.Models;
using System.Windows.Interop;

namespace unison
{
    public partial class MainWindow : Window
    {
        private readonly MPC _mpd = new();
        private bool _connected;
        public int _currentVolume;
        private bool _currentRandom;
        private bool _currentRepeat;
        private bool _currentSingle;
        private bool _currentConsume;
        private double _currentElapsed;

        private string _mpdHost = "192.168.1.13";
        private int _mpdPort = 6600;
        private string _mpdPassword = null;

        Settings SettingsWindow = new Settings();

        Thickness SelectedThickness;
        Thickness BaseThickness;

        public MainWindow()
        {
            InitHwnd();
            InitializeComponent();

            WindowState = WindowState.Minimized;

            ConnectToMPD();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.2);
            timer.Tick += Timer_Tick;
            timer.Start();

            SelectedThickness.Left = SelectedThickness.Right = SelectedThickness.Top = 0.0f;
            SelectedThickness.Bottom = 2.0f;
            BaseThickness.Left = BaseThickness.Right = BaseThickness.Top = BaseThickness.Bottom = 0.0f;
        }

        public async void ConnectToMPD()
        {
            Trace.WriteLine("Trying to connect...");
            _connected = await _mpd.MpdCommandConnectionStart(_mpdHost, _mpdPort, _mpdPassword);
            if (_connected)
            {
                await _mpd.MpdQueryStatus();
                await Task.Delay(5);
                _currentVolume = _mpd.MpdStatus.MpdVolume;
                _currentRandom = _mpd.MpdStatus.MpdRandom;
                _currentRepeat = _mpd.MpdStatus.MpdRepeat;
                _currentSingle = _mpd.MpdStatus.MpdSingle;
                _currentConsume = _mpd.MpdStatus.MpdConsume;
                _currentElapsed = _mpd.MpdStatus.MpdSongElapsed;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            LoopMPD();
            UpdateInterface();
        }

        public void CheckStatus<T>(ref T a, T b)
        {
            if (Comparer<T>.Default.Compare(a, b) != 0)
                a = b;
        }

        public async void LoopMPD()
        {
            if (!_connected)
                return;

            var status = await _mpd.MpdQueryStatus();
            //Trace.WriteLine(status.ResultText);
            await Task.Delay(5);

            if (status != null)
            {
                CheckStatus(ref _currentVolume, _mpd.MpdStatus.MpdVolume);
                CheckStatus(ref _currentRandom, _mpd.MpdStatus.MpdRandom);
                CheckStatus(ref _currentRepeat, _mpd.MpdStatus.MpdRepeat);
                CheckStatus(ref _currentSingle, _mpd.MpdStatus.MpdSingle);
                CheckStatus(ref _currentConsume, _mpd.MpdStatus.MpdConsume);
                CheckStatus(ref _currentElapsed, _mpd.MpdStatus.MpdSongElapsed);
            }

            await _mpd.MpdQueryCurrentSong();
            await Task.Delay(5);
        }

        public void UpdateButton(ref Border border, bool b)
        {
            if (b)
                border.BorderThickness = SelectedThickness;
            else
                border.BorderThickness = BaseThickness;
        }

        public string FormatSeconds(double time)
        {
            TimeSpan timespan = TimeSpan.FromSeconds(time);
            return timespan.ToString(@"mm\:ss");
        }

        public void UpdateInterface()
        {
            if (_mpd.MpdCurrentSong != null)
            {
                SongTitle.Text = _mpd.MpdCurrentSong.Title;
                SongTitle.ToolTip = _mpd.MpdCurrentSong.File;
                SongArtist.Text = _mpd.MpdCurrentSong.Artist;
                SongAlbum.Text = _mpd.MpdCurrentSong.Album + " (" + _mpd.MpdCurrentSong.Date + ")";
                Bitrate.Text = _mpd.MpdCurrentSong.File.Substring(_mpd.MpdCurrentSong.File.LastIndexOf(".") + 1);
                Bitrate.Text += " – ";
                Bitrate.Text += _mpd.MpdStatus.MpdBitrate + "kbps";

                CurrentTime.Text = FormatSeconds(_currentElapsed);
                EndTime.Text = FormatSeconds(_mpd.MpdStatus.MpdSongTime);

                TimeSlider.Value = _currentElapsed / _mpd.MpdCurrentSong.TimeSort * 100;
            }

            if (VolumeSlider.Value != _currentVolume)
            {
                VolumeSlider.Value = _currentVolume;
                VolumeSlider.ToolTip = _currentVolume;
            }

            if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Play)
                PauseButtonEmoji.Text = "⏸️";
            else if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Pause)
                PauseButtonEmoji.Text = "▶️";

            SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
            if (snapcast.Started)
                SnapcastText.Text = "Stop Snapcast";
            else
                SnapcastText.Text = "Start Snapcast";

            Connection.Text = (_connected ? "✔️" : "❌") + _mpd.MpdHost + ":" + _mpd.MpdPort;

            UpdateButton(ref BorderRandom, _currentRandom);
            UpdateButton(ref BorderRepeat, _currentRepeat);
            UpdateButton(ref BorderSingle, _currentSingle);
            UpdateButton(ref BorderConsume, _currentConsume);
        }

        public async void Pause_Clicked(object sender, RoutedEventArgs e)
        {
            if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Play)
                await _mpd.MpdPlaybackPause();
            else if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Pause)
                await _mpd.MpdPlaybackPlay(_currentVolume);
        }

        public async void Previous_Clicked(object sender, RoutedEventArgs e)
        {
            await _mpd.MpdPlaybackPrev(_currentVolume);
        }

        public async void Next_Clicked(object sender, RoutedEventArgs e)
        {
            await _mpd.MpdPlaybackNext(_currentVolume);
        }

        public async void Random_Clicked(object sender, RoutedEventArgs e)
        {
            await _mpd.MpdSetRandom(!_currentRandom);
        }

        private async void Repeat_Clicked(object sender, RoutedEventArgs e)
        {
            await _mpd.MpdSetRepeat(!_currentRepeat);
        }

        private async void Single_Clicked(object sender, RoutedEventArgs e)
        {
            await _mpd.MpdSetSingle(!_currentSingle);
        }

        private async void Consume_Clicked(object sender, RoutedEventArgs e)
        {
            await _mpd.MpdSetConsume(!_currentConsume);
        }

        public async void ChangeVolume(int value)
        {
            await _mpd.MpdSetVolume(value);
        }

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
            if (SettingsWindow.WindowState == WindowState.Minimized)
            {
                SettingsWindow.WindowState = WindowState.Normal;
                SettingsWindow.Show();
                SettingsWindow.Activate();
            }
            else
            {
                SettingsWindow.Hide();
                SettingsWindow.WindowState = WindowState.Minimized;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
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
    }
}