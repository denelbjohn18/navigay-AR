using System;
using Navigay.Config;
using Navigay.Data;
using Navigay.Position;
using Navigay.Routing;
using UnityEngine;

namespace Navigay.Guidance
{
    public sealed class Guide
    {
        public const string WalkAFewSteps = "Walk a few steps.";
        public const string FigureEight = "Wave the phone in a figure-eight.";
        public const string StepOutside = "Step outside.";
        public const string UnderpassLine = "This is the entrance. navigay stops here.";

        Route _route;
        int _index;
        double _along;
        double _peakAlong;
        Vector2 _lastSnap;
        bool _haveSnap;
        float _movedSinceRoute;
        float _heading;
        double _headingSince;
        Countdown _countdown = new Countdown();
        Pace _pace = new Pace();
        bool _arrived;
        bool _vibrated;

        public int Calculations { get; private set; }

        public bool Arrived => _arrived;

        public GuidanceState Current { get; private set; }

        public void Begin(Route route, NavConfig config)
        {
            _route = route;
            _index = 0;
            _along = 0d;
            _peakAlong = 0d;
            _haveSnap = false;
            _movedSinceRoute = 0f;
            _heading = float.NaN;
            _headingSince = 0d;
            _countdown.Reset();
            _pace.Reset(config);
            _arrived = false;
            _vibrated = false;
        }

        public void Open(Route route, NavConfig config)
        {
            Calculations = 0;
            Begin(route, config);
        }

        public GuidanceState Update(
            GraphData graph,
            string placeId,
            SnapFix snap,
            PositionEstimate estimate,
            bool inside,
            bool cameraFine,
            ConfidenceLevel level,
            NavConfig config,
            double time,
            Func<GraphData, string, SnapFix, RouteResult> calculate)
        {
            if (_route?.Polyline == null || _route.Checkpoints == null || _route.Checkpoints.Length == 0)
            {
                Current = default;
                return Current;
            }

            if (snap.Locked)
            {
                Track(snap, estimate, time, config);
            }
            else
            {
                var here = estimate.MapPosition;
                if (_haveSnap)
                {
                    var dx = here.x - _lastSnap.x;
                    var dy = here.y - _lastSnap.y;
                    _movedSinceRoute += Mathf.Sqrt((dx * dx) + (dy * dy));
                }

                _lastSnap = here;
                _haveSnap = true;
            }

            if (!_arrived && OffRoute(snap, estimate, config) && calculate != null)
            {
                var next = calculate(graph, placeId, snap);
                Calculations += 1;
                if (next != null && next.Found)
                {
                    Begin(next.Route, config);
                    if (snap.Locked)
                    {
                        Track(snap, estimate, time, config);
                    }
                }
            }

            Advance(config);
            var remaining = Remaining();
            var walkedBack = _peakAlong - _along > config.countdown_back_m;
            var shown = _countdown.Update(remaining, walkedBack, config);
            var speed = _pace.Update(_along, time, config);
            var minutes = Pace.Minutes(_route.LengthM - _along, speed);
            var turn = Kind();
            var extra = Extra(inside, cameraFine, estimate, level, config);
            Current = new GuidanceState(
                _route,
                _index,
                _along,
                remaining,
                shown,
                minutes,
                turn,
                Landmark(),
                extra,
                inside,
                _arrived,
                level);
            return Current;
        }

        void Track(SnapFix snap, PositionEstimate estimate, double time, NavConfig config)
        {
            var along = LaneMath.Along(_route.Polyline, snap.X, snap.Y);
            if (_haveSnap)
            {
                var dx = (float)(snap.X - _lastSnap.x);
                var dy = (float)(snap.Y - _lastSnap.y);
                _movedSinceRoute += Mathf.Sqrt((dx * dx) + (dy * dy));
            }

            _lastSnap = new Vector2((float)snap.X, (float)snap.Y);
            _haveSnap = true;
            _along = along;
            if (_along > _peakAlong)
            {
                _peakAlong = _along;
            }

            ResetAtTurn(snap, estimate, time, config);
        }

        void ResetAtTurn(SnapFix snap, PositionEstimate estimate, double time, NavConfig config)
        {
            if (_route.Checkpoints == null)
            {
                return;
            }

            if (float.IsNaN(_heading))
            {
                _heading = estimate.HeadingDeg;
                _headingSince = time;
                return;
            }

            if (Mathf.Abs(Mathf.DeltaAngle(_heading, estimate.HeadingDeg)) <= config.turn_reset_deg)
            {
                _heading = estimate.HeadingDeg;
                _headingSince = time;
                return;
            }

            if (time - _headingSince > config.turn_reset_s)
            {
                _heading = estimate.HeadingDeg;
                _headingSince = time;
                return;
            }

            foreach (var point in _route.Checkpoints)
            {
                if (point == null)
                {
                    continue;
                }

                var dx = snap.X - point.X;
                var dy = snap.Y - point.Y;
                if ((dx * dx) + (dy * dy) <= config.turn_reset_m * config.turn_reset_m)
                {
                    _along = point.AlongM;
                    _heading = estimate.HeadingDeg;
                    _headingSince = time;
                    return;
                }
            }
        }

        void Advance(NavConfig config)
        {
            while (!_arrived && _index < _route.Checkpoints.Length)
            {
                var point = _route.Checkpoints[_index];
                var left = point.AlongM - _along;
                if (left > config.checkpoint_reach_m && _along < point.AlongM)
                {
                    return;
                }

                if (_index == _route.Checkpoints.Length - 1)
                {
                    _arrived = true;
                    _vibrated = true;
                    return;
                }

                _index += 1;
                _vibrated = true;
                _countdown.Reset();
            }
        }

        bool OffRoute(SnapFix snap, PositionEstimate estimate, NavConfig config)
        {
            if (_arrived)
            {
                return false;
            }

            var x = snap.Locked ? snap.X : estimate.MapPosition.x;
            var y = snap.Locked ? snap.Y : estimate.MapPosition.y;
            var off = LaneMath.DistanceTo(x, y, _route.Polyline);
            return off > config.reroute_off_m && _movedSinceRoute >= config.reroute_moved_m;
        }

        double Remaining()
        {
            if (_route.Checkpoints == null || _index >= _route.Checkpoints.Length)
            {
                return 0d;
            }

            return Math.Max(0d, _route.Checkpoints[_index].AlongM - _along);
        }

        TurnKind Kind()
        {
            if (_arrived || _index >= _route.Checkpoints.Length - 1)
            {
                return TurnKind.Arrive;
            }

            var line = _route.Polyline;
            if (line == null || line.Length < 3)
            {
                return TurnKind.Straight;
            }

            var point = _route.Checkpoints[_index];
            var at = IndexAt(line, point.X, point.Y);
            if (at <= 0 || at >= line.Length - 1)
            {
                return TurnKind.Straight;
            }

            var ax = line[at][0] - line[at - 1][0];
            var ay = line[at][1] - line[at - 1][1];
            var bx = line[at + 1][0] - line[at][0];
            var by = line[at + 1][1] - line[at][1];
            var cross = (ax * by) - (ay * bx);
            var magA = Math.Sqrt((ax * ax) + (ay * ay));
            var magB = Math.Sqrt((bx * bx) + (by * by));
            if (magA < 1e-6 || magB < 1e-6)
            {
                return TurnKind.Straight;
            }

            var dot = ((ax * bx) + (ay * by)) / (magA * magB);
            var deg = Math.Acos(Math.Max(-1d, Math.Min(1d, dot))) * (180d / Math.PI);
            if (deg <= 30d)
            {
                return TurnKind.Straight;
            }

            return cross > 0d ? TurnKind.Left : TurnKind.Right;
        }

        string Landmark()
        {
            var point = CurrentCheckpoint();
            return point == null ? string.Empty : point.Name;
        }

        Checkpoint CurrentCheckpoint()
        {
            if (_route.Checkpoints == null || _index < 0 || _index >= _route.Checkpoints.Length)
            {
                return null;
            }

            return _route.Checkpoints[_index];
        }

        string Extra(bool inside, bool cameraFine, PositionEstimate estimate, ConfidenceLevel level, NavConfig config)
        {
            if (inside)
            {
                return StepOutside;
            }

            if (level != ConfidenceLevel.Hold)
            {
                return null;
            }

            if (!cameraFine)
            {
                return null;
            }

            if (estimate.PositionErrorM > config.short_error_m)
            {
                return WalkAFewSteps;
            }

            if (estimate.HeadingErrorDeg > config.short_heading_deg)
            {
                return FigureEight;
            }

            return null;
        }

        static int IndexAt(double[][] line, double x, double y)
        {
            var best = 0;
            var bestD = double.MaxValue;
            for (var i = 0; i < line.Length; i++)
            {
                var dx = line[i][0] - x;
                var dy = line[i][1] - y;
                var d = (dx * dx) + (dy * dy);
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            return best;
        }

        public bool ConsumeVibrate()
        {
            if (!_vibrated)
            {
                return false;
            }

            _vibrated = false;
            return true;
        }
    }
}
