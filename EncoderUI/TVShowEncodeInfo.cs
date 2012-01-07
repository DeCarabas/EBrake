namespace EBrake
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.IO;
    using System.Windows.Threading;
    using HandBrake.ApplicationServices.Model;
    using HandBrake.ApplicationServices.Parsing;
    using HandBrake.ApplicationServices.Services;

    public sealed class TVShowEncodeInfo : EncodeInfo
    {
        readonly ObservableCollection<TitleEncodeInfo> episodes = new ObservableCollection<TitleEncodeInfo>();
        bool isScanning;
        bool propagating;
        readonly ScanService scanService = new ScanService();
        string series;

        public TVShowEncodeInfo(MainWindow window, Queue encodingQueue)
            : base(window, encodingQueue)
        {
            this.episodes.CollectionChanged += OnEpisodesChanged;

            this.scanService.ScanStared += OnScanStarted;
            this.scanService.ScanStatusChanged += OnScanStatusChanged;
            this.scanService.ScanCompleted += OnScanCompleted;

            ScanDisc();
        }

        public IList<TitleEncodeInfo> Episodes { get { return this.episodes; } }

        public override InfoError InfoError
        {
            get 
            {
                if (this.series == null) { return InfoError.NoSeriesTitle; }
                for (int i = 0; i < this.episodes.Count; i++)
                {
                    TitleEncodeInfo episode = this.episodes[i];
                    if (!String.IsNullOrWhiteSpace(episode.Season) && !String.IsNullOrWhiteSpace(episode.Episode))
                    {
                        return InfoError.OK;
                    }
                }
                if (SourceDrive != null && !SourceDrive.IsReady) { return InfoError.DiscNotReady; }
                return InfoError.NoEpisodesToEncode;
            }
        }

        public bool IsScanning
        {
            get { return this.isScanning; }
            set { this.isScanning = value; Notify("IsScanning"); }
        }

        public string Series
        {
            get { return this.series; }
            set { this.series = value; Notify("Series"); }
        }

        protected override void AddEncodingJobs(List<Job> encodingQueue)
        {
            string source = Path.Combine(SourceDrive.RootDirectory.FullName, "VIDEO_TS");
            string targetBase = Path.Combine(OutputPath, EscapeFileName(Series));
            foreach (TitleEncodeInfo titleInfo in this.episodes)
            {
                if (String.IsNullOrWhiteSpace(titleInfo.Season) || String.IsNullOrWhiteSpace(titleInfo.Episode))
                {
                    continue;
                }

                string seasonString = GetDigitString(titleInfo.Season);
                string episodeString = GetDigitString(titleInfo.Episode);

                string outputFileName = String.Format("{0} S{1}E{2}", Series, seasonString, episodeString);
                if (!String.IsNullOrWhiteSpace(titleInfo.EpisodeTitle))
                {
                    outputFileName += " " + titleInfo.EpisodeTitle;
                }
                outputFileName += ".m4v";

                string outputDirectory = Path.Combine(
                    targetBase, 
                    String.Format("Season {0}", EscapeFileName(titleInfo.Season)));
                if (!Directory.Exists(outputDirectory)) { Directory.CreateDirectory(outputDirectory); }

                string outputFile = Path.Combine(outputDirectory, EscapeFileName(outputFileName));                

                string commandLine = String.Format("-t {0} ", titleInfo.TitleNumber) + 
                    GetStandardCommandLine(source, outputFile);

                encodingQueue.Add(new Job 
                {
                    Query = commandLine, 
                    Title = titleInfo.TitleNumber, 
                    Source = source, 
                    Destination = outputFile, 
                    CustomQuery = true
                });
            }
        }

        static string GetDigitString(string value)
        {
            string result = value;
            int i;
            if (int.TryParse(value, out i)) { result = string.Format("{0:00}", i); }
            return result;
        }

        protected override void Notify(string property)
        {
            base.Notify(property);
            if (property == "Series") { Notify("InfoError"); }
        }

        void OnEpisodesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                for (int i = 0; i < e.OldItems.Count; i++)
                {
                    ((TitleEncodeInfo)e.OldItems[i]).PropertyChanged -= OnTitleEncodeInfoChanged;
                }
            }

            if (e.NewItems != null)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    ((TitleEncodeInfo)e.NewItems[i]).PropertyChanged += OnTitleEncodeInfoChanged;
                }
            }
        }

        void OnScanCompleted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                this.IsScanning = this.scanService.IsScanning;
                this.episodes.Clear();

                DVD dvd = this.scanService.SouceData;
                if (dvd != null)
                {
                    for (int i = 0; i < dvd.Titles.Count; i++)
                    {
                        if (dvd.Titles[i].Duration < TimeSpan.FromMinutes(2)) { continue; }
                        this.episodes.Add(new TitleEncodeInfo
                        {
                            Index = this.episodes.Count + 1,
                            Length = dvd.Titles[i].Duration,
                            TitleNumber = dvd.Titles[i].TitleNumber
                        });
                    }
                }
            });
        }

        void OnScanStarted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                this.IsScanning = this.scanService.IsScanning;
            });
        }

        void OnScanStatusChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                this.IsScanning = this.scanService.IsScanning;
            });
        }

        protected override void OnSourceDriveChanged()
        {
            base.OnSourceDriveChanged();

            this.episodes.Clear();
            ScanDisc();

            Notify("InfoError");
        }

        void OnTitleEncodeInfoChanged(object sender, PropertyChangedEventArgs args)
        {
            VerifyAccess();

            if (this.propagating) { return; }
            this.propagating = true;
            try
            {
                var episode = (TitleEncodeInfo)sender;
                if (args.PropertyName == "Season")
                {
                    // Default assumption: all titles on this disc are from the same season.
                    foreach (TitleEncodeInfo otherEpisode in this.episodes)
                    {
                        if (String.IsNullOrEmpty(otherEpisode.Season))
                        {
                            otherEpisode.Season = episode.Season;
                        }
                    }
                }
                else if (args.PropertyName == "Episode" && episode.Index == 1)
                {
                    int episodeNumber;
                    if (int.TryParse(episode.Episode, out episodeNumber))
                    {
                        for (int i = 1; i < this.episodes.Count; i++)
                        {
                            if (String.IsNullOrWhiteSpace(this.episodes[i].Episode))
                            {
                                this.episodes[i].Episode = (episodeNumber + i).ToString();
                            }
                        }
                    }
                }
            }
            finally
            {
                this.propagating = false;
            }

            Notify("InfoError");
        }

        public void ScanDisc()
        {
            VerifyAccess();
            if (this.scanService.IsScanning) { scanService.Stop(); }
            if (SourceDrive != null && SourceDrive.IsReady)
            {
                scanService.Scan(Path.Combine(SourceDrive.RootDirectory.FullName, "VIDEO_TS"), 0);
            }
        }
    }
}
