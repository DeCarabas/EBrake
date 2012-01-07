namespace EBrake.Metadata.Tmdb
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    // N.B. This is a subset...
    [JsonObject(MemberSerialization.OptIn)]
    public class TmdbImageInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
    }
}
