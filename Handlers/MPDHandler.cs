using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MpcNET;
using MpcNET.Commands.Database;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Reflection;
using MpcNET.Commands.Status;

namespace unison
{
    public class MPDHandler
    {
        public bool _connected;
        public string _version;
        public int _currentVolume;
        public bool _currentRandom;
        public bool _currentRepeat;
        public bool _currentSingle;
        public bool _currentConsume;
        public double _currentTime;
        public double _totalTime;

        BitmapFrame _cover;

        public static MpdStatus BOGUS_STATUS = new MpdStatus(0, false, false, false, false, -1, -1, -1, MpdState.Unknown, -1, -1, -1, -1, TimeSpan.Zero, TimeSpan.Zero, -1, -1, -1, -1, -1, "", "");
        public MpdStatus CurrentStatus { get; private set; } = BOGUS_STATUS;

        MpcNET.Types.IMpdFile CurrentSong { get; set; }

        private readonly System.Timers.Timer _elapsedTimer;
        private async void ElapsedTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((_currentTime < _totalTime) && (CurrentStatus.State == MpdState.Play))
            {
                _currentTime += 0.5;
                await Task.Delay(5);
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        private MpcConnection _connection;

        private MpcConnection _commandConnection;

        private IPEndPoint _mpdEndpoint;

        private CancellationTokenSource cancelToken;

        public MPDHandler()
        {
            cancelToken = new CancellationTokenSource();

            Initialize();

            _elapsedTimer = new System.Timers.Timer(500);
            _elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);
        }

        private async void Initialize()
        {
            var token = cancelToken.Token;
            _connection = await Connect(token);
            _commandConnection = await Connect(token);
            if (_connection.IsConnected)
            {
                _connected = true;
                _version = _connection.Version;
            }
            Trace.WriteLine("is connected: " + _connected);
            Trace.WriteLine("version: " + _version);

            await UpdateStatusAsync();
            await UpdateSongAsync();

            Loop(token);
        }

        private async Task<MpcConnection> Connect(CancellationToken token)
        {
            IPAddress.TryParse(Properties.Settings.Default.mpd_host, out IPAddress ipAddress);

            _mpdEndpoint = new IPEndPoint(ipAddress, Properties.Settings.Default.mpd_port);
            MpcConnection connection = new MpcConnection(_mpdEndpoint);
            await connection.ConnectAsync(token);

            if (!string.IsNullOrEmpty(Properties.Settings.Default.mpd_password))
            {
                MpcNET.Message.IMpdMessage<string> result = await connection.SendAsync(new PasswordCommand(Properties.Settings.Default.mpd_password));
                if (!result.IsResponseValid)
                {
                    string mpdError = result.Response?.Result?.MpdError;
                }
            }

            return connection;
        }

        private void Loop(CancellationToken token)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (token.IsCancellationRequested || _connection == null)
                            break;

                        Trace.WriteLine("loop");
                        var idleChanges = await _connection.SendAsync(new IdleCommand("stored_playlist playlist player mixer output options"));

                        if (idleChanges.IsResponseValid)
                            await HandleIdleResponseAsync(idleChanges.Response.Content);
                        else
                            throw new Exception(idleChanges.Response?.Content);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in Idle connection thread: {e.Message}");
                    }
                }

            }).ConfigureAwait(false);
        }

        private async Task HandleIdleResponseAsync(string subsystems)
        {
            if (subsystems.Contains("player") || subsystems.Contains("mixer") || subsystems.Contains("output") || subsystems.Contains("options"))
            {
                await UpdateStatusAsync();

                if (subsystems.Contains("player"))
                {
                    await UpdateSongAsync();
                }
            }
        }

        bool _isUpdatingStatus = false;
        private async Task UpdateStatusAsync()
        {
            if (_connection == null) return;

            if (_isUpdatingStatus) return;
            _isUpdatingStatus = true;

            try
            {
                MpcNET.Message.IMpdMessage<MpdStatus> response = await _connection.SendAsync(new StatusCommand());
                if (response != null && response.IsResponseValid)
                {
                    CurrentStatus = response.Response.Content;
                    UpdateStatus();
                }
                else
                    throw new Exception();
            }
            catch
            {
                await Connect(cancelToken.Token);
            }
            _isUpdatingStatus = false;
        }

        bool _isUpdatingSong = false;
        private async Task UpdateSongAsync()
        {
            if (_connection == null) return;

            if (_isUpdatingSong) return;
            _isUpdatingSong = true;

            try
            {
                MpcNET.Message.IMpdMessage<MpcNET.Types.IMpdFile> response = await _connection.SendAsync(new CurrentSongCommand());
                if (response != null && response.IsResponseValid)
                {
                    CurrentSong = response.Response.Content;
                    UpdateSong();
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                await Connect(cancelToken.Token);
            }
            _isUpdatingSong = false;
        }

        public async Task<T> SafelySendCommandAsync<T>(IMpcCommand<T> command)
        {
            try
            {
                var response = await _commandConnection.SendAsync(command);
                if (!response.IsResponseValid)
                {
                    // If we have an MpdError string, only show that as the error to avoid extra noise
                    var mpdError = response.Response?.Result?.MpdError;
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

        private async void GetAlbumBitmap(string path, CancellationToken token = default)
        {
            List<byte> data = new List<byte>();
            try
            {
                if (_connection == null) // We got cancelled
                    return;

                long totalBinarySize = 9999;
                long currentSize = 0;

                do
                {
                    var albumReq = await _connection.SendAsync(new AlbumArtCommand(path, currentSize));
                    if (!albumReq.IsResponseValid) break;

                    var response = albumReq.Response.Content;
                    if (response.Binary == 0) break; // MPD isn't giving us any more data, let's roll with what we have.

                    totalBinarySize = response.Size;
                    currentSize += response.Binary;
                    data.AddRange(response.Data);
                    Debug.WriteLine($"Downloading albumart: {currentSize}/{totalBinarySize}");
                } while (currentSize < totalBinarySize && !token.IsCancellationRequested);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception caught while getting albumart: " + e);
                return;
            }

            if (data.Count == 0)
            {
                Trace.WriteLine("empty cover");
                _cover = null;
                return;
            }

            using var stream = new MemoryStream(data.ToArray());
            _cover = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            UpdateCover();
        }

        public void UpdateSong()
        {
            if (!_connected)
                return;
            if (CurrentSong == null)
                return;

            Trace.WriteLine($"[DEBUG] Song: name[{CurrentSong.Title}] artist[{CurrentSong.Artist}] album[{CurrentSong.Album}] uri[{CurrentSong.Path}]");

            _currentTime = CurrentStatus.Elapsed.TotalSeconds;
            _totalTime = CurrentSong.Time;
            if (!_elapsedTimer.Enabled)
                _elapsedTimer.Start();

            string uri = Regex.Escape(CurrentSong.Path);
            GetAlbumBitmap(uri);
        }

        public void UpdateCover()
        {

        }

        public void UpdateStatus()
        {
            if (!_connected)
                return;

            if (CurrentStatus == null)
                return;

            _currentRandom = CurrentStatus.Random;
            _currentRepeat = CurrentStatus.Repeat;
            _currentConsume = CurrentStatus.Consume;
            _currentSingle = CurrentStatus.Single;
            _currentVolume = CurrentStatus.Volume;

            Trace.WriteLine($"[DEBUG] Status: volume[{CurrentStatus.Volume}] random[{CurrentStatus.Random}] consume[{CurrentStatus.Consume}] State[{CurrentStatus.State}] Bitrate[{CurrentStatus.Bitrate}]");
        }

        public MpcNET.Types.IMpdFile GetCurrentSong() => CurrentSong;
        public MpdStatus GetStatus() => CurrentStatus;
        public BitmapFrame GetCover() => _cover;
        public string GetVersion() => _version;

        public async void Prev()
        {
            await SafelySendCommandAsync(new PreviousCommand());
        }

        public async void Next()
        {
            await SafelySendCommandAsync(new NextCommand());
        }

        public async void PlayPause()
        {
            await SafelySendCommandAsync(new PauseResumeCommand());
        }

        public async void Random()
        {
            await SafelySendCommandAsync(new RandomCommand(!_currentRandom));
        }

        public async void Repeat()
        {
            await SafelySendCommandAsync(new RepeatCommand(!_currentRepeat));
        }

        public async void Single()
        {
            await SafelySendCommandAsync(new SingleCommand(!_currentSingle));
        }

        public async void Consume()
        {
            var response2 = await SafelySendCommandAsync(new ConsumeCommand(!_currentConsume));
        }

        public async void SetVolume(int value)
        {
            await SafelySendCommandAsync(new SetVolumeCommand((byte)value));
        }

        public async void SetTime(double value)
        {
            await SafelySendCommandAsync(new SeekCurCommand(value));
        }

        public bool IsPlaying()
        {
            return CurrentStatus?.State == MpdState.Play;
        }
    }
}
