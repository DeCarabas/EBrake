namespace EBrake
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Windows.Threading;
    using HandBrake.ApplicationServices;
    using HandBrake.ApplicationServices.Model;
    using HandBrake.ApplicationServices.Services;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Net;
    using Newtonsoft.Json;
    using System.Text;
    using EBrake.Metadata.Tmdb;

    public static class MovieInfo
    {
        const string TheMovieDBKey = "1d13cf5cbcc52fadc0107aa5baa9be3e";
        const string BaseUri = "http://api.themoviedb.org/2.1/Movie.search/en/json";

        static string BuildQueryUrl(string title)
        {
            string url = BaseUri + "/" + TheMovieDBKey + "/" + Uri.EscapeUriString(title);
            return url;
        }

        public static Task<TmdbMovie[]> StartQueryMetadata(string title, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                string url = BuildQueryUrl(title);
                cancellationToken.ThrowIfCancellationRequested();

                var request = (HttpWebRequest)WebRequest.Create(url);
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        return JsonConvert.DeserializeObject<List<TmdbMovie>>(reader.ReadToEnd()).ToArray();
                    }
                }
            }, cancellationToken);
        }
    }
}
