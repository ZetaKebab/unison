﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MpcNET.Commands.Database;
using MpcNET.Tags;
using MpcNET.Types;
using MpcNET.Types.Filters;

namespace unison
{
    public partial class Shuffle : Window
    {
        private readonly MPDHandler _mpd;
        private readonly ShuffleHandler _shuffle;

        List<string> GenreList { get; }
        List<string> FolderList { get; }
        List<IFilter> Filters { get; }

        bool _continuous = false;

        public Shuffle()
        {
            InitializeComponent();
            GenreList = new();
            FolderList = new();
            Filters = new();
            SongFilterNumber.Text = "0";

            _mpd = (MPDHandler)Application.Current.Properties["mpd"];
            _shuffle = (ShuffleHandler)Application.Current.Properties["shuffle"];
        }

        public void Initialize()
        {
            ListGenre(_mpd._cancelCommand.Token);
            ListFolder(_mpd._cancelCommand.Token);
        }

        public async void ListGenre(CancellationToken token)
        {
            if (GenreList.Count != 0)
                return;

            if (token.IsCancellationRequested)
                return;

            List<string> Response = await _mpd.SafelySendCommandAsync(new ListCommand(MpdTags.Genre, null, null));

            if (Response == null)
                return;

            foreach (string genre in Response)
                GenreList.Add(genre);
        }

        public async void ListFolder(CancellationToken token)
        {
            if (FolderList.Count != 0)
                return;

            if (token.IsCancellationRequested)
                return;

            IEnumerable<IMpdFilePath> Response = await _mpd.SafelySendCommandAsync(new LsInfoCommand(""));

            if (Response == null)
                return;

            foreach (IMpdFilePath folder in Response)
                FolderList.Add(folder.Name);
        }

        private bool IsFilterEmpty()
        {
            if (Filters.Count() == 0)
                return true;
            return false;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);

            if (parent == null)
                return null;

            if (parent is T)
                return parent as T;
            else
                return FindParent<T>(parent);
        }

        private void AddFilter_Clicked(object sender, RoutedEventArgs e)
        {
            FilterPanel.Children.Add(new ContentPresenter { ContentTemplate = (DataTemplate)FindResource("FilterPanel") });
            SongFilterNumber.Text = "0";
        }

        private void RemoveFilter_Clicked(object sender, RoutedEventArgs e)
        {
            if (FilterPanel.Children.Count > 1)
                FilterPanel.Children.Remove(FindParent<ContentPresenter>(sender as Button));
            else
                Reset_Clicked(null, null);
            SongFilterNumber.Text = "0";
        }

        private void Reset_Clicked(object sender, RoutedEventArgs e)
        {
            FilterPanel.Children.RemoveRange(0, FilterPanel.Children.Count);
            FilterPanel.Children.Add(new ContentPresenter { ContentTemplate = (DataTemplate)FindResource("FilterPanel") });
            SongFilterNumber.Text = "0";
            _shuffle.SongList.Clear();
        }

        private ITag FilterEquivalence_Type(string value)
        {
            if (value == "Song")
                return MpdTags.Title;
            else if (value == "Artist")
                return MpdTags.Artist;
            else if (value == "Album")
                return MpdTags.Album;
            else if (value == "Year")
                return MpdTags.Date;
            else if (value == "Genre")
                return MpdTags.Genre;
            return MpdTags.Title;
        }

        private FilterOperator FilterEquivalence_Operator(string value)
        {
            if (value == "contains")
                return FilterOperator.Contains;
            else if (value == "is")
                return FilterOperator.Equal;
            else if (value == "is not")
                return FilterOperator.Different;
            return FilterOperator.Equal;
        }

        private void FilterType_Change(object sender, string Operator, List<string> Listing)
        {
            ComboBox comboBox = sender as ComboBox;
            StackPanel stackPanel = comboBox.Parent as StackPanel;
            foreach (ComboBox child in stackPanel.Children.OfType<ComboBox>())
            {
                if (child.Name == "FilterOperator")
                {
                    child.ItemsSource = (Array)FindResource(Operator);
                    child.SelectedItem = child.Items[0];
                }

                if (child.Name == "FilterList")
                {
                    child.Visibility = Visibility.Visible;
                    child.ItemsSource = Listing;
                    child.SelectedItem = child.Items[0];
                }
            }
            foreach (TextBox child in stackPanel.Children.OfType<TextBox>())
            {
                if (child.Name == "FilterValue")
                    child.Visibility = Visibility.Collapsed;
            }
            SongFilterNumber.Text = "0";
        }

        private void FilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string item = e.AddedItems[0].ToString();
            if (item == "Genre")
                FilterType_Change(sender, "OperatorTypeB", GenreList);
            else if (item == "Directory")
                FilterType_Change(sender, "OperatorTypeC", FolderList);
            else
            {
                ComboBox combobox = sender as ComboBox;
                StackPanel stackpanel = combobox.Parent as StackPanel;
                foreach (ComboBox child in stackpanel.Children.OfType<ComboBox>())
                {
                    if (child.Name == "FilterOperator")
                    {
                        child.ItemsSource = (Array)FindResource("OperatorTypeA");
                        child.SelectedItem = child.Items[0];
                    }

                    if (child.Name == "FilterList")
                        child.Visibility = Visibility.Collapsed;
                }
                foreach (TextBox child in stackpanel.Children.OfType<TextBox>())
                {
                    if (child.Name == "FilterValue")
                        child.Visibility = Visibility.Visible;
                }
            }
            SongFilterNumber.Text = "0";
        }

        private void OperatorType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SongFilterNumber.Text = "0";
        }

        private async void UpdateFilter_Clicked(object sender, RoutedEventArgs e)
        {
            QueryFilterText.Visibility = Visibility.Visible;
            await UpdateFilter();

            QueryFilterText2.Visibility = Visibility.Visible;

            TimedText(QueryFilterText, 1);
            TimedText(QueryFilterText2, 1);
        }

        private void TimedText(TextBlock textBlock, int time)
        {
            DispatcherTimer Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(time);
            Timer.Tick += (sender, args) =>
            {
                Timer.Stop();
                textBlock.Visibility = Visibility.Collapsed;
            };
            Timer.Start();
        }

        private async Task UpdateFilter()
        {
            Filters.Clear();

            foreach (ContentPresenter superChild in FilterPanel.Children)
            {
                ITag tag = MpdTags.Title;
                FilterOperator op = FilterOperator.None;
                string value = "";
                bool isDir = false;

                StackPanel stackPanel = VisualTreeHelper.GetChild(superChild, 0) as StackPanel;
                foreach (TextBox child in stackPanel.Children.OfType<TextBox>())
                {
                    if (child.Name == "FilterValue")
                        value = child.Text;
                }

                foreach (ComboBox child in stackPanel.Children.OfType<ComboBox>())
                {
                    if (child.Name == "FilterType")
                    {
                        if (child.SelectedItem.ToString() == "Directory")
                            isDir = true;
                        else
                            tag = FilterEquivalence_Type(child.SelectedItem.ToString());
                    }

                    if (child.Name == "FilterOperator")
                        op = FilterEquivalence_Operator(child.SelectedItem.ToString());

                    if (child.Name == "FilterList" && child.Visibility == Visibility.Visible)
                        value = child.SelectedItem.ToString();
                }

                if (value != "")
                {
                    if (!isDir)
                        Filters.Add(new FilterTag(tag, value, op));
                    else
                        Filters.Add(new FilterBase(value, FilterOperator.None));

                    await Task.Run(async () =>
                    {
                        await _shuffle.GetSongsFromFilter(Filters, _mpd._cancelCommand.Token);
                    });
                    SongFilterPanel.Visibility = Visibility.Visible;
                    SongFilterNumber.Text = _shuffle.SongList.Count.ToString();
                }
            }
        }

        private void QueryFilterHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                UpdateFilter_Clicked(null, null);
        }

        private void QueueValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void QueueValidationNumber()
        {
            int Number;
            try
            {
                Number = int.Parse(SongNumber.Text);
            }
            catch (Exception)
            {
                return;
            }

            if (Number < 1)
                SongNumber.Text = "1";
            if (IsFilterEmpty())
            {
                if (Number > 100)
                    SongNumber.Text = "100";
            }
            else
            {
                if (Number > 1000)
                    SongNumber.Text = "1000";
            }
        }

        private async void AddToQueue()
        {
            if (_mpd.GetStats() == null)
                return;

            await UpdateFilter();
            QueueValidationNumber();

            NumberAddedSongs.Text = SongNumber.Text;
            SearchStatus.Visibility = Visibility.Visible;

            int Num = int.Parse(SongNumber.Text);
            await AddToQueue_Internal(Num);

            SearchStatus2.Visibility = Visibility.Visible;

            TimedText(SearchStatus, 2);
            TimedText(SearchStatus2, 2);
        }

        private async Task AddToQueue_Internal(int Num)
        {
            if (IsFilterEmpty())
            {
                await Task.Run(async () =>
                {
                    await _shuffle.AddToQueueRandom(Num, _mpd._cancelCommand.Token);
                });
            }
            else
            {
                await Task.Run(async () =>
                {
                    await _shuffle.AddToQueueFilter(Num, _mpd._cancelCommand.Token);
                });
            }
        }

        private void AddToQueueHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                AddToQueue();
        }

        private void AddToQueue_Clicked(object sender, RoutedEventArgs e)
        {
            AddToQueue();
        }

        public bool GetContinuous()
        {
            return _continuous;
        }

        public async Task HandleContinuous()
        {
            if (!_continuous)
                return;

            int PlaylistLength = _mpd.GetStatus().PlaylistLength;
            int Num = 10 - PlaylistLength;
            if (Num < 1)
                return;

            await UpdateFilter();           
            await AddToQueue_Internal(Num);
        }

        private async void ContinuousShuffle_Checked(object sender, RoutedEventArgs e)
        {
            if (ContinuousShuffle.IsChecked == true)
                _continuous = true;
            else
                _continuous = false;

            if (_mpd.GetStatus().PlaylistLength < 10)
                await HandleContinuous();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
        }

        public void InitHwnd()
        {
            WindowInteropHelper helper = new(this);
            helper.EnsureHandle();
        }
    }
}