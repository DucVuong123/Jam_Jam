# Khối nhỏ có khe, chạy thẳng ra theo mũi tên, vòng ngoài bảng, nhiều khối chạy cùng lúc

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** khối trên bảng nhỏ lại (ô 0.8) và có khe; bấm khối hợp lệ thì nó chạy thẳng theo mũi tên ra khỏi bảng ở nguyên kích thước, vòng theo vành đai ngoài bảng tới slot của nó; nhiều khối chạy song song, không còn hàng đợi FIFO một khối một lúc.

**Kiến trúc:** logic thuần (`WoolRouting`, `WoolGame`, không UnityEngine) đổi luật: `CellSize` 0.8 cho mọi chuyển đổi ô → thế giới; `TryFind` dựng đường hình học (đoạn thẳng ra theo mũi tên → vành đai ngoài bảng → làn trên → slot) thay cho BFS co nhỏ; `Step` cho mọi khối Phase 1 cùng tiến, mỗi spool có `Arrival` riêng; `HasClearArrowLane` tính khối đang chạy ở vị trí hiện tại, nguyên kích thước. Harness .NET viết lại các check FIFO/scale/BFS (TDD). Phần hiển thị: `WoolScene` bỏ scale, xoay cả khối theo hướng chạy, collider theo kích thước hiển thị, đệm bảng co theo level; `WoolBlockMesh` inset 0.09 ô; `WoolFeel` bỏ nhấc cung, pop 1.08; builder chỉ sửa code rồi chạy `Rebuild Art`.

**Tech Stack:** Unity 6000.3.19f1, URP 17.3, C# 9 (LangVersion 9.0), R3 1.3.1, harness .NET 10 (`Tools/WoolJamChecks`), Python + PIL (ảnh ghép).

**Spec:** `docs/superpowers/specs/2026-11-08-wool-block-motion-design.md`

## Ràng buộc chung

- Luật mới thay luật đã khóa (nudge 0.5 ô + scale 0.45, FIFO một khối). **Được sửa** `WoolGame.cs`, `WoolRouting.cs`. **Không sửa** level asset `Assets/JamJam/Generated/Levels/*.asset`; giữ mọi `.meta`/GUID.
- `WoolRouting.CellSize` = **0.8**; inset mesh **0.09 ô** mỗi cạnh; `Speed` = **9**; `LaunchPop` 1.18 → **1.08**; bỏ `FlightLift`.
- File logic thuần (`WoolRouting`, `WoolGame`) không `using UnityEngine` (harness compile trực tiếp).
- Không comment trong code (`//`, `///`, `/* */`, XML doc). Method trả reference dùng interface/abstract; factory `void Create(..., out IThing x)`. Timer chỉ R3 (không thêm timer mới: mọi thứ chạy trong `WoolScene.Tick`/`RenderFrame` trên `Observable.Interval` sẵn có).
- C# 9: không file-scoped namespace, không global using, không record struct.
- KHÔNG chạy `WoolSceneBuilder.Build` hay `WoolEditorChecks.RunBatch`. Sinh lại asset chỉ bằng `JamJam.Editor.WoolArtBuilder.Rebuild()` (menu `JamJam/Rebuild Art`; phải thoát Play Mode và lưu mọi scene đang mở). Plan này không đổi UI nên không cần `WoolUiBuilder.Rebuild()`.
- Không `git add`/`git commit`; mỗi task kết thúc bằng bước kiểm tra rồi "Dừng, báo user".
- Số dòng trong header **Trước** là của file hiện tại (2026-11-08) trước khi bắt đầu plan; khi một file đã bị sửa ở bước/task trước, tìm theo nội dung Trước.
- Lệnh Unity: `unity recompile` (không thêm cờ `--caller`; nếu trả HTTP 400, xác nhận bằng eval `return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;` → `False False`). Eval: `unity command eval --caller plugin --skill unity-cli --no-banner '<C# statements; return value;>'` (viết tắt `E '...'`). Vào/thoát Play Mode: `unity command start --caller plugin --skill unity-cli` / `unity command stop --caller plugin --skill unity-cli` (**[PLAY START]**/**[PLAY STOP]**). Chụp: `unity command game_view --caller plugin --skill unity-cli --source camera --overlay true --width W --height H --output-path P` (**[CAPTURE W×H → P]**). Lỗi console: `unity command get --caller plugin --skill unity-cli --level error --tail 50` (**[CONSOLE ERRORS]**).
- Harness: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`. Dòng PASS hiện tại: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; 108 feel assertions`. Dòng PASS mới: `PASS: 105 gameplay assertions; 13011 route assertions; 18 progress assertions; 108 feel assertions`.
- Biến shell (Git Bash): `SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"`, `PY="$USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe"` (luôn chạy `"$PY" -I`).

## Quyết định (spec để ngỏ hoặc phải lệch nhẹ)

| Chủ đề | Quyết định |
|---|---|
| Vành đai | Spec: "hình chữ nhật bao bảng giãn `ExitMargin`". Vành đai là đường đi của **tâm** khối nên mỗi khối dùng độ giãn `RingOffset = ExitMargin + max(W,H)·CellSize/2` (`ExitMargin` = 0.3). Nhờ vậy điểm cuối đoạn thẳng nằm đúng trên vành đai, khối nguyên kích thước đã ra khỏi mép bảng ít nhất `ExitMargin`, và sau khi xoay 90° ở góc vẫn không chạm bảng. Ba level hiện có toàn khối 2×1/1×2 nên mọi khối dùng chung một vành đai (giãn 1.1). |
| Đoạn thẳng ra | Kết thúc trên vành đai: Up → `z = top`, Down → `z = bottom`, Left/Right → `x = left/right`. `ExitDistance` = độ dài đoạn này. Điểm cuối đoạn được giữ làm một điểm của route kể cả khi đoạn kế tiếp thẳng hàng (Up đi tiếp lên làn trên). |
| Hướng ngắn hơn | Left/Right luôn đi lên (vòng xuống luôn dài hơn). Down so tổng `(cx−left)+|slotX−left|` với `(right−cx)+|right−slotX|`, bằng nhau thì đi trái. Up đi thẳng lên làn trên. |
| Làn trên | `LaneZ = TrayZ − 0.8 = −1.55`. Nếu cạnh trên vành đai cao hơn `LaneZ` (bảng ≥ 7 hàng, không có trong level hiện tại) thì làn trên = cạnh trên vành đai để không cắt bảng. |
| Kiểm làn khi chọn | Khối Phase 1 được đặt ở vị trí hiện tại, nguyên kích thước, theo hướng ban đầu (đoạn thẳng ra không xoay; khi đã lên vành đai thì nằm ngoài bảng và bị cắt bỏ). Đúng spec: chỉ vị trí hiện tại, không dự đoán — hai khối có làn cắt nhau có thể chồng nhẹ khi chạy (spec chấp nhận). |
| `Speed` | **9**. Đo trên 3 level (47 khối × 6 slot, không vật cản): xa nhất 17.92 đơn vị (Level 3, khối 0 mũi tên Down → slot 5) = **1.99 s** ≤ 2.5 s; Level 1 tối đa 13.28 (1.48 s), Level 2 16.20 (1.80 s). Muốn chậm hơn vẫn giữ mục tiêu thì `Speed ≥ 7.2`. Ngắn nhất 4.93 đơn vị (0.55 s). |
| Độ dày | Giữ `Thickness` 0.35 tuyệt đối (không nhân 0.8) để mũi tên, bóng, MPB không đổi. |
| Mesh | `Inset` đổi nghĩa: 0.04 đơn vị → **0.09 ô** (`halfX = (W/2 − Inset)·CellSize`; khe giữa hai khối kề = 0.144). `Corner` 0.22 → 0.18, `Border` 0.12 → 0.10 (≈ ×0.8), `Bevel` 0.06 giữ. Vân len theo UV thế giới nên mật độ không đổi. |
| Mũi tên | Rộng `0.56·CellSize`, dài `0.78·len·CellSize`; prefab `ArrowBlock` (preview 2×1) `0.56·CellSize × 1.56·CellSize`. |
| Collider | `((W − 2·Inset)·CellSize, 0.55, (H − 2·Inset)·CellSize)` = đúng kích thước hiển thị. |
| Board cushion | `WoolScene.Cushion` (Transform) do `Rebuild Art` gán (`WoolArtBuilder.Rebuild` tìm "Board Cushion"); `WoolSceneBuilder` chỉ sửa code để một lần Build đầy đủ cũng gán. `LoadLevel` đặt `localScale = (C·0.8 + 0.5, y giữ, R·0.8 + 0.5)` — "lề 0.5" hiểu là cộng 0.5 vào mỗi chiều (0.25 mỗi bên). Mesh `Rounded` là khối 1×1×1 nên scale = kích thước. |
| Xoay theo hướng | Xoay **cả** gốc khối (thân + mũi tên con + collider): yaw = `atan2(dir.x, dir.z) − Direction·90`, `RotateTowards` 1080°/s (90° trong ~0.08 s), `delta == 0` thì gán thẳng. Mũi tên con không phải cập nhật mỗi frame nữa. |
| Game feel | Xóa `FlightLift` và API `BlockLift(int)`; `LaunchPop` 0.18 → 0.08 (đỉnh 1.08). Puff ở vị trí xuất phát giữ nguyên. |
| FeelChecks | Chạy lại nguyên trạng với luật song song vẫn PASS 108 — không cần sửa: các check chỉ dựa vào chuyển trạng thái `Arrival` về 0 của từng spool và `Phase`, không dựa vào thứ tự FIFO giữa hai khối đang chạy. |
| Đếm assertion | Gameplay 153 → 105 (bỏ các vòng lặp FIFO kiểm từng frame "đứng yên chờ khối trước", thêm check song song/chặn bởi khối chạy/pause). Route 4121 → 13011 (mỗi route lấy mẫu mỗi 0.0625 trên đường vòng dài hơn). |
| `ValidateLevels` | Không đổi code; solver chọn ≤ 1 khối mỗi frame nên tự nhiên có nhiều khối chạy cùng lúc (đo được tối đa 4). Kết quả đo trên harness: Level 1 39 units 4.52 s, Level 2 75 units 9.00 s, Level 3 120 units 14.74 s, cả ba `Won`. |
| `RunBatch`/`InspectMotion` | Không chạy; vẫn compile và vẫn đúng (khối phẳng ở y 0.12, đoạn thẳng ra không xoay nên `Arrow.up` vẫn trùng hướng mũi tên). |
| Check Play Mode mới | Partial `WoolArtChecks.PlayMotion.cs`: `PlayMotionArm(int level)` (nạp level, kiểm đệm bảng, chọn liên tiếp 2 khối, `timeScale` 0.1), `PlayMotionMid()` (cùng `Travel`, đúng điểm route, y 0.12, scale 1, chỉ xoay quanh trục đứng), `PlayMotionEnd()` (tới đúng slot, trả `timeScale` 1). Chỉ số khối giữ trong static field giữa các eval. |
| Khung hình | `Stage` thay 4 điểm góc cố định bằng mọi điểm route (47 khối × 6 slot) của 3 level. Điểm vành đai thấp nhất (Level 3, z −8.55) ở viewport y ≈ 0.128 > 0.12, nên `ExitMargin` không được tăng quá 0.3 nếu không sửa camera. |
| Ảnh | `docs/motion-start.png`, `docs/motion-start-tall.png` (đầu Level 3), `docs/motion-run.png` (3 khối đang chạy), `docs/motion-compare.png` (ghép với `ref/yt2/oar2.jpg`). |

## Review Focus

- Hai khối được chọn liên tiếp trong cùng frame rồi chạy song song, có lúc chồng nhau trên vành đai → cả hai vẫn tới đúng slot, mỗi spool đếm `Arrival` riêng, không khối nào bị đứng chờ. Test `Both departures" --include move in the same Step` + `Spool arrivals advance independently every Step` (Task 1) và `PlayMotionArm/Mid/End` (Task 3).
- Khối đang chạy cắt ngang làn mũi tên của khối khác → khối kia không chọn được; khi khối chạy đã qua (vẫn chưa tới khay) → chọn được. Test `CheckMovingBlocker` (Task 1).
- Pause khi nhiều khối đang chạy → mọi khối đứng yên, Resume thì cùng chạy tiếp. Test `Pause freezes every moving block` trong `CheckStaggered` (Task 1).
- Khối mũi tên Down ở hàng dưới cùng: phải chọn đúng phía vòng ngắn hơn theo slot, và vành đai dưới vẫn nằm trong khung hình 1080×2400. Test `Down arrow takes the right side when slot 5 makes it shorter` (Task 1) và `Stage()` với mọi route (Task 2).
- Retry/LoadLevel giữa lúc khối đang chạy hoặc sang level kích thước khác → khối mới không xoay, đệm bảng đúng kích thước level mới. `PlayMotionArm(2)` gọi `LoadLevel` sau các check khác và kiểm đệm bảng (Task 3).

## Bản đồ file

| File | Việc |
|---|---|
| `Tools/WoolJamChecks/RouteChecks.cs` | Sửa — check hình học route mới (T1) |
| `Tools/WoolJamChecks/Program.cs` | Sửa — check song song, chặn bởi khối chạy, pause (T1) |
| `Assets/JamJam/Runtime/WoolRouting.cs` | Sửa — `CellSize`, `ExitMargin`, `LaneZ`, `RingOffset`, `TryFind` mới; bỏ `ArrowDistance`, `TravelScale`, `ScaleAt` (T1) |
| `Assets/JamJam/Runtime/WoolGame.cs` | Sửa — bỏ `departures" --include`, Step song song, làn theo `CellSize` (T1) |
| `Assets/JamJam/Runtime/WoolScene.cs` | Sửa — RenderFrame Phase 1 (T1); `Cushion`, `CushionPad`, mũi tên, collider, đệm bảng (T2) |
| `Assets/JamJam/Runtime/WoolBlockMesh.cs` | Sửa — inset/bo góc (T2) |
| `Assets/JamJam/Runtime/WoolFeel.cs` | Sửa — bỏ nhấc cung, pop 0.08 (T2) |
| `Assets/JamJam/Editor/WoolAssetsBuilder.cs` | Sửa — scale mũi tên prefab (T2) |
| `Assets/JamJam/Editor/WoolArtBuilder.cs` | Sửa — gán `WoolScene.Cushion` khi Rebuild Art (T2) |
| `Assets/JamJam/Editor/WoolSceneBuilder.cs` | Sửa — gán `Cushion` trong Build (chỉ code, không chạy) (T2) |
| `Assets/JamJam/Editor/WoolArtChecks.Blocks.cs`, `.Play.cs`, `.PlayFeel.cs`, `.Stage.cs` | Sửa — kỳ vọng mới (T2) |
| `Assets/JamJam/Editor/WoolArtChecks.PlayMotion.cs` | Tạo — check Play Mode song song (T2, chạy ở T3) |
| Asset sinh lại tại chỗ | `Generated/Meshes/KnitBlock.asset`, `Generated/Prefabs/ArrowBlock.prefab`, `Assets/Scenes/WoolJam.unity` (field `Cushion`) và các asset `Rebuild Art` vốn ghi lại (T2) |
| Ảnh | `docs/motion-start.png`, `docs/motion-start-tall.png`, `docs/motion-run.png`, `docs/motion-compare.png` (T3) |

Đã xác minh `WoolArtChecks.PlayMotion.cs` chưa tồn tại (2026-11-08). Toàn bộ code trong plan đã được thử ngoài project: harness + logic thuần (bản copy, `LangVersion 9.0`) chạy ra đúng dòng PASS mới ở trên và RED đúng như Task 1 Bước 2 khi chạy với runtime cũ; runtime + editor compile sạch (0 lỗi, 0 cảnh báo) với DLL Unity 6000.3.19f1 ở ba trạng thái: sau Task 1, giữa Task 2 (RED đúng như Task 2 Bước 2), sau Task 2. Mọi khối **Trước** đã so khớp từng dòng với file thật.

---

### Task 1: Luật mới trong logic thuần + harness (TDD)

**Files:**
- Modify: `Tools/WoolJamChecks/RouteChecks.cs:33-82`, `:100-114`, `:144-154`
- Modify: `Tools/WoolJamChecks/Program.cs:72-84`, `:90-119`, `:133`, `:142`, `:145`, `:202-211`
- Modify: `Assets/JamJam/Runtime/WoolRouting.cs:16-21`, `:25-29`, `:36-117`
- Modify: `Assets/JamJam/Runtime/WoolGame.cs:74-75`, `:158-166`, `:189-190`, `:229-239`
- Modify: `Assets/JamJam/Runtime/WoolScene.cs:267-280`

**Interfaces — Produces:**
- `WoolRouting`: `const float Speed = 9f`, `CellSize = 0.8f`, `ExitMargin = 0.3f`, `BoardCenterZ = -5.05f`, `TrayZ = -0.75f`, `LaneZ = TrayZ - 0.8f`, `SlotSpacing = 1.27f`; `RoutePoint BlockCenter(BlockDefinition, LevelDefinition)` (đơn vị thế giới, đã nhân `CellSize`); `RoutePoint SlotCenter(int)`; `float RingOffset(BlockDefinition)`; `bool TryFind(LevelDefinition, IReadOnlyList<BlockState>, int index, int slot, out IReadOnlyList<RoutePoint> points, out float exitDistance)`; `float Length(...)`, `void Evaluate(...)` giữ nguyên. **Không còn** `ArrowDistance`, `TravelScale`, `ScaleAt`.
- `WoolGame`: API công khai giữ nguyên (`CanSelect`, `Select`, `Step`, `Blocks`, `Spools`…); hành vi: mọi `BlockState` Phase 1 tiến mỗi `Step`, `Spools[slot].Arrival = max(0, Duration − Travel)` của chính khối đó.
- `WoolScene.RenderFrame`: khối Phase 1 ở `(point.X, 0.12, point.Z)`, scale `feel.BlockScale(i)`, xoay yaw theo hướng chạy.

- [ ] **Bước 0: Baseline (trước mọi thay đổi)**

```bash
SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"
mkdir -p "$SCRATCH/motion-run"
cd "/d/Unity Project/Jam_Jam"
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor Assets/Scenes --include=*.meta | sort > "$SCRATCH/motion-run/guids.before.txt"
md5sum Assets/JamJam/Generated/Levels/*.asset > "$SCRATCH/motion-run/levels.md5"
test ! -e Assets/JamJam/Editor/WoolArtChecks.PlayMotion.cs && echo "PlayMotion absent"
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
```
Kỳ vọng: `PlayMotion absent`; `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; 108 feel assertions`.

- [ ] **Bước 1: Viết test RED — `RouteChecks.cs`**

`Validate` mới: điểm đầu = tâm khối, điểm cuối = tâm slot, mọi đoạn Manhattan, đoạn đầu cùng hướng mũi tên đúng bằng `exit`, khối nguyên kích thước ra khỏi mép + `ExitMargin`, mọi điểm sau điểm đầu nằm ngoài hình chữ nhật bảng, vào slot thẳng từ làn trên, mọi mẫu sau `exit` cách bảng ≥ `RingOffset`, đoạn thẳng ra không chạm khối tĩnh (lấy mẫu nguyên kích thước).

**Trước**
```33:82:Tools/WoolJamChecks/RouteChecks.cs
    static void Validate(LevelDefinition level, IReadOnlyList<BlockState> blocks,
        IReadOnlyList<RoutePoint> route, float exit, int slot)
    {
        var block = blocks[0].Definition;
        var start = WoolRouting.BlockCenter(block, level);
        var end = WoolRouting.SlotCenter(slot);
        Check(route[0].X == start.X && route[0].Z == start.Z, "Exact block center start");
        Check(route[route.Count - 1].X == end.X && route[route.Count - 1].Z == end.Z, "Exact slot center end");
        for (var i = 1; i < route.Count; i++)
        {
            var dx = Math.Abs(route[i].X - route[i - 1].X);
            var dz = Math.Abs(route[i].Z - route[i - 1].Z);
            Check((dx < 0.0001f) != (dz < 0.0001f), "Every segment cardinal and nonzero");
        }
        var arrowX=block.Direction==ExitDirection.Right?1:block.Direction==ExitDirection.Left?-1:0;
        var arrowZ=block.Direction==ExitDirection.Up?1:block.Direction==ExitDirection.Down?-1:0;
        Check(Near(exit,0.5f), "Initial arrow move stops after half a cell before safe routing");
        WoolRouting.Evaluate(route, exit, out var goal, out _);
        Check(Near(goal.X,start.X+arrowX*exit) && Near(goal.Z,start.Z+arrowZ*exit),
            "Initial nudge follows the arrow before a turn");
        for (var distance = 0f; distance <= exit + 0.001f; distance += 0.125f)
        {
            WoolRouting.Evaluate(route, Math.Min(distance, exit), out var point, out _);
            Check(Near(point.X,start.X+arrowX*Math.Min(distance,exit)) && Near(point.Z,start.Z+arrowZ*Math.Min(distance,exit)),
                "Every point before board exit follows the original arrow");
            var x = point.X - (block.Width - level.Columns) * 0.5f;
            var y = point.Z - WoolRouting.BoardCenterZ - (block.Height - level.Rows) * 0.5f;
            for (var i = 1; i < blocks.Count; i++)
            {
                if (blocks[i].Phase != 0) continue;
                var other = blocks[i].Definition;
                var overlap = x < other.X + other.Width - 0.0001f && x + block.Width > other.X + 0.0001f &&
                    y < other.Y + other.Height - 0.0001f && y + block.Height > other.Y + 0.0001f;
                Check(!overlap, "Moving full rectangle avoids static blocks through board exit");
            }
        }
        for(var distance=exit;distance<=WoolRouting.Length(route);distance+=0.0625f)
        {
            WoolRouting.Evaluate(route,distance,out var point,out _);
            for(var i=1;i<blocks.Count;i++)
            {
                if(blocks[i].Phase!=0) continue;
                var other=blocks[i].Definition;
                var center=WoolRouting.BlockCenter(other,level);
                var overlap=Math.Abs(point.X-center.X)<((block.Width+0.02f)*0.45f+other.Width+0.02f)*0.5f-0.0001f &&
                    Math.Abs(point.Z-center.Z)<((block.Height+0.02f)*0.45f+other.Height+0.02f)*0.5f-0.0001f;
                Check(!overlap,"Every moving sprite segment avoids stationary sprites after its initial nudge");
            }
        }
    }
```

**Sau**
```csharp
    static float Outside(LevelDefinition level, RoutePoint point)
    {
        var halfX = level.Columns * WoolRouting.CellSize * 0.5f;
        var halfZ = level.Rows * WoolRouting.CellSize * 0.5f;
        var dx = Math.Abs(point.X) - halfX;
        var dz = Math.Abs(point.Z - WoolRouting.BoardCenterZ) - halfZ;
        return Math.Max(dx, dz);
    }

    static void Validate(LevelDefinition level, IReadOnlyList<BlockState> blocks,
        IReadOnlyList<RoutePoint> route, float exit, int slot)
    {
        var block = blocks[0].Definition;
        var start = WoolRouting.BlockCenter(block, level);
        var end = WoolRouting.SlotCenter(slot);
        Check(route[0].X == start.X && route[0].Z == start.Z, "Exact block center start");
        Check(route[route.Count - 1].X == end.X && route[route.Count - 1].Z == end.Z, "Exact slot center end");
        for (var i = 1; i < route.Count; i++)
        {
            var dx = Math.Abs(route[i].X - route[i - 1].X);
            var dz = Math.Abs(route[i].Z - route[i - 1].Z);
            Check((dx < 0.0001f) != (dz < 0.0001f), "Every segment cardinal and nonzero");
        }
        var arrowX=block.Direction==ExitDirection.Right?1:block.Direction==ExitDirection.Left?-1:0;
        var arrowZ=block.Direction==ExitDirection.Up?1:block.Direction==ExitDirection.Down?-1:0;
        Check(Near(route[1].X,start.X+arrowX*exit) && Near(route[1].Z,start.Z+arrowZ*exit),
            "First segment follows the arrow for exactly the exit distance");
        var halfAlong=(arrowX!=0?block.Width:block.Height)*WoolRouting.CellSize*0.5f;
        Check(Outside(level,route[1])-halfAlong>=WoolRouting.ExitMargin-0.0002f,
            "Exit leaves the whole full-size block past the board edge plus ExitMargin");
        for(var i=1;i<route.Count;i++)
            Check(Outside(level,route[i])>0,"Every point after the start lies outside the board rectangle");
        var lane=Math.Max(WoolRouting.BoardCenterZ+level.Rows*WoolRouting.CellSize*0.5f+WoolRouting.RingOffset(block),WoolRouting.LaneZ);
        Check(Near(route[route.Count-2].Z,lane) && Near(route[route.Count-2].X,end.X),
            "Route enters the slot straight up from the top lane");
        for(var distance=exit;distance<=WoolRouting.Length(route);distance+=0.0625f)
        {
            WoolRouting.Evaluate(route,distance,out var point,out _);
            Check(Outside(level,point)>=WoolRouting.RingOffset(block)-0.0002f,
                "Full-size block stays off the board after its exit");
        }
        for(var distance=0f;distance<=exit;distance+=0.125f)
        {
            WoolRouting.Evaluate(route,distance,out var point,out _);
            var x=point.X/WoolRouting.CellSize-(block.Width-level.Columns)*0.5f;
            var y=(point.Z-WoolRouting.BoardCenterZ)/WoolRouting.CellSize-(block.Height-level.Rows)*0.5f;
            for(var i=1;i<blocks.Count;i++)
            {
                if(blocks[i].Phase!=0) continue;
                var other=blocks[i].Definition;
                var overlap=x<other.X+other.Width-0.0001f && x+block.Width>other.X+0.0001f &&
                    y<other.Y+other.Height-0.0001f && y+block.Height>other.Y+0.0001f;
                Check(!overlap,"Full-size exit avoids static blocks");
            }
        }
    }
```

Thay check "nudge ngắn rồi rẽ" và "co nhỏ rồi rẽ" bằng check vành đai, phía vòng của Down, làn trên của bảng cao:

**Trước**
```100:114:Tools/WoolJamChecks/RouteChecks.cs
        Setup(8,6,out level,out blocks,B(3,2,2,1,ExitDirection.Right));
        Check(WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Open board has a short route");
        Check(Near(exit,0.5f) && Math.Abs(route[2].Z-route[1].Z)>0.1f && WoolRouting.Length(route)<12,
            "Right arrow can turn upward after a short nudge without reaching the far edge");
        Validate(level,blocks,route,exit,2);
        Setup(8,6,out level,out blocks,B(3,2,2,1,ExitDirection.Right),B(3,3,2));
        Check(WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Blocked early bend still finds a clear route");
        var firstTurn=0f;
        for(var i=1;i<route.Count;i++)
        {
            if(Math.Abs(route[i].Z-route[i-1].Z)>0.001f) break;
            firstTurn+=Math.Abs(route[i].X-route[i-1].X);
        }
        Check(firstTurn>=1.5f,"Block continues along arrow until its reduced footprint has room to turn");
        Validate(level,blocks,route,exit,2);
```

**Sau**
```csharp
        Setup(8,6,out level,out blocks,B(3,2,2,1,ExitDirection.Right));
        Check(WoolRouting.TryFind(level,blocks,0,2,out route,out exit), "Open board has a route");
        var ring=level.Columns*WoolRouting.CellSize*0.5f+WoolRouting.RingOffset(blocks[0].Definition);
        Check(Near(route[1].X,ring) && Near(exit,ring-WoolRouting.BlockCenter(blocks[0].Definition,level).X),
            "Right arrow drives straight to the right ring before turning");
        Check(Near(route[2].X,route[1].X) && route[2].Z>route[1].Z, "Side exit turns toward the tray");
        Validate(level,blocks,route,exit,2);
        Setup(8,6,out level,out blocks,B(3,2,2,1,ExitDirection.Right),B(3,3,2));
        Check(WoolRouting.TryFind(level,blocks,0,2,out var beside,out var besideExit) && Near(besideExit,exit) && beside.Count==route.Count,
            "Static block beside the lane does not bend the exit");
        Validate(level,blocks,beside,besideExit,2);
        Setup(8,6,out level,out blocks,B(1,0,1,1,ExitDirection.Down));
        Check(WoolRouting.TryFind(level,blocks,0,0,out route,out exit) && route[2].X<0, "Down arrow near the left edge goes round the left side to slot 0");
        Validate(level,blocks,route,exit,0);
        Check(WoolRouting.TryFind(level,blocks,0,5,out route,out exit) && route[2].X>0, "Down arrow takes the right side when slot 5 makes it shorter");
        Validate(level,blocks,route,exit,5);
        Setup(8,7,out level,out blocks,B(3,3,2,1));
        Check(WoolRouting.TryFind(level,blocks,0,1,out route,out exit) && route[route.Count-2].Z>WoolRouting.LaneZ,
            "Tall board raises the top lane to its ring");
        Validate(level,blocks,route,exit,1);
```

Thay check "sprite co nhỏ tránh hàng trên" bằng hình dạng route Down và check API co nhỏ đã biến mất:

**Trước**
```144:154:Tools/WoolJamChecks/RouteChecks.cs
        Setup(8,6,out level,out blocks,B(0,0,1,2,ExitDirection.Down),B(0,5,2));
        Check(WoolRouting.TryFind(level,blocks,0,0,out route,out exit), "Tall down arrow circles outside to first slot");
        Validate(level,blocks,route,exit,0);
        var obstacle=WoolRouting.BlockCenter(blocks[1].Definition,level);
        for(var distance=exit;distance<WoolRouting.Length(route);distance+=0.05f)
        {
            WoolRouting.Evaluate(route,distance,out var point,out _);
            var overlap=Math.Abs(point.X-obstacle.X)<(1.02f*0.45f+2.02f)*0.5f &&
                Math.Abs(point.Z-obstacle.Z)<(2.02f*0.45f+1.02f)*0.5f;
            Check(!overlap,"Shrunk tall sprite avoids top row throughout outside route");
        }
```

**Sau**
```csharp
        Setup(8,6,out level,out blocks,B(0,0,1,2,ExitDirection.Down),B(0,5,2));
        Check(WoolRouting.TryFind(level,blocks,0,0,out route,out exit), "Tall down arrow circles outside to first slot");
        Validate(level,blocks,route,exit,0);
        Check(route.Count==6 && route[2].X<route[1].X && Near(route[3].X,route[2].X) && route[3].Z>route[2].Z,
            "Down exit runs along the bottom ring, up the side ring, then along the top lane");
        Check(typeof(WoolRouting).GetMethod("ScaleAt")==null && typeof(WoolRouting).GetField("TravelScale")==null &&
            typeof(WoolRouting).GetField("ArrowDistance")==null, "Shrink API is gone");
```

- [ ] **Bước 2: Viết test RED — `Program.cs`**

Đổi tên hai check FIFO trong `CheckArrowRules`, thêm `CheckMovingBlocker` (khối chạy cắt ngang làn) và `CheckStaggered` (chọn lệch thời điểm, pause), gọi chúng trong `Main`:

**Trước**
```72:84:Tools/WoolJamChecks/Program.cs
        Check(game.Select(2) && game.Select(1), "Outer block and purple blocker can queue in order");
        Check(game.Blocks[1].Travel==0 && !game.Select(0), "Queued unmoved blocker still closes green arrow lane");
        for(var i=0;i<500 && !game.CanSelect(0);i++) game.Step(0.02f);
        Check(game.CanSelect(0) && game.Blocks[1].Phase==1 && game.Select(0),
            "Green arrow opens once moving purple footprint clears its lane before tray arrival");
    }

    static int Main()
    {
        ProgressChecks.Run();
        FeelChecks.Run();
        RouteChecks.Run();
        CheckArrowRules();
```

**Sau**
```csharp
        Check(game.Select(2) && game.Select(1), "Outer block and purple blocker both depart");
        Check(game.Blocks[1].Travel==0 && !game.Select(0), "Moving blocker still at its start closes green arrow lane");
        for(var i=0;i<500 && !game.CanSelect(0);i++) game.Step(0.02f);
        Check(game.CanSelect(0) && game.Blocks[1].Phase==1 && game.Select(0),
            "Green arrow opens once moving purple footprint clears its lane before tray arrival");
    }

    static void CheckMovingBlocker()
    {
        var level=new LevelDefinition
        {
            Columns=5,Rows=3,Slots=2,
            Blocks=new[]
            {
                new BlockDefinition { X=4,Y=1,Width=1,Height=1,Direction=ExitDirection.Left,Color=0,Capacity=1 },
                new BlockDefinition { X=1,Y=0,Width=1,Height=1,Direction=ExitDirection.Up,Color=0,Capacity=1 }
            },
            Yarn=new[] { 0,0 }
        };
        var game=new WoolGame(level);
        Check(game.CanSelect(1) && game.Select(0), "Crossing lane is open before the mover reaches it");
        var blocked=false;
        var reopened=false;
        for(var i=0;i<500 && game.Blocks[0].Phase==1 && !reopened;i++)
        {
            game.Step(0.02f);
            if(!game.CanSelect(1)) blocked=true;
            else if(blocked) reopened=true;
        }
        Check(blocked && game.Blocks[1].Phase==0, "Full-size moving block crossing a lane blocks selection");
        Check(reopened && game.Blocks[0].Phase==1 && game.Select(1), "Lane reopens once the mover has passed, before it reaches the tray");
    }

    static void CheckStaggered()
    {
        Fill(out var level);
        var game=new WoolGame(level);
        Check(game.Select(0), "First block departs");
        Advance(game,10);
        var first=game.Blocks[0];
        var travel=first.Travel;
        Check(game.Select(1), "Second block departs while the first is moving");
        game.Step(0.02f);
        var second=game.Blocks[1];
        Check(Math.Abs(first.Travel-travel-0.02f)<0.0001f && Math.Abs(second.Travel-0.02f)<0.0001f,
            "Later departure neither pauses nor restarts the earlier one");
        Check(game.Spools[0].Arrival<game.Spools[1].Arrival && game.Spools[0].Arrival==first.Duration-first.Travel &&
            game.Spools[1].Arrival==second.Duration-second.Travel, "Staggered spools keep separate arrival clocks");
        var firstHeld=first.Travel;
        var secondHeld=second.Travel;
        game.SetPaused(true);
        Advance(game,20);
        Check(first.Travel==firstHeld && second.Travel==secondHeld && first.Phase==1 && second.Phase==1, "Pause freezes every moving block");
        game.SetPaused(false);
        game.Step(0.02f);
        Check(first.Travel>firstHeld && second.Travel>secondHeld, "Resume moves every block again");
    }

    static int Main()
    {
        ProgressChecks.Run();
        FeelChecks.Run();
        RouteChecks.Run();
        CheckArrowRules();
        CheckMovingBlocker();
        CheckStaggered();
```

Thay khối check FIFO ("Only first departure moves", "Queued block and spool stay frozen…") bằng check song song:

**Trước**
```90:119:Tools/WoolJamChecks/Program.cs
        Check(game.Select(0) && game.Select(1), "Both blocks reserve FIFO departures immediately");
        Check(!game.Select(0), "Duplicate tap cannot reserve twice");
        var first = game.Blocks[0];
        var second = game.Blocks[1];
        Check(first.Slot == 0 && second.Slot == 1 && game.Spools[0].Block == 0 && game.Spools[1].Block == 1,
            "Queued blocks reserve distinct slots");
        Check(first.Route.Count > 1 && first.ExitDistance > 0 &&
            Math.Abs(first.Duration * WoolRouting.Speed - WoolRouting.Length(first.Route)) < 0.0001f,
            "Route, duration and exit distance are stored at selection");
        var queuedArrival = game.Spools[1].Arrival;
        game.Step(0.02f);
        Check(first.Travel > 0 && second.Travel == 0, "Only first departure moves");
        Check(game.Spools[0].Arrival < first.Duration && game.Spools[1].Arrival == queuedArrival,
            "Only active arrival advances");
        Check(game.Remaining == 4 && game.Spools[0].Target < 0 && game.Spools[1].Target < 0,
            "Reserved spools cannot pull while traveling");
        for (var i = 0; i < 500 && first.Phase == 1; i++)
        {
            game.Step(0.02f);
            Check(second.Travel == 0 && game.Spools[1].Arrival == queuedArrival && game.Spools[1].Target < 0,
                "Queued block and spool stay frozen until predecessor completes");
        }
        Check(first.Phase == 2 && first.Travel == first.Duration && game.Spools[0].Arrival == 0,
            "Completion clamps travel and makes spool ready");
        game.Step(0.02f);
        Check(second.Travel > 0, "Next departure starts after predecessor completes");
        Advance(game, 400);
        Check(game.Status == RoundStatus.Won && game.Remaining == 0, "Queued matching spools win without double consumption");
        Advance(game, 30);
        Check(game.Spools[0].Block < 0 && game.Spools[1].Block < 0, "Completed spools release reserved slots");
```

**Sau**
```csharp
        Check(game.Select(0) && game.Select(1), "Both blocks depart immediately");
        Check(!game.Select(0), "Duplicate tap cannot reserve twice");
        var first = game.Blocks[0];
        var second = game.Blocks[1];
        Check(first.Slot == 0 && second.Slot == 1 && game.Spools[0].Block == 0 && game.Spools[1].Block == 1,
            "Concurrent blocks reserve distinct slots");
        Check(first.Route.Count > 1 && first.ExitDistance > 0 &&
            Math.Abs(first.Duration * WoolRouting.Speed - WoolRouting.Length(first.Route)) < 0.0001f,
            "Route, duration and exit distance are stored at selection");
        game.Step(0.02f);
        Check(first.Travel > 0 && second.Travel > 0 && first.Phase == 1 && second.Phase == 1, "Both departures move in the same Step");
        Check(game.Spools[0].Arrival == first.Duration - first.Travel && game.Spools[1].Arrival == second.Duration - second.Travel,
            "Each spool Arrival counts down its own block");
        Check(game.Remaining == 4 && game.Spools[0].Target < 0 && game.Spools[1].Target < 0,
            "Reserved spools cannot pull while traveling");
        for (var i = 0; i < 500 && (first.Phase == 1 || second.Phase == 1); i++)
        {
            game.Step(0.02f);
            Check(game.Spools[0].Arrival == Math.Max(0, first.Duration - first.Travel) &&
                game.Spools[1].Arrival == Math.Max(0, second.Duration - second.Travel),
                "Spool arrivals advance independently every Step");
        }
        Check(first.Phase == 2 && second.Phase == 2 && first.Travel == first.Duration && second.Travel == second.Duration &&
            game.Spools[0].Arrival == 0 && game.Spools[1].Arrival == 0, "Completion clamps travel and makes both spools ready");
        Advance(game, 400);
        Check(game.Status == RoundStatus.Won && game.Remaining == 0, "Concurrent matching spools win without double consumption");
        Advance(game, 30);
        Check(game.Spools[0].Block < 0 && game.Spools[1].Block < 0, "Completed spools release reserved slots");
```

Đổi chữ ba check còn nhắc FIFO/queue:

**Trước**
```133:133:Tools/WoolJamChecks/Program.cs
            game.Spools[0].Arrival == arrival && game.Remaining == 4, "Pause freezes FIFO travel, arrival, yarn and dragon");
```

**Sau**
```csharp
            game.Spools[0].Arrival == arrival && game.Remaining == 4, "Pause freezes travel, arrival, yarn and dragon");
```

**Trước**
```142:142:Tools/WoolJamChecks/Program.cs
            reset.Blocks[1].ExitDistance == 0 && reset.Blocks[1].Slot == -1, "New round clears all route and queue state");
```

**Sau**
```csharp
            reset.Blocks[1].ExitDistance == 0 && reset.Blocks[1].Slot == -1, "New round clears all route and travel state");
```

**Trước**
```145:145:Tools/WoolJamChecks/Program.cs
        Check(reset.Blocks[0].Travel > 0, "Fresh queue starts without stale predecessor");
```

**Sau**
```csharp
        Check(reset.Blocks[0].Travel > 0, "Fresh round departs without stale travel");
```

Thay check "follower chờ gate hoàn tất" bằng check follower chạy song song:

**Trước**
```202:211:Tools/WoolJamChecks/Program.cs
        Check(game.Select(2) && !game.Select(0), "Moving gate still blocks its original arrow lane");
        for(var i=0;i<500 && !game.CanSelect(0);i++) game.Step(0.02f);
        Check(game.Select(0), "Follower can queue after gate clears the arrow lane");
        for (var i = 0; i < 500 && game.Blocks[2].Phase == 1; i++)
        {
            game.Step(0.02f);
            Check(game.Blocks[0].Travel == 0, "Enclosed follower waits until gate physically completes departure");
        }
        game.Step(0.02f);
        Check(game.Blocks[0].Travel > 0, "Follower moves only after gate completes");
```

**Sau**
```csharp
        Check(game.Select(2) && !game.Select(0), "Moving gate still blocks its original arrow lane");
        for(var i=0;i<500 && !game.CanSelect(0);i++) game.Step(0.02f);
        Check(game.Blocks[2].Phase == 1 && game.Select(0), "Follower departs once the moving gate clears its arrow lane");
        game.Step(0.02f);
        Check(game.Blocks[0].Travel > 0 && game.Blocks[2].Phase == 1, "Follower moves while the gate is still on its route");
```

- [ ] **Bước 3: Chạy RED**

Run: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`
Kỳ vọng: build FAIL với đúng bốn loại lỗi `CS0117: 'WoolRouting' does not contain a definition for 'CellSize'` / `'ExitMargin'` / `'LaneZ'` / `'RingOffset'`.

- [ ] **Bước 4: `WoolRouting` — hằng số**

**Trước**
```16:21:Assets/JamJam/Runtime/WoolRouting.cs
        public const float Speed = 6f;
        public const float ArrowDistance = 0.5f;
        public const float TravelScale = 0.45f;
        public const float BoardCenterZ = -5.05f;
        public const float TrayZ = -0.75f;
        public const float SlotSpacing = 1.27f;
```

**Sau**
```csharp
        public const float Speed = 9f;
        public const float CellSize = 0.8f;
        public const float ExitMargin = 0.3f;
        public const float BoardCenterZ = -5.05f;
        public const float TrayZ = -0.75f;
        public const float LaneZ = TrayZ - 0.8f;
        public const float SlotSpacing = 1.27f;
```

- [ ] **Bước 5: `WoolRouting.BlockCenter` theo `CellSize`**

**Trước**
```25:29:Assets/JamJam/Runtime/WoolRouting.cs
        public static RoutePoint BlockCenter(BlockDefinition block, LevelDefinition level)
        {
            return new RoutePoint(block.X + (block.Width - level.Columns) * 0.5f,
                BoardCenterZ + block.Y + (block.Height - level.Rows) * 0.5f);
        }
```

**Sau**
```csharp
        public static RoutePoint BlockCenter(BlockDefinition block, LevelDefinition level)
        {
            return new RoutePoint((block.X + (block.Width - level.Columns) * 0.5f) * CellSize,
                BoardCenterZ + (block.Y + (block.Height - level.Rows) * 0.5f) * CellSize);
        }
```

- [ ] **Bước 6: `WoolRouting` — `RingOffset`, `TryFind` hình học, `IsClear` nguyên kích thước; xóa `ScaleAt`**

**Trước**
```36:117:Assets/JamJam/Runtime/WoolRouting.cs
        public static bool TryFind(LevelDefinition level, IReadOnlyList<BlockState> blocks, int index, int slot,
            out IReadOnlyList<RoutePoint> points, out float boardExitDistance)
        {
            points = Array.Empty<RoutePoint>();
            boardExitDistance = 0;
            if (level == null || blocks == null || index < 0 || index >= blocks.Count) return false;
            var block = blocks[index].Definition;
            if (level.Columns < 1 || level.Rows < 1 || block.Width < 1 || block.Height < 1) return false;
            var direction=(int)block.Direction;
            if(direction<0 || direction>3) return false;
            var steps=block.Direction==ExitDirection.Up?level.Rows-block.Y:
                block.Direction==ExitDirection.Down?block.Y+block.Height:
                block.Direction==ExitDirection.Right?level.Columns-block.X:block.X+block.Width;
            for(var step=1;step<=steps;step++)
                if(!IsClear(blocks,index,block.X+StepX[direction]*step,block.Y+StepY[direction]*step,block.Width,block.Height)) return false;
            const int resolution=2;
            var target=SlotCenter(slot);
            var goalX=(int)Math.Round((target.X-(block.Width-level.Columns)*0.5f)*resolution);
            var goalY=(int)Math.Ceiling((level.Rows-block.Height*(1-TravelScale)*0.5f)*resolution);
            var padding=(Math.Max(block.Width,block.Height)+1)*resolution;
            var minX=Math.Min(-block.Width*resolution-padding,goalX-padding);
            var maxX=Math.Max(level.Columns*resolution+padding,goalX+padding);
            var minY=-block.Height*resolution-padding;
            var maxY=level.Rows*resolution+padding;
            var columns=maxX-minX+1;
            var previous=new int[columns*(maxY-minY+1)];
            for(var i=0;i<previous.Length;i++) previous[i]=-1;
            var startX=block.X*resolution+StepX[direction];
            var startY=block.Y*resolution+StepY[direction];
            var start=(startY-minY)*columns+startX-minX;
            var goal=(goalY-minY)*columns+goalX-minX;
            var queue=new Queue<int>(); previous[start]=start; queue.Enqueue(start);
            while(queue.Count>0 && previous[goal]<0)
            {
                var current=queue.Dequeue();
                var x=current%columns+minX; var y=current/columns+minY;
                for(var turn=0;turn<4;turn++)
                {
                    if(current==start && turn==(direction+2)%4) continue;
                    var nextX=x+StepX[turn]; var nextY=y+StepY[turn];
                    if(nextX<minX || nextX>maxX || nextY<minY || nextY>maxY) continue;
                    var next=(nextY-minY)*columns+nextX-minX;
                    if(previous[next]>=0 || !IsClear(blocks,index,nextX/(float)resolution,nextY/(float)resolution,block.Width,block.Height,TravelScale)) continue;
                    previous[next]=current; queue.Enqueue(next);
                }
            }
            if(previous[goal]<0) return false;
            var route=new List<RoutePoint>();
            for(var current=goal;;current=previous[current])
            {
                route.Add(new RoutePoint((current%columns+minX)/(float)resolution+(block.Width-level.Columns)*0.5f,
                    BoardCenterZ+(current/columns+minY)/(float)resolution+(block.Height-level.Rows)*0.5f));
                if(current==start) break;
            }
            route.Add(BlockCenter(block,level)); route.Reverse();
            boardExitDistance=ArrowDistance;
            Add(route,new RoutePoint(route[route.Count-1].X,TrayZ-0.8f));
            Add(route,new RoutePoint(target.X,TrayZ-0.8f));
            Add(route,target);
            points = route;
            return true;
        }

        static bool IsClear(IReadOnlyList<BlockState> blocks, int index, float x, float y, int width, int height, float scale=1)
        {
            x+=width*(1-scale)*0.5f; y+=height*(1-scale)*0.5f;
            var scaledWidth=width*scale; var scaledHeight=height*scale;
            for (var i = 0; i < blocks.Count; i++)
            {
                if (i == index || blocks[i].Phase != 0) continue;
                var other = blocks[i].Definition;
                if (x < other.X + other.Width && x + scaledWidth > other.X &&
                    y < other.Y + other.Height && y + scaledHeight > other.Y) return false;
            }
            return true;
        }

        public static float ScaleAt(float distance)
        {
            var t=Math.Max(0,Math.Min(1,distance/ArrowDistance));
            return 1+(TravelScale-1)*t*t*(3-2*t);
        }
```

**Sau**
```csharp
        public static float RingOffset(BlockDefinition block)
        {
            return ExitMargin + Math.Max(block.Width, block.Height) * CellSize * 0.5f;
        }

        public static bool TryFind(LevelDefinition level, IReadOnlyList<BlockState> blocks, int index, int slot,
            out IReadOnlyList<RoutePoint> points, out float exitDistance)
        {
            points = Array.Empty<RoutePoint>();
            exitDistance = 0;
            if (level == null || blocks == null || index < 0 || index >= blocks.Count) return false;
            var block = blocks[index].Definition;
            if (level.Columns < 1 || level.Rows < 1 || block.Width < 1 || block.Height < 1) return false;
            var direction = (int)block.Direction;
            if (direction < 0 || direction > 3) return false;
            var steps = block.Direction == ExitDirection.Up ? level.Rows - block.Y :
                block.Direction == ExitDirection.Down ? block.Y + block.Height :
                block.Direction == ExitDirection.Right ? level.Columns - block.X : block.X + block.Width;
            for (var step = 1; step <= steps; step++)
                if (!IsClear(blocks, index, block.X + StepX[direction] * step, block.Y + StepY[direction] * step, block.Width, block.Height)) return false;
            var start = BlockCenter(block, level);
            var target = SlotCenter(slot);
            var offset = RingOffset(block);
            var left = -level.Columns * CellSize * 0.5f - offset;
            var right = level.Columns * CellSize * 0.5f + offset;
            var bottom = BoardCenterZ - level.Rows * CellSize * 0.5f - offset;
            var top = BoardCenterZ + level.Rows * CellSize * 0.5f + offset;
            var lane = Math.Max(top, LaneZ);
            var route = new List<RoutePoint> { start };
            if (block.Direction == ExitDirection.Up) Add(route, new RoutePoint(start.X, top));
            else if (block.Direction == ExitDirection.Left) Add(route, new RoutePoint(left, start.Z));
            else if (block.Direction == ExitDirection.Right) Add(route, new RoutePoint(right, start.Z));
            else
            {
                Add(route, new RoutePoint(start.X, bottom));
                var viaLeft = start.X - left + Math.Abs(target.X - left);
                var viaRight = right - start.X + Math.Abs(right - target.X);
                Add(route, new RoutePoint(viaLeft <= viaRight ? left : right, bottom));
            }
            exitDistance = Math.Abs(route[1].X - start.X) + Math.Abs(route[1].Z - start.Z);
            Add(route, new RoutePoint(route[route.Count - 1].X, lane));
            Add(route, new RoutePoint(target.X, lane));
            Add(route, target);
            points = route;
            return true;
        }

        static bool IsClear(IReadOnlyList<BlockState> blocks, int index, int x, int y, int width, int height)
        {
            for (var i = 0; i < blocks.Count; i++)
            {
                if (i == index || blocks[i].Phase != 0) continue;
                var other = blocks[i].Definition;
                if (x < other.X + other.Width && x + width > other.X &&
                    y < other.Y + other.Height && y + height > other.Y) return false;
            }
            return true;
        }
```

- [ ] **Bước 7: `WoolGame` — bỏ hàng đợi**

**Trước**
```74:75:Assets/JamJam/Runtime/WoolGame.cs
        readonly List<int> spoolOrder = new List<int>();
        readonly Queue<int> departures = new Queue<int>();
```

**Sau**
```csharp
        readonly List<int> spoolOrder = new List<int>();
```

**Trước**
```189:190:Assets/JamJam/Runtime/WoolGame.cs
            block.ExitDistance = exitDistance;
            departures.Enqueue(index);
```

**Sau**
```csharp
            block.ExitDistance = exitDistance;
```

- [ ] **Bước 8: `WoolGame.HasClearArrowLane` — khối đang chạy nguyên kích thước, đổi thế giới → ô bằng `CellSize`**

**Trước**
```158:166:Assets/JamJam/Runtime/WoolGame.cs
                if(other.Phase==1)
                {
                    WoolRouting.Evaluate(other.Route,other.Travel*WoolRouting.Speed,out var point,out _);
                    x=point.X-(b.Width-level.Columns)*0.5f;
                    y=point.Z-WoolRouting.BoardCenterZ-(b.Height-level.Rows)*0.5f;
                    var scale=WoolRouting.ScaleAt(other.Travel*WoolRouting.Speed);
                    x+=width*(1-scale)*0.5f; y+=height*(1-scale)*0.5f;
                    width*=scale; height*=scale;
                }
```

**Sau**
```csharp
                if(other.Phase==1)
                {
                    WoolRouting.Evaluate(other.Route,other.Travel*WoolRouting.Speed,out var point,out _);
                    x=point.X/WoolRouting.CellSize-(b.Width-level.Columns)*0.5f;
                    y=(point.Z-WoolRouting.BoardCenterZ)/WoolRouting.CellSize-(b.Height-level.Rows)*0.5f;
                }
```

- [ ] **Bước 9: `WoolGame.Step` — mọi khối Phase 1 cùng tiến**

**Trước**
```229:239:Assets/JamJam/Runtime/WoolGame.cs
            if (departures.Count > 0)
            {
                var block = blocks[departures.Peek()];
                block.Travel = Math.Min(block.Duration, block.Travel + delta);
                spools[block.Slot].Arrival = Math.Max(0, block.Duration - block.Travel);
                if (block.Travel >= block.Duration)
                {
                    block.Phase = 2;
                    departures.Dequeue();
                }
            }
```

**Sau**
```csharp
            foreach (var block in blocks)
            {
                if (block.Phase != 1) continue;
                block.Travel = Math.Min(block.Duration, block.Travel + delta);
                spools[block.Slot].Arrival = Math.Max(0, block.Duration - block.Travel);
                if (block.Travel >= block.Duration) block.Phase = 2;
            }
```

- [ ] **Bước 10: Chạy GREEN**

Run: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`
Kỳ vọng: `PASS: 105 gameplay assertions; 13011 route assertions; 18 progress assertions; 108 feel assertions`.

- [ ] **Bước 11: `WoolScene.RenderFrame` — giữ Unity compile được (không còn `ScaleAt`)**

Khối Phase 1 phẳng ở y 0.12, scale chỉ còn pop của `WoolFeel`, xoay cả khối theo hướng chạy (mũi tên là con nên tự theo):

**Trước**
```267:280:Assets/JamJam/Runtime/WoolScene.cs
                if (block.Phase == 1)
                {
                    var distance=block.Travel*WoolRouting.Speed;
                    WoolRouting.Evaluate(block.Route,distance,out var point,out var movement);
                    view.transform.position=new Vector3(point.X,0.12f+feel.BlockLift(i),point.Z);
                    view.transform.localScale=Vector3.one*(WoolRouting.ScaleAt(distance)*feel.BlockScale(i));
                    if(block.Travel>0)
                    {
                        var angle=Mathf.Atan2(movement.X,movement.Z)*Mathf.Rad2Deg;
                        view.Arrow.localRotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,-angle);
                        var length=Mathf.Abs(movement.Z)>0.5f?block.Definition.Height:block.Definition.Width;
                        view.Arrow.localScale=new Vector3(0.56f,length*Cell*0.78f,1);
                    }
                }
```

**Sau**
```csharp
                if (block.Phase == 1)
                {
                    WoolRouting.Evaluate(block.Route,block.Travel*WoolRouting.Speed,out var point,out var movement);
                    view.transform.position=new Vector3(point.X,0.12f,point.Z);
                    view.transform.localScale=Vector3.one*feel.BlockScale(i);
                    var heading=Quaternion.Euler(0,Mathf.Atan2(movement.X,movement.Z)*Mathf.Rad2Deg-(int)block.Definition.Direction*90,0);
                    view.transform.rotation=delta==0?heading:Quaternion.RotateTowards(view.transform.rotation,heading,1080*delta);
                }
```

- [ ] **Bước 12: Unity compile** — `unity recompile`, rồi `E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'`. Kỳ vọng `False False`, không cảnh báo mới trong file đã sửa.

- [ ] **Bước 13: Kiểm tra**

```bash
cd "/d/Unity Project/Jam_Jam"
md5sum -c "$SCRATCH/motion-run/levels.md5"
grep -n "UnityEngine" Assets/JamJam/Runtime/WoolRouting.cs Assets/JamJam/Runtime/WoolGame.cs
grep -rn "ScaleAt\|TravelScale\|ArrowDistance\|departures" --include=*.cs Assets/JamJam/Runtime/WoolRouting.cs Assets/JamJam/Runtime/WoolGame.cs Assets/JamJam/Runtime/WoolScene.cs Assets/JamJam/Editor
grep -n "//\|/\*" Assets/JamJam/Runtime/WoolRouting.cs Assets/JamJam/Runtime/WoolGame.cs Tools/WoolJamChecks/RouteChecks.cs Tools/WoolJamChecks/Program.cs
```
Kỳ vọng: ba dòng `OK`; ba lệnh `grep` rỗng.

- [ ] **Bước 14: Dừng, báo user.**

---

### Task 2: Hiển thị — kích thước, khe, collider, đệm bảng, game feel, Rebuild Art

**Files:**
- Create: `Assets/JamJam/Editor/WoolArtChecks.PlayMotion.cs`
- Modify: `Assets/JamJam/Editor/WoolArtChecks.Blocks.cs:12-14`, `:35`; `WoolArtChecks.Play.cs:22-26`, `:31-32`; `WoolArtChecks.PlayFeel.cs:55`; `WoolArtChecks.Stage.cs:31`, `:35-45`
- Modify: `Assets/JamJam/Runtime/WoolBlockMesh.cs:9-12`, `:27-28`
- Modify: `Assets/JamJam/Runtime/WoolScene.cs:14`, `:63`, `:123`, `:136-142`
- Modify: `Assets/JamJam/Runtime/WoolFeel.cs:9-11`, `:91-99`
- Modify: `Assets/JamJam/Editor/WoolAssetsBuilder.cs:476`, `WoolArtBuilder.cs:41-44`, `WoolSceneBuilder.cs:49`

**Interfaces:**
- Consumes (Task 1): `WoolRouting.CellSize`, `RingOffset`, `TryFind`, `SlotCenter`, `Evaluate`, `Speed`; `BlockState`.
- Produces: `WoolScene.Cushion : Transform` (public field), `WoolScene.CushionPad = 0.5f` (public const); `WoolBlockMesh.Inset = 0.09f` (đơn vị ô), `Corner = 0.18f`, `Bevel = 0.06f`, `Border = 0.1f`, `Thickness = 0.35f`; `WoolFeel.LaunchPop = 0.08f`, `LaunchPopTime = 0.18f`, không còn `FlightLift`/`BlockLift(int)`; `WoolArtChecks.PlayMotionArm(int level) : string`, `PlayMotionMid() : string`, `PlayMotionEnd() : string`.

- [ ] **Bước 1: Check RED — kỳ vọng kích thước/collider/khe/khung hình mới**

`Blocks()` — bounds mesh theo ô 0.8 và mũi tên prefab:

**Trước**
```12:14:Assets/JamJam/Editor/WoolArtChecks.Blocks.cs
            var inset = WoolBlockMesh.Inset * 2;
            ExpectBounds(wide, new Vector3(2 - inset, WoolBlockMesh.Thickness, 1 - inset), "Block 2x1");
            ExpectBounds(tall, new Vector3(1 - inset, WoolBlockMesh.Thickness, 2 - inset), "Block 1x2");
```

**Sau**
```csharp
            var inset = WoolBlockMesh.Inset * 2;
            var cell = WoolRouting.CellSize;
            ExpectBounds(wide, new Vector3((2 - inset) * cell, WoolBlockMesh.Thickness, (1 - inset) * cell), "Block 2x1");
            ExpectBounds(tall, new Vector3((1 - inset) * cell, WoolBlockMesh.Thickness, (2 - inset) * cell), "Block 1x2");
```

**Trước**
```35:35:Assets/JamJam/Editor/WoolArtChecks.Blocks.cs
            Expect(Vector3.Dot(view.Arrow.up, Vector3.forward) > 0.99f, "Arrow points along +Z by default");
```

**Sau**
```csharp
            Expect(Vector3.Dot(view.Arrow.up, Vector3.forward) > 0.99f, "Arrow points along +Z by default");
            Expect(Mathf.Abs(view.Arrow.localScale.x - 0.56f * WoolRouting.CellSize) < 0.001f && Mathf.Abs(view.Arrow.localScale.y - 1.56f * WoolRouting.CellSize) < 0.001f, "Arrow plate scaled to the cell");
```

`PlayBlocks()` — collider = kích thước hiển thị, khe ≥ 0.13 giữa mọi cặp khối đang đứng:

**Trước**
```22:26:Assets/JamJam/Editor/WoolArtChecks.Play.cs
                Expect(Vector3.Distance(collider.size, new Vector3(definition.Width - 0.035f, 0.55f, definition.Height - 0.035f)) < 0.001f, "Collider size of block " + view.Index);
                var filter = view.Body.GetComponent<MeshFilter>();
                Expect(filter != null && filter.sharedMesh != null, "Block " + view.Index + " body mesh");
                var size = filter.sharedMesh.bounds.size;
                Expect(Mathf.Abs(size.x - (definition.Width - WoolBlockMesh.Inset * 2)) < 0.01f && Mathf.Abs(size.z - (definition.Height - WoolBlockMesh.Inset * 2)) < 0.01f, "Block " + view.Index + " mesh matches " + definition.Width + "x" + definition.Height);
```

**Sau**
```csharp
                var cell = WoolRouting.CellSize;
                Expect(Vector3.Distance(collider.size, new Vector3((definition.Width - WoolBlockMesh.Inset * 2) * cell, 0.55f, (definition.Height - WoolBlockMesh.Inset * 2) * cell)) < 0.001f, "Collider size of block " + view.Index);
                var filter = view.Body.GetComponent<MeshFilter>();
                Expect(filter != null && filter.sharedMesh != null, "Block " + view.Index + " body mesh");
                var size = filter.sharedMesh.bounds.size;
                Expect(Mathf.Abs(size.x - (definition.Width - WoolBlockMesh.Inset * 2) * cell) < 0.01f && Mathf.Abs(size.z - (definition.Height - WoolBlockMesh.Inset * 2) * cell) < 0.01f, "Block " + view.Index + " mesh matches " + definition.Width + "x" + definition.Height);
```

**Trước**
```31:32:Assets/JamJam/Editor/WoolArtChecks.Play.cs
            }
            var blocked = false;
```

**Sau**
```csharp
            }
            for (var i = 0; i < views.Length; i++)
                for (var j = i + 1; j < views.Length; j++)
                {
                    if (scene.State.Blocks[views[i].Index].Phase != 0 || scene.State.Blocks[views[j].Index].Phase != 0) continue;
                    var a = views[i].Body.GetComponent<Renderer>().bounds;
                    a.Expand(new Vector3(0.26f, 0, 0.26f));
                    Expect(!a.Intersects(views[j].Body.GetComponent<Renderer>().bounds), "Blocks " + views[i].Index + " and " + views[j].Index + " keep a gap of at least 0.13");
                }
            var blocked = false;
```

`PlayFeel()` — không còn nhấc cung:

**Trước**
```55:55:Assets/JamJam/Editor/WoolArtChecks.PlayFeel.cs
            Expect(feel.BlockLift(launched) > 0, "Flying block lifts on its arc");
```

**Sau**
```csharp
            Expect(feel.BlockScale(launched) == 1 && Mathf.Abs(scene.BlockAt(launched).y - 0.12f) < 0.0001f, "Flying block is back to full size and stays flat");
```

`Stage()` — `WoolScene.Cushion` được gán, khung hình bao mọi điểm route vành đai của 3 level (thay 4 góc cố định):

**Trước**
```31:31:Assets/JamJam/Editor/WoolArtChecks.Stage.cs
            Expect(cushion != null && cushion.GetComponent<Renderer>().sharedMaterial == cushionMaterial, "Lighter navy board cushion");
```

**Sau**
```csharp
            Expect(cushion != null && cushion.GetComponent<Renderer>().sharedMaterial == cushionMaterial, "Lighter navy board cushion");
            var controller = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            Expect(controller != null && controller.Cushion == cushion.transform, "WoolScene sizes the Board Cushion");
```

**Trước**
```35:45:Assets/JamJam/Editor/WoolArtChecks.Stage.cs
            var points = new List<Vector3>();
            for (var i = 1; ; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<WoolLevel>(WoolAssetsBuilder.Root + "/Levels/Level_" + i + ".asset");
                if (level == null) break;
                foreach (var p in level.Path) { points.Add(p + new Vector3(0.66f, 0, 0.66f)); points.Add(p - new Vector3(0.66f, 0, 0.66f)); }
            }
            Expect(points.Count > 0, "Level paths found");
            for (var slot = 0; slot < 6; slot++) points.Add(WoolScene.SlotPosition(slot) + new Vector3(slot < 3 ? -0.56f : 0.56f, 0, -0.62f));
            foreach (var x in new[] { -4.35f, 4.35f })
                foreach (var z in new[] { -8.2f, -1.9f }) points.Add(new Vector3(x, 0, z));
```

**Sau**
```csharp
            var points = new List<Vector3>();
            var routes = 0;
            for (var i = 1; ; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<WoolLevel>(WoolAssetsBuilder.Root + "/Levels/Level_" + i + ".asset");
                if (level == null) break;
                foreach (var p in level.Path) { points.Add(p + new Vector3(0.66f, 0, 0.66f)); points.Add(p - new Vector3(0.66f, 0, 0.66f)); }
                var data = level.Data;
                for (var b = 0; b < data.Blocks.Length; b++)
                    for (var slot = 0; slot < 6; slot++)
                    {
                        Expect(WoolRouting.TryFind(data, new[] { new BlockState { Definition = data.Blocks[b] } }, 0, slot, out var route, out _), "Level " + i + " block " + b + " has a ring route to slot " + slot);
                        foreach (var r in route) points.Add(new Vector3(r.X, 0, r.Z));
                        routes++;
                    }
            }
            Expect(points.Count > 0 && routes > 0, "Level paths and ring routes found");
            for (var slot = 0; slot < 6; slot++) points.Add(WoolScene.SlotPosition(slot) + new Vector3(slot < 3 ? -0.56f : 0.56f, 0, -0.62f));
```

Tạo `Assets/JamJam/Editor/WoolArtChecks.PlayMotion.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        static int[] motionPicked = Array.Empty<int>();

        public static string PlayMotionArm(int level)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null, "WoolScene is running");
            scene.LoadLevel(level);
            var game = scene.State;
            var data = scene.Levels[scene.CurrentLevel].Data;
            Expect(scene.Cushion != null &&
                Mathf.Abs(scene.Cushion.localScale.x - (data.Columns * WoolRouting.CellSize + WoolScene.CushionPad)) < 0.001f &&
                Mathf.Abs(scene.Cushion.localScale.z - (data.Rows * WoolRouting.CellSize + WoolScene.CushionPad)) < 0.001f,
                "Board Cushion fits level " + (scene.CurrentLevel + 1));
            var picked = new List<int>();
            for (var b = 0; b < game.Blocks.Count && picked.Count < 2; b++)
            {
                if (!game.CanSelect(b)) continue;
                scene.Select(b);
                if (game.Blocks[b].Phase == 1) picked.Add(b);
            }
            Expect(picked.Count == 2, "Two clear blocks depart back to back");
            motionPicked = picked.ToArray();
            Time.timeScale = 0.1f;
            return "MOTION ARMED / blocks " + picked[0] + " " + picked[1];
        }

        public static string PlayMotionMid()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            Expect(motionPicked.Length == 2, "Run PlayMotionArm first");
            var scene = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            var feel = scene.GetComponent<WoolFeel>();
            var game = scene.State;
            var a = game.Blocks[motionPicked[0]];
            var b = game.Blocks[motionPicked[1]];
            Expect(a.Phase == 1 && b.Phase == 1, "Both blocks are still on their routes");
            Expect(a.Travel > WoolFeel.LaunchPopTime && a.Travel == b.Travel, "Both blocks advanced together every frame (travel " + a.Travel.ToString("F3") + " / " + b.Travel.ToString("F3") + ")");
            var seen = 0;
            foreach (var view in UnityEngine.Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None))
            {
                if (view.Index != motionPicked[0] && view.Index != motionPicked[1]) continue;
                var block = game.Blocks[view.Index];
                WoolRouting.Evaluate(block.Route, block.Travel * WoolRouting.Speed, out var point, out _);
                var position = view.transform.position;
                Expect(Mathf.Abs(position.x - point.X) < 0.001f && Mathf.Abs(position.z - point.Z) < 0.001f && Mathf.Abs(position.y - 0.12f) < 0.0001f, "Block " + view.Index + " rides flat on its route");
                Expect(Vector3.Distance(view.transform.localScale, Vector3.one) < 0.0001f && feel.BlockScale(view.Index) == 1, "Block " + view.Index + " keeps full size");
                Expect(Vector3.Dot(view.transform.up, Vector3.up) > 0.9999f, "Block " + view.Index + " only turns around the vertical axis");
                seen++;
            }
            Expect(seen == 2, "Both moving block views are visible");
            return "MOTION MID PASS / travel " + a.Travel.ToString("F2") + " s";
        }

        public static string PlayMotionEnd()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            Expect(motionPicked.Length == 2, "Run PlayMotionArm first");
            var game = UnityEngine.Object.FindFirstObjectByType<WoolScene>().State;
            foreach (var index in motionPicked)
            {
                var block = game.Blocks[index];
                var slot = WoolRouting.SlotCenter(block.Slot);
                var end = block.Route[block.Route.Count - 1];
                Expect(block.Phase == 2 && block.Travel == block.Duration && end.X == slot.X && end.Z == slot.Z, "Block " + index + " reached slot " + block.Slot);
                Expect(game.Spools[block.Slot].Block != index || game.Spools[block.Slot].Arrival == 0, "Spool " + block.Slot + " arrived for block " + index);
            }
            Time.timeScale = 1;
            var result = "MOTION END PASS / slots " + game.Blocks[motionPicked[0]].Slot + " " + game.Blocks[motionPicked[1]].Slot;
            motionPicked = Array.Empty<int>();
            return result;
        }
    }
}
```

- [ ] **Bước 2: Chạy RED** — `unity recompile`. Kỳ vọng lỗi trong Assembly-CSharp-Editor: `CS1061: 'WoolScene' does not contain a definition for 'Cushion'` (Stage, PlayMotion) và `CS0117: 'WoolScene' does not contain a definition for 'CushionPad'` (PlayMotion).

- [ ] **Bước 3: `WoolBlockMesh` — inset 0.09 ô, bo góc nhỏ hơn**

**Trước**
```9:12:Assets/JamJam/Runtime/WoolBlockMesh.cs
        public const float Inset = 0.04f;
        public const float Corner = 0.22f;
        public const float Bevel = 0.06f;
        public const float Border = 0.12f;
```

**Sau**
```csharp
        public const float Inset = 0.09f;
        public const float Corner = 0.18f;
        public const float Bevel = 0.06f;
        public const float Border = 0.1f;
```

**Trước**
```27:28:Assets/JamJam/Runtime/WoolBlockMesh.cs
            var halfX = width * 0.5f - Inset;
            var halfZ = height * 0.5f - Inset;
```

**Sau**
```csharp
            var halfX = (width * 0.5f - Inset) * WoolRouting.CellSize;
            var halfZ = (height * 0.5f - Inset) * WoolRouting.CellSize;
```

- [ ] **Bước 4: `WoolScene` — field đệm bảng và hằng lề**

**Trước**
```14:14:Assets/JamJam/Runtime/WoolScene.cs
        public Transform World;
```

**Sau**
```csharp
        public Transform World;
        public Transform Cushion;
```

**Trước**
```63:63:Assets/JamJam/Runtime/WoolScene.cs
        const float Cell = 1f;
```

**Sau**
```csharp
        public const float CushionPad = 0.5f;
```

- [ ] **Bước 5: `WoolScene.LoadLevel` — đệm bảng theo level**

**Trước**
```123:123:Assets/JamJam/Runtime/WoolScene.cs
            BuildRoad(level.Path);
```

**Sau**
```csharp
            BuildRoad(level.Path);
            if (Cushion != null) Cushion.localScale = new Vector3(level.Data.Columns * WoolRouting.CellSize + CushionPad, Cushion.localScale.y, level.Data.Rows * WoolRouting.CellSize + CushionPad);
```

- [ ] **Bước 6: `WoolScene.LoadLevel` — mũi tên, collider theo `CellSize`**

**Trước**
```136:142:Assets/JamJam/Runtime/WoolScene.cs
                var vertical=data.Direction==ExitDirection.Up || data.Direction==ExitDirection.Down;
                view.Arrow.localRotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,-(int)data.Direction*90);
                view.Arrow.localScale=new Vector3(0.56f,(vertical?data.Height:data.Width)*Cell*0.78f,1);
                var collider = go.GetComponent<BoxCollider>();
                collider.size = new Vector3(data.Width * Cell - 0.035f, 0.55f, data.Height * Cell - 0.035f);
                collider.center = new Vector3(0, 0.28f, 0);
                go.transform.position = BlockPosition(data, level.Data);
```

**Sau**
```csharp
                var vertical=data.Direction==ExitDirection.Up || data.Direction==ExitDirection.Down;
                view.Arrow.localRotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,-(int)data.Direction*90);
                view.Arrow.localScale=new Vector3(0.56f*WoolRouting.CellSize,(vertical?data.Height:data.Width)*WoolRouting.CellSize*0.78f,1);
                var collider = go.GetComponent<BoxCollider>();
                collider.size = new Vector3((data.Width - WoolBlockMesh.Inset * 2) * WoolRouting.CellSize, 0.55f, (data.Height - WoolBlockMesh.Inset * 2) * WoolRouting.CellSize);
                collider.center = new Vector3(0, 0.28f, 0);
                go.transform.position = BlockPosition(data, level.Data);
```

- [ ] **Bước 7: `WoolFeel` — pop 1.08, bỏ nhấc cung**

**Trước**
```9:11:Assets/JamJam/Runtime/WoolFeel.cs
        public const float LaunchPop = 0.18f;
        public const float LaunchPopTime = 0.18f;
        public const float FlightLift = 0.7f;
```

**Sau**
```csharp
        public const float LaunchPop = 0.08f;
        public const float LaunchPopTime = 0.18f;
```

**Trước**
```91:99:Assets/JamJam/Runtime/WoolFeel.cs
        public float SlotFlash(int index) { return TrayFlash * WoolEase.Arc(Age(slotFlash, index) / TrayFlashTime); }

        public float BlockLift(int index)
        {
            if (game == null || index < 0 || index >= game.Blocks.Count) return 0;
            var block = game.Blocks[index];
            return block.Phase == 1 && block.Duration > 0 ? FlightLift * WoolEase.Arc(block.Travel / block.Duration) : 0;
        }

```

**Sau**
```csharp
        public float SlotFlash(int index) { return TrayFlash * WoolEase.Arc(Age(slotFlash, index) / TrayFlashTime); }

```

- [ ] **Bước 8: Builder (chỉ code)**

Mũi tên prefab theo ô:

**Trước**
```476:476:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            Part(go.transform, "Arrow", arrowPlate, new Vector3(0, WoolBlockMesh.Thickness + 0.005f, 0), new Vector3(0.56f, 1.56f, 1), charcoal, out view.Arrow);
```

**Sau**
```csharp
            Part(go.transform, "Arrow", arrowPlate, new Vector3(0, WoolBlockMesh.Thickness + 0.005f, 0), new Vector3(0.56f * WoolRouting.CellSize, 1.56f * WoolRouting.CellSize, 1), charcoal, out view.Arrow);
```

`Rebuild Art` gán `WoolScene.Cushion`:

**Trước**
```41:44:Assets/JamJam/Editor/WoolArtBuilder.cs
            Find("Board Cushion", out var cushion);
            var volume = UnityEngine.Object.FindFirstObjectByType<Volume>();
            if (volume == null) volume = new GameObject("Art Volume").AddComponent<Volume>();
            Apply(cameraObject.GetComponent<Camera>(), sunObject.GetComponent<Light>(), ground.GetComponent<Renderer>(), cushion.GetComponent<Renderer>(), volume);
```

**Sau**
```csharp
            Find("Board Cushion", out var cushion);
            var controller = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            if (controller == null) throw new InvalidOperationException("WoolScene is missing from " + ScenePath);
            controller.Cushion = cushion.transform;
            EditorUtility.SetDirty(controller);
            var volume = UnityEngine.Object.FindFirstObjectByType<Volume>();
            if (volume == null) volume = new GameObject("Art Volume").AddComponent<Volume>();
            Apply(cameraObject.GetComponent<Camera>(), sunObject.GetComponent<Light>(), ground.GetComponent<Renderer>(), cushion.GetComponent<Renderer>(), volume);
```

`WoolSceneBuilder.Build` (không chạy) cũng gán, để một lần Build đầy đủ vẫn nhất quán:

**Trước**
```49:49:Assets/JamJam/Editor/WoolSceneBuilder.cs
            WoolAssetsBuilder.Part(null,"Board Cushion",mesh,new Vector3(0,-0.01f,-5.05f),new Vector3(8.7f,0.18f,6.3f),art.Rim,out var cushion);
```

**Sau**
```csharp
            WoolAssetsBuilder.Part(null,"Board Cushion",mesh,new Vector3(0,-0.01f,-5.05f),new Vector3(8.7f,0.18f,6.3f),art.Rim,out var cushion);
            controller.Cushion=cushion;
```

- [ ] **Bước 9: Compile** — `unity recompile`, eval `isCompiling`/`scriptCompilationFailed` như Task 1 Bước 12. Kỳ vọng `False False` (RED Bước 2 hết), không cảnh báo mới.

- [ ] **Bước 10: `Rebuild Art`** — thoát Play Mode, lưu mọi scene đang mở, rồi:

```bash
E 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/WoolJam.unity"); return "open";'
E 'JamJam.Editor.WoolArtBuilder.Rebuild(); return UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().Cushion.name;'
```
Kỳ vọng: `open`; `Board Cushion`; console có `Wool art rebuilt`, không lỗi.

- [ ] **Bước 11: GREEN check edit mode + hồi quy art**

```bash
E 'return JamJam.Editor.WoolArtChecks.Blocks();'
E 'return JamJam.Editor.WoolArtChecks.Stage();'
E 'return JamJam.Editor.WoolArtChecks.Textures() + " | " + JamJam.Editor.WoolArtChecks.Tray() + " | " + JamJam.Editor.WoolArtChecks.Chevron() + " | " + JamJam.Editor.WoolArtChecks.Feel() + " | " + JamJam.Editor.WoolArtChecks.UiGame();'
```
Kỳ vọng: `BLOCKS PASS / … vertices per block`; `STAGE PASS / … framed points at 1080x1920 and 1080x2400`; `TEXTURES PASS / 6 palette colors / 5 textures | TRAY PASS | CHEVRON PASS / … | FEEL PASS / 2 particle prefabs / null haptics | UI GAME PASS / …`. Nếu `Stage` báo một điểm z ≈ −8.55 ra ngoài khung: không tăng `ExitMargin`, báo user (camera nằm ngoài phạm vi plan).

- [ ] **Bước 12: GUID/level/comment**

```bash
cd "/d/Unity Project/Jam_Jam"
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor Assets/Scenes --include=*.meta | sort > "$SCRATCH/motion-run/guids.after.txt"
comm -23 "$SCRATCH/motion-run/guids.before.txt" "$SCRATCH/motion-run/guids.after.txt"
md5sum -c "$SCRATCH/motion-run/levels.md5"
grep -n "  Cushion: " Assets/Scenes/WoolJam.unity
grep -rn "BlockLift\|FlightLift" --include=*.cs Assets
grep -n "//\|/\*" Assets/JamJam/Runtime/WoolScene.cs Assets/JamJam/Runtime/WoolBlockMesh.cs Assets/JamJam/Runtime/WoolFeel.cs Assets/JamJam/Editor/WoolArtChecks.PlayMotion.cs Assets/JamJam/Editor/WoolArtChecks.Stage.cs Assets/JamJam/Editor/WoolArtChecks.Play.cs
```
Kỳ vọng: `comm` rỗng (không GUID nào mất; chỉ thêm `WoolArtChecks.PlayMotion.cs.meta`); ba dòng `OK`; scene có đúng một dòng `Cushion: {fileID: N}` với N khác 0; hai lệnh `grep` cuối rỗng.

- [ ] **Bước 13: Dừng, báo user.**

---

### Task 3: Kiểm thử tổng — lời giải, Play Mode song song, feel/UI/kết quả, ảnh, knob

**Files:**
- Không sửa code (trừ khi chỉnh knob ở bảng cuối task).
- Tạo ảnh: `docs/motion-start.png`, `docs/motion-start-tall.png`, `docs/motion-run.png`, `docs/motion-compare.png`.
- Script tạm (ngoài project): `$SCRATCH/motion-run/compare.py`.

**Interfaces — Consumes:** `WoolArtChecks.PlayMotionArm/Mid/End`, `PlayBlocks`, `PlayFeel`, `PlayFinish(bool)`, `PlayUi`, `PlayResultArm/Early/Late/Quiet`, `WoolEditorChecks.ValidateLevels`.

- [ ] **Bước 1: Hồi quy logic + lời giải 3 level**

```bash
cd "/d/Unity Project/Jam_Jam"
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
E 'JamJam.Editor.WoolEditorChecks.ValidateLevels(); var lines = System.IO.File.ReadAllLines("Logs/WoolJam.verify.txt"); return string.Join("\n", lines, System.Math.Max(0, lines.Length - 3), System.Math.Min(3, lines.Length));'
```
Kỳ vọng: dòng PASS mới của Task 1; ba dòng `LEVEL 1 SOLUTION PASS / 39 units / ≈4.52 s`, `LEVEL 2 SOLUTION PASS / 75 units / ≈9.00 s`, `LEVEL 3 SOLUTION PASS / 120 units / ≈14.74 s`. (`ValidateLevels` chỉ ghi thêm vào log, không chạm asset.)

- [ ] **Bước 2: Lưu PlayerPrefs** (các check thắng sẽ đánh dấu level đã qua)

```bash
E 'return UnityEngine.PlayerPrefs.GetInt("JamJam.Cleared", -1);' | tee "$SCRATCH/motion-run/cleared.txt"
```

- [ ] **Bước 3: Play Mode — song song, nguyên kích thước, tới đúng slot**

Mở scene (`E 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/WoolJam.unity"); return "open";'`), Game view hiển thị, **[PLAY START]**, đợi level nạp, rồi:

```bash
E 'return JamJam.Editor.WoolArtChecks.PlayMotionArm(2);'
```
Kỳ vọng `MOTION ARMED / blocks 0 3`. Đợi ~3 giây thật (≈0.3 s game ở `timeScale` 0.1; chuyến ngắn nhất của hai khối là 1.37 s game):
```bash
E 'return JamJam.Editor.WoolArtChecks.PlayMotionMid();'
```
Kỳ vọng `MOTION MID PASS / travel 0.xx s`. Nếu báo `Both blocks advanced together…` với travel ≤ 0.18 thì đợi thêm 1 giây rồi chạy lại đúng eval này. Chạy lại `PlayMotionMid()` một lần nữa sau ~3 giây (lấy mẫu thứ hai, khối đã lên vành đai) → vẫn PASS. Rồi:
```bash
E 'UnityEngine.Time.timeScale = 1f; return "ts";'
```
Đợi ~3 giây thật:
```bash
E 'return JamJam.Editor.WoolArtChecks.PlayMotionEnd();'
```
Kỳ vọng `MOTION END PASS / slots 0 1`.

- [ ] **Bước 4: Play Mode — block, feel, kết thúc, UI** (vẫn Play Mode; mỗi dòng một eval, level nạp ở eval riêng trước check)

```bash
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(2); return "L3";'
E 'return JamJam.Editor.WoolArtChecks.PlayBlocks();'
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(2); return "L3";'
E 'return JamJam.Editor.WoolArtChecks.PlayFeel();'
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(0); return "L1";'
E 'return JamJam.Editor.WoolArtChecks.PlayFinish(true);'
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(0); return "L1";'
E 'return JamJam.Editor.WoolArtChecks.PlayFinish(false);'
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(0); return "L1";'
E 'return JamJam.Editor.WoolArtChecks.PlayUi();'
E 'return JamJam.Editor.WoolArtChecks.PlayResultArm(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultEarly(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultLate(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultArm(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultEarly(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultLate(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultArm(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultEarly(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultQuiet();'
```
Kỳ vọng lần lượt: `L3`; `PLAY BLOCKS PASS / 24 blocks`; `L3`; `PLAY FEEL PASS / blocked … / launched …`; `L1`; `PLAY FINISH PASS / Won`; `L1`; `PLAY FINISH PASS / Lost`; `L1`; `PLAY UI PASS / juice / pop-in / pulse`; `ARMED / Won`; `EARLY PASS / 0.xx s` (xx < 0.90); `PLAY RESULT PASS / Won / …` (nếu `ART CHECK FAILED: Wait longer: …` thì chạy lại đúng eval đó); `ARMED / Lost`; `EARLY PASS …`; `PLAY RESULT PASS / Lost / …`; `ARMED / Won`; `EARLY PASS / … / replayed`; `PLAY RESULT QUIET PASS` (chạy khi đã qua ≥ 1.2 s thật). Nếu một eval `PlayResultEarly` FAIL: `E 'UnityEngine.Time.timeScale = 1f; return "ts";'` rồi làm lại từ `PlayResultArm`. **[CONSOLE ERRORS]** không lỗi mới.

- [ ] **Bước 5: Ảnh** (vẫn Play Mode)

```bash
E 'UnityEngine.Time.timeScale = 1f; UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(2); return "L3";'
```
Đợi ~1 giây, **[CAPTURE 1080×1920 → docs/motion-start.png]**, **[CAPTURE 1080×2400 → docs/motion-start-tall.png]**. Rồi cho 3 khối chạy chậm:
```bash
E 'var s = UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(2); var n = 0; for (var i = 0; i < s.State.Blocks.Count && n < 3; i++) if (s.State.CanSelect(i)) { s.Select(i); if (s.State.Blocks[i].Phase == 1) n++; } UnityEngine.Time.timeScale = 0.15f; return "moving " + n;'
```
Kỳ vọng `moving 3`. Đợi ~2.5 giây thật, **[CAPTURE 1080×1920 → docs/motion-run.png]**, rồi:
```bash
E 'var g = UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().State; var n = 0; foreach (var b in g.Blocks) if (b.Phase == 1) n++; UnityEngine.Time.timeScale = 1f; return "moving " + n;'
```
Kỳ vọng `moving 2` hoặc `moving 3` (nếu < 2, chụp lại với thời gian đợi ngắn hơn). **[PLAY STOP]**.

Tạo `$SCRATCH/motion-run/compare.py`:
```python
import sys
from PIL import Image


def fit(path, height):
    image = Image.open(path).convert("RGB")
    width = round(image.width * height / image.height)
    return image.resize((width, height), Image.LANCZOS)


output = sys.argv[1]
height = 1280
gap = 24
images = [fit(path, height) for path in sys.argv[2:]]
canvas = Image.new("RGB", (sum(image.width for image in images) + gap * (len(images) - 1), height), (20, 35, 74))
x = 0
for image in images:
    canvas.paste(image, (x, 0))
    x += image.width + gap
canvas.save(output)
print(output, canvas.size)
```
```bash
cd "/d/Unity Project/Jam_Jam"
"$PY" -I "$SCRATCH/motion-run/compare.py" docs/motion-compare.png "$SCRATCH/ref/yt2/oar2.jpg" docs/motion-run.png
```
Kỳ vọng in `docs/motion-compare.png (…, 1280)`. Đọc `docs/motion-start.png`, `docs/motion-run.png`, `docs/motion-compare.png` bằng Read: khối nhỏ hơn, có khe rõ giữa các khối kề; đệm bảng ôm sát lưới; khối đang chạy giữ nguyên kích thước, phẳng trên mặt bàn, đầu (mũi tên) hướng theo chiều chạy, nằm ngoài đệm bảng; vành đai và khay trong khung; cảm giác giống khung tham khảo (xe nhỏ, khe, chạy vòng ngoài). Nếu chưa đạt, chỉnh theo bảng knob dưới đây rồi chạy lại các bước được ghi trong cột "Chạy lại".

| Knob | File | Giá trị | Ảnh hưởng / giới hạn | Chạy lại |
|---|---|---|---|---|
| `CellSize` | `WoolRouting.cs` | 0.8 | Kích thước ô; giảm làm bảng nhỏ hơn. Mọi vị trí tự theo. | Harness, T2 B10–11, T3 B1/3/5 |
| `Inset` | `WoolBlockMesh.cs` | 0.09 ô | Khe = 2·Inset·CellSize (0.144). Check khe yêu cầu ≥ 0.13 → `Inset ≥ 0.082`. | T2 B10–11, T3 B4–5 |
| `Corner` / `Border` | `WoolBlockMesh.cs` | 0.18 / 0.10 | Bo góc / viền vân; `Corner − Bevel − Border` ≥ 0.01. | T2 B10–11 |
| `ExitMargin` | `WoolRouting.cs` | 0.3 | Khoảng hở khi ra khỏi bảng; tăng làm vành đai dưới rời khung (Stage y ≥ 0.12). | Harness, T2 B11 |
| `Speed` | `WoolRouting.cs` | 9 | Chuyến xa nhất 17.92/Speed; giữ `Speed ≥ 7.2` để ≤ 2.5 s. | Harness, T3 B1/3 |
| `LaneZ` | `WoolRouting.cs` | `TrayZ − 0.8` | Độ cao làn trên dưới khay. | Harness, T2 B11 |
| Tốc độ xoay | `WoolScene.RenderFrame` | 1080°/s | Thấp hơn → khối trôi ngang ở góc; cao hơn → bẻ góc gắt. | T3 B3/5 |
| `CushionPad` | `WoolScene.cs` | 0.5 | Tổng lề đệm bảng (0.25 mỗi bên). | T3 B3/5 |
| `LaunchPop` | `WoolFeel.cs` | 0.08 | Đỉnh pop khi xuất phát (1.08). | T3 B4 |

- [ ] **Bước 6: Khôi phục PlayerPrefs** — với `N` trong `cleared.txt`: `E 'UnityEngine.PlayerPrefs.SetInt("JamJam.Cleared", N); UnityEngine.PlayerPrefs.Save(); return UnityEngine.PlayerPrefs.GetInt("JamJam.Cleared", -1);'` (nếu `N = -1`: `E 'UnityEngine.PlayerPrefs.DeleteKey("JamJam.Cleared"); UnityEngine.PlayerPrefs.Save(); return "deleted";'`). Kỳ vọng trả lại đúng `N` (hoặc `deleted`).

- [ ] **Bước 7: Kiểm cuối**

```bash
cd "/d/Unity Project/Jam_Jam"
git status --short | grep "^ D"
md5sum -c "$SCRATCH/motion-run/levels.md5"
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
```
Kỳ vọng: không có file bị xóa; ba dòng `OK`; dòng PASS mới. **[CONSOLE ERRORS]** không lỗi mới.

- [ ] **Bước 8: Dừng, báo user** — gửi `docs/motion-start.png`, `docs/motion-run.png`, `docs/motion-compare.png`, dòng PASS mới, ba dòng `LEVEL n SOLUTION PASS`, danh sách knob đã chỉnh (nếu có). Không commit.

## Tóm tắt

- Luật mới trong logic thuần: `WoolRouting.CellSize` 0.8 cho mọi chuyển đổi ô → thế giới; `TryFind` dựng route hình học — đoạn thẳng theo mũi tên tới vành đai (`RingOffset = 0.3 + max(W,H)·0.8/2`), vòng theo vành đai ngoài bảng về phía khay (Down chọn phía ngắn hơn theo slot), làn trên `TrayZ − 0.8`, vào slot; không còn `ScaleAt`/`TravelScale`/`ArrowDistance`, khối nguyên kích thước suốt đường. `WoolGame` bỏ hàng đợi `departures" --include`: mọi khối Phase 1 cùng tiến mỗi `Step`, mỗi spool có `Arrival` riêng, `spoolOrder` giữ nguyên; kiểm làn tính khối đang chạy ở vị trí hiện tại, nguyên kích thước. `Speed` 9 (chuyến xa nhất 1.99 s).
- Harness viết lại theo TDD: route (điểm đầu/cuối, Manhattan, đoạn đầu theo mũi tên, mọi điểm sau ngoài bảng, cách bảng ≥ `RingOffset`, phía vòng của Down, làn trên bảng cao, API co nhỏ đã biến mất), song song (cùng tiến ngay frame đầu, `Arrival` độc lập, chọn lệch thời điểm, pause), chặn bởi khối đang chạy ngang làn rồi mở lại. FeelChecks/ProgressChecks không đổi. Dòng PASS mới: `PASS: 105 gameplay assertions; 13011 route assertions; 18 progress assertions; 108 feel assertions`.
- Hiển thị: mesh inset 0.09 ô (khe 0.144), bo góc 0.18, viền 0.10; mũi tên và collider theo kích thước mới; khối đang chạy phẳng, scale 1 (chỉ pop 1.08 lúc xuất phát), xoay cả khối theo hướng chạy 1080°/s; bỏ nhấc cung; đệm bảng co theo level qua `WoolScene.Cushion` (gán bởi `Rebuild Art`). Builder chỉ sửa code; asset sinh lại bằng `Rebuild Art`, GUID và level giữ nguyên.
- Kiểm thử: harness RED→GREEN; check editor RED→GREEN (`Blocks`, `Stage` bao mọi điểm vành đai ở 1080×1920/1080×2400, `PlayBlocks` collider + khe, `PlayFeel`); Play Mode `PlayMotionArm/Mid/End` (hai khối cùng `Travel`, đúng điểm route, scale 1, tới đúng slot); `ValidateLevels` cả ba level còn lời giải; `PlayFinish`, `PlayUi`, `PlayResult*`; ảnh `docs/motion-*.png` ghép so với khung tham khảo.
- Lệch spec có ghi lý do: vành đai giãn thêm nửa cạnh dài của khối (để khối nguyên kích thước thực sự ở ngoài bảng); làn trên nâng lên cạnh vành đai khi bảng ≥ 7 hàng; độ dày khối 0.35 không nhân 0.8.
