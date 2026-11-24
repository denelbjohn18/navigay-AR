namespace Navigay.Routing
{
    public sealed class Route
    {
        public string PlaceId;
        public string DoorId;
        public double LengthM;
        public string[] NodeIds;
        public string[] EdgeIds;
        public string[] LaneIds;
        public double[][] Polyline;
        public Checkpoint[] Checkpoints;
    }
}
