﻿using System;
using System.Diagnostics;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace unison
{
    public class SnapcastHandler
    {
        private readonly Process _snapcast = new();
        public bool HasStarted { get; private set; }

        public void OnConnectionChanged(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.snapcast_startup)
            {
                var mpd = (MPDHandler)Application.Current.Properties["mpd"];
                if (mpd.IsConnected())
                    LaunchOrExit();
            }
        }

        public void UpdateInterface()
        {
            TaskbarIcon Systray = (TaskbarIcon)Application.Current.Properties["systray"];
            SystrayViewModel DataContext = Systray.DataContext as SystrayViewModel;
            DataContext.OnPropertyChanged("SnapcastText");

            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow MainWin = (MainWindow)Application.Current.MainWindow;
                MainWin.OnSnapcastChanged();
            });
        }

        public void LaunchOrExit(bool ForceExit = false)
        {
            if (!HasStarted && !ForceExit)
            {
                _snapcast.StartInfo.FileName = Properties.Settings.Default.snapcast_path + @"\snapclient.exe";
                _snapcast.StartInfo.Arguments = $"--host {Properties.Settings.Default.mpd_host}";
                _snapcast.StartInfo.CreateNoWindow = !Properties.Settings.Default.snapcast_window;
                try
                {
                    _snapcast.Start();
                }
                catch (Exception err)
                {
                    MessageBox.Show($"[{unison.Resources.Resources.Snapcast_Popup1}]\n" +
                                    $"{unison.Resources.Resources.Snapcast_Popup2} {err.Message}\n\n" +
                                    $"{unison.Resources.Resources.Snapcast_Popup3} {Properties.Settings.Default.snapcast_path}\n" +
                                    $"{unison.Resources.Resources.Snapcast_Popup4}",
                                    "unison", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                HasStarted = true;
            }
            else if (HasStarted)
            {
                _snapcast.Kill();
                HasStarted = false;
            }

            if (!ForceExit)
                UpdateInterface();
        }
    }
}