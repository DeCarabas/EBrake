namespace EBrake
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Windows.Threading;
    using HandBrake.ApplicationServices;
    using HandBrake.ApplicationServices.Model;
    using HandBrake.ApplicationServices.Services;
    using System.Threading.Tasks;
    using System.Threading;
    using HandBrake.ApplicationServices.EventArgs;

    public abstract class EncodeInfo : DispatcherObject, INotifyPropertyChanged
    {
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa365247(v=vs.85).aspx
        static readonly char[] reservedCharacters = new char[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        readonly Encode encodingQueue;
        int encodeIndex;
        string eta;
        bool isEncoding;
        readonly List<QueueTask> jobQueue = new List<QueueTask>();
        readonly MainWindow mainWindow;
        string outputPath;
        float percentComplete;
        SettingError settingError;
        double totalPercentComplete;

        protected EncodeInfo(MainWindow mainWindow, Encode encodingQueue)
        {
            this.mainWindow = mainWindow;
            this.encodingQueue = encodingQueue;

            this.encodingQueue.EncodeStarted += OnEncodeStarted;
            this.encodingQueue.EncodeStatusChanged += OnEncodeStatusChanged;
            this.encodingQueue.EncodeCompleted += OnEncodeEnded;

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

        public bool CanStartEncoding
        {
            get { return InfoError == InfoError.OK && SettingError == SettingError.OK; }
        }

        public abstract InfoError InfoError { get; }

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

        public double TotalPercentComplete
        {
            get { return this.totalPercentComplete; }
            set { this.totalPercentComplete = value; Notify("TotalPercentComplete"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected abstract void AddEncodingJobs(List<QueueTask> encodingQueue);

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

        protected static string GetStandardCommandLine(string input, string output)
        {

            return String.Format(
                "-o \"{0}\" -i \"{1}\" -Z \"High Profile\" -E \"faac,copy:ac3\" -N \"eng\" -f \"mkv\" -m ", output, input);
        }

        protected virtual void Notify(string property)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(property)); }
            if (property == "SettingError" || property == "InfoError")
            {
                Notify("CanStartEncoding");
            }
        }

        protected virtual void OnSourceDriveChanged()
        {
            CheckSettingErrors();
        }

        void OnEncodeEnded(object sender, EncodeCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    // TODO: How do I detect errors?
                    this.encodeIndex++;
                    if (this.encodeIndex < this.jobQueue.Count)
                    {
                        this.encodingQueue.Start(this.jobQueue[this.encodeIndex], true);

                        TotalPercentComplete = Math.Round(
                            ((double)this.encodeIndex / (double)this.jobQueue.Count) * 100.0);
                    }
                    else
                    {
                        IsEncoding = this.encodingQueue.IsEncoding;
                        ETA = "Complete";
                        PercentComplete = 100;
                        TotalPercentComplete = 100;
                    }
                });
        }

        void OnEncodeStarted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    IsEncoding = this.encodingQueue.IsEncoding;
                    ETA = "Starting...";
                    PercentComplete = 0;
                });
        }

        void OnEncodeStatusChanged(object sender, EncodeProgressEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    if (this.encodingQueue.IsEncoding)
                    {
                        PercentComplete = e.PercentComplete;
                        ETA = String.Format(
                            "{0}% Complete. ETA: {1}", Math.Round(e.PercentComplete), e.EstimatedTimeLeft);
                    }
                });
        }

        public abstract void SelectMetadata(object metadata);

        public void Start()
        {
            this.jobQueue.Clear();
            this.encodeIndex = 0;

            AddEncodingJobs(this.jobQueue);
            if (this.jobQueue.Count > 0)
            {
                this.encodingQueue.Start(this.jobQueue[0], true);
            }
        }

        public abstract Task<object[]> StartQueryMetadata(string text, out CancellationToken token);

        public void Stop()
        {
            this.encodeIndex = 0;
            this.jobQueue.Clear();

            // Can't do this; WPF isn't processing messages correctly or something.
            // "SendKeys cannot run inside this application because the application is not handling Windows messages."
            // this.encodingQueue.SafelyClose();
            //
            this.encodingQueue.Stop();
        }
    }
}
