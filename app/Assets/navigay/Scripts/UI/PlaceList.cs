using System;
using System.Collections.Generic;
using Navigay.Data;

namespace Navigay.UI
{
    public static class PlaceList
    {
        public const string NearYouHeading = "Near you.";

        public static PlaceSection[] Build(
            IReadOnlyList<PlaceRecord> places,
            string query,
            StuckRanking ranking,
            string stuckId,
            IReadOnlyDictionary<string, double> routeMetres)
        {
            if (places == null)
            {
                throw new ArgumentNullException(nameof(places));
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var needle = query.Trim();
                var matches = new List<PlaceRecord>();
                for (var i = 0; i < places.Count; i++)
                {
                    var place = places[i];
                    if (place.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matches.Add(place);
                    }
                }

                matches.Sort((left, right) => NameOrder.Instance.Compare(left.Name, right.Name));
                return new[] { new PlaceSection(null, matches.ToArray()) };
            }

            if (!string.IsNullOrEmpty(stuckId))
            {
                if (ranking == null)
                {
                    throw new ArgumentNullException(nameof(ranking));
                }

                return new[]
                {
                    new PlaceSection(NearYouHeading, ranking.Order(places, stuckId, routeMetres)),
                };
            }

            var sections = new List<PlaceSection>();
            foreach (var kind in PlaceTypeWord.Groups)
            {
                var rows = new List<PlaceRecord>();
                for (var i = 0; i < places.Count; i++)
                {
                    if (places[i].Kind == kind)
                    {
                        rows.Add(places[i]);
                    }
                }

                if (rows.Count == 0)
                {
                    continue;
                }

                rows.Sort((left, right) => NameOrder.Instance.Compare(left.Name, right.Name));
                sections.Add(new PlaceSection(PlaceTypeWord.Heading(kind), rows.ToArray()));
            }

            return sections.ToArray();
        }
    }
}
