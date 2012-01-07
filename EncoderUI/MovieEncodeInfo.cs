namespace EBrake
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using HandBrake.ApplicationServices.Model;
    using HandBrake.ApplicationServices.Services;

    public sealed class MovieEncodeInfo : EncodeInfo
    {
        string movieTitle;
        string movieYear;

        public MovieEncodeInfo(MainWindow mainWindow, Queue encodingQueue)
            : base(mainWindow, encodingQueue)
        {
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
            set { this.movieTitle = value; Notify("MovieTitle"); }
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
    }
}
