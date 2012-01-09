namespace EBrake.Metadata.Tmdb
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    [JsonObject(MemberSerialization.OptIn)]
    public class TmdbMovie
    {
        [JsonProperty("popularity")]
        public string Popularity { get; set; }

        [JsonProperty("translated")]
        public bool? Translated { get; set; }
        
        [JsonProperty("adult")]
        public bool Adult { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("original_name")]
        public string OriginalName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("alternative_name")]
        public string AlternativeName { get; set; }

        [JsonProperty("movie_type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("imdb_id")]
        public string ImdbId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("votes")]
        public string Votes { get; set; }

        [JsonProperty("rating")]
        public string Rating { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("tagline")]
        public string Tagline { get; set; }

        [JsonProperty("certification")]
        public string Certification { get; set; }

        [JsonProperty("overview")]
        public string Overview { get; set; }

        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; }

        [JsonProperty("released")]
        public string Released { get; set; }

        [JsonProperty("runtime")]
        public string Runtime { get; set; }

        [JsonProperty("budget")]
        public string Budget { get; set; }

        [JsonProperty("revenue")]
        public string Revenue { get; set; }

        [JsonProperty("homepage")]
        public string Homepage { get; set; }
       
        [JsonProperty("posters")]
        public List<TmdbImage> Posters { get; set; }

        [JsonProperty("backdrops")]
        public List<TmdbImage> Backdrops { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        public override string ToString()
        {
            DateTime release;
            if (DateTime.TryParse(Released, out release))
            {
                return String.Format("{0} ({1})", Name, release.Year);
            }

            return Name;            
        }
    }
}
