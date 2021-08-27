using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace unison
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public readonly Settings SettingsWindow = new Settings();

        private MPDHandler mpd;

        Thickness SelectedThickness;
        Thickness BaseThickness;

        public MainWindow()
        {
            InitHwnd();
            InitializeComponent();

            WindowState = WindowState.Minimized;

            mpd = (MPDHandler)Application.Current.Properties["mpd"];

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.5);
            timer.Tick += Timer_Tick;
            timer.Start();

            SelectedThickness.Left = SelectedThickness.Right = SelectedThickness.Top = 0.0f;
            SelectedThickness.Bottom = 2.0f;
            BaseThickness.Left = BaseThickness.Right = BaseThickness.Top = BaseThickness.Bottom = 0.0f;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateInterface();
        }

        public void UpdateButton(ref Border border, bool b)
        {
            if (b)
                border.BorderThickness = SelectedThickness;
            else
                border.BorderThickness = BaseThickness;
        }

        public string FormatSeconds(double time)
        {
            TimeSpan timespan = TimeSpan.FromSeconds(time);
            return timespan.ToString(@"mm\:ss");
        }

        public void UpdateInterface()
        {
            if (mpd.GetCurrentSong() != null && mpd.GetStatus() != null)
            {
                SongTitle.Text = mpd.GetCurrentSong().Title;
                SongTitle.ToolTip = mpd.GetCurrentSong().File;
                SongArtist.Text = mpd.GetCurrentSong().Artist;
                SongAlbum.Text = mpd.GetCurrentSong().Album;
                if (mpd.GetCurrentSong().Date.Length > 0)
                    SongAlbum.Text += $" ({ mpd.GetCurrentSong().Date})";
                Bitrate.Text = mpd.GetCurrentSong().File.Substring(mpd.GetCurrentSong().File.LastIndexOf(".") + 1) + " – ";
                Bitrate.Text += mpd.GetStatus().MpdBitrate + "kbps";

                CurrentTime.Text = FormatSeconds(mpd._elapsed);
                EndTime.Text = FormatSeconds(mpd.GetStatus().MpdSongTime);

                if (!System.Double.IsNaN(mpd.GetCurrentSong().TimeSort))
                    TimeSlider.Value = mpd._elapsed / mpd.GetCurrentSong().TimeSort * 100;
            }

            if (VolumeSlider.Value != mpd._currentVolume)
            {
                VolumeSlider.Value = mpd._currentVolume;
                VolumeSlider.ToolTip = mpd._currentVolume;
            }

            if (mpd.IsPlaying())
                PlayPause.Text = "\xedb4";
            else
                PlayPause.Text = "\xedb5";

            SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
            if (snapcast.Started)
                SnapcastText.Text = "Stop Snapcast";
            else
                SnapcastText.Text = "Start Snapcast";

            Connection.Text = (mpd._connected ? "✔️" : "❌") + $"{Properties.Settings.Default.mpd_host}:{Properties.Settings.Default.mpd_port}";

            UpdateButton(ref BorderRandom, mpd._currentRandom);
            UpdateButton(ref BorderRepeat, mpd._currentRepeat);
            UpdateButton(ref BorderSingle, mpd._currentSingle);
            UpdateButton(ref BorderConsume, mpd._currentConsume);

            if (mpd.GetCover() != null)
            {
                if ((!mpd.GetCover().IsDownloading) && mpd.GetCover().IsSuccess)
                {
                    if (mpd.GetCurrentSong().File == mpd.GetCover().SongFilePath)
                    {
                        Cover.Source = mpd.GetCover().AlbumImageSource;
                        Cover.Visibility = Visibility.Visible;
                        NoCover.Visibility = Visibility.Collapsed;
                        return;
                    }
                }
            }
            NoCover.Visibility = Visibility.Visible;
            Cover.Visibility = Visibility.Collapsed;
        }

        public void Pause_Clicked(object sender, RoutedEventArgs e) => mpd.PlayPause();
        public void Previous_Clicked(object sender, RoutedEventArgs e) =>  mpd.Prev();
        public void Next_Clicked(object sender, RoutedEventArgs e) => mpd.Next();

        public void Random_Clicked(object sender, RoutedEventArgs e) => mpd.Random();
        public void Repeat_Clicked(object sender, RoutedEventArgs e) => mpd.Repeat();
        public void Single_Clicked(object sender, RoutedEventArgs e) => mpd.Single();
        public void Consume_Clicked(object sender, RoutedEventArgs e) => mpd.Consume();
        public void ChangeVolume(int value) => mpd.SetVolume(value);

        public void Snapcast_Clicked(object sender, RoutedEventArgs e)
        {
            SnapcastHandler snapcast = (SnapcastHandler)Application.Current.Properties["snapcast"];
            if (!snapcast.Started)
                snapcast.Start();
            else
                snapcast.Stop();
        }

        public void Settings_Clicked(object sender, RoutedEventArgs e)
        {
            SettingsWindow.Show();
            SettingsWindow.Activate();

            if (SettingsWindow.WindowState == WindowState.Minimized)
                SettingsWindow.WindowState = WindowState.Normal;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
        }

        private void TimeSlider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            Slider slider = (Slider)sender;

            double SongPercentage = slider.Value;
            double SongTime = mpd.GetStatus().MpdSongTime;
            double SeekTime = SongPercentage / 100 * SongTime;

            mpd.SetTime((int)SeekTime);
        }

        private void VolumeSlider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            Slider slider = (Slider)sender;
            mpd.SetVolume((int)slider.Value);
            slider.ToolTip = mpd._currentVolume;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HotkeyHandler hk = (HotkeyHandler)Application.Current.Properties["hotkeys"];
            hk.Activate(this);
        }

        public void InitHwnd()
        {
            WindowInteropHelper helper = new(this);
            helper.EnsureHandle();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}