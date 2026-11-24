using Navigay.Routing;

namespace Navigay.Position
{
    public enum ReadyWord
    {
        FindingYou,
        InsideNamed,
        InsideBuilding,
        StepIntoTheOpen,
        NoPath,
    }

    public readonly struct ReadyView
    {
        public readonly ReadyWord Word;
        public readonly string BlockName;
        public readonly bool PreciseLocation;
        public readonly bool Guiding;
        public readonly Route Route;

        public ReadyView(ReadyWord word, string blockName, bool preciseLocation, bool guiding, Route route)
        {
            Word = word;
            BlockName = blockName;
            PreciseLocation = preciseLocation;
            Guiding = guiding;
            Route = route;
        }
    }
}
