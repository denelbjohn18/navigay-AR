using Newtonsoft.Json;

namespace Navigay.Data
{
    public sealed class GraphEdge
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("kind")]
        public string Kind;

        [JsonProperty("from")]
        public string From;

        [JsonProperty("to")]
        public string To;

        [JsonProperty("lane_id")]
        public string LaneId;

        [JsonProperty("access")]
        public string Access;

        [JsonProperty("length_m")]
        public double LengthM;

        [JsonProperty("polyline")]
        public double[][] Polyline;
    }
}
