﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MpcNET;
using MpcNET.Commands.Database;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Queue;
using MpcNET.Commands.Reflection;
using MpcNET.Commands.Status;
using MpcNET.Message;
using MpcNET.Types;

namespace unison
{
    public class Statistics
    {
        public int Songs { get; set; }
        public int Albums { get; set; }
        public int Artists { get; set; }
        public string TotalPlaytime { get; set; }
        public string Uptime { get; set; }
        public string TotalTimePlayed { get; set; }
        public string DatabaseUpdate { get; set; }
    }

    public class MPDHandler
    {
        private bool _connected;
        public string _version;
        private int _currentVolume;
        private int _previousVolume;
        private bool _currentRandom;
        private bool _currentRepeat;
        private bool _currentSingle;
        private bool _currentConsume;
        private double _currentTime;
        private double _totalTime;

        private MpdStatus _currentStatus;
        private IMpdFile _currentSong;
        private BitmapFrame _cover;
        public Statistics _stats;
        private readonly System.Timers.Timer _elapsedTimer;
        private DispatcherTimer _retryTimer;

        bool _isUpdatingStatus = false;
        bool _isUpdatingSong = false;

        public IPAddress _ipAddress;
        bool _invalidIp = false;

        private event EventHandler ConnectionChanged;
        private event EventHandler StatusChanged;
        private event EventHandler SongChanged;
        private event EventHandler CoverChanged;

        private MpcConnection _connection;
        private MpcConnection _commandConnection;
        private IPEndPoint _mpdEndpoint;
        private CancellationTokenSource _cancelToken;

        public MPDHandler()
        {
            Initialize(null, null);

            _stats = new Statistics();

            _retryTimer = new DispatcherTimer();
            _retryTimer.Interval = TimeSpan.FromSeconds(5);
            _retryTimer.Tick += Initialize;

            _elapsedTimer = new System.Timers.Timer(500);
            _elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);

            ConnectionChanged += OnConnectionChanged;
            StatusChanged += OnStatusChanged;
            SongChanged += OnSongChanged;
            CoverChanged += OnCoverChanged;
        }

        private void ElapsedTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((_currentTime < _totalTime || _totalTime == -1) && (_currentStatus.State == MpdState.Play))
                _currentTime += 0.5;
            else
                _elapsedTimer.Stop();
        }

        void OnConnectionChanged(object sender, EventArgs e)
        {
            if (!_connected && !_invalidIp)
                _retryTimer.Start();
            else
                _retryTimer.Stop();

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow MainWin = (MainWindow)Application.Current.MainWindow;
                MainWin.OnConnectionChanged(sender, e);

                SnapcastHandler Snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
                Snapcast.OnConnectionChanged(sender, e);
            });
        }

        static void OnStatusChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow MainWin = (MainWindow)Application.Current.MainWindow;
                MainWin.OnStatusChanged(sender, e);
            });
        }

        static void OnSongChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow MainWin = (MainWindow)Application.Current.MainWindow;
                MainWin.OnSongChanged(sender, e);
            });
        }

        static void OnCoverChanged(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow MainWin = (MainWindow)Application.Current.MainWindow;
                MainWin.OnCoverChanged(sender, e);
            });
        }

        public void SendCommand<T>(IMpcCommand<T> command)
        {
            Task.Run(async () =>
            {
                await SafelySendCommandAsync(command);
            });
        }

        public async Task<T> SafelySendCommandAsync<T>(IMpcCommand<T> command)
        {
            if (_commandConnection == null)
            {
                Trace.WriteLine("[SafelySendCommandAsync] no command connection");
                return default(T);
            }

            try
            {
                IMpdMessage<T> response = await _commandConnection.SendAsync(command);
                if (!response.IsResponseValid)
                {
                    string mpdError = response.Response?.Result?.MpdError;
                    if (mpdError != null && mpdError != "")
                        throw new Exception(mpdError);
                    else
                        throw new Exception($"Invalid server response: {response}.");
                }

                return response.Response.Content;
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Sending {command.GetType().Name} failed: {e.Message}");
            }

            return default(T);
        }

        private void Initialize(object sender, EventArgs e)
        {
            if (!_connected)
                Connect();
        }

        public async void Connect()
        {
            _cancelToken = new CancellationTokenSource();
            CancellationToken token = _cancelToken.Token;

            try
            {
                _connection = await ConnectInternal(token);
                _commandConnection = await ConnectInternal(token);
            }
            catch (MpcNET.Exceptions.MpcConnectException e)
            {
                _connected = false;
                _invalidIp = true;
                Trace.WriteLine($"Error in connect: {e.Message}");
                ConnectionChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (_connection != null && _commandConnection != null)
            {
                _invalidIp = false;
                if (_connection.IsConnected && _commandConnection.IsConnected)
                {
                    _connected = true;
                    _version = _connection.Version;
                    ConnectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _connected = false;
                ConnectionChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            await UpdateStatusAsync();
            await UpdateSongAsync();

            Loop(token);
        }

        private async Task<MpcConnection> ConnectInternal(CancellationToken token)
        {
            IPAddress.TryParse(Properties.Settings.Default.mpd_host, out _ipAddress);

            if (_ipAddress == null)
            {
                try
                {
                    IPAddress[] addrList = Dns.GetHostAddresses(Properties.Settings.Default.mpd_host);
                    if (addrList.Length > 0)
                    {
                        foreach (IPAddress addr in addrList)
                        {
                            if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                _ipAddress = addr;
                        }
                    }
                }
                catch (Exception)
                {
                    throw new MpcNET.Exceptions.MpcConnectException("No correct IP provided by user.");
                }
            }

            _mpdEndpoint = new IPEndPoint(_ipAddress, Properties.Settings.Default.mpd_port);
            MpcConnection connection = new MpcConnection(_mpdEndpoint);
            await connection.ConnectAsync(token);

            /*if (!string.IsNullOrEmpty(Properties.Settings.Default.mpd_password))
            {
                IMpdMessage<string> result = await connection.SendAsync(new PasswordCommand(Properties.Settings.Default.mpd_password));
                if (!result.IsResponseValid)
                {
                    string mpdError = result.Response?.Result?.MpdError;
                    Trace.WriteLine(mpdError);
                }
            }*/

            return connection;
        }

        public void Disconnected()
        {            
            _connected = false;
            ConnectionChanged?.Invoke(this, EventArgs.Empty);

            if (_connection != null)
                _connection = null;

            if (_commandConnection != null)
                _commandConnection = null;
        }

        private void Loop(CancellationToken token)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        if (token.IsCancellationRequested || _connection == null)
                            break;

                        IMpdMessage<string> idleChanges = await _connection.SendAsync(new IdleCommand("stored_playlist playlist player mixer output options update"));

                        if (idleChanges.IsResponseValid)
                            await HandleIdleResponseAsync(idleChanges.Response.Content);
                        else
                        {
                            Trace.WriteLine($"Error in Idle connection thread: {idleChanges.Response?.Content}");
                            throw new Exception(idleChanges.Response?.Content);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Error in Idle connection thread: {e.Message}");
                        Disconnected();
                        break;
                    }
                }

            }, token).ConfigureAwait(false);
        }

        private async Task HandleIdleResponseAsync(string subsystems)
        {
            try
            {
                if (subsystems.Contains("player") || subsystems.Contains("mixer") || subsystems.Contains("output") || subsystems.Contains("options"))
                {
                    await UpdateStatusAsync();

                    if (subsystems.Contains("player"))
                        await UpdateSongAsync();
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error in Idle connection thread: {e.Message}");
            }
        }

        private async Task UpdateStatusAsync()
        {
            if (_connection == null || _isUpdatingStatus)
                return;
 
            _isUpdatingStatus = true;

            try
            {
                IMpdMessage<MpdStatus> response = await _connection.SendAsync(new StatusCommand());
                if (response != null && response.IsResponseValid)
                {
                    _currentStatus = response.Response.Content;
                    UpdateStatus();
                }
                else
                    throw new Exception();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error in Idle connection thread: {e.Message}");
            }

            _isUpdatingStatus = false;
        }

        private async Task UpdateSongAsync()
        {
            if (_connection == null || _isUpdatingSong)
                return;

            _isUpdatingSong = true;

            try
            {
                IMpdMessage<IMpdFile> response = await _connection.SendAsync(new CurrentSongCommand());
                if (response != null && response.IsResponseValid)
                {
                    _currentSong = response.Response.Content;
                    UpdateSong();
                }
                else
                    throw new Exception();
            }
            catch (Exception e)
            {
                Trace.WriteLine($"Error in Idle connection thread: {e.Message}");
            }

            _isUpdatingSong = false;
        }

        private async void GetAlbumCover(string path, CancellationToken token = default)
        {
            List<byte> data = new List<byte>();
            try
            {
                bool ReadPictureFailed = true;
                long totalBinarySize = 9999;
                long currentSize = 0;

                do
                {
                    if (_connection == null)
                        return;

                    IMpdMessage<MpdBinaryData> albumReq = await _connection.SendAsync(new ReadPictureCommand(path, currentSize));
                    if (!albumReq.IsResponseValid)
                        break;

                    MpdBinaryData response = albumReq.Response.Content;
                    if (response == null || response.Binary == 0)
                        break;

                    ReadPictureFailed = false;
                    totalBinarySize = response.Size;
                    currentSize += response.Binary;
                    data.AddRange(response.Data);
                }
                while (currentSize < totalBinarySize && !token.IsCancellationRequested);

                do
                {
                    if (!ReadPictureFailed)
                        break;
                    if (_connection == null)
                        return;

                    IMpdMessage<MpdBinaryData> albumReq = await _connection.SendAsync(new AlbumArtCommand(path, currentSize));
                    if (!albumReq.IsResponseValid)
                        break;

                    MpdBinaryData response = albumReq.Response.Content;
                    if (response == null || response.Binary == 0)
                        break;

                    totalBinarySize = response.Size;
                    currentSize += response.Binary;
                    data.AddRange(response.Data);
                }
                while (currentSize < totalBinarySize && !token.IsCancellationRequested);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception caught while getting albumart: " + e);
                return;
            }

            if (data.Count == 0)
                _cover = null;
            else
            {
                using MemoryStream stream = new MemoryStream(data.ToArray());
                try
                {
                    _cover = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
                catch (System.NotSupportedException)
                {
                    _cover = null;
                }
            }
            UpdateCover();
        }

        public void UpdateStatus()
        {
            if (!_connected || _currentStatus == null)
                return;

            _currentRandom = _currentStatus.Random;
            _currentRepeat = _currentStatus.Repeat;
            _currentConsume = _currentStatus.Consume;
            _currentSingle = _currentStatus.Single;
            _currentVolume = _currentStatus.Volume;

            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateSong()
        {
            if (!_connected || _currentSong == null)
                return;

            _currentTime = _currentStatus.Elapsed.TotalSeconds;
            _totalTime = _currentSong.Time;
            if (!_elapsedTimer.Enabled)
                _elapsedTimer.Start();

            SongChanged?.Invoke(this, EventArgs.Empty);

            string uri = Regex.Escape(_currentSong.Path);
            GetAlbumCover(uri);
        }

        public void UpdateCover()
        {
            CoverChanged?.Invoke(this, EventArgs.Empty);
        }

        public IMpdFile GetCurrentSong() => _currentSong;
        public MpdStatus GetStatus() => _currentStatus;
        public BitmapFrame GetCover() => _cover;
        public string GetVersion() => _version;
        public Statistics GetStats() => _stats;
        public double GetCurrentTime() => _currentTime;

        public bool IsConnected() => _connected;
        public bool IsPlaying() => _currentStatus?.State == MpdState.Play;

        public void Prev() => SendCommand(new PreviousCommand());
        public void Next() => SendCommand(new NextCommand());
        public void PlayPause() => SendCommand(new PauseResumeCommand());

        public void Random() => SendCommand(new RandomCommand(!_currentRandom));
        public void Repeat() => SendCommand(new RepeatCommand(!_currentRepeat));
        public void Single() => SendCommand(new SingleCommand(!_currentSingle));
        public void Consume() => SendCommand(new ConsumeCommand(!_currentConsume));

        public void SetTime(double value) => SendCommand(new SeekCurCommand(value));
        public void SetVolume(int value) => SendCommand(new SetVolumeCommand((byte)value));
        
        public void VolumeUp()
        {
            _currentVolume += Properties.Settings.Default.volume_offset;
            if (_currentVolume > 100)
                _currentVolume = 100;
            SetVolume(_currentVolume);
        }

        public void VolumeDown()
        {
            _currentVolume -= Properties.Settings.Default.volume_offset;
            if (_currentVolume < 0)
                _currentVolume = 0;
            SetVolume(_currentVolume);
        }

        public void VolumeMute()
        {
            if (_currentVolume == 0)
            {
                _currentVolume = _previousVolume;
                _previousVolume = 0;
            }
            else
            {
                _previousVolume = _currentVolume;
                _currentVolume = 0;
            }
            SetVolume(_currentVolume);
        }

        public void ClearQueue() => SendCommand(new ClearCommand());
        public void PlayCommand() => SendCommand(new PlayCommand(0));

        public void AddSong(string Uri)
        {
            SendCommand(new AddCommand(Uri));
        }

        public void ClearAddAndPlay(string Uri)
        {
            CommandList commandList = new CommandList(new IMpcCommand<object>[] { new ClearCommand(), new AddCommand(Uri), new PlayCommand(0) });
            SendCommand(commandList);
        }

        public async void QueryStats()
        {
            Dictionary<string, string> response = await SafelySendCommandAsync(new StatsCommand());

            if (response != null)
            {
                _stats.Songs = int.Parse(response["songs"]);
                _stats.Albums = int.Parse(response["albums"]);
                _stats.Artists = int.Parse(response["artists"]);

                TimeSpan time;
                time = TimeSpan.FromSeconds(int.Parse(response["uptime"]));
                _stats.Uptime = time.ToString(@"dd\:hh\:mm\:ss");
                time = TimeSpan.FromSeconds(int.Parse(response["db_playtime"]));
                _stats.TotalPlaytime = time.ToString(@"dd\:hh\:mm\:ss");
                time = TimeSpan.FromSeconds(int.Parse(response["playtime"]));
                _stats.TotalTimePlayed = time.ToString(@"dd\:hh\:mm\:ss");

                DateTime date = new DateTime(1970, 1, 1).AddSeconds(int.Parse(response["db_update"])).ToLocalTime();
                _stats.DatabaseUpdate = date.ToString("dd/MM/yyyy @ HH:mm");
            }
        }
    }
}