using Newtonsoft.Json;

namespace Navigay.Data
{
    public sealed class GraphData
    {
        [JsonProperty("schema")]
        public int Schema;

        [JsonProperty("origin")]
        public MapOrigin Origin;

        [JsonProperty("metres_per_deg")]
        public MetresPerDegree MetresPerDegree;

        [JsonProperty("nodes")]
        public GraphNode[] Nodes;

        [JsonProperty("edges")]
        public GraphEdge[] Edges;

        [JsonProperty("places")]
        public PlaceRecord[] Places;

        [JsonProperty("blocks")]
        public BlockOutline[] Blocks;

        [JsonProperty("photo_maps")]
        public PhotoMapRecord[] PhotoMaps;
    }
}
