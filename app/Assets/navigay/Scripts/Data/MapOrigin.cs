using Newtonsoft.Json;

namespace Navigay.Data
{
    public sealed class MapOrigin
    {
        [JsonProperty("lat")]
        public double Lat;

        [JsonProperty("lon")]
        public double Lon;
    }
}
