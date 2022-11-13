using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MpcNET;
using MpcNET.Commands.Database;
using MpcNET.Commands.Playback;
using MpcNET.Commands.Queue;
using MpcNET.Commands.Reflection;
using MpcNET.Tags;
using MpcNET.Types;
using MpcNET.Types.Filters;

namespace unison
{
    class ShuffleHandler
    {
        private readonly MPDHandler _mpd;
        public int AddedSongs = 0;

        public List<string> SongList { get; }

        public ShuffleHandler()
        {
            SongList = new();
            _mpd = (MPDHandler)Application.Current.Properties["mpd"];
        }

        public async Task GetSongsFromFilter(List<IFilter> filter, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            SongList.Clear();

            int song = _mpd.GetStats().Songs;
            IEnumerable<IMpdFile> response = await _mpd.SafelySendCommandAsync(new SearchCommand(filter, 0, song + 1));
            foreach (IMpdFile file in response)
                SongList.Add(file.Path);
        }

        public async Task AddToQueueRandom(int SongNumber, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            int AddedSongs = 0;

            var commandList = new CommandList();
            int songTotal = _mpd.GetStats().Songs;

            for (int i = 0; i < SongNumber; i++)
            {
                int song = new Random().Next(0, songTotal - 1);
                commandList.Add(new SearchAddCommand(new FilterTag(MpdTags.Title, "", FilterOperator.Contains), song, song + 1));
                AddedSongs++;

                // play if stopped or unknown state (no queue managing at the moment, so mandatory)
                if (i == 0 && (_mpd.GetStatus().State != MpdState.Play && _mpd.GetStatus().State != MpdState.Pause))
                    commandList.Add(new PlayCommand(0));
            }

            await _mpd.SafelySendCommandAsync(commandList);
        }

        public async Task AddToQueueFilter(int SongNumber, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            int AddedSongs = 0;

            // more (or equal) requested songs than available => add everything
            if (SongNumber >= SongList.Count)
            {
                var commandList = new CommandList();
                foreach (string path in SongList)
                {
                    commandList.Add(new AddCommand(path));
                    AddedSongs++;
                }

                await _mpd.SafelySendCommandAsync(commandList);
            }
            // more available songs than requested =>
            // we add unique indexes until we reach the requested amount
            else
            {
                HashSet<int> SongIndex = new();
                while (SongIndex.Count < SongNumber)
                {
                    int MaxIndex = new Random().Next(0, SongList.Count - 1);
                    SongIndex.Add(MaxIndex);
                }

                var commandList = new CommandList();
                foreach (int index in SongIndex)
                {
                    commandList.Add(new AddCommand(SongList[index]));
                    AddedSongs++;
                }

                await _mpd.SafelySendCommandAsync(commandList);
            }
        }
    }
}