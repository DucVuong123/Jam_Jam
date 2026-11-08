# Game feel gameplay Wool Jam

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** mỗi hành động trong `WoolJam.unity` có phản hồi rõ (pop/nảy, VFX đúng màu len, rung camera, haptic Android) mà không đổi luật chơi, level, route, FIFO hay thời lượng logic.

**Kiến trúc:** lớp logic thuần (không UnityEngine) gồm `WoolFeelTracker` dò sự kiện bằng cách so snapshot `WoolGame` giữa hai lần `Collect`, `WoolEase`, `WoolShake` (trauma), `WoolHapticPattern`; harness .NET kiểm bằng level `WoolGame` thật. MonoBehaviour `WoolFeel` do `WoolScene.LoadLevel` tự thêm, được gọi trong `Tick` có sẵn (R3 Interval), phát VFX bằng `ParticleSystem.Emit` trên hai prefab `FeelPuff`/`FeelSparkle`, rung camera, gọi `IWoolHaptics`; `WoolScene.RenderFrame` nhân các hệ số juice vào scale/position hiện có.

**Tech Stack:** Unity 6000.3.19f1, URP 17.3 (shader `Universal Render Pipeline/Particles/Unlit`), C# 9 (LangVersion 9.0), Input System, R3 1.3.1, harness .NET 10 (`Tools/WoolJamChecks`), Android JNI (`AndroidJavaObject`, `VibrationEffect`).

**Spec:** `docs/superpowers/specs/2026-11-08-wool-game-feel-design.md`

## Ràng buộc chung

- **Không âm thanh.** Không sửa `WoolGame.cs`, `WoolRouting.cs`, level asset (`Assets/JamJam/Generated/Levels/*.asset`).
- Sự kiện suy ra bằng so trạng thái `WoolGame` giữa hai frame; không thêm callback vào `WoolGame`.
- File logic thuần (`WoolFeelEvent`, `WoolFeelTracker`, `WoolEase`, `WoolShake`, `WoolHapticPattern`) không `using UnityEngine`, được harness compile trực tiếp.
- Không comment trong code (`//`, `///`, `/* */`, XML doc). Method trả reference dùng interface/abstract; factory `void Create(..., out IThing x)`.
- Không tạo timer lặp mới: mọi thứ chạy trong `WoolScene.Tick`/`RenderFrame` (đã nằm trên `Observable.Interval`).
- C# 9: không file-scoped namespace, không global using, không record struct.
- Giữ `.meta`/GUID; asset sinh lại tại chỗ. KHÔNG chạy `WoolSceneBuilder.Build` hay `WoolEditorChecks.RunBatch`. Sinh asset bằng menu `JamJam/Rebuild Art` = `JamJam.Editor.WoolArtBuilder.Rebuild()` (yêu cầu thoát Play Mode và lưu mọi scene đang mở).
- Không `git add`/`git commit`; mỗi task kết thúc bằng bước kiểm tra rồi "Dừng, báo user".
- Số dòng trong header **Trước** là của file hiện tại (2026-11-08) trước khi bắt đầu plan; khi làm task sau, tìm theo nội dung Trước.
- Lệnh Unity: `unity recompile`; `unity command eval --caller plugin --skill unity-cli --no-banner '<C# statements; return value;>'`. Vào/thoát Play Mode, chụp Game view 1080×1920, đọc lỗi console: dùng lệnh Unity CLI thật cho từng việc (tìm bằng `unity command --caller plugin --skill unity-cli --no-banner --no-pager`). Plan ghi chúng là **[PLAY START]**, **[PLAY STOP]**, **[CAPTURE 1080×1920 → path]**, **[CONSOLE ERRORS]**.
- Harness: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`. Dòng PASS cuối: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; 104 feel assertions`.
- Biến shell (Git Bash): `SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"`.

## Quyết định (spec để ngỏ hoặc phải lệch nhẹ)

| Chủ đề | Quyết định |
|---|---|
| Thời điểm thêm `WoolFeel` | Trong `LoadLevel` (lần đầu được gọi từ `Start`), tạo lười nếu chưa có — tương đương "thêm lúc Start" nhưng an toàn khi check gọi `LoadLevel`. |
| Tên method | `WoolFeel.Restart(WoolGame)` thay cho `Reset(WoolGame)`: `Reset` là message của MonoBehaviour, Unity báo lỗi nếu có tham số. `WoolFeelTracker.Reset` giữ đúng tên spec (class thường). |
| Thứ tự sự kiện trong một `Collect` | BlockLaunched → SlotUnlocked → SpoolArrived → YarnConsumed → SpoolCompleted → TrayFull → Danger → Won/Lost. |
| Nhận diện spool | So cả `Block` của slot: slot được tái dùng sau Finish (`Block` -1 rồi block mới) vẫn phát Arrived/Completed đúng một lần. |
| TrayFull, Danger | Chỉ khi trạng thái trước là `Playing`. Danger có trễ: phát khi Head/PathLength ≥ 0.8, chỉ phát lại sau khi tụt dưới 0.75 (consume kéo đầu rồng lùi). |
| Haptic | Mỗi frame phát tối đa một mẫu: sự kiện có amplitude đỉnh lớn nhất (bằng nhau lấy cái đầu). `Handheld.Vibrate()` là nhánh dự phòng khi không lấy được service `vibrator` (để Unity tự thêm quyền `VIBRATE`). |
| SpoolCompleted | Punch 1→1.25 nhân với co `Finish` có sẵn (không thể hoãn `Finish` vì không sửa `WoolGame`). |
| TrayFull chớp | Lerp màu pad tới `#FF5A5A` tối đa 35% (`Arc`, 0.5s) bằng MPB, trả MPB null khi xong. |
| Won sparkle | 40 hạt, màu xoay vòng palette len `Yarn[i % 6]`. Lost puff màu `#FF6A3D`. |
| Hạt | Puff: tốc 1.6, cỡ 0.32, sống 0.45s, drag 3; Sparkle: tốc 2.6, cỡ 0.26, sống 0.7s, gravity 0.5, drag 1.2; Won: tốc 4, cỡ 0.34, sống 1.1s; Lost: tốc 2.4, cỡ 0.5, sống 0.7s. Hướng bắn theo góc vàng (tất định), không dùng `Random`. |
| Easing | `OutBack` chuẩn (overshoot 1.70158); `Punch(t)=1.4·sin(3πt)·(1−t)²` (đỉnh ≈1.0, nảy âm ≈−0.38); `Arc(t)=sin(πt)`; t ngoài [0,1] kẹp, NaN = 0, `Punch`/`Arc` trả đúng 0 tại biên. |
| Rung camera | Nhiễu value-noise băm sin 1D (3 kênh seed 1/2/3), `time` double, tần số 18Hz; offset theo `cameraRotation * (x, y, 0)`. |
| Check editor | Đặt trong partial `WoolArtChecks` (`Feel()`, `PlayFeel()`, `PlayFinish(bool)`) để dùng lại helper `Expect`/`TextureAt`/`Click`, thay cho class riêng `WoolFeelChecks`. |
| API phụ cho check | `WoolFeel.Trauma`, `PuffCount`, `SparkleCount`; `WoolScene.BlockAt(int)`, `UnitAt(int)`, `KittenAt` (index ngoài phạm vi trả vị trí mèo). |
| Ảnh | `docs/feel-launch.png`, `docs/feel-complete.png`, `docs/feel-won.png` (1080×1920), chụp với `Time.timeScale` chậm. |

## Review Focus

- Slot được tái dùng sau khi spool cũ Finish (`Block` → -1 rồi block mới cùng slot) → Arrived/Completed mỗi lần đúng một lần, không sót không lặp. Test `CheckReuse` (Task 1).
- Đầu rồng dao động quanh 80% vì consume kéo lùi `Head` → Danger không spam; phát lại sau khi tụt dưới 75% và vượt lại. Test `CheckDangerRearm` (Task 1).
- Replay/LoadLevel giữa lúc rung hoặc khi tracker gặp `WoolGame` mới → không phát lại trạng thái cũ, camera về đúng gốc ngay. Test `CheckReset` (Task 1) và đoạn Replay trong `PlayFeel()` (Task 4).
- Delta xấu (NaN, âm, 0 khi Pause) → không NaN trên camera, trauma đóng băng. Test trong `CheckShake` (Task 1).
- Tắt rung trong Settings hoặc chạy Editor → không gọi JNI, factory trả bản rỗng; nhánh Android compile được. `WoolArtChecks.Feel()` (Task 3) và build scratch `UNITY_ANDROID` (Task 2).

## Bản đồ file

| File | Việc |
|---|---|
| `Assets/JamJam/Runtime/WoolFeelEvent.cs` | Tạo — enum + struct sự kiện (T1) |
| `Assets/JamJam/Runtime/WoolFeelTracker.cs` | Tạo — dò sự kiện (T1) |
| `Assets/JamJam/Runtime/WoolEase.cs` | Tạo — OutBack/Punch/Arc (T1) |
| `Assets/JamJam/Runtime/WoolShake.cs` | Tạo — trauma shake (T1) |
| `Assets/JamJam/Runtime/WoolHapticPattern.cs` | Tạo — mẫu rung (T1) |
| `Tools/WoolJamChecks/FeelChecks.cs` | Tạo — test harness (T1) |
| `Tools/WoolJamChecks/Program.cs` | Sửa — gọi `FeelChecks.Run()`, dòng PASS (T1) |
| `Tools/WoolJamChecks/WoolJamChecks.csproj` | Sửa — compile file mới (T1) |
| `Assets/JamJam/Runtime/IWoolHaptics.cs` | Tạo (T2) |
| `Assets/JamJam/Runtime/WoolHaptics.cs` | Tạo — factory, bản rỗng, bản Android (T2) |
| `Assets/JamJam/Runtime/WoolArt.cs` | Sửa — `Puff`, `Sparkle` (T3) |
| `Assets/JamJam/Editor/WoolAssetsBuilder.cs` | Sửa — texture/material/prefab hạt (T3) |
| `Assets/JamJam/Editor/WoolArtChecks.Feel.cs` | Tạo — `Feel()` (T3) |
| `Assets/JamJam/Runtime/WoolFeel.cs` | Tạo — MonoBehaviour phản hồi (T4) |
| `Assets/JamJam/Runtime/WoolScene.cs` | Sửa — tích hợp (T4) |
| `Assets/JamJam/Editor/WoolArtChecks.PlayFeel.cs` | Tạo — `PlayFeel()`, `PlayFinish(bool)` (T4) |
| Asset sinh mới | `Generated/Textures/SoftDot.png`, `Star.png`; `Generated/Materials/FeelPuff.mat`, `FeelSparkle.mat`; `Generated/Prefabs/FeelPuff.prefab`, `FeelSparkle.prefab` (T3) |
| Ảnh | `docs/feel-launch.png`, `docs/feel-complete.png`, `docs/feel-won.png` (T5) |

Đã xác minh mọi file "Tạo" chưa tồn tại (2026-11-08). Toàn bộ code mới trong plan đã được compile thử ngoài project: logic thuần + harness với `LangVersion 9.0` (chạy ra đúng dòng PASS ở trên), runtime/editor với DLL của Unity 6000.3.19f1 (`UNITY_EDITOR` và `UNITY_ANDROID`).

---

### Task 1: Logic thuần + harness (TDD)

**Files:**
- Create: `Assets/JamJam/Runtime/WoolFeelEvent.cs`, `WoolFeelTracker.cs`, `WoolEase.cs`, `WoolShake.cs`, `WoolHapticPattern.cs`
- Create: `Tools/WoolJamChecks/FeelChecks.cs`
- Modify: `Tools/WoolJamChecks/Program.cs:81`, `:232`; `Tools/WoolJamChecks/WoolJamChecks.csproj:16`

**Interfaces — Produces:**
- `enum WoolFeelKind { BlockLaunched, BlockBlocked, SpoolArrived, YarnConsumed, SpoolCompleted, SlotUnlocked, TrayFull, Danger, Won, Lost }`
- `readonly struct WoolFeelEvent { WoolFeelKind Kind; int Index; int Color; WoolFeelEvent(WoolFeelKind, int, int) }` — Index: block (Launched/Blocked), slot (Arrived/Completed/Unlocked), yarn unit (Consumed), -1 còn lại; Color: màu len hoặc -1.
- `sealed class WoolFeelTracker { const float DangerRatio = 0.8f; const float DangerRearm = 0.75f; void Reset(WoolGame); void Collect(WoolGame, List<WoolFeelEvent>); void Blocked(int index, int color, List<WoolFeelEvent>) }` — `Collect` chỉ append; gặp `WoolGame` khác với lần `Reset` thì tự `Reset` và không phát gì.
- `static class WoolEase { float Clamp(float); float OutBack(float); float Punch(float); float Arc(float) }`
- `sealed class WoolShake { const MaxOffset 0.35f, MaxRoll 2f, Frequency 18f, Decay 1.6f; float Trauma; void Add(float); void Reset(); void Step(float delta, out float x, out float y, out float roll) }`
- `static class WoolHapticPattern { void Get(WoolFeelKind, out long[] timings, out int[] amplitudes); int Strength(WoolFeelKind) }`

- [ ] **Bước 0: Baseline (trước mọi thay đổi)**

```bash
SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"
mkdir -p "$SCRATCH/feel-run"
cd "/d/Unity Project/Jam_Jam"
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor --include=*.meta | sort > "$SCRATCH/feel-run/guids.before.txt"
md5sum Assets/JamJam/Generated/Levels/*.asset > "$SCRATCH/feel-run/levels.md5"
md5sum Assets/JamJam/Runtime/WoolGame.cs Assets/JamJam/Runtime/WoolRouting.cs > "$SCRATCH/feel-run/logic.md5"
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
```
Kỳ vọng: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions`.

Mở `WoolJam.unity`, Game view hiển thị, **[PLAY START]**, rồi:
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(1); return "ok";'
unity command eval --caller plugin --skill unity-cli --no-banner 'return "batches " + UnityEditor.UnityStats.batches + " draw " + UnityEditor.UnityStats.drawCalls + " setpass " + UnityEditor.UnityStats.setPassCalls;'
```
**[PLAY STOP]**. Ghi dòng `batches` vào `$SCRATCH/feel-run/drawcalls.txt` với nhãn `before L2` (spec ghi cuối phần 1 là 302).

- [ ] **Bước 1: Viết test RED — tạo `Tools/WoolJamChecks/FeelChecks.cs`**

```csharp
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
        CheckEase();
        CheckShake();
        CheckHaptics();
    }
}
```

- [ ] **Bước 2: Gọi test trong `Program.cs`**

**Trước**
```81:81:Tools/WoolJamChecks/Program.cs
        ProgressChecks.Run();
```

**Sau**
```csharp
        ProgressChecks.Run();
        FeelChecks.Run();
```

**Trước**
```232:232:Tools/WoolJamChecks/Program.cs
        Console.WriteLine("PASS: " + passed + " gameplay assertions; " + routeAssertions + " route assertions; " + ProgressChecks.Assertions + " progress assertions");
```

**Sau**
```csharp
        Console.WriteLine("PASS: " + passed + " gameplay assertions; " + routeAssertions + " route assertions; " + ProgressChecks.Assertions + " progress assertions; " + FeelChecks.Assertions + " feel assertions");
```

- [ ] **Bước 3: Thêm file vào harness**

**Trước**
```16:16:Tools/WoolJamChecks/WoolJamChecks.csproj
    <Compile Include="../../Assets/JamJam/Runtime/WoolSettings.cs" />
```

**Sau**
```xml
    <Compile Include="../../Assets/JamJam/Runtime/WoolSettings.cs" />
    <Compile Include="FeelChecks.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolFeelEvent.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolFeelTracker.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolEase.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolShake.cs" />
    <Compile Include="../../Assets/JamJam/Runtime/WoolHapticPattern.cs" />
```

- [ ] **Bước 4: Chạy RED**

Run: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`
Kỳ vọng: build FAIL với `CS2001` (không tìm thấy `WoolFeelEvent.cs`…) và/hoặc `CS0246` (`WoolFeelTracker`, `WoolEase`, `WoolShake`, `WoolHapticPattern`).

- [ ] **Bước 5: Tạo `Assets/JamJam/Runtime/WoolFeelEvent.cs`**

```csharp
namespace JamJam
{
    public enum WoolFeelKind { BlockLaunched, BlockBlocked, SpoolArrived, YarnConsumed, SpoolCompleted, SlotUnlocked, TrayFull, Danger, Won, Lost }

    public readonly struct WoolFeelEvent
    {
        public readonly WoolFeelKind Kind;
        public readonly int Index;
        public readonly int Color;

        public WoolFeelEvent(WoolFeelKind kind, int index, int color)
        {
            Kind = kind;
            Index = index;
            Color = color;
        }
    }
}
```

- [ ] **Bước 6: Tạo `Assets/JamJam/Runtime/WoolFeelTracker.cs`**

Đối chiếu chuyển trạng thái thật trong `WoolGame`: `Select` đặt `Phase=1`, `spool.Block`, `Arrival=Duration` ngoài `Step` (Launched/TrayFull thấy ở `Collect` kế tiếp); trong `Step`, `Arrival` về đúng `0` ở frame block bay xong (dòng 233) và cùng frame đó spool đặt `Target`; `Consumed=true` và `Remaining--` chỉ trong `Step` (dòng 257–260), tối đa một đơn vị mỗi spool mỗi `Step`; `Status=Won` ở cuối cùng `Step` với lần consume cuối (dòng 280); `Block=-1` khi `Finish` hết (dòng 248).

```csharp
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
```

- [ ] **Bước 7: Tạo `Assets/JamJam/Runtime/WoolEase.cs`**

```csharp
using System;

namespace JamJam
{
    public static class WoolEase
    {
        const double Overshoot = 1.70158;
        const double PunchGain = 1.4;

        public static float Clamp(float t)
        {
            if (!(t > 0)) return 0;
            return t > 1 ? 1 : t;
        }

        public static float OutBack(float t)
        {
            t = Clamp(t);
            if (t <= 0 || t >= 1) return t;
            var x = t - 1.0;
            return (float)(1 + (Overshoot + 1) * x * x * x + Overshoot * x * x);
        }

        public static float Punch(float t)
        {
            t = Clamp(t);
            if (t <= 0 || t >= 1) return 0;
            var decay = 1.0 - t;
            return (float)(PunchGain * Math.Sin(3 * Math.PI * t) * decay * decay);
        }

        public static float Arc(float t)
        {
            t = Clamp(t);
            if (t <= 0 || t >= 1) return 0;
            return (float)Math.Sin(Math.PI * t);
        }
    }
}
```

- [ ] **Bước 8: Tạo `Assets/JamJam/Runtime/WoolShake.cs`**

```csharp
using System;

namespace JamJam
{
    public sealed class WoolShake
    {
        public const float MaxOffset = 0.35f;
        public const float MaxRoll = 2f;
        public const float Frequency = 18f;
        public const float Decay = 1.6f;
        double time;

        public float Trauma { get; private set; }

        public void Add(float amount)
        {
            if (!(amount > 0)) return;
            Trauma = Math.Min(1f, Trauma + amount);
        }

        public void Reset()
        {
            Trauma = 0;
            time = 0;
        }

        public void Step(float delta, out float x, out float y, out float roll)
        {
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            time += delta;
            Trauma = Math.Max(0f, Trauma - Decay * delta);
            var amplitude = Trauma * Trauma;
            x = 0;
            y = 0;
            roll = 0;
            if (amplitude <= 0) return;
            var phase = time * Frequency;
            x = MaxOffset * amplitude * Noise(phase, 1);
            y = MaxOffset * amplitude * Noise(phase, 2);
            roll = MaxRoll * amplitude * Noise(phase, 3);
        }

        static float Noise(double phase, int seed)
        {
            var cell = Math.Floor(phase);
            var t = phase - cell;
            var smooth = t * t * (3 - 2 * t);
            var a = Hash(cell, seed);
            var b = Hash(cell + 1, seed);
            return (float)(a + (b - a) * smooth);
        }

        static double Hash(double cell, int seed)
        {
            var value = Math.Sin(cell * 127.1 + seed * 311.7) * 43758.5453;
            return (value - Math.Floor(value)) * 2 - 1;
        }
    }
}
```

- [ ] **Bước 9: Tạo `Assets/JamJam/Runtime/WoolHapticPattern.cs`**

Mẫu theo bảng spec: Launched 12ms/90; Arrived và SlotUnlocked 15ms/110; Completed 30ms/170; Blocked, TrayFull, Danger = cảnh báo 0/18/40/18 ms, amp 0/140/0/140; Won 30/60/30/60/60 ms, amp 160/0/200/0/255; Lost 90ms/255; YarnConsumed rỗng.

```csharp
using System;

namespace JamJam
{
    public static class WoolHapticPattern
    {
        static readonly long[] LightTimings = { 12 };
        static readonly int[] LightAmplitudes = { 90 };
        static readonly long[] SoftTimings = { 15 };
        static readonly int[] SoftAmplitudes = { 110 };
        static readonly long[] MediumTimings = { 30 };
        static readonly int[] MediumAmplitudes = { 170 };
        static readonly long[] WarningTimings = { 0, 18, 40, 18 };
        static readonly int[] WarningAmplitudes = { 0, 140, 0, 140 };
        static readonly long[] WinTimings = { 30, 60, 30, 60, 60 };
        static readonly int[] WinAmplitudes = { 160, 0, 200, 0, 255 };
        static readonly long[] HeavyTimings = { 90 };
        static readonly int[] HeavyAmplitudes = { 255 };

        public static void Get(WoolFeelKind kind, out long[] timings, out int[] amplitudes)
        {
            switch (kind)
            {
                case WoolFeelKind.BlockLaunched: timings = LightTimings; amplitudes = LightAmplitudes; return;
                case WoolFeelKind.SpoolArrived:
                case WoolFeelKind.SlotUnlocked: timings = SoftTimings; amplitudes = SoftAmplitudes; return;
                case WoolFeelKind.SpoolCompleted: timings = MediumTimings; amplitudes = MediumAmplitudes; return;
                case WoolFeelKind.BlockBlocked:
                case WoolFeelKind.TrayFull:
                case WoolFeelKind.Danger: timings = WarningTimings; amplitudes = WarningAmplitudes; return;
                case WoolFeelKind.Won: timings = WinTimings; amplitudes = WinAmplitudes; return;
                case WoolFeelKind.Lost: timings = HeavyTimings; amplitudes = HeavyAmplitudes; return;
                default: timings = Array.Empty<long>(); amplitudes = Array.Empty<int>(); return;
            }
        }

        public static int Strength(WoolFeelKind kind)
        {
            Get(kind, out _, out var amplitudes);
            var peak = 0;
            foreach (var amplitude in amplitudes) peak = Math.Max(peak, amplitude);
            return peak;
        }
    }
}
```

- [ ] **Bước 10: Chạy GREEN**

Run: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`
Kỳ vọng: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; 104 feel assertions`.

- [ ] **Bước 11: Unity compile** — `unity recompile`. Kỳ vọng 0 lỗi; Unity sinh `.meta` cho 5 file mới.

- [ ] **Bước 12: Kiểm tra** — `md5sum -c "$SCRATCH/feel-run/logic.md5"` → hai dòng `OK`; `grep -n "UnityEngine" Assets/JamJam/Runtime/WoolFeel{Event,Tracker}.cs Assets/JamJam/Runtime/WoolEase.cs Assets/JamJam/Runtime/WoolShake.cs Assets/JamJam/Runtime/WoolHapticPattern.cs` → rỗng.

- [ ] **Bước 13: Dừng, báo user.**

---

### Task 2: Haptic (`IWoolHaptics`, `WoolHaptics`)

**Files:**
- Create: `Assets/JamJam/Runtime/IWoolHaptics.cs`, `Assets/JamJam/Runtime/WoolHaptics.cs`
- Test: eval Unity + build scratch `$SCRATCH/feel-run/android/A.csproj`

**Interfaces:**
- Consumes: không.
- Produces: `interface IWoolHaptics { void Play(long[] timings, int[] amplitudes); }`; `static class WoolHaptics { static void Create(bool enabled, out IWoolHaptics haptics) }`; `public sealed class WoolNullHaptics : IWoolHaptics`. Bản Android (`WoolAndroidHaptics`, internal) chỉ tồn tại khi `UNITY_ANDROID && !UNITY_EDITOR`.

- [ ] **Bước 1: RED**

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.WoolHaptics.Create(false, out var off); JamJam.WoolHaptics.Create(true, out var on); return (off is JamJam.WoolNullHaptics) + " " + (on is JamJam.WoolNullHaptics);'
```
Kỳ vọng: lỗi compile eval (`WoolHaptics` không tồn tại).

- [ ] **Bước 2: Tạo `Assets/JamJam/Runtime/IWoolHaptics.cs`**

```csharp
namespace JamJam
{
    public interface IWoolHaptics
    {
        void Play(long[] timings, int[] amplitudes);
    }
}
```

- [ ] **Bước 3: Tạo `Assets/JamJam/Runtime/WoolHaptics.cs`**

Android: SDK ≥ 26 dùng `VibrationEffect.createWaveform(timings, amplitudes, -1)`, thấp hơn dùng `vibrate(tổng ms)`; mọi exception JNI bị nuốt; `Handheld.Vibrate()` chỉ chạy khi không lấy được service.

```csharp
using System;
using UnityEngine;

namespace JamJam
{
    public static class WoolHaptics
    {
        public static void Create(bool enabled, out IWoolHaptics haptics)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (enabled)
            {
                haptics = new WoolAndroidHaptics();
                return;
            }
#endif
            haptics = new WoolNullHaptics();
        }
    }

    public sealed class WoolNullHaptics : IWoolHaptics
    {
        public void Play(long[] timings, int[] amplitudes) { }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    sealed class WoolAndroidHaptics : IWoolHaptics
    {
        readonly AndroidJavaObject vibrator;
        readonly int sdk;

        public WoolAndroidHaptics()
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            catch (Exception)
            {
                vibrator = null;
            }
        }

        public void Play(long[] timings, int[] amplitudes)
        {
            if (timings == null || amplitudes == null || timings.Length == 0 || timings.Length != amplitudes.Length) return;
            try
            {
                if (vibrator == null)
                {
                    Handheld.Vibrate();
                    return;
                }
                if (sdk >= 26)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var wave = effect.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                        vibrator.Call("vibrate", wave);
                    return;
                }
                long total = 0;
                foreach (var timing in timings) total += timing;
                vibrator.Call("vibrate", total);
            }
            catch (Exception)
            {
            }
        }
    }
#endif
}
```

- [ ] **Bước 4: GREEN Editor** — `unity recompile` (0 lỗi), chạy lại eval Bước 1. Kỳ vọng `True True`.

- [ ] **Bước 5: Compile nhánh Android ngoài Unity** (Editor không compile được nhánh `!UNITY_EDITOR`)

```bash
mkdir -p "$SCRATCH/feel-run/android"
cat > "$SCRATCH/feel-run/android/A.csproj" <<'XML'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <LangVersion>9.0</LangVersion>
    <DefineConstants>UNITY_ANDROID</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="D:/Unity Project/Jam_Jam/Assets/JamJam/Runtime/IWoolHaptics.cs" />
    <Compile Include="D:/Unity Project/Jam_Jam/Assets/JamJam/Runtime/WoolHaptics.cs" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data/Managed/UnityEngine/*.dll" Private="false" />
  </ItemGroup>
</Project>
XML
dotnet build "$SCRATCH/feel-run/android/A.csproj" 2>&1 | grep -E " error |Build succeeded" | sort -u
```
Kỳ vọng: `Build succeeded.` và không có ` error `.

- [ ] **Bước 6: Dừng, báo user.**

---

### Task 3: Asset hạt (texture, material, prefab) + `WoolArt` + check editor

**Files:**
- Modify: `Assets/JamJam/Runtime/WoolArt.cs:19`, `Assets/JamJam/Editor/WoolAssetsBuilder.cs:83-84`, `:344-347`
- Create: `Assets/JamJam/Editor/WoolArtChecks.Feel.cs`

**Interfaces:**
- Consumes: `WoolHaptics.Create`, `WoolNullHaptics` (Task 2); helper có sẵn `WoolArtChecks.Expect`, `LoadArt`, `TextureAt`; builder có sẵn `WriteTexture`, `ShaderMat`, `SavePrefab`, `Smooth`.
- Produces: `WoolArt.Puff`, `WoolArt.Sparkle : GameObject` (prefab có `ParticleSystem`, `playOnAwake=false`, `loop=false`, `maxParticles=256`, `simulationSpace=World`, emission/shape tắt, sortingOrder 50); `WoolAssetsBuilder.ParticleShader`, `FeelMaxParticles`, `ParticleTextureSize` (public const); `WoolArtChecks.Feel() : string`.

- [ ] **Bước 1: Field `WoolArt`**

**Trước**
```19:19:Assets/JamJam/Runtime/WoolArt.cs
        public Material PadLocked;
```

**Sau**
```csharp
        public Material PadLocked;
        public GameObject Puff;
        public GameObject Sparkle;
```

- [ ] **Bước 2: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.Feel.cs`**

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string Feel()
        {
            LoadArt(out var art);
            ExpectParticles(art.Puff, "FeelPuff", "SoftDot", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            ExpectParticles(art.Sparkle, "FeelSparkle", "Star", (int)UnityEngine.Rendering.BlendMode.One);
            ExpectAlpha("SoftDot", 32, 32, 0.95f, 1.01f);
            ExpectAlpha("SoftDot", 0, 0, -0.01f, 0.01f);
            ExpectAlpha("Star", 32, 32, 0.95f, 1.01f);
            ExpectAlpha("Star", 48, 32, 0.3f, 1.01f);
            ExpectAlpha("Star", 48, 48, -0.01f, 0.05f);
            WoolHaptics.Create(false, out var off);
            WoolHaptics.Create(true, out var on);
            Expect(off is WoolNullHaptics && on is WoolNullHaptics, "Editor and disabled haptics use the null implementation");
            on.Play(new long[] { 12 }, new[] { 90 });
            on.Play(null, null);
            return "FEEL PASS / 2 particle prefabs / null haptics";
        }

        static void ExpectParticles(GameObject prefab, string name, string texture, int dstBlend)
        {
            Expect(prefab != null && prefab.name == name, name + " prefab assigned to WoolArt");
            var system = prefab.GetComponent<ParticleSystem>();
            Expect(system != null, name + " has a ParticleSystem");
            var main = system.main;
            Expect(!main.playOnAwake && main.maxParticles == WoolAssetsBuilder.FeelMaxParticles && main.simulationSpace == ParticleSystemSimulationSpace.World, name + " main module");
            Expect(!system.emission.enabled && !system.shape.enabled, name + " emits only from code");
            var material = prefab.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            Expect(material != null && material.shader.name == WoolAssetsBuilder.ParticleShader && !ShaderUtil.ShaderHasError(material.shader), name + " particle shader compiles");
            TextureAt(texture, out var image);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image));
            Expect(image.width == WoolAssetsBuilder.ParticleTextureSize && image.height == WoolAssetsBuilder.ParticleTextureSize && importer.wrapMode == TextureWrapMode.Clamp && importer.alphaIsTransparency, texture + " is a 64x64 clamped alpha texture");
            Expect(material.GetTexture("_BaseMap") == image && material.renderQueue == (int)UnityEngine.Rendering.RenderQueue.Transparent && Mathf.RoundToInt(material.GetFloat("_DstBlend")) == dstBlend, name + " transparent blend " + dstBlend);
        }

        static void ExpectAlpha(string texture, int x, int y, float min, float max)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(WoolAssetsBuilder.Root + "/Textures/" + texture + ".png"));
            var alpha = image.GetPixel(x, y).a;
            Object.DestroyImmediate(image);
            Expect(alpha >= min && alpha <= max, texture + " alpha at " + x + "," + y + " in " + min + ".." + max + " but was " + alpha);
        }
    }
}
```

- [ ] **Bước 3: Chạy RED** — `unity recompile`, rồi:

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.Feel();'
```
Kỳ vọng: `ART CHECK FAILED: FeelPuff prefab assigned to WoolArt`.

- [ ] **Bước 4: Gọi bộ sinh hạt trong `Build`**

**Trước**
```83:84:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            SavePrefab(star, "Confetti", out art.Confetti);
            EditorUtility.SetDirty(art);
```

**Sau**
```csharp
            SavePrefab(star, "Confetti", out art.Confetti);
            MakeFeel(out art.Puff, out art.Sparkle);
            EditorUtility.SetDirty(art);
```

- [ ] **Bước 5: Texture/material/prefab hạt** (chèn sau `ShaderMat`). Material dùng `Universal Render Pipeline/Particles/Unlit` transparent: Puff alpha (`_Blend 0`, `SrcAlpha/OneMinusSrcAlpha`), Sparkle additive (`_Blend 2`, `SrcAlpha/One`); texture 64×64 Clamp, alpha từ input.

**Trước**
```344:347:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }
```

**Sau**
```csharp
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        public const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        public const int FeelMaxParticles = 256;
        public const int ParticleTextureSize = 64;

        static void MakeFeel(out GameObject puff, out GameObject sparkle)
        {
            ParticleTexture("SoftDot", SoftDot, out var dot);
            ParticleTexture("Star", StarShape, out var starTexture);
            ParticleMat("FeelPuff", dot, false, out var puffMaterial);
            ParticleMat("FeelSparkle", starTexture, true, out var sparkleMaterial);
            MakeParticles("FeelPuff", puffMaterial, 0.45f, 0.32f, 0f, 3f, out puff);
            MakeParticles("FeelSparkle", sparkleMaterial, 0.7f, 0.26f, 0.5f, 1.2f, out sparkle);
        }

        static Color SoftDot(float u, float v)
        {
            var d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2;
            return new Color(1, 1, 1, 1 - Smooth(0.15f, 1, d));
        }

        static Color StarShape(float u, float v)
        {
            var x = Mathf.Abs(u - 0.5f) * 2;
            var y = Mathf.Abs(v - 0.5f) * 2;
            var arms = Mathf.Max((1 - y) * Mathf.Exp(-x * x * 90), (1 - x) * Mathf.Exp(-y * y * 90));
            var core = 1 - Smooth(0, 0.35f, Mathf.Sqrt(x * x + y * y));
            return new Color(1, 1, 1, Mathf.Clamp01(Mathf.Max(arms, core)));
        }

        static void ParticleTexture(string name, Func<float, float, Color> pixel, out Texture2D texture)
        {
            WriteTexture(name, ParticleTextureSize, ParticleTextureSize, pixel, false, out texture);
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void ParticleMat(string name, Texture2D texture, bool additive, out Material material)
        {
            ShaderMat(name, ParticleShader, out material);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", additive ? 2 : 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
        }

        static void MakeParticles(string name, Material material, float life, float size, float gravity, float drag, out GameObject prefab)
        {
            var go = new GameObject(name);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1;
            main.maxParticles = FeelMaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = life;
            main.startSize = size;
            main.startSpeed = 0;
            main.gravityModifier = gravity;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100;
            limit.drag = drag;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(gradient);
            var shrink = system.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.2f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 50;
            SavePrefab(go, name, out prefab);
        }
```

- [ ] **Bước 6: Compile** — `unity recompile`. Kỳ vọng 0 lỗi, 0 cảnh báo mới trong file đã sửa.

- [ ] **Bước 7: `Rebuild Art`** (đã thoát Play Mode, lưu mọi scene đang mở)

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtBuilder.Rebuild(); return "rebuilt";'
```
Kỳ vọng: `rebuilt`, console có `Wool art rebuilt`.

- [ ] **Bước 8: GREEN + hồi quy art**

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.Feel() + " | " + JamJam.Editor.WoolArtChecks.Textures() + " | " + JamJam.Editor.WoolArtChecks.Tray() + " | " + JamJam.Editor.WoolArtChecks.Blocks() + " | " + JamJam.Editor.WoolArtChecks.Chevron() + " | " + JamJam.Editor.WoolArtChecks.Stage();'
```
Kỳ vọng: `FEEL PASS / 2 particle prefabs / null haptics` và năm PASS cũ.

- [ ] **Bước 9: GUID/level**

```bash
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor --include=*.meta | sort > "$SCRATCH/feel-run/guids.t3.txt"
comm -23 "$SCRATCH/feel-run/guids.before.txt" "$SCRATCH/feel-run/guids.t3.txt"
md5sum -c "$SCRATCH/feel-run/levels.md5"
ls Assets/JamJam/Generated/Prefabs/FeelPuff.prefab Assets/JamJam/Generated/Prefabs/FeelSparkle.prefab Assets/JamJam/Generated/Textures/SoftDot.png Assets/JamJam/Generated/Textures/Star.png
```
Kỳ vọng: `comm` rỗng (không GUID nào mất), level `OK`, bốn file tồn tại.

- [ ] **Bước 10: Dừng, báo user.**

---

### Task 4: `WoolFeel` + tích hợp `WoolScene`

**Files:**
- Create: `Assets/JamJam/Runtime/WoolFeel.cs`, `Assets/JamJam/Editor/WoolArtChecks.PlayFeel.cs`
- Modify: `Assets/JamJam/Runtime/WoolScene.cs:41-43`, `:95-99`, `:126-127`, `:132-133`, `:169-170`, `:189-190`, `:223-224`, `:236-238`, `:262-267`, `:275`

**Interfaces:**
- Consumes: Task 1 (`WoolFeelTracker`, `WoolFeelEvent`, `WoolEase`, `WoolShake`, `WoolHapticPattern`), Task 2 (`WoolHaptics.Create`, `IWoolHaptics`), Task 3 (`WoolArt.Puff`, `WoolArt.Sparkle`); `WoolSettings(new WoolPlayerPrefs()).Vibration`; `WoolArtChecks.Click(WoolBlockView, out bool)` có sẵn.
- Produces: `sealed class WoolFeel : MonoBehaviour` với `void Begin(WoolScene, Camera)`, `void Restart(WoolGame)`, `void Blocked(int index)`, `void Step(WoolGame, float delta)`, `float BlockScale(int)`, `float BlockLift(int)`, `float BlockFlash(int)`, `float SpoolScale(int)`, `float SlotScale(int)`, `float SlotFlash(int)`, `float DragonScale`, `float CameraZoom`, `float Trauma`, `int PuffCount`, `int SparkleCount`, `static readonly Color FlashRed` (`#FF5A5A`), các hằng tinh chỉnh `public const`. `WoolScene.BlockAt(int) : Vector3`, `UnitAt(int) : Vector3`, `KittenAt : Vector3`. `WoolArtChecks.PlayFeel() : string`, `WoolArtChecks.PlayFinish(bool win) : string`.

Thứ tự trong `Tick`: `State.Step` → `feel.Step` → `RenderFrame` (hệ số juice của frame đúng bằng sự kiện vừa dò). `Tick` đã return sớm khi `Paused` nên rung/hạt hệ số đóng băng khi tạm dừng; khi `Lost`, `State.Step` không chạy nhưng `feel.Step` vẫn chạy để hiệu ứng chạy hết.

- [ ] **Bước 1: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.PlayFeel.cs`**

```csharp
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string PlayFeel()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null, "WoolScene is running");
            var feel = scene.GetComponent<WoolFeel>();
            Expect(feel != null, "WoolFeel is added at runtime");
            var camera = Camera.main;
            var origin = camera.transform.position;
            var rotation = camera.transform.rotation;
            var size = camera.orthographicSize;
            Expect(feel.Trauma == 0, "Fresh level starts without trauma");
            Physics.SyncTransforms();
            var views = Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None);
            var blocked = -1;
            foreach (var view in views)
            {
                if (scene.State.CanSelect(view.Index)) continue;
                Click(view, out var hit);
                if (!hit) continue;
                blocked = view.Index;
                break;
            }
            Expect(blocked >= 0, "A blocked block was clicked through EventSystem");
            Expect(feel.Trauma > 0.17f, "Blocked click adds trauma");
            feel.Step(scene.State, 0.05f);
            Expect(feel.BlockFlash(blocked) > 0, "Blocked block flashes red");
            Expect(Vector3.Distance(camera.transform.position, origin) > 0.0001f, "Camera leaves its origin while shaking");
            for (var i = 0; i < 30; i++) feel.Step(scene.State, 0.05f);
            Expect(Vector3.Distance(camera.transform.position, origin) < 0.0001f && Quaternion.Angle(camera.transform.rotation, rotation) < 0.01f && Mathf.Abs(camera.orthographicSize - size) < 0.0001f, "Camera returns exactly to its origin after 1.5 s");
            Expect(feel.BlockFlash(blocked) == 0 && feel.Trauma == 0, "Flash and trauma settle");
            var launched = -1;
            foreach (var view in views)
            {
                if (!scene.State.CanSelect(view.Index)) continue;
                Click(view, out var hit);
                if (!hit) continue;
                launched = view.Index;
                break;
            }
            Expect(launched >= 0, "A clear block was clicked through EventSystem");
            var puffs = feel.PuffCount;
            feel.Step(scene.State, 0.02f);
            Expect(feel.PuffCount > puffs, "Launch emits Puff particles");
            feel.Step(scene.State, 0.02f);
            Expect(feel.BlockScale(launched) > 1, "Launched block pops");
            for (var i = 0; i < 10; i++) { scene.State.Step(0.02f); feel.Step(scene.State, 0.02f); }
            Expect(feel.BlockLift(launched) > 0, "Flying block lifts on its arc");
            feel.Blocked(blocked);
            feel.Step(scene.State, 0.02f);
            Expect(Vector3.Distance(camera.transform.position, origin) > 0.0001f, "Camera shakes before replay");
            scene.Replay();
            Expect(camera.transform.position == origin && camera.transform.rotation == rotation && camera.orthographicSize == size && feel.Trauma == 0, "Replay mid-shake restores the camera exactly");
            return "PLAY FEEL PASS / blocked " + blocked + " / launched " + launched;
        }

        public static string PlayFinish(bool win)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            var feel = scene.GetComponent<WoolFeel>();
            var game = scene.State;
            var camera = Camera.main;
            var origin = camera.transform.position;
            var size = camera.orthographicSize;
            for (var frame = 0; frame < 30000 && game.Status == RoundStatus.Playing; frame++)
            {
                if (win)
                    for (var b = 0; b < game.Blocks.Count; b++)
                        if (game.CanSelect(b) && game.HasVisibleColor(game.Blocks[b].Definition.Color)) { game.Select(b); break; }
                game.Step(0.02f);
            }
            Expect(game.Status == (win ? RoundStatus.Won : RoundStatus.Lost), "Round ends " + (win ? "Won" : "Lost") + " but was " + game.Status);
            feel.Step(game, 0.02f);
            feel.Step(game, 0.02f);
            feel.Step(game, 0.02f);
            if (win) Expect(feel.SparkleCount > 0 && feel.CameraZoom > 0 && camera.orthographicSize < size, "Win sparkles and zooms in");
            else Expect(feel.Trauma > 0 && feel.DragonScale > 1 && feel.PuffCount > 0, "Loss shakes, swells the dragon and puffs");
            for (var i = 0; i < 60; i++) feel.Step(game, 0.02f);
            Expect(Vector3.Distance(camera.transform.position, origin) < 0.0001f && Mathf.Abs(camera.orthographicSize - size) < 0.0001f, "Camera settles back to its origin");
            Expect(feel.CameraZoom == 0 && feel.DragonScale == 1, "Zoom and dragon punch settle");
            return "PLAY FINISH PASS / " + game.Status;
        }
    }
}
```

- [ ] **Bước 2: Chạy RED** — `unity recompile`. Kỳ vọng lỗi `CS0246: The type or namespace name 'WoolFeel' could not be found` trong `WoolArtChecks.PlayFeel.cs`.

- [ ] **Bước 3: Tạo `Assets/JamJam/Runtime/WoolFeel.cs`**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JamJam
{
    public sealed class WoolFeel : MonoBehaviour
    {
        public const float LaunchPop = 0.18f;
        public const float LaunchPopTime = 0.18f;
        public const float FlightLift = 0.7f;
        public const float BlockedFlash = 0.45f;
        public const float BlockedFlashTime = 0.3f;
        public const float ArrivePopTime = 0.28f;
        public const float CompletePunch = 0.25f;
        public const float CompletePunchTime = 0.2f;
        public const float SlotPop = 0.2f;
        public const float SlotPopTime = 0.25f;
        public const float TrayFlash = 0.35f;
        public const float TrayFlashTime = 0.5f;
        public const float EatPunch = 0.08f;
        public const float EatPunchTime = 0.15f;
        public const float LostPunch = 0.3f;
        public const float LostPunchTime = 0.4f;
        public const float WinZoom = 0.06f;
        public const float WinZoomTime = 0.6f;
        public const float BlockedTrauma = 0.18f;
        public const float ArriveTrauma = 0.05f;
        public const float CompleteTrauma = 0.08f;
        public const float DangerTrauma = 0.25f;
        public const float LostTrauma = 0.6f;
        public const float PuffSpeed = 1.6f;
        public const float PuffSize = 0.32f;
        public const float PuffLife = 0.45f;
        public const float SparkleSpeed = 2.6f;
        public const float SparkleSize = 0.26f;
        public const float SparkleLife = 0.7f;
        public const float WinSpeed = 4f;
        public const float WinSize = 0.34f;
        public const float WinLife = 1.1f;
        public const float LostSpeed = 2.4f;
        public const float LostSize = 0.5f;
        public const float LostLife = 0.7f;
        public static readonly Color FlashRed = new Color32(0xFF, 0x5A, 0x5A, 0xFF);
        static readonly Color LostOrange = new Color32(0xFF, 0x6A, 0x3D, 0xFF);
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        const float Idle = 1000f;
        readonly WoolFeelTracker tracker = new WoolFeelTracker();
        readonly WoolShake shake = new WoolShake();
        readonly List<WoolFeelEvent> events = new List<WoolFeelEvent>();
        WoolScene owner;
        WoolGame game;
        Camera view;
        Vector3 cameraPosition;
        Quaternion cameraRotation;
        float cameraSize;
        WoolSettings settings;
        IWoolHaptics haptics;
        bool vibration;
        ParticleSystem puff;
        ParticleSystem sparkle;
        float[] blockPop = Array.Empty<float>();
        float[] blockFlash = Array.Empty<float>();
        float[] spoolArrive = Array.Empty<float>();
        float[] spoolPunch = Array.Empty<float>();
        float[] slotPop = Array.Empty<float>();
        float[] slotFlash = Array.Empty<float>();
        float dragonAge = Idle;
        float dragonAmount;
        float dragonTime = 1;
        float zoomAge = Idle;
        int seed;

        public float Trauma => shake.Trauma;
        public int PuffCount => puff != null ? puff.particleCount : 0;
        public int SparkleCount => sparkle != null ? sparkle.particleCount : 0;
        public float DragonScale => 1 + dragonAmount * WoolEase.Punch(dragonAge / dragonTime);
        public float CameraZoom => WinZoom * WoolEase.Punch(zoomAge / WinZoomTime);

        public float BlockScale(int index) { return 1 + LaunchPop * WoolEase.Punch(Age(blockPop, index) / LaunchPopTime); }
        public float BlockFlash(int index) { return BlockedFlash * WoolEase.Arc(Age(blockFlash, index) / BlockedFlashTime); }
        public float SlotScale(int index) { return 1 + SlotPop * WoolEase.Punch(Age(slotPop, index) / SlotPopTime); }
        public float SlotFlash(int index) { return TrayFlash * WoolEase.Arc(Age(slotFlash, index) / TrayFlashTime); }

        public float BlockLift(int index)
        {
            if (game == null || index < 0 || index >= game.Blocks.Count) return 0;
            var block = game.Blocks[index];
            return block.Phase == 1 && block.Duration > 0 ? FlightLift * WoolEase.Arc(block.Travel / block.Duration) : 0;
        }

        public float SpoolScale(int index)
        {
            var arrive = Age(spoolArrive, index);
            if (arrive < ArrivePopTime) return WoolEase.OutBack(arrive / ArrivePopTime);
            return 1 + CompletePunch * WoolEase.Punch(Age(spoolPunch, index) / CompletePunchTime);
        }

        public void Begin(WoolScene scene, Camera camera)
        {
            owner = scene;
            if (settings == null) settings = new WoolSettings(new WoolPlayerPrefs());
            if (camera != view)
            {
                Restore();
                view = camera;
                if (view != null)
                {
                    cameraPosition = view.transform.position;
                    cameraRotation = view.transform.rotation;
                    cameraSize = view.orthographicSize;
                }
            }
            if (scene == null || scene.Art == null) return;
            if (puff == null && scene.Art.Puff != null) Spawn(scene.Art.Puff, out puff);
            if (sparkle == null && scene.Art.Sparkle != null) Spawn(scene.Art.Sparkle, out sparkle);
        }

        public void Restart(WoolGame state)
        {
            game = state;
            tracker.Reset(state);
            shake.Reset();
            events.Clear();
            var blocks = state != null ? state.Blocks.Count : 0;
            var spools = state != null ? state.Spools.Count : 0;
            Rest(ref blockPop, blocks);
            Rest(ref blockFlash, blocks);
            Rest(ref spoolArrive, spools);
            Rest(ref spoolPunch, spools);
            Rest(ref slotPop, spools);
            Rest(ref slotFlash, spools);
            dragonAge = Idle;
            zoomAge = Idle;
            if (puff != null) puff.Clear();
            if (sparkle != null) sparkle.Clear();
            Restore();
            var enabled = settings != null && settings.Vibration;
            if (haptics != null && enabled == vibration) return;
            WoolHaptics.Create(enabled, out haptics);
            vibration = enabled;
        }

        public void Blocked(int index)
        {
            if (game == null || index < 0 || index >= game.Blocks.Count) return;
            events.Clear();
            tracker.Blocked(index, game.Blocks[index].Definition.Color, events);
            Play();
        }

        public void Step(WoolGame state, float delta)
        {
            if (state != game) Restart(state);
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            Advance(delta);
            events.Clear();
            tracker.Collect(state, events);
            Play();
            shake.Step(delta, out var x, out var y, out var roll);
            Apply(x, y, roll);
        }

        void OnDisable() { Restore(); }

        void Spawn(GameObject prefab, out ParticleSystem system)
        {
            var go = Instantiate(prefab, transform);
            go.name = prefab.name;
            system = go.GetComponent<ParticleSystem>();
        }

        static float Age(float[] ages, int index) { return index >= 0 && index < ages.Length ? ages[index] : Idle; }

        static void Rest(ref float[] ages, int count)
        {
            if (ages.Length != count) ages = new float[count];
            for (var i = 0; i < count; i++) ages[i] = Idle;
        }

        static void Trigger(float[] ages, int index) { if (index >= 0 && index < ages.Length) ages[index] = 0; }

        static void Grow(float[] ages, float delta)
        {
            for (var i = 0; i < ages.Length; i++) ages[i] = Mathf.Min(Idle, ages[i] + delta);
        }

        void Advance(float delta)
        {
            Grow(blockPop, delta);
            Grow(blockFlash, delta);
            Grow(spoolArrive, delta);
            Grow(spoolPunch, delta);
            Grow(slotPop, delta);
            Grow(slotFlash, delta);
            dragonAge = Mathf.Min(Idle, dragonAge + delta);
            zoomAge = Mathf.Min(Idle, zoomAge + delta);
        }

        void Play()
        {
            var strongest = -1;
            var strength = 0;
            for (var i = 0; i < events.Count; i++)
            {
                React(events[i]);
                var value = WoolHapticPattern.Strength(events[i].Kind);
                if (value <= strength) continue;
                strength = value;
                strongest = i;
            }
            if (strongest < 0 || haptics == null) return;
            WoolHapticPattern.Get(events[strongest].Kind, out var timings, out var amplitudes);
            haptics.Play(timings, amplitudes);
        }

        void React(WoolFeelEvent e)
        {
            if (owner == null) return;
            switch (e.Kind)
            {
                case WoolFeelKind.BlockLaunched:
                    Trigger(blockPop, e.Index);
                    Emit(puff, owner.BlockAt(e.Index) + Vector3.up * 0.3f, Color.white, 6, PuffSpeed, PuffSize, PuffLife);
                    break;
                case WoolFeelKind.BlockBlocked:
                    Trigger(blockFlash, e.Index);
                    shake.Add(BlockedTrauma);
                    break;
                case WoolFeelKind.SpoolArrived:
                    Trigger(spoolArrive, e.Index);
                    Emit(puff, WoolScene.SlotPosition(e.Index) + Vector3.up * 0.45f, Yarn(e.Color), 10, PuffSpeed, PuffSize, PuffLife);
                    shake.Add(ArriveTrauma);
                    break;
                case WoolFeelKind.YarnConsumed:
                    if (dragonAge >= dragonTime || dragonAmount <= EatPunch) Dragon(EatPunch, EatPunchTime);
                    Emit(puff, owner.UnitAt(e.Index) + Vector3.up * 0.3f, Yarn(e.Color), 4, PuffSpeed, PuffSize, PuffLife);
                    break;
                case WoolFeelKind.SpoolCompleted:
                    Trigger(spoolPunch, e.Index);
                    var top = WoolScene.SlotPosition(e.Index) + Vector3.up * 0.6f;
                    Emit(sparkle, top, Yarn(e.Color), 8, SparkleSpeed, SparkleSize, SparkleLife);
                    Emit(sparkle, top, Color.white, 8, SparkleSpeed, SparkleSize, SparkleLife);
                    shake.Add(CompleteTrauma);
                    break;
                case WoolFeelKind.SlotUnlocked:
                    Trigger(slotPop, e.Index);
                    Emit(sparkle, WoolScene.SlotPosition(e.Index) + Vector3.up * 0.3f, Color.white, 12, SparkleSpeed, SparkleSize, SparkleLife);
                    break;
                case WoolFeelKind.TrayFull:
                    for (var i = 0; game != null && i < game.OpenSlots; i++) Trigger(slotFlash, i);
                    break;
                case WoolFeelKind.Danger:
                    shake.Add(DangerTrauma);
                    break;
                case WoolFeelKind.Won:
                    zoomAge = 0;
                    for (var i = 0; i < 40; i++) Emit(sparkle, owner.KittenAt + Vector3.up, Yarn(i % 6), 1, WinSpeed, WinSize, WinLife);
                    break;
                case WoolFeelKind.Lost:
                    Dragon(LostPunch, LostPunchTime);
                    Emit(puff, owner.KittenAt + Vector3.up * 0.6f, LostOrange, 20, LostSpeed, LostSize, LostLife);
                    shake.Add(LostTrauma);
                    break;
            }
        }

        void Dragon(float amount, float time)
        {
            dragonAmount = amount;
            dragonTime = time;
            dragonAge = 0;
        }

        Color Yarn(int color)
        {
            var yarn = owner != null && owner.Art != null ? owner.Art.Yarn : null;
            return yarn != null && color >= 0 && color < yarn.Length && yarn[color] != null ? yarn[color].GetColor(BaseColor) : Color.white;
        }

        void Emit(ParticleSystem system, Vector3 position, Color color, int count, float speed, float size, float life)
        {
            if (system == null) return;
            for (var i = 0; i < count; i++)
            {
                seed++;
                var angle = seed * 2.39996f;
                var rise = 0.45f + 0.5f * Mathf.Repeat(seed * 0.618034f, 1);
                var direction = new Vector3(Mathf.Cos(angle), rise, Mathf.Sin(angle)).normalized;
                var emit = new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = direction * speed,
                    startColor = color,
                    startSize = size * (0.75f + 0.5f * Mathf.Repeat(seed * 0.381966f, 1)),
                    startLifetime = life,
                    rotation = Mathf.Repeat(angle * Mathf.Rad2Deg, 360),
                    applyShapeToPosition = false
                };
                system.Emit(emit, 1);
            }
        }

        void Apply(float x, float y, float roll)
        {
            if (view == null) return;
            view.transform.SetPositionAndRotation(cameraPosition + cameraRotation * new Vector3(x, y, 0), cameraRotation * Quaternion.AngleAxis(roll, Vector3.forward));
            view.orthographicSize = cameraSize * (1 - CameraZoom);
        }

        void Restore()
        {
            if (view == null) return;
            view.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            view.orthographicSize = cameraSize;
        }
    }
}
```

- [ ] **Bước 4: `WoolScene` — field**

**Trước**
```41:43:Assets/JamJam/Runtime/WoolScene.cs
        readonly List<Mesh> roadMeshes = new List<Mesh>();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock blockTint;
```

**Sau**
```csharp
        readonly List<Mesh> roadMeshes = new List<Mesh>();
        readonly List<float> blockFlashShown = new List<float>();
        readonly List<float> slotFlashShown = new List<float>();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock blockTint;
        WoolFeel feel;
```

- [ ] **Bước 5: `WoolScene.LoadLevel` — tạo/khởi động `WoolFeel`**

**Trước**
```95:99:Assets/JamJam/Runtime/WoolScene.cs
            blockViews.Clear(); units.Clear(); spoolViews.Clear(); spoolLabels.Clear(); threads.Clear(); slots.Clear(); confetti.Clear(); confettiVelocity.Clear();
            CurrentLevel = Mathf.Clamp(index, 0, Levels.Length - 1);
            var level = Levels[CurrentLevel];
            gameCamera=Camera.main;
            State = new WoolGame(level.Data);
```

**Sau**
```csharp
            blockViews.Clear(); units.Clear(); spoolViews.Clear(); spoolLabels.Clear(); threads.Clear(); slots.Clear(); confetti.Clear(); confettiVelocity.Clear();
            blockFlashShown.Clear(); slotFlashShown.Clear();
            CurrentLevel = Mathf.Clamp(index, 0, Levels.Length - 1);
            var level = Levels[CurrentLevel];
            gameCamera=Camera.main;
            State = new WoolGame(level.Data);
            if (feel == null) feel = gameObject.AddComponent<WoolFeel>();
            feel.Begin(this, gameCamera);
            feel.Restart(State);
```

- [ ] **Bước 6: `WoolScene.LoadLevel` — sổ flash cho block và slot**

**Trước**
```126:127:Assets/JamJam/Runtime/WoolScene.cs
                go.transform.position = BlockPosition(data, level.Data);
                blockViews.Add(view);
```

**Sau**
```csharp
                go.transform.position = BlockPosition(data, level.Data);
                blockViews.Add(view);
                blockFlashShown.Add(0);
```

**Trước**
```132:133:Assets/JamJam/Runtime/WoolScene.cs
                slot.transform.position = SlotPosition(i); slot.Owner = this; slot.Index = i;
                slots.Add(slot);
```

**Sau**
```csharp
                slot.transform.position = SlotPosition(i); slot.Owner = this; slot.Index = i;
                slots.Add(slot);
                slotFlashShown.Add(0);
```

- [ ] **Bước 7: `WoolScene.Select` — click bị chặn + accessor vị trí**

**Trước**
```169:170:Assets/JamJam/Runtime/WoolScene.cs
            if (!State.Select(index) && index >= 0 && index < blockViews.Count) blockViews[index].Shake = 0.35f;
        }
```

**Sau**
```csharp
            if (State.Select(index) || index < 0 || index >= blockViews.Count) return;
            blockViews[index].Shake = 0.35f;
            feel.Blocked(index);
        }

        public Vector3 BlockAt(int index) { return index >= 0 && index < blockViews.Count ? blockViews[index].transform.position : kittenRest; }
        public Vector3 UnitAt(int index) { return index >= 0 && index < units.Count ? units[index].position : kittenRest; }
        public Vector3 KittenAt => kittenRest;
```

- [ ] **Bước 8: `WoolScene.Tick`**

**Trước**
```189:190:Assets/JamJam/Runtime/WoolScene.cs
            State.Step(delta);
            RenderFrame(delta);
```

**Sau**
```csharp
            State.Step(delta);
            feel.Step(State, delta);
            RenderFrame(delta);
```

- [ ] **Bước 9: `RenderFrame` — block bay: nhấc cung + pop**

**Trước**
```223:224:Assets/JamJam/Runtime/WoolScene.cs
                    view.transform.position=new Vector3(point.X,0.12f,point.Z);
                    view.transform.localScale=Vector3.one*WoolRouting.ScaleAt(distance);
```

**Sau**
```csharp
                    view.transform.position=new Vector3(point.X,0.12f+feel.BlockLift(i),point.Z);
                    view.transform.localScale=Vector3.one*(WoolRouting.ScaleAt(distance)*feel.BlockScale(i));
```

- [ ] **Bước 10: `RenderFrame` — chớp đỏ block** (chỉ gọi `SetPropertyBlock` khi giá trị đổi; `Arc` trả đúng 0 ở cuối nên màu về đúng `Yarn_i`)

**Trước**
```236:238:Assets/JamJam/Runtime/WoolScene.cs
                    view.transform.position = start + Vector3.right * Mathf.Sin(view.Shake * 95) * view.Shake * 0.26f;
                }
            }
```

**Sau**
```csharp
                    view.transform.position = start + Vector3.right * Mathf.Sin(view.Shake * 95) * view.Shake * 0.26f;
                }
                var flash = feel.BlockFlash(i);
                if (flash == blockFlashShown[i]) continue;
                blockFlashShown[i] = flash;
                blockTint ??= new MaterialPropertyBlock();
                blockTint.SetColor(BaseColor, Color.Lerp(Art.Yarn[block.Definition.Color].GetColor(BaseColor), WoolFeel.FlashRed, flash));
                view.Body.GetComponent<Renderer>().SetPropertyBlock(blockTint);
            }
```

- [ ] **Bước 11: `RenderFrame` — rồng phồng, pad pop + chớp khay đầy**

**Trước**
```262:267:Assets/JamJam/Runtime/WoolScene.cs
            dragon.rotation=delta==0?headRotation:Quaternion.RotateTowards(dragon.rotation,headRotation,240*delta);
            for (var i = 0; i < spoolViews.Count; i++)
            {
                var spool = State.Spools[i];
                slots[i].Label.gameObject.SetActive(i >= State.OpenSlots);
                slots[i].GetComponent<Renderer>().sharedMaterial = i < State.OpenSlots ? Art.Pad : Art.PadLocked;
```

**Sau**
```csharp
            dragon.rotation=delta==0?headRotation:Quaternion.RotateTowards(dragon.rotation,headRotation,240*delta);
            dragon.localScale=Vector3.one*feel.DragonScale;
            for (var i = 0; i < spoolViews.Count; i++)
            {
                var spool = State.Spools[i];
                slots[i].Label.gameObject.SetActive(i >= State.OpenSlots);
                var pad = slots[i].GetComponent<Renderer>();
                pad.sharedMaterial = i < State.OpenSlots ? Art.Pad : Art.PadLocked;
                slots[i].transform.localScale = Vector3.one * feel.SlotScale(i);
                var padFlash = feel.SlotFlash(i);
                if (padFlash != slotFlashShown[i])
                {
                    slotFlashShown[i] = padFlash;
                    if (padFlash <= 0) pad.SetPropertyBlock(null);
                    else
                    {
                        blockTint ??= new MaterialPropertyBlock();
                        blockTint.SetColor(BaseColor, Color.Lerp(Art.Pad.GetColor(BaseColor), WoolFeel.FlashRed, padFlash));
                        pad.SetPropertyBlock(blockTint);
                    }
                }
```

- [ ] **Bước 12: `RenderFrame` — spool pop khi tới / punch khi xong**

**Trước**
```275:275:Assets/JamJam/Runtime/WoolScene.cs
                spoolViews[i].localScale = Vector3.one * finish;
```

**Sau**
```csharp
                spoolViews[i].localScale = Vector3.one * (finish * feel.SpoolScale(i));
```

- [ ] **Bước 13: Compile** — `unity recompile`. Kỳ vọng 0 lỗi (RED Bước 2 hết), không cảnh báo mới (đặc biệt không có cảnh báo message MonoBehaviour có tham số).

- [ ] **Bước 14: GREEN Play Mode** (`WoolJam.unity` mở, Game view hiển thị). **[PLAY START]**, rồi lần lượt (mỗi dòng một eval; level phải nạp ở eval riêng trước check):

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(2); return "ok";'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.PlayFeel();'
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(0); return "ok";'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.PlayFinish(true);'
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(0); return "ok";'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.PlayFinish(false);'
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(2); return "ok";'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.PlayBlocks();'
```
Kỳ vọng: `PLAY FEEL PASS / blocked … / launched …`, `PLAY FINISH PASS / Won`, `PLAY FINISH PASS / Lost`, `PLAY BLOCKS PASS / 24 blocks`. **[CONSOLE ERRORS]** không có lỗi mới. **[PLAY STOP]**.

- [ ] **Bước 15: Dừng, báo user.**

---

### Task 5: Kiểm thử tổng, ảnh, hiệu năng, tinh chỉnh

**Files:** không sửa source ngoài các hằng ở bảng knob (nếu cần). Tạo `docs/feel-launch.png`, `docs/feel-complete.png`, `docs/feel-won.png`.

**Interfaces — Consumes:** mọi thứ Task 1–4.

**Knob được phép chỉnh** (không đụng `WoolGame`, `WoolRouting`, level, route, collider):

| Nơi | Hằng |
|---|---|
| `WoolFeel` | `LaunchPop`, `LaunchPopTime`, `FlightLift`, `BlockedFlash`, `BlockedFlashTime`, `ArrivePopTime`, `CompletePunch`, `CompletePunchTime`, `SlotPop`, `SlotPopTime`, `TrayFlash`, `TrayFlashTime`, `EatPunch`, `EatPunchTime`, `LostPunch`, `LostPunchTime`, `WinZoom`, `WinZoomTime`, `*Trauma`, `Puff*`, `Sparkle*`, `Win*`, `Lost*` |
| `WoolShake` | `MaxOffset`, `MaxRoll`, `Frequency`, `Decay` (giữ `Decay ≥ 1.57` vì harness kiểm trauma 1 về 0 trong 40 frame 0.016s; `PlayFeel` cần trauma 0.18 hết trong 1.5s) |
| `WoolFeelTracker` | `DangerRatio`, `DangerRearm` (giữ `DangerRearm < DangerRatio`) |
| `WoolAssetsBuilder.MakeFeel` | đối số `MakeParticles` (sống, cỡ, gravity, drag); sau khi đổi chạy lại `Rebuild Art` |

- [ ] **Bước 1: Hồi quy gameplay**

```bash
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolEditorChecks.ValidateLevels(); return System.IO.File.ReadAllText("Logs/WoolJam.verify.txt");'
md5sum -c "$SCRATCH/feel-run/logic.md5"
md5sum -c "$SCRATCH/feel-run/levels.md5"
```
Kỳ vọng: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; 104 feel assertions`; file verify có `LEVEL 1/2/3 SOLUTION PASS`; `WoolGame.cs`, `WoolRouting.cs`, level đều `OK`.

- [ ] **Bước 2: `Rebuild Art` idempotent** (thoát Play Mode, lưu scene)

```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtBuilder.Rebuild(); return "rebuilt";'
md5sum Assets/Scenes/WoolJam.unity Assets/JamJam/Generated/Prefabs/Feel*.prefab Assets/JamJam/Generated/Materials/Feel*.mat Assets/JamJam/Generated/Textures/SoftDot.png Assets/JamJam/Generated/Textures/Star.png Assets/JamJam/Generated/WoolArt.asset > "$SCRATCH/feel-run/rebuild1.md5"
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtBuilder.Rebuild(); return "rebuilt";'
md5sum -c "$SCRATCH/feel-run/rebuild1.md5"
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor --include=*.meta | sort > "$SCRATCH/feel-run/guids.now.txt"
comm -23 "$SCRATCH/feel-run/guids.before.txt" "$SCRATCH/feel-run/guids.now.txt"
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.Feel();'
```
Kỳ vọng: mọi dòng `md5sum -c` là `OK`; `comm` rỗng; `FEEL PASS …`.

- [ ] **Bước 3: Hiệu năng (không có hạt)** — **[PLAY START]**, eval `LoadLevel(1)`, đợi ~1 giây, eval `UnityStats` như Task 1 Bước 0. Ghi nhãn `final L2` vào `drawcalls.txt`. Kỳ vọng `batches ≤ 332` (302 × 1.1) và không vượt baseline `before L2` quá 10%. Nếu vượt: kiểm hai hệ hạt không có hạt sống (`PuffCount`, `SparkleCount` = 0) và pad không giữ MPB (`slotFlashShown` về 0), rồi báo user.

- [ ] **Bước 4: Ảnh 1080×1920** (vẫn Play Mode; `Time.timeScale` chậm để bắt khoảnh khắc)

Block bay (lift + puff):
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(1); UnityEngine.Time.timeScale=0.1f; for(var i=0;i<s.State.Blocks.Count;i++) if(s.State.CanSelect(i)){s.Select(i);break;} return "launched";'
```
Đợi ~2 giây thật, **[CAPTURE 1080×1920 → docs/feel-launch.png]**.

Spool hoàn thành (sparkle):
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Time.timeScale=1f; var g=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().State; for(var f=0;f<30000&&g.Status==JamJam.RoundStatus.Playing;f++){ var near=false; foreach(var sp in g.Spools) if(sp.Block>=0&&sp.Arrival<=0&&sp.Remaining==1&&sp.Target>=0) near=true; if(near) break; for(var b=0;b<g.Blocks.Count;b++) if(g.CanSelect(b)&&g.HasVisibleColor(g.Blocks[b].Definition.Color)){g.Select(b);break;} g.Step(0.02f);} UnityEngine.Time.timeScale=0.1f; return g.Status.ToString();'
```
Kỳ vọng `Playing`. Đợi ~2.5 giây thật, **[CAPTURE 1080×1920 → docs/feel-complete.png]**; nếu ảnh chưa có sparkle, chụp lại sau 1 giây.

Thắng (sparkle quanh mèo + zoom):
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Time.timeScale=1f; var g=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().State; for(var f=0;f<30000&&g.Status==JamJam.RoundStatus.Playing;f++){ for(var b=0;b<g.Blocks.Count;b++) if(g.CanSelect(b)&&g.HasVisibleColor(g.Blocks[b].Definition.Color)){g.Select(b);break;} g.Step(0.02f);} UnityEngine.Time.timeScale=0.25f; return g.Status.ToString();'
```
Kỳ vọng `Won`. Đợi ~1 giây thật, **[CAPTURE 1080×1920 → docs/feel-won.png]**. Sau đó:
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEngine.Time.timeScale=1f; return "restored";'
```
**[PLAY STOP]**. Đọc ba ảnh bằng Read: block bay phải nhấc khỏi mặt bàn có vệt puff trắng; ảnh complete có sao lấp lánh màu len trên khay; ảnh won có sparkle nhiều màu quanh mèo và khung hình hơi zoom. Nếu yếu/lố, chỉnh knob ở bảng trên, chạy lại Task 4 Bước 14 và Bước 4 này.

- [ ] **Bước 5: Kiểm cuối**

```bash
git status --short
grep -rn "//\|/\*" Assets/JamJam/Runtime/WoolFeel*.cs Assets/JamJam/Runtime/WoolEase.cs Assets/JamJam/Runtime/WoolShake.cs Assets/JamJam/Runtime/WoolHaptic*.cs Assets/JamJam/Runtime/IWoolHaptics.cs Assets/JamJam/Editor/WoolArtChecks.*Feel.cs Tools/WoolJamChecks/FeelChecks.cs
```
Kỳ vọng: `git status` không có file bị xóa (` D`), không có thay đổi ở `WoolGame.cs`, `WoolRouting.cs`, `Generated/Levels`; `grep` rỗng. **[CONSOLE ERRORS]** không có lỗi mới.

- [ ] **Bước 6: Dừng, báo user** — gửi ba ảnh `docs/feel-*.png`, bảng `drawcalls.txt`, danh sách knob đã chỉnh (nếu có). Không commit.

## Tóm tắt

- Logic thuần mới (không UnityEngine) được harness .NET kiểm bằng level `WoolGame` thật: `WoolFeelTracker` dò 10 loại sự kiện bằng so snapshot (đúng các chuyển trạng thái `Select`/`Step`: Arrival về đúng 0, Consumed trong `Step`, Won cùng `Step` với consume cuối, `Block` về -1 sau Finish, slot tái dùng, Danger có trễ 0.8/0.75), `WoolEase` (OutBack/Punch/Arc), `WoolShake` (trauma², giảm 1.6/s, offset ≤ 0.35, roll ≤ 2°, 18Hz, tất định), `WoolHapticPattern` theo bảng spec. Dòng PASS mới thêm `104 feel assertions`.
- Haptic qua `IWoolHaptics`: `WoolHaptics.Create(bool, out IWoolHaptics)` trả bản rỗng trong Editor/khi tắt rung, bản Android dùng `VibrationEffect.createWaveform` (SDK ≥ 26) hoặc `vibrate(ms)`, nuốt lỗi JNI; nhánh Android được compile thử ngoài Unity.
- `WoolAssetsBuilder` sinh `SoftDot.png`, `Star.png`, `FeelPuff.mat` (alpha), `FeelSparkle.mat` (additive), prefab `FeelPuff`/`FeelSparkle` (emit từ code, World space, 256 hạt), gán `WoolArt.Puff/Sparkle`; chạy qua `JamJam/Rebuild Art`, idempotent, GUID giữ nguyên.
- `WoolFeel` (thêm lúc `LoadLevel`, chạy trong `Tick` có sẵn) phát pop/nảy, nhấc cung khi bay, chớp đỏ MPB, Puff/Sparkle đúng màu len, rung + zoom camera về đúng gốc, haptic một mẫu mỗi frame theo `WoolSettings.Vibration`; `WoolScene` chỉ nhân hệ số vào scale/position hiện có. Không đổi luật chơi, level, route, FIFO, `WoolGame`, `WoolRouting`.
- Kiểm thử: harness RED→GREEN, `Feel()` RED→GREEN, `PlayFeel()`/`PlayFinish(true|false)` trong Play Mode (camera về gốc < 1e-4, Replay giữa lúc rung), `PlayBlocks`, `ValidateLevels`, idempotent hai lần, batches ≤ 332, ảnh `docs/feel-*.png`.
