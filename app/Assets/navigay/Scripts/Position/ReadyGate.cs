using System;
using Navigay.Config;
using Navigay.Data;
using Navigay.Routing;

namespace Navigay.Position
{
    public sealed class ReadyGate
    {
        public delegate RouteResult CalculateRoute(GraphData graph, string placeId, SnapFix snap);

        bool _active;
        bool _calculated;
        bool _settled;
        bool _guiding;
        string _holdEdge;
        double _holdSince;
        Route _route;
        int _calculations;

        public int Calculations => _calculations;

        public Route Route => _route;

        public void Begin()
        {
            _active = true;
            _calculated = false;
            _settled = false;
            _guiding = false;
            _holdEdge = null;
            _route = null;
            _calculations = 0;
        }

        public void End()
        {
            _active = false;
            _guiding = false;
        }

        public ReadyView Sample(
            GraphData graph,
            string placeId,
            FixSample fix,
            NavConfig config,
            double time,
            CalculateRoute calculate)
        {
            if (!_active)
            {
                return new ReadyView(ReadyWord.FindingYou, null, false, false, null);
            }

            var precise = fix.HasFix && fix.AccuracyM > config.precise_error_m;
            if (!fix.HasFix)
            {
                _holdEdge = null;
                return View(ReadyWord.FindingYou, null, precise);
            }

            if (fix.Inside)
            {
                _holdEdge = null;
                var word = string.IsNullOrEmpty(fix.BlockName) ? ReadyWord.InsideBuilding : ReadyWord.InsideNamed;
                return View(word, fix.BlockName, precise);
            }

            var legal = fix.Snap.Locked && fix.AccuracyM <= config.stuck_error_m;
            if (legal && !_calculated)
            {
                Apply(calculate(graph, placeId, fix.Snap));
            }

            if (legal && !_guiding && !_settled)
            {
                Hold(fix.Snap.EdgeId, time, config, graph, placeId, fix.Snap, calculate);
            }
            else if (!legal)
            {
                _holdEdge = null;
            }

            if (_guiding)
            {
                return View(ReadyWord.FindingYou, null, precise);
            }

            if (_calculated && _route == null)
            {
                return View(ReadyWord.NoPath, null, precise);
            }

            if (!legal)
            {
                return View(ReadyWord.StepIntoTheOpen, null, precise);
            }

            return View(ReadyWord.FindingYou, null, precise);
        }

        void Hold(
            string edgeId,
            double time,
            NavConfig config,
            GraphData graph,
            string placeId,
            SnapFix snap,
            CalculateRoute calculate)
        {
            if (_holdEdge != edgeId)
            {
                _holdEdge = edgeId;
                _holdSince = time;
            }

            if (_guiding || _settled || time - _holdSince < config.stuck_hold_s)
            {
                return;
            }

            _settled = true;
            if (!OnRoute(_route, snap, config))
            {
                Apply(calculate(graph, placeId, snap));
            }

            _guiding = _route != null;
        }

        void Apply(RouteResult result)
        {
            _calculated = true;
            _calculations += 1;
            _route = result != null && result.Found ? result.Route : null;
        }

        static bool OnRoute(Route route, SnapFix snap, NavConfig config)
        {
            if (route == null)
            {
                return false;
            }

            if (route.LaneIds != null && !string.IsNullOrEmpty(snap.LaneId))
            {
                foreach (var lane in route.LaneIds)
                {
                    if (lane == snap.LaneId)
                    {
                        return true;
                    }
                }
            }

            return route.Polyline != null
                && LaneMath.DistanceTo(snap.X, snap.Y, route.Polyline) <= config.on_route_m;
        }

        ReadyView View(ReadyWord word, string blockName, bool precise)
        {
            return new ReadyView(word, blockName, precise, _guiding, _route);
        }
    }
}
