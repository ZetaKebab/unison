using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace unison
{
    public partial class Settings : Window
    {
        Key _pressedKey = Key.None;

        public static string GetVersion => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public static string GetLicense
        {
            get
            {
                try
                {
                    StreamReader Reader = new("LICENSE");
                    string file = "";
                    file += Reader.ReadToEnd();
                    return file;
                }
                catch (IOException e)
                {
                    return e.Message;
                }
            }
        }

        public Settings()
        {
            InitHwnd();
            InitializeComponent();
            DataContext = this;

            WindowState = WindowState.Minimized;

            MpdHost.Text = Properties.Settings.Default.mpd_host;
            MpdPort.Text = Properties.Settings.Default.mpd_port.ToString();
            //MpdPassword.Text = Properties.Settings.Default.mpd_password;
            SnapcastStartup.IsChecked = Properties.Settings.Default.snapcast_startup;
            SnapcastWindow.IsChecked = Properties.Settings.Default.snapcast_window;
            SnapcastPath.Text = Properties.Settings.Default.snapcast_path;
            SnapcastPort.Text = Properties.Settings.Default.snapcast_port.ToString();
            VolumeOffset.Text = Properties.Settings.Default.volume_offset.ToString();

            SetupShortcuts();
        }

        private void SetupShortcuts()
        {
            var MODValues = System.Enum.GetValues(typeof(HotkeyHandler.MOD));

            System.Collections.Generic.IEnumerable<StackPanel> stackPanelCollection = RebindKeyWrapper.Children.OfType<StackPanel>();
            foreach (StackPanel stackPanel in stackPanelCollection)
            {
                if (stackPanel.Name.Contains("Shortcut"))
                {
                    HotkeyHandler.HotkeyPair hotkey = GetHotkeyVariable(stackPanel.Name);
                    System.Collections.Generic.IEnumerable<ComboBox> comboBoxCollection = stackPanel.Children.OfType<ComboBox>();

                    HotkeyHandler.MOD[] MODList = System.Enum.GetValues(typeof(HotkeyHandler.MOD))
                        .OfType<HotkeyHandler.MOD>()
                        .Select(x => x & (HotkeyHandler.MOD)hotkey.GetMOD())
                        .Where(x => x != HotkeyHandler.MOD.None)
                        .ToArray();

                    foreach (ComboBox comboBox in comboBoxCollection)
                    {
                        foreach (var value in MODValues)
                        {
                            comboBox.Items.Add(value.ToString().ToLower());
                            comboBox.SelectedItem = comboBox.Items[0];
                            comboBox.FontWeight = FontWeights.Light;
                        }
                    }

                    for (int i = 0; i < comboBoxCollection.ToArray().Length; i++)
                    {
                        if (i < MODList.Length)
                        {
                            comboBoxCollection.ToArray()[i].SelectedItem = MODList[i].ToString().ToLower();
                            comboBoxCollection.ToArray()[i].FontWeight = FontWeights.Bold;
                        }
                    }

                    System.Collections.Generic.IEnumerable<Button> buttonCollection = stackPanel.Children.OfType<Button>();
                    TextBlock textblock = (TextBlock)buttonCollection.First().Content;
                    textblock.Text = ((HotkeyHandler.VK)hotkey.GetVK()).ToString();
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo psi = new(e.Uri.AbsoluteUri);
            psi.UseShellExecute = true;
            Process.Start(psi);
            e.Handled = true;
        }

        public void UpdateConnectionStatus()
        {
            MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
            if (mpd.IsConnected())
                ConnectionStatus.Text = $"{unison.Resources.Resources.Settings_ConnectionStatusConnected} {mpd.GetVersion()}.";
            else
                ConnectionStatus.Text = unison.Resources.Resources.Settings_ConnectionStatusOffline;
        }

        private void MPDConnect_Clicked(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            ConnectionStatus.Text = unison.Resources.Resources.Settings_ConnectionStatusConnecting;
            MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
            mpd.Connect();
        }

        private void SnapcastReset_Clicked(object sender, RoutedEventArgs e)
        {
            SnapcastPath.Text = (string)Application.Current.FindResource("snapcastPath");
            SnapcastPort.Text = (string)Application.Current.FindResource("snapcastPort");
        }

        private void HotkeyChanged()
        {
            HotkeyHandler hotkeys = (HotkeyHandler)Application.Current.Properties["hotkeys"];
            hotkeys.RemoveHotkeys();
            hotkeys.AddHotkeys();
        }

        private ref HotkeyHandler.HotkeyPair GetHotkeyVariable(string Name)
        {
            HotkeyHandler hotkeys = (HotkeyHandler)Application.Current.Properties["hotkeys"];

            if (Name == "Shortcut_NextTrack")
                return ref hotkeys._NextTrack;
            if (Name == "Shortcut_PreviousTrack")
                return ref hotkeys._PreviousTrack;
            if (Name == "Shortcut_PlayPause")
                return ref hotkeys._PlayPause;
            if (Name == "Shortcut_VolumeUp")
                return ref hotkeys._VolumeUp;
            if (Name == "Shortcut_VolumeDown")
                return ref hotkeys._VolumeDown;
            if (Name == "Shortcut_VolumeMute")
                return ref hotkeys._VolumeMute;
            if (Name == "Shortcut_ShowWindow")
                return ref hotkeys._ShowWindow;
            return ref hotkeys._NextTrack;
        }

        private void UpdateHotkey_MOD(string Name, HotkeyHandler.MOD mod) => GetHotkeyVariable(Name).SetMOD(mod);
        private void UpdateHotkey_VK(string Name, HotkeyHandler.VK vk) => GetHotkeyVariable(Name).SetVK(vk);

        private void MOD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ComboBox combobox = (ComboBox)sender;
            StackPanel stackpanel = (StackPanel)combobox.Parent;
            System.Collections.Generic.IEnumerable<ComboBox> collection = stackpanel.Children.OfType<ComboBox>();

            HotkeyHandler.MOD MOD1, MOD2;

            // we need to do this because the element is modified -after- this function
            if (combobox.Tag.ToString() == "MOD1")
            {
                MOD1 = GetMOD(e.AddedItems[0].ToString());
                MOD2 = GetMOD(collection.Last().Text);
            }
            else
            {
                MOD1 = GetMOD(collection.First().Text);
                MOD2 = GetMOD(e.AddedItems[0].ToString());
            }

            if (e.AddedItems[0].ToString() == "none")
                combobox.FontWeight = FontWeights.Light;
            else
                combobox.FontWeight = FontWeights.Bold;

            HotkeyHandler.MOD ModKey = MOD1 | MOD2;

            UpdateHotkey_MOD(stackpanel.Name, ModKey);
            HotkeyChanged();
        }

        private void RemapKey_Clicked(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            TextBlock textBlock = (TextBlock)button.Content;
            textBlock.Text = "Enter key...";
            button.PreviewKeyDown += DetectPressedKey;
        }

        private void DetectPressedKey(object sender, KeyEventArgs e)
        {
            e.Handled = true; // not enough to handle media keys triggering in this setup

            _pressedKey = e.Key;
            HotkeyHandler.VK VirtualKey = GetVirtualKey(_pressedKey);
            if (VirtualKey == HotkeyHandler.VK.None)
                _pressedKey = Key.None;

            Button button = (Button)sender;
            TextBlock textBlock = (TextBlock)button.Content;
            StackPanel stackPanel = (StackPanel)button.Parent;

            textBlock.Text = _pressedKey.ToString();
            button.PreviewKeyDown -= DetectPressedKey;

            UpdateHotkey_VK(stackPanel.Name, VirtualKey);
            HotkeyChanged();
        }

        private HotkeyHandler.VK GetVirtualKey(Key key)
        {
            var values = System.Enum.GetValues(typeof(HotkeyHandler.VK));
            foreach (object value in values)
            {
                if (key.ToString().ToLower() == value.ToString().ToLower())
                    return (HotkeyHandler.VK)value;
            }
            return HotkeyHandler.VK.None;
        }

        private HotkeyHandler.MOD GetMOD(string str)
        {
            var values = System.Enum.GetValues(typeof(HotkeyHandler.MOD));
            foreach (object value in values)
            {
                if (str.ToLower() == value.ToString().ToLower())
                    return (HotkeyHandler.MOD)value;
            }
            return HotkeyHandler.MOD.None;
        }

        private void ShortcutsReset_Clicked(object sender, RoutedEventArgs e)
        {
            // todo
        }

        public void UpdateStats()
        {
            MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
            StatSong.Text = mpd.GetStats().Songs.ToString();
            StatAlbum.Text = mpd.GetStats().Albums.ToString();
            StatArtist.Text = mpd.GetStats().Artists.ToString();
            StatTotalPlaytime.Text = mpd.GetStats().TotalPlaytime.ToString();
            StatUptime.Text = mpd.GetStats().Uptime.ToString();
            StatTotalTimePlayed.Text = mpd.GetStats().TotalTimePlayed.ToString();
            StatDatabaseUpdate.Text = mpd.GetStats().DatabaseUpdate.ToString();
        }



        public void SaveSettings()
        {
            Properties.Settings.Default.mpd_host = MpdHost.Text;
            Properties.Settings.Default.mpd_port = int.Parse(MpdPort.Text, CultureInfo.InvariantCulture);
            //Properties.Settings.Default.mpd_password = MpdPassword.Text;
            Properties.Settings.Default.snapcast_startup = (bool)SnapcastStartup.IsChecked;
            Properties.Settings.Default.snapcast_window = (bool)SnapcastWindow.IsChecked;
            Properties.Settings.Default.snapcast_path = SnapcastPath.Text;
            Properties.Settings.Default.snapcast_port = int.Parse(SnapcastPort.Text, CultureInfo.InvariantCulture);
            Properties.Settings.Default.volume_offset = int.Parse(VolumeOffset.Text, CultureInfo.InvariantCulture);

            // todo
            Properties.Settings.Default.nextTrack_mod = (uint)(GetMOD(Shortcut_NextTrack.Children.OfType<ComboBox>().First().SelectedItem.ToString()) | GetMOD(Shortcut_NextTrack.Children.OfType<ComboBox>().Last().SelectedItem.ToString()));
            /*Properties.Settings.Default.nextTrack_vk) = (uint)GetVirtualKey(Shortcut_NextTrack.Children.OfType<Button>().First().)
            Properties.Settings.Default.previousTrack_mod;
            Properties.Settings.Default.previousTrack_vk);
            Properties.Settings.Default.playPause_mod;
            Properties.Settings.Default.playPause_vk);
            Properties.Settings.Default.volumeUp_mod;
            Properties.Settings.Default.volumeUp_vk);
            Properties.Settings.Default.volumeDown_mod;
            Properties.Settings.Default.volumeDown_vk);
            Properties.Settings.Default.volumeMute_mod;
            Properties.Settings.Default.volumeMute_vk);
            Properties.Settings.Default.showWindow_mod;
            Properties.Settings.Default.showWindow_vk);*/

            Properties.Settings.Default.Save();
        }

        private void ConnectHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                MPDConnect_Clicked(null, null);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            SaveSettings();
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