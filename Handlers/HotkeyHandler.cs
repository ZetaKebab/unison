using System;
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

        public enum MOD : int
        {
            None = 0x0000,
            Alt = 0x0001,
            Control = 0x0002,
            Shift = 0x0004,
            //Win = 0x0008
        };

        // reference => https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        public enum VK : int
        {
            None = 0x00,
            Back = 0x08,
            Tab = 0x09,
            Clear = 0x0c,
            Return = 0x0d,
            Menu = 0x12,
            Pause = 0x13,
            Capital = 0x14,
            Kana = 0x15,
            Hangul = 0x15,
            Junja = 0x17,
            Final = 0x18,
            Hanja = 0x19,
            Kanji = 0x19,
            Escape = 0x1b,
            Convert = 0x1c,
            NonConvert = 0x1d,
            Accept = 0x1e,
            ModeChange = 0x1f,
            Space = 0x20,
            Prior = 0x21,
            Next = 0x22,
            End = 0x23,
            Home = 0x24,
            Left = 0x25,
            Up = 0x26,
            Right = 0x27,
            Down = 0x28,
            Select = 0x29,
            Print = 0x2a,
            Execute = 0x2b,
            Snapshot = 0x2c,
            Insert = 0x2d,
            Delete = 0x2e,
            Help = 0x2f,
            NumRow0 = 0x30,
            NumRow1 = 0x31,
            NumRow2 = 0x32,
            NumRow3 = 0x33,
            NumRow4 = 0x34,
            NumRow5 = 0x35,
            NumRow6 = 0x36,
            NumRow7 = 0x37,
            NumRow8 = 0x38,
            NumRow9 = 0x39,
            A = 0x41,
            B = 0x42,
            C = 0x43,
            D = 0x44,
            E = 0x45,
            F = 0x46,
            G = 0x47,
            H = 0x48,
            I = 0x49,
            J = 0x4a,
            K = 0x4b,
            L = 0x4c,
            M = 0x4d,
            N = 0x4e,
            O = 0x4f,
            P = 0x50,
            Q = 0x51,
            R = 0x52,
            S = 0x53,
            T = 0x54,
            U = 0x55,
            V = 0x56,
            W = 0x57,
            X = 0x58,
            Y = 0x59,
            Z = 0x5a,
            Apps = 0x5d,
            Sleep = 0x5f,
            NumPad0 = 0x60,
            NumPad1 = 0x61,
            NumPad2 = 0x62,
            NumPad3 = 0x63,
            NumPad4 = 0x64,
            NumPad5 = 0x65,
            NumPad6 = 0x66,
            NumPad7 = 0x67,
            NumPad8 = 0x68,
            NumPad9 = 0x69,
            Multiply = 0x6a,
            Add = 0x6b,
            Separator = 0x6c,
            Subtract = 0x6d,
            Decimal = 0x6e,
            Divide = 0x6f,
            F1 = 0x70,
            F2 = 0x71,
            F3 = 0x72,
            F4 = 0x73,
            F5 = 0x74,
            F6 = 0x75,
            F7 = 0x76,
            F8 = 0x77,
            F9 = 0x78,
            F10 = 0x79,
            F11 = 0x7a,
            F12 = 0x7b,
            F13 = 0x7c,
            F14 = 0x7d,
            F15 = 0x7e,
            F16 = 0x7f,
            F17 = 0x80,
            F18 = 0x81,
            F19 = 0x82,
            F20 = 0x83,
            F21 = 0x84,
            F22 = 0x85,
            F23 = 0x86,
            F24 = 0x87,
            NumLock = 0x90,
            Scroll = 0x91,
            LMenu = 0xa4,
            RMenu = 0xa5,
            BrowserBack = 0xa6,
            BrowserForward = 0xa7,
            BrowserRefresh = 0xa8,
            BrowserStop = 0xa9,
            BrowserSearch = 0xaa,
            BrowserFavorites = 0xab,
            BrowserHome = 0xac,
            VolumeMute = 0xad,
            VolumeDown = 0xae,
            VolumeUp = 0xaf,
            MediaNextTrack = 0xb0,
            MediaPreviousTrack = 0xb1,
            MediaStop = 0xb2,
            MediaPlayPause = 0xb3,
            LaunchMail = 0xb4,
            LaunchMediaSelect = 0xb5,
            LaunchApp1 = 0xb6,
            LaunchApp2 = 0xb7,
        };

        public struct HotkeyPair
        {
            public MOD mod;
            public VK vk;

            public HotkeyPair(MOD _mod, VK _vk) { mod = _mod; vk = _vk; }
            
            public uint GetMOD() { return (uint)mod; }
            public uint GetVK() { return (uint)vk; }

            public void SetMOD(MOD modmod) { mod = modmod; }
            public void SetVK(VK vkvk) { vk = vkvk; }
        }

        private MainWindow _appWindow;
        private readonly MPDHandler _mpd;

        private IntPtr _windowHandle;
        private HwndSource _source;

        public HotkeyPair _NextTrack;
        public HotkeyPair _PreviousTrack;
        public HotkeyPair _PlayPause;
        public HotkeyPair _VolumeUp;
        public HotkeyPair _VolumeDown;
        public HotkeyPair _VolumeMute;
        public HotkeyPair _ShowWindow;
        public HotkeyPair[] _Shortcuts;

        public HotkeyHandler()
        {
            _mpd = (MPDHandler)Application.Current.Properties["mpd"];
            Initialize();
        }

        public void Initialize()
        {
            _NextTrack = new HotkeyPair((MOD)Properties.Settings.Default.nextTrack_mod, (VK)Properties.Settings.Default.nextTrack_vk);
            _PreviousTrack = new HotkeyPair((MOD)Properties.Settings.Default.previousTrack_mod, (VK)Properties.Settings.Default.previousTrack_vk);
            _PlayPause = new HotkeyPair((MOD)Properties.Settings.Default.playPause_mod, (VK)Properties.Settings.Default.playPause_vk);
            _VolumeUp = new HotkeyPair((MOD)Properties.Settings.Default.volumeUp_mod, (VK)Properties.Settings.Default.volumeUp_vk);
            _VolumeDown = new HotkeyPair((MOD)Properties.Settings.Default.volumeDown_mod, (VK)Properties.Settings.Default.volumeDown_vk);
            _VolumeMute = new HotkeyPair((MOD)Properties.Settings.Default.volumeMute_mod, (VK)Properties.Settings.Default.volumeMute_vk);
            _ShowWindow = new HotkeyPair((MOD)Properties.Settings.Default.showWindow_mod, (VK)Properties.Settings.Default.showWindow_vk);
            _Shortcuts = new HotkeyPair[] { _NextTrack, _PreviousTrack, _PlayPause, _VolumeUp, _VolumeDown, _VolumeMute, _ShowWindow };
        }

        public void Activate(Window win)
        {
            if (_source == null)
            {
                _windowHandle = new WindowInteropHelper(win).Handle;
                _source = HwndSource.FromHwnd(_windowHandle);
                AddHotkeys();
            }
        }

        public void AddHotkeys()
        {
            _source.AddHook(HwndHook);
            RegisterHotKey(_windowHandle, HOTKEY_ID, _NextTrack.GetMOD(), _NextTrack.GetVK());
            RegisterHotKey(_windowHandle, HOTKEY_ID, _PreviousTrack.GetMOD(), _PreviousTrack.GetVK());
            RegisterHotKey(_windowHandle, HOTKEY_ID, _PlayPause.GetMOD(), _PlayPause.GetVK());
            RegisterHotKey(_windowHandle, HOTKEY_ID, _VolumeUp.GetMOD(), _VolumeUp.GetVK());
            RegisterHotKey(_windowHandle, HOTKEY_ID, _VolumeDown.GetMOD(), _VolumeDown.GetVK());
            RegisterHotKey(_windowHandle, HOTKEY_ID, _VolumeMute.GetMOD(), _VolumeMute.GetVK());
            RegisterHotKey(_windowHandle, HOTKEY_ID, _ShowWindow.GetMOD(), _ShowWindow.GetVK());
        }

        public void RemoveHotkeys()
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                uint vkey = ((uint)lParam >> 16) & 0xFFFF;

                if (vkey == _NextTrack.GetVK())
                    _mpd.Next();
                else if (vkey == _PreviousTrack.GetVK())
                    _mpd.Prev();
                else if (vkey == _PlayPause.GetVK())
                    _mpd.PlayPause();
                else if (vkey == _VolumeUp.GetVK())
                    _mpd.VolumeUp();
                else if (vkey == _VolumeDown.GetVK())
                    _mpd.VolumeDown();
                else if (vkey == _VolumeMute.GetVK())
                    _mpd.VolumeMute();
                else if (vkey == _ShowWindow.GetVK())
                {
                    if (_appWindow == null)
                        _appWindow = (MainWindow)Application.Current.MainWindow;

                    if (_appWindow.WindowState == WindowState.Minimized)
                    {
                        _appWindow.Show();
                        _appWindow.Activate();
                        _appWindow.WindowState = WindowState.Normal;
                    }
                    else
                    {
                        if (_appWindow.IsActive)
                        {
                            _appWindow.Hide();
                            _appWindow.WindowState = WindowState.Minimized;
                        }
                        else // not minimized but not in front
                        {
                            _appWindow.Show();
                            _appWindow.Activate();
                            _appWindow.WindowState = WindowState.Normal;
                        }
                    }
                }
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}