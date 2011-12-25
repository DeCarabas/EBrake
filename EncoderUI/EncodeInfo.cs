namespace EncoderUI
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using HandBrake.ApplicationServices;
    using HandBrake.ApplicationServices.Services;

    public abstract class EncodeInfo : INotifyPropertyChanged
    {
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa365247(v=vs.85).aspx
        static readonly char[] reservedCharacters = new char[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        readonly Queue encodingQueue;
        string eta;
        bool isEncoding;
        readonly MainWindow mainWindow;
        string outputPath;
        float percentComplete;
        DriveInfo sourceDrive;

        protected EncodeInfo(MainWindow mainWindow, Queue encodingQueue)
        {
            this.mainWindow = mainWindow;
            this.encodingQueue = encodingQueue;

            this.encodingQueue.EncodeStarted += OnEncodeStarted;
            this.encodingQueue.EncodeStatusChanged += OnEncodeStatusChanged;
            this.encodingQueue.EncodeEnded += OnEncodeEnded;
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
            set { this.outputPath = value; }
        }

        public float PercentComplete
        {
            get { return this.percentComplete; }
            set { this.percentComplete = value; Notify("PercentComplete"); }
        }

        public DriveInfo SourceDrive
        {
            get { return this.sourceDrive; }
            set { this.sourceDrive = value; Notify("SourceDrive"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected abstract void AddEncodingJobs(Queue encodingQueue);

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

        void OnEncodeEnded(object sender, EventArgs e)
        {
            IsEncoding = false;
            ETA = "";
            PercentComplete = 0;
        }

        void OnEncodeStarted(object sender, EventArgs e)
        {
            IsEncoding = true;
            ETA = "";
            PercentComplete = 0;
        }

        void OnEncodeStatusChanged(object sender, EncodeProgressEventArgs e)
        {
            ETA = e.EstimatedTimeLeft;
            PercentComplete = e.PercentComplete;
        }

        public void Start()
        {
            AddEncodingJobs(this.encodingQueue);
            if (this.encodingQueue.Count > 0) { this.encodingQueue.Start(); }
        }
    }
}
