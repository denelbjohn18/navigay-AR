using Newtonsoft.Json;

namespace Navigay.Data
{
    public sealed class GraphNode
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("kind")]
        public string Kind;

        [JsonProperty("x")]
        public double X;

        [JsonProperty("y")]
        public double Y;

        [JsonProperty("lat")]
        public double Lat;

        [JsonProperty("lon")]
        public double Lon;

        [JsonProperty("place_id")]
        public string PlaceId;

        [JsonProperty("landmark")]
        public string Landmark;

        [JsonProperty("edge_ids")]
        public string[] EdgeIds;
    }
}
