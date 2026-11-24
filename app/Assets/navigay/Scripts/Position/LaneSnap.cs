using System;
using Navigay.Config;
using Navigay.Data;
using Navigay.Routing;

namespace Navigay.Position
{
    public sealed class LaneSnap
    {
        string _edgeId;
        string _challenger;
        double _challengerSince;

        public SnapFix Update(
            double x,
            double y,
            float headingDeg,
            float headingErrorDeg,
            float positionErrorM,
            GraphData graph,
            Route route,
            NavConfig config,
            double time)
        {
            var radius = Math.Max(config.snap_search_m, 2d * positionErrorM);
            var best = default(Candidate);
            var locked = default(Candidate);
            var found = false;
            var lockedFound = false;
            if (graph?.Edges != null)
            {
                foreach (var edge in graph.Edges)
                {
                    if (edge == null || edge.Kind != "lane" || !LaneMath.TryProject(x, y, edge.Polyline, out var hit))
                    {
                        continue;
                    }

                    if (hit.Distance > radius || LaneMath.Blocked(x, y, hit.X, hit.Y, graph.Blocks))
                    {
                        continue;
                    }

                    var candidate = new Candidate(edge, hit, Score(edge, hit.Distance, headingDeg, headingErrorDeg, route, config));
                    if (!found || Prefer(candidate, best))
                    {
                        best = candidate;
                        found = true;
                    }

                    if (edge.Id == _edgeId)
                    {
                        locked = candidate;
                        lockedFound = true;
                    }
                }
            }

            if (!found)
            {
                Clear();
                return SnapFix.None;
            }

            if (_edgeId == null || !lockedFound)
            {
                Adopt(best, time);
                return Fix(best);
            }

            if (best.Edge.Id == _edgeId)
            {
                _challenger = null;
                return Fix(locked);
            }

            var better = locked.Score - best.Score;
            if (better < config.snap_stick_better_m)
            {
                _challenger = null;
                return Fix(locked);
            }

            if (_challenger != best.Edge.Id)
            {
                _challenger = best.Edge.Id;
                _challengerSince = time;
                return Fix(locked);
            }

            if (time - _challengerSince < config.snap_stick_s)
            {
                return Fix(locked);
            }

            Adopt(best, time);
            return Fix(best);
        }

        public void Clear()
        {
            _edgeId = null;
            _challenger = null;
        }

        void Adopt(Candidate candidate, double time)
        {
            _edgeId = candidate.Edge.Id;
            _challenger = null;
            _challengerSince = time;
        }

        static SnapFix Fix(Candidate candidate)
        {
            return new SnapFix(
                true,
                candidate.Edge.Id,
                candidate.Edge.LaneId,
                candidate.Hit.X,
                candidate.Hit.Y,
                candidate.Hit.Distance,
                candidate.Hit.Segment,
                candidate.Hit.T);
        }

        static float Score(
            GraphEdge edge,
            double distance,
            float headingDeg,
            float headingErrorDeg,
            Route route,
            NavConfig config)
        {
            var score = (float)distance;
            if (!OnRoute(edge, route))
            {
                score += config.snap_off_route_m;
            }

            if (headingErrorDeg < 180f && Disagrees(edge, headingDeg, config.snap_heading_disagree_deg))
            {
                score += config.snap_heading_penalty_m;
            }

            return score;
        }

        static bool OnRoute(GraphEdge edge, Route route)
        {
            if (route?.LaneIds == null || string.IsNullOrEmpty(edge.LaneId))
            {
                return false;
            }

            foreach (var lane in route.LaneIds)
            {
                if (lane == edge.LaneId)
                {
                    return true;
                }
            }

            return false;
        }

        static bool Disagrees(GraphEdge edge, float headingDeg, float limit)
        {
            var line = edge.Polyline;
            if (line == null || line.Length < 2)
            {
                return false;
            }

            var dx = line[line.Length - 1][0] - line[0][0];
            var dy = line[line.Length - 1][1] - line[0][1];
            var length = Math.Sqrt((dx * dx) + (dy * dy));
            if (length < 1e-6)
            {
                return false;
            }

            var rad = headingDeg * Math.PI / 180d;
            var dot = Math.Abs(((dx / length) * Math.Sin(rad)) + ((dy / length) * Math.Cos(rad)));
            if (dot > 1d)
            {
                dot = 1d;
            }

            var degrees = Math.Acos(dot) * (180d / Math.PI);
            return degrees > limit;
        }

        static bool Prefer(Candidate candidate, Candidate incumbent)
        {
            if (Math.Abs(candidate.Score - incumbent.Score) > 0.001f)
            {
                return candidate.Score < incumbent.Score;
            }

            return string.CompareOrdinal(candidate.Edge.Id, incumbent.Edge.Id) < 0;
        }

        readonly struct Candidate
        {
            public readonly GraphEdge Edge;
            public readonly LaneMath.Hit Hit;
            public readonly float Score;

            public Candidate(GraphEdge edge, LaneMath.Hit hit, float score)
            {
                Edge = edge;
                Hit = hit;
                Score = score;
            }
        }
    }
}
