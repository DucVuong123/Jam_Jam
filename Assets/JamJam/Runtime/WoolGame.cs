using System;
using System.Collections.Generic;

namespace JamJam
{
    public enum ExitDirection { Up, Right, Down, Left, UpRight, DownRight, DownLeft, UpLeft }
    public enum RoundStatus { Playing, Won, Lost }

    [Serializable]
    public struct BlockDefinition
    {
        public float X;
        public float Y;
        public int Width;
        public int Height;
        public ExitDirection Direction;
        public int Color;
        public int Capacity;
    }

    [Serializable]
    public sealed class LevelDefinition
    {
        public int Columns = 6;
        public int Rows = 5;
        public int Slots = 4;
        public float PathLength = 30f;
        public float InitialHead = 9f;
        public float Speed = 0.24f;
        public float Spacing = 0.18f;
        public BlockDefinition[] Blocks = Array.Empty<BlockDefinition>();
        public int[] Yarn = Array.Empty<int>();
    }

    public sealed class BlockState
    {
        public BlockDefinition Definition;
        public int Phase;
        public float Travel;
        public float Duration;
        public float ExitDistance;
        public IReadOnlyList<RoutePoint> Route = Array.Empty<RoutePoint>();
        public int Slot = -1;
        public int Order;
        public bool Yielding;
    }

    public sealed class SpoolState
    {
        public int Block = -1;
        public int Color;
        public int Remaining;
        public int Capacity;
        public int Target = -1;
        public float Arrival;
        public float Pull;
        public float Finish;
    }

    public sealed class YarnState
    {
        public int Color;
        public bool Consumed;
        public int ReservedBy = -1;
        public float Distance;
    }

    public sealed class WoolGame
    {
        public const float PullDuration = 0.16f;
        public const float FinishDuration = 0.26f;
        readonly LevelDefinition level;
        readonly List<BlockState> blocks = new List<BlockState>();
        readonly List<YarnState> yarn = new List<YarnState>();
        readonly List<SpoolState> spools = new List<SpoolState>();
        readonly List<int> spoolOrder = new List<int>();
        int selectCounter;
        public IReadOnlyList<BlockState> Blocks => blocks;
        public IReadOnlyList<YarnState> Yarn => yarn;
        public IReadOnlyList<SpoolState> Spools => spools;
        public RoundStatus Status { get; private set; }
        public bool Paused { get; private set; }
        public int Remaining { get; private set; }
        public int Total => yarn.Count;
        public float Head { get; private set; }
        public float Elapsed { get; private set; }
        public float PathLength => level.PathLength;
        public int OpenSlots { get; private set; }

        public WoolGame(LevelDefinition definition)
        {
            var errors = Validate(definition);
            if (errors.Count > 0) throw new ArgumentException(string.Join("; ", errors));
            level = definition;
            foreach (var block in level.Blocks) blocks.Add(new BlockState { Definition = block });
            foreach (var color in level.Yarn) yarn.Add(new YarnState { Color = color });
            for (var i = 0; i < level.Slots + 2; i++) spools.Add(new SpoolState());
            OpenSlots = level.Slots;
            Remaining = yarn.Count;
            Head = level.InitialHead;
            RefreshDistances();
        }

        public static IReadOnlyList<string> Validate(LevelDefinition data)
        {
            var errors = new List<string>();
            if (data == null) { errors.Add("Level is missing"); return errors; }
            if (data.Columns < 1 || data.Rows < 1 || data.Slots < 1 || data.Slots > 6 ||
                data.PathLength <= 0 || data.InitialHead < 0 || data.InitialHead >= data.PathLength ||
                data.Speed <= 0 || data.Spacing <= 0 || float.IsNaN(data.Speed) || float.IsInfinity(data.Speed))
                errors.Add("Invalid level dimensions or timing");
            if (data.Blocks == null || data.Yarn == null || data.Blocks.Length == 0 || data.Yarn.Length == 0)
            { errors.Add("Level is empty"); return errors; }
            var footprints = new List<IReadOnlyList<ShapePoint>>();
            var balance = new int[6];
            foreach (var b in data.Blocks)
            {
                if (b.Width < 1 || b.Height < 1 || float.IsNaN(b.X) || float.IsNaN(b.Y) || float.IsInfinity(b.X) || float.IsInfinity(b.Y) ||
                    b.Color < 0 || b.Color >= 6 || b.Capacity < 1 || !WoolShape.IsValid(b.Direction))
                { errors.Add("Invalid block"); continue; }
                WoolShape.Footprint(b, out var footprint);
                if (!WoolShape.Inside(footprint, data.Columns, data.Rows)) { errors.Add("Invalid block"); continue; }
                if (!WoolRouting.LaneFitsTray(data, b) && !errors.Contains("Board too tall for the tray lane")) errors.Add("Board too tall for the tray lane");
                balance[b.Color] += b.Capacity;
                foreach (var other in footprints)
                    if (WoolShape.Overlaps(footprint, other)) errors.Add("Overlapping blocks");
                footprints.Add(footprint);
            }
            foreach (var color in data.Yarn)
                if (color < 0 || color >= 6) errors.Add("Invalid yarn color"); else balance[color]--;
            foreach (var count in balance) if (count != 0) errors.Add("Yarn and spool capacities do not match");
            return errors;
        }

        public bool CanSelect(int index)
        {
            return TryPrepareSelection(index, out _, out _, out _);
        }

        bool TryPrepareSelection(int index, out int slot, out IReadOnlyList<RoutePoint> route, out float exitDistance)
        {
            slot = -1;
            route = Array.Empty<RoutePoint>();
            exitDistance = 0;
            if (Paused || Status != RoundStatus.Playing || index < 0 || index >= blocks.Count || blocks[index].Phase != 0)
                return false;
            for (var i = 0; i < OpenSlots; i++)
                if (spools[i].Block < 0) { slot = i; break; }
            return slot >= 0 && HasClearArrowLane(index) && WoolRouting.TryFind(level, blocks, index, slot, out route, out exitDistance);
        }

        bool HasClearArrowLane(int index)
        {
            return WoolShape.LaneClear(level, blocks, index, true);
        }

        public bool Select(int index)
        {
            if (!TryPrepareSelection(index, out var slot, out var route, out var exitDistance)) return false;
            var block = blocks[index];
            block.Phase = 1;
            block.Order = ++selectCounter;
            block.Yielding = false;
            block.Slot = slot;
            block.Travel = 0;
            block.Route = route;
            block.Duration = WoolRouting.Length(route) / WoolRouting.Speed;
            block.ExitDistance = exitDistance;
            var spool = spools[slot];
            spoolOrder.Remove(slot);
            spoolOrder.Add(slot);
            spool.Block = index;
            spool.Color = block.Definition.Color;
            spool.Capacity = spool.Remaining = block.Definition.Capacity;
            spool.Target = -1;
            spool.Arrival = block.Duration;
            spool.Pull = spool.Finish = 0;
            return true;
        }
        public void SetPaused(bool paused) { Paused = paused; }

        public bool UnlockSlot()
        {
            if (Paused || Status != RoundStatus.Playing || OpenSlots >= spools.Count) return false;
            OpenSlots++;
            return true;
        }

        public bool HasVisibleColor(int color)
        {
            foreach (var unit in yarn)
                if (!unit.Consumed && unit.ReservedBy < 0 && unit.Distance >= 0 && unit.Distance < level.PathLength && unit.Color == color)
                    return true;
            return false;
        }

        public void Step(float delta)
        {
            if (Paused || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            if (Status == RoundStatus.Lost) return;
            delta = Math.Min(delta, 0.1f);
            if (Status == RoundStatus.Playing)
            {
                Elapsed += delta;
                Head = (float)(Head + (float)(level.Speed * delta));
            }
            var lead = int.MaxValue;
            foreach (var block in blocks)
                if (block.Phase == 1 && block.Order < lead) lead = block.Order;
            foreach (var block in blocks)
                block.Yielding = block.Phase == 1 && block.Order != lead && ShouldYield(block, delta);
            foreach (var block in blocks)
            {
                if (block.Phase != 1 || block.Yielding) continue;
                block.Travel = Math.Min(block.Duration, (float)(block.Travel + delta));
                spools[block.Slot].Arrival = Math.Max(0, (float)(block.Duration - block.Travel));
                if (block.Travel >= block.Duration) block.Phase = 2;
            }
            foreach (var i in spoolOrder)
            {
                var spool = spools[i];
                if (spool.Block < 0) continue;
                if (spool.Arrival > 0) continue;
                if (spool.Remaining == 0)
                {
                    spool.Finish -= delta;
                    if (spool.Finish <= 0) spool.Block = -1;
                    continue;
                }
                if (Status != RoundStatus.Playing) continue;
                if (spool.Target >= 0)
                {
                    spool.Pull -= delta;
                    if (spool.Pull > 0) continue;
                    var unit = yarn[spool.Target];
                    unit.Consumed = true;
                    unit.ReservedBy = -1;
                    spool.Target = -1;
                    spool.Remaining--;
                    Remaining--;
                    Head = Math.Max(0.25f, (float)(Head - (float)(level.Spacing * 0.45f)));
                    if (spool.Remaining == 0) spool.Finish = FinishDuration;
                    RefreshDistances();
                }
                if (spool.Remaining > 0 && spool.Target < 0)
                {
                    for (var j = 0; j < yarn.Count; j++)
                    {
                        var unit = yarn[j];
                        if (unit.Consumed || unit.ReservedBy >= 0 || unit.Color != spool.Color || unit.Distance < 0 || unit.Distance >= level.PathLength) continue;
                        spool.Target = j;
                        spool.Pull = PullDuration;
                        unit.ReservedBy = i;
                        break;
                    }
                }
            }
            RefreshDistances();
            if (Remaining == 0) Status = RoundStatus.Won;
            else if (Head >= level.PathLength) Status = RoundStatus.Lost;
        }

        bool OffBoard(BlockState block)
        {
            return block.Phase == 1 && (float)(block.Travel * WoolRouting.Speed) > block.ExitDistance;
        }

        bool SeesAhead(BlockState block, BlockState other, float delta)
        {
            WoolRouting.Evaluate(block.Route, block.Travel * WoolRouting.Speed, out var position, out var heading);
            WoolRouting.Evaluate(block.Route, Math.Min(block.Duration, block.Travel + delta) * WoolRouting.Speed, out var next, out _);
            WoolRouting.Evaluate(other.Route, other.Travel * WoolRouting.Speed, out var otherPosition, out var otherHeading);
            if (heading.X * otherHeading.X + heading.Z * otherHeading.Z < 0) return false;
            if ((otherPosition.X - position.X) * heading.X + (otherPosition.Z - position.Z) * heading.Z <= 0) return false;
            var reach = WoolRouting.MaxHalf(block.Definition) + WoolRouting.MaxHalf(other.Definition);
            return Math.Abs(next.X - otherPosition.X) < reach && Math.Abs(next.Z - otherPosition.Z) < reach;
        }

        bool ShouldYield(BlockState block, float delta)
        {
            foreach (var other in blocks)
            {
                if (other == block || !OffBoard(other) || !SeesAhead(block, other, delta)) continue;
                if (SeesAhead(other, block, delta) && block.Order < other.Order) continue;
                return true;
            }
            return false;
        }

        void RefreshDistances()
        {
            var rank = 0;
            foreach (var unit in yarn)
            {
                if (unit.Consumed) continue;
                unit.Distance = (float)(Head - (float)((++rank) * level.Spacing));
            }
        }
    }
}
