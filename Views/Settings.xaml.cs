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

        HotkeyHandler _hotkeys = (HotkeyHandler)Application.Current.Properties["hotkeys"];


        public Settings()
        {
            InitHwnd();
            InitializeComponent();
            DataContext = this;

            WindowState = WindowState.Minimized;

            Initialize();
        }

        void Initialize()
        {
            MpdHost.Text = Properties.Settings.Default.mpd_host;
            MpdPort.Text = Properties.Settings.Default.mpd_port.ToString();
            //MpdPassword.Text = Properties.Settings.Default.mpd_password;
            SnapcastStartup.IsChecked = Properties.Settings.Default.snapcast_startup;
            SnapcastWindow.IsChecked = Properties.Settings.Default.snapcast_window;
            SnapcastPath.Text = Properties.Settings.Default.snapcast_path;
            SnapcastPort.Text = Properties.Settings.Default.snapcast_port.ToString();
            VolumeOffset.Text = Properties.Settings.Default.volume_offset.ToString();

            InitializeShortcuts();
        }

        public void UpdateConnectionStatus()
        {
            MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
            if (mpd.IsConnected())
            {
                ConnectionStatus.Text = $"{unison.Resources.Resources.Settings_ConnectionStatusConnected} {mpd.GetVersion()}.";
                ConnectButton.IsEnabled = false;
            }
            else
            {
                ConnectionStatus.Text = unison.Resources.Resources.Settings_ConnectionStatusOffline;
                ConnectButton.IsEnabled = true;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void MpdConnectTextBox(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            if (textBox.Text == Properties.Settings.Default.mpd_host)
                ConnectButton.IsEnabled = false;
            else
                ConnectButton.IsEnabled = true;

            e.Handled = true;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo psi = new(e.Uri.AbsoluteUri);
            psi.UseShellExecute = true;
            Process.Start(psi);
            e.Handled = true;
        }

        private void MPDConnect_Clicked(object sender, RoutedEventArgs e)
        {
            if (!ConnectButton.IsEnabled)
                return;

            SaveSettings();

            MPDHandler mpd = (MPDHandler)Application.Current.Properties["mpd"];
            if (mpd.IsConnected())
                mpd = new MPDHandler();

            ConnectButton.IsEnabled = false;
            ConnectionStatus.Text = unison.Resources.Resources.Settings_ConnectionStatusConnecting;

            System.Threading.Tasks.Task.Run(() => { mpd.Connect(); });
        }

        private void SnapcastReset_Clicked(object sender, RoutedEventArgs e)
        {
            SnapcastPath.Text = (string)Application.Current.FindResource("snapcastPath");
            SnapcastPort.Text = (string)Application.Current.FindResource("snapcastPort");
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

            Properties.Settings.Default.nextTrack_mod = GetMod(Shortcut_NextTrack);
            Properties.Settings.Default.nextTrack_vk = GetVk(Shortcut_NextTrack);
            Properties.Settings.Default.previousTrack_mod = GetMod(Shortcut_PreviousTrack);
            Properties.Settings.Default.previousTrack_vk = GetVk(Shortcut_PreviousTrack);
            Properties.Settings.Default.playPause_mod = GetMod(Shortcut_PlayPause);
            Properties.Settings.Default.playPause_vk = GetVk(Shortcut_PlayPause);
            Properties.Settings.Default.volumeUp_mod = GetMod(Shortcut_VolumeUp);
            Properties.Settings.Default.volumeUp_vk = GetVk(Shortcut_VolumeUp);
            Properties.Settings.Default.volumeDown_mod = GetMod(Shortcut_VolumeDown);
            Properties.Settings.Default.volumeDown_vk = GetVk(Shortcut_VolumeDown);
            Properties.Settings.Default.volumeMute_mod = GetMod(Shortcut_VolumeMute);
            Properties.Settings.Default.volumeMute_vk = GetVk(Shortcut_VolumeMute);
            Properties.Settings.Default.showWindow_mod = GetMod(Shortcut_ShowWindow);
            Properties.Settings.Default.showWindow_vk = GetVk(Shortcut_ShowWindow);

            Properties.Settings.Default.Save();
        }


        // Hotkeys

        void InitializeShortcuts()
        {
            System.Collections.Generic.IEnumerable<StackPanel> stackPanelCollection = RebindKeyWrapper.Children.OfType<StackPanel>();
            StackPanel[] stackPanelList = stackPanelCollection.ToArray();

            // Default state
            for (int i = 0; i < stackPanelList.Length; i++)
            {
                ComboBox[] comboBoxList = stackPanelList[i].Children.OfType<ComboBox>().ToArray();
                foreach (ComboBox comboBox in comboBoxList) // default status (for reset)
                {
                    comboBox.FontWeight = FontWeights.Light;
                    comboBox.SelectedItem = "None";
                }
                TextBlock textBlock = (TextBlock)stackPanelList[i].Children.OfType<Button>().FirstOrDefault().Content;
                textBlock.Text = "None";
            }

            // Populate values
            for (int i = 0; i < stackPanelList.Length; i++)
            {
                // setup MOD
                HotkeyHandler.MOD mod = _hotkeys._Shortcuts[i].mod;

                HotkeyHandler.MOD[] MODList = System.Enum.GetValues(typeof(HotkeyHandler.MOD))
                    .OfType<HotkeyHandler.MOD>()
                    .Select(x => x & _hotkeys._Shortcuts[i].mod)
                    .Where(x => x != HotkeyHandler.MOD.None)
                    .ToArray();

                ComboBox[] comboBox = stackPanelList[i].Children.OfType<ComboBox>().ToArray();
                for (int j = 0; j < MODList.Length; j++)
                {
                    comboBox[j].SelectedItem = MODList[j].ToString();
                    if (comboBox[j].SelectedItem.ToString() != "None")
                        comboBox[j].FontWeight = FontWeights.Bold;
                }

                // setup VK
                TextBlock textBlock = (TextBlock)stackPanelList[i].Children.OfType<Button>().FirstOrDefault().Content;
                textBlock.Text = _hotkeys._Shortcuts[i].vk.ToString();
                if (textBlock.Text != "None")
                    textBlock.FontWeight = FontWeights.Bold;
            }
        }

        private void HotkeyChanged()
        {
            _hotkeys.RemoveHotkeys();
            _hotkeys.AddHotkeys();
        }

        private ref HotkeyHandler.HotkeyPair GetHotkeyVariable(string Name)
        {
            if (Name == "Shortcut_NextTrack")
                return ref _hotkeys._NextTrack;
            if (Name == "Shortcut_PreviousTrack")
                return ref _hotkeys._PreviousTrack;
            if (Name == "Shortcut_PlayPause")
                return ref _hotkeys._PlayPause;
            if (Name == "Shortcut_VolumeUp")
                return ref _hotkeys._VolumeUp;
            if (Name == "Shortcut_VolumeDown")
                return ref _hotkeys._VolumeDown;
            if (Name == "Shortcut_VolumeMute")
                return ref _hotkeys._VolumeMute;
            if (Name == "Shortcut_ShowWindow")
                return ref _hotkeys._ShowWindow;
            return ref _hotkeys._NextTrack;
        }

        private void UpdateHotkey_MOD(string Name, HotkeyHandler.MOD mod) => GetHotkeyVariable(Name).SetMOD(mod);
        private void UpdateHotkey_VK(string Name, HotkeyHandler.VK vk) => GetHotkeyVariable(Name).SetVK(vk);

        private void MOD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ComboBox comboBox = (ComboBox)sender;
            StackPanel stackPanel = (StackPanel)comboBox.Parent;
            System.Collections.Generic.IEnumerable<ComboBox> stackPanelCollection = stackPanel.Children.OfType<ComboBox>();

            HotkeyHandler.MOD MOD1, MOD2;

            // we need to do this because the element is modified -after- this function
            if (comboBox.Tag.ToString() == "MOD1")
            {
                MOD1 = GetMOD(e.AddedItems[0].ToString());
                MOD2 = GetMOD(stackPanelCollection.Last().Text);
            }
            else
            {
                MOD1 = GetMOD(stackPanelCollection.First().Text);
                MOD2 = GetMOD(e.AddedItems[0].ToString());
            }

            if (e.AddedItems[0].ToString() == "None")
                comboBox.FontWeight = FontWeights.Light;
            else
                comboBox.FontWeight = FontWeights.Bold;

            HotkeyHandler.MOD ModKey = MOD1 | MOD2;

            UpdateHotkey_MOD(stackPanel.Name, ModKey);
            HotkeyChanged();
        }

        private void RemapKey_Clicked(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            TextBlock textBlock = (TextBlock)button.Content;
            textBlock.Text = unison.Resources.Resources.Settings_ShortcutsKey;
            textBlock.FontWeight = FontWeights.Bold;
            button.PreviewKeyDown += DetectPressedKey;
        }

        private void DetectPressedKey(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            Key pressedKey = e.Key;
            HotkeyHandler.VK VirtualKey = GetVirtualKey(pressedKey);

            Button button = (Button)sender;
            TextBlock textBlock = (TextBlock)button.Content;
            StackPanel stackPanel = (StackPanel)button.Parent;

            if (VirtualKey == HotkeyHandler.VK.None)
            {
                pressedKey = Key.None;
                textBlock.FontWeight = FontWeights.Light;
            }
            else
                textBlock.FontWeight = FontWeights.Bold;

            textBlock.Text = pressedKey.ToString();
            button.PreviewKeyDown -= DetectPressedKey;

            UpdateHotkey_VK(stackPanel.Name, VirtualKey);
            HotkeyChanged();
        }

        private HotkeyHandler.VK GetVirtualKey(Key key)
        {
            foreach (object value in System.Enum.GetValues(typeof(HotkeyHandler.VK)))
            {
                if (key.ToString().ToLower() == value.ToString().ToLower())
                    return (HotkeyHandler.VK)value;
            }
            return HotkeyHandler.VK.None;
        }

        private HotkeyHandler.MOD GetMOD(string str)
        {
            foreach (object value in System.Enum.GetValues(typeof(HotkeyHandler.MOD)))
            {
                if (str.ToLower() == value.ToString().ToLower())
                    return (HotkeyHandler.MOD)value;
            }
            return HotkeyHandler.MOD.None;
        }

        private void ShortcutsReset_Clicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.nextTrack_mod = (uint)Application.Current.FindResource("nextTrack_mod");
            Properties.Settings.Default.nextTrack_vk = (uint)Application.Current.FindResource("nextTrack_vk");
            Properties.Settings.Default.previousTrack_mod = (uint)Application.Current.FindResource("previousTrack_mod");
            Properties.Settings.Default.previousTrack_vk = (uint)Application.Current.FindResource("previousTrack_vk");
            Properties.Settings.Default.playPause_mod = (uint)Application.Current.FindResource("playPause_mod");
            Properties.Settings.Default.playPause_vk = (uint)Application.Current.FindResource("playPause_vk");
            Properties.Settings.Default.volumeUp_mod = (uint)Application.Current.FindResource("volumeUp_mod");
            Properties.Settings.Default.volumeUp_vk = (uint)Application.Current.FindResource("volumeUp_vk");
            Properties.Settings.Default.volumeDown_mod = (uint)Application.Current.FindResource("volumeDown_mod");
            Properties.Settings.Default.volumeDown_vk = (uint)Application.Current.FindResource("volumeDown_vk");
            Properties.Settings.Default.volumeMute_mod = (uint)Application.Current.FindResource("volumeMute_mod");
            Properties.Settings.Default.volumeMute_vk = (uint)Application.Current.FindResource("volumeMute_vk");
            Properties.Settings.Default.showWindow_mod = (uint)Application.Current.FindResource("showWindow_mod");
            Properties.Settings.Default.showWindow_vk = (uint)Application.Current.FindResource("showWindow_vk");

            _hotkeys.Initialize();
            HotkeyChanged();
            InitializeShortcuts();
        }

        public uint GetMod(StackPanel stackPanel)
        {
            return (uint)(GetMOD(stackPanel.Children.OfType<ComboBox>().First().SelectedItem.ToString()) | GetMOD(stackPanel.Children.OfType<ComboBox>().Last().SelectedItem.ToString()));
        }

        public uint GetVk(StackPanel stackPanel)
        {
            Button button = stackPanel.Children.OfType<Button>().First();
            TextBlock textBlock = (TextBlock)button.Content;
            return (uint)(HotkeyHandler.VK)System.Enum.Parse(typeof(HotkeyHandler.VK), textBlock.Text, true);
        }
    }
}