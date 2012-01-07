namespace EBrake
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using EBrake.Metadata.Tmdb;
    using HandBrake.ApplicationServices.Model;
    using HandBrake.ApplicationServices.Services;

    public sealed class MovieEncodeInfo : EncodeInfo
    {
        static readonly Random random = new Random();
        const string stockBackdrop = "/EBrake;component/Popcorn.jpg";

        string backdrop = stockBackdrop;
        string movieTitle;
        string movieYear;
        CancellationTokenSource queryCancellationSource;
        DispatcherTimer queryTimer = new DispatcherTimer();

        public MovieEncodeInfo(MainWindow mainWindow, Queue encodingQueue)
            : base(mainWindow, encodingQueue)
        {
            this.queryTimer.Interval = TimeSpan.FromSeconds(1);
            this.queryTimer.Tick += QueryMetadata;
            this.queryTimer.IsEnabled = false;
        }

        public string Backdrop
        {
            get { return this.backdrop; }
            set { this.backdrop = value; Notify("Backdrop"); }
        }

        public override InfoError InfoError
        {
            get 
            {
                if (String.IsNullOrWhiteSpace(MovieTitle)) { return InfoError.NoMovieTitle; }
                if (SourceDrive != null && !SourceDrive.IsReady) { return InfoError.DiscNotReady; }
                return InfoError.OK; 
            }
        }

        public string MovieTitle
        {
            get { return this.movieTitle; }
            set 
            { 
                this.movieTitle = value;

                if (this.queryTimer.IsEnabled) { this.queryTimer.Stop(); }
                this.queryTimer.Start();

                Notify("MovieTitle");
            }
        }

        public string MovieYear
        {
            get { return this.movieYear; }
            set { this.movieYear = value; Notify("MovieYear"); }
        }

        protected override void AddEncodingJobs(List<Job> encodingQueue)
        {
            string baseName = MovieTitle.Trim();
            if (!String.IsNullOrWhiteSpace(MovieYear)) { baseName += String.Format(" ({0})", MovieYear.Trim()); }

            string movieDirectory = Path.Combine(OutputPath, EscapeFileName(baseName));
            if (!Directory.Exists(movieDirectory)) { Directory.CreateDirectory(movieDirectory); }
            
            string outputFile = Path.Combine(movieDirectory, EscapeFileName(baseName) + ".m4v");

            string commandLine = "--main-feature " + 
                GetStandardCommandLine(Path.Combine(SourceDrive.RootDirectory.FullName, "VIDEO_TS"), outputFile);

            encodingQueue.Add(new Job
            {
                Query = commandLine, 
                Title = 0, 
                Source = SourceDrive.RootDirectory.FullName, 
                Destination = outputFile, 
                CustomQuery = true
            });
        }

        protected override void Notify(string property)
        {
            base.Notify(property);
            if (property == "MovieTitle") { Notify("InfoError"); }
        }

        protected override void OnSourceDriveChanged()
        {
            base.OnSourceDriveChanged();
            Notify("InfoError");
        }

        void QueryMetadata(object sender, EventArgs e)
        {
            this.queryTimer.Stop();

            if (this.queryCancellationSource != null) { this.queryCancellationSource.Cancel(); }
            this.queryCancellationSource = new CancellationTokenSource();
            CancellationToken token = this.queryCancellationSource.Token;
            MovieInfo.StartQueryMetadata(MovieTitle, MovieYear, token)
                .ContinueWith(t => Dispatcher.BeginInvoke(UpdateMetadata, t, token), token);
        }

        void UpdateMetadata(Task<TmdbMovie[]> task, CancellationToken token)
        {
            VerifyAccess();
            //
            // Last chance for cancellation... after this, we know we won't get canceled because cancellation can 
            // only happen on this thread.
            //
            if (task.Status == TaskStatus.Canceled || token.IsCancellationRequested) { return; }

            string newBackdrop = null;
            try
            {
                TmdbMovie[] movies = task.Result;
                if (movies.Length > 0 && movies[0].Backdrops.Count > 0)
                {
                    TmdbMovie movie = movies[0];
                    TmdbImage image = movie.Backdrops.OrderByDescending(i => i.Image.Width).First();
                    newBackdrop = image.Image.Url;
                }
            }
            catch
            {
                // Oh well, we did our best.
            }

            Backdrop = newBackdrop ?? stockBackdrop;
        }
    }
}
