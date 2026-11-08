using System;
using System.Collections.Generic;
using JamJam;

static class GeneratorChecks
{
    static int passed;
    public static int Assertions => passed;
    public static uint Fingerprint { get; private set; }

    static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        passed++;
    }

    static float Distance(RoutePoint a, RoutePoint b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dz * dz);
    }

    static void Paths()
    {
        WoolPaths.Sample(WoolPathShape.S, out var s);
        Check(s.Count == 201 && WoolPaths.Length(s) == 32.16851f, "Shape S reproduces the current path length 32.16851");
        for (var shape = 0; shape < 5; shape++)
        {
            var name = ((WoolPathShape)shape).ToString();
            WoolPaths.Sample((WoolPathShape)shape, out var points);
            var length = WoolPaths.Length(points);
            Check(length >= 26 && length <= 40, "Path length 26..40: " + name + " " + length);
            var arc = new float[points.Count];
            for (var i = 1; i < points.Count; i++) arc[i] = arc[i - 1] + Distance(points[i - 1], points[i]);
            foreach (var p in points)
                Check(p.X >= WoolPaths.MinX - 1e-4f && p.X <= WoolPaths.MaxX + 1e-4f && p.Z >= WoolPaths.MinZ - 1e-4f && p.Z <= WoolPaths.MaxZ + 1e-4f, "Path stays on the meadow: " + name);
            var clear = float.MaxValue;
            for (var i = 0; i < points.Count; i++)
                for (var j = i + 1; j < points.Count; j++)
                    if (arc[j] - arc[i] > 4) clear = Math.Min(clear, Distance(points[i], points[j]));
            Check(clear >= 2.2f, "Road never overlaps itself: " + name + " " + clear);
            var end = points[points.Count - 1];
            var before = points[points.Count - 2];
            var step = Distance(end, before);
            var kitten = new RoutePoint(end.X + (end.X - before.X) / step * 0.85f, end.Z + (end.Z - before.Z) / step * 0.85f);
            var room = float.MaxValue;
            for (var i = 0; i < points.Count; i++) if (arc[i] < length - 2) room = Math.Min(room, Distance(kitten, points[i]));
            Check(room >= 2f, "Kitten keeps clear of earlier road: " + name + " " + room);
            var bubble = float.MaxValue;
            var raised = new RoutePoint(kitten.X, kitten.Z + 0.73f);
            for (var i = 0; i < points.Count; i++) if (arc[i] < length - 2) bubble = Math.Min(bubble, Distance(raised, points[i]));
            Check((WoolPathShape)shape == WoolPathShape.S ? bubble > 1.5f : bubble >= 2.1f, "Kitten bubble keeps clear of the road: " + name + " " + bubble);
        }
        Check(WoolPaths.ForLevel(1) == WoolPathShape.S && WoolPaths.ForLevel(2) == WoolPathShape.U && WoolPaths.ForLevel(3) == WoolPathShape.Zigzag &&
            WoolPaths.ForLevel(4) == WoolPathShape.Hook && WoolPaths.ForLevel(5) == WoolPathShape.Spiral && WoolPaths.ForLevel(6) == WoolPathShape.S, "Shapes rotate S, U, Zigzag, Hook, Spiral");
    }

    static void Levels()
    {
        var hash = 2166136261u;
        var shapes = new HashSet<WoolPathShape>();
        for (var level = 1; level <= WoolLevelGenerator.Count; level++)
        {
            Check(WoolLevelGenerator.TryGenerate(level, out var result, out var error), error ?? "generated");
            Check(WoolLevelGenerator.TryGenerate(level, out var again, out _) && WoolLevelGenerator.Fingerprint(again.Data) == WoolLevelGenerator.Fingerprint(result.Data), "Generator is deterministic: level " + level);
            var data = result.Data;
            var recipe = result.Recipe;
            Check(WoolGame.Validate(data).Count == 0, "Generated level " + level + " is valid");
            Check(data.Blocks.Length == recipe.Blocks && data.Columns == recipe.Columns && data.Rows == recipe.Rows, "Level " + level + " follows its recipe size");
            var diagonals = 0;
            var colors = new HashSet<int>();
            foreach (var block in data.Blocks)
            {
                colors.Add(block.Color);
                Check(block.Capacity >= 3 && block.Capacity <= 6 && block.Width * block.Height == 2, "Level " + level + " block is 2x1 with capacity 3..6");
                if (WoolShape.IsDiagonal(block.Direction))
                {
                    diagonals++;
                    Check(block.Width == 2 && block.X * 2 == Math.Floor(block.X * 2) && block.Y * 2 == Math.Floor(block.Y * 2), "Diagonal centre on the half lattice");
                }
                else Check(block.X == Math.Floor(block.X) && block.Y == Math.Floor(block.Y), "Cardinal block on the integer lattice");
            }
            Check(diagonals == recipe.Diagonals && (level < 9 ? diagonals == 0 : diagonals > 0), "Level " + level + " diagonal count");
            Check(colors.Count == recipe.Colors, "Level " + level + " uses " + recipe.Colors + " colours");
            var directions = new int[8];
            foreach (var block in data.Blocks) directions[(int)block.Direction]++;
            var straightKinds = 0;
            var diagonalKinds = 0;
            var most = 0;
            for (var d = 0; d < 8; d++)
            {
                if (directions[d] > 0) { if (d < 4) straightKinds++; else diagonalKinds++; }
                most = Math.Max(most, directions[d]);
            }
            Check(diagonals < 4 || diagonalKinds >= 3, "Level " + level + " mixes diagonal directions: " + diagonalKinds);
            Check(data.Blocks.Length - diagonals < 6 || straightKinds >= 3, "Level " + level + " mixes straight directions: " + straightKinds);
            Check(most * 2 <= data.Blocks.Length, "Level " + level + " no direction above half: " + most + " of " + data.Blocks.Length);
            var states = new List<BlockState>();
            foreach (var block in data.Blocks) states.Add(new BlockState { Definition = block });
            foreach (var index in result.Removal)
            {
                Check(WoolShape.LaneClear(data, states, index, false), "Level " + level + " removal order is always open");
                states[index].Phase = 2;
            }
            WoolSolver.Run(data, out var won, out var peak, out _);
            Check(won && Math.Abs(peak - recipe.Target) <= WoolLevelGenerator.Tolerance && peak == result.Ratio, "Level " + level + " solver ratio " + peak + " near " + recipe.Target);
            Check(data.Speed >= WoolLevelGenerator.MinSpeed / 1000f && data.Speed <= WoolLevelGenerator.MaxSpeed / 1000f, "Level " + level + " speed in range");
            Frame(data);
            shapes.Add(recipe.Shape);
            unchecked { hash = (hash ^ WoolLevelGenerator.Fingerprint(data)) * 16777619u; }
        }
        Check(shapes.Count == 5, "All five dragon paths are used");
        Fingerprint = hash;
        Check(hash.ToString("X8") == WoolLevelGenerator.ExpectedFingerprint, "Generator fingerprint " + hash.ToString("X8") + " equals " + WoolLevelGenerator.ExpectedFingerprint);
    }

    static void Frame(LevelDefinition data)
    {
        var sin = (float)Math.Sin(WoolStage.Pitch * Math.PI / 180);
        var tablet = WoolStage.Size(0.75f);
        var halfX = 0.96f * WoolStage.HalfWidth;
        foreach (var block in data.Blocks)
        {
            var width = block.Width - 2 * 0.09f;
            var height = block.Height - 2 * 0.09f;
            var radius = 0.5f * WoolRouting.CellSize * (float)Math.Sqrt(width * width + height * height);
            for (var slot = 0; slot < 6; slot++)
            {
                Check(WoolRouting.TryFind(data, new[] { new BlockState { Definition = block } }, 0, slot, out var route, out _), "Generated block reaches slot " + slot);
                foreach (var p in route)
                {
                    var low = (p.Z - radius - WoolStage.TargetZ) * sin;
                    var high = (p.Z + radius - WoolStage.TargetZ) * sin;
                    Check(Math.Abs(p.X) + radius < halfX && low > -tablet + WoolStage.BottomBand && high < tablet - WoolStage.HudBand, "Ring route stays inside the frame");
                }
            }
        }
    }

    public static void Run()
    {
        Paths();
        Levels();
    }
}
