using System;
using System.Collections.Generic;

namespace JamJam
{
    public sealed class WoolFeelTracker
    {
        public const float DangerRatio = 0.8f;
        public const float DangerRearm = 0.75f;
        WoolGame source;
        int[] phases = Array.Empty<int>();
        int[] spoolBlocks = Array.Empty<int>();
        float[] arrivals = Array.Empty<float>();
        int[] remaining = Array.Empty<int>();
        bool[] consumed = Array.Empty<bool>();
        int openSlots;
        bool trayFull;
        bool dangerArmed;
        RoundStatus status;

        public void Reset(WoolGame game)
        {
            source = game;
            if (game == null) return;
            var blocks = game.Blocks;
            var spools = game.Spools;
            var yarn = game.Yarn;
            phases = new int[blocks.Count];
            for (var i = 0; i < blocks.Count; i++) phases[i] = blocks[i].Phase;
            spoolBlocks = new int[spools.Count];
            arrivals = new float[spools.Count];
            remaining = new int[spools.Count];
            for (var i = 0; i < spools.Count; i++)
            {
                spoolBlocks[i] = spools[i].Block;
                arrivals[i] = spools[i].Arrival;
                remaining[i] = spools[i].Remaining;
            }
            consumed = new bool[yarn.Count];
            for (var i = 0; i < yarn.Count; i++) consumed[i] = yarn[i].Consumed;
            openSlots = game.OpenSlots;
            trayFull = game.Status == RoundStatus.Playing && IsTrayFull(game);
            dangerArmed = game.Head / game.PathLength < DangerRatio;
            status = game.Status;
        }

        public void Blocked(int index, int color, List<WoolFeelEvent> events)
        {
            if (events != null) events.Add(new WoolFeelEvent(WoolFeelKind.BlockBlocked, index, color));
        }

        public void Collect(WoolGame game, List<WoolFeelEvent> events)
        {
            if (game == null || events == null) return;
            if (!ReferenceEquals(game, source)) { Reset(game); return; }
            var blocks = game.Blocks;
            for (var i = 0; i < blocks.Count; i++)
            {
                var phase = blocks[i].Phase;
                if (phases[i] == 0 && phase != 0) events.Add(new WoolFeelEvent(WoolFeelKind.BlockLaunched, i, blocks[i].Definition.Color));
                phases[i] = phase;
            }
            for (var i = openSlots; i < game.OpenSlots; i++) events.Add(new WoolFeelEvent(WoolFeelKind.SlotUnlocked, i, -1));
            openSlots = game.OpenSlots;
            var spools = game.Spools;
            for (var i = 0; i < spools.Count; i++)
            {
                var spool = spools[i];
                if (spool.Block >= 0 && spool.Arrival <= 0 && (spool.Block != spoolBlocks[i] || arrivals[i] > 0))
                    events.Add(new WoolFeelEvent(WoolFeelKind.SpoolArrived, i, spool.Color));
            }
            var yarn = game.Yarn;
            for (var i = 0; i < yarn.Count; i++)
            {
                if (!consumed[i] && yarn[i].Consumed) events.Add(new WoolFeelEvent(WoolFeelKind.YarnConsumed, i, yarn[i].Color));
                consumed[i] = yarn[i].Consumed;
            }
            for (var i = 0; i < spools.Count; i++)
            {
                var spool = spools[i];
                if (spool.Block >= 0 && spool.Remaining == 0 && (spool.Block != spoolBlocks[i] || remaining[i] > 0))
                    events.Add(new WoolFeelEvent(WoolFeelKind.SpoolCompleted, i, spool.Color));
                spoolBlocks[i] = spool.Block;
                arrivals[i] = spool.Arrival;
                remaining[i] = spool.Remaining;
            }
            var full = game.Status == RoundStatus.Playing && IsTrayFull(game);
            if (full && !trayFull) events.Add(new WoolFeelEvent(WoolFeelKind.TrayFull, -1, -1));
            trayFull = full;
            var ratio = game.Head / game.PathLength;
            if (dangerArmed && ratio >= DangerRatio && status == RoundStatus.Playing)
            {
                dangerArmed = false;
                events.Add(new WoolFeelEvent(WoolFeelKind.Danger, -1, -1));
            }
            else if (!dangerArmed && ratio < DangerRearm) dangerArmed = true;
            if (status == RoundStatus.Playing && game.Status != RoundStatus.Playing)
                events.Add(new WoolFeelEvent(game.Status == RoundStatus.Won ? WoolFeelKind.Won : WoolFeelKind.Lost, -1, -1));
            status = game.Status;
        }

        static bool IsTrayFull(WoolGame game)
        {
            var spools = game.Spools;
            for (var i = 0; i < game.OpenSlots; i++)
                if (spools[i].Block < 0) return false;
            return true;
        }
    }
}
