using System;
using System.Collections.Generic;
using Navigay.Config;
using Navigay.Data;

namespace Navigay.Routing
{
    public static class Router
    {
        const string GoalId = "\u0000virtual-end";

        // Go calls this once. Opening the guide does not.
        public static RouteResult Calculate(GraphData graph, string startNodeId, string placeId, NavConfig config)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(startNodeId))
            {
                throw new ArgumentException("start node is missing.", nameof(startNodeId));
            }

            if (string.IsNullOrEmpty(placeId))
            {
                throw new ArgumentException("place is missing.", nameof(placeId));
            }

            var nodes = IndexNodes(graph.Nodes);
            if (!nodes.ContainsKey(startNodeId))
            {
                throw new ArgumentException("start node is missing.", nameof(startNodeId));
            }

            var place = FindPlace(graph.Places, placeId);
            if (place == null)
            {
                throw new ArgumentException("place is missing.", nameof(placeId));
            }

            var edges = IndexEdges(graph.Edges);
            var links = Links(nodes, edges);
            var doors = Doors(place, nodes);
            if (doors.Count == 0)
            {
                return RouteResult.NoPath();
            }

            var doorIds = new HashSet<string>();
            foreach (var door in doors)
            {
                doorIds.Add(door.Id);
            }

            if (!TrySearch(startNodeId, nodes, links, doors, doorIds, out var nodeIds, out var edgeIds))
            {
                return RouteResult.NoPath();
            }

            return RouteResult.Ready(Build(graph, place, nodes, edges, nodeIds, edgeIds, config.checkpoint_turn_deg));
        }

        static bool TrySearch(
            string startNodeId,
            Dictionary<string, GraphNode> nodes,
            Dictionary<string, List<Link>> links,
            List<GraphNode> doors,
            HashSet<string> doorIds,
            out string[] nodeIds,
            out string[] edgeIds)
        {
            var best = new Dictionary<string, double> { [startNodeId] = 0d };
            var previous = new Dictionary<string, string>();
            var via = new Dictionary<string, string>();
            var closed = new HashSet<string>();
            var open = new List<Candidate> { new Candidate(Heuristic(nodes[startNodeId], doors), 0d, startNodeId) };

            while (open.Count > 0)
            {
                var index = BestOpen(open);
                var current = open[index];
                open.RemoveAt(index);
                if (current.G > best[current.Id] + 1e-6)
                {
                    continue;
                }

                if (!closed.Add(current.Id))
                {
                    continue;
                }

                if (current.Id == GoalId)
                {
                    Collect(startNodeId, previous, via, out nodeIds, out edgeIds);
                    return true;
                }

                if (links.TryGetValue(current.Id, out var roads))
                {
                    foreach (var link in roads)
                    {
                        Relax(current, link.To, link.EdgeId, link.Length, nodes, doors, best, previous, via, closed, open);
                    }
                }

                if (doorIds.Contains(current.Id))
                {
                    Relax(current, GoalId, null, 0d, nodes, doors, best, previous, via, closed, open);
                }
            }

            nodeIds = null;
            edgeIds = null;
            return false;
        }

        static void Relax(
            Candidate current,
            string nextId,
            string edgeId,
            double length,
            Dictionary<string, GraphNode> nodes,
            List<GraphNode> doors,
            Dictionary<string, double> best,
            Dictionary<string, string> previous,
            Dictionary<string, string> via,
            HashSet<string> closed,
            List<Candidate> open)
        {
            var nextG = current.G + length;
            if (best.TryGetValue(nextId, out var known) && nextG >= known - 1e-6)
            {
                return;
            }

            best[nextId] = nextG;
            previous[nextId] = current.Id;
            via[nextId] = edgeId;
            closed.Remove(nextId);
            var heuristic = nextId == GoalId ? 0d : Heuristic(nodes[nextId], doors);
            open.Add(new Candidate(nextG + heuristic, nextG, nextId));
        }

        static void Collect(
            string startNodeId,
            Dictionary<string, string> previous,
            Dictionary<string, string> via,
            out string[] nodeIds,
            out string[] edgeIds)
        {
            var nodes = new List<string>();
            var edges = new List<string>();
            var cursor = GoalId;
            while (previous.TryGetValue(cursor, out var prior))
            {
                if (cursor != GoalId)
                {
                    nodes.Add(cursor);
                    edges.Add(via[cursor]);
                }

                cursor = prior;
            }

            nodes.Add(startNodeId);
            nodes.Reverse();
            edges.Reverse();
            nodeIds = nodes.ToArray();
            edgeIds = edges.ToArray();
        }

        static Route Build(
            GraphData graph,
            PlaceRecord place,
            Dictionary<string, GraphNode> nodes,
            Dictionary<string, GraphEdge> edges,
            string[] nodeIds,
            string[] edgeIds,
            float turnDeg)
        {
            var lines = new List<double[][]>();
            var length = 0d;
            for (var i = 0; i < edgeIds.Length; i++)
            {
                var edge = edges[edgeIds[i]];
                length += edge.LengthM;
                lines.Add(Oriented(edge, nodeIds[i], nodes[nodeIds[i]], nodes[nodeIds[i + 1]]));
            }

            var points = new List<double[]>();
            if (lines.Count == 0)
            {
                var start = nodes[nodeIds[0]];
                points.Add(new[] { start.X, start.Y });
            }
            else
            {
                foreach (var line in lines)
                {
                    Append(points, line);
                }
            }

            var checkpoints = new List<Checkpoint>();
            var along = 0d;
            for (var i = 0; i < nodeIds.Length; i++)
            {
                if (i > 0)
                {
                    along += edges[edgeIds[i - 1]].LengthM;
                }

                var node = nodes[nodeIds[i]];
                var isDoor = i == nodeIds.Length - 1;
                var turned = !isDoor && i > 0 && i < lines.Count && Turn(lines[i - 1], lines[i]) > turnDeg;
                if (!isDoor && !turned)
                {
                    continue;
                }

                checkpoints.Add(new Checkpoint
                {
                    NodeId = node.Id,
                    Name = Name(node, graph.Places, nodes),
                    X = node.X,
                    Y = node.Y,
                    AlongM = along,
                });
            }

            return new Route
            {
                PlaceId = place.PlaceId,
                DoorId = nodeIds[nodeIds.Length - 1],
                LengthM = length,
                NodeIds = nodeIds,
                EdgeIds = edgeIds,
                LaneIds = LaneIdsOf(edges, edgeIds),
                Polyline = points.ToArray(),
                Checkpoints = checkpoints.ToArray(),
            };
        }

        static string[] LaneIdsOf(Dictionary<string, GraphEdge> edges, string[] edgeIds)
        {
            var lanes = new List<string>();
            foreach (var edgeId in edgeIds)
            {
                var lane = edges[edgeId].LaneId;
                if (string.IsNullOrEmpty(lane))
                {
                    continue;
                }

                if (lanes.Count == 0 || lanes[lanes.Count - 1] != lane)
                {
                    lanes.Add(lane);
                }
            }

            return lanes.ToArray();
        }

        static string Name(GraphNode node, PlaceRecord[] places, Dictionary<string, GraphNode> nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.Landmark))
            {
                return node.Landmark;
            }

            var best = double.MaxValue;
            string name = null;
            if (places == null)
            {
                return string.Empty;
            }

            foreach (var place in places)
            {
                if (place?.DoorIds == null)
                {
                    continue;
                }

                foreach (var doorId in place.DoorIds)
                {
                    if (string.IsNullOrEmpty(doorId) || !nodes.TryGetValue(doorId, out var door))
                    {
                        continue;
                    }

                    var dx = node.X - door.X;
                    var dy = node.Y - door.Y;
                    var distance = (dx * dx) + (dy * dy);
                    if (distance < best)
                    {
                        best = distance;
                        name = place.Name;
                    }
                }
            }

            return name ?? string.Empty;
        }

        static double Turn(double[][] incoming, double[][] outgoing)
        {
            var arrive = Arrival(incoming);
            var leave = Departure(outgoing);
            var arriveLength = Math.Sqrt((arrive.X * arrive.X) + (arrive.Y * arrive.Y));
            var leaveLength = Math.Sqrt((leave.X * leave.X) + (leave.Y * leave.Y));
            if (arriveLength < 1e-6 || leaveLength < 1e-6)
            {
                return 0d;
            }

            var dot = ((arrive.X * leave.X) + (arrive.Y * leave.Y)) / (arriveLength * leaveLength);
            if (dot > 1d)
            {
                dot = 1d;
            }
            else if (dot < -1d)
            {
                dot = -1d;
            }

            return Math.Acos(dot) * (180d / Math.PI);
        }

        static (double X, double Y) Departure(double[][] line)
        {
            for (var i = 1; i < line.Length; i++)
            {
                var dx = line[i][0] - line[0][0];
                var dy = line[i][1] - line[0][1];
                if ((dx * dx) + (dy * dy) > 1e-8)
                {
                    return (dx, dy);
                }
            }

            return (0d, 0d);
        }

        static (double X, double Y) Arrival(double[][] line)
        {
            var last = line.Length - 1;
            for (var i = last - 1; i >= 0; i--)
            {
                var dx = line[last][0] - line[i][0];
                var dy = line[last][1] - line[i][1];
                if ((dx * dx) + (dy * dy) > 1e-8)
                {
                    return (dx, dy);
                }
            }

            return (0d, 0d);
        }

        static double[][] Oriented(GraphEdge edge, string fromId, GraphNode from, GraphNode to)
        {
            var line = edge.Polyline;
            if (line == null || line.Length == 0)
            {
                return new[] { new[] { from.X, from.Y }, new[] { to.X, to.Y } };
            }

            if (edge.From == fromId)
            {
                return line;
            }

            var reversed = new double[line.Length][];
            for (var i = 0; i < line.Length; i++)
            {
                reversed[i] = line[line.Length - 1 - i];
            }

            return reversed;
        }

        static void Append(List<double[]> points, double[][] line)
        {
            var start = 0;
            if (points.Count > 0 && line.Length > 0 && Same(points[points.Count - 1], line[0]))
            {
                start = 1;
            }

            for (var i = start; i < line.Length; i++)
            {
                if (line[i] != null && line[i].Length >= 2)
                {
                    points.Add(line[i]);
                }
            }
        }

        static bool Same(double[] a, double[] b)
        {
            var dx = a[0] - b[0];
            var dy = a[1] - b[1];
            return (dx * dx) + (dy * dy) < 1e-6;
        }

        static double Heuristic(GraphNode node, List<GraphNode> doors)
        {
            var best = double.MaxValue;
            foreach (var door in doors)
            {
                var dx = node.X - door.X;
                var dy = node.Y - door.Y;
                var distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        static int BestOpen(List<Candidate> open)
        {
            var best = 0;
            for (var i = 1; i < open.Count; i++)
            {
                if (Prefer(open[i], open[best]))
                {
                    best = i;
                }
            }

            return best;
        }

        static bool Prefer(Candidate candidate, Candidate incumbent)
        {
            if (candidate.F != incumbent.F)
            {
                return candidate.F < incumbent.F;
            }

            if (candidate.G != incumbent.G)
            {
                return candidate.G < incumbent.G;
            }

            return string.CompareOrdinal(candidate.Id, incumbent.Id) < 0;
        }

        static List<GraphNode> Doors(PlaceRecord place, Dictionary<string, GraphNode> nodes)
        {
            var doors = new List<GraphNode>();
            if (place.DoorIds == null)
            {
                return doors;
            }

            foreach (var doorId in place.DoorIds)
            {
                if (!string.IsNullOrEmpty(doorId) && nodes.TryGetValue(doorId, out var door))
                {
                    doors.Add(door);
                }
            }

            return doors;
        }

        static Dictionary<string, List<Link>> Links(Dictionary<string, GraphNode> nodes, Dictionary<string, GraphEdge> edges)
        {
            var links = new Dictionary<string, List<Link>>();
            foreach (var node in nodes.Values)
            {
                var list = new List<Link>();
                if (node.EdgeIds != null)
                {
                    foreach (var edgeId in node.EdgeIds)
                    {
                        if (string.IsNullOrEmpty(edgeId) || !edges.TryGetValue(edgeId, out var edge))
                        {
                            continue;
                        }

                        var other = edge.From == node.Id ? edge.To : edge.From;
                        if (string.IsNullOrEmpty(other) || other == node.Id || !nodes.ContainsKey(other))
                        {
                            continue;
                        }

                        list.Add(new Link(other, edge.Id, edge.LengthM));
                    }
                }

                links[node.Id] = list;
            }

            return links;
        }

        static Dictionary<string, GraphNode> IndexNodes(GraphNode[] source)
        {
            var nodes = new Dictionary<string, GraphNode>();
            if (source == null)
            {
                return nodes;
            }

            foreach (var node in source)
            {
                if (node != null && !string.IsNullOrEmpty(node.Id))
                {
                    nodes[node.Id] = node;
                }
            }

            return nodes;
        }

        static Dictionary<string, GraphEdge> IndexEdges(GraphEdge[] source)
        {
            var edges = new Dictionary<string, GraphEdge>();
            if (source == null)
            {
                return edges;
            }

            foreach (var edge in source)
            {
                if (edge != null && !string.IsNullOrEmpty(edge.Id))
                {
                    edges[edge.Id] = edge;
                }
            }

            return edges;
        }

        static PlaceRecord FindPlace(PlaceRecord[] places, string placeId)
        {
            if (places == null)
            {
                return null;
            }

            foreach (var place in places)
            {
                if (place != null && place.PlaceId == placeId)
                {
                    return place;
                }
            }

            return null;
        }

        readonly struct Candidate
        {
            public readonly double F;
            public readonly double G;
            public readonly string Id;

            public Candidate(double f, double g, string id)
            {
                F = f;
                G = g;
                Id = id;
            }
        }

        readonly struct Link
        {
            public readonly string To;
            public readonly string EdgeId;
            public readonly double Length;

            public Link(string to, string edgeId, double length)
            {
                To = to;
                EdgeId = edgeId;
                Length = length;
            }
        }
    }
}
