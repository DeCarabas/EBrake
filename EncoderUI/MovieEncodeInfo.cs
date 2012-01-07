namespace EBrake
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
        readonly ObservableCollection<TmdbMovie> potentialMatches = new ObservableCollection<TmdbMovie>();
        CancellationTokenSource queryCancellationSource;
        DispatcherTimer queryTimer = new DispatcherTimer();

        public MovieEncodeInfo(MainWindow mainWindow, Queue encodingQueue)
            : base(mainWindow, encodingQueue)
        {
            this.queryTimer.Interval = TimeSpan.FromMilliseconds(500);
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
                if (!String.Equals(value, MovieTitle))
                {
                    this.movieTitle = value;

                    if (this.queryTimer.IsEnabled) { this.queryTimer.Stop(); }
                    this.queryTimer.Start();

                    Notify("MovieTitle");
                }
            }
        }

        public string MovieYear
        {
            get { return this.movieYear; }
            set { this.movieYear = value; Notify("MovieYear"); }
        }

        public IList<TmdbMovie> PotentialMatches { get { return this.potentialMatches; } }

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

            if (String.IsNullOrWhiteSpace(MovieTitle))
            {
                Backdrop = stockBackdrop;
            }
            else
            {
                if (this.queryCancellationSource != null) { this.queryCancellationSource.Cancel(); }
                this.queryCancellationSource = new CancellationTokenSource();
                CancellationToken token = this.queryCancellationSource.Token;
                MovieInfo.StartQueryMetadata(MovieTitle, token)
                    .ContinueWith(t => Dispatcher.BeginInvoke(UpdateMetadata, t, token), token);
            }
        }

        public void SelectMetadata(TmdbMovie movie)
        {            
            MovieTitle = movie.Name;

            DateTime released;
            if (DateTime.TryParse(movie.Released, out released))
            {
                MovieYear = released.Year.ToString();
            }

            TmdbImage image = movie.Backdrops.OrderByDescending(i => i.Image.Width).FirstOrDefault();
            if (image != null) { Backdrop = image.Image.Url; } else { Backdrop = stockBackdrop; }
        }

        void UpdateMetadata(Task<TmdbMovie[]> task, CancellationToken token)
        {
            VerifyAccess();
            //
            // Last chance for cancellation... after this, we know we won't get canceled because cancellation can 
            // only happen on this thread.
            //
            if (task.Status != TaskStatus.RanToCompletion || token.IsCancellationRequested) { return; }

            bool matched = false;
            TmdbMovie[] movies = null;
            try
            {
                movies = task.Result;
                if (movies.Length > 0)
                {
                    TmdbMovie movie = movies[0];
                    if (String.Equals(movie.Name, MovieTitle, StringComparison.CurrentCultureIgnoreCase))
                    {
                        SelectMetadata(movie);
                        matched = true;
                    }
                }
            }
            catch
            {
                // Oh well, we did our best.
            }

            if (!matched) { Backdrop = stockBackdrop; }
            this.potentialMatches.Clear();
            if (movies != null)
            {
                for (int i = 0; i < movies.Length; i++) { this.potentialMatches.Add(movies[i]); }
            }
        }
    }
}
