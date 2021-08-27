﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace unison
{
    public class HotkeyHandler
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;

        // modifiers
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        // reference => https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        private const uint VK_MEDIA_PREV_TRACK = 0xB1;
        private const uint VK_MEDIA_NEXT_TRACK = 0xB0;
        private const uint VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const uint VK_VOLUME_UP = 0xAF;
        private const uint VK_VOLUME_DOWN = 0xAE;
        private const uint VK_ENTER = 0x0D;

        private IntPtr _windowHandle;
        private HwndSource _source;

        private readonly MPDHandler mpd;

        public HotkeyHandler()
        {
            mpd = (MPDHandler)Application.Current.Properties["mpd"];
        }

        public void Activate(Window win)
        {
            if (_source == null)
            {
                _windowHandle = new WindowInteropHelper(win).Handle;
                _source = HwndSource.FromHwnd(_windowHandle);
                _source.AddHook(HwndHook);

                RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_MEDIA_PREV_TRACK);
                RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_MEDIA_NEXT_TRACK);
                RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_MEDIA_PLAY_PAUSE);
                RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_VOLUME_UP);
                RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_VOLUME_DOWN);
                RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_ENTER);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                uint vkey = ((uint)lParam >> 16) & 0xFFFF;
                MainWindow AppWindow = (MainWindow)Application.Current.MainWindow;
                switch (vkey)
                {
                    case VK_MEDIA_NEXT_TRACK:
                        mpd.Next();
                        break;
                    case VK_MEDIA_PREV_TRACK:
                        mpd.Prev();
                        break;
                    case VK_VOLUME_DOWN:
                        mpd._currentVolume -= Properties.Settings.Default.volume_offset;
                        if (mpd._currentVolume < 0)
                            mpd._currentVolume = 0;
                        mpd.SetVolume(mpd._currentVolume);
                        break;
                    case VK_VOLUME_UP:
                        mpd._currentVolume += Properties.Settings.Default.volume_offset;
                        if (mpd._currentVolume > 100)
                            mpd._currentVolume = 100;
                        mpd.SetVolume(mpd._currentVolume);
                        break;
                    case VK_MEDIA_PLAY_PAUSE:
                        mpd.PlayPause();
                        break;
                    case VK_ENTER:
                        if (AppWindow.WindowState == WindowState.Minimized)
                        {
                            AppWindow.Show();
                            AppWindow.Activate();
                            AppWindow.WindowState = WindowState.Normal;
                        }
                        else
                        {
                            if (AppWindow.IsActive)
                            {
                                AppWindow.Hide();
                                AppWindow.WindowState = WindowState.Minimized;
                            }
                            else  // not minimized but not in front
                            {
                                AppWindow.Show();
                                AppWindow.Activate();
                                AppWindow.WindowState = WindowState.Normal;
                            }
                        }
                        break;
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void RemoveHotKeys()
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }
    }
}