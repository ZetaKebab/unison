using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using MPDCtrl.Models;

namespace unison
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MPC _mpd = new();
        private bool _connected = false;
        private int _currentVolume;
        private bool _currentRandom;
        private bool _currentRepeat;
        private bool _currentSingle;
        private bool _currentConsume;
        private double _currentElapsed;

        private readonly Process _snapcast = new Process();
        private bool _snapcastStarted = false;

        private string _snapcastVersion = "snapclient_0.25.0-1_win64";
        private string _mpdHost = "192.168.1.13";
        private int _mpdPort = 6600;
        private string _mpdPassword = null;

        public MainWindow()
        {
            InitializeComponent();
            ConnectToMPD();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.2);
            timer.Tick += timer_Tick;
            timer.Start();
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

        void timer_Tick(object sender, EventArgs e)
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

        public void UpdateButton(ref Button button, bool b)
        {
            if (b)
                button.Foreground = System.Windows.SystemColors.GradientActiveCaptionBrush;
            else
                button.Foreground = System.Windows.SystemColors.DesktopBrush;
        }

        public string FormatSeconds(double time)
        {
            var timespan = TimeSpan.FromSeconds(time);
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
                PauseButton.Content = "⏸";
            else if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Pause)
                PauseButton.Content = "▶️";

            if (_snapcastStarted)
                Snapcast.Content = "Stop Snapcast";
            else
                Snapcast.Content = "Start Snapcast";

            DebugText.Text = _mpd.MpdHost + ":" + _mpd.MpdPort;

            UpdateButton(ref Random, _currentRandom);
            UpdateButton(ref Repeat, _currentRepeat);
            UpdateButton(ref Single, _currentSingle);
            UpdateButton(ref Consume, _currentConsume);
        }

        private async void Pause_Clicked(object sender, RoutedEventArgs e)
        {
            if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Play)
                await _mpd.MpdPlaybackPause();
            else if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Pause)
                await _mpd.MpdPlaybackPlay(_currentVolume);
        }

        private async void Previous_Clicked(object sender, RoutedEventArgs e)
        {
            await _mpd.MpdPlaybackPrev(_currentVolume);
        }

        private async void Next_Clicked(object sender, RoutedEventArgs e)
        {
            await _mpd.MpdPlaybackNext(_currentVolume);
        }

        private async void Random_Clicked(object sender, RoutedEventArgs e)
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

        private void Snapcast_Clicked(object sender, RoutedEventArgs e)
        {
            if (!_snapcastStarted)
            {
                _snapcast.StartInfo.FileName = _snapcastVersion + @"\snapclient.exe";
                _snapcast.StartInfo.Arguments = "--host " + _mpd.MpdHost;
                _snapcast.StartInfo.CreateNoWindow = true;
                _snapcast.Start();
                _snapcastStarted = true;
            }
            else
            {
                _snapcast.Kill();
                _snapcastStarted = false;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_snapcastStarted)
                _snapcast.Kill();
        }
    }
}