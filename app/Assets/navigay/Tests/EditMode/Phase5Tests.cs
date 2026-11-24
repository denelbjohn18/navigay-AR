using System;
using System.IO;
using Navigay.Config;
using Navigay.Data;
using Navigay.Dev;
using Navigay.Guidance;
using Navigay.Position;
using Navigay.Routing;
using Navigay.UI;
using NUnit.Framework;
using UnityEngine;

namespace Navigay.Tests.EditMode
{
    public sealed class Phase5Tests
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
        public void SnapDoesNotCrossABlock()
        {
            var graph = new GraphData
            {
                Edges = new[]
                {
                    Lane("hidden", "lane_hidden", "a", "b", 12, -30, 12, 30),
                    Lane("open", "lane_open", "c", "d", -20, -30, -20, 30),
                },
                Blocks = new[] { Block(4, -6, 10, 6) },
            };
            var snap = new LaneSnap().Update(0, 0, 0, 180, 5, graph, null, _config, 0);
            Assert.IsTrue(snap.Locked);
            Assert.AreEqual("open", snap.EdgeId);
        }

        [Test]
        public void SnapStaysUntilTheOtherLaneIsClearlyBetter()
        {
            var graph = new GraphData
            {
                Edges = new[]
                {
                    Lane("lane_a", "a", "n1", "n2", 0, -50, 0, 50),
                    Lane("lane_b", "b", "n3", "n4", 10, -50, 10, 50),
                },
            };
            var snap = new LaneSnap();
            Assert.AreEqual("lane_a", snap.Update(2, 0, 0, 180, 5, graph, null, _config, 0).EdgeId);
            Assert.AreEqual("lane_a", snap.Update(8, 0, 0, 180, 5, graph, null, _config, 10).EdgeId);
            Assert.AreEqual("lane_a", snap.Update(8, 0, 0, 180, 5, graph, null, _config, 11).EdgeId);
            Assert.AreEqual("lane_b", snap.Update(8, 0, 0, 180, 5, graph, null, _config, 12.1).EdgeId);
            Assert.AreEqual("lane_a", new LaneSnap().Update(2, 0, 0, 180, 5, graph, null, _config, 0).EdgeId);
            var sticky = new LaneSnap();
            sticky.Update(2, 0, 0, 180, 5, graph, null, _config, 0);
            Assert.AreEqual("lane_a", sticky.Update(6, 0, 0, 180, 5, graph, null, _config, 10).EdgeId);
        }

        [Test]
        public void SearchRadiusGrowsWithTheReportedError()
        {
            var graph = new GraphData
            {
                Edges = new[] { Lane("far", "far", "a", "b", 30, -10, 30, 10) },
            };
            var snap = new LaneSnap();
            Assert.IsFalse(snap.Update(0, 0, 0, 180, 5, graph, null, _config, 0).Locked);
            Assert.IsTrue(new LaneSnap().Update(0, 0, 0, 180, 20, graph, null, _config, 0).Locked);
        }

        [Test]
        public void AHoleIsOutsideTheBuilding()
        {
            var block = new BlockOutline
            {
                Rings = new[]
                {
                    new[] { new[] { -10d, -10d }, new[] { 10d, -10d }, new[] { 10d, 10d }, new[] { -10d, 10d } },
                    new[] { new[] { -2d, -2d }, new[] { 2d, -2d }, new[] { 2d, 2d }, new[] { -2d, 2d } },
                },
            };
            Assert.IsNull(LaneMath.Containing(0, 0, new[] { block }));
            Assert.AreEqual(block, LaneMath.Containing(5, 0, new[] { block }));
        }

        [Test]
        public void GoCalculatesOnceAndTheSnapPointIsTheStart()
        {
            var graph = RouteGraph();
            var gate = new ReadyGate();
            gate.Begin();
            var snap = Locked("edge", "lane_a", 0, 40, 0, 0.4);
            var first = gate.Sample(graph, "place", Sample(snap, 5), _config, 0, RouteAt);
            Assert.AreEqual(1, gate.Calculations);
            Assert.IsFalse(first.Guiding);
            Assert.AreEqual(ReadyWord.FindingYou, first.Word);
            Assert.AreEqual(60d, first.Route.LengthM, 0.5);
            Assert.AreEqual(40d, first.Route.Polyline[0][1], 0.2);
            gate.Sample(graph, "place", Sample(snap, 5), _config, 1, RouteAt);
            Assert.AreEqual(1, gate.Calculations);
            var held = gate.Sample(graph, "place", Sample(snap, 5), _config, 2, RouteAt);
            Assert.IsTrue(held.Guiding);
            Assert.AreEqual(1, gate.Calculations);
            gate.Sample(graph, "place", Sample(snap, 5), _config, 3, RouteAt);
            Assert.AreEqual(1, gate.Calculations);
        }

        [Test]
        public void NoPathIsShownAtOnceAndDoesNotOpenTheGuide()
        {
            var graph = RouteGraph();
            var gate = new ReadyGate();
            gate.Begin();
            var snap = Locked("other", "lane_b", 40, 10, 0, 0.2);
            var now = gate.Sample(graph, "place", Sample(snap, 5), _config, 0, RouteAt);
            Assert.AreEqual(ReadyWord.NoPath, now.Word);
            Assert.IsFalse(now.Guiding);
            Assert.AreEqual("Can't find a way there.", ReadyScreen.NoPath);
            var later = gate.Sample(graph, "place", Sample(snap, 5), _config, 2, RouteAt);
            Assert.IsFalse(later.Guiding);
            Assert.AreEqual(ReadyWord.NoPath, later.Word);
        }

        [Test]
        public void InsideBlockNamesTheBuilding()
        {
            var gate = new ReadyGate();
            gate.Begin();
            var named = gate.Sample(null, "place", new FixSample(true, 5, true, "B3", SnapFix.None), _config, 0, RouteAt);
            Assert.AreEqual(ReadyWord.InsideNamed, named.Word);
            Assert.AreEqual("You're inside B3. Step outside to start.", ReadyScreen.InsideNamed(named.BlockName));
            Assert.AreEqual(0, gate.Calculations);
            var unnamed = gate.Sample(null, "place", new FixSample(true, 5, true, null, SnapFix.None), _config, 1, RouteAt);
            Assert.AreEqual(ReadyWord.InsideBuilding, unnamed.Word);
            Assert.AreEqual("You're inside a building. Step outside to start.", ReadyScreen.InsideBuilding);
        }

        [Test]
        public void DarkCameraStillLeavesReady()
        {
            var gate = new ReadyGate();
            gate.Begin();
            var snap = Locked("edge", "lane_a", 0, 40, 0, 0.4);
            var left = gate.Sample(RouteGraph(), "place", Sample(snap, 5), _config, 0, RouteAt);
            Assert.IsFalse(left.Guiding);
            left = gate.Sample(RouteGraph(), "place", Sample(snap, 5), _config, 2, RouteAt);
            Assert.IsTrue(left.Guiding);
        }

        [Test]
        public void PreciseLocationAndALooseFixStayOnReady()
        {
            var gate = new ReadyGate();
            gate.Begin();
            var precise = gate.Sample(RouteGraph(), "place", Sample(SnapFix.None, 1500), _config, 0, RouteAt);
            Assert.IsTrue(precise.PreciseLocation);
            Assert.AreEqual(ReadyWord.StepIntoTheOpen, precise.Word);
            Assert.AreEqual(0, gate.Calculations);
            Assert.AreEqual(
                "iOS is reporting an accuracy worse than 1,000 m. Turn on Precise Location for navigay.",
                PermissionsScreen.PreciseSentence);
            var loose = gate.Sample(RouteGraph(), "place", Sample(SnapFix.None, 40), _config, 1, RouteAt);
            Assert.AreEqual(ReadyWord.StepIntoTheOpen, loose.Word);
            Assert.IsFalse(loose.Guiding);
        }

        [Test]
        public void StuckPointOffTheRouteIsCalculatedOnceMore()
        {
            var gate = new ReadyGate();
            gate.Begin();
            var first = Locked("edge", "lane_a", 0, 40, 0, 0.4);
            gate.Sample(RouteGraph(), "place", Sample(first, 5), _config, 0, RouteAt);
            var other = Locked("other", "lane_b", 40, 10, 0, 0.2);
            gate.Sample(RouteGraph(), "place", Sample(other, 5), _config, 0.2, RouteAt);
            var settled = gate.Sample(RouteGraph(), "place", Sample(other, 5), _config, 2.2, RouteAt);
            Assert.AreEqual(2, gate.Calculations);
            Assert.IsFalse(settled.Guiding);
            Assert.AreEqual(ReadyWord.NoPath, settled.Word);
        }

        [Test]
        public void MapMetresComeFromTheGraphAndTheCompassTurnsArMotion()
        {
            var graph = new GraphData
            {
                Origin = new MapOrigin { Lat = 0, Lon = 0 },
                MetresPerDegree = new MetresPerDegree { X = 10, Y = 20 },
            };
            var frame = MapFrame.From(graph);
            frame.ToMap(1, 1, out var x, out var y);
            Assert.AreEqual(10d, x, 0.001);
            Assert.AreEqual(20d, y, 0.001);

            var where = new WhereAmI();
            where.Bind(new GraphData
            {
                Origin = new MapOrigin { Lat = 0, Lon = 0 },
                MetresPerDegree = new MetresPerDegree { X = 1, Y = 1 },
            });
            where.PushCompass(90f, 5f);
            where.PushGps(0, 0, 5, 1);
            where.PushAr(Vector3.zero, Quaternion.identity, TrackingState.Tracking, 1);
            where.PushAr(new Vector3(0f, 0f, 8f), Quaternion.identity, TrackingState.Tracking, 2);
            Assert.AreEqual(8f, where.Current.MapPosition.x, 0.2f);
            Assert.AreEqual(0f, where.Current.MapPosition.y, 0.2f);
        }

        [Test]
        public void ConfidenceDropsImmediatelyAndRisesAfterTwoSeconds()
        {
            var confidence = new Confidence();
            var fine = Estimate(4f, 5f);
            confidence.Update(fine, true, false, _config, 0);
            Assert.AreEqual(ConfidenceLevel.Hold, confidence.Level);
            confidence.Update(fine, true, false, _config, 2);
            Assert.AreEqual(ConfidenceLevel.Full, confidence.Level);
            confidence.Update(Estimate(40f, 5f), true, false, _config, 2.1);
            Assert.AreEqual(ConfidenceLevel.Hold, confidence.Level);
            var medium = Estimate(10f, 15f);
            confidence.Update(medium, true, false, _config, 3);
            Assert.AreEqual(ConfidenceLevel.Hold, confidence.Level);
            confidence.Update(medium, true, false, _config, 5);
            Assert.AreEqual(ConfidenceLevel.Short, confidence.Level);
            confidence.Update(fine, false, false, _config, 5.1);
            Assert.AreEqual(ConfidenceLevel.Hold, confidence.Level);
        }

        [Test]
        public void WalkLogRecordsSensorsAndNoImage()
        {
            var path = Path.Combine(Application.temporaryCachePath, "navigay-walk-test.jsonl");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var recorder = new WalkRecorder(path);
            recorder.Capture(SampleAt(0));
            recorder.Capture(SampleAt(0.05));
            recorder.Capture(SampleAt(0.2));
            var lines = File.ReadAllLines(path);
            Assert.AreEqual(2, lines.Length);
            StringAssert.Contains("accuracy_m", lines[0]);
            StringAssert.Contains("heading_deg", lines[0]);
            StringAssert.Contains("tracking", lines[0]);
            StringAssert.Contains("Hold", lines[0]);
            StringAssert.Contains("\"photo_map\":null", lines[0].Replace(" ", string.Empty));
            Assert.IsFalse(lines[0].Contains("image"));
            Assert.IsFalse(lines[0].Contains("pixel"));
        }

        static WalkSample SampleAt(double time)
        {
            return new WalkSample
            {
                Time = time,
                HasGps = true,
                Latitude = 26.8,
                Longitude = 75.5,
                GpsAccuracyM = 8,
                HasCompass = true,
                HeadingDeg = 12,
                HeadingAccuracyDeg = 4,
                Tracking = "Tracking",
                Confidence = ConfidenceLevel.Hold,
            };
        }

        static PositionEstimate Estimate(float error, float headingError)
        {
            return new PositionEstimate(Vector2.zero, 0, error, headingError, PositionSource.Gps, TrackingState.Tracking, 0);
        }

        static FixSample Sample(SnapFix snap, float accuracy)
        {
            return new FixSample(true, accuracy, false, null, snap);
        }

        static SnapFix Locked(string edge, string lane, double x, double y, int segment, double t)
        {
            return new SnapFix(true, edge, lane, x, y, 1, segment, t);
        }

        static RouteResult RouteAt(GraphData graph, string placeId, SnapFix snap)
        {
            var split = RouteStart.WithStart(graph, snap, out var startId);
            return Router.Calculate(split, startId, placeId, NavConfig.CreateDefault());
        }

        static GraphData RouteGraph()
        {
            return new GraphData
            {
                Schema = 1,
                Origin = new MapOrigin(),
                MetresPerDegree = new MetresPerDegree { X = 1, Y = 1 },
                Nodes = new[]
                {
                    Node("start", 0, 0, "edge"),
                    Node("door", 0, 100, "edge", "place"),
                    Node("side", 40, 0, "other"),
                    Node("side_end", 40, 50, "other"),
                },
                Edges = new[]
                {
                    Lane("edge", "lane_a", "start", "door", 0, 0, 0, 100),
                    Lane("other", "lane_b", "side", "side_end", 40, 0, 40, 50),
                },
                Places = new[]
                {
                    new PlaceRecord
                    {
                        PlaceId = "place",
                        Name = "Underpass entrance",
                        Type = "gate",
                        Access = "all",
                        DoorIds = new[] { "door" },
                    },
                },
            };
        }

        static GraphNode Node(string id, double x, double y, string edge, string placeId = null)
        {
            return new GraphNode
            {
                Id = id,
                Kind = placeId == null ? "junction" : "door",
                X = x,
                Y = y,
                PlaceId = placeId,
                EdgeIds = new[] { edge },
            };
        }

        static GraphEdge Lane(string id, string lane, string from, string to, double x0, double y0, double x1, double y1)
        {
            var dx = x1 - x0;
            var dy = y1 - y0;
            return new GraphEdge
            {
                Id = id,
                Kind = "lane",
                From = from,
                To = to,
                LaneId = lane,
                Access = "all",
                LengthM = Math.Sqrt((dx * dx) + (dy * dy)),
                Polyline = new[] { new[] { x0, y0 }, new[] { x1, y1 } },
            };
        }

        static BlockOutline Block(double x0, double y0, double x1, double y1)
        {
            return new BlockOutline
            {
                Id = "block",
                Name = "B3",
                Rings = new[]
                {
                    new[] { new[] { x0, y0 }, new[] { x1, y0 }, new[] { x1, y1 }, new[] { x0, y1 } },
                },
            };
        }
    }
}
