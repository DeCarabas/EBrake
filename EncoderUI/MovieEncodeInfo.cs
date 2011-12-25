namespace EncoderUI
{
    using System;
    using System.IO;
    using HandBrake.ApplicationServices.Services;

    public sealed class MovieEncodeInfo : EncodeInfo
    {
        string movieTitle;
        string movieYear;

        public MovieEncodeInfo(MainWindow mainWindow, Queue encodingQueue)
            : base(mainWindow, encodingQueue)
        {
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

        protected override void AddEncodingJobs(Queue encodingQueue)
        {
            string outputFileName = EscapeFileName(String.Format("{0} ({1}).m4v", MovieTitle, MovieYear));
            string outputFile = Path.Combine(OutputPath, outputFileName);

            string commandLine = String.Format(
                "--main-feature -o \"{0}\" -i \"{1}\" -m -e x264 --native-language eng --decomb --strict-anamorphic -q 20",
                outputFile,
                Path.Combine(SourceDrive.RootDirectory.FullName, "VIDEO_TS"));

            encodingQueue.Add(commandLine, 0, SourceDrive.RootDirectory.FullName, outputFile, true);
        }
    }
}
