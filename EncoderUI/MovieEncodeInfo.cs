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

        public MovieEncodeInfo(MainWindow mainWindow, Queue encodingQueue)
            : base(mainWindow, encodingQueue)
        {
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
                if (!String.Equals(this.movieTitle, value, StringComparison.CurrentCulture))
                {
                    this.movieTitle = value; 
                    Notify("MovieTitle"); 
                }
            }
        }

        protected override void AddEncodingJobs(List<Job> encodingQueue)
        {
            string baseName = MovieTitle.Trim();

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

        public void SelectMetadata(TmdbMovie movie)
        {
            MovieTitle = movie.DisplayName;
            
            TmdbImage image = null;
            if (movie.Backdrops != null)
            {
                image = movie.Backdrops.OrderByDescending(i => i.Image.Width).FirstOrDefault();
            }            
            if (image != null) { Backdrop = image.Image.Url; } else { Backdrop = stockBackdrop; }
        }
    }
}
