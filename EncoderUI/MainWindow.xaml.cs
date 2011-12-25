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
        static readonly DependencyProperty movieEncodeInfoProperty = DependencyProperty.Register(
            "MovieEncodeInfo", typeof(MovieEncodeInfo), typeof(MainWindow));
        static readonly DependencyProperty outputPathProperty = DependencyProperty.Register(
            "OutputPath", typeof(string), typeof(MainWindow), new PropertyMetadata(String.Empty));

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

            this.scanService.ScanStatusChanged += OnScanStatusChanged;
            this.scanService.ScanCompleted += OnScanCompleted;

            MovieEncodeInfo = new MovieEncodeInfo(this, this.encodeQueue);
        }

        public MovieEncodeInfo MovieEncodeInfo 
        { 
            get { return (MovieEncodeInfo)GetValue(movieEncodeInfoProperty); }
            set { SetValue(movieEncodeInfoProperty, value); }
        }

        public IList<DriveInfo> OpticalDrives { get { return this.opticalDrives; } }

        public string OutputPath
        {
            get { return (string)GetValue(outputPathProperty); }
            set { SetValue(outputPathProperty, value); }
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

        void OnScanCompleted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (scanService.SouceData.Titles.Count > 0)
                {
                    status.Text = scanService.SouceData.Titles.Find(t => t.MainTitle).Duration.ToString();
                }
            });
        }

        void OnScanStatusChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() => { status.Text = scanService.ScanStatus; });
        }

        static void OnSelectedDriveChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var that = (MainWindow)o;
            that.StartRefreshScan();
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

        void StartButton_Click(object sender, RoutedEventArgs e)
        {
            MovieEncodeInfo.Start();
        }

        Task StartRefreshOpticalDrives()
        {
            Task<DriveInfo[]> getDrives = Task.Factory.StartNew(() =>
            {
                return DriveInfo.GetDrives().Where(d => (d.DriveType == DriveType.CDRom && d.IsReady)).ToArray();
            });

            return getDrives.ContinueWith(d =>
            {
                DriveInfo[] drives = d.Result;
                this.opticalDrives.Clear();
                for (int i = 0; i < drives.Length; i++) { this.opticalDrives.Add(drives[i]); }

                if (this.opticalDrives.Count == 0)
                {
                    MovieEncodeInfo.SourceDrive = null;
                }
                else if (MovieEncodeInfo.SourceDrive == null || !MovieEncodeInfo.SourceDrive.IsReady)
                {
                    MovieEncodeInfo.SourceDrive = this.opticalDrives[0];
                }
            }, this.wpfScheduler);
        }

        void StartRefreshScan()
        {
            if (MovieEncodeInfo.SourceDrive != null)
            {
                if (this.scanService.IsScanning) { this.scanService.Stop(); }
                this.scanService.Scan(MovieEncodeInfo.SourceDrive.RootDirectory.FullName, 0);
            }
        }

    }
}
