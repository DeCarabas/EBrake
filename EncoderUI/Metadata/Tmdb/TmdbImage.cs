namespace EBrake.Metadata.Tmdb
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    [JsonObject(MemberSerialization.OptIn)]
    public class TmdbImage
    {
        [JsonProperty("image")]
        public TmdbImageInfo Image { get; set; }
    }


}
