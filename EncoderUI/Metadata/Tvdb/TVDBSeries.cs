namespace EBrake.Metadata.Tvdb
{
    using System;
    using System.Xml.Linq;

    public class TVDBSeries
    {
        public string Banner { get; set; }
        public string FirstAired { get; set; }
        public string Id { get; set; }
        public string ImdbId { get; set; }
        public string Language { get; set; }
        public string Overview { get; set; }
        public string SeriesId { get; set; }
        public string SeriesName { get; set; }
        public string Zap2ItId { get; set; }        

        public static TVDBSeries FromSearchResult(XElement element, string bannerRoot)
        {
            var series = new TVDBSeries();
            series.SeriesId = ValueOrEmpty(element.Element("seriesid"));
            series.Language = ValueOrEmpty(element.Element("language"));
            series.SeriesName = ValueOrEmpty(element.Element("SeriesName"));
            series.Banner = ValueOrEmpty(element.Element("banner"));
            series.Overview = ValueOrEmpty(element.Element("Overview"));
            series.FirstAired = ValueOrEmpty(element.Element("FirstAired"));
            series.ImdbId = ValueOrEmpty(element.Element("IMDB_ID"));
            series.Zap2ItId = ValueOrEmpty(element.Element("zaip2it_id"));
            series.Id = ValueOrEmpty(element.Element("id"));

            if (series.Banner.Length != 0) { series.Banner = bannerRoot + series.Banner; }

            return series;
        }

        static string ValueOrEmpty(XElement element)
        {
            return (element != null) ? element.Value : String.Empty;
        }

        public override string ToString()
        {
            return SeriesName;
        }
    }
}
