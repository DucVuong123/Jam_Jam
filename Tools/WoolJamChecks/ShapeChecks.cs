using System;
using System.Collections.Generic;
using JamJam;

static class ShapeChecks
{
    static int passed;
    public static int Assertions => passed;
    public static int Boards { get; private set; }
    public static int Comparisons { get; private set; }

    static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        passed++;
    }

    static bool Near(float a, float b) { return Math.Abs(a - b) < 0.0002f; }

    static bool Has(LevelDefinition level, string error) { return new List<string>(WoolGame.Validate(level)).Contains(error); }

    static IReadOnlyList<ShapePoint> Box(float x0, float y0, float x1, float y1)
    {
        return new[] { new ShapePoint(x0, y0), new ShapePoint(x1, y0), new ShapePoint(x1, y1), new ShapePoint(x0, y1) };
    }

    static BlockDefinition D(float x, float y, ExitDirection direction, int width = 2, int height = 1)
    {
        return new BlockDefinition { X = x, Y = y, Width = width, Height = height, Direction = direction, Color = 0, Capacity = 1 };
    }

    static void Primitives()
    {
        Check(WoolShape.Overlaps(Box(0, 0, 2, 1), Box(1, 0, 3, 1)), "Overlapping boxes intersect");
        Check(!WoolShape.Overlaps(Box(0, 0, 2, 1), Box(2, 0, 3, 1)), "Edge contact is not an intersection");
        Check(!WoolShape.Overlaps(Box(0, 0, 2, 1), Box(2.00005f, 0, 3, 1)), "Gap under epsilon is not an intersection");
        Check(!WoolShape.Overlaps(Box(0, 0, 2, 1), Box(1.99995f, 0, 3, 1)), "Penetration under epsilon is ignored");
        Check(WoolShape.Overlaps(Box(0, 0, 2, 1), Box(1.9f, 0.9f, 3, 2)), "Corner penetration intersects");
        WoolShape.Footprint(D(2, 2.5f, ExitDirection.UpRight), out var diagonal);
        WoolShape.Bounds(diagonal, out var minX, out var minY, out var maxX, out var maxY);
        Check(Near(minX, 3 - 1.06066f) && Near(maxX, 3 + 1.06066f) && Near(minY, 3 - 1.06066f) && Near(maxY, 3 + 1.06066f), "45 degree 2x1 footprint has half extent 1.0607");
        Check(Near(diagonal[1].X - diagonal[0].X, 2 * WoolShape.Diagonal) && Near(diagonal[1].Y - diagonal[0].Y, 2 * WoolShape.Diagonal), "UpRight long side points up-right");
        WoolShape.Footprint(D(2, 2.5f, ExitDirection.UpLeft), out var mirrored);
        Check(Near(mirrored[1].X - mirrored[0].X, 2 * WoolShape.Diagonal) && Near(mirrored[1].Y - mirrored[0].Y, -2 * WoolShape.Diagonal), "UpLeft long side points down-right/up-left");
        Check(WoolShape.Overlaps(diagonal, Box(3.6f, 3.6f, 4.6f, 4.6f)) && !WoolShape.Overlaps(diagonal, Box(3.6f, 1.4f, 4.6f, 2.4f)), "SAT respects the rotated corners");
        WoolShape.Sweep(Box(0, 0, 2, 1), 0, 3, out var tall);
        WoolShape.Bounds(tall, out minX, out minY, out maxX, out maxY);
        Check(tall.Count == 4 && minX == 0 && maxX == 2 && minY == 0 && maxY == 4, "Orthogonal sweep is a rectangle");
        WoolShape.Sweep(diagonal, 2, 2, out var hull);
        Check(hull.Count == 6, "Diagonal sweep along its long side is a hexagon");
        for (var i = 0; i < hull.Count; i++)
        {
            var a = hull[i]; var b = hull[(i + 1) % hull.Count]; var c = hull[(i + 2) % hull.Count];
            Check((b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X) > 0, "Sweep hull is strictly convex counter-clockwise");
        }
        Check(WoolShape.ExitDistance(Box(3, 2, 5, 3), ExitDirection.Up, 8, 6) == 4, "Up exit distance is rows minus bottom");
        Check(WoolShape.ExitDistance(Box(3, 2, 5, 3), ExitDirection.Left, 8, 6) == 5, "Left exit distance is right edge");
        var exit = WoolShape.ExitDistance(diagonal, ExitDirection.UpRight, 8, 6);
        Check(Near(exit, (6 - (3 - 1.06066f)) / WoolShape.Diagonal), "Diagonal exit distance clears the nearer board side");
        WoolShape.Clip(Box(-1, 1, 3, 2), 2, 4, out var clipped);
        WoolShape.Bounds(clipped, out minX, out minY, out maxX, out maxY);
        Check(clipped.Count == 4 && minX == 0 && maxX == 2 && minY == 1 && maxY == 2, "Clip trims to the board");
        WoolShape.Clip(Box(3, 1, 4, 2), 2, 4, out clipped);
        Check(clipped.Count == 0, "Clip outside the board is empty");
        Check(WoolShape.Inside(diagonal, 6, 6) && !WoolShape.Inside(diagonal, 4, 6), "Inside uses rotated bounds");
        Check(WoolShape.ArrowQuarter(ExitDirection.UpRight) == 1 && WoolShape.ArrowQuarter(ExitDirection.DownLeft) == 3 &&
            WoolShape.ArrowQuarter(ExitDirection.UpLeft) == 3 && WoolShape.ArrowQuarter(ExitDirection.DownRight) == 1, "Diagonal arrows run along the long side");
        for (var d = 0; d < 8; d++)
        {
            var direction = (ExitDirection)d;
            Check(WoolShape.Opposite(WoolShape.Opposite(direction)) == direction && WoolShape.Opposite(direction) != direction, "Opposite is an involution: " + direction);
            WoolShape.Step(direction, out var sx, out var sy);
            WoolShape.Step(WoolShape.Opposite(direction), out var ox, out var oy);
            Check(Near(sx, -ox) && Near(sy, -oy) && Near(sx * sx + sy * sy, 1), "Unit step and opposite: " + direction);
            var radians = WoolShape.Angle(direction) * Math.PI / 180;
            Check(Near(sx, (float)Math.Sin(radians)) && Near(sy, (float)Math.Cos(radians)), "Compass angle matches step: " + direction);
            if (d < 4) Check(WoolShape.ArrowQuarter(direction) == d && WoolShape.RestYaw(direction) == 0, "Cardinal arrow and yaw unchanged: " + direction);
        }
    }

    static bool OldClear(LevelDefinition level, IReadOnlyList<BlockState> blocks, int index)
    {
        var a = blocks[index].Definition;
        const float epsilon = 0.0001f;
        for (var i = 0; i < blocks.Count; i++)
        {
            var other = blocks[i];
            if (i == index || other.Phase == 2) continue;
            var b = other.Definition;
            var x = b.X; var y = b.Y;
            var width = (float)b.Width; var height = (float)b.Height;
            if (other.Phase == 1)
            {
                WoolRouting.Evaluate(other.Route, other.Travel * WoolRouting.Speed, out var point, out _);
                x = point.X / WoolRouting.CellSize - (b.Width - level.Columns) * 0.5f;
                y = (point.Z - WoolRouting.BoardCenterZ) / WoolRouting.CellSize - (b.Height - level.Rows) * 0.5f;
            }
            var left = Math.Max(0, x); var right = Math.Min(level.Columns, x + width);
            var bottom = Math.Max(0, y); var top = Math.Min(level.Rows, y + height);
            if (other.Phase == 1 && right - left > epsilon && top - bottom > epsilon)
            {
                if (b.Direction == ExitDirection.Up) top = level.Rows;
                else if (b.Direction == ExitDirection.Down) bottom = 0;
                else if (b.Direction == ExitDirection.Right) right = level.Columns;
                else left = 0;
            }
            if (right - left <= epsilon || top - bottom <= epsilon) continue;
            var overlapX = a.X < right - epsilon && a.X + a.Width > left + epsilon;
            var overlapY = a.Y < top - epsilon && a.Y + a.Height > bottom + epsilon;
            if ((a.Direction == ExitDirection.Up && overlapX && top > a.Y + a.Height + epsilon) ||
                (a.Direction == ExitDirection.Down && overlapX && bottom < a.Y - epsilon) ||
                (a.Direction == ExitDirection.Right && overlapY && right > a.X + a.Width + epsilon) ||
                (a.Direction == ExitDirection.Left && overlapY && left < a.X - epsilon)) return false;
        }
        var steps = a.Direction == ExitDirection.Up ? level.Rows - a.Y : a.Direction == ExitDirection.Down ? a.Y + a.Height :
            a.Direction == ExitDirection.Right ? level.Columns - a.X : a.X + a.Width;
        var sx = a.Direction == ExitDirection.Right ? 1 : a.Direction == ExitDirection.Left ? -1 : 0;
        var sy = a.Direction == ExitDirection.Up ? 1 : a.Direction == ExitDirection.Down ? -1 : 0;
        for (var step = 1; step <= steps; step++)
            for (var i = 0; i < blocks.Count; i++)
            {
                if (i == index || blocks[i].Phase != 0) continue;
                var o = blocks[i].Definition;
                var px = a.X + sx * step; var py = a.Y + sy * step;
                if (px < o.X + o.Width && px + a.Width > o.X && py < o.Y + o.Height && py + a.Height > o.Y) return false;
            }
        return true;
    }

    static void RandomBoard(Random random, out LevelDefinition level)
    {
        var columns = random.Next(3, 9);
        var rows = random.Next(2, 7);
        var occupied = new bool[columns, rows];
        var blocks = new List<BlockDefinition>();
        var sizes = new[] { new[] { 1, 1 }, new[] { 2, 1 }, new[] { 1, 2 }, new[] { 2, 2 }, new[] { 3, 1 }, new[] { 1, 3 } };
        var attempts = random.Next(4, 40);
        for (var n = 0; n < attempts; n++)
        {
            var size = sizes[random.Next(sizes.Length)];
            if (size[0] > columns || size[1] > rows) continue;
            var x = random.Next(columns - size[0] + 1);
            var y = random.Next(rows - size[1] + 1);
            var free = true;
            for (var i = x; i < x + size[0]; i++) for (var j = y; j < y + size[1]; j++) free &= !occupied[i, j];
            if (!free) continue;
            for (var i = x; i < x + size[0]; i++) for (var j = y; j < y + size[1]; j++) occupied[i, j] = true;
            blocks.Add(new BlockDefinition { X = x, Y = y, Width = size[0], Height = size[1], Direction = (ExitDirection)random.Next(4), Color = 0, Capacity = 1 });
        }
        if (blocks.Count == 0) blocks.Add(new BlockDefinition { X = 0, Y = 0, Width = 1, Height = 1, Color = 0, Capacity = 1 });
        level = new LevelDefinition { Columns = columns, Rows = rows, Slots = 6, PathLength = 1000, InitialHead = 1, Speed = 0.001f, Spacing = 0.1f, Blocks = blocks.ToArray(), Yarn = new int[blocks.Count] };
    }

    static void Compare(WoolGame game, LevelDefinition level)
    {
        var free = false;
        for (var s = 0; s < game.OpenSlots; s++) free |= game.Spools[s].Block < 0;
        for (var i = 0; i < game.Blocks.Count; i++)
        {
            var expected = free && game.Status == RoundStatus.Playing && game.Blocks[i].Phase == 0 && OldClear(level, game.Blocks, i);
            if (game.CanSelect(i) != expected) throw new InvalidOperationException("Grid oracle mismatch on board " + Boards + " block " + i);
            Comparisons++;
        }
    }

    static void Equivalence()
    {
        var random = new Random(20261108);
        for (var board = 0; board < 2500; board++)
        {
            RandomBoard(random, out var level);
            var errors = WoolGame.Validate(level);
            if (errors.Count > 0) continue;
            var game = new WoolGame(level);
            Boards++;
            Compare(game, level);
            for (var turn = 0; turn < 60 && game.Status == RoundStatus.Playing; turn++)
            {
                var pick = random.Next(game.Blocks.Count);
                if (game.CanSelect(pick)) game.Select(pick);
                var frames = random.Next(0, 12);
                for (var f = 0; f < frames; f++) game.Step(0.02f);
                Compare(game, level);
            }
        }
        Check(Boards >= 2000, "At least 2000 orthogonal boards compared, got " + Boards);
    }

    static void Lanes()
    {
        var level = new LevelDefinition { Columns = 8, Rows = 6, Slots = 4, Blocks = new[] { D(2, 1.5f, ExitDirection.UpRight), D(5, 4, ExitDirection.Up, 1, 1) }, Yarn = new[] { 0, 0 } };
        var game = new WoolGame(level);
        Check(!game.CanSelect(0), "Block on the diagonal ray blocks the diagonal arrow");
        level.Blocks[1] = D(0, 5, ExitDirection.Up, 1, 1);
        game = new WoolGame(level);
        Check(game.CanSelect(0), "Block off the diagonal ray does not block it");
        level.Blocks[1] = D(1, 0, ExitDirection.Up, 1, 1);
        game = new WoolGame(level);
        Check(game.CanSelect(0), "Block behind the diagonal arrow does not block it");
        level.Blocks[1] = D(5, 4, ExitDirection.Up, 1, 1);
        game = new WoolGame(level);
        Check(game.Select(1) && !game.CanSelect(0), "Moving blocker still on the ray closes the diagonal lane");
        for (var i = 0; i < 200 && !game.CanSelect(0); i++) game.Step(0.02f);
        Check(game.Blocks[1].Phase == 1 && game.CanSelect(0), "Diagonal lane opens once the mover clears it");
        level = new LevelDefinition { Columns = 8, Rows = 6, Slots = 4, Blocks = new[] { D(6, 0, ExitDirection.Up), D(2, 1.5f, ExitDirection.UpRight) }, Yarn = new[] { 0, 0 } };
        game = new WoolGame(level);
        Check(game.CanSelect(0) && game.CanSelect(1), "Cardinal and diagonal neighbours both clear");
        Check(game.Select(1) && !game.CanSelect(0), "Moving diagonal block still over the cardinal lane closes it");
        level = new LevelDefinition { Columns = 8, Rows = 6, Slots = 4, Blocks = new[] { D(2, 1.5f, ExitDirection.UpRight), D(3, 2.5f, ExitDirection.UpRight) }, Yarn = new[] { 0, 0 } };
        Check(Has(level, "Overlapping blocks"), "Overlapping diagonal footprints are rejected");
        level.Blocks[1] = D(2.5f, 2.5f, ExitDirection.UpLeft);
        Check(Has(level, "Overlapping blocks"), "Crossing diagonal footprints are rejected");
        level.Blocks[1] = D(2.5f, 1, ExitDirection.UpRight);
        Check(Has(level, "Overlapping blocks"), "Parallel diagonal blocks 0.71 apart overlap");
        level.Blocks[1] = D(3, 1, ExitDirection.UpRight);
        Check(WoolGame.Validate(level).Count == 0, "Parallel diagonal blocks 1.06 apart are valid");
        level.Blocks[0] = D(-0.5f, 1, ExitDirection.UpRight);
        Check(Has(level, "Invalid block"), "Diagonal footprint outside the board is rejected");
        level.Blocks[0] = D(0, 0, (ExitDirection)8);
        Check(Has(level, "Invalid block"), "Direction 8 is rejected");
    }

    static float Outside(LevelDefinition level, RoutePoint point)
    {
        var dx = Math.Abs(point.X) - level.Columns * WoolRouting.CellSize * 0.5f;
        var dz = Math.Abs(point.Z - WoolRouting.BoardCenterZ) - level.Rows * WoolRouting.CellSize * 0.5f;
        return Math.Max(dx, dz);
    }

    static void Routes()
    {
        for (var d = 4; d < 8; d++)
            for (var cx = 1.5f; cx <= 6.5f; cx += 0.5f)
                for (var cy = 1.5f; cy <= 4.5f; cy += 0.5f)
                    for (var slot = 0; slot < 6; slot++)
                    {
                        var block = D(cx - 1, cy - 0.5f, (ExitDirection)d);
                        var level = new LevelDefinition { Columns = 8, Rows = 6, Blocks = new[] { block } };
                        var states = new[] { new BlockState { Definition = block } };
                        Check(WoolRouting.TryFind(level, states, 0, slot, out var route, out var exit), "Diagonal block has a route");
                        var start = WoolRouting.BlockCenter(block, level);
                        var end = WoolRouting.SlotCenter(slot);
                        Check(route[0].X == start.X && route[0].Z == start.Z && route[route.Count - 1].X == end.X && route[route.Count - 1].Z == end.Z, "Diagonal route starts at the block and ends at the slot");
                        WoolShape.Step(block.Direction, out var sx, out var sz);
                        Check(Near(route[1].X, start.X + sx * exit) && Near(route[1].Z, start.Z + sz * exit), "First segment follows the diagonal arrow");
                        Check(Near(Outside(level, route[1]), WoolRouting.RingOffset(block)), "First segment ends on the ring");
                        for (var i = 2; i < route.Count; i++)
                        {
                            var ddx = Math.Abs(route[i].X - route[i - 1].X); var ddz = Math.Abs(route[i].Z - route[i - 1].Z);
                            Check((ddx < 0.0001f) != (ddz < 0.0001f), "Every segment after the diagonal is cardinal");
                        }
                        for (var distance = exit; distance <= WoolRouting.Length(route); distance += 0.0625f)
                        {
                            WoolRouting.Evaluate(route, distance, out var point, out _);
                            Check(Outside(level, point) >= WoolRouting.RingOffset(block) - 0.0002f, "Diagonal block stays on the ring after its exit");
                        }
                        Check(Near(route[route.Count - 2].Z, WoolRouting.LaneZFor(level, block)) && Near(route[route.Count - 2].X, end.X), "Diagonal route enters the slot from the top lane");
                        Check(WoolRouting.LaneFitsTray(level, block), "Diagonal lane fits under the tray");
                    }
        Check(Near(WoolRouting.MaxHalf(D(0, 0, ExitDirection.UpRight)), (float)Math.Sqrt(5) * 0.5f * WoolRouting.CellSize), "Diagonal MaxHalf is half the footprint diagonal");
        IReadOnlyList<RoutePoint> slope = new[] { new RoutePoint(0, 0), new RoutePoint(3, 4) };
        Check(Near(WoolRouting.Length(slope), 5), "Diagonal segment length is Euclidean");
        WoolRouting.Evaluate(slope, 2.5f, out var mid, out var heading);
        Check(Near(mid.X, 1.5f) && Near(mid.Z, 2) && Near(heading.X, 0.6f) && Near(heading.Z, 0.8f), "Evaluate moves at unit speed along a diagonal");
    }

    public static void Run()
    {
        Primitives();
        Lanes();
        Routes();
        Equivalence();
    }
}
