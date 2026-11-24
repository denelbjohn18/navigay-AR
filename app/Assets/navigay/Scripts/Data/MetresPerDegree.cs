using Newtonsoft.Json;

namespace Navigay.Data
{
    public sealed class MetresPerDegree
    {
        [JsonProperty("x")]
        public double X;

        [JsonProperty("y")]
        public double Y;
    }
}
