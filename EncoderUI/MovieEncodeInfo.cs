namespace EncoderUI
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
            if (!String.IsNullOrWhiteSpace(MovieYear)) { baseName += String.Format("({0})", MovieYear.Trim()); }
            baseName += ".m4v";

            string outputFileName = EscapeFileName(baseName);            
            string outputFile = Path.Combine(OutputPath, outputFileName);

            string commandLine = String.Format(
                "--main-feature -o \"{0}\" -i \"{1}\" -m -e x264 --native-language eng --decomb --strict-anamorphic -q 20",
                outputFile,
                Path.Combine(SourceDrive.RootDirectory.FullName, "VIDEO_TS"));

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
        }
    }
}
