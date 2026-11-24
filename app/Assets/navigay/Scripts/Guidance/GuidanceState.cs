using Navigay.Routing;

namespace Navigay.Guidance
{
    public readonly struct GuidanceState
    {
        public readonly Route Route;
        public readonly int CheckpointIndex;
        public readonly double AlongM;
        public readonly double RemainingM;
        public readonly int ShownCountdownM;
        public readonly int MinutesLeft;
        public readonly TurnKind Turn;
        public readonly string Landmark;
        public readonly string ExtraSentence;
        public readonly bool Inside;
        public readonly bool Arrived;
        public readonly ConfidenceLevel Level;

        public GuidanceState(
            Route route,
            int checkpointIndex,
            double alongM,
            double remainingM,
            int shownCountdownM,
            int minutesLeft,
            TurnKind turn,
            string landmark,
            string extraSentence,
            bool inside,
            bool arrived,
            ConfidenceLevel level)
        {
            Route = route;
            CheckpointIndex = checkpointIndex;
            AlongM = alongM;
            RemainingM = remainingM;
            ShownCountdownM = shownCountdownM;
            MinutesLeft = minutesLeft;
            Turn = turn;
            Landmark = landmark;
            ExtraSentence = extraSentence;
            Inside = inside;
            Arrived = arrived;
            Level = level;
        }

        public Checkpoint Checkpoint =>
            Route?.Checkpoints != null && CheckpointIndex >= 0 && CheckpointIndex < Route.Checkpoints.Length
                ? Route.Checkpoints[CheckpointIndex]
                : null;
    }
}
