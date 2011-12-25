namespace EncoderUI
{
    using System;
    using HandBrake.ApplicationServices.Services;

    public sealed class TVShowEncodeInfo : EncodeInfo
    {
        string season;
        string series;

        public TVShowEncodeInfo(MainWindow window, Queue encodingQueue)
            : base(window, encodingQueue)
        { 
        }

        public string Season
        {
            get { return this.season; }
            set { this.season = value; Notify("Season"); }
        }

        public string Series
        {
            get { return this.series; }
            set { this.series = value; Notify("Series"); }
        }

        protected override void AddEncodingJobs(Queue encodingQueue)
        {
            throw new NotImplementedException();
        }
    }
}
