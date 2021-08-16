using System.Collections.Generic;
using System.Threading.Tasks;

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

        public MPDHandler()
        {
        }

        public async void Connect()
        {
            _connected = await _mpd.MpdCommandConnectionStart(Properties.Settings.Default.mpd_host, Properties.Settings.Default.mpd_port, Properties.Settings.Default.mpd_password);
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

        public void CheckStatus<T>(ref T a, T b)
        {
            if (Comparer<T>.Default.Compare(a, b) != 0)
                a = b;
        }

        public async void Loop()
        {
            if (!_connected)
                return;

            CommandResult status = await _mpd.MpdQueryStatus();
            //Trace.WriteLine(status.ResultText);
            await Task.Delay(5);
            if (status != null)
            {
                _currentStatus = _mpd.MpdStatus;

                CheckStatus(ref _currentVolume, _mpd.MpdStatus.MpdVolume);
                CheckStatus(ref _currentRandom, _mpd.MpdStatus.MpdRandom);
                CheckStatus(ref _currentRepeat, _mpd.MpdStatus.MpdRepeat);
                CheckStatus(ref _currentSingle, _mpd.MpdStatus.MpdSingle);
                CheckStatus(ref _currentConsume, _mpd.MpdStatus.MpdConsume);
                CheckStatus(ref _currentElapsed, _mpd.MpdStatus.MpdSongElapsed);
            }

            CommandResult song = await _mpd.MpdQueryCurrentSong();
            await Task.Delay(5);
            if (song != null)
                _currentSong = _mpd.MpdCurrentSong;
        }

        public SongInfoEx GetCurrentSong() => _currentSong;
        public Status GetStatus() => _currentStatus;

        public async void Prev()
        {
            await _mpd.MpdPlaybackPrev(_currentVolume);
        }

        public async void Next()
        {
            await _mpd.MpdPlaybackNext(_currentVolume);
        }

        public async void PlayPause()
        {
            if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Play)
                await _mpd.MpdPlaybackPause();
            else if (_mpd.MpdStatus.MpdState == Status.MpdPlayState.Pause)
                await _mpd.MpdPlaybackPlay(_currentVolume);
        }

        public async void Random()
        {
            await _mpd.MpdSetRandom(!_currentRandom);

        }

        public async void Repeat()
        {
            await _mpd.MpdSetRepeat(!_currentRepeat);

        }

        public async void Single()
        {
            await _mpd.MpdSetSingle(!_currentSingle);

        }

        public async void Consume()
        {
            await _mpd.MpdSetConsume(!_currentConsume);

        }

        public async void SetVolume(int value)
        {
            await _mpd.MpdSetVolume(value);

        }

        public bool IsPlaying() => _currentStatus?.MpdState == MPDCtrl.Models.Status.MpdPlayState.Play;
    }
}
