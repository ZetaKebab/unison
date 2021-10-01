using RadioBrowser;
using RadioBrowser.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;

namespace unison
{
    public class StationView : StationInfo
    {
        public StationView(string _name, Uri _url, int _bitrate, string _codec, string _country, Uri _favicon)
        {
            Name = _name;
            Url = _url;
            Bitrate = _bitrate;
            Codec = _codec;
            CountryCode = _country;
            Favicon = _favicon;
        }

        public override string ToString()
        {
            return $"{this.Name} - {this.Bitrate} - {this.Codec} - {this.CountryCode}";
        }
    }

    public partial class Radios : Window
    {
        RadioBrowserClient _radioBrowser;
        List<StationView> _stations = new List<StationView>();

        private MPDHandler _mpd;

        public Radios()
        {
            InitializeComponent();
            _radioBrowser = new RadioBrowserClient();
        }

        public async Task Search(string name)
        {
            var searchByName = await _radioBrowser.Search.ByNameAsync(name);
            Debug.WriteLine(searchByName.FirstOrDefault()?.Name);
            Debug.WriteLine("");
        }
    
        public async Task SearchAdvanced(string name)
        {
            Debug.Write(name);

            _stations.Clear();
            var advancedSearch = await _radioBrowser.Search.AdvancedAsync(new AdvancedSearchOptions
            {
                Name = name
            });
            foreach (var station in advancedSearch)
                _stations.Add(new StationView(station.Name, station.Url, station.Bitrate, station.Codec, station.CountryCode, station.Favicon));
            lvDataBinding.ItemsSource = _stations;
            ICollectionView view = CollectionViewSource.GetDefaultView(_stations);
            view.Refresh();

            Debug.WriteLine(_stations[0].Url.AbsoluteUri);
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("Selected: {0}", e.AddedItems[0]);

            _mpd = (MPDHandler)Application.Current.Properties["mpd"];
            _mpd.ClearQueue();

            StationView station = (StationView)e.AddedItems[0];
            _mpd.AddSong(station.Url.AbsoluteUri);
            _mpd.PlayCommand();
        }

        public List<StationView> GetStations()
        {
            return _stations;
        }

        private async void Search_Clicked(object sender, RoutedEventArgs e)
        {
            await SearchAdvanced(SearchBar.Text);
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
