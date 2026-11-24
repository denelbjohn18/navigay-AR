using System;
using Navigay.Data;

namespace Navigay.Position
{
    public static class LaneMath
    {
        public readonly struct Hit
        {
            public readonly double Distance;
            public readonly double X;
            public readonly double Y;
            public readonly int Segment;
            public readonly double T;

            public Hit(double distance, double x, double y, int segment, double t)
            {
                Distance = distance;
                X = x;
                Y = y;
                Segment = segment;
                T = t;
            }
        }

        public static bool TryProject(double x, double y, double[][] line, out Hit hit)
        {
            hit = default;
            if (line == null || line.Length < 2)
            {
                return false;
            }

            var best = double.MaxValue;
            var found = false;
            for (var i = 0; i < line.Length - 1; i++)
            {
                var ax = line[i][0];
                var ay = line[i][1];
                var bx = line[i + 1][0];
                var by = line[i + 1][1];
                var dx = bx - ax;
                var dy = by - ay;
                var length = (dx * dx) + (dy * dy);
                var t = length < 1e-8 ? 0d : (((x - ax) * dx) + ((y - ay) * dy)) / length;
                if (t < 0d)
                {
                    t = 0d;
                }
                else if (t > 1d)
                {
                    t = 1d;
                }

                var px = ax + (dx * t);
                var py = ay + (dy * t);
                var distance = Math.Sqrt(((x - px) * (x - px)) + ((y - py) * (y - py)));
                if (distance < best)
                {
                    best = distance;
                    found = true;
                    hit = new Hit(distance, px, py, i, t);
                }
            }

            return found;
        }

        public static double DistanceTo(double x, double y, double[][] line)
        {
            return TryProject(x, y, line, out var hit) ? hit.Distance : double.MaxValue;
        }

        public static double Along(double[][] line, double x, double y)
        {
            if (!TryProject(x, y, line, out var hit))
            {
                return 0d;
            }

            var along = 0d;
            for (var i = 0; i < hit.Segment; i++)
            {
                along += Span(line[i][0], line[i][1], line[i + 1][0], line[i + 1][1]);
            }

            var ax = line[hit.Segment][0];
            var ay = line[hit.Segment][1];
            var bx = line[hit.Segment + 1][0];
            var by = line[hit.Segment + 1][1];
            along += hit.T * Span(ax, ay, bx, by);
            return along;
        }

        public static BlockOutline Containing(double x, double y, BlockOutline[] blocks)
        {
            if (blocks == null)
            {
                return null;
            }

            foreach (var block in blocks)
            {
                if (Inside(x, y, block))
                {
                    return block;
                }
            }

            return null;
        }

        public static bool Blocked(double x0, double y0, double x1, double y1, BlockOutline[] blocks)
        {
            if (blocks == null)
            {
                return false;
            }

            foreach (var block in blocks)
            {
                if (block?.Rings == null || block.Rings.Length == 0 || block.Rings[0] == null)
                {
                    continue;
                }

                if (Inside(x0, y0, block) || Inside(x1, y1, block))
                {
                    return true;
                }

                var ring = block.Rings[0];
                for (var i = 0; i < ring.Length; i++)
                {
                    var next = ring[(i + 1) % ring.Length];
                    if (SegmentsCross(x0, y0, x1, y1, ring[i][0], ring[i][1], next[0], next[1]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool Inside(double x, double y, BlockOutline block)
        {
            if (block?.Rings == null || block.Rings.Length == 0 || block.Rings[0] == null)
            {
                return false;
            }

            if (!InsideRing(x, y, block.Rings[0]))
            {
                return false;
            }

            for (var i = 1; i < block.Rings.Length; i++)
            {
                if (block.Rings[i] != null && InsideRing(x, y, block.Rings[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool InsideRing(double x, double y, double[][] ring)
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

                var hit = (((ring[j][0] - ring[i][0]) * (y - yi)) / (yj - yi)) + ring[i][0];
                if (x < hit)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        static bool SegmentsCross(
            double ax,
            double ay,
            double bx,
            double by,
            double cx,
            double cy,
            double dx,
            double dy)
        {
            var ab = Side(ax, ay, bx, by, cx, cy) * Side(ax, ay, bx, by, dx, dy);
            var cd = Side(cx, cy, dx, dy, ax, ay) * Side(cx, cy, dx, dy, bx, by);
            return ab < 0d && cd < 0d;
        }

        static double Side(double ax, double ay, double bx, double by, double px, double py)
        {
            return ((bx - ax) * (py - ay)) - ((by - ay) * (px - ax));
        }

        static double Span(double ax, double ay, double bx, double by)
        {
            var dx = bx - ax;
            var dy = by - ay;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }
}
