using System;
using System.Collections.Generic;
using Navigay.Data;
using Navigay.Position;

namespace Navigay.Routing
{
    public static class RouteStart
    {
        public const string NodeId = "snap_start";
        const double OnNodeM = 0.75d;

        public static GraphData WithStart(GraphData source, SnapFix snap, out string startId)
        {
            var edge = FindEdge(source, snap.EdgeId);
            if (edge == null)
            {
                throw new ArgumentException("snap edge is missing.", nameof(snap));
            }

            var from = FindNode(source, edge.From);
            var to = FindNode(source, edge.To);
            if (from != null && Near(from, snap))
            {
                startId = from.Id;
                return source;
            }

            if (to != null && Near(to, snap))
            {
                startId = to.Id;
                return source;
            }

            var line = edge.Polyline;
            if (line == null || line.Length < 2 || snap.Segment < 0 || snap.Segment >= line.Length - 1)
            {
                throw new ArgumentException("snap is not on the lane.", nameof(snap));
            }

            var left = new List<double[]>();
            for (var i = 0; i <= snap.Segment; i++)
            {
                left.Add(Copy(line[i]));
            }

            var point = new[] { snap.X, snap.Y };
            if (!Same(left[left.Count - 1], point))
            {
                left.Add(point);
            }

            var right = new List<double[]> { point };
            for (var i = snap.Segment + 1; i < line.Length; i++)
            {
                if (!Same(right[right.Count - 1], line[i]))
                {
                    right.Add(Copy(line[i]));
                }
            }

            var leftLength = Length(left);
            var rightLength = Length(right);
            var total = leftLength + rightLength;
            var leftMetres = total < 1e-6 ? edge.LengthM * 0.5d : edge.LengthM * (leftLength / total);
            var rightMetres = edge.LengthM - leftMetres;
            startId = NodeId;
            var leftEdge = Piece(edge, edge.Id + "_a", edge.From, startId, leftMetres, left);
            var rightEdge = Piece(edge, edge.Id + "_b", startId, edge.To, rightMetres, right);
            return CopyGraph(source, edge, from, to, leftEdge, rightEdge, snap);
        }

        static GraphData CopyGraph(
            GraphData source,
            GraphEdge replaced,
            GraphNode from,
            GraphNode to,
            GraphEdge left,
            GraphEdge right,
            SnapFix snap)
        {
            var nodes = new List<GraphNode>();
            if (source.Nodes != null)
            {
                foreach (var node in source.Nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    if (from != null && node.Id == from.Id)
                    {
                        nodes.Add(CopyNode(node, replaced.Id, left.Id));
                    }
                    else if (to != null && node.Id == to.Id)
                    {
                        nodes.Add(CopyNode(node, replaced.Id, right.Id));
                    }
                    else
                    {
                        nodes.Add(node);
                    }
                }
            }

            nodes.Add(new GraphNode
            {
                Id = NodeId,
                Kind = "auto",
                X = snap.X,
                Y = snap.Y,
                EdgeIds = new[] { left.Id, right.Id },
            });

            var edges = new List<GraphEdge>();
            if (source.Edges != null)
            {
                foreach (var edge in source.Edges)
                {
                    if (edge != null && edge.Id != replaced.Id)
                    {
                        edges.Add(edge);
                    }
                }
            }

            edges.Add(left);
            edges.Add(right);
            return new GraphData
            {
                Schema = source.Schema,
                Origin = source.Origin,
                MetresPerDegree = source.MetresPerDegree,
                Nodes = nodes.ToArray(),
                Edges = edges.ToArray(),
                Places = source.Places,
                Blocks = source.Blocks,
                PhotoMaps = source.PhotoMaps,
            };
        }

        static GraphNode CopyNode(GraphNode node, string removeEdge, string addEdge)
        {
            var ids = new List<string>();
            if (node.EdgeIds != null)
            {
                foreach (var id in node.EdgeIds)
                {
                    if (id != removeEdge)
                    {
                        ids.Add(id);
                    }
                }
            }

            ids.Add(addEdge);
            return new GraphNode
            {
                Id = node.Id,
                Kind = node.Kind,
                X = node.X,
                Y = node.Y,
                Lat = node.Lat,
                Lon = node.Lon,
                PlaceId = node.PlaceId,
                Landmark = node.Landmark,
                EdgeIds = ids.ToArray(),
            };
        }

        static GraphEdge Piece(GraphEdge source, string id, string from, string to, double length, List<double[]> line)
        {
            return new GraphEdge
            {
                Id = id,
                Kind = source.Kind,
                From = from,
                To = to,
                LaneId = source.LaneId,
                Access = source.Access,
                LengthM = length,
                Polyline = line.ToArray(),
            };
        }

        static GraphEdge FindEdge(GraphData graph, string id)
        {
            if (graph?.Edges == null)
            {
                return null;
            }

            foreach (var edge in graph.Edges)
            {
                if (edge != null && edge.Id == id)
                {
                    return edge;
                }
            }

            return null;
        }

        static GraphNode FindNode(GraphData graph, string id)
        {
            if (graph?.Nodes == null)
            {
                return null;
            }

            foreach (var node in graph.Nodes)
            {
                if (node != null && node.Id == id)
                {
                    return node;
                }
            }

            return null;
        }

        static bool Near(GraphNode node, SnapFix snap)
        {
            var dx = node.X - snap.X;
            var dy = node.Y - snap.Y;
            return (dx * dx) + (dy * dy) <= OnNodeM * OnNodeM;
        }

        static bool Same(double[] a, double[] b)
        {
            var dx = a[0] - b[0];
            var dy = a[1] - b[1];
            return (dx * dx) + (dy * dy) < 1e-6;
        }

        static double[] Copy(double[] point)
        {
            return new[] { point[0], point[1] };
        }

        static double Length(List<double[]> line)
        {
            var length = 0d;
            for (var i = 1; i < line.Count; i++)
            {
                var dx = line[i][0] - line[i - 1][0];
                var dy = line[i][1] - line[i - 1][1];
                length += Math.Sqrt((dx * dx) + (dy * dy));
            }

            return length;
        }
    }
}
