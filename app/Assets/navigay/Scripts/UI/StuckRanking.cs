using System;
using System.Collections.Generic;
using Navigay.Data;

namespace Navigay.UI
{
    public sealed class StuckRanking
    {
        string _stuckId;
        string[] _order;

        public PlaceRecord[] Order(IReadOnlyList<PlaceRecord> places, string stuckId, IReadOnlyDictionary<string, double> routeMetres)
        {
            if (places == null)
            {
                throw new ArgumentNullException(nameof(places));
            }

            if (string.IsNullOrEmpty(stuckId))
            {
                throw new ArgumentException("A stuck point needs an id.", nameof(stuckId));
            }

            if (_stuckId == stuckId && _order != null)
            {
                return Replay(places);
            }

            var ordered = new PlaceRecord[places.Count];
            for (var i = 0; i < places.Count; i++)
            {
                ordered[i] = places[i];
            }

            Array.Sort(ordered, (left, right) =>
            {
                var compared = Metres(left, routeMetres).CompareTo(Metres(right, routeMetres));
                if (compared != 0)
                {
                    return compared;
                }

                return NameOrder.Instance.Compare(left.Name, right.Name);
            });

            _stuckId = stuckId;
            _order = new string[ordered.Length];
            for (var i = 0; i < ordered.Length; i++)
            {
                _order[i] = ordered[i].PlaceId;
            }

            return ordered;
        }

        PlaceRecord[] Replay(IReadOnlyList<PlaceRecord> places)
        {
            var replay = new PlaceRecord[_order.Length];
            var count = 0;
            foreach (var id in _order)
            {
                for (var i = 0; i < places.Count; i++)
                {
                    if (places[i].PlaceId == id)
                    {
                        replay[count] = places[i];
                        count++;
                        break;
                    }
                }
            }

            if (count == replay.Length)
            {
                return replay;
            }

            var trimmed = new PlaceRecord[count];
            Array.Copy(replay, trimmed, count);
            return trimmed;
        }

        static double Metres(PlaceRecord place, IReadOnlyDictionary<string, double> routeMetres)
        {
            if (routeMetres != null && place != null && routeMetres.TryGetValue(place.PlaceId, out var metres))
            {
                return metres;
            }

            return double.MaxValue;
        }
    }
}
