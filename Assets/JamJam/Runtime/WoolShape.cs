using System;
using System.Collections.Generic;

namespace JamJam
{
    public readonly struct ShapePoint
    {
        public readonly float X;
        public readonly float Y;

        public ShapePoint(float x, float y) { X = x; Y = y; }
    }

    public static class WoolShape
    {
        public const float Epsilon = 0.0001f;
        public const float Diagonal = 0.70710678f;
        static readonly float[] StepX = { 0, 1, 0, -1, Diagonal, Diagonal, -Diagonal, -Diagonal };
        static readonly float[] StepY = { 1, 0, -1, 0, Diagonal, -Diagonal, -Diagonal, Diagonal };
        static readonly float[] Angles = { 0, 90, 180, 270, 45, 135, 225, 315 };
        static readonly float[] Yaws = { 0, 0, 0, 0, -45, 45, -45, 45 };

        public static bool IsValid(ExitDirection direction) { return (int)direction >= 0 && (int)direction <= 7; }
        public static bool IsDiagonal(ExitDirection direction) { return (int)direction >= 4 && (int)direction <= 7; }
        public static float Angle(ExitDirection direction) { return IsValid(direction) ? Angles[(int)direction] : 0; }
        public static float RestYaw(ExitDirection direction) { return IsValid(direction) ? Yaws[(int)direction] : 0; }

        public static int ArrowQuarter(ExitDirection direction)
        {
            var quarter = (int)Math.Round((Angle(direction) - RestYaw(direction)) / 90f);
            return ((quarter % 4) + 4) % 4;
        }

        public static ExitDirection Opposite(ExitDirection direction)
        {
            var index = (int)direction;
            return (ExitDirection)(index < 4 ? (index + 2) % 4 : 4 + (index - 2) % 4);
        }

        public static void Step(ExitDirection direction, out float x, out float y)
        {
            var index = IsValid(direction) ? (int)direction : 0;
            x = StepX[index];
            y = StepY[index];
        }

        public static void Footprint(BlockDefinition block, out IReadOnlyList<ShapePoint> corners)
        {
            Footprint(block, block.X, block.Y, out corners);
        }

        public static void Footprint(BlockDefinition block, float x, float y, out IReadOnlyList<ShapePoint> corners)
        {
            if (!IsDiagonal(block.Direction))
            {
                corners = new[]
                {
                    new ShapePoint(x, y), new ShapePoint(x + block.Width, y),
                    new ShapePoint(x + block.Width, y + block.Height), new ShapePoint(x, y + block.Height)
                };
                return;
            }
            var sin = block.Direction == ExitDirection.UpRight || block.Direction == ExitDirection.DownLeft ? Diagonal : -Diagonal;
            var halfWidth = block.Width * 0.5f;
            var halfHeight = block.Height * 0.5f;
            var centerX = x + halfWidth;
            var centerY = y + halfHeight;
            var result = new ShapePoint[4];
            for (var i = 0; i < 4; i++)
            {
                var localX = i == 0 || i == 3 ? -halfWidth : halfWidth;
                var localY = i < 2 ? -halfHeight : halfHeight;
                result[i] = new ShapePoint(centerX + localX * Diagonal - localY * sin, centerY + localX * sin + localY * Diagonal);
            }
            corners = result;
        }

        public static void Sweep(IReadOnlyList<ShapePoint> polygon, float dx, float dy, out IReadOnlyList<ShapePoint> hull)
        {
            var points = new List<ShapePoint>(polygon.Count * 2);
            foreach (var point in polygon)
            {
                points.Add(point);
                points.Add(new ShapePoint(point.X + dx, point.Y + dy));
            }
            Hull(points, out hull);
        }

        public static void Hull(List<ShapePoint> points, out IReadOnlyList<ShapePoint> hull)
        {
            points.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            var result = new List<ShapePoint>(points.Count + 1);
            for (var pass = 0; pass < 2; pass++)
            {
                var start = result.Count;
                for (var k = 0; k < points.Count; k++)
                {
                    var point = points[pass == 0 ? k : points.Count - 1 - k];
                    while (result.Count >= start + 2 && Cross(result[result.Count - 2], result[result.Count - 1], point) <= 1e-7f)
                        result.RemoveAt(result.Count - 1);
                    result.Add(point);
                }
                result.RemoveAt(result.Count - 1);
            }
            hull = result;
        }

        static float Cross(ShapePoint o, ShapePoint a, ShapePoint b)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }

        public static bool Overlaps(IReadOnlyList<ShapePoint> a, IReadOnlyList<ShapePoint> b)
        {
            if (a.Count < 3 || b.Count < 3) return false;
            return !Separated(a, a, b) && !Separated(b, a, b);
        }

        static bool Separated(IReadOnlyList<ShapePoint> edges, IReadOnlyList<ShapePoint> a, IReadOnlyList<ShapePoint> b)
        {
            for (var i = 0; i < edges.Count; i++)
            {
                var from = edges[i];
                var to = edges[(i + 1) % edges.Count];
                var ex = to.X - from.X;
                var ey = to.Y - from.Y;
                float nx, ny;
                if (ey == 0) { nx = 0; ny = 1; }
                else if (ex == 0) { nx = 1; ny = 0; }
                else
                {
                    var length = (float)Math.Sqrt(ex * ex + ey * ey);
                    if (length < 1e-6f) continue;
                    nx = -ey / length;
                    ny = ex / length;
                }
                Project(a, nx, ny, out var minA, out var maxA);
                Project(b, nx, ny, out var minB, out var maxB);
                if (Math.Min(maxA, maxB) - Math.Max(minA, minB) <= Epsilon) return true;
            }
            return false;
        }

        static void Project(IReadOnlyList<ShapePoint> polygon, float nx, float ny, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            foreach (var point in polygon)
            {
                var value = nx == 0 ? point.Y * ny : ny == 0 ? point.X * nx : point.X * nx + point.Y * ny;
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        public static void Bounds(IReadOnlyList<ShapePoint> polygon, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = minY = float.MaxValue;
            maxX = maxY = float.MinValue;
            foreach (var point in polygon)
            {
                minX = Math.Min(minX, point.X); maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y); maxY = Math.Max(maxY, point.Y);
            }
        }

        public static bool Inside(IReadOnlyList<ShapePoint> polygon, int columns, int rows)
        {
            Bounds(polygon, out var minX, out var minY, out var maxX, out var maxY);
            return minX >= -Epsilon && minY >= -Epsilon && maxX <= columns + Epsilon && maxY <= rows + Epsilon;
        }

        public static float ExitDistance(IReadOnlyList<ShapePoint> polygon, ExitDirection direction, int columns, int rows)
        {
            Step(direction, out var dx, out var dy);
            Bounds(polygon, out var minX, out var minY, out var maxX, out var maxY);
            var distance = float.MaxValue;
            if (dx > 0) distance = Math.Min(distance, (columns - minX) / dx);
            if (dx < 0) distance = Math.Min(distance, maxX / -dx);
            if (dy > 0) distance = Math.Min(distance, (rows - minY) / dy);
            if (dy < 0) distance = Math.Min(distance, maxY / -dy);
            return Math.Max(0, distance);
        }

        public static void Lane(IReadOnlyList<ShapePoint> footprint, ExitDirection direction, int columns, int rows, out IReadOnlyList<ShapePoint> lane)
        {
            Step(direction, out var dx, out var dy);
            var distance = ExitDistance(footprint, direction, columns, rows);
            Sweep(footprint, dx * distance, dy * distance, out lane);
        }

        public static void Clip(IReadOnlyList<ShapePoint> polygon, int columns, int rows, out IReadOnlyList<ShapePoint> clipped)
        {
            var current = new List<ShapePoint>(polygon);
            for (var side = 0; side < 4 && current.Count > 0; side++)
            {
                var next = new List<ShapePoint>(current.Count + 2);
                for (var i = 0; i < current.Count; i++)
                {
                    var a = current[i];
                    var b = current[(i + 1) % current.Count];
                    var inA = Keep(a, side, columns, rows);
                    var inB = Keep(b, side, columns, rows);
                    if (inA) next.Add(a);
                    if (inA != inB) next.Add(Cut(a, b, side, columns, rows));
                }
                current = next;
            }
            clipped = current;
        }

        static bool Keep(ShapePoint point, int side, int columns, int rows)
        {
            return side == 0 ? point.X >= 0 : side == 1 ? point.X <= columns : side == 2 ? point.Y >= 0 : point.Y <= rows;
        }

        static ShapePoint Cut(ShapePoint a, ShapePoint b, int side, int columns, int rows)
        {
            if (side < 2)
            {
                var x = side == 0 ? 0f : columns;
                var y = a.Y == b.Y ? a.Y : a.Y + (b.Y - a.Y) * ((x - a.X) / (b.X - a.X));
                return new ShapePoint(x, y);
            }
            var cutY = side == 2 ? 0f : rows;
            var cutX = a.X == b.X ? a.X : a.X + (b.X - a.X) * ((cutY - a.Y) / (b.Y - a.Y));
            return new ShapePoint(cutX, cutY);
        }

        public static bool LaneClear(LevelDefinition level, IReadOnlyList<BlockState> blocks, int index, bool includeMoving)
        {
            var block = blocks[index].Definition;
            Footprint(block, out var footprint);
            Lane(footprint, block.Direction, level.Columns, level.Rows, out var lane);
            Bounds(lane, out var laneMinX, out var laneMinY, out var laneMaxX, out var laneMaxY);
            for (var i = 0; i < blocks.Count; i++)
            {
                var other = blocks[i];
                if (i == index || other.Phase == 2) continue;
                if (other.Phase == 1 && (!includeMoving || other.Travel * WoolRouting.Speed >= other.ExitDistance)) continue;
                IReadOnlyList<ShapePoint> region;
                if (other.Phase == 0) Footprint(other.Definition, out region);
                else
                {
                    Moving(level, other, out var moving);
                    Lane(moving, other.Definition.Direction, level.Columns, level.Rows, out var sweep);
                    Clip(sweep, level.Columns, level.Rows, out region);
                    if (region.Count < 3) continue;
                }
                Bounds(region, out var minX, out var minY, out var maxX, out var maxY);
                if (minX >= laneMaxX - Epsilon || maxX <= laneMinX + Epsilon || minY >= laneMaxY - Epsilon || maxY <= laneMinY + Epsilon) continue;
                if (Overlaps(lane, region)) return false;
            }
            return true;
        }

        public static void Moving(LevelDefinition level, BlockState block, out IReadOnlyList<ShapePoint> footprint)
        {
            var b = block.Definition;
            WoolRouting.Evaluate(block.Route, block.Travel * WoolRouting.Speed, out var point, out _);
            var x = point.X / WoolRouting.CellSize - (b.Width - level.Columns) * 0.5f;
            var y = (point.Z - WoolRouting.BoardCenterZ) / WoolRouting.CellSize - (b.Height - level.Rows) * 0.5f;
            Footprint(b, x, y, out footprint);
        }
    }
}
