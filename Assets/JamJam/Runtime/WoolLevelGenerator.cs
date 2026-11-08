using System;
using System.Collections.Generic;

namespace JamJam
{
    public readonly struct WoolLevelRecipe
    {
        public readonly int Level;
        public readonly int Columns;
        public readonly int Rows;
        public readonly int Blocks;
        public readonly int Colors;
        public readonly int Diagonals;
        public readonly float Target;
        public readonly int Window;
        public readonly int MaxCapacity;
        public readonly float HeadShare;
        public readonly float Spacing;
        public readonly WoolPathShape Shape;
        public readonly string Title;

        public WoolLevelRecipe(int level, int columns, int rows, int blocks, int colors, int diagonals, float target, int window,
            int maxCapacity, float headShare, float spacing, string title)
        {
            Level = level; Columns = columns; Rows = rows; Blocks = blocks; Colors = colors; Diagonals = diagonals;
            Target = target; Window = window; MaxCapacity = maxCapacity; HeadShare = headShare; Spacing = spacing;
            Shape = WoolPaths.ForLevel(level); Title = title;
        }
    }

    public sealed class WoolLevelResult
    {
        public WoolLevelRecipe Recipe;
        public LevelDefinition Data;
        public IReadOnlyList<RoutePoint> Path;
        public int Seed;
        public float Ratio;
        public IReadOnlyList<int> Removal;
    }

    public static class WoolLevelGenerator
    {
        public const int Count = 30;
        public const int MaxSeeds = 50;
        public const int MinSpeed = 120;
        public const int MaxSpeed = 4000;
        public const float Tolerance = 0.03f;
        public const float HeadMargin = 0.02f;
        public const float MaxHeadShare = 0.6f;
        public const string ExpectedFingerprint = "4B0E362C";
        const float ContactGap = 0.1f;
        static readonly string[] Titles =
        {
            "First Threads", "Color Traffic", "The Great Unravel", "Loose Ends", "Soft Landing",
            "Purl Jam", "Knot Rush", "Stitch Lanes", "Bias Cut", "Cozy Break",
            "Skein Shuffle", "Cross Stitch", "Tangle Town", "Woolly Detour", "Lazy Loops",
            "Needle Dash", "Spindle Spin", "Felted Maze", "Double Knit", "Yarn Nap",
            "Cable Twist", "Warp and Weft", "Spool Panic", "Fringe Frenzy", "Sweater Weather",
            "Tassel Storm", "Loom Rush", "Dragon Darn", "Last Stitch", "The Grand Unravel"
        };
        static readonly int[,] Table =
        {
            { 4, 4, 6, 2, 0, 40, 0, 4, 36 },
            { 5, 4, 8, 3, 0, 45, 0, 4, 32 },
            { 6, 4, 10, 3, 0, 50, 0, 4, 28 },
            { 6, 5, 12, 4, 0, 52, 1, 4, 24 },
            { 7, 5, 10, 4, 0, 44, 1, 4, 24 },
            { 7, 5, 14, 4, 0, 56, 1, 4, 24 },
            { 8, 5, 16, 4, 0, 59, 1, 4, 24 },
            { 8, 5, 18, 4, 0, 62, 1, 4, 24 },
            { 8, 5, 13, 4, 3, 58, 2, 5, 24 },
            { 8, 5, 11, 4, 2, 50, 2, 5, 24 },
            { 8, 6, 16, 5, 4, 64, 2, 5, 24 },
            { 8, 6, 17, 5, 4, 68, 2, 5, 24 },
            { 8, 6, 16, 5, 4, 66, 2, 5, 24 },
            { 8, 6, 16, 5, 5, 68, 3, 5, 24 },
            { 8, 6, 13, 5, 4, 60, 3, 5, 24 },
            { 8, 6, 16, 5, 5, 70, 3, 5, 24 },
            { 8, 6, 15, 6, 5, 72, 3, 5, 24 },
            { 8, 6, 15, 6, 6, 74, 4, 5, 24 },
            { 8, 6, 16, 6, 5, 75, 4, 5, 24 },
            { 8, 6, 12, 6, 4, 67, 4, 6, 24 },
            { 8, 6, 15, 6, 6, 76, 4, 6, 24 },
            { 8, 6, 14, 6, 6, 77, 4, 6, 24 },
            { 8, 6, 15, 6, 6, 78, 5, 6, 24 },
            { 8, 6, 14, 6, 6, 80, 5, 6, 24 },
            { 8, 6, 12, 6, 5, 72, 5, 6, 24 },
            { 8, 6, 14, 6, 6, 82, 5, 6, 24 },
            { 8, 6, 14, 6, 6, 84, 6, 6, 24 },
            { 8, 6, 14, 6, 7, 85, 6, 6, 24 },
            { 8, 6, 14, 6, 7, 86, 6, 6, 24 },
            { 8, 6, 15, 6, 7, 88, 6, 6, 24 }
        };

        sealed class Rng
        {
            uint state;
            public Rng(int seed) { state = unchecked((uint)seed * 0x9E3779B9u + 0x6D2B79F5u); }

            public uint Next()
            {
                unchecked
                {
                    state += 0x9E3779B9u;
                    var z = state;
                    z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                    z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                    return z ^ (z >> 16);
                }
            }

            public int Range(int min, int max) { return max <= min ? min : min + (int)(Next() % (uint)(max - min)); }

            public void Shuffle<T>(IList<T> list)
            {
                for (var i = list.Count - 1; i > 0; i--)
                {
                    var j = Range(0, i + 1);
                    var swap = list[i]; list[i] = list[j]; list[j] = swap;
                }
            }
        }

        public static void Recipe(int level, out WoolLevelRecipe recipe)
        {
            var i = Math.Max(1, Math.Min(Count, level)) - 1;
            recipe = new WoolLevelRecipe(i + 1, Table[i, 0], Table[i, 1], Table[i, 2], Table[i, 3], Table[i, 4], Table[i, 5] / 100f,
                Table[i, 6], Table[i, 7], Math.Min((float)(Table[i, 5] / 100f - HeadMargin), MaxHeadShare), Table[i, 8] / 100f, Titles[i]);
        }

        public static bool TryGenerate(int level, out WoolLevelResult result, out string error)
        {
            Recipe(level, out var recipe);
            WoolPaths.Sample(recipe.Shape, out var path);
            var length = WoolPaths.Length(path);
            for (var attempt = 0; attempt < MaxSeeds; attempt++)
            {
                var seed = recipe.Level * 1000 + attempt;
                if (!TryLayout(recipe, seed, out var blocks, out var removal) || !Mixed(recipe, blocks)) continue;
                Build(recipe, seed, blocks, removal, length, out var data);
                if (!TryTune(data, recipe.Target, out var ratio)) continue;
                result = new WoolLevelResult { Recipe = recipe, Data = data, Path = path, Seed = seed, Ratio = ratio, Removal = removal };
                error = null;
                return true;
            }
            result = null;
            error = "Level " + recipe.Level + " has no solvable seed within " + MaxSeeds + " attempts";
            return false;
        }

        public static bool TryLayout(WoolLevelRecipe recipe, int seed, out IReadOnlyList<BlockDefinition> blocks, out IReadOnlyList<int> removal)
        {
            var rng = new Rng(seed);
            var kinds = new List<bool>();
            for (var i = 0; i < recipe.Blocks; i++) kinds.Add(i < recipe.Diagonals);
            var placed = new List<BlockDefinition>();
            var footprints = new List<IReadOnlyList<ShapePoint>>();
            var level = new LevelDefinition { Columns = recipe.Columns, Rows = recipe.Rows };
            var diagonalPlan = new List<ExitDirection> { ExitDirection.UpRight, ExitDirection.DownRight, ExitDirection.DownLeft, ExitDirection.UpLeft };
            var straightPlan = new List<ExitDirection> { ExitDirection.Up, ExitDirection.Right, ExitDirection.Down, ExitDirection.Left };
            rng.Shuffle(diagonalPlan);
            rng.Shuffle(straightPlan);
            var diagonalCount = 0;
            var straightCount = 0;
            foreach (var diagonal in kinds)
            {
                var candidates = new List<BlockDefinition>();
                Candidates(recipe.Columns, recipe.Rows, diagonal, candidates);
                var valid = new List<BlockDefinition>();
                foreach (var candidate in candidates)
                    if (Fits(level, candidate, footprints)) valid.Add(candidate);
                if (valid.Count == 0) { blocks = Array.Empty<BlockDefinition>(); removal = Array.Empty<int>(); return false; }
                var planned = diagonal ? DiagonalPlan(diagonalPlan, diagonalCount++, recipe.Diagonals) : straightPlan[straightCount++ % straightPlan.Count];
                var preferred = new List<BlockDefinition>();
                foreach (var candidate in valid)
                    if (candidate.Direction == planned) preferred.Add(candidate);
                if (preferred.Count > 0) valid = preferred;
                rng.Shuffle(valid);
                var best = valid[0];
                var bestScore = -1f;
                for (var i = 0; i < valid.Count; i++)
                {
                    WoolShape.Footprint(valid[i], out var footprint);
                    var score = Contact(footprint, footprints);
                    if (score > bestScore + 1e-4f) { bestScore = score; best = valid[i]; }
                }
                WoolShape.Footprint(best, out var chosen);
                placed.Add(best);
                footprints.Add(chosen);
            }
            var order = new List<int>();
            for (var i = 0; i < placed.Count; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                var ay = placed[a].Y + placed[a].Height * 0.5f;
                var by = placed[b].Y + placed[b].Height * 0.5f;
                if (ay != by) return by.CompareTo(ay);
                var ax = placed[a].X + placed[a].Width * 0.5f;
                var bx = placed[b].X + placed[b].Width * 0.5f;
                return ax != bx ? ax.CompareTo(bx) : a.CompareTo(b);
            });
            var sorted = new BlockDefinition[placed.Count];
            var index = new int[placed.Count];
            for (var i = 0; i < order.Count; i++) { sorted[i] = placed[order[i]]; index[order[i]] = i; }
            blocks = sorted;
            var sequence = new int[placed.Count];
            for (var i = 0; i < placed.Count; i++) sequence[i] = index[placed.Count - 1 - i];
            removal = sequence;
            return true;
        }

        public static bool Mixed(WoolLevelRecipe recipe, IReadOnlyList<BlockDefinition> blocks)
        {
            var counts = new int[8];
            foreach (var block in blocks) counts[(int)block.Direction]++;
            var straightKinds = 0;
            var diagonalKinds = 0;
            var most = 0;
            for (var d = 0; d < 8; d++)
            {
                if (counts[d] > 0) { if (d < 4) straightKinds++; else diagonalKinds++; }
                most = Math.Max(most, counts[d]);
            }
            return (recipe.Diagonals < 4 || diagonalKinds >= 3) && (recipe.Blocks - recipe.Diagonals < 6 || straightKinds >= 3) && most * 2 <= recipe.Blocks;
        }

        static ExitDirection DiagonalPlan(IReadOnlyList<ExitDirection> plan, int index, int count)
        {
            var first = plan[0];
            var second = WoolShape.Opposite(first);
            var other = plan[1] == second ? plan[2] : plan[1];
            if (index == count - 1 && count > 1) return other;
            return index % 2 == 0 ? first : second;
        }

        static void Candidates(int columns, int rows, bool diagonal, List<BlockDefinition> list)
        {
            if (diagonal)
            {
                for (var cy = 3; cy <= rows * 2 - 3; cy++)
                    for (var cx = 3; cx <= columns * 2 - 3; cx++)
                        for (var d = 4; d < 8; d++)
                            list.Add(new BlockDefinition { X = cx * 0.5f - 1, Y = cy * 0.5f - 0.5f, Width = 2, Height = 1, Direction = (ExitDirection)d });
                return;
            }
            for (var shape = 0; shape < 2; shape++)
            {
                var width = shape == 0 ? 2 : 1;
                var height = shape == 0 ? 1 : 2;
                for (var y = 0; y <= rows - height; y++)
                    for (var x = 0; x <= columns - width; x++)
                        for (var d = 0; d < 4; d++)
                            list.Add(new BlockDefinition { X = x, Y = y, Width = width, Height = height, Direction = (ExitDirection)d });
            }
        }

        static bool Fits(LevelDefinition level, BlockDefinition candidate, List<IReadOnlyList<ShapePoint>> footprints)
        {
            WoolShape.Footprint(candidate, out var footprint);
            if (!WoolShape.Inside(footprint, level.Columns, level.Rows)) return false;
            if (!WoolRouting.LaneFitsTray(level, candidate)) return false;
            WoolShape.Lane(footprint, candidate.Direction, level.Columns, level.Rows, out var lane);
            foreach (var other in footprints)
                if (WoolShape.Overlaps(footprint, other) || WoolShape.Overlaps(lane, other)) return false;
            return true;
        }

        static float Contact(IReadOnlyList<ShapePoint> footprint, List<IReadOnlyList<ShapePoint>> footprints)
        {
            var total = 0f;
            foreach (var other in footprints) total += Touch(footprint, other);
            return total;
        }

        static float Touch(IReadOnlyList<ShapePoint> a, IReadOnlyList<ShapePoint> b)
        {
            var total = 0f;
            for (var i = 0; i < a.Count; i++)
            {
                var a0 = a[i]; var a1 = a[(i + 1) % a.Count];
                var ex = a1.X - a0.X; var ey = a1.Y - a0.Y;
                var length = (float)Math.Sqrt(ex * ex + ey * ey);
                var ux = ex / length; var uy = ey / length;
                for (var j = 0; j < b.Count; j++)
                {
                    var b0 = b[j]; var b1 = b[(j + 1) % b.Count];
                    var fx = b1.X - b0.X; var fy = b1.Y - b0.Y;
                    if (Math.Abs(ux * fy - uy * fx) > 1e-3f * Math.Max(1f, Math.Abs(fx) + Math.Abs(fy))) continue;
                    if (Math.Abs((b0.X - a0.X) * uy - (b0.Y - a0.Y) * ux) > ContactGap) continue;
                    var s0 = (b0.X - a0.X) * ux + (b0.Y - a0.Y) * uy;
                    var s1 = (b1.X - a0.X) * ux + (b1.Y - a0.Y) * uy;
                    var overlap = Math.Min(length, Math.Max(s0, s1)) - Math.Max(0, Math.Min(s0, s1));
                    if (overlap > 0) total += overlap;
                }
            }
            return total;
        }

        static void Build(WoolLevelRecipe recipe, int seed, IReadOnlyList<BlockDefinition> layout, IReadOnlyList<int> removal, float length, out LevelDefinition data)
        {
            var blocks = new BlockDefinition[layout.Count];
            for (var i = 0; i < blocks.Length; i++) blocks[i] = layout[i];
            var rng = new Rng(seed * 7 + 3);
            var palette = new List<int> { 0, 1, 2, 3, 4, 5 };
            rng.Shuffle(palette);
            var colors = new List<int>();
            for (var i = 0; i < blocks.Length; i++) colors.Add(palette[i % recipe.Colors]);
            rng.Shuffle(colors);
            for (var i = 0; i < removal.Count; i++)
            {
                var block = blocks[removal[i]];
                block.Color = colors[i];
                block.Capacity = rng.Range(3, recipe.MaxCapacity + 1);
                blocks[removal[i]] = block;
            }
            var yarn = new List<int>();
            foreach (var index in removal)
                for (var c = 0; c < blocks[index].Capacity; c++) yarn.Add(blocks[index].Color);
            for (var i = 0; i < yarn.Count && recipe.Window > 0; i++)
            {
                var j = Math.Min(yarn.Count - 1, i + rng.Range(0, recipe.Window + 1));
                var swap = yarn[i]; yarn[i] = yarn[j]; yarn[j] = swap;
            }
            data = new LevelDefinition
            {
                Columns = recipe.Columns, Rows = recipe.Rows, Slots = 4, PathLength = length,
                InitialHead = (float)Math.Round((float)(recipe.HeadShare * length), 2), Speed = MinSpeed / 1000f, Spacing = recipe.Spacing,
                Blocks = blocks, Yarn = yarn.ToArray()
            };
        }

        public static uint Fingerprint(LevelDefinition data)
        {
            var hash = 2166136261u;
            void Mix(int value) { unchecked { hash = (hash ^ (uint)value) * 16777619u; } }
            Mix(data.Columns); Mix(data.Rows); Mix(data.Slots);
            Mix(BitConverter.SingleToInt32Bits(data.PathLength)); Mix(BitConverter.SingleToInt32Bits(data.InitialHead));
            Mix(BitConverter.SingleToInt32Bits(data.Speed)); Mix(BitConverter.SingleToInt32Bits(data.Spacing));
            foreach (var block in data.Blocks)
            {
                Mix(BitConverter.SingleToInt32Bits(block.X)); Mix(BitConverter.SingleToInt32Bits(block.Y));
                Mix(block.Width); Mix(block.Height); Mix((int)block.Direction); Mix(block.Color); Mix(block.Capacity);
            }
            foreach (var color in data.Yarn) Mix(color);
            return hash;
        }

        public static bool TryTune(LevelDefinition data, float target, out float ratio)
        {
            data.Speed = MinSpeed / 1000f;
            WoolSolver.Run(data, out var won, out ratio, out _);
            if (!won || ratio > target + Tolerance) return false;
            var low = MinSpeed;
            var lowRatio = ratio;
            var high = MaxSpeed;
            var highRatio = 2f;
            while (high - low > 1)
            {
                var mid = (low + high) / 2;
                data.Speed = mid / 1000f;
                WoolSolver.Run(data, out won, out var value, out _);
                if (!won) value = 2f;
                if (value < target) { low = mid; lowRatio = value; }
                else { high = mid; highRatio = value; }
            }
            var pickHigh = highRatio <= 1f && Math.Abs(highRatio - target) < Math.Abs(lowRatio - target);
            data.Speed = (pickHigh ? high : low) / 1000f;
            ratio = pickHigh ? highRatio : lowRatio;
            return Math.Abs(ratio - target) <= Tolerance;
        }
    }
}
