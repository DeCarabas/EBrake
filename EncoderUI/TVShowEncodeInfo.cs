namespace EncoderUI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using HandBrake.ApplicationServices.Services;

    public class TitleEncodeInfo
    {
        public int Index { get; set; }
        public TimeSpan Length { get; set; }
        public string Episode { get; set; }
        public string Season { get; set; }
        public string EpisodeTitle { get; set; }
    }

    public sealed class TVShowEncodeInfo : EncodeInfo
    {
        string series;
        readonly ObservableCollection<TitleEncodeInfo> episodes = new ObservableCollection<TitleEncodeInfo>();

        public TVShowEncodeInfo(MainWindow window, Queue encodingQueue)
            : base(window, encodingQueue)
        {
            this.episodes.Add(new TitleEncodeInfo { Index = 1, Length = TimeSpan.FromMinutes(30) });
            this.episodes.Add(new TitleEncodeInfo { Index = 2, Length = TimeSpan.FromMinutes(30) });
            this.episodes.Add(new TitleEncodeInfo { Index = 3, Length = TimeSpan.FromMinutes(30) });
            this.episodes.Add(new TitleEncodeInfo { Index = 4, Length = TimeSpan.FromMinutes(30) });
            this.episodes.Add(new TitleEncodeInfo { Index = 5, Length = TimeSpan.FromMinutes(30) });
            this.episodes.Add(new TitleEncodeInfo { Index = 6, Length = TimeSpan.FromMinutes(30) });
            this.episodes.Add(new TitleEncodeInfo { Index = 7, Length = TimeSpan.FromMinutes(30) });
        }

        public IList<TitleEncodeInfo> Episodes { get { return this.episodes; } }

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
