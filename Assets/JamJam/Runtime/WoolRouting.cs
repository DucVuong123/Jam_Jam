using System;
using System.Collections.Generic;

namespace JamJam
{
    public readonly struct RoutePoint
    {
        public readonly float X;
        public readonly float Z;

        public RoutePoint(float x, float z) { X = x; Z = z; }
    }

    public static class WoolRouting
    {
        public const float Speed = 9f;
        public const float CellSize = 0.8f;
        public const float ExitMargin = 0.25f;
        public const float BoardCenterZ = -6.4f;
        public const float TrayZ = -0.75f;
        public const float PadHalfDepth = 0.41f;
        public const float LaneClearance = 0.05f;
        public const float SlotSpacing = 1.6f;

        public static RoutePoint BlockCenter(BlockDefinition block, LevelDefinition level)
        {
            return new RoutePoint((block.X + (block.Width - level.Columns) * 0.5f) * CellSize,
                BoardCenterZ + (block.Y + (block.Height - level.Rows) * 0.5f) * CellSize);
        }

        public static RoutePoint SlotCenter(int slot)
        {
            return new RoutePoint((slot - 2.5f) * SlotSpacing, TrayZ);
        }

        public static float RingOffset(BlockDefinition block)
        {
            return ExitMargin + MaxHalf(block);
        }

        public static float MaxHalf(BlockDefinition block)
        {
            if (WoolShape.IsDiagonal(block.Direction)) return (float)Math.Sqrt(block.Width * block.Width + block.Height * block.Height) * CellSize * 0.5f;
            return Math.Max(block.Width, block.Height) * CellSize * 0.5f;
        }

        public static float LaneZFor(LevelDefinition level, BlockDefinition block)
        {
            return BoardCenterZ + level.Rows * CellSize * 0.5f + RingOffset(block);
        }

        public static bool LaneFitsTray(LevelDefinition level, BlockDefinition block)
        {
            return LaneZFor(level, block) + MaxHalf(block) <= TrayZ - PadHalfDepth - LaneClearance;
        }

        public static bool TryFind(LevelDefinition level, IReadOnlyList<BlockState> blocks, int index, int slot,
            out IReadOnlyList<RoutePoint> points, out float exitDistance)
        {
            points = Array.Empty<RoutePoint>();
            exitDistance = 0;
            if (level == null || blocks == null || index < 0 || index >= blocks.Count) return false;
            var block = blocks[index].Definition;
            if (level.Columns < 1 || level.Rows < 1 || block.Width < 1 || block.Height < 1) return false;
            if (!WoolShape.IsValid(block.Direction) || !WoolShape.LaneClear(level, blocks, index, false)) return false;
            var start = BlockCenter(block, level);
            var target = SlotCenter(slot);
            var offset = RingOffset(block);
            var left = -level.Columns * CellSize * 0.5f - offset;
            var right = level.Columns * CellSize * 0.5f + offset;
            var bottom = BoardCenterZ - level.Rows * CellSize * 0.5f - offset;
            var top = BoardCenterZ + level.Rows * CellSize * 0.5f + offset;
            var lane = top;
            var route = new List<RoutePoint> { start };
            if (WoolShape.IsDiagonal(block.Direction)) Diagonal(route, block.Direction, start, target, left, right, bottom, top);
            else if (block.Direction == ExitDirection.Up) Add(route, new RoutePoint(start.X, top));
            else if (block.Direction == ExitDirection.Left) Add(route, new RoutePoint(left, start.Z));
            else if (block.Direction == ExitDirection.Right) Add(route, new RoutePoint(right, start.Z));
            else
            {
                Add(route, new RoutePoint(start.X, bottom));
                var viaLeft = start.X - left + Math.Abs(target.X - left);
                var viaRight = right - start.X + Math.Abs(right - target.X);
                Add(route, new RoutePoint(viaLeft <= viaRight ? left : right, bottom));
            }
            exitDistance = Distance(route[1].X - start.X, route[1].Z - start.Z);
            Add(route, new RoutePoint(route[route.Count - 1].X, lane));
            Add(route, new RoutePoint(target.X, lane));
            Add(route, target);
            points = route;
            return true;
        }

        static void Diagonal(List<RoutePoint> route, ExitDirection direction, RoutePoint start, RoutePoint target,
            float left, float right, float bottom, float top)
        {
            WoolShape.Step(direction, out var dx, out var dz);
            var alongX = ((dx > 0 ? right : left) - start.X) / dx;
            var alongZ = ((dz > 0 ? top : bottom) - start.Z) / dz;
            if (alongZ <= alongX)
            {
                var hit = new RoutePoint(start.X + dx * alongZ, dz > 0 ? top : bottom);
                Add(route, hit);
                if (dz > 0) return;
                var viaLeft = hit.X - left + Math.Abs(target.X - left);
                var viaRight = right - hit.X + Math.Abs(right - target.X);
                Add(route, new RoutePoint(viaLeft <= viaRight ? left : right, bottom));
                return;
            }
            Add(route, new RoutePoint(dx > 0 ? right : left, start.Z + dz * alongX));
        }

        static float Distance(float dx, float dz)
        {
            if (dx == 0) return Math.Abs(dz);
            if (dz == 0) return Math.Abs(dx);
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static void Add(List<RoutePoint> points, RoutePoint point)
        {
            var last = points[points.Count - 1];
            if (Distance(point.X - last.X, point.Z - last.Z) > 0.0001f) points.Add(point);
        }

        public static float Length(IReadOnlyList<RoutePoint> points)
        {
            var length = 0f;
            if (points != null)
                for (var i = 1; i < points.Count; i++)
                    length += Distance(points[i].X - points[i - 1].X, points[i].Z - points[i - 1].Z);
            return length;
        }

        public static void Evaluate(IReadOnlyList<RoutePoint> points, float distance,
            out RoutePoint position, out RoutePoint direction)
        {
            position = default;
            direction = default;
            if (points == null || points.Count == 0) return;
            position = points[0];
            distance = float.IsNaN(distance) ? 0 : Math.Max(0, distance);
            for (var i = 1; i < points.Count; i++)
            {
                var start = points[i - 1];
                var end = points[i];
                var dx = end.X - start.X;
                var dz = end.Z - start.Z;
                var length = Distance(dx, dz);
                if (length <= 0.0001f) continue;
                direction = new RoutePoint(dx / length, dz / length);
                if (distance <= length)
                {
                    position = new RoutePoint(start.X + direction.X * distance, start.Z + direction.Z * distance);
                    return;
                }
                distance -= length;
                position = end;
            }
        }
    }
}
