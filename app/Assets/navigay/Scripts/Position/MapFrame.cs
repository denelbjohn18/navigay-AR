using System;
using Navigay.Data;

namespace Navigay.Position
{
    public readonly struct MapFrame
    {
        public readonly double OriginLat;
        public readonly double OriginLon;
        public readonly double MetresX;
        public readonly double MetresY;

        public MapFrame(double originLat, double originLon, double metresX, double metresY)
        {
            OriginLat = originLat;
            OriginLon = originLon;
            MetresX = metresX;
            MetresY = metresY;
        }

        public static MapFrame From(GraphData graph)
        {
            return new MapFrame(graph.Origin.Lat, graph.Origin.Lon, graph.MetresPerDegree.X, graph.MetresPerDegree.Y);
        }

        public void ToMap(double latitude, double longitude, out double x, out double y)
        {
            var scale = Math.Cos(OriginLat * Math.PI / 180d);
            x = (longitude - OriginLon) * scale * MetresX;
            y = (latitude - OriginLat) * MetresY;
        }
    }
}
