namespace EBrake.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using EBrake.Metadata.Tvdb;

    public static class TVInfo
    {
        const string TheTVDBKey = "D104E30D46D7F823";
        static readonly Random Random = new Random();

        static string[] BannerMirrors;
        static string[] XmlMirrors;
        static string[] ZipMirrors;

        static string BannerRoot
        {
            get
            {
                if (BannerMirrors == null) { GetMirrors(); }
                return BannerMirrors[Random.Next(BannerMirrors.Length)] + "/api/" + TheTVDBKey + "/";
            }
        }

        static string XmlRoot
        {
            get
            {
                if (XmlMirrors == null) { GetMirrors(); }
                return XmlMirrors[Random.Next(XmlMirrors.Length)] + "/api/" + TheTVDBKey + "/";
            }
        }

        static string ZipRoot
        {
            get
            {
                if (ZipMirrors == null) { GetMirrors(); }
                return ZipMirrors[Random.Next(ZipMirrors.Length)] + "/api/" + TheTVDBKey + "/";
            }
        }

        static void GetMirrors()
        {
            List<string> xmlMirrorList = new List<string>();
            List<string> bannerMirrorList = new List<string>();
            List<string> zipMirrorList = new List<string>();

            const string mirrorsUrl = "http://www.thetvdb.com/api/" + TheTVDBKey + "/mirrors.xml";
            var request = (HttpWebRequest)WebRequest.Create(mirrorsUrl);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    var doc = XDocument.Load(stream);
                    foreach (XElement mirror in doc.Ancestors("Mirror"))
                    {
                        string baseUri = mirror.Element("mirrorbase").Value;
                        int type = Int32.Parse(mirror.Element("typemask").Value);

                        if ((type & 1) != 0) { xmlMirrorList.Add(baseUri); }
                        if ((type & 2) != 0) { bannerMirrorList.Add(baseUri); }
                        if ((type & 4) != 0) { zipMirrorList.Add(baseUri); }
                    }
                }
            }

            XmlMirrors = xmlMirrorList.ToArray();
            BannerMirrors = bannerMirrorList.ToArray();
            ZipMirrors = zipMirrorList.ToArray();
        }

        static string BuildQueryUrl(string title)
        {
            string url = "http://www.thetvdb.com/api/GetSeries.php?seriesname=" + Uri.EscapeUriString(title) + "&language=en";
            return url;
        }

        public static Task<TVDBSeries[]> StartQueryMetadata(string title, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                string url = BuildQueryUrl(title);
                cancellationToken.ThrowIfCancellationRequested();

                var request = (HttpWebRequest)WebRequest.Create(url);
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var stream = response.GetResponseStream())
                    {
                        var results = new List<TVDBSeries>();

                        XDocument doc = XDocument.Load(stream);
                        foreach (XElement element in doc.Ancestors("Series"))
                        {
                            results.Add(TVDBSeries.FromSearchResult(element, BannerRoot));
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        return results.ToArray();
                    }                    
                }
            }, cancellationToken);
        }
    }
}
