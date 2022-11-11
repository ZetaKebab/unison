using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RadioBrowser;
using RadioBrowser.Models;

namespace unison.Handlers
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

    internal class RadioHandler
    {
        private readonly RadioBrowserClient _radioBrowser;
        private readonly bool _connected = true;

        public bool IsConnected() => _connected;


        public RadioHandler()
        {
            try
            {
                _radioBrowser = new RadioBrowserClient();
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception while connecting to RadioBrowser: " + e.Message);
                return;
            }

            _connected = true;
        }

        public async Task<List<NameAndCount>> GetCountries()
        {
            return await _radioBrowser.Lists.GetCountriesAsync();
        }

        public async Task<List<StationInfo>> AdvancedSearch(AdvancedSearchOptions options)
        {

            return await _radioBrowser.Search.AdvancedAsync(options);
        }
    }
}
