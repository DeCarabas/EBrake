namespace EncoderUI
{
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Management;
    using System.Windows;
    using HandBrake.ApplicationServices;
    using HandBrake.ApplicationServices.Services;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Threading;
    using System.IO;
    using System.Linq;
    using System.Windows.Interop;
    using EncoderUI.Interop;
    using System.Runtime.InteropServices;
    using System.Net;
    using Newtonsoft.Json;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty MovieEncodeInfoProperty = DependencyProperty.Register(
            "MovieEncodeInfo", typeof(MovieEncodeInfo), typeof(MainWindow));
        public static readonly DependencyProperty SourceDriveProperty = DependencyProperty.Register(
            "SourceDrive", typeof(DriveInfo), typeof(MainWindow));
        public static readonly DependencyProperty TVShowEncodeInfoProperty = DependencyProperty.Register(
            "TVShowEncodeInfo", typeof(TVShowEncodeInfo), typeof(MainWindow));

        HwndSource interopSource;
        readonly Queue encodeQueue = new Queue();
        readonly ObservableCollection<DriveInfo> opticalDrives = new ObservableCollection<DriveInfo>();
        readonly ScanService scanService = new ScanService();
        readonly TaskScheduler wpfScheduler = TaskScheduler.FromCurrentSynchronizationContext();

        public MainWindow()
        {
            DataContext = this;

            InitializeComponent();
            SetupHandbrake();
            StartRefreshOpticalDrives();
            RegisterDeviceChange();

            MovieEncodeInfo = new MovieEncodeInfo(this, this.encodeQueue);
            TVShowEncodeInfo = new TVShowEncodeInfo(this, this.encodeQueue);
        }

        public MovieEncodeInfo MovieEncodeInfo
        {
            get { return (MovieEncodeInfo)GetValue(MovieEncodeInfoProperty); }
            set { SetValue(MovieEncodeInfoProperty, value); }
        }

        public IList<DriveInfo> OpticalDrives { get { return this.opticalDrives; } }

        public DriveInfo SourceDrive
        {
            get { return (DriveInfo)GetValue(SourceDriveProperty); }
            set { SetValue(SourceDriveProperty, value); }
        }

        public TVShowEncodeInfo TVShowEncodeInfo
        {
            get { return (TVShowEncodeInfo)GetValue(TVShowEncodeInfoProperty); }
            set { SetValue(TVShowEncodeInfoProperty, value); }
        }

        IntPtr MessageHook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            switch ((WM)msg)
            {
                case WM.DEVICECHANGE:
                    switch ((DBT)wparam)
                    {
                        case DBT.DEVICEARRIVAL:
                        case DBT.DEVICEREMOVECOMPLETE:
                            var header = new DEV_BROADCAST_HDR();
                            Marshal.PtrToStructure(lparam, header);

                            if (header.dbcv_devicetype == DBT_DEVTYP.VOLUME)
                            {
                                var lpdbv = new DEV_BROADCAST_VOLUME();
                                Marshal.PtrToStructure(lparam, lpdbv);

                                if ((lpdbv.dbcv_flags & DBTF.MEDIA) != 0) { StartRefreshOpticalDrives(); }
                            }
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }
        
        void OnFixSettingsClicked(object sender, RoutedEventArgs e)
        {
            Tabs.SelectedItem = SettingsTab;
            // TODO: Focus appropriately
        }

        void OnStartButtonClicked(object sender, RoutedEventArgs e)
        {
            MovieEncodeInfo.Start();
        }

        void RegisterDeviceChange()
        {
            var interopHelper = new WindowInteropHelper(this);
            this.interopSource = HwndSource.FromHwnd(interopHelper.EnsureHandle());
            this.interopSource.AddHook(MessageHook);
        }

        static void SetupHandbrake()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\HandBrake\\logs";
            if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

            int instanceId = Process.GetProcessesByName("HandBrake").Length;
            Init.SetupSettings(
                versionString: "EncoderUI Quick Encoder v0.1",
                instanceId: instanceId,
                completionOption: "Do Nothing",
                disableDvdNav: false,
                growlEncode: false,
                growlQueue: false,
                processPriority: "Below Normal",
                saveLogPath: "",
                saveLogToSpecifiedPath: false,
                saveLogWithVideo: false,
                showCliForInGuiEncodeStatus: false,
                preventSleep: false);
        }

        Task StartRefreshOpticalDrives()
        {
            Task<DriveInfo[]> getDrives = Task.Factory.StartNew(() =>
            {
                return DriveInfo.GetDrives().Where(d => (d.DriveType == DriveType.CDRom)).ToArray();
            });

            return getDrives.ContinueWith(d =>
            {
                DriveInfo[] drives = d.Result;
                this.opticalDrives.Clear();
                for (int i = 0; i < drives.Length; i++) { this.opticalDrives.Add(drives[i]); }

                if (this.opticalDrives.Count == 0)
                {
                    SourceDrive = null;
                }
                else if (SourceDrive == null || !SourceDrive.IsReady)
                {
                    SourceDrive = this.opticalDrives.FirstOrDefault(drv => drv.IsReady) ?? this.opticalDrives[0];
                }
            }, this.wpfScheduler);
        }
    }
}
