using Navigay.Data;

namespace Navigay.UI
{
    public sealed class PlaceSection
    {
        public PlaceSection(string heading, PlaceRecord[] places)
        {
            Heading = heading;
            Places = places;
        }

        public string Heading { get; }

        public PlaceRecord[] Places { get; }
    }
}
