using System;
using Navigay.Config;
using Navigay.Data;
using Navigay.Routing;
using NUnit.Framework;
using UnityEngine;

namespace Navigay.Tests.EditMode
{
    public sealed class RouterTests
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
        public void NearestDoorWinsForAMessWithTwoDoors()
        {
            var graph = Map(
                new[]
                {
                    Node("start", "junction", 0, 0, "walk", "out", "across", "in"),
                    Node("north", "junction", 0, 40, "out", "across"),
                    Node("side", "junction", 3, 40, "across", "in"),
                    Node("door_geom", "door", 3, 0, Edges("in"), placeId: "mess"),
                    Node("door_walk", "door", 0, 20, Edges("walk"), placeId: "mess"),
                },
                new[]
                {
                    Edge("walk", "start", "door_walk", 20, "all", 0, 0, 0, 20),
                    Edge("out", "start", "north", 40, "all", 0, 0, 0, 40),
                    Edge("across", "north", "side", 3, "all", 0, 40, 3, 40),
                    Edge("in", "side", "door_geom", 40, "all", 3, 40, 3, 0),
                },
                new[] { Place("mess", "Quess Mess", "food", "door_geom", "door_walk") });

            var route = Router.Calculate(graph, "start", "mess", _config).Route;

            Assert.AreEqual("door_walk", route.DoorId);
            Assert.AreEqual(20d, route.LengthM, 0.001);
            Assert.AreEqual("door_walk", route.NodeIds[route.NodeIds.Length - 1]);
            Assert.AreEqual(0d, route.Polyline[0][1], 0.001);
            Assert.AreEqual(20d, route.Polyline[route.Polyline.Length - 1][1], 0.001);
        }

        [Test]
        public void StaffLaneIsUsedWhenItIsShorter()
        {
            var graph = Map(
                new[]
                {
                    Node("start", "junction", 0, 0, "edge_staff", "edge_all_1"),
                    Node("north", "junction", 0, 20, "edge_all_1", "edge_all_2"),
                    Node("east", "junction", 8, 20, "edge_all_2", "edge_all_3"),
                    Node("door", "door", 8, 0, Edges("edge_staff", "edge_all_3"), placeId: "mess"),
                },
                new[]
                {
                    Edge("edge_staff", "door", "start", 8, "staff", 8, 0, 0, 0),
                    Edge("edge_all_1", "start", "north", 20, "all", 0, 0, 0, 20),
                    Edge("edge_all_2", "north", "east", 8, "all", 0, 20, 8, 20),
                    Edge("edge_all_3", "east", "door", 20, "all", 8, 20, 8, 0),
                },
                new[] { Place("mess", "Quess Mess", "food", "door") });

            var route = Router.Calculate(graph, "start", "mess", _config).Route;

            Assert.AreEqual(8d, route.LengthM, 0.001);
            CollectionAssert.AreEqual(new[] { "edge_staff" }, route.EdgeIds);
            Assert.AreEqual(0d, route.Polyline[0][0], 0.001);
            Assert.AreEqual(8d, route.Polyline[route.Polyline.Length - 1][0], 0.001);
        }

        [Test]
        public void BoysGirlsAndStaffLanesStayWalkable()
        {
            var graph = Map(
                new[]
                {
                    Node("start", "junction", 0, 0, "boys"),
                    Node("a", "junction", 5, 0, "boys", "girls"),
                    Node("b", "junction", 12, 0, "girls", "staff"),
                    Node("door", "door", 21, 0, Edges("staff"), placeId: "desk"),
                },
                new[]
                {
                    Edge("boys", "start", "a", 5, "boys", 0, 0, 5, 0),
                    Edge("girls", "a", "b", 7, "girls", 5, 0, 12, 0),
                    Edge("staff", "b", "door", 9, "staff", 12, 0, 21, 0),
                },
                new[] { Place("desk", "Desk", "office", "door") });

            var route = Router.Calculate(graph, "start", "desk", _config).Route;

            Assert.AreEqual(21d, route.LengthM, 0.001);
            CollectionAssert.AreEqual(new[] { "boys", "girls", "staff" }, route.EdgeIds);
        }

        [Test]
        public void RoutePolylineDoesNotCrossABlock()
        {
            var ring = new[]
            {
                new[] { 10d, -5d },
                new[] { 22d, -5d },
                new[] { 22d, 5d },
                new[] { 10d, 5d },
            };
            var graph = Map(
                new[]
                {
                    Node("start", "junction", 0, 0, "south"),
                    Node("south", "junction", 0, -20, "south", "east"),
                    Node("east", "junction", 32, -20, "east", "north"),
                    Node("door", "door", 32, 0, Edges("north"), placeId: "block"),
                },
                new[]
                {
                    Edge("south", "start", "south", 20, "all", 0, 0, 0, -20),
                    Edge("east", "south", "east", 32, "all", 0, -20, 32, -20),
                    Edge("north", "east", "door", 20, "all", 32, -20, 32, 0),
                },
                new[] { Place("block", "B3", "block", "door") },
                new[]
                {
                    new BlockOutline
                    {
                        Id = "block_001",
                        PlaceId = "block",
                        Name = "B3",
                        Rings = new[] { ring },
                    },
                });

            var route = Router.Calculate(graph, "start", "block", _config).Route;

            Assert.AreEqual(72d, route.LengthM, 0.001);
            Assert.IsFalse(Crosses(route.Polyline, ring));
            Assert.IsTrue(Crosses(new[] { new[] { 0d, 0d }, new[] { 32d, 0d } }, ring));
        }

        [Test]
        public void UnderpassRouteEndsAtItsDoor()
        {
            var turn = 20d * Math.PI / 180d;
            var afterX = 10d + (10d * Math.Cos(turn));
            var afterY = 10d * Math.Sin(turn);
            var left = turn + (Math.PI / 2d);
            var doorX = afterX + (10d * Math.Cos(left));
            var doorY = afterY + (10d * Math.Sin(left));
            var graph = Map(
                new[]
                {
                    Node("start", "junction", 0, 0, "e1"),
                    Node("slight", "junction", 10, 0, Edges("e1", "e2"), "Not a turn"),
                    Node("auto_0001", "auto", afterX, afterY, "e2", "e3"),
                    Node("door_001", "door", doorX, doorY, Edges("e3"), placeId: "gate"),
                },
                new[]
                {
                    Edge("e1", "start", "slight", 10, "all", 0, 0, 10, 0),
                    Edge("e2", "slight", "auto_0001", 10, "all", 10, 0, afterX, afterY),
                    Edge("e3", "auto_0001", "door_001", 10, "all", afterX, afterY, doorX, doorY),
                },
                new[] { Place("gate", "Underpass entrance", "gate", "door_001") });

            var route = Router.Calculate(graph, "start", "gate", _config).Route;
            var last = route.Checkpoints[route.Checkpoints.Length - 1];

            Assert.AreEqual("door_001", route.DoorId);
            Assert.AreEqual("door_001", route.NodeIds[route.NodeIds.Length - 1]);
            Assert.AreEqual(doorX, route.Polyline[route.Polyline.Length - 1][0], 0.001);
            Assert.AreEqual(doorY, route.Polyline[route.Polyline.Length - 1][1], 0.001);
            Assert.AreEqual("door_001", last.NodeId);
            Assert.AreEqual("Underpass entrance", last.Name);
            Assert.AreEqual(route.LengthM, last.AlongM, 0.001);
            Assert.AreEqual(2, route.Checkpoints.Length);
            Assert.AreEqual("auto_0001", route.Checkpoints[0].NodeId);
            Assert.AreEqual("Underpass entrance", route.Checkpoints[0].Name);
            Assert.AreEqual(20d, route.Checkpoints[0].AlongM, 0.001);

            _config.checkpoint_turn_deg = 100f;
            var wide = Router.Calculate(graph, "start", "gate", _config).Route;
            Assert.AreEqual(1, wide.Checkpoints.Length);
            Assert.AreEqual("door_001", wide.Checkpoints[0].NodeId);
        }

        [Test]
        public void NoPathIsTheFailureGoCalculates()
        {
            var graph = Map(
                new[]
                {
                    Node("start", "junction", 0, 0, "side"),
                    Node("other", "junction", 4, 0, "side"),
                    Node("door_001", "door", 100, 0, Edges(), placeId: "gate"),
                },
                new[] { Edge("side", "start", "other", 4, "all", 0, 0, 4, 0) },
                new[] { Place("gate", "Underpass entrance", "gate", "door_001") });

            var result = Router.Calculate(graph, "start", "gate", _config);

            Assert.IsFalse(result.Found);
            Assert.IsNull(result.Route);
            Assert.AreEqual("Can't find a way there.", result.Failure);
            Assert.AreEqual("Can't find a way there.", RouteResult.NoPathMessage);
        }

        static GraphData Map(GraphNode[] nodes, GraphEdge[] edges, PlaceRecord[] places, BlockOutline[] blocks = null)
        {
            return new GraphData
            {
                Schema = 1,
                Origin = new MapOrigin { Lat = 0, Lon = 0 },
                MetresPerDegree = new MetresPerDegree { X = 1, Y = 1 },
                Nodes = nodes,
                Edges = edges,
                Places = places,
                Blocks = blocks ?? Array.Empty<BlockOutline>(),
                PhotoMaps = Array.Empty<PhotoMapRecord>(),
            };
        }

        static string[] Edges(params string[] edgeIds)
        {
            return edgeIds;
        }

        static GraphNode Node(string id, string kind, double x, double y, params string[] edgeIds)
        {
            return Node(id, kind, x, y, edgeIds, null, null);
        }

        static GraphNode Node(string id, string kind, double x, double y, string[] edgeIds, string landmark = null, string placeId = null)
        {
            return new GraphNode
            {
                Id = id,
                Kind = kind,
                X = x,
                Y = y,
                Landmark = landmark,
                PlaceId = placeId,
                EdgeIds = edgeIds,
            };
        }

        static GraphEdge Edge(
            string id,
            string from,
            string to,
            double length,
            string access,
            double x0,
            double y0,
            double x1,
            double y1)
        {
            return new GraphEdge
            {
                Id = id,
                Kind = "lane",
                From = from,
                To = to,
                Access = access,
                LengthM = length,
                Polyline = new[] { new[] { x0, y0 }, new[] { x1, y1 } },
            };
        }

        static PlaceRecord Place(string id, string name, string type, params string[] doors)
        {
            return new PlaceRecord
            {
                PlaceId = id,
                Name = name,
                Type = type,
                Access = "all",
                DoorIds = doors,
            };
        }

        static bool Crosses(double[][] polyline, double[][] ring)
        {
            for (var i = 0; i < polyline.Length; i++)
            {
                if (Inside(polyline[i][0], polyline[i][1], ring))
                {
                    return true;
                }
            }

            for (var i = 0; i < polyline.Length - 1; i++)
            {
                for (var j = 0; j < ring.Length; j++)
                {
                    if (SegmentsCross(polyline[i], polyline[i + 1], ring[j], ring[(j + 1) % ring.Length]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool SegmentsCross(double[] a, double[] b, double[] c, double[] d)
        {
            var ab = Side(a, b, c) * Side(a, b, d);
            var cd = Side(c, d, a) * Side(c, d, b);
            return ab < 0 && cd < 0;
        }

        static double Side(double[] a, double[] b, double[] point)
        {
            return ((b[0] - a[0]) * (point[1] - a[1])) - ((b[1] - a[1]) * (point[0] - a[0]));
        }

        static bool Inside(double x, double y, double[][] ring)
        {
            var inside = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                var yi = ring[i][1];
                var yj = ring[j][1];
                if ((yi > y) == (yj > y))
                {
                    continue;
                }

                var hit = ((ring[j][0] - ring[i][0]) * (y - yi) / (yj - yi)) + ring[i][0];
                if (x < hit)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
