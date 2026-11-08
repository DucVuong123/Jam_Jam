using System;
using System.Collections.Generic;
using JamJam;

static class FeelChecks
{
    static int passed;
    public static int Assertions => passed;

    static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException(name);
        passed++;
    }

    static bool Near(float a, float b) { return Math.Abs(a - b) < 0.0001f; }

    static void Single(out LevelDefinition level)
    {
        level = new LevelDefinition
        {
            Columns = 4, Rows = 2, Slots = 2, PathLength = 20, InitialHead = 4, Speed = 0.3f,
            Blocks = new[] { new BlockDefinition { X = 0, Y = 0, Width = 2, Height = 1, Direction = ExitDirection.Up, Color = 3, Capacity = 3 } },
            Yarn = new[] { 3, 3, 3 }
        };
    }

    static void Pair(out LevelDefinition level, int slots)
    {
        level = new LevelDefinition
        {
            Columns = 4, Rows = 2, Slots = slots, PathLength = 20, InitialHead = 4, Speed = 0.3f,
            Blocks = new[]
            {
                new BlockDefinition { X = 0, Y = 0, Width = 2, Height = 1, Direction = ExitDirection.Up, Color = 0, Capacity = 2 },
                new BlockDefinition { X = 2, Y = 0, Width = 2, Height = 1, Direction = ExitDirection.Up, Color = 0, Capacity = 2 }
            },
            Yarn = new[] { 0, 0, 0, 0 }
        };
    }

    static void Frame(WoolGame game, WoolFeelTracker tracker, List<WoolFeelEvent> events, List<int> frames, int frame)
    {
        game.Step(0.02f);
        var before = events.Count;
        tracker.Collect(game, events);
        for (var i = before; i < events.Count; i++) frames.Add(frame);
    }

    static int Count(List<WoolFeelEvent> events, WoolFeelKind kind)
    {
        var count = 0;
        foreach (var e in events) if (e.Kind == kind) count++;
        return count;
    }

    static void CheckSequence()
    {
        Single(out var level);
        var game = new WoolGame(level);
        var tracker = new WoolFeelTracker();
        tracker.Reset(game);
        var events = new List<WoolFeelEvent>();
        var frames = new List<int>();
        tracker.Collect(game, events);
        Check(events.Count == 0, "Reset snapshot emits nothing on an idle frame");
        Check(game.Select(0), "Single block launches");
        var arrivedFrame = -1;
        for (var frame = 0; frame < 2000 && game.Status == RoundStatus.Playing; frame++)
        {
            var before = events.Count;
            Frame(game, tracker, events, frames, frame);
            for (var i = before; i < events.Count; i++)
                if (events[i].Kind == WoolFeelKind.SpoolArrived)
                {
                    arrivedFrame = frame;
                    Check(game.Spools[0].Arrival == 0 && game.Blocks[0].Phase == 2 && game.Spools[0].Target >= 0, "SpoolArrived fires in the Step where Arrival reaches exactly 0");
                }
        }
        Check(game.Status == RoundStatus.Won, "Single block level is won");
        var expected = new[] { WoolFeelKind.BlockLaunched, WoolFeelKind.SpoolArrived, WoolFeelKind.YarnConsumed, WoolFeelKind.YarnConsumed, WoolFeelKind.YarnConsumed, WoolFeelKind.SpoolCompleted, WoolFeelKind.Won };
        Check(events.Count == expected.Length, "Seven events for one block of capacity three but got " + events.Count);
        for (var i = 0; i < expected.Length; i++) Check(events[i].Kind == expected[i], "Event " + i + " is " + expected[i]);
        Check(events[0].Index == 0 && events[0].Color == 3 && frames[0] == 0, "BlockLaunched carries block index and color on the first frame");
        Check(events[1].Index == 0 && events[1].Color == 3 && frames[1] == arrivedFrame, "SpoolArrived carries slot index and color");
        Check(events[2].Index == 0 && events[3].Index == 1 && events[4].Index == 2, "YarnConsumed carries unit indices in FIFO order");
        Check(events[2].Color == 3 && events[3].Color == 3 && events[4].Color == 3, "YarnConsumed carries unit color");
        Check(frames[2] > frames[1] && frames[3] > frames[2], "Consumption happens on later frames than arrival");
        Check(events[5].Index == 0 && events[5].Color == 3, "SpoolCompleted carries slot index and color");
        Check(frames[4] == frames[5] && frames[5] == frames[6], "Last consume, completion and Won land in the same Step");
        Check(events[6].Index == -1 && events[6].Color == -1, "Won has no index or color");
        var count = events.Count;
        tracker.Collect(game, events);
        Check(events.Count == count, "Collect without a Step repeats nothing");
        for (var frame = 0; frame < 40; frame++) Frame(game, tracker, events, frames, frame);
        Check(game.Spools[0].Block < 0, "Finished spool releases its slot after Finish");
        Check(events.Count == count, "Releasing the spool after Won emits nothing");
    }

    static void CheckReset()
    {
        Single(out var level);
        var tracker = new WoolFeelTracker();
        var events = new List<WoolFeelEvent>();
        var played = new WoolGame(level);
        tracker.Reset(played);
        played.Select(0);
        for (var i = 0; i < 2000 && played.Status == RoundStatus.Playing; i++) { played.Step(0.02f); tracker.Collect(played, events); }
        var fresh = new WoolGame(level);
        events.Clear();
        tracker.Reset(fresh);
        tracker.Collect(fresh, events);
        Check(events.Count == 0, "Reset onto a fresh round clears the old snapshot");
        var stranger = new WoolGame(level);
        stranger.Select(0);
        tracker.Collect(stranger, events);
        Check(events.Count == 0, "Collect on an untracked round resets instead of replaying its state");
        stranger.Step(0.02f);
        tracker.Collect(stranger, events);
        Check(events.Count == 0, "Launch made before adoption is not replayed");
        tracker.Collect(null, events);
        tracker.Collect(stranger, null);
        tracker.Blocked(1, 4, null);
        Check(events.Count == 0, "Null round or list is ignored");
        tracker.Blocked(1, 4, events);
        Check(events.Count == 1 && events[0].Kind == WoolFeelKind.BlockBlocked && events[0].Index == 1 && events[0].Color == 4, "Blocked adds one BlockBlocked with index and color");
        tracker.Collect(stranger, events);
        Check(events.Count == 1, "Blocked does not leak into Collect");
    }

    static void CheckTray()
    {
        Pair(out var level, 1);
        var game = new WoolGame(level);
        var tracker = new WoolFeelTracker();
        tracker.Reset(game);
        var events = new List<WoolFeelEvent>();
        var frames = new List<int>();
        Check(game.Select(1), "Reserve the only open slot");
        tracker.Collect(game, events);
        Check(events.Count == 2 && events[0].Kind == WoolFeelKind.BlockLaunched && events[0].Index == 1 && events[1].Kind == WoolFeelKind.TrayFull, "Filling the last open slot emits BlockLaunched then TrayFull");
        for (var frame = 0; frame < 16; frame++) Frame(game, tracker, events, frames, frame);
        Check(Count(events, WoolFeelKind.TrayFull) == 1, "TrayFull fires once while the tray stays full");
        var before = events.Count;
        Check(game.UnlockSlot(), "Unlock a slot");
        tracker.Collect(game, events);
        Check(events.Count == before + 1 && events[before].Kind == WoolFeelKind.SlotUnlocked && events[before].Index == 1, "UnlockSlot emits SlotUnlocked with the new slot index");
        Check(game.Select(0), "Second block takes the unlocked slot");
        tracker.Collect(game, events);
        Check(Count(events, WoolFeelKind.TrayFull) == 2, "TrayFull fires again when the unlocked slot fills");
        for (var frame = 0; frame < 2000 && game.Status == RoundStatus.Playing; frame++) Frame(game, tracker, events, frames, frame);
        Check(game.Status == RoundStatus.Won, "Two block round is won");
        Check(Count(events, WoolFeelKind.SpoolArrived) == 2 && Count(events, WoolFeelKind.SpoolCompleted) == 2 && Count(events, WoolFeelKind.YarnConsumed) == 4, "Two arrivals, two completions, four consumes");
        Check(Count(events, WoolFeelKind.Won) == 1 && Count(events, WoolFeelKind.Lost) == 0 && Count(events, WoolFeelKind.Danger) == 0, "One Won and no Lost or Danger");
        Check(events[events.Count - 1].Kind == WoolFeelKind.Won, "Won is the last event");
    }

    static void CheckLoss()
    {
        Pair(out var level, 2);
        level.PathLength = 6;
        var game = new WoolGame(level);
        var tracker = new WoolFeelTracker();
        tracker.Reset(game);
        var events = new List<WoolFeelEvent>();
        var frames = new List<int>();
        var dangerRatio = 0f;
        for (var frame = 0; frame < 2000 && game.Status == RoundStatus.Playing; frame++)
        {
            var before = events.Count;
            Frame(game, tracker, events, frames, frame);
            if (events.Count > before && events[before].Kind == WoolFeelKind.Danger) dangerRatio = game.Head / game.PathLength;
        }
        Check(game.Status == RoundStatus.Lost, "Idle round is lost");
        Check(events.Count == 2 && events[0].Kind == WoolFeelKind.Danger && events[1].Kind == WoolFeelKind.Lost, "Danger then Lost, each exactly once");
        Check(frames[0] < frames[1], "Danger fires on an earlier frame than Lost");
        Check(dangerRatio >= WoolFeelTracker.DangerRatio && dangerRatio < WoolFeelTracker.DangerRatio + 0.01f, "Danger fires as Head crosses 0.8 of the path");
        for (var frame = 0; frame < 20; frame++) Frame(game, tracker, events, frames, frame);
        Check(events.Count == 2, "Nothing repeats after Lost");
    }

    static void CheckReuse()
    {
        Pair(out var level, 1);
        var game = new WoolGame(level);
        var tracker = new WoolFeelTracker();
        tracker.Reset(game);
        var events = new List<WoolFeelEvent>();
        var frames = new List<int>();
        Check(game.Select(1), "First block takes slot 0");
        for (var frame = 0; frame < 2000 && game.Spools[0].Block >= 0; frame++) Frame(game, tracker, events, frames, frame);
        Check(game.Spools[0].Block < 0 && game.Status == RoundStatus.Playing, "First spool finishes and frees slot 0");
        Check(game.Select(0), "Second block reuses slot 0");
        for (var frame = 0; frame < 2000 && game.Status == RoundStatus.Playing; frame++) Frame(game, tracker, events, frames, frame);
        var arrived = 0;
        var completed = 0;
        foreach (var e in events)
        {
            if (e.Kind == WoolFeelKind.SpoolArrived && e.Index == 0) arrived++;
            if (e.Kind == WoolFeelKind.SpoolCompleted && e.Index == 0) completed++;
        }
        Check(game.Status == RoundStatus.Won && arrived == 2 && completed == 2, "Reused slot reports each arrival and completion once");
    }

    static void CheckDangerRearm()
    {
        var level = new LevelDefinition
        {
            Columns = 4, Rows = 2, Slots = 2, PathLength = 10, InitialHead = 7.9f, Speed = 0.3f, Spacing = 2,
            Blocks = new[]
            {
                new BlockDefinition { X = 0, Y = 0, Width = 2, Height = 1, Direction = ExitDirection.Up, Color = 0, Capacity = 2 },
                new BlockDefinition { X = 2, Y = 0, Width = 2, Height = 1, Direction = ExitDirection.Up, Color = 1, Capacity = 1 }
            },
            Yarn = new[] { 0, 0, 1 }
        };
        var game = new WoolGame(level);
        var tracker = new WoolFeelTracker();
        tracker.Reset(game);
        var events = new List<WoolFeelEvent>();
        var frames = new List<int>();
        Check(game.Select(0), "Pull-back block launches");
        var low = 1f;
        for (var frame = 0; frame < 4000 && Count(events, WoolFeelKind.Danger) < 2 && game.Status == RoundStatus.Playing; frame++)
        {
            Frame(game, tracker, events, frames, frame);
            if (Count(events, WoolFeelKind.Danger) == 1) low = Math.Min(low, game.Head / game.PathLength);
        }
        Check(low < WoolFeelTracker.DangerRearm, "Consumption pulls the head back under 0.75 but lowest was " + low);
        Check(Count(events, WoolFeelKind.Danger) == 2 && game.Status == RoundStatus.Playing, "Danger re-arms after dropping below 0.75 and fires again on the next crossing");
    }

    static void CheckDangerBand()
    {
        var level = new LevelDefinition
        {
            Columns = 4, Rows = 2, Slots = 2, PathLength = 10, InitialHead = 7.95f, Speed = 0.3f, Spacing = 0.667f,
            Blocks = new[]
            {
                new BlockDefinition { X = 0, Y = 0, Width = 2, Height = 1, Direction = ExitDirection.Up, Color = 0, Capacity = 2 },
                new BlockDefinition { X = 2, Y = 0, Width = 2, Height = 1, Direction = ExitDirection.Up, Color = 1, Capacity = 1 }
            },
            Yarn = new[] { 0, 0, 1 }
        };
        var game = new WoolGame(level);
        var tracker = new WoolFeelTracker();
        tracker.Reset(game);
        var events = new List<WoolFeelEvent>();
        var frames = new List<int>();
        Check(game.Select(0), "Band block launches");
        var low = 1f;
        var dipped = false;
        var climbed = false;
        for (var frame = 0; frame < 4000 && !climbed && game.Status == RoundStatus.Playing; frame++)
        {
            Frame(game, tracker, events, frames, frame);
            var ratio = game.Head / game.PathLength;
            if (Count(events, WoolFeelKind.Danger) == 1 && ratio < WoolFeelTracker.DangerRatio) { dipped = true; low = Math.Min(low, ratio); }
            if (dipped && ratio > WoolFeelTracker.DangerRatio + 0.01f) climbed = true;
        }
        Check(dipped && low >= WoolFeelTracker.DangerRearm && low < WoolFeelTracker.DangerRatio, "Head dips into the 0.75 to 0.8 band but lowest was " + low);
        Check(climbed && game.Status == RoundStatus.Playing, "Head climbs back above 0.8 while playing");
        Check(Count(events, WoolFeelKind.Danger) == 1, "Danger fires once while the head stays inside the hysteresis band");
    }

    static void CheckEase()
    {
        Check(WoolEase.OutBack(0) == 0 && WoolEase.OutBack(1) == 1, "OutBack spans 0 to 1");
        Check(WoolEase.Punch(0) == 0 && WoolEase.Punch(1) == 0, "Punch starts and ends at 0");
        Check(WoolEase.Arc(0) == 0 && WoolEase.Arc(1) == 0 && Near(WoolEase.Arc(0.5f), 1), "Arc is 0, 1, 0");
        Check(WoolEase.OutBack(-1) == 0 && WoolEase.OutBack(2) == 1 && WoolEase.Punch(-3) == 0 && WoolEase.Punch(4) == 0 && WoolEase.Arc(5) == 0, "Inputs outside 0..1 are clamped");
        Check(WoolEase.OutBack(float.NaN) == 0 && WoolEase.Punch(float.NaN) == 0 && WoolEase.Arc(float.NaN) == 0, "NaN behaves like 0");
        var backPeak = 0f;
        var punchPeak = 0f;
        var punchLow = 0f;
        for (var i = 0; i <= 1000; i++)
        {
            var t = i / 1000f;
            backPeak = Math.Max(backPeak, WoolEase.OutBack(t));
            punchPeak = Math.Max(punchPeak, WoolEase.Punch(t));
            punchLow = Math.Min(punchLow, WoolEase.Punch(t));
        }
        Check(backPeak > 1.05f && backPeak < 1.15f, "OutBack overshoots about 10% but was " + backPeak);
        Check(punchPeak > 0.95f && punchPeak < 1.05f, "Punch peaks near 1 but was " + punchPeak);
        Check(punchLow < 0 && punchLow > -0.5f, "Punch bounces below 0 but stays above -0.5, was " + punchLow);
    }

    static void CheckShake()
    {
        var shake = new WoolShake();
        shake.Step(0.016f, out var x, out var y, out var roll);
        Check(x == 0 && y == 0 && roll == 0 && shake.Trauma == 0, "No trauma means no offset");
        shake.Add(5);
        Check(shake.Trauma == 1, "Trauma clamps to 1");
        shake.Add(-1);
        shake.Add(float.NaN);
        Check(shake.Trauma == 1, "Negative or NaN trauma is ignored");
        var twin = new WoolShake();
        twin.Step(0.016f, out _, out _, out _);
        twin.Add(1);
        var moved = false;
        var bounded = true;
        var same = true;
        for (var i = 0; i < 40; i++)
        {
            shake.Step(0.016f, out x, out y, out roll);
            twin.Step(0.016f, out var tx, out var ty, out var troll);
            if (Math.Abs(x) > 0.01f || Math.Abs(y) > 0.01f) moved = true;
            if (Math.Abs(x) > WoolShake.MaxOffset || Math.Abs(y) > WoolShake.MaxOffset || Math.Abs(roll) > WoolShake.MaxRoll) bounded = false;
            if (x != tx || y != ty || roll != troll) same = false;
        }
        Check(moved, "Full trauma moves the camera");
        Check(bounded, "Offset stays within 0.35 and roll within 2 degrees");
        Check(same, "Same deltas give the same offsets");
        Check(shake.Trauma == 0 && x == 0 && y == 0 && roll == 0, "Trauma 1 decays to exactly 0 within 0.64 s");
        shake.Add(0.5f);
        var trauma = shake.Trauma;
        shake.Step(float.NaN, out x, out y, out roll);
        Check(shake.Trauma == trauma && !float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(roll), "NaN delta freezes trauma and yields finite offsets");
        shake.Step(-1, out x, out y, out roll);
        Check(shake.Trauma == trauma, "Negative delta freezes trauma");
        shake.Reset();
        shake.Step(0.016f, out x, out y, out roll);
        Check(shake.Trauma == 0 && x == 0 && y == 0 && roll == 0, "Reset clears trauma");
    }

    static void CheckHaptics()
    {
        foreach (WoolFeelKind kind in Enum.GetValues(typeof(WoolFeelKind)))
        {
            WoolHapticPattern.Get(kind, out var timings, out var amplitudes);
            Check(timings != null && amplitudes != null && timings.Length == amplitudes.Length, kind + " timings and amplitudes match");
            var total = 0L;
            var valid = true;
            foreach (var timing in timings) { if (timing < 0) valid = false; total += timing; }
            foreach (var amplitude in amplitudes) if (amplitude < 0 || amplitude > 255) valid = false;
            Check(valid, kind + " timings are non-negative and amplitudes within 0..255");
            if (kind == WoolFeelKind.YarnConsumed) Check(timings.Length == 0 && WoolHapticPattern.Strength(kind) == 0, "YarnConsumed does not vibrate");
            else Check(total > 0 && WoolHapticPattern.Strength(kind) > 0, kind + " vibrates");
        }
        WoolHapticPattern.Get(WoolFeelKind.Won, out var won, out var wonAmplitudes);
        Check(string.Join(",", won) == "30,60,30,60,60" && string.Join(",", wonAmplitudes) == "160,0,200,0,255", "Won pattern from spec");
        WoolHapticPattern.Get(WoolFeelKind.BlockBlocked, out var warning, out var warningAmplitudes);
        Check(string.Join(",", warning) == "0,18,40,18" && string.Join(",", warningAmplitudes) == "0,140,0,140", "Warning pattern from spec");
        Check(WoolHapticPattern.Strength(WoolFeelKind.BlockLaunched) == 90 && WoolHapticPattern.Strength(WoolFeelKind.SpoolArrived) == 110 &&
            WoolHapticPattern.Strength(WoolFeelKind.SpoolCompleted) == 170 && WoolHapticPattern.Strength(WoolFeelKind.Lost) == 255, "Peak amplitudes from spec");
        WoolHapticPattern.Get((WoolFeelKind)99, out var unknown, out var unknownAmplitudes);
        Check(unknown.Length == 0 && unknownAmplitudes.Length == 0, "Unknown kind does not vibrate");
    }

    public static void Run()
    {
        CheckSequence();
        CheckReset();
        CheckTray();
        CheckLoss();
        CheckReuse();
        CheckDangerRearm();
        CheckDangerBand();
        CheckEase();
        CheckShake();
        CheckHaptics();
    }
}
