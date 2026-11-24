using Newtonsoft.Json;

namespace Navigay.Data
{
    public sealed class BlockOutline
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("place_id")]
        public string PlaceId;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("rings")]
        public double[][][] Rings;
    }
}
