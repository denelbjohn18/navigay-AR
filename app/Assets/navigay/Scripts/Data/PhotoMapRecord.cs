using Newtonsoft.Json;

namespace Navigay.Data
{
    public sealed class PhotoMapRecord
    {
        [JsonProperty("pmap_id")]
        public string PmapId;

        [JsonProperty("file")]
        public string File;

        [JsonProperty("x")]
        public double X;

        [JsonProperty("y")]
        public double Y;

        [JsonProperty("z")]
        public double Z;

        [JsonProperty("yaw_deg")]
        public double YawDeg;

        [JsonProperty("load_radius_m")]
        public double LoadRadiusM;

        [JsonProperty("junction_ids")]
        public string[] JunctionIds;
    }
}
