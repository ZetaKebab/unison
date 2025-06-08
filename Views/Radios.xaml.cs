using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using unison.Handlers;
using RadioBrowser.Models;

namespace unison
{
    public partial class Radios : Window
    {
        private MPDHandler _mpd;
        private RadioHandler _radio;

        public bool IsConnected() => _radio.IsConnected();

        public Radios()
        {
            InitializeComponent();
            Initialize();
            InitHwnd();

            WindowState = WindowState.Minimized;

            Top = Properties.Settings.Default.RadiosWindowTop;
            Left = Properties.Settings.Default.RadiosWindowLeft;
        }

        public async void Initialize()
        {
            _radio = new RadioHandler();

            if (!_radio.IsConnected())
                SearchButton.IsEnabled = false;

            try
            {
                CountryList.Items.Add(new CountryListItem { Name = "", Count = 0 });

                List<NameAndCount> Countries = await _radio.GetCountries();
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
                Trace.WriteLine("Exception while getting countries in RadioBrowser: " + e.Message);
                return;
            }
        }

        private static string CleanString(string str)
        {
            return str.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
        }

        private void SearchHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Search_Clicked(null, null);
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
                Trace.WriteLine("Error on RadioBrowser search: " + except.Message);
            }
        }

        public async Task SearchAdvanced(string name, string country, string tags)
        {
            try
            {
                SearchStatus.Text = unison.Resources.Resources.Radio_Loading;

                List<StationInfo> advancedSearch = await Task.Run(async () =>
                {
                    return await _radio.AdvancedSearch(new AdvancedSearchOptions
                    {
                        Name = name,
                        Country = country,
                        TagList = tags
                    });
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
                Trace.WriteLine("Error on RadioBrowser search advanced: " + except.Message);
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
                Trace.WriteLine("Error: Invalid index.");
                return;
            }

            if (station.Url == null)
            {
                Trace.WriteLine("Error: Invalid station.");
                return;
            }

            _mpd = (MPDHandler)Application.Current.Properties["mpd"];
            _mpd.ClearAddAndPlay(station.Url.AbsoluteUri);
        }

        private void Reset_Clicked(object sender, RoutedEventArgs e)
        {
            NameSearch.Text = "";
            TagSearch.Text = "";
            CountryList.SelectedIndex = 0;
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

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.RadiosWindowTop = Top;
            Properties.Settings.Default.RadiosWindowLeft = Left;
            Properties.Settings.Default.Save();
        }
    }
}