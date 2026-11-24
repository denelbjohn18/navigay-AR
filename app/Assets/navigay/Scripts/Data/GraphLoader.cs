using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Navigay.Data
{
    public static class GraphLoader
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            FloatParseHandling = FloatParseHandling.Double,
        };

        public static GraphData LoadFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new GraphLoadException("graph.json is missing.");
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (IOException exception)
            {
                throw new GraphLoadException("graph.json could not be read.", exception);
            }

            return Load(json);
        }

        public static GraphData Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new GraphLoadException("graph.json is empty.");
            }

            GraphData data;
            try
            {
                data = JsonConvert.DeserializeObject<GraphData>(json, Settings);
            }
            catch (JsonException exception)
            {
                throw new GraphLoadException("graph.json is not valid JSON.", exception);
            }

            if (data == null || data.Schema != 1)
            {
                throw new GraphLoadException("graph.json schema is not 1.");
            }

            if (data.Origin == null || double.IsNaN(data.Origin.Lat) || double.IsNaN(data.Origin.Lon))
            {
                throw new GraphLoadException("graph.json origin is missing.");
            }

            if (data.MetresPerDegree == null || data.MetresPerDegree.X <= 0 || data.MetresPerDegree.Y <= 0)
            {
                throw new GraphLoadException("graph.json metres_per_deg is missing.");
            }

            if (data.Places == null)
            {
                throw new GraphLoadException("graph.json has no places.");
            }

            data.Nodes ??= Array.Empty<GraphNode>();
            data.Edges ??= Array.Empty<GraphEdge>();
            data.Blocks ??= Array.Empty<BlockOutline>();
            data.PhotoMaps ??= Array.Empty<PhotoMapRecord>();

            var seen = new HashSet<string>();
            foreach (var place in data.Places)
            {
                if (place == null || string.IsNullOrWhiteSpace(place.PlaceId) || string.IsNullOrWhiteSpace(place.Name))
                {
                    throw new GraphLoadException("graph.json has a place without an id or a name.");
                }

                if (!seen.Add(place.PlaceId))
                {
                    throw new GraphLoadException("graph.json repeats " + place.PlaceId + ".");
                }

                if (!KnownAccess(place.Access))
                {
                    throw new GraphLoadException("graph.json place " + place.PlaceId + " has access " + place.Access + ".");
                }

                try
                {
                    place.Kind = PlaceTypeWord.Parse(place.Type);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    throw new GraphLoadException("graph.json place " + place.PlaceId + " has type " + place.Type + ".", exception);
                }

                place.DoorIds ??= Array.Empty<string>();
            }

            return data;
        }

        static bool KnownAccess(string access)
        {
            switch (access)
            {
                case "all":
                case "boys":
                case "girls":
                case "staff":
                    return true;
                default:
                    return false;
            }
        }
    }
}
