namespace EBrake
{
    using System;
    using System.ComponentModel;

    public class TitleEncodeInfo : INotifyPropertyChanged
    {
        string episode;
        string episodeTitle;
        int index;
        TimeSpan length;
        string season;
        int titleNumber;

        public string Episode
        {
            get { return this.episode; }
            set { this.episode = value; Notify("Episode"); }
        }

        public string EpisodeTitle
        {
            get { return this.episodeTitle; }
            set { this.episodeTitle = value; Notify("EpisodeTitle"); }
        }

        public int Index 
        {
            get { return this.index; }
            set { this.index = value; Notify("Index"); }
        }
        
        public TimeSpan Length 
        {
            get { return this.length; }
            set { this.length = value; Notify("Length"); }
        }
        
        public string Season 
        {
            get { return this.season; }
            set { this.season = value; Notify("Season"); }
        }

        public int TitleNumber
        {
            get { return this.titleNumber; }
            set { this.titleNumber = value; Notify("TitleNumber"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void Notify(string property)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(property)); }
        }
    }
}
