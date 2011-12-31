namespace EBrake
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Interop;
    using EBrake.Controls;
    using EBrake.Interop;
    using HandBrake.ApplicationServices;
    using HandBrake.ApplicationServices.Services;
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

            MovieEncodeInfo = new MovieEncodeInfo(this, this.encodeQueue);
            MovieEncodeInfo.PropertyChanged += OnEncodeInfoPropertyChanged;

            TVShowEncodeInfo = new TVShowEncodeInfo(this, this.encodeQueue);
            TVShowEncodeInfo.PropertyChanged += OnEncodeInfoPropertyChanged;

            Tabs.SelectionChanged += OnTabChanged;

            SourceInitialized += OnSourceInitialized;

            LoadSettings();
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

        void LoadSettings()
        {
            try
            {
                string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string fileName = Path.Combine(rootPath, "EncoderUI", "settings.json");
                if (File.Exists(fileName))
                {
                    var settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(fileName));
                    MovieEncodeInfo.OutputPath = settings.MovieOutputPath;
                    TVShowEncodeInfo.OutputPath = settings.TVShowOutputPath;
                }
            }
            catch
            {
                // Oh well! Best effort.
            }
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

        void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            var encodeInfo = ((FrameworkElement)e.OriginalSource).Tag as EncodeInfo;
            if (encodeInfo != null)
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog 
                { 
                    SelectedPath = encodeInfo.OutputPath, 
                    ShowNewFolderButton = true 
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    encodeInfo.OutputPath = dialog.SelectedPath;
                }
            }
        }

        void OnClosingWindow(object sender, ClosingWindowEventArgs e)
        {
            SaveSettings();
            if (MovieEncodeInfo.IsEncoding) { MovieEncodeInfo.Stop(); }
            if (TVShowEncodeInfo.IsEncoding) { TVShowEncodeInfo.Stop(); }
        }

        void OnEncodeButtonClicked(EncodeInfo encodeInfo)
        {
            if (encodeInfo.IsEncoding)
            {
                encodeInfo.Stop();
            }
            else
            {
                encodeInfo.Start();
            }
        }

        void OnEncodeInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsEncoding")
            {
                var encodeInfo = (EncodeInfo)sender;
                foreach (TabItem tabItem in Tabs.Items)
                {
                    if (tabItem != Tabs.SelectedItem)
                    {
                        tabItem.IsEnabled = !encodeInfo.IsEncoding;
                    }
                }                
            }
        }

        void OnEpisodeListKeyDown(object sender, KeyEventArgs e)
        {
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
            if (elementWithFocus != null)
            {
                FocusNavigationDirection? direction = null;
                switch (e.Key)
                {
                    case Key.Up: direction = FocusNavigationDirection.Up; break;
                    case Key.Down: direction = FocusNavigationDirection.Down; break;
                    case Key.Right:
                        {
                            var textBox = elementWithFocus as TextBox;
                            if (textBox != null)
                            {
                                if (textBox.CaretIndex == textBox.Text.Length)
                                {
                                    direction = FocusNavigationDirection.Right;
                                }
                            }
                        }
                        break;
                    case Key.Left:
                        {
                            var textBox = elementWithFocus as TextBox;
                            if (textBox != null)
                            {
                                if (textBox.CaretIndex == 0)
                                {
                                    direction = FocusNavigationDirection.Left;
                                }
                            }
                        }
                        break;
                }

                if (direction != null)
                {
                    elementWithFocus.MoveFocus(new TraversalRequest(direction.Value));
                    e.Handled = true;
                }
            }
        }

        void OnFixSettingsClicked(object sender, RoutedEventArgs e)
        {
            Tabs.SelectedItem = SettingsTab;
            // TODO: Focus appropriately
        }

        void OnMovieEncodeButtonClicked(object sender, RoutedEventArgs e)
        {
            OnEncodeButtonClicked(MovieEncodeInfo);
        }

        void OnPreviewShowClicked(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source != null)
            {
                var encodeInfo = (TitleEncodeInfo)source.DataContext;
                // vlc dvd://device@title
            }
        }

        void OnSourceInitialized(object sender, EventArgs e)
        {
            // Hook the window proc so that we can get the disc-change notifications.
            var interopHelper = new WindowInteropHelper(this);
            this.interopSource = HwndSource.FromHwnd(interopHelper.EnsureHandle());
            this.interopSource.AddHook(MessageHook);
        }

        void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Contains(SettingsTab))
            {
                SaveSettings();
            }
        }

        void OnTVEncodeButtonClicked(object sender, RoutedEventArgs e)
        {
            OnEncodeButtonClicked(TVShowEncodeInfo);
        }

        void OnWindowTopMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed)
                DragMove();
        }

        void SaveSettings()
        {
            try
            {
                var settings = new Settings
                {
                    MovieOutputPath = MovieEncodeInfo.OutputPath,
                    TVShowOutputPath = TVShowEncodeInfo.OutputPath
                };

                string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string settingsPath = Path.Combine(rootPath, "EncoderUI");
                if (!Directory.Exists(settingsPath)) { Directory.CreateDirectory(settingsPath); }

                string fileName = Path.Combine(settingsPath, "settings.json");
                File.WriteAllText(fileName, JsonConvert.SerializeObject(settings));
            }
            catch
            {
                // Oh well! Best effort.
            }
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

        class Settings
        {
            public string MovieOutputPath { get; set; }
            public string TVShowOutputPath { get; set; }
        }
    }
}
