using Newtonsoft.Json;

namespace Navigay.Data
{
    public sealed class PlaceRecord
    {
        [JsonProperty("place_id")]
        public string PlaceId;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("access")]
        public string Access;

        [JsonProperty("door_ids")]
        public string[] DoorIds;

        [JsonProperty("start")]
        public bool Start;

        [JsonIgnore]
        public PlaceKind Kind;
    }
}
