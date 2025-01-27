﻿using Hardcodet.Wpf.TaskbarNotification;
using NintrollerLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using Shared;
using Shared.Windows;

namespace WiinUSoft
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        private List<DeviceInfo> hidList;
        private List<DeviceControl> deviceList;
        private Task _refreshTask;
        private CancellationTokenSource _refreshToken;
        private bool _refreshing;

        public MainWindow()
        {
            hidList = new List<DeviceInfo>();
            deviceList = new List<DeviceControl>();

            InitializeComponent();

            Instance = this;
        }

        public void HideWindow()
        {
            if (WindowState == WindowState.Minimized)
            {
                trayIcon.Visibility = Visibility.Visible;
                Hide();
            }
        }

        public void ShowWindow()
        {
            trayIcon.Visibility = Visibility.Hidden;
            Show();
            WindowState = WindowState.Normal;
        }

        public void ShowBalloon(string title, string message, BalloonIcon icon)
        {
            ShowBalloon(title, message, icon, null);
        }

        public void ShowBalloon(string title, string message, BalloonIcon icon, SystemSound sound)
        {
            trayIcon.Visibility = Visibility.Visible;
            trayIcon.ShowBalloonTip(title, message, icon);

            if (sound != null)
            {
                sound.Play();
            }

            Task restoreTray = new Task(new Action(() =>
            {
                Thread.Sleep(7000);
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => trayIcon.Visibility = WindowState == WindowState.Minimized ? Visibility.Visible : Visibility.Hidden));
            }));
            restoreTray.Start();
        }

        private void Refresh()
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Refreshing");
#endif
            hidList = WinBtStream.GetPaths();
            List<KeyValuePair<int, DeviceControl>> connectSeq = new List<KeyValuePair<int, DeviceControl>>();
            
            foreach (var hid in hidList)
            {
                DeviceControl existingDevice = null;

                foreach (DeviceControl d in deviceList)
                {
                    if (d.DevicePath == hid.DevicePath)
                    {
                        existingDevice = d;
                        break;
                    }
                }

                if (existingDevice != null)
                {
                    if (!existingDevice.Connected)
                    {
                        existingDevice.RefreshState();
                        if (existingDevice.properties.autoConnect && existingDevice.ConnectionState == DeviceState.Discovered)
                        {
                            connectSeq.Add(new KeyValuePair<int, DeviceControl>(existingDevice.properties.autoNum, existingDevice));
                        }
                    }
                }
                else
                {
                    var stream = new WinBtStream(
                        hid.DevicePath, 
                        UserPrefs.Instance.toshibaMode ? WinBtStream.BtStack.Toshiba : WinBtStream.BtStack.Microsoft, 
                        UserPrefs.Instance.greedyMode ? FileShare.None : FileShare.ReadWrite);
                    Nintroller n = new Nintroller(stream, hid.Type);

                    if (stream.OpenConnection() && stream.CanRead)
                    {
                        deviceList.Add(new DeviceControl(n, hid.DevicePath));
                        deviceList[deviceList.Count - 1].OnConnectStateChange += DeviceControl_OnConnectStateChange;
                        deviceList[deviceList.Count - 1].OnConnectionLost += DeviceControl_OnConnectionLost;
                        deviceList[deviceList.Count - 1].RefreshState();
                        if (deviceList[deviceList.Count - 1].properties.autoConnect)
                        {
                            connectSeq.Add(new KeyValuePair<int, DeviceControl>(deviceList[deviceList.Count - 1].properties.autoNum, deviceList[deviceList.Count - 1]));
                        }
                    }
                }
            }

            int target = 0;
            while (!Holders.XInputHolder.availabe[target] && target < 4)
            {
                target++;
            }

            // Auto Connect First Available devices
            for (int a = 0; a < connectSeq.Count; a++)
            {
                var thingy = connectSeq[a];

                if (thingy.Key == 5)
                {
                    if (Holders.XInputHolder.availabe[target] && target < 4)
                    {
                        if (thingy.Value.Device.Connected || (thingy.Value.Device.DataStream as WinBtStream).OpenConnection())
                        {
                            thingy.Value.targetXDevice = target + 1;
                            thingy.Value.ConnectionState = DeviceState.Connected_XInput;
                            thingy.Value.Device.BeginReading();
                            thingy.Value.Device.GetStatus();
                            thingy.Value.Device.SetPlayerLED(target + 1);
                            target++;
                        }
                    }

                    connectSeq.Remove(thingy);
                }
            }

            // Auto connect in preferred order
            for (int i = 1; i < connectSeq.Count; i++)
            {
                if (connectSeq[i].Key < connectSeq[i - 1].Key)
                {
                    var tmp = connectSeq[i];
                    connectSeq[i] = connectSeq[i - 1];
                    connectSeq[i - 1] = tmp;
                    i = 0;
                }
            }
            
            foreach(KeyValuePair<int, DeviceControl> d in connectSeq)
            {
                if (Holders.XInputHolder.availabe[target] && target < 4)
                {
                    if (d.Value.Device.Connected || (d.Value.Device.DataStream as WinBtStream).OpenConnection())
                    {
                        d.Value.targetXDevice = target + 1;
                        d.Value.ConnectionState = DeviceState.Connected_XInput;
                        d.Value.Device.BeginReading();
                        d.Value.Device.GetStatus();
                        d.Value.Device.SetPlayerLED(target + 1);
                        target++;
                    }
                }
            }
        }

        private void AutoRefresh(bool enable, int currentDeviceCount)
        {
            AutoRefresh(enable && (menu_AutoRefreshCount.Value == 0 || currentDeviceCount < menu_AutoRefreshCount.Value));
        }

        private void AutoRefresh(bool set)
        {
            if (set && !_refreshing)
            {
                _refreshing = true;
                _refreshToken = new CancellationTokenSource();
                _refreshTask = new Task(new Action(() =>
                {
                    while (!_refreshToken.IsCancellationRequested)
                    {
                        Thread.Sleep(5000);
                        if (_refreshToken.IsCancellationRequested) break;
                        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Refresh()));
                    }

                    _refreshing = false;
                }), _refreshToken.Token);
                _refreshTask.Start();
            }
            else if (!set && _refreshing)
            {
                _refreshToken.Cancel();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
                menu_version.Header = string.Format("version {0}.{1}.{2}", version.Major, version.Minor, version.Revision);
            }
            catch { }

            if (UserPrefs.Instance.startMinimized)
            {
                menu_StartMinimized.IsChecked = true;
                WindowState = WindowState.Minimized;
            }
            
            menu_AutoStart.IsChecked = UserPrefs.Instance.autoStartup;
            menu_NoSharing.IsChecked = UserPrefs.Instance.greedyMode;
            menu_AutoRefresh.IsChecked = UserPrefs.Instance.autoRefresh;
            menu_AutoRefreshCount.Value = UserPrefs.Instance.autoRefreshCount;
            menu_MsBluetooth.IsChecked = !UserPrefs.Instance.toshibaMode;

            if (UserPrefs.Instance.greedyMode)
            {
                WinBtStream.OverrideSharingMode = true;
                WinBtStream.OverridenFileShare = FileShare.None;
            }

            Refresh();
            AutoRefresh(menu_AutoRefresh.IsChecked, deviceList.Count);
        }

        private void DeviceControl_OnConnectStateChange(DeviceControl sender, DeviceState oldState, DeviceState newState)
        {
            if (oldState == newState)
                return;

            switch (oldState)
            {
                case DeviceState.Discovered:
                    groupAvailable.Children.Remove(sender);
                    break;

                case DeviceState.Connected_XInput:
                    groupXinput.Children.Remove(sender);
                    break;

                //case DeviceState.Connected_VJoy:
                //    groupXinput.Children.Remove(sender);
                //    break;
            }

            switch (newState)
            {
                case DeviceState.Discovered:
                    groupAvailable.Children.Add(sender);
                    break;

                case DeviceState.Connected_XInput:
                    groupXinput.Children.Add(sender);
                    break;

                //case DeviceState.Connected_VJoy:
                //    groupXinput.Children.Add(sender);
                //    break;
            }
            
            if (menu_AutoRefresh.IsChecked)
            {
                AutoRefresh(true, groupAvailable.Children.Count + groupXinput.Children.Count);
            }
        }

        private void DeviceControl_OnConnectionLost(DeviceControl sender)
        {
            if (groupAvailable.Children.Contains(sender))
            {
                groupAvailable.Children.Remove(sender);
            }
            else if (groupXinput.Children.Contains(sender))
            {
                groupXinput.Children.Remove(sender);
            }

            deviceList.Remove(sender);

            AutoRefresh(menu_AutoRefresh.IsChecked, deviceList.Count);
        }
        
        private void btnDetatchAllXInput_Click(object sender, RoutedEventArgs e)
        {
            List<DeviceControl> detatchList = new List<DeviceControl>();
            foreach (DeviceControl d in groupXinput.Children)
            {
                detatchList.Add(d);
            }
            foreach (DeviceControl d in detatchList)
            {
                d.Detatch();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void btnSync_Click(object sender, RoutedEventArgs e)
        {
            Windows.SyncWindow sync = new Windows.SyncWindow();
            sync.ShowDialog();
            Refresh();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            HideWindow();
        }

        private void MenuItem_Show_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void MenuItem_Refresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            foreach (DeviceControl dc in deviceList)
            {
                if (dc.ConnectionState == DeviceState.Connected_XInput
                 || dc.ConnectionState == DeviceState.Connected_VJoy)
                {
                    dc.Detatch();
                }
            }

            Close();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (btnSettings.ContextMenu != null)
            {
                btnSettings.ContextMenu.IsOpen = true;
            }
        }

        private void menu_AutoStart_Click(object sender, RoutedEventArgs e)
        {
            menu_AutoStart.IsChecked = !menu_AutoStart.IsChecked;
        }

        private void menu_StartMinimized_Click(object sender, RoutedEventArgs e)
        {
            menu_StartMinimized.IsChecked = !menu_StartMinimized.IsChecked;
        }

        private void menu_NoSharing_Click(object sender, RoutedEventArgs e)
        {
            menu_NoSharing.IsChecked = !menu_NoSharing.IsChecked;
            WinBtStream.OverrideSharingMode = UserPrefs.Instance.greedyMode;
            if (UserPrefs.Instance.greedyMode)
            {
                WinBtStream.OverridenFileShare = FileShare.None;
            }
        }

        private void menu_AutoRefresh_Click(object sender, RoutedEventArgs e)
        {
            menu_AutoRefresh.IsChecked = !menu_AutoRefresh.IsChecked;
            AutoRefresh(menu_AutoRefresh.IsChecked, groupAvailable.Children.Count + groupXinput.Children.Count);
        }

        private void menu_SetDefaultCalibration_Click(object sender, RoutedEventArgs e)
        {
            var dWin = new Windows.CalDefaultWindow();
            dWin.ShowDialog();
        }

        private void menu_MsBluetooth_Click(object sender, RoutedEventArgs e)
        {
            menu_MsBluetooth.IsChecked = !menu_MsBluetooth.IsChecked;
            WinBtStream.ForceToshibaMode = !menu_MsBluetooth.IsChecked;
        }

        private void SettingsMenu_Closing(object sender, RoutedEventArgs e)
        {
            UserPrefs.AutoStart = menu_AutoStart.IsChecked;
            UserPrefs.Instance.startMinimized = menu_StartMinimized.IsChecked;
            UserPrefs.Instance.autoRefresh = menu_AutoRefresh.IsChecked;
            UserPrefs.Instance.autoRefreshCount = menu_AutoRefreshCount.Value;
            UserPrefs.Instance.greedyMode = menu_NoSharing.IsChecked;
            UserPrefs.Instance.toshibaMode = !menu_MsBluetooth.IsChecked;
            UserPrefs.SavePrefs();
        }

#region Shortcut Creation
        public void CreateShortcut(string path)
        {
            IShellLink link = (IShellLink)new ShellLink();

            link.SetDescription("WiinUSoft");
            link.SetPath(new Uri(System.Reflection.Assembly.GetEntryAssembly().CodeBase).LocalPath);

            IPersistFile file = (IPersistFile)link;
            file.Save(Path.Combine(path, "WiinUSoft.lnk"), false);
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }
#endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _refreshToken?.Cancel();
        }
    }

    class ShowWindowCommand : ICommand
    {
        public void Execute(object parameter)
        {
            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.ShowWindow();
            }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
