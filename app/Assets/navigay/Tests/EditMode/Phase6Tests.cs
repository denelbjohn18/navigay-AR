using System;
using Navigay.Config;
using Navigay.Data;
using Navigay.Guidance;
using Navigay.Position;
using Navigay.Routing;
using Navigay.UI;
using NUnit.Framework;
using UnityEngine;

namespace Navigay.Tests.EditMode
{
    public sealed class Phase6Tests
    {
        NavConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = NavConfig.CreateDefault();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void CountdownNeverRisesOnAGpsJump()
        {
            var countdown = new Countdown();
            Assert.AreEqual(40, countdown.Update(40, false, _config));
            Assert.AreEqual(40, countdown.Update(80, false, _config));
            Assert.AreEqual(50, countdown.Update(52, true, _config));
        }

        [Test]
        public void CheckpointIsReachedAtEightMetresOrOncePassed()
        {
            var guide = new Guide();
            var route = Path();
            guide.Open(route, _config);
            var first = Sample(guide, Snap(0, 80), 0);
            Assert.AreEqual(0, first.CheckpointIndex);
            Assert.IsFalse(first.Arrived);
            var passed = Sample(guide, Snap(0, 101), 1);
            Assert.AreEqual(1, passed.CheckpointIndex);
            var near = Sample(guide, Snap(0, 193), 2);
            Assert.IsTrue(near.Arrived);
            Assert.AreEqual(TurnKind.Arrive, near.Turn);
        }

        [Test]
        public void OpeningTheGuideDoesNotCalculateAndANewRouteWaitsUntilTheUserHasLeft()
        {
            var guide = new Guide();
            guide.Open(Path(), _config);
            var calcs = 0;
            RouteResult Recalc(GraphData graph, string placeId, SnapFix snap)
            {
                calcs += 1;
                return RouteResult.Ready(Path());
            }

            Sample(guide, Snap(0, 10), 0, Recalc);
            Assert.AreEqual(0, guide.Calculations);
            Assert.AreEqual(0, calcs);
            Sample(guide, Snap(5, 10), 1, Recalc);
            Assert.AreEqual(0, calcs);
            var far = new Guide();
            far.Open(Path(), _config);
            far.Update(null, "place", Snap(0, 10), FineAt(0, 10), false, true, ConfidenceLevel.Full, _config, 0, Recalc);
            far.Update(null, "place", SnapFix.None, FineAt(40, 10), false, true, ConfidenceLevel.Full, _config, 1, Recalc);
            Assert.AreEqual(0, far.Calculations);
            far.Update(null, "place", Snap(40, 10), FineAt(40, 10), false, true, ConfidenceLevel.Full, _config, 2, Recalc);
            Assert.AreEqual(1, far.Calculations);
            far.Update(null, "place", Snap(40, 10), FineAt(40, 10), false, true, ConfidenceLevel.Full, _config, 3, Recalc);
            Assert.AreEqual(1, far.Calculations);
        }

        [Test]
        public void AFailedRerouteWaitsUntilTheUserMovesAgain()
        {
            var guide = new Guide();
            guide.Open(Path(), _config);
            RouteResult Missing(GraphData graph, string placeId, SnapFix snap)
            {
                return RouteResult.NoPath();
            }

            guide.Update(null, "place", Snap(0, 10), FineAt(0, 10), false, true, ConfidenceLevel.Full, _config, 0, Missing);
            guide.Update(null, "place", Snap(40, 10), FineAt(40, 10), false, true, ConfidenceLevel.Full, _config, 1, Missing);
            Assert.AreEqual(1, guide.Calculations);
            guide.Update(null, "place", Snap(40, 10), FineAt(40, 10), false, true, ConfidenceLevel.Full, _config, 2, Missing);
            Assert.AreEqual(1, guide.Calculations);
            guide.Update(null, "place", Snap(40, 30), FineAt(40, 30), false, true, ConfidenceLevel.Full, _config, 3, Missing);
            Assert.AreEqual(2, guide.Calculations);
        }

        [Test]
        public void PlaceCardMinutesMatchTheOpeningTripBar()
        {
            var route = Path();
            var minutes = Pace.Minutes(route.LengthM, _config.walk_speed_mps);
            var guide = new Guide();
            guide.Open(route, _config);
            var opening = Sample(guide, Snap(0, 0), 0);
            Assert.AreEqual(minutes, opening.MinutesLeft);
            Assert.AreEqual(3, minutes);
            Assert.AreEqual(PlaceCard.TimeUntilStuck, "Step outside to see the time.");
        }

        [Test]
        public void ArrivalMinutesAreTheTimeTaken()
        {
            var guide = new Guide();
            guide.Open(Path(), _config);
            Sample(guide, Snap(0, 0), 0);
            var arrived = Sample(guide, Snap(0, 193), 120);
            Assert.IsTrue(arrived.Arrived);
            Assert.AreEqual(2, guide.ElapsedMinutes(120));
        }

        [Test]
        public void EdgeArrowShowsWhenTheCheckpointIsOutsideTheView()
        {
            var route = new Route
            {
                Polyline = new[] { new[] { 0d, 0d }, new[] { 100d, 0d } },
                Checkpoints = new[]
                {
                    new Checkpoint { Name = "east", X = 100d, Y = 0d, AlongM = 100d },
                },
            };
            var state = new GuidanceState(
                route,
                0,
                0d,
                100d,
                100,
                3,
                TurnKind.Right,
                "east",
                null,
                false,
                false,
                ConfidenceLevel.Full);
            Assert.IsTrue(GuideScreen.CheckpointOffScreen(0f, state));
            Assert.IsFalse(GuideScreen.CheckpointOffScreen(90f, state));
        }

        [Test]
        public void UnderpassArrivalKeepsTheEntranceLine()
        {
            Assert.AreEqual("This is the entrance. navigay stops here.", ArrivalScreen.UnderpassLine);
        }

        GuidanceState Sample(
            Guide guide,
            SnapFix snap,
            double time,
            Func<GraphData, string, SnapFix, RouteResult> calculate = null)
        {
            return guide.Update(null, "place", snap, Fine(), false, true, ConfidenceLevel.Full, _config, time, calculate);
        }

        static PositionEstimate Fine()
        {
            return FineAt(0, 0);
        }

        static PositionEstimate FineAt(float x, float y)
        {
            return new PositionEstimate(new Vector2(x, y), 0, 4, 5, PositionSource.Gps, TrackingState.Tracking, 0);
        }

        static SnapFix Snap(double x, double y)
        {
            return new SnapFix(true, "edge", "lane", x, y, 0, 0, 0);
        }

        static Route Path()
        {
            return new Route
            {
                PlaceId = "place",
                DoorId = "door",
                LengthM = 200,
                Polyline = new[]
                {
                    new[] { 0d, 0d },
                    new[] { 0d, 100d },
                    new[] { 0d, 200d },
                },
                Checkpoints = new[]
                {
                    new Checkpoint { NodeId = "turn", Name = "B2", X = 0, Y = 100, AlongM = 100 },
                    new Checkpoint { NodeId = "door", Name = "Quess Mess", X = 0, Y = 200, AlongM = 200 },
                },
            };
        }
    }
}
