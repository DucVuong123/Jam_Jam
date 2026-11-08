# Màn chơi bố cục mới theo ảnh mẫu

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Màn `WoolJam.unity` giống `videoframe_11278.png`. HUD gọn ở góc. Sân rồng nền cỏ ở trên. Khay nằm trên kệ gỗ ở giữa. Bảng khối hộp len cao, to, nằm trên nền kem ở dưới. Sợi len dày, liên tục. Có nút "Unlock +" dưới ô khóa và nút tốc độ x1/x2.

**Kiến trúc:**
- `WoolStage` (Runtime, thuần, có trong harness) giữ hình học camera: pitch 50°, khớp theo chiều ngang, dải HUD, dải nội dung. `WoolScene.FitCamera()` áp nó mỗi khi load level hoặc khi tỉ lệ màn hình đổi, rồi báo cho `WoolFeel.Rebase` để rung/zoom quay về đúng gốc mới.
- `WoolRouting` đổi hằng số hình học `CellSize`, `BoardCenterZ`, `SlotSpacing`; luật không đổi.
- `JamJam/Rebuild Art` dựng nền cỏ, kệ gỗ, màu ấm, camera mới.
- `JamJam/Rebuild UI` dựng HUD góc, nút tốc độ, mẫu nút Unlock, và chuyển Retry vào Pause.
- `WoolScene` thêm tốc độ, nút Unlock bám ô, sợi len mượt.

**Tech Stack:** Unity 6000.3.19f1, URP 17.3, C# 9, uGUI legacy `Text`, R3 1.3.1, Python+PIL (ghép ảnh so sánh).

**Spec:** `docs/superpowers/specs/2026-11-08-wool-gameplay-layout-design.md`

## Ràng buộc chung

- Plan bằng tiếng Việt; code, identifier, lệnh giữ tiếng Anh. Không comment trong code (`//`, `/* */`, `///`).
- Method trả reference dùng interface/abstract; factory/tìm kiếm dùng `void` + `out`. Timer/animation chỉ R3. Không coroutine, DOTween, UniTask.
- Không `git add`/`git commit`. Mỗi task kết thúc bằng "Dừng, báo user".
- KHÔNG chạy `WoolSceneBuilder.Build`, `WoolLobbyBuilder.Build`, `WoolEditorChecks.RunBatch`. Hai builder chỉ sửa code để cùng kết quả và phải compile.
- Patch scene chỉ bằng `JamJam.Editor.WoolArtBuilder.Rebuild()` và `JamJam.Editor.WoolUiBuilder.Rebuild()`. Trước mỗi lần rebuild: thoát Play Mode, lưu mọi scene.
- Không sửa `Generated/Levels/*.asset`, ảnh trong `Art/UI` (md5 giữ nguyên), luật trong `WoolGame.cs` (chỉ được đổi hằng số hình học trong `WoolRouting.cs`).
- Chuỗi `m_Text` scene-owned giữ nguyên. Chữ không dùng trên màn chơi thì `SetActive(false)`. Object UI mới do builder tạo: `Speed`, `Speed/Label`, `UnlockTemplate`, `UnlockTemplate/Label`, `ProgressIcon`.
- C# 9: không file-scoped namespace, không global using. Với `UnityEngine.Object` luôn so `== null`, không `?.`/`??`.
- Số dòng trong **Trước** là của file ngày 2026-11-08. Khi task trước đã sửa file, tìm theo nội dung **Trước**.
- Lệnh (Git Bash):
  ```bash
  cd "/d/Unity Project/Jam_Jam"
  SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"
  PY="$USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe"
  U="--caller plugin --skill unity-cli --no-banner"
  E(){ unity command eval $U "$1"; }
  CAP(){ unity command game_view $U --source camera --overlay true --width 1080 --height "$2" --output-path "$1"; }
  ```
  - `unity recompile` (không cờ `--caller`). Nếu trả HTTP 400 thì xác nhận bằng `E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'` → `False False`.
  - Play Mode: `unity command set_run_in_background --enabled true $U`, `unity command start $U`, `unity command stop $U`. Cuối task trả về `set_run_in_background --enabled false`.
  - Python luôn chạy `"$PY" -I`.
- Check `WoolArtChecks.*` ném `InvalidOperationException("ART CHECK FAILED: …")` khi sai. Chuỗi ghi dưới mỗi lệnh là kết quả khi đúng.
- Harness: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`. Dòng hiện tại: `PASS: 418 gameplay assertions; 16296 route assertions; 18 progress assertions; 108 feel assertions`. Task 1 đổi số route và thêm nhóm `stage`; ghi dòng mới vào báo cáo.
- Quirk check đã biết:
  - `PlayFinish(false)` cần `LoadLevel(0)` sau `PlayFinish(true)`.
  - Case thua của `PlayResult` cần `Time.timeScale = 0.05f` trong cùng eval với `Arm`.

## Hình học đã tính (pitch 50°, sin 0.766, cos 0.643)

Tọa độ màn hình dọc `v` của điểm mặt đất = `(z − camZ)·sin50`. Mọi khối đều là 2×1 hoặc 1×2, nên `MaxHalf = CellSize`.

| Đại lượng | Giá trị | Lý do |
|---|---|---|
| `WoolRouting.CellSize` | 0.7 → **0.9** | Ô ở vành đai bên của bảng 8 cột nằm ở `x = 5·0.9 + 0.25 = 4.75`, mép khối mỏng tới 5.12, vẫn < nửa rộng 5.4. Bảng 8 cột rộng 7.2, bằng 67% màn hình. |
| `WoolRouting.BoardCenterZ` | −5.05 → **−6.6** | Bảng 6 hàng có đỉnh −3.9. Làn trên ở −2.75, mép làn −1.85, nằm dưới mép pad −1.21 với dư 0.64; chỗ dư này để nút Unlock. `LaneFitsTray` của cả 3 level vẫn đúng. |
| `WoolRouting.SlotSpacing` | 1.27 → **1.6** | 6 ô trải `x ±4.0`. Pad rộng 1.4 (sửa mesh `Slot`), khe giữa hai pad 0.2. |
| `WoolRouting.TrayZ` | giữ −0.75 | Đường rồng (z 0.01…8.75 kể cả lề đường) giữ nguyên, không scale. |
| `WoolBlockMesh.Thickness` | 0.35 → **0.54** | 0.6 ô. |
| `WoolStage.Pitch` | **50** | |
| `WoolStage.HalfWidth` | **5.4** | Màn rộng 10.8 đơn vị; canvas 1080 px ⇒ 100 px/đơn vị khi khớp ngang. |
| `WoolStage.HudBand` | **1.8** | Dải HUD 154 px ref; canvas match 0.5 cho ra ≤ 172 px ở 1080×2400, tức ≤ 1.72 đơn vị. |
| `WoolStage.BottomBand` | **0.3** | |
| `WoolStage.ContentTop` | **7.7** | Đỉnh nội dung so với tâm khay theo `v`: lề trên của đường z 8.75, cộng đầu rồng và lề ≈ z 9.3, nên `(9.3 + 0.75)·0.766 ≈ 7.7`. |
| `WoolStage.ContentBottom` | **−7.98** | Đáy vành đai dưới của bảng 6 hàng: z −6.6 − 2.7 − 0.25 − 0.9 − 0.37 = −10.82, nên `(−10.82 + 0.75)·0.766 = −7.71`, cộng lề 0.27. |
| `WoolStage.Lift` | **0.61** | `(ContentTop + ContentBottom)/2 + (HudBand − BottomBand)/2`. |
| `WoolStage.TargetZ` | **0.0463** | `TrayZ + Lift / sin50 = −0.75 + 0.7963`. Camera nhìn vào `(0, 0, TargetZ)` từ khoảng cách 24. |
| `WoolStage.MinSize` | **8.89** | `max(ContentTop − Lift + HudBand, Lift − ContentBottom + BottomBand)` = `max(8.89, 8.89)`. |
| `WoolStage.Size(aspect)` | `max(HalfWidth / aspect, MinSize)` | 1080×1920 → 9.6; 1080×2400 → 12; 3:4 → 8.89. |

`Lift` không phụ thuộc `size`, nên mọi tỉ lệ màn hình đều căn nội dung giữa dải HUD và dải đáy; phần dư chia đều trên/dưới.

## Quyết định (spec để ngỏ hoặc phải lệch)

| Chủ đề | Quyết định |
|---|---|
| Co đường rồng | **Lệch spec.** Không cần `PathScale`: tính ra đường giữ nguyên kích thước vẫn vừa vùng sân rồng. `WoolStage` chỉ giữ hình học camera, không có `ToWorld`. Check `stage` của harness kiểm `Size`, `Lift`, `TargetZ`, `MinSize`. |
| Tỉ lệ vùng | **Lệch spec, ghi nhận.** Ở 1080×1920: HUD 0–9%, sân rồng ~9–50%, khay ~50–58%, bảng ~58–92%. Bảng nhỏ hơn mục tiêu 45% vì vành đai xe chạy phải nằm trong khung ngang. |
| Mặt bên tối | Shader `JamJam/WoolKnit` đã có `_SideShade`. Không thêm vertex color; giữ `BlockSideShade` hiện tại, chỉ tăng nếu ảnh chụp cho thấy mặt bên chưa tách khỏi mặt trên (task 2 quyết định). |
| Cỏ / kem | `Ground.mat` đổi màu `#F6E7CF`. Thêm cube `Meadow` (Rebuild Art tạo nếu thiếu): vị trí `(0, −0.035, 9.9)`, scale `(35, 0.01, 20.2)` (z từ −0.2 tới 20), material mới `Meadow.mat` = fabric `#BFE8C8` tiling `(18, 10)`, không collider, không đổ bóng. Camera clear color `#F6E7CF`. |
| Kệ gỗ | Thêm object `Tray Shelf` (Rebuild Art tạo nếu thiếu): mesh `Rounded.asset`, vị trí `(0, −0.02, TrayZ − 0.05)`, scale `(10.1, 0.14, 1.3)`, material mới `Shelf.mat` (không texture, `#E2B577`, smoothness 0.25). Không collider. |
| Đường, pad, nỉ bảng | `Road` `#E9C99A`, `RoadRim` `#B98755`, `Pad` `#F3E3C6`, `PadLocked` `#CDBFA8`, `Cushion` `#EBDDC4` (kem đậm hơn nền một chút). `Board Cushion` dời z theo `BoardCenterZ` (Rebuild Art và `LoadLevel`). |
| Post | Vignette giữ intensity 0.22 nhưng đổi màu sang nâu ấm `(0.25, 0.17, 0.1)`; saturation/contrast/bloom giữ. Check `Stage` cập nhật mô tả "Navy" → "Cream". |
| Spool / số | Spool đã nằm ngang (trục X). Nhãn `Counter` giữ ở z −0.52 so với spool, xoay theo `WoolStage.Pitch` (mọi `Label` trong `WoolAssetsBuilder` dùng `WoolStage.Pitch` thay `CameraPitch`; xóa hằng `CameraPitch`). |
| Nhãn "+" của pad | `WoolSlotView.Label` luôn ẩn (`SetActive(false)`); nút UI Unlock thay chỗ nó. Click vào pad khóa vẫn mở ô như cũ. |
| Nút Unlock | `WoolScene.UnlockTemplate` (Button, inactive) nằm trong Canvas. `LoadLevel` clone mỗi ô một nút thành `Unlock_i` (cùng parent), lưu vào list `unlockButtons` (hủy ở `LoadLevel` sau). Vị trí = chiếu `SlotPosition(i) + (0, 0, −0.75)` qua `gameCamera.WorldToScreenPoint`, rồi `RectTransformUtility.ScreenPointToLocalPointInRectangle` vào rect cha (camera `null`, canvas overlay). Đặt lại trong `FitCamera`. `RenderFrame`: `active = i >= OpenSlots`, `interactable = i == OpenSlots`, nhãn trắng mờ alpha 0.5 khi không bấm được. onClick gọi `Unlock()`. Kích thước 230×80 (≥ 96 px chạm nhờ `WoolButtonJuice` + vùng raycast = cả nút; check đòi ≥ 72 cao, ≥ 96 rộng). |
| Sợi len | Độ rộng `0.2 → 0.15` (đầu spool → đầu rồng). Điểm cuối mượt: list `threadEnds` (Vector3). Khi đổi target thì lerp tới vị trí đơn vị mới với `1 − exp(−18·delta)`; lần đầu (vừa bật) gán thẳng. Sợi võng: `point = Lerp(a, b, t) + up·(0.35·sin πt) + back·(0.12·|b − a|·sin πt)` với `back = −Vector3.forward`, cộng rung nhẹ hiện có. Hiện khi `visible && spool.Remaining > 0 && spool.Target >= 0`. Giữa hai lần kéo, `WoolGame.Step` chọn target mới ngay trong cùng Step nên sợi không tắt; khi không còn đơn vị cùng màu thì ẩn. |
| Tốc độ | `public static int Speed { get; private set; } = 1;` và `public void ToggleSpeed()` (1 ↔ 2, cập nhật `SpeedLabel.text = "x" + Speed`). Field `public Button SpeedButton; public Text SpeedLabel;`. `Tick`: `scaled = delta·Speed`. `State.Step` được gọi `ceil(scaled / 0.1)` lần, mỗi lần `scaled / n`. `feel.Step(State, scaled)` và `RenderFrame(scaled)` gọi một lần. Hàm `public static void ResetSpeed()` chỉ dùng cho check, trả `Speed` về 1. |
| HUD | Mọi vị trí tính theo ref 1080×1920. `Pause`: neo trên-trái, `anchoredPosition (100, −100)`, size 120×120, `WoolUiArt.Round(pause, "Button-Round-Orange", "Icon-Settings")` (fallback: tròn procedural + chữ "II"). `LevelBadge`: neo trên-trái, pivot (0, 0.5), `(40, −205)`, size 260×60, `Image.enabled = false`, ẩn con `Face` nếu có. `LevelCaption`: rect (0,0)…(120,60) căn phải, cỡ 36, `Light`. `LevelNumber`: (128,0)…(260,60) căn trái, cỡ 36, `Light`. `ProgressIcon` (Image mới, `Icon-YarnBall`, 44×44): neo trên-trái `(62, −262)`; thiếu ảnh thì `UiCircle` màu `WoolUiTheme.Warm`. `ProgressNumber`: neo trên-trái, pivot (0, 0.5), `(92, −262)`, 200×50, căn trái, cỡ 32, `Light`. `Speed` (Button mới): neo trên-phải `(−130, −100)`, 200×96, `WoolUiArt.Button(speed, WoolUiTone.Warm…)`. `Button-Orange` có ảnh thì skin bằng ảnh cam (`Warm` → `Button-Orange`, mới thêm vào map) + `Icon-Play`, nhãn `Light` cỡ 40 "x1"; thiếu ảnh thì pill procedural `Warm`. |
| Ẩn | `Brand`, `Goal`, `ProgressTrack`, `TrayCaption`, `Instructions`, `Help`: `SetActive(false)`. `ShowProgress` vẫn chạy trên object ẩn. |
| Retry | Reparent `Retry` vào `PausePanel/Card` (giữ wiring `RetryButton`). Card `Fit` 820×640 → 820×760. Vị trí: Title (0, 220), Body (0, 120), Resume (0, −40), Retry (0, −160) size = size Resume, Home (0, −280) (thay `PauseHomePosition`). Skin Retry giữ `Button-Cream` + `Icon-Retry`. |
| Builder đầy đủ | `WoolSceneBuilder.BuildCanvas` tạo thêm `Speed`, `UnlockTemplate`, `ProgressIcon` và gán field mới, để build lại từ đầu ra cùng kết quả. Chỉ compile, không chạy. |

## Review Focus

1. **Đổi tỉ lệ màn hình giữa chừng** (xoay, game view đổi kích thước) khi camera đang rung: rung phải về đúng gốc **mới**, không nhảy về size cũ. Test: `PlayLayout` đổi aspect lúc trauma > 0, chờ 1.5 s, camera = `WoolStage` của aspect mới (sai số 1e-4).
2. **x2 + frame dài**: `delta` thực 0.1 s ở x2 phải tiến `Elapsed` 0.2 s, không bị kẹp còn 0.1. Test trong `PlaySpeed`.
3. **Unlock bấm liên tục hoặc khi Pause**: chỉ ô khóa đầu tiên mở được; khi Pause, nút Unlock không mở (luật `UnlockSlot` hiện tại không chặn Pause, nên `WoolScene.Unlock` phải chặn `State.Paused`). Test trong `PlayUnlock`.
4. **Thay level với tốc độ x2**: `Speed` giữ nguyên, nhãn đúng "x2" sau `LoadLevel`. Test trong `PlaySpeed`.
5. **Màn tablet 3:4**: nội dung vẫn trong khung (`Size = MinSize`). Test: `Stage` thêm aspect 0.75.

---

### Task 1: Hình học và camera

**Files:**
- Create: `Assets/JamJam/Runtime/WoolStage.cs`
- Modify: `Assets/JamJam/Runtime/WoolRouting.cs:17-22`, `Assets/JamJam/Runtime/WoolBlockMesh.cs:8`, `Assets/JamJam/Runtime/WoolScene.cs`, `Assets/JamJam/Runtime/WoolFeel.cs:99-113`, `Assets/JamJam/Editor/WoolArtBuilder.cs`, `Assets/JamJam/Editor/WoolAssetsBuilder.cs` (`CameraPitch`, mesh `Slot`), `Assets/JamJam/Editor/WoolSceneBuilder.cs:50`
- Test: `Tools/WoolJamChecks/` (thêm `WoolStage.cs` vào csproj như các file Runtime thuần khác, thêm nhóm check `stage`), `Assets/JamJam/Editor/WoolArtChecks.Stage.cs`, `WoolArtChecks.Tray.cs`, `WoolArtChecks.Blocks.cs`, `Assets/JamJam/Editor/WoolArtChecks.PlayLayout.cs` (mới)

**Interfaces:**
- Produces:
  - `JamJam.WoolStage` (thuần, không UnityEngine): hằng `Pitch`, `HalfWidth`, `HudBand`, `BottomBand`, `ContentTop`, `ContentBottom`, `Distance = 24f`; property tĩnh `Lift`, `TargetZ`, `MinSize`; `static float Size(float aspect)`. Pose camera (Vector3/Quaternion) tính trong `WoolScene.FitCamera` và `WoolArtBuilder.Apply`.
  - `WoolScene.FitCamera()` (public), `WoolScene.CameraFitAspect` (float, public get).
  - `WoolFeel.Rebase(Vector3 position, Quaternion rotation, float size)`.

- [ ] **Step 1: Viết check harness `stage` (fail vì chưa có `WoolStage`)**

Trong file check của harness (nhóm mới, đếm vào dòng PASS là `N stage assertions`):

```csharp
Check(Math.Abs(WoolStage.Size(1080f/1920f) - 9.6f) < 1e-4f, "stage size 1080x1920");
Check(Math.Abs(WoolStage.Size(1080f/2400f) - 12f) < 1e-4f, "stage size 1080x2400");
Check(Math.Abs(WoolStage.Size(0.75f) - WoolStage.MinSize) < 1e-4f, "stage size tablet uses MinSize");
Check(Math.Abs(WoolStage.Lift - 0.61f) < 1e-4f, "stage lift");
Check(Math.Abs(WoolStage.TargetZ - (WoolRouting.TrayZ + 0.61f / (float)Math.Sin(50 * Math.PI / 180))) < 1e-4f, "stage target");
Check(Math.Abs(WoolStage.MinSize - 8.89f) < 1e-3f, "stage min size");
Check(WoolStage.ContentTop - WoolStage.Lift + WoolStage.HudBand <= WoolStage.Size(1080f/1920f) + 1e-4f, "content top below HUD at 1080x1920");
Check(WoolStage.Lift - WoolStage.ContentBottom + WoolStage.BottomBand <= WoolStage.Size(1080f/1920f) + 1e-4f, "content bottom above bottom band at 1080x1920");
```

Tạo `Tools/WoolJamChecks/StageChecks.cs` theo mẫu `ProgressChecks.cs` (cùng helper assert, cùng cách trả số assertion), gọi từ `Program.cs` và in thêm `; N stage assertions` vào cuối dòng PASS. Thêm `<Compile Include="StageChecks.cs" />` và `<Compile Include="../../Assets/JamJam/Runtime/WoolStage.cs" />` vào csproj. Chạy harness: build fail vì thiếu `WoolStage`.

- [ ] **Step 2: Tạo `Runtime/WoolStage.cs`**

```csharp
using System;

namespace JamJam
{
    public static class WoolStage
    {
        public const float Pitch = 50f;
        public const float HalfWidth = 5.4f;
        public const float HudBand = 1.8f;
        public const float BottomBand = 0.3f;
        public const float ContentTop = 7.7f;
        public const float ContentBottom = -7.98f;
        public const float Distance = 24f;

        public static float Lift => (ContentTop + ContentBottom) * 0.5f + (HudBand - BottomBand) * 0.5f;
        public static float TargetZ => WoolRouting.TrayZ + Lift / (float)Math.Sin(Pitch * Math.PI / 180);
        public static float MinSize => Math.Max(ContentTop - Lift + HudBand, Lift - ContentBottom + BottomBand);

        public static float Size(float aspect)
        {
            if (aspect <= 0 || float.IsNaN(aspect) || float.IsInfinity(aspect)) return MinSize;
            return Math.Max(HalfWidth / aspect, MinSize);
        }
    }
}
```

Thêm vào csproj harness theo cùng cách các file Runtime thuần đang được include.

- [ ] **Step 3: Đổi hằng hình học**

`WoolRouting.cs` **Trước** `17:22:Assets/JamJam/Runtime/WoolRouting.cs`:
```csharp
        public const float CellSize = 0.7f;
        public const float ExitMargin = 0.25f;
        public const float BoardCenterZ = -5.05f;
        public const float TrayZ = -0.75f;
        public const float PadHalfDepth = 0.41f;
        public const float LaneClearance = 0.05f;
```
**Sau**: `CellSize = 0.9f`, `BoardCenterZ = -6.6f`, còn lại giữ. Dòng 23 `SlotSpacing = 1.27f` → `1.6f`.

`WoolBlockMesh.cs:8` `Thickness = 0.35f` → `0.54f`.

Chạy harness. Check `stage` PASS. Check route có thể đổi **số** assertion nhưng phải PASS hết. Nếu một assertion route có giá trị cứng theo 0.7/−5.05/1.27, sửa nó để suy ra từ hằng (không nới điều kiện). Ghi dòng PASS mới.

- [ ] **Step 4: `WoolFeel.Rebase`**

**Trước** `99:113:Assets/JamJam/Runtime/WoolFeel.cs` (đoạn `Begin` lưu `cameraPosition/cameraRotation/cameraSize` khi `camera != view`). Giữ nguyên, thêm method ngay sau `Begin`:

```csharp
        public void Rebase(Vector3 position, Quaternion rotation, float size)
        {
            cameraPosition = position;
            cameraRotation = rotation;
            cameraSize = size;
            if (view == null) return;
            if (Trauma <= 0 && CameraZoom <= 0) Restore();
        }
```

Dùng đúng tên property trauma/zoom hiện có trong `WoolFeel` (`Trauma`, `CameraZoom` đã được check dùng).

- [ ] **Step 5: `WoolScene.FitCamera` và chỗ gọi**

- Thêm field `float fittedAspect;`. Thêm `public float CameraFitAspect => fittedAspect;`.
- Thêm method:

```csharp
        public void FitCamera()
        {
            if (gameCamera == null) return;
            var aspect = gameCamera.aspect;
            var rotation = Quaternion.Euler(WoolStage.Pitch, 0, 0);
            var position = new Vector3(0, 0, WoolStage.TargetZ) - rotation * Vector3.forward * WoolStage.Distance;
            var size = WoolStage.Size(aspect);
            fittedAspect = aspect;
            if (feel != null) feel.Rebase(position, rotation, size);
            else { gameCamera.transform.SetPositionAndRotation(position, rotation); gameCamera.orthographicSize = size; }
            PlaceUnlockButtons();
        }
```

- `PlaceUnlockButtons()` (private) ở task này là method rỗng; Task 3 viết thân.
- Trong `LoadLevel`: sau `feel.Restart(State);` gọi `FitCamera();`.
- Trong `Tick`, trước `if (State == null || State.Paused) return;`: `if (gameCamera != null && Mathf.Abs(gameCamera.aspect - fittedAspect) > 1e-4f) FitCamera();`.
- `LoadLevel`: dòng `Cushion.localScale = …` thêm `Cushion.position = new Vector3(Cushion.position.x, Cushion.position.y, WoolRouting.BoardCenterZ);`.
- Collider khối: `0.55f` → `WoolBlockMesh.Thickness + 0.2f`, center y `0.28f` → `(WoolBlockMesh.Thickness + 0.2f) * 0.5f`.

- [ ] **Step 6: Editor camera / pitch / slot**

- `WoolArtBuilder.Apply`:
  - `camera.orthographicSize = WoolStage.Size(1080f / 1920f)`.
  - `rotation = Quaternion.Euler(WoolStage.Pitch, 0, 0)`.
  - `position = new Vector3(0, 0, WoolStage.TargetZ) - forward * WoolStage.Distance`.
  - Xóa `CameraSize`, `CameraDistance`, `CameraTarget` nếu không còn ai dùng (grep cả `Editor/`; check nào dùng thì đổi sang `WoolStage`).
- `WoolArtBuilder.Rebuild`: đặt `cushion.transform.position.z = WoolRouting.BoardCenterZ`.
- `WoolAssetsBuilder`: xóa `CameraPitch`, mọi chỗ dùng đổi sang `WoolStage.Pitch`. Mesh `Slot` scale `(1.12, 0.12, 0.82)` → `(1.4, 0.12, 0.82)`, collider `(1.4, 0.3, 0.82)`.
- `WoolSceneBuilder.cs:50`: vị trí cushion z `-5.05f` → `WoolRouting.BoardCenterZ`.

- [ ] **Step 7: Check editor**

- `Stage()`:
  - pitch kỳ vọng `WoolStage.Pitch`.
  - Vòng aspect `{1080/1920, 1080/2400, 0.75}`: đặt `camera.aspect = a`, `camera.orthographicSize = WoolStage.Size(a)`, camera ở pose `WoolStage`. `finally` trả size/pose/aspect cũ.
  - Biên viewport: `x ∈ (0.02, 0.98)`, `y ∈ (BottomBand / (2·size), 1 − HudBand / (2·size))`.
  - Điểm slot dùng nửa rộng 0.7 (pad mới).
  - Thêm điểm đầu rồng/mèo: với mỗi level, điểm cuối path + hướng·0.85 ± (1.1, 0, 1.1).
- `Tray()`: so pitch với `WoolStage.Pitch`.
- `Blocks()`: bounds dùng `WoolBlockMesh.Thickness` (tự đúng).
- `WoolArtChecks.PlayLayout.cs` mới, `public static string PlayLayout()` (Play Mode, level 0):
  - camera pose/size = `WoolStage` cho aspect hiện tại (1e-4).
  - Đổi `Camera.main.aspect = 0.45f`, gọi `scene.FitCamera()`, thêm trauma (dùng API thêm trauma mà `PlayFeel` đang dùng), đợi 1.5 s (`Time.timeScale` bình thường, eval nhiều lượt như `PlayFeel`). Camera về pose `WoolStage` với size `Size(0.45)` (1e-4).
  - `ResetAspect()`, `FitCamera()`.
  - Trả `"PLAY LAYOUT PASS"`.

- [ ] **Step 8: Chạy**

```bash
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
unity recompile
E 'JamJam.Editor.WoolArtBuilder.Rebuild(); return "ok";'
E 'return JamJam.Editor.WoolArtChecks.Stage();'
E 'return JamJam.Editor.WoolArtChecks.Tray();'
E 'return JamJam.Editor.WoolArtChecks.Blocks();'
E 'return JamJam.Editor.WoolEditorChecks.ValidateAllLevels();'
```

Kỳ vọng:
- Harness: `PASS: … stage assertions`.
- `STAGE PASS / …`.
- Tray/Blocks PASS.
- Validate: 3 level hợp lệ.

Tên hàm validate lấy đúng tên trong `WoolEditorChecks` (menu `Validate All Levels`).

Play Mode: `PlayLayout`, `PlayBlocks`, `PlayMotion`, `PlayFeel` (gọi theo cách các plan trước gọi) đều PASS. Chụp `CAP "$SCRATCH/layout/t1.png" 1920` và xem ảnh.

- [ ] **Step 9: Dừng, báo user** (dòng PASS harness mới, ảnh t1).

---

### Task 2: Thế giới ấm, kệ gỗ, sợi len

**Files:**
- Modify: `Assets/JamJam/Editor/WoolAssetsBuilder.cs:67-72` (màu), `Assets/JamJam/Editor/WoolArtBuilder.cs` (Meadow, Tray Shelf, clear color, vignette), `Assets/JamJam/Runtime/WoolScene.cs` (sợi len, ẩn nhãn "+"), `Assets/JamJam/Editor/WoolSceneBuilder.cs` (tạo Meadow/Shelf qua cùng hàm của `WoolArtBuilder`)
- Test: `WoolArtChecks.Stage.cs`, `WoolArtChecks.PlayLayout.cs` (thêm `PlayThread`)

**Interfaces:**
- Consumes: hằng Task 1.
- Produces:
  - `WoolArtBuilder.EnsureMeadow(out GameObject meadow)`, `WoolArtBuilder.EnsureShelf(out GameObject shelf)` (public, tạo nếu thiếu, luôn đặt lại transform/material).
  - Material `Generated/Materials/Meadow.mat`, `Shelf.mat`.
  - `WoolScene.ThreadVisible(int slot)` (bool) và `WoolScene.ThreadEnd(int slot)` (Vector3), dùng cho check.

- [ ] **Step 1: Check fail trước**

`Stage()` thêm:
- `GameObject.Find("Meadow")` có material `Meadow.mat`, màu `#BFE8C8`.
- `Tray Shelf` có `Shelf.mat`.
- `camera.backgroundColor ≈ #F6E7CF`.
- `Ground.mat` màu `#F6E7CF`.
- Vignette màu ấm (r > b).

Recompile + `Stage()` → FAIL "Meadow".

- [ ] **Step 2: Màu và object**

- Đổi màu theo bảng Quyết định.
- `Mat("Meadow", "#BFE8C8", fabric, null, new Vector2(18, 10), 0.05f, out _)`.
- `Shelf.mat` theo cách `Mat` không texture.
- `EnsureMeadow`/`EnsureShelf` theo transform trong Quyết định, gọi trong `Rebuild()` và `WoolSceneBuilder` (sau khi tạo ground).
- Clear color, vignette.
- Chạy `Rebuild()` → `Stage()` PASS.

- [ ] **Step 3: Sợi len**

- `ThreadStartWidth = 0.2f`, `ThreadEndWidth = 0.15f`. Thêm `readonly List<Vector3> threadEnds` và `readonly List<int> threadTargets`, clear/add cùng `threads`.
- **Trước** (trong `RenderFrame`, khối từ `threads[i].enabled = visible && spool.Target >= 0;` tới hết vòng `for (var j = 0; j < 20; j++)`): giữ cấu trúc, đổi thành:
  - `var show = visible && spool.Remaining > 0 && spool.Target >= 0;`, `threads[i].enabled = show;`.
  - Khi `show`:
    - `var goal = units[spool.Target].position + Vector3.up * 0.22f;`.
    - Nếu `threadTargets[i] < 0 || delta == 0` thì `threadEnds[i] = goal`, ngược lại `threadEnds[i] = Vector3.Lerp(threadEnds[i], goal, 1 - Mathf.Exp(-18 * delta))`.
    - `threadTargets[i] = spool.Target`.
  - Khi không `show`: `threadTargets[i] = -1`.
  - Điểm: `a = threadEnds[i]`, `b = spoolViews[i].position + Vector3.up * 0.15f`, `sag = Mathf.Sin(t * Mathf.PI)`, `point = Vector3.Lerp(a, b, t) + Vector3.up * (0.35f * sag) + Vector3.back * (0.12f * Vector3.Distance(a, b) * sag) + rung hiện có`.
- `slots[i].Label.gameObject.SetActive(false)` (thay dòng `SetActive(i >= State.OpenSlots)`).
- Thêm `public bool ThreadVisible(int slot)` và `public Vector3 ThreadEnd(int slot)`, kiểm biên, ngoài biên trả `false`/`kittenRest`.

- [ ] **Step 4: `PlayThread()`**

Trong `WoolArtChecks.PlayLayout.cs`. Play Mode, level 0:
- Chọn khối hợp lệ đầu tiên, step cho tới khi spool của nó có `Target >= 0`.
- Mỗi frame cho tới khi `Remaining == 0`:
  - `ThreadVisible(slot)` luôn true khi `Remaining > 0 && Target >= 0`.
  - Đếm số frame liên tiếp `Remaining > 0` mà sợi tắt. Số đó phải bằng số frame không có target (target ≥ 0 thì phải hiện).
  - Khoảng cách `ThreadEnd` giữa hai frame liên tiếp ≤ `0.5` (không nhảy tức thì).
- Sau `Remaining == 0`: `ThreadVisible == false`.
- `LineRenderer.startWidth == 0.2f`.
- Trả `"PLAY THREAD PASS / <n> frames"`.

- [ ] **Step 5: Chạy**

- `Rebuild()` art.
- `Stage()`, `Tray()`.
- Play Mode: `PlayThread`, `PlayFeel`, `PlayBlocks`, `PlayArt` PASS.
- `CAP "$SCRATCH/layout/t2.png" 1920`, xem ảnh. Nếu mặt bên khối chưa tách mặt trên, tăng `BlockSideShade` tới tối đa 0.45 và ghi lại giá trị.

- [ ] **Step 6: Dừng, báo user.**

---

### Task 3: HUD, tốc độ, Unlock, Retry trong Pause

**Files:**
- Modify:
  - `Assets/JamJam/Runtime/WoolScene.cs` (Speed, Unlock, field mới).
  - `Assets/JamJam/Editor/WoolUiBuilder.cs` (`StyleGame`).
  - `Assets/JamJam/Editor/WoolUiBuilder.ArtGame.cs` (`ArtGame`).
  - `Assets/JamJam/Editor/WoolUiArt.cs` (map `Warm` → `Button-Orange`).
  - `Assets/JamJam/Editor/WoolSceneBuilder.cs` (`BuildCanvas` tạo object mới).
- Test:
  - `WoolArtChecks.Ui.cs`, `WoolArtChecks.UiArtGame.cs`, `WoolArtChecks.PlayUi.cs`.
  - `WoolArtChecks.PlayLayout.cs` thêm `PlaySpeed`, `PlayUnlock`.

**Interfaces:**
- Consumes: `FitCamera`/`PlaceUnlockButtons` của Task 1.
- Produces:
  - `WoolScene.SpeedButton`, `SpeedLabel`, `UnlockTemplate` (Button).
  - `WoolScene.Speed`, `ToggleSpeed()`, `ResetSpeed()`, `UnlockButton(int slot, out Button button)`.
  - `WoolUiBuilder.HudPausePosition = (100, −100)`, `SpeedPosition = (−130, −100)`, `SpeedSize = (200, 96)`, `UnlockSize = (230, 80)`, `PauseCardSize = (820, 760)`.

- [ ] **Step 1: Check fail trước**

`UiGame` (trong `WoolArtChecks.Ui.cs`) và `UiArtGame`:
- `Brand`, `Goal`, `ProgressTrack`, `TrayCaption`, `Instructions`, `Help` có `activeSelf == false`, và `m_Text` của chúng giống trước (so chuỗi cứng hiện check đang có).
- `Retry` là con của `PausePanel/Card`.
- `Pause` neo trên-trái tại `HudPausePosition`; có ảnh thì icon là `Icon-Settings`.
- `Speed` tồn tại, `scene.SpeedButton`/`SpeedLabel` đã gán, nhãn "x1", size ≥ 96×96.
- `UnlockTemplate` inactive, đã gán, size `UnlockSize`, có `Label` "Unlock" và `Icon`/`Plus`.
- `ProgressIcon` tồn tại.
- Bỏ assert cũ về vị trí/size Retry ngoài Pause (`RetrySize` chỉ còn kiểm nếu vẫn dùng).

Recompile → `UiGame()` FAIL.

- [ ] **Step 2: Runtime**

- Field: `public Button SpeedButton; public Text SpeedLabel; public Button UnlockTemplate;` và `readonly List<Button> unlockButtons`.
- `Start`: `if (SpeedButton != null) SpeedButton.onClick.AddListener(ToggleSpeed);`, rồi cập nhật nhãn.
- `Speed`/`ToggleSpeed`/`ResetSpeed` theo bảng Quyết định.
- `Tick`:

```csharp
            var scaled = delta * Speed;
            var steps = Mathf.Max(1, Mathf.CeilToInt(scaled / 0.1f - 1e-4f));
            for (var s = 0; s < steps; s++) State.Step(scaled / steps);
            feel.Step(State, scaled);
            RenderFrame(scaled);
```

- `Unlock()`: `if (State == null || State.Paused) return; State.UnlockSlot();`.
- `LoadLevel`:
  - hủy nút cũ trong `unlockButtons` (`Destroy(button.gameObject)`), clear.
  - nếu `UnlockTemplate != null`, với mỗi spool `i`: `Instantiate(UnlockTemplate, UnlockTemplate.transform.parent)`, tên `"Unlock_" + i`, `onClick.AddListener(Unlock)`, add vào list.
  - Thứ tự: trước `FitCamera()`.
  - `SpeedLabel.text = "x" + Speed`.
- `PlaceUnlockButtons()`: thân theo Quyết định.
- `RenderFrame`: active/interactable/alpha như Quyết định. Chỉ gán khi khác giá trị cũ, để không dirty layout mỗi frame.
- `public void UnlockButton(int slot, out Button button)`: `button` = phần tử hoặc `null`.

- [ ] **Step 3: Builder**

- `StyleGame`/`ArtGame` theo bảng HUD/Ẩn/Retry. Object mới tạo bằng helper tạo-nếu-thiếu theo pattern hiện có trong `WoolUiBuilder`, và gán vào field `WoolScene`.
- `UnlockTemplate`:
  - nền `Button-Cream`, hoặc pill `Secondary` khi thiếu ảnh;
  - `Label` "Unlock" `Dark` cỡ 30 ở `(−20, 4)`;
  - `Plus` = `UiCircle` 44×44 màu `WoolUiTheme.Primary`, có Text "+" `Light` cỡ 34, ở `(80, 4)`;
  - lưu inactive.
- `WoolUiArt`: `Warm` → `Button-Orange`. Kiểm `Pause` vẫn dùng `Round` riêng, không qua map này.
- `WoolSceneBuilder.BuildCanvas` tạo `Speed`, `Speed/Label`, `UnlockTemplate`, `ProgressIcon`, rồi gọi `StyleGame` như hiện tại.

- [ ] **Step 4: Check Play Mode**

`PlaySpeed()`:
- `ResetSpeed()`, `LoadLevel(0)`.
- Ghi `Elapsed`, chạy khoảng 1 s thực, đo `dE1`. Toggle (nhãn "x2"), chạy khoảng 1 s, đo `dE2`. Kỳ vọng `dE2 / dE1 ∈ [1.8, 2.2]`.
- Pause: `Elapsed` đứng yên 0.5 s.
- `Resume`, `LoadLevel(1)`: `Speed == 2` và nhãn "x2".
- Gọi trực tiếp `Tick` với delta 0.1? `Tick` là private, nên dùng reflection như `PlayFeel` (nếu có) hoặc tạm đặt `lastTime` lùi 0.1 s qua reflection, rồi đợi 1 frame. Kỳ vọng `Elapsed` tăng ≥ 0.19.
- `ResetSpeed()` trong `finally`.
- Trả `"PLAY SPEED PASS"`.

`PlayUnlock()`:
- `LoadLevel(0)`, `OpenSlots = 4`.
- `UnlockButton(4)` active + interactable; `UnlockButton(5)` active, không interactable; `UnlockButton(0)` inactive.
- Vị trí `Unlock_4` trên màn hình nằm dưới tâm pad 4 chiếu ra màn hình (y nhỏ hơn) và cách ≤ 120 px theo x.
- `Pause()`, bấm `Unlock_4.onClick.Invoke()`: `OpenSlots` vẫn 4. `Resume()`, bấm lại: `OpenSlots == 5`, `Unlock_4` inactive, `Unlock_5` interactable.
- Trả `"PLAY UNLOCK PASS"`.

`PlayUi`: cập nhật nếu đang bấm Retry ngoài Pause (mở Pause trước).

- [ ] **Step 5: Chạy**

- `WoolUiBuilder.Rebuild()` hai lần. Sau lần 2, `git diff --stat Assets/Scenes/WoolJam.unity` không đổi so với sau lần 1 (so md5).
- Edit Mode PASS: `UiAssets`, `UiGame`, `UiArt`, `UiArtGame`, `UiLobby`, `UiArtLobby`.
- Play Mode PASS: `PlayUi`, `PlayArt`, `PlayArtResult(true)`, `PlayArtResult(false)`, `PlayResult*`, `PlaySpeed`, `PlayUnlock`, `PlayLayout`, `PlayThread`.
- `CAP "$SCRATCH/layout/t3.png" 1920`, xem ảnh.

- [ ] **Step 6: Dừng, báo user.**

---

### Task 4: Kiểm toàn bộ, ảnh, so sánh

**Files:**
- Create: `docs/layout-start.png`, `docs/layout-start-2400.png`, `docs/layout-run.png`, `docs/layout-run-2400.png`, `docs/layout-pause.png`, `docs/layout-pause-2400.png`, `docs/layout-compare.png`
- Modify: không sửa code, trừ khi check fail (sửa tối thiểu rồi ghi lại).

- [ ] **Step 1: Toàn bộ check**

- Harness.
- `ValidateAllLevels`.
- `WoolArtBuilder.Rebuild()` và `WoolUiBuilder.Rebuild()`, mỗi cái hai lần. md5 của `WoolJam.unity`, `Lobby.unity`, `Generated/**` giữ nguyên giữa lần 1 và lần 2.
- md5 `Art/UI/*.png` giữ nguyên so với đầu plan.
- `git diff --stat Assets/JamJam/Generated/Levels` rỗng.
- Mọi check Edit Mode và Play Mode ở Task 1–3, cùng `PlayLobby`, `PlayArtLobby`, `PlayFeel`, `PlayMotion`, `PlayBlocks`.

- [ ] **Step 2: Ảnh**

- Level 3 (8×6), ở 1080×1920 và 1080×2400:
  - đầu level → `layout-start`.
  - chọn 3 khối hợp lệ, step tới lúc 2–3 khối đang chạy và có ít nhất một sợi len → `layout-run`.
  - Pause → `layout-pause`.
- Ghép `layout-run.png` cạnh `C:\Users\Admin\Downloads\videoframe_11278.png` (cùng chiều cao 1280, nhãn "Hiện tại" / "Mẫu") → `docs/layout-compare.png`, bằng `"$PY" -I`.
- Xem từng ảnh. HUD, sân, khay, bảng không chồng nhau; nút Unlock không đè chữ số spool.

- [ ] **Step 3: Dừng, báo user** (dòng PASS, danh sách ảnh, các chỗ còn lệch so với ảnh mẫu).

## Tóm tắt

- `WoolStage` khớp camera theo chiều ngang với pitch 50°.
- Ô 0.9, bảng dời xuống, khay rộng hơn, khối dày 0.54.
- Nền cỏ/kem, kệ gỗ, màu ấm.
- Sợi len dày, mượt, liên tục.
- HUD góc (bánh răng, Level, %, x1/x2).
- Nút "Unlock +" bám ô khóa.
- Retry vào Pause.
- Luật chơi và level không đổi.
