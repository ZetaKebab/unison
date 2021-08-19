using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MPDCtrl.Models;

namespace unison
{
    public class MPDHandler
    {
        private readonly MPC _mpd = new();

        public bool _connected;
        public int _currentVolume;
        public bool _currentRandom;
        public bool _currentRepeat;
        public bool _currentSingle;
        public bool _currentConsume;
        public double _currentElapsed;

        private Status _currentStatus = null;
        private SongInfoEx _currentSong = null;
        private AlbumImage _currentAlbumCover = null;

        public double _elapsed;
        private double _time;

        private readonly System.Timers.Timer _elapsedTimer;
        private async void ElapsedTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((_elapsed < _time) && (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Play))
            {
                _elapsed += 0.5;
                await Task.Delay(5);
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        bool IsBusy = false;

        public MPDHandler()
        {
            _mpd.IsBusy += new MPC.IsBusyEvent(OnMpcIsBusy);

            _mpd.MpdIdleConnected += new MPC.IsMpdIdleConnectedEvent(OnMpdIdleConnected);

            _mpd.ConnectionStatusChanged += new MPC.ConnectionStatusChangedEvent(OnConnectionStatusChanged);
            _mpd.ConnectionError += new MPC.ConnectionErrorEvent(OnConnectionError);

            _mpd.MpdPlayerStatusChanged += new MPC.MpdPlayerStatusChangedEvent(OnMpdPlayerStatusChanged);
            _mpd.MpdCurrentQueueChanged += new MPC.MpdCurrentQueueChangedEvent(OnMpdCurrentQueueChanged);

            _mpd.MpdAlbumArtChanged += new MPC.MpdAlbumArtChangedEvent(OnAlbumArtChanged);

            _elapsedTimer = new System.Timers.Timer(500);
            _elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);


            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += QueryStatus;
            timer.Start();
        }

        private void OnMpcIsBusy(MPC sender, bool on)
        {
            IsBusy = on;
        }

        public void Start()
        {
            Task.Run(() => _mpd.MpdIdleConnect(Properties.Settings.Default.mpd_host, Properties.Settings.Default.mpd_port));
        }

        private void OnMpdIdleConnected(MPC sender)
        {
            Trace.WriteLine($"Connection to mpd {_mpd.MpdVerText}...");
            LoadInitialData();
        }

        private void OnConnectionStatusChanged(MPC sender, MPC.ConnectionStatus status)
        {
            Trace.WriteLine("Connection changed...");
        }

        private void OnConnectionError(MPC sender, string msg)
        {
            Trace.WriteLine("Connection ERROR!");
            LoadInitialData();
        }

        private void OnMpdPlayerStatusChanged(MPC sender)
        {
            Trace.WriteLine("Status changed...");

            UpdateStatus();
        }

        private void OnMpdCurrentQueueChanged(MPC sender)
        {
            Trace.WriteLine("Queue changed...");

            UpdateStatus();
        }

        private void OnAlbumArtChanged(MPC sender)
        {
            // AlbumArt
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                if ((!_mpd.AlbumCover.IsDownloading) && _mpd.AlbumCover.IsSuccess)
                {
                    if ((_mpd.MpdCurrentSong != null) && (_mpd.AlbumCover.AlbumImageSource != null))
                    {
                        if (!String.IsNullOrEmpty(_mpd.MpdCurrentSong.File))
                        {
                            if (_mpd.MpdCurrentSong.File == _mpd.AlbumCover.SongFilePath)
                            {
                                Trace.WriteLine("found cover");
                                _currentAlbumCover = _mpd.AlbumCover;
                            }
                        }
                    }
                }
            });
        }

        public async void LoadInitialData()
        {
            //todo : test if the password works

            IsBusy = true;
            await Task.Delay(5);

            CommandResult result = await _mpd.MpdIdleSendPassword(Properties.Settings.Default.mpd_password);

            if (result.IsSuccess)
            {
                _connected = await _mpd.MpdCommandConnectionStart(Properties.Settings.Default.mpd_host, Properties.Settings.Default.mpd_port, Properties.Settings.Default.mpd_password);
                if (_connected)
                {
                    await _mpd.MpdSendUpdate();

                    result = await _mpd.MpdIdleQueryStatus();
                    await Task.Delay(5);

                    if (result.IsSuccess)
                    {
                        _currentVolume = _mpd.MpdStatus.MpdVolume;
                        _currentRandom = _mpd.MpdStatus.MpdRandom;
                        _currentRepeat = _mpd.MpdStatus.MpdRepeat;
                        _currentSingle = _mpd.MpdStatus.MpdSingle;
                        _currentConsume = _mpd.MpdStatus.MpdConsume;
                        _currentElapsed = _mpd.MpdStatus.MpdSongElapsed;
                    }

                    await Task.Delay(5);
                    CommandResult song = await _mpd.MpdIdleQueryCurrentSong();
                    await Task.Delay(5);
                    if (song != null)
                        _currentSong = _mpd.MpdCurrentSong;

                    await Task.Delay(5);
                    _mpd.MpdIdleStart();
                    await Task.Delay(5);
                    UpdateStatus();
                }
            }
        }

        public async void QueryStatus(object sender, EventArgs e)
        {
            if (IsBusy)
                return;

            Trace.WriteLine("Querying status...");

            CommandResult result = await _mpd.MpdQueryStatus();
            await Task.Delay(5);

            if (result.IsSuccess)
            {
                result = await _mpd.MpdQueryCurrentSong();
                await Task.Delay(5);

                if (result.IsSuccess)
                {
                    UpdateStatus();
                }
            }
        }

        public async void UpdateStatus()
        {
            if (!_connected)
                return;

            await Task.Delay(50);

            _currentStatus = _mpd.MpdStatus;

            _currentRandom = _mpd.MpdStatus.MpdRandom;
            _currentRepeat = _mpd.MpdStatus.MpdRepeat;
            _currentConsume = _mpd.MpdStatus.MpdConsume;
            _currentSingle = _mpd.MpdStatus.MpdSingle;

            _currentVolume = _mpd.MpdStatus.MpdVolume;
            _currentElapsed = _mpd.MpdStatus.MpdSongElapsed;

            _currentSong = _mpd.MpdCurrentSong;

            _time = _mpd.MpdStatus.MpdSongTime;

            _elapsed = _mpd.MpdStatus.MpdSongElapsed;

            if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Play)
            {
                if (!_elapsedTimer.Enabled)
                    _elapsedTimer.Start();
            }
            else
            {
                _elapsedTimer.Stop();
            }

            await _mpd.MpdQueryAlbumArt(_currentSong.File, false);
        }

        public SongInfoEx GetCurrentSong() => _currentSong;
        public Status GetStatus() => _currentStatus;
        public AlbumImage GetCover() => _currentAlbumCover;

        public async void Prev()
        {
            if (!IsBusy)
                await _mpd.MpdPlaybackPrev(_currentVolume);
        }

        public async void Next()
        {
            if (!IsBusy)
                await _mpd.MpdPlaybackNext(_currentVolume);
        }

        public async void PlayPause()
        {
            if (IsBusy)
                return;
            if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Play)
                await _mpd.MpdPlaybackPause();
            else if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Pause)
                await _mpd.MpdPlaybackPlay(_currentVolume);
        }

        public async void Random()
        {
            if (!IsBusy)
                await _mpd.MpdSetRandom(!_currentRandom);
        }

        public async void Repeat()
        {
            if (!IsBusy)
                await _mpd.MpdSetRepeat(!_currentRepeat);
        }

        public async void Single()
        {
            if (!IsBusy)
                await _mpd.MpdSetSingle(!_currentSingle);
        }

        public async void Consume()
        {
            if (!IsBusy)
                await _mpd.MpdSetConsume(!_currentConsume);
        }

        public async void SetVolume(int value)
        {
            if (!IsBusy)
                await _mpd.MpdSetVolume(value);
        }

        public bool IsPlaying()
        {
            return _currentStatus?.MpdState == MPDCtrl.Models.Status.MpdPlayState.Play;
        }
    }
}
