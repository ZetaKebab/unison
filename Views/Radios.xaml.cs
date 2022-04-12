using RadioBrowser;
using RadioBrowser.Models;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;

namespace unison
{
    public class CountryListItem
    {
        public uint Count { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            if (Name == "")
                return "None";
            return $"{Name} ({Count})";
        }
    }

    public class StationListItem
    {
        public string Name { get; set; }
        public string Codec { get; set; }
        public string Tags { get; set; }
        public int Bitrate { get; set; }
        public Uri Url { get; set; }

        private string _country;
        public string Country
        {
            get
            {
                if (_country.Length == 0)
                    return "🏴‍☠️";
                return string.Concat(_country.ToUpper().Select(x => char.ConvertFromUtf32(x + 0x1F1A5))); // return emoji
            }
            set
            {
                _country = value;
            }
        }
    }

    public partial class Radios : Window
    {
        private RadioBrowserClient _radioBrowser;
        private MPDHandler _mpd;

        public Radios()
        {
            InitializeComponent();

            try
            {
                _radioBrowser = new RadioBrowserClient();
                Initialize();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while connecting to RadioBrowser: " + e.Message);
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow MainWin = (MainWindow)Application.Current.MainWindow;
                MainWin.OnRadioBrowserConnected();
            });
        }

        public async void Initialize()
        {
            try
            {
                List<NameAndCount> Countries = await _radioBrowser.Lists.GetCountriesAsync();
                CountryList.Items.Add(new CountryListItem { Name = "", Count = 0 });

                foreach (NameAndCount Country in Countries)
                {
                    CountryList.Items.Add(new CountryListItem
                    {
                        Name = Country.Name,
                        Count = Country.Stationcount
                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while getting countries in RadioBrowser: " + e.Message);
                return;
            }
        }

        private string CleanString(string str)
        {
            return str.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
        }

        public async Task SearchAdvanced(string name, string country, string tags)
        {
            try
            {
                SearchStatus.Text = unison.Resources.Resources.Radio_Loading;

                List<StationInfo> advancedSearch = await _radioBrowser.Search.AdvancedAsync(new AdvancedSearchOptions
                {
                    Name = name,
                    Country = country,
                    TagList = tags
                });

                RadioListGrid.Items.Clear();
                if (advancedSearch.Count > 0)
                {
                    SearchStatus.Text = "";
                    foreach (StationInfo station in advancedSearch)
                    {
                        RadioListGrid.Items.Add(new StationListItem
                        {
                            Name = CleanString(station.Name),
                            Country = station.CountryCode,
                            Codec = station.Codec,
                            Bitrate = station.Bitrate,
                            Url = station.Url,
                            Tags = string.Join(", ", station.Tags)
                        });
                    }
                    FitToContent();
                }
                else
                    SearchStatus.Text = unison.Resources.Resources.Radio_NotFound;
            }
            catch (Exception except)
            {
                Debug.WriteLine("Error on RadioBrowser search advanced: " + except.Message);
            }
        }

        private void FitToContent()
        {
            foreach (DataGridColumn column in RadioListGrid.Columns)
                column.Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid grid = sender as DataGrid;
            StationListItem station;
            try
            {
                station = grid.Items[grid.SelectedIndex] as StationListItem;
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.WriteLine("Error: Invalid index.");
                return;
            }

            if (station.Url == null)
            {
                Debug.WriteLine("Error: Invalid station.");
                return;
            }

            _mpd = (MPDHandler)Application.Current.Properties["mpd"];
            _mpd.ClearAddAndPlay(station.Url.AbsoluteUri);
        }

        private async void Search_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                CountryListItem a = (CountryListItem)CountryList.SelectedItem;
                await SearchAdvanced(NameSearch.Text, a?.Name, TagSearch.Text);
            }
            catch (Exception except)
            {
                Debug.WriteLine("Error on RadioBrowser search: " + except.Message);
            }
        }

        private void Reset_Clicked(object sender, RoutedEventArgs e)
        {
            NameSearch.Text = "";
            TagSearch.Text = "";
            CountryList.SelectedIndex = 0;
        }

        private void SearchHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Search_Clicked(null, null);
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