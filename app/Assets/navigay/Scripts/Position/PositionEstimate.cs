using UnityEngine;

namespace Navigay.Position
{
    public readonly struct PositionEstimate
    {
        public readonly Vector2 MapPosition;
        public readonly float HeadingDeg;
        public readonly float PositionErrorM;
        public readonly float HeadingErrorDeg;
        public readonly PositionSource Source;
        public readonly TrackingState Tracking;
        public readonly double Timestamp;

        public PositionEstimate(
            Vector2 mapPosition,
            float headingDeg,
            float positionErrorM,
            float headingErrorDeg,
            PositionSource source,
            TrackingState tracking,
            double timestamp)
        {
            MapPosition = mapPosition;
            HeadingDeg = headingDeg;
            PositionErrorM = positionErrorM;
            HeadingErrorDeg = headingErrorDeg;
            Source = source;
            Tracking = tracking;
            Timestamp = timestamp;
        }
    }
}
