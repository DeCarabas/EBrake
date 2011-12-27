namespace EncoderUI
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Windows.Threading;
    using HandBrake.ApplicationServices;
    using HandBrake.ApplicationServices.Services;

    public abstract class EncodeInfo : DispatcherObject, INotifyPropertyChanged
    {
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa365247(v=vs.85).aspx
        static readonly char[] reservedCharacters = new char[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        readonly Queue encodingQueue;
        string eta;
        bool isEncoding;
        readonly MainWindow mainWindow;
        string outputPath;
        float percentComplete;
        SettingError settingError;

        protected EncodeInfo(MainWindow mainWindow, Queue encodingQueue)
        {
            this.mainWindow = mainWindow;
            this.encodingQueue = encodingQueue;

            this.encodingQueue.EncodeStarted += OnEncodeStarted;
            this.encodingQueue.EncodeStatusChanged += OnEncodeStatusChanged;
            this.encodingQueue.EncodeEnded += OnEncodeEnded;

            DependencyPropertyDescriptor descriptor;
            descriptor = DependencyPropertyDescriptor.FromProperty(MainWindow.SourceDriveProperty, typeof(MainWindow));
            descriptor.AddValueChanged(this.mainWindow, (o, e) => OnSourceDriveChanged());

            CheckSettingErrors();
        }

        public string ETA
        {
            get { return this.eta; }
            set { this.eta = value; Notify("ETA"); }
        }

        public bool IsEncoding
        {
            get { return this.isEncoding; }
            set { this.isEncoding = value; Notify("IsEncoding"); }
        }

        public IList<DriveInfo> OpticalDrives
        {
            get { return this.mainWindow.OpticalDrives; }
        }

        public string OutputPath
        {
            get { return this.outputPath; }
            set 
            { 
                this.outputPath = value; 
                Notify("OutputPath");
                CheckSettingErrors();
            }
        }

        public float PercentComplete
        {
            get { return this.percentComplete; }
            set { this.percentComplete = value; Notify("PercentComplete"); }
        }

        public SettingError SettingError
        {
            get { return this.settingError; }
            set 
            {
                if (this.settingError != value)
                {
                    this.settingError = value; 
                    Notify("SettingError");
                }
            }
        }

        protected DriveInfo SourceDrive
        {
            get { return this.mainWindow.SourceDrive; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected abstract void AddEncodingJobs(Queue encodingQueue);
        
        void CheckSettingErrors()
        {
            if (SourceDrive == null)
            {
                SettingError = SettingError.NoDVD;
            }
            else if (String.IsNullOrWhiteSpace(OutputPath))
            {
                SettingError = SettingError.NoOutputPath;
            }
            else
            {
                SettingError = SettingError.OK;
            }
        }

        protected static string EscapeFileName(string fileName)
        {
            for (int i = 0; i < reservedCharacters.Length; i++)
            {
                fileName = fileName.Replace(reservedCharacters[i], '-');
            }
            return fileName;
        }

        protected void Notify(string property)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(property)); }
        }

        protected virtual void OnSourceDriveChanged()
        {
            CheckSettingErrors();
        }

        void OnEncodeEnded(object sender, EventArgs e)
        {
            IsEncoding = false;
            ETA = "Complete";
            PercentComplete = 1;
        }

        void OnEncodeStarted(object sender, EventArgs e)
        {
            IsEncoding = true;
            ETA = "Starting...";
            PercentComplete = 0;
        }

        void OnEncodeStatusChanged(object sender, EncodeProgressEventArgs e)
        {
            if (IsEncoding)
            {
                PercentComplete = e.PercentComplete;
                ETA = String.Format("{0}% Complete. ETA: {1}", Math.Round(e.PercentComplete * 100), e.EstimatedTimeLeft);
            }
        }

        public void Start()
        {
            AddEncodingJobs(this.encodingQueue);
            if (this.encodingQueue.Count > 0) { this.encodingQueue.Start(); }
        }
    }
}
