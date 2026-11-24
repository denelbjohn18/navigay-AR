namespace Navigay.Position
{
    public readonly struct SnapFix
    {
        public readonly bool Locked;
        public readonly string EdgeId;
        public readonly string LaneId;
        public readonly double X;
        public readonly double Y;
        public readonly double DistanceM;
        public readonly int Segment;
        public readonly double T;

        public SnapFix(
            bool locked,
            string edgeId,
            string laneId,
            double x,
            double y,
            double distanceM,
            int segment,
            double t)
        {
            Locked = locked;
            EdgeId = edgeId;
            LaneId = laneId;
            X = x;
            Y = y;
            DistanceM = distanceM;
            Segment = segment;
            T = t;
        }

        public static SnapFix None => default;
    }
}
