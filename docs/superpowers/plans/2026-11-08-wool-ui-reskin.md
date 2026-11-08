# UI reskin Wool Jam

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** đổi giao diện `WoolJam.unity` (HUD, Pause, Result) và `Lobby.unity` (Home, Level Select, Settings) sang phong cách casual tươi sáng kiểu "Wool Crush" (chữ Lilita One trắng viền navy, nút bo tròn nổi khối, thẻ kem, nền navy) và thêm chuyển động UI (nút lún, panel bật lên, kết quả trễ, thẻ level hiện tại nhịp nhẹ) mà không đổi luồng màn hình, chuỗi chữ hay luật chơi.

**Kiến trúc:** token màu/hằng số nằm ở `WoolUiTheme` (Runtime, dùng chung cho Editor và runtime). Editor có `WoolUiStyle` (font, sprite procedural, các hàm áp style: `Light`/`Dark`/`Title`/`Logo`/`Raised`/`Button`/`Card`/`Overlay`/`Bar`/`Toggle`/`Badge`) và `WoolUiBuilder` (menu `JamJam/Rebuild UI` mở từng scene/prefab, tìm object theo tên/đường dẫn hiện có và áp style tại chỗ, idempotent); `WoolSceneBuilder`/`WoolLobbyBuilder` gọi đúng các hàm `StyleGame`/`StyleLobby`/`StyleCard` đó nên rebuild đầy đủ ra cùng giao diện (không chạy). Runtime có `WoolButtonJuice`, `WoolPopIn` (R3 `EveryUpdate`, chỉ subscribe khi đang animate), nhịp thẻ level trong `WoolLevelCard.Animate` (do clock R3 có sẵn của `WoolLobby` gọi) và `WoolScene` hoãn panel kết quả bằng R3 `Observable.Timer`.

**Tech Stack:** Unity 6000.3.19f1, URP 17.3, C# 9 (LangVersion 9.0), uGUI legacy `Text` + `Outline`/`Shadow`, Input System, R3 1.3.1 (`Observable.EveryUpdate(UnityFrameProvider.Update)`, `Observable.Timer(TimeSpan, UnityTimeProvider.Update)`), font Lilita One (SIL OFL 1.1), Python+PIL (ghép ảnh so sánh).

**Spec:** `docs/superpowers/specs/2026-11-08-wool-ui-reskin-design.md`

## Ràng buộc chung

- **Không âm thanh.** Không sửa `WoolGame.cs`, `WoolRouting.cs`, level asset (`Assets/JamJam/Generated/Levels/*.asset`), `WoolEditorChecks.cs`.
- Không đổi luồng màn hình, logic lobby/progress, wiring button (`onClick` gắn trong `Start` như cũ), chuỗi chữ do scene/prefab sở hữu (`m_Text`). Giữ `.meta`/GUID.
- Reskin bằng patch tại chỗ: menu `JamJam/Rebuild UI` = `JamJam.Editor.WoolUiBuilder.Rebuild()`. KHÔNG chạy `WoolSceneBuilder.Build`, `WoolLobbyBuilder.Build` hay `WoolEditorChecks.RunBatch`; hai builder chỉ được sửa để gọi cùng hàm style và phải compile.
- Timer/animation: R3, dispose khi disable/destroy. Không coroutine, không DOTween/UniTask, không `Update()` MonoBehaviour mới.
- Không comment trong code (`//`, `///`, `/* */`, XML doc). Method trả reference dùng interface/abstract; factory `void Create(..., out IThing x)` (helper Editor dùng `out` với kiểu component Unity như code hiện có).
- C# 9: không file-scoped namespace, không global using, không record struct. `GetComponent` trong Editor có thể trả "fake null": luôn so `== null`, không dùng `??` với `UnityEngine.Object`.
- Không `git add`/`git commit`; mỗi task kết thúc bằng bước kiểm tra rồi "Dừng, báo user".
- Số dòng trong header **Trước** là của file hiện tại (2026-11-08) trước khi bắt đầu plan; khi làm task sau, tìm theo nội dung Trước.
- Lệnh Unity: `unity recompile` (không thêm cờ `--caller`; nếu trả HTTP 400, xác nhận bằng eval `return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;` → `False False`); `unity command eval --caller plugin --skill unity-cli --no-banner '<C# statements; return value;>'`. Vào/thoát Play Mode, chụp Game view, đặt kích thước Game view, đọc lỗi console: dùng lệnh Unity CLI thật cho từng việc (tìm bằng `unity command --caller plugin --skill unity-cli --no-banner --no-pager`). Plan ghi chúng là **[PLAY START]**, **[PLAY STOP]**, **[CAPTURE W×H → path]**, **[CONSOLE ERRORS]**.
- Eval `WoolArtChecks.*` ném `InvalidOperationException("ART CHECK FAILED: …")` khi sai; kỳ vọng ghi dưới mỗi lệnh là chuỗi trả về khi đúng.
- Harness: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet`. Dòng PASS cuối (không đổi trong plan này): `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; 108 feel assertions`.
- Biến shell (Git Bash): `SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"`; `PY="$USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe"` (luôn chạy `"$PY" -I`).
- Mọi `Rebuild Art`/`Rebuild UI` yêu cầu thoát Play Mode và lưu mọi scene đang mở.

## Quyết định (spec để ngỏ hoặc phải lệch nhẹ)

| Chủ đề | Quyết định |
|---|---|
| Nút "nổi khối" | uGUI vẽ con sau cha nên không thể đặt con `Edge` *phía sau* thân. Đảo vai: Image của chính nút (raycast, `targetGraphic`) mang màu **cạnh** + sprite `UiPill`, con `Face` (sibling 0, stretch, `offsetMin.y = 10`) mang màu **thân**. Nhìn giống hệt spec (lip 10px ở dưới); nhãn dịch lên 5px. `Button.transition = None` (lún do `WoolButtonJuice`). |
| Thẻ panel | Cùng cơ chế: Image `Card` = `CardEdge`, con `Face` = `Card`, inset 6px ba cạnh, 18px cạnh dưới (6 viền + 12 độ dày). Sprite `UiCard` (bo 32px). |
| Tương phản | Cặp kiểm = màu chữ với màu chạm trực tiếp vào nét chữ: nếu Text có `Outline` thì so với màu Outline đầu tiên, không thì so với nền gần nhất (`Face` của cha, hoặc Image cha alpha > 0.5, hoặc navy camera). Ngưỡng 4.5. Trắng/`TextOutline` = 17.5, `TextDark`/`Secondary` = 12.6, `Primary`/`TextOutline` = 7.3, `Danger`/`TextOutline` = 5.4. Alpha chữ (0.85) bỏ qua. |
| Tiêu đề thắng/thua | Spec "xanh `Primary` viền trắng dày" không đạt 4.5 trên kem (2.3). Dùng hai `Outline`: navy 4px trong + trắng 8px ngoài + Shadow; thua dùng `Danger` cùng kiểu. Giữ viền trắng dày của spec, đạt 7.3/5.4. |
| Nền Lobby | Camera Lobby đổi từ kem sang navy `#14234A` (cùng màu nền game) để chữ trắng viền đọc được như spec. |
| Font | Lilita One chỉ có Regular: `fontStyle = Normal`. Fallback `LegacyRuntime.ttf` + `Bold` nếu không có file. Mọi `Text` bật best fit (`min = max(12, size/2)`, `max = size`) để không bị cắt ở 1080×2400 và với chuỗi runtime (`100%`, `PLAY  03`, tiêu đề level). |
| Sprite | `UiPill` (128, bo 60, border 62), `UiCard` (128, bo 32, border 40), `UiCircle`, `UiCheck`, `UiLock` (128, không border), thêm `UiBar` (128×32, vạch sáng trên `y∈(2,10)`, thân 0.85) cho fill tiến độ vì fill giữ `Image.Type.Filled` (không sửa logic `fillAmount`). PNG ở `Generated/Textures/UI/`, sinh bởi `WoolUiStyle.BuildSprites()` (gọi trong `Rebuild UI`). `RoundedPanel.png` cũ giữ nguyên (không còn dùng trong scene sau patch). |
| Kích thước | `Retry` 265×95 → 265×100 (chạm ≥ 96). `LevelBadge` 124×124 → 150×124 (pill). Track 790×28 → 790×36, fill 778×18 → 778×24. |
| Toggle Settings | Hàng `Secondary`, `Box` = pill `Off #5E6B8C` (xám navy), `Check` (graphic của Toggle) = pill `Primary`; không thêm núm (CrossFadeAlpha của Toggle chỉ tác động graphic, núm con sẽ không ẩn). |
| Thẻ level | `WoolLevelCard` có thêm `Face`/`Edge` (Image), `Bind` tô theo `WoolUiTheme.CardColors`: cleared `Primary`, current `Warm`, locked `#2A3A66`/`#172245`, còn lại `Secondary`. Badge `Check`/`Lock` = pill `Badge` (navy alpha 0.35) + icon `UiCheck`/`UiLock` + nhãn cũ; `Current` = pill `Primary` "PLAY". Chữ thẻ trắng viền. Alpha thẻ khóa 0.55 → 0.8 (thẻ navy trên nền navy). Null-safe khi prefab chưa patch. |
| Nhịp thẻ current | Trong `WoolLevelCard.Animate` (đã được `WoolLobby.Tick` gọi trên `Observable.Interval(…, UpdateIgnoreTimeScale)`, dispose ở `OnDisable`): `1 + 0.02·(1 − cos(2πt/1.2))`. Không tạo timer mới. |
| Haptic nút | `WoolHapticPattern.Get(WoolFeelKind.BlockLaunched, …)` = mẫu Light (12ms/90), qua `WoolHaptics.Create(WoolSettings.Vibration, out IWoolHaptics)`, tạo lại khi giá trị toggle đổi; không sửa file thuần `WoolHapticPattern`. |
| Trễ kết quả | `Observable.Timer(…, UnityTimeProvider.Update)` (thời gian có scale, cùng đồng hồ với `Tick` và VFX phần 2). Thắng 0.9s, thua 0.6s. `MarkCleared` + `Burst` vẫn chạy ngay. Hủy trong `LoadLevel` (Replay/Next), `Home`, `OnDisable`, `OnDestroy`. API cho check: `ResultPending`, `ResultQueuedAt`. |
| `WoolPopIn` | Gắn vào `Card` của `PausePanel`, `ResultPanel`, `SettingsPanel`, `ConfirmPanel`; `Fade` = `CanvasGroup` thêm vào overlay cha (overlay màu `#0A1230` alpha 0.72 nên alpha nhóm 0→1 cho 0→0.72). Thời gian bỏ qua timeScale. `OnDisable` trả scale 1/alpha 1. |
| Số ống chỉ | `WoolAssetsBuilder.Label` dùng font theme (ảnh hưởng cả dấu "+" của slot). Spool có con `Counter/Shadow` (TextMesh navy, lệch `(0.3,-0.45,0.4)` local, z dương = xa camera nên vẽ trước). `WoolScene` đồng bộ text bóng; `GetComponentInChildren<TextMesh>()` vẫn trả `Counter` (cha trước con). |
| `RebuildLobby` | Task 3 khai báo `static partial void RebuildLobby();` trong `WoolUiBuilder.cs` (không có thân thì C# bỏ lời gọi); Task 4 thêm thân trong `WoolUiBuilder.Lobby.cs`. Nhờ vậy không task nào sửa file mới của task trước. |
| Check | Partial `WoolArtChecks` (dùng lại `Expect`/`Near`/`LoadArt`): `UiAssets()` (T1), `PlayUi()`/`PlayResultArm(bool)`/`PlayResultEarly(bool)`/`PlayResultLate(bool)`/`PlayResultQuiet()` (T2), `UiGame()` (T3), `UiLobby()`/`PlayLobby()` (T4). `UiAssets` thử API style trên nội dung prefab `LevelCard` nạp bằng `LoadPrefabContents` rồi bỏ (không lưu). Chuỗi chữ kiểm bằng `grep m_Text` trước/sau. |
| Trễ trong check | `PlayResultArm` đặt `Time.timeScale = 0.25` để eval kế tiếp chắc chắn rơi trước mốc 0.9s; `PlayResultEarly` trả `timeScale = 1`; `PlayResultLate` tự báo "Wait longer" nếu chưa đủ `delay + 0.8s` (chạy lại eval). |
| Ảnh | `docs/ui-{game,pause,won,lost,home,levels,settings}.png` (1080×1920) và `docs/ui-…-2400.png` (1080×2400); so sánh `docs/ui-compare-{game,won,home}.png` với `shot4`/`shot5`/`shot8` của store. |

## Review Focus

- Người chơi bấm Retry/Home (hoặc Next) trong lúc chờ 0.6–0.9s sau khi thắng/thua → panel kết quả không bao giờ hiện ở màn mới, timer bị hủy. Test `PlayResultEarly(true)` + `PlayResultQuiet()` (Task 2); `Home` gọi `CancelResult` (Task 2 Bước 11).
- Chạy `Rebuild UI` nhiều lần (hoặc sau `Rebuild Art`) → không nhân đôi `Face`/`Icon`/`Outline`/`Shadow`, file scene/prefab giống hệt byte. Test áp style hai lần trong `UiAssets()` (Task 1) và md5 hai lần rebuild (Task 5).
- Nhấn nút rồi kéo ngón ra ngoài, hoặc `onClick` tắt chính panel chứa nút (Resume, Close) → nút về scale 1, không rò subscription. Test `OnPointerExit` và Resume trong `PlayUi()` (Task 2); `OnDisable` reset.
- Chuỗi runtime dài hơn chuỗi scene (`100%`, `PLAY  03`, "The Great Unravel") với font mới ở 1080×2400 → không bị cắt. Best fit kiểm trong `UiAssets()` (Task 1), ảnh hai tỉ lệ (Task 5).
- Tải font thất bại (mất mạng, GitHub đổi đường dẫn) → dùng `LegacyRuntime` + Bold, mọi check vẫn qua. Nhánh fallback trong `UiAssets()` (Task 1) và bước tải có điều kiện (Task 1 Bước 1).

## Bản đồ file

| File | Việc |
|---|---|
| `Assets/JamJam/Art/Fonts/LilitaOne-Regular.ttf`, `OFL.txt` | Tạo — tải bằng curl (T1) |
| `Assets/JamJam/Runtime/WoolUiTheme.cs` | Tạo — token, `CardColors`, `Contrast` (T1) |
| `Assets/JamJam/Editor/WoolUiStyle.cs` | Tạo — font, sprite, API style (T1) |
| `Assets/JamJam/Editor/WoolArtChecks.UiAssets.cs` | Tạo — `UiAssets()` (T1) |
| `Assets/JamJam/Generated/Textures/UI/*.png` | Sinh — 6 sprite (T1) |
| `Assets/JamJam/Runtime/WoolButtonJuice.cs`, `WoolPopIn.cs` | Tạo (T2) |
| `Assets/JamJam/Runtime/WoolLevelCard.cs` | Sửa — `Face`/`Edge`, màu theo trạng thái, nhịp (T2) |
| `Assets/JamJam/Runtime/WoolScene.cs` | Sửa — trễ kết quả, bóng số ống chỉ (T2) |
| `Assets/JamJam/Editor/WoolArtChecks.PlayUi.cs` | Tạo — check Play Mode (T2) |
| `Assets/JamJam/Editor/WoolUiBuilder.cs` | Tạo — menu `Rebuild UI`, `StyleGame` (T3) |
| `Assets/JamJam/Editor/WoolAssetsBuilder.cs` | Sửa — font theme cho TextMesh, bóng số ống chỉ (T3) |
| `Assets/JamJam/Editor/WoolArtChecks.Ui.cs` | Tạo — `UiGame()` + helper quét (T3) |
| `Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs` | Tạo — `RebuildLobby`, `StyleLobby`, `StyleCard` (T4) |
| `Assets/JamJam/Editor/WoolArtChecks.UiLobby.cs` | Tạo — `UiLobby()`, `PlayLobby()` (T4) |
| `Assets/JamJam/Editor/WoolSceneBuilder.cs`, `WoolLobbyBuilder.cs` | Sửa — dùng chung theme (T4) |
| Patch tại chỗ | `Assets/Scenes/WoolJam.unity` (T3), `Assets/Scenes/Lobby.unity`, `Generated/Prefabs/LevelCard.prefab` (T4), `Generated/Prefabs/Spool.prefab`, `SpoolSlot.prefab` qua `Rebuild Art` (T3) |
| Ảnh | `docs/ui-*.png`, `docs/ui-compare-*.png` (T5) |

Đã xác minh mọi file "Tạo" chưa tồn tại (2026-11-08). Toàn bộ code mới và mọi khối **Sau** đã được compile thử ngoài project cùng toàn bộ `Runtime/*.cs` + `Editor/*.cs` hiện tại, với `LangVersion 9.0`, `UNITY_EDITOR`, DLL của Unity 6000.3.19f1 (`Editor/Data/Managed/UnityEngine/*.dll`) và `Library/ScriptAssemblies` (UGUI, R3.Unity, URP, Input System): 0 lỗi, 0 cảnh báo.

---

### Task 1: Font + token + sprite UI + API style Editor

**Files:**
- Create: `Assets/JamJam/Art/Fonts/LilitaOne-Regular.ttf`, `Assets/JamJam/Art/Fonts/OFL.txt`
- Create: `Assets/JamJam/Runtime/WoolUiTheme.cs`, `Assets/JamJam/Editor/WoolUiStyle.cs`, `Assets/JamJam/Editor/WoolArtChecks.UiAssets.cs`
- Generate: `Assets/JamJam/Generated/Textures/UI/UiPill.png`, `UiCard.png`, `UiCircle.png`, `UiCheck.png`, `UiLock.png`, `UiBar.png`

**Interfaces — Produces:**
- `static class WoolUiTheme` (namespace `JamJam`): `Color TextLight, TextOutline, TextDark, Primary, PrimaryEdge, Secondary, SecondaryEdge, Warm, WarmEdge, Danger, DangerEdge, Card, CardEdge, Overlay (α 0.72), Track, Fill, Locked, LockedEdge, Off, Badge, Navy`; `Vector2 ShadowOffset (0,-4), LogoShadowOffset (0,-8)`; `const float SoftAlpha 0.85, ShadowAlpha 0.5, OutlineSize 3, LogoOutlineSize 6, TitleInnerOutline 4, TitleOuterOutline 8, EdgeDepth 10, CardBorder 6, CardDepth 12, MinTouch 96, MinContrast 4.5, LockedAlpha 0.8`; `void CardColors(bool unlocked, bool cleared, bool current, out Color face, out Color edge)`; `float Luminance(Color)`; `float Contrast(Color, Color)`.
- `enum WoolUiTone { Primary, Secondary, Warm, Danger }` và `static class WoolUiStyle` (namespace `JamJam.Editor`): `const string FontPath, SpriteFolder`; `string[] SpriteNames`; `void LoadFont(out Font, out FontStyle)`; `void BuildSprites()`; `void LoadSprite(string name, out Sprite)`; `void Light(Text, int size)`; `void Light(Text, int size, float alpha)`; `void Logo(Text, int)`; `void Title(Text, Color fill, int)`; `void Dark(Text, int)`; `void Raised(Image host, Sprite, Color face, Color edge, Vector4 inset(l,b,r,t), out Image face)`; `void Button(Button, WoolUiTone, Sprite, int labelSize)`; `void Card(Image, Sprite)`; `void Overlay(Image, out CanvasGroup)`; `void Bar(Image track, Image fill, Sprite pill, Sprite bar)`; `void Toggle(Toggle, Sprite pill, int labelSize)`; `void Badge(Image, Color, Sprite pill, Sprite icon or null, int labelSize)`; `void Tone(WoolUiTone, out Color face, out Color edge, out bool light)`.
- `WoolArtChecks.UiAssets()` → `"UI ASSETS PASS / <font name> / 6 sprites"`.

- [ ] **Bước 0: Baseline (trước mọi thay đổi)**

```bash
SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"
mkdir -p "$SCRATCH/ui-run"
cd "/d/Unity Project/Jam_Jam"
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor Assets/Scenes --include=*.meta | sort > "$SCRATCH/ui-run/guids.before.txt"
md5sum Assets/JamJam/Generated/Levels/*.asset > "$SCRATCH/ui-run/levels.md5"
md5sum Assets/JamJam/Runtime/WoolGame.cs Assets/JamJam/Runtime/WoolRouting.cs Assets/JamJam/Editor/WoolEditorChecks.cs > "$SCRATCH/ui-run/logic.md5"
md5sum Assets/Scenes/WoolJam.unity Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab > "$SCRATCH/ui-run/ui.before.md5"
for f in Assets/Scenes/WoolJam.unity Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab; do grep "m_Text:" "$f" | sort > "$SCRATCH/ui-run/strings.$(basename "$f").before.txt"; done
wc -l "$SCRATCH"/ui-run/strings.*.before.txt
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
unity command eval --caller plugin --skill unity-cli --no-banner 'return UnityEngine.PlayerPrefs.GetInt("JamJam.Cleared", -1);'
```
Kỳ vọng: `strings` có 21 dòng (WoolJam), 17 (Lobby), 5 (LevelCard); harness `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; 108 feel assertions`. Ghi giá trị `JamJam.Cleared` vào `$SCRATCH/ui-run/cleared.txt` (Task 2/4/5 khôi phục về giá trị này).

- [ ] **Bước 1: Tải Lilita One (SIL OFL) vào thư mục tạm riêng rồi mới chép vào project**

```bash
mkdir -p "$SCRATCH/ui-run/font-dl" && cd "$SCRATCH/ui-run/font-dl"
curl -fsSL -o LilitaOne-Regular.ttf https://github.com/google/fonts/raw/main/ofl/lilitaone/LilitaOne-Regular.ttf
curl -fsSL -o OFL.txt https://github.com/google/fonts/raw/main/ofl/lilitaone/OFL.txt
stat -c %s LilitaOne-Regular.ttf
head -c 4 LilitaOne-Regular.ttf | od -An -tx1
grep -c "SIL OPEN FONT LICENSE" OFL.txt
```
Kỳ vọng: kích thước > 10000 byte, magic ` 00 01 00 00`, `grep` ≥ 1. Khi cả ba đạt:
```bash
cd "/d/Unity Project/Jam_Jam"
mkdir -p Assets/JamJam/Art/Fonts
cp "$SCRATCH/ui-run/font-dl/LilitaOne-Regular.ttf" "$SCRATCH/ui-run/font-dl/OFL.txt" Assets/JamJam/Art/Fonts/
echo "font=LilitaOne" > "$SCRATCH/ui-run/font.txt"
```
Nếu `curl` lỗi hoặc một kiểm tra không đạt: KHÔNG chép gì vào `Assets`, ghi `echo "font=LegacyRuntime fallback: <lý do>" > "$SCRATCH/ui-run/font.txt"` và tiếp tục; `WoolUiStyle.LoadFont` tự dùng `LegacyRuntime.ttf` + `Bold`, mọi check có nhánh fallback. Báo user ở cuối task.

- [ ] **Bước 2: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.UiAssets.cs`**

```csharp
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string UiAssets()
        {
            WoolUiStyle.LoadFont(out var font, out var style);
            var lilita = File.Exists(WoolUiStyle.FontPath);
            if (lilita) Expect(AssetDatabase.GetAssetPath(font) == WoolUiStyle.FontPath && style == FontStyle.Normal, "Theme font is Lilita One Regular");
            else Expect(font != null && font.name == "LegacyRuntime" && style == FontStyle.Bold, "Fallback font is LegacyRuntime Bold");
            ExpectUiSprite("UiPill", WoolUiStyle.PillBorder, 64, 64, 0, 0);
            ExpectUiSprite("UiCard", WoolUiStyle.CardSpriteBorder, 64, 64, 0, 0);
            ExpectUiSprite("UiCircle", 0, 64, 64, 1, 1);
            ExpectUiSprite("UiCheck", 0, 52, 40, 64, 120);
            ExpectUiSprite("UiLock", 0, 64, 44, 64, 120);
            ExpectUiSprite("UiBar", 0, 64, 16, 0, 0);
            ExpectContrast(WoolUiTheme.TextLight, WoolUiTheme.TextOutline, "TextLight on TextOutline");
            ExpectContrast(WoolUiTheme.TextDark, WoolUiTheme.Secondary, "TextDark on Secondary");
            ExpectContrast(WoolUiTheme.TextDark, WoolUiTheme.Card, "TextDark on Card");
            ExpectContrast(WoolUiTheme.Primary, WoolUiTheme.TextOutline, "Primary title on TextOutline");
            ExpectContrast(WoolUiTheme.Danger, WoolUiTheme.TextOutline, "Danger title on TextOutline");
            Expect(Mathf.Abs(WoolUiTheme.Contrast(Color.white, Color.black) - 21) < 0.01f, "Contrast formula gives 21 for white on black");
            var root = PrefabUtility.LoadPrefabContents(WoolAssetsBuilder.Root + "/Prefabs/LevelCard.prefab");
            try
            {
                WoolUiStyle.LoadSprite("UiPill", out var pill);
                WoolUiStyle.LoadSprite("UiCard", out var cardSprite);
                var view = root.GetComponent<WoolLevelCard>();
                var host = view.Button.GetComponent<Image>();
                WoolUiStyle.Button(view.Button, WoolUiTone.Primary, pill, 30);
                WoolUiStyle.Button(view.Button, WoolUiTone.Primary, pill, 30);
                var face = host.transform.Find("Face");
                Expect(face != null && face.GetSiblingIndex() == 0 && host.transform.Find("Face") == face, "Raised button has one Face child drawn first");
                var faceImage = face.GetComponent<Image>();
                Expect(Near(faceImage.color, WoolUiTheme.Primary) && Near(host.color, WoolUiTheme.PrimaryEdge) && host.sprite == pill && host.type == Image.Type.Sliced, "Raised button uses Primary face and PrimaryEdge lip");
                var faceRect = (RectTransform)face;
                Expect(faceRect.offsetMin == new Vector2(0, WoolUiTheme.EdgeDepth) && faceRect.offsetMax == Vector2.zero && !faceImage.raycastTarget, "Face sits 10 px above the lip");
                Expect(view.Button.transition == Selectable.Transition.None && view.Button.targetGraphic == host && host.raycastTarget, "Button keeps its raycast host without tint");
                WoolUiStyle.Light(view.Number, 64);
                WoolUiStyle.Light(view.Number, 64);
                var effects = view.Number.GetComponents<Shadow>();
                Expect(view.Number.font == font && view.Number.fontStyle == style && view.Number.fontSize == 64 && view.Number.resizeTextForBestFit, "Light text uses the theme font with best fit");
                Expect(effects.Length == 2 && effects[0] is Outline && !(effects[1] is Outline), "Light text has one Outline then one Shadow after two passes");
                Expect(Near(effects[0].effectColor, WoolUiTheme.TextOutline) && effects[0].effectDistance == new Vector2(3, -3), "Outline is TextOutline 3 px");
                Expect(Mathf.Abs(effects[1].effectColor.a - 0.5f) < 0.01f && effects[1].effectDistance == new Vector2(0, -4), "Shadow is (0,-4) at alpha 0.5");
                WoolUiStyle.Title(view.Number, WoolUiTheme.Primary, 80);
                effects = view.Number.GetComponents<Shadow>();
                Expect(effects.Length == 3 && effects[0] is Outline && effects[1] is Outline && Near(effects[1].effectColor, Color.white) && effects[1].effectDistance == new Vector2(8, -8), "Title has a navy then a thick white outline");
                WoolUiStyle.Dark(view.Number, 40);
                Expect(view.Number.GetComponents<Shadow>().Length == 0 && Near(view.Number.color, WoolUiTheme.TextDark), "Dark text drops outline and shadow");
                WoolUiStyle.Card(host, cardSprite);
                Expect(Near(host.color, WoolUiTheme.CardEdge) && Near(face.GetComponent<Image>().color, WoolUiTheme.Card) && faceRect.offsetMin == new Vector2(6, 18) && faceRect.offsetMax == new Vector2(-6, -6), "Card has a 6 px CardEdge border");
                var badge = view.Check.GetComponent<Image>();
                WoolUiStyle.LoadSprite("UiCheck", out var check);
                WoolUiStyle.Badge(badge, WoolUiTheme.Badge, pill, check, 26);
                WoolUiStyle.Badge(badge, WoolUiTheme.Badge, pill, check, 26);
                Expect(badge.transform.Find("Icon") != null && badge.transform.Find("Icon").GetComponent<Image>().sprite == check, "Badge gets one check icon");
                WoolUiStyle.Overlay(badge, out var group);
                Expect(group != null && badge.sprite == null && Mathf.Abs(badge.color.a - 0.72f) < 0.001f, "Overlay is #0A1230 at alpha 0.72 with a CanvasGroup");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return "UI ASSETS PASS / " + font.name + " / " + WoolUiStyle.SpriteNames.Length + " sprites";
        }

        static void ExpectUiSprite(string name, float border, int solidX, int solidY, int clearX, int clearY)
        {
            var path = WoolUiStyle.SpriteFolder + "/" + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Expect(sprite != null, name + " sprite exists");
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Expect(importer.textureType == TextureImporterType.Sprite && !importer.mipmapEnabled && importer.alphaIsTransparency, name + " imports as a sprite");
            Expect(Mathf.Abs(sprite.border.x - border) < 0.01f && Mathf.Abs(sprite.border.w - border) < 0.01f, name + " border " + border + " but was " + sprite.border);
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(path));
            var solid = image.GetPixel(solidX, solidY).a;
            var clear = image.GetPixel(clearX, clearY).a;
            Object.DestroyImmediate(image);
            Expect(solid > 0.99f, name + " is solid at " + solidX + "," + solidY + " but alpha was " + solid);
            Expect(clear < 0.01f, name + " is clear at " + clearX + "," + clearY + " but alpha was " + clear);
        }

        static void ExpectContrast(Color text, Color back, string name)
        {
            var ratio = WoolUiTheme.Contrast(text, back);
            Expect(ratio >= WoolUiTheme.MinContrast, name + " contrast " + ratio.ToString("F2") + " is at least 4.5");
        }
    }
}
```

- [ ] **Bước 3: Chạy RED** — `unity recompile`. Kỳ vọng lỗi `CS0103: The name 'WoolUiStyle' does not exist in the current context` và `'WoolUiTheme'` trong `WoolArtChecks.UiAssets.cs`.

- [ ] **Bước 4: Tạo `Assets/JamJam/Runtime/WoolUiTheme.cs`**

```csharp
using System;
using UnityEngine;

namespace JamJam
{
    public static class WoolUiTheme
    {
        public static readonly Color TextLight = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color TextOutline = new Color32(0x0E, 0x18, 0x36, 0xFF);
        public static readonly Color TextDark = new Color32(0x1E, 0x2B, 0x4F, 0xFF);
        public static readonly Color Primary = new Color32(0x3C, 0xC0, 0x3A, 0xFF);
        public static readonly Color PrimaryEdge = new Color32(0x23, 0x86, 0x2A, 0xFF);
        public static readonly Color Secondary = new Color32(0xFF, 0xF3, 0xD6, 0xFF);
        public static readonly Color SecondaryEdge = new Color32(0xD9, 0xB7, 0x7A, 0xFF);
        public static readonly Color Warm = new Color32(0xFF, 0xB2, 0x1F, 0xFF);
        public static readonly Color WarmEdge = new Color32(0xC7, 0x7A, 0x0A, 0xFF);
        public static readonly Color Danger = new Color32(0xF0, 0x60, 0x3A, 0xFF);
        public static readonly Color DangerEdge = new Color32(0xB2, 0x3E, 0x1E, 0xFF);
        public static readonly Color Card = new Color32(0xFF, 0xF8, 0xEC, 0xFF);
        public static readonly Color CardEdge = new Color32(0x1E, 0x2B, 0x4F, 0xFF);
        public static readonly Color Overlay = new Color(0x0A / 255f, 0x12 / 255f, 0x30 / 255f, 0.72f);
        public static readonly Color Track = new Color32(0x0E, 0x18, 0x36, 0xFF);
        public static readonly Color Fill = new Color32(0x3C, 0xC0, 0x3A, 0xFF);
        public static readonly Color Locked = new Color32(0x2A, 0x3A, 0x66, 0xFF);
        public static readonly Color LockedEdge = new Color32(0x17, 0x22, 0x45, 0xFF);
        public static readonly Color Off = new Color32(0x5E, 0x6B, 0x8C, 0xFF);
        public static readonly Color Badge = new Color(0x0E / 255f, 0x18 / 255f, 0x36 / 255f, 0.35f);
        public static readonly Color Navy = new Color32(0x14, 0x23, 0x4A, 0xFF);
        public static readonly Vector2 ShadowOffset = new Vector2(0, -4);
        public static readonly Vector2 LogoShadowOffset = new Vector2(0, -8);
        public const float SoftAlpha = 0.85f;
        public const float ShadowAlpha = 0.5f;
        public const float OutlineSize = 3f;
        public const float LogoOutlineSize = 6f;
        public const float TitleInnerOutline = 4f;
        public const float TitleOuterOutline = 8f;
        public const float EdgeDepth = 10f;
        public const float CardBorder = 6f;
        public const float CardDepth = 12f;
        public const float MinTouch = 96f;
        public const float MinContrast = 4.5f;
        public const float LockedAlpha = 0.8f;

        public static void CardColors(bool unlocked, bool cleared, bool current, out Color face, out Color edge)
        {
            if (cleared) { face = Primary; edge = PrimaryEdge; return; }
            if (current) { face = Warm; edge = WarmEdge; return; }
            if (unlocked) { face = Secondary; edge = SecondaryEdge; return; }
            face = Locked;
            edge = LockedEdge;
        }

        public static float Luminance(Color color)
        {
            return 0.2126f * Channel(color.r) + 0.7152f * Channel(color.g) + 0.0722f * Channel(color.b);
        }

        public static float Contrast(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05f) / (Math.Min(la, lb) + 0.05f);
        }

        static float Channel(float c)
        {
            return c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
        }
    }
}
```

- [ ] **Bước 5: Tạo `Assets/JamJam/Editor/WoolUiStyle.cs`**

```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public enum WoolUiTone { Primary, Secondary, Warm, Danger }

    public static class WoolUiStyle
    {
        public const string FontPath = "Assets/JamJam/Art/Fonts/LilitaOne-Regular.ttf";
        public const string SpriteFolder = WoolAssetsBuilder.Root + "/Textures/UI";
        public const int SpriteSize = 128;
        public const float PillRadius = 60f;
        public const float PillBorder = 62f;
        public const float CardRadius = 32f;
        public const float CardSpriteBorder = 40f;
        public const int BarWidth = 128;
        public const int BarHeight = 32;
        public const float BarShade = 0.85f;
        public static readonly string[] SpriteNames = { "UiPill", "UiCard", "UiCircle", "UiCheck", "UiLock", "UiBar" };

        public static void LoadFont(out Font font, out FontStyle style)
        {
            font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            style = FontStyle.Normal;
            if (font != null) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            style = FontStyle.Bold;
        }

        public static void BuildSprites()
        {
            if (!AssetDatabase.IsValidFolder(SpriteFolder)) AssetDatabase.CreateFolder(WoolAssetsBuilder.Root + "/Textures", "UI");
            WriteSprite("UiPill", SpriteSize, SpriteSize, PillBorder, (x, y) => Solid(RoundedBox(x, y, SpriteSize * 0.5f, SpriteSize * 0.5f, PillRadius)));
            WriteSprite("UiCard", SpriteSize, SpriteSize, CardSpriteBorder, (x, y) => Solid(RoundedBox(x, y, SpriteSize * 0.5f, SpriteSize * 0.5f, CardRadius)));
            WriteSprite("UiCircle", SpriteSize, SpriteSize, 0, (x, y) => Solid(new Vector2(x, y).magnitude - (SpriteSize * 0.5f - 1)));
            WriteSprite("UiCheck", SpriteSize, SpriteSize, 0, (x, y) => Solid(Mathf.Min(Segment(x, y, -36, 2, -12, -24), Segment(x, y, -12, -24, 38, 30)) - 12));
            WriteSprite("UiLock", SpriteSize, SpriteSize, 0, (x, y) => Solid(Lock(x, y)));
            WriteSprite("UiBar", BarWidth, BarHeight, 0, Bar);
        }

        public static void LoadSprite(string name, out Sprite sprite)
        {
            var path = SpriteFolder + "/" + name + ".png";
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException(path + " is missing; run JamJam/Rebuild UI");
        }

        public static void Light(Text text, int size) { Light(text, size, 1); }

        public static void Light(Text text, int size, float alpha)
        {
            Font(text, size);
            text.color = new Color(1, 1, 1, alpha);
            Effects(text, new[] { WoolUiTheme.TextOutline }, new[] { WoolUiTheme.OutlineSize }, WoolUiTheme.ShadowOffset);
        }

        public static void Logo(Text text, int size)
        {
            Font(text, size);
            text.color = WoolUiTheme.TextLight;
            Effects(text, new[] { WoolUiTheme.TextOutline }, new[] { WoolUiTheme.LogoOutlineSize }, WoolUiTheme.LogoShadowOffset);
        }

        public static void Title(Text text, Color fill, int size)
        {
            Font(text, size);
            text.color = fill;
            Effects(text, new[] { WoolUiTheme.TextOutline, WoolUiTheme.TextLight }, new[] { WoolUiTheme.TitleInnerOutline, WoolUiTheme.TitleOuterOutline }, WoolUiTheme.ShadowOffset);
        }

        public static void Dark(Text text, int size)
        {
            Font(text, size);
            text.color = WoolUiTheme.TextDark;
            Effects(text, Array.Empty<Color>(), Array.Empty<float>(), Vector2.zero);
        }

        public static void Raised(Image host, Sprite sprite, Color face, Color edge, Vector4 inset, out Image faceImage)
        {
            host.sprite = sprite;
            host.type = Image.Type.Sliced;
            host.color = edge;
            var node = host.transform.Find("Face");
            RectTransform rect;
            if (node == null)
            {
                rect = new GameObject("Face", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                rect.SetParent(host.transform, false);
            }
            else rect = (RectTransform)node;
            rect.SetSiblingIndex(0);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset.x, inset.y);
            rect.offsetMax = new Vector2(-inset.z, -inset.w);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            faceImage = rect.GetComponent<Image>();
            faceImage.sprite = sprite;
            faceImage.type = Image.Type.Sliced;
            faceImage.color = face;
            faceImage.raycastTarget = false;
        }

        public static void Button(Button button, WoolUiTone tone, Sprite sprite, int labelSize)
        {
            Tone(tone, out var face, out var edge, out var light);
            var host = button.GetComponent<Image>();
            Raised(host, sprite, face, edge, new Vector4(0, WoolUiTheme.EdgeDepth, 0, 0), out _);
            host.raycastTarget = true;
            button.targetGraphic = host;
            button.transition = Selectable.Transition.None;
            var node = button.transform.Find("Label");
            if (node == null) return;
            var label = node.GetComponent<Text>();
            label.rectTransform.anchoredPosition = new Vector2(0, WoolUiTheme.EdgeDepth * 0.5f);
            if (light) Light(label, labelSize); else Dark(label, labelSize);
        }

        public static void Card(Image card, Sprite sprite)
        {
            var border = WoolUiTheme.CardBorder;
            Raised(card, sprite, WoolUiTheme.Card, WoolUiTheme.CardEdge, new Vector4(border, border + WoolUiTheme.CardDepth, border, border), out _);
        }

        public static void Overlay(Image overlay, out CanvasGroup group)
        {
            overlay.sprite = null;
            overlay.type = Image.Type.Simple;
            overlay.color = WoolUiTheme.Overlay;
            overlay.raycastTarget = true;
            group = overlay.GetComponent<CanvasGroup>();
            if (group == null) group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        public static void Bar(Image track, Image fill, Sprite pill, Sprite bar)
        {
            track.sprite = pill;
            track.type = Image.Type.Sliced;
            track.color = WoolUiTheme.Track;
            fill.sprite = bar;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.preserveAspect = false;
            fill.color = WoolUiTheme.Fill;
        }

        public static void Toggle(Toggle toggle, Sprite pill, int labelSize)
        {
            var row = toggle.GetComponent<Image>();
            row.sprite = pill;
            row.type = Image.Type.Sliced;
            row.color = WoolUiTheme.Secondary;
            toggle.transition = Selectable.Transition.None;
            var box = (Image)toggle.targetGraphic;
            box.sprite = pill;
            box.type = Image.Type.Sliced;
            box.color = WoolUiTheme.Off;
            var check = (Image)toggle.graphic;
            check.sprite = pill;
            check.type = Image.Type.Sliced;
            check.color = WoolUiTheme.Primary;
            var label = toggle.transform.Find("Label");
            if (label != null) Dark(label.GetComponent<Text>(), labelSize);
        }

        public static void Badge(Image badge, Color color, Sprite pill, Sprite icon, int labelSize)
        {
            badge.sprite = pill;
            badge.type = Image.Type.Sliced;
            badge.color = color;
            var label = badge.transform.Find("Label").GetComponent<Text>();
            var node = badge.transform.Find("Icon");
            if (icon == null)
            {
                if (node != null) UnityEngine.Object.DestroyImmediate(node.gameObject);
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.sizeDelta = new Vector2(170, 60);
            }
            else
            {
                RectTransform rect;
                if (node == null)
                {
                    rect = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                    rect.SetParent(badge.transform, false);
                }
                else rect = (RectTransform)node;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-58, 0);
                rect.sizeDelta = new Vector2(44, 44);
                var image = rect.GetComponent<Image>();
                image.sprite = icon;
                image.type = Image.Type.Simple;
                image.color = WoolUiTheme.TextLight;
                image.raycastTarget = false;
                label.rectTransform.anchoredPosition = new Vector2(22, 0);
                label.rectTransform.sizeDelta = new Vector2(118, 60);
            }
            Light(label, labelSize);
        }

        public static void Tone(WoolUiTone tone, out Color face, out Color edge, out bool light)
        {
            switch (tone)
            {
                case WoolUiTone.Primary: face = WoolUiTheme.Primary; edge = WoolUiTheme.PrimaryEdge; light = true; return;
                case WoolUiTone.Warm: face = WoolUiTheme.Warm; edge = WoolUiTheme.WarmEdge; light = true; return;
                case WoolUiTone.Danger: face = WoolUiTheme.Danger; edge = WoolUiTheme.DangerEdge; light = true; return;
                default: face = WoolUiTheme.Secondary; edge = WoolUiTheme.SecondaryEdge; light = false; return;
            }
        }

        static void Font(Text text, int size)
        {
            LoadFont(out var font, out var style);
            text.font = font;
            text.fontStyle = style;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, size / 2);
            text.resizeTextMaxSize = size;
        }

        static void Effects(Text text, Color[] outlines, float[] sizes, Vector2 shadow)
        {
            var wanted = outlines.Length + (shadow != Vector2.zero ? 1 : 0);
            var existing = text.GetComponents<Shadow>();
            var match = existing.Length == wanted;
            for (var i = 0; match && i < existing.Length; i++) match = (i < outlines.Length) == (existing[i] is Outline);
            if (!match)
            {
                foreach (var effect in existing) UnityEngine.Object.DestroyImmediate(effect);
                for (var i = 0; i < outlines.Length; i++) text.gameObject.AddComponent<Outline>();
                if (shadow != Vector2.zero) text.gameObject.AddComponent<Shadow>();
                existing = text.GetComponents<Shadow>();
            }
            for (var i = 0; i < outlines.Length; i++)
            {
                existing[i].effectColor = outlines[i];
                existing[i].effectDistance = new Vector2(sizes[i], -sizes[i]);
                existing[i].useGraphicAlpha = true;
            }
            if (shadow == Vector2.zero) return;
            var last = existing[outlines.Length];
            var tint = WoolUiTheme.TextOutline;
            last.effectColor = new Color(tint.r, tint.g, tint.b, WoolUiTheme.ShadowAlpha);
            last.effectDistance = shadow;
            last.useGraphicAlpha = true;
        }

        static void WriteSprite(string name, int width, int height, float border, Func<float, float, Color> pixel)
        {
            var path = SpriteFolder + "/" + name + ".png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    pixels[y * width + x] = pixel(x + 0.5f - width * 0.5f, y + 0.5f - height * 0.5f);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        static Color Solid(float distance)
        {
            return new Color(1, 1, 1, Mathf.Clamp01(0.5f - distance));
        }

        static float RoundedBox(float x, float y, float halfWidth, float halfHeight, float radius)
        {
            var qx = Mathf.Abs(x) - (halfWidth - radius);
            var qy = Mathf.Abs(y) - (halfHeight - radius);
            return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
        }

        static float Segment(float x, float y, float ax, float ay, float bx, float by)
        {
            var p = new Vector2(x - ax, y - ay);
            var ab = new Vector2(bx - ax, by - ay);
            return (p - ab * Mathf.Clamp01(Vector2.Dot(p, ab) / ab.sqrMagnitude)).magnitude;
        }

        static float Lock(float x, float y)
        {
            var body = RoundedBox(x, y + 20, 40, 32, 10);
            var arc = y > 14 ? Mathf.Abs(new Vector2(x, y - 14).magnitude - 26) : Mathf.Min(Segment(x, y, -26, 14, -26, -4), Segment(x, y, 26, 14, 26, -4));
            return Mathf.Min(body, arc - 8);
        }

        static Color Bar(float x, float y)
        {
            var alpha = Mathf.Clamp01(0.5f - RoundedBox(x, y, BarWidth * 0.5f, BarHeight * 0.5f, BarHeight * 0.5f));
            var shade = y > 2 && y < 10 ? 1f : BarShade;
            return new Color(shade, shade, shade, alpha);
        }
    }
}
```

- [ ] **Bước 6: Compile + RED lần hai (chưa có sprite)** — `unity recompile` (0 lỗi; Unity import font, sinh `.meta`). Rồi:
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.UiAssets();'
```
Kỳ vọng: `ART CHECK FAILED: UiPill sprite exists`.

- [ ] **Bước 7: Sinh sprite + GREEN** (scene đang mở đã lưu, không ở Play Mode)
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolUiStyle.BuildSprites(); return "sprites";'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.UiAssets();'
unity command eval --caller plugin --skill unity-cli --no-banner 'var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene(); return s.path + " dirty=" + s.isDirty;'
```
Kỳ vọng: `UI ASSETS PASS / LilitaOne-Regular / 6 sprites` (fallback: `UI ASSETS PASS / LegacyRuntime / 6 sprites`); scene đang mở `dirty=False`.

- [ ] **Bước 8: Kiểm tra** — `md5sum -c "$SCRATCH/ui-run/ui.before.md5"` → ba dòng `OK` (Task 1 không chạm scene/prefab); `ls Assets/JamJam/Generated/Textures/UI/*.png | wc -l` → `6`; `grep -rn "//\|/\*" Assets/JamJam/Runtime/WoolUiTheme.cs Assets/JamJam/Editor/WoolUiStyle.cs Assets/JamJam/Editor/WoolArtChecks.UiAssets.cs` → rỗng. Đọc `UiPill.png`, `UiCheck.png`, `UiLock.png` bằng Read: pill bo tròn trắng, dấu ✓, ổ khóa có quai.

- [ ] **Bước 9: Dừng, báo user** (kèm nội dung `font.txt`).

---

### Task 2: Runtime `WoolButtonJuice`, `WoolPopIn`, nhịp thẻ level, trễ panel kết quả

**Files:**
- Create: `Assets/JamJam/Runtime/WoolButtonJuice.cs`, `Assets/JamJam/Runtime/WoolPopIn.cs`, `Assets/JamJam/Editor/WoolArtChecks.PlayUi.cs`
- Modify: `Assets/JamJam/Runtime/WoolLevelCard.cs:1-34`; `Assets/JamJam/Runtime/WoolScene.cs:29-30, 35, 47-48, 84-87, 93-98, 146, 196, 209-220, 310`

**Interfaces — Consumes:** `WoolUiTheme.CardColors`, `WoolUiTheme.LockedAlpha` (Task 1); `WoolEase.Clamp/OutBack`, `WoolHaptics.Create`, `WoolHapticPattern.Get`, `WoolSettings`, `WoolPlayerPrefs` (có sẵn).

**Interfaces — Produces:**
- `sealed class WoolButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler` — `const float PressedScale 0.92, PressTime 0.06, ReleaseTime 0.18`; `bool Animating`; `bool Pressed`; `void Step(float delta)` (public để check lái tất định; EveryUpdate gọi với `Time.unscaledDeltaTime`).
- `sealed class WoolPopIn : MonoBehaviour` — `const float DefaultFromScale 0.8, DefaultScaleTime 0.3, DefaultFadeTime 0.2`; field `CanvasGroup Fade; float FromScale, ScaleTime, FadeTime`; `bool Animating`; `void Step(float delta)`.
- `WoolLevelCard`: field `Image Face, Edge`; `const float PulseScale 0.04, PulsePeriod 1.2`; `bool Pulsing`; `static float PulseAt(float time)`.
- `WoolScene`: `const float WinResultDelay 0.9, LoseResultDelay 0.6`; `bool ResultPending`; `double ResultQueuedAt`.
- `WoolArtChecks.PlayUi()`, `PlayResultArm(bool win)`, `PlayResultEarly(bool replay)`, `PlayResultLate(bool win)`, `PlayResultQuiet()`.

- [ ] **Bước 1: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.PlayUi.cs`**

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string PlayUi()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null && scene.State.Status == RoundStatus.Playing && !scene.State.Paused, "WoolScene is playing");
            var juice = scene.RetryButton.GetComponent<WoolButtonJuice>();
            if (juice == null) juice = scene.RetryButton.gameObject.AddComponent<WoolButtonJuice>();
            var target = juice.transform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            juice.OnPointerDown(pointer);
            Expect(juice.Animating && juice.Pressed, "Press starts the squash");
            juice.Step(0.03f);
            Expect(target.localScale.x < 1 && target.localScale.x > WoolButtonJuice.PressedScale, "Squash is half way after 0.03 s but scale was " + target.localScale.x);
            juice.Step(0.05f);
            Expect(Mathf.Abs(target.localScale.x - WoolButtonJuice.PressedScale) < 1e-5f && !juice.Animating, "Squash settles at 0.92 and unsubscribes");
            juice.OnPointerUp(pointer);
            juice.Step(0.09f);
            Expect(target.localScale.x > 1, "Release overshoots past 1 but scale was " + target.localScale.x);
            juice.Step(0.2f);
            Expect(target.localScale.x == 1 && !juice.Animating && !juice.Pressed, "Release returns exactly to 1");
            juice.OnPointerUp(pointer);
            Expect(!juice.Animating, "Release without a press does nothing");
            juice.OnPointerDown(pointer);
            juice.OnPointerExit(pointer);
            juice.Step(1);
            Expect(target.localScale.x == 1 && !juice.Pressed, "Pointer exit releases the button");
            var card = scene.PausePanel.transform.Find("Card");
            var group = scene.PausePanel.GetComponent<CanvasGroup>();
            if (group == null) group = scene.PausePanel.AddComponent<CanvasGroup>();
            var pop = card.GetComponent<WoolPopIn>();
            if (pop == null) { pop = card.gameObject.AddComponent<WoolPopIn>(); pop.Fade = group; }
            scene.Pause();
            Expect(scene.PausePanel.activeSelf && pop.Animating && Mathf.Abs(card.localScale.x - WoolPopIn.DefaultFromScale) < 1e-4f && group.alpha == 0, "Pause card starts at 0.8 and transparent");
            pop.Step(0.1f);
            Expect(Mathf.Abs(group.alpha - 0.5f) < 0.01f && card.localScale.x > WoolPopIn.DefaultFromScale && card.localScale.x < 1.1f, "Overlay is half faded after 0.1 s");
            pop.Step(0.25f);
            Expect(Mathf.Abs(card.localScale.x - 1) < 0.01f && group.alpha == 1 && !pop.Animating, "Pause card settles at scale 1 and alpha 1");
            scene.Resume();
            Expect(!scene.PausePanel.activeSelf && card.localScale.x == 1 && !scene.State.Paused, "Resume hides the card at rest");
            Expect(Mathf.Abs(WoolLevelCard.PulseAt(0) - 1) < 1e-5f && Mathf.Abs(WoolLevelCard.PulseAt(0.6f) - 1.04f) < 1e-4f && Mathf.Abs(WoolLevelCard.PulseAt(1.2f) - 1) < 1e-4f, "Current card pulse runs 1 to 1.04 over 1.2 s");
            return "PLAY UI PASS / juice / pop-in / pulse";
        }

        public static string PlayResultArm(bool win)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            scene.Replay();
            var game = scene.State;
            for (var frame = 0; frame < 30000 && game.Status == RoundStatus.Playing; frame++)
            {
                if (win)
                    for (var b = 0; b < game.Blocks.Count; b++)
                        if (game.CanSelect(b) && game.HasVisibleColor(game.Blocks[b].Definition.Color)) { game.Select(b); break; }
                game.Step(0.02f);
            }
            Expect(game.Status == (win ? RoundStatus.Won : RoundStatus.Lost), "Round ends " + (win ? "Won" : "Lost") + " but was " + game.Status);
            Expect(!scene.ResultPending && !scene.ResultPanel.activeSelf, "Nothing is shown before the next Tick");
            Time.timeScale = 0.25f;
            return "ARMED / " + game.Status;
        }

        public static string PlayResultEarly(bool replay)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            var delay = scene.State.Status == RoundStatus.Won ? WoolScene.WinResultDelay : WoolScene.LoseResultDelay;
            var elapsed = Time.timeAsDouble - scene.ResultQueuedAt;
            Expect(scene.ResultPending, "Result is queued after the round ends");
            Expect(!scene.ResultPanel.activeSelf && elapsed < delay, "ResultPanel is still hidden " + elapsed.ToString("F2") + " s after the end");
            if (replay)
            {
                scene.Replay();
                Expect(!scene.ResultPending && !scene.ResultPanel.activeSelf && scene.State.Status == RoundStatus.Playing, "Replay while waiting cancels the result");
            }
            Time.timeScale = 1;
            return "EARLY PASS / " + elapsed.ToString("F2") + " s" + (replay ? " / replayed" : "");
        }

        public static string PlayResultLate(bool win)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            var delay = win ? WoolScene.WinResultDelay : WoolScene.LoseResultDelay;
            var elapsed = Time.timeAsDouble - scene.ResultQueuedAt;
            Expect(elapsed >= delay + 0.8f, "Wait longer: only " + elapsed.ToString("F2") + " s since the round ended");
            Expect(scene.ResultPanel.activeSelf && !scene.ResultPending, "ResultPanel is shown after the delay");
            Expect(scene.WonContent.activeSelf == win && scene.LostContent.activeSelf == !win && scene.NextButton.gameObject.activeSelf == win, "Result content matches " + (win ? "Won" : "Lost"));
            var card = scene.ResultPanel.transform.Find("Card");
            Expect(Mathf.Abs(card.localScale.x - 1) < 0.01f, "Result card settles at scale 1");
            return "PLAY RESULT PASS / " + (win ? "Won" : "Lost") + " / " + elapsed.ToString("F2") + " s";
        }

        public static string PlayResultQuiet()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(!scene.ResultPanel.activeSelf && !scene.ResultPending && scene.State.Status == RoundStatus.Playing, "Cancelled result never appears");
            return "PLAY RESULT QUIET PASS";
        }
    }
}
```

- [ ] **Bước 2: Chạy RED** — `unity recompile`. Kỳ vọng `CS0246: The type or namespace name 'WoolButtonJuice' could not be found` (và `WoolPopIn`), `CS0117` cho `WoolLevelCard.PulseAt`, `WoolScene.WinResultDelay`, `ResultPending`.

- [ ] **Bước 3: Tạo `Assets/JamJam/Runtime/WoolButtonJuice.cs`**

```csharp
using System;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float PressedScale = 0.92f;
        public const float PressTime = 0.06f;
        public const float ReleaseTime = 0.18f;
        IDisposable clock;
        IWoolHaptics haptics;
        WoolSettings settings;
        bool vibration;
        bool pressed;
        bool overshoot;
        float from = 1;
        float to = 1;
        float elapsed;
        float duration;

        public bool Animating => clock != null;
        public bool Pressed => pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            var selectable = GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return;
            pressed = true;
            Buzz();
            Move(PressedScale, PressTime, false);
        }

        public void OnPointerUp(PointerEventData eventData) { Release(); }

        public void OnPointerExit(PointerEventData eventData) { Release(); }

        public void Step(float delta)
        {
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            elapsed += delta;
            var t = duration > 0 ? WoolEase.Clamp(elapsed / duration) : 1;
            var k = overshoot ? WoolEase.OutBack(t) : t;
            transform.localScale = Vector3.one * (t >= 1 ? to : from + (to - from) * k);
            if (t >= 1) Stop();
        }

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            Move(1, ReleaseTime, true);
        }

        void Move(float target, float time, bool back)
        {
            from = transform.localScale.x;
            to = target;
            elapsed = 0;
            duration = time;
            overshoot = back;
            if (clock == null) clock = Observable.EveryUpdate(UnityFrameProvider.Update).Subscribe(_ => Step(Time.unscaledDeltaTime));
        }

        void Stop()
        {
            clock?.Dispose();
            clock = null;
        }

        void Buzz()
        {
            if (settings == null) settings = new WoolSettings(new WoolPlayerPrefs());
            var enabled = settings.Vibration;
            if (haptics == null || enabled != vibration)
            {
                haptics?.Dispose();
                WoolHaptics.Create(enabled, out haptics);
                vibration = enabled;
            }
            WoolHapticPattern.Get(WoolFeelKind.BlockLaunched, out var timings, out var amplitudes);
            haptics.Play(timings, amplitudes);
        }

        void OnDisable()
        {
            Stop();
            pressed = false;
            transform.localScale = Vector3.one;
        }

        void OnDestroy()
        {
            haptics?.Dispose();
            haptics = null;
        }
    }
}
```

- [ ] **Bước 4: Tạo `Assets/JamJam/Runtime/WoolPopIn.cs`**

```csharp
using System;
using R3;
using UnityEngine;

namespace JamJam
{
    public sealed class WoolPopIn : MonoBehaviour
    {
        public const float DefaultFromScale = 0.8f;
        public const float DefaultScaleTime = 0.3f;
        public const float DefaultFadeTime = 0.2f;
        public CanvasGroup Fade;
        public float FromScale = DefaultFromScale;
        public float ScaleTime = DefaultScaleTime;
        public float FadeTime = DefaultFadeTime;
        IDisposable clock;
        float elapsed;

        public bool Animating => clock != null;

        void OnEnable()
        {
            elapsed = 0;
            Apply();
            clock?.Dispose();
            clock = Observable.EveryUpdate(UnityFrameProvider.Update).Subscribe(_ => Step(Time.unscaledDeltaTime));
        }

        public void Step(float delta)
        {
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            elapsed += delta;
            Apply();
            if (elapsed >= ScaleTime && elapsed >= FadeTime) Stop();
        }

        void Apply()
        {
            var t = ScaleTime > 0 ? WoolEase.Clamp(elapsed / ScaleTime) : 1;
            transform.localScale = Vector3.one * (t >= 1 ? 1 : FromScale + (1 - FromScale) * WoolEase.OutBack(t));
            if (Fade != null) Fade.alpha = FadeTime > 0 ? WoolEase.Clamp(elapsed / FadeTime) : 1;
        }

        void Stop()
        {
            clock?.Dispose();
            clock = null;
        }

        void OnDisable()
        {
            Stop();
            transform.localScale = Vector3.one;
            if (Fade != null) Fade.alpha = 1;
        }
    }
}
```

- [ ] **Bước 5: `WoolLevelCard` — màu theo trạng thái + nhịp**

**Trước**
```1:34:Assets/JamJam/Runtime/WoolLevelCard.cs
using UnityEngine;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolLevelCard : MonoBehaviour
    {
        public Button Button;
        public RectTransform Content;
        public CanvasGroup Group;
        public Text Number;
        public Text Title;
        public GameObject Check;
        public GameObject Lock;
        public GameObject Current;
        public float Shake;

        public void Bind(int index,string title,bool unlocked,bool cleared,bool current)
        {
            Number.text=(index+1).ToString("00");
            Title.text=unlocked?title:"Locked";
            Check.SetActive(cleared);
            Lock.SetActive(!unlocked);
            Current.SetActive(current);
            Group.alpha=unlocked?1:0.55f;
        }

        public void Animate(float delta)
        {
            Shake=Mathf.Max(0,Shake-delta);
            Content.anchoredPosition=new Vector2(Mathf.Sin(Shake*95)*Shake*40,0);
        }
    }
}
```

**Sau**
```csharp
using UnityEngine;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolLevelCard : MonoBehaviour
    {
        public const float PulseScale = 0.04f;
        public const float PulsePeriod = 1.2f;
        public Button Button;
        public RectTransform Content;
        public CanvasGroup Group;
        public Text Number;
        public Text Title;
        public GameObject Check;
        public GameObject Lock;
        public GameObject Current;
        public Image Face;
        public Image Edge;
        public float Shake;
        bool pulsing;
        float pulse;

        public bool Pulsing => pulsing;

        public void Bind(int index,string title,bool unlocked,bool cleared,bool current)
        {
            Number.text=(index+1).ToString("00");
            Title.text=unlocked?title:"Locked";
            Check.SetActive(cleared);
            Lock.SetActive(!unlocked);
            Current.SetActive(current);
            Group.alpha=unlocked?1:WoolUiTheme.LockedAlpha;
            WoolUiTheme.CardColors(unlocked,cleared,current,out var face,out var edge);
            if(Face!=null) Face.color=face;
            if(Edge!=null) Edge.color=edge;
            pulsing=current;
            if(pulsing) return;
            pulse=0;
            Content.localScale=Vector3.one;
        }

        public void Animate(float delta)
        {
            Shake=Mathf.Max(0,Shake-delta);
            Content.anchoredPosition=new Vector2(Mathf.Sin(Shake*95)*Shake*40,0);
            if(!pulsing) return;
            pulse=Mathf.Repeat(pulse+delta,PulsePeriod);
            Content.localScale=Vector3.one*PulseAt(pulse);
        }

        public static float PulseAt(float time)
        {
            return 1+PulseScale*0.5f*(1-Mathf.Cos(time/PulsePeriod*Mathf.PI*2));
        }
    }
}
```

- [ ] **Bước 6: `WoolScene` — hằng và API trễ**

**Trước**
```29:30:Assets/JamJam/Runtime/WoolScene.cs
        public int CurrentLevel { get; private set; }
        WoolProgress progress;
```

**Sau**
```csharp
        public int CurrentLevel { get; private set; }
        public const float WinResultDelay = 0.9f;
        public const float LoseResultDelay = 0.6f;
        public bool ResultPending => resultDelay != null;
        public double ResultQueuedAt { get; private set; }
        WoolProgress progress;
```

- [ ] **Bước 7: `WoolScene` — danh sách bóng số ống chỉ và field timer**

**Trước**
```35:35:Assets/JamJam/Runtime/WoolScene.cs
        readonly List<TextMesh> spoolLabels = new List<TextMesh>();
```

**Sau**
```csharp
        readonly List<TextMesh> spoolLabels = new List<TextMesh>();
        readonly List<TextMesh> spoolShadows = new List<TextMesh>();
```

**Trước**
```47:48:Assets/JamJam/Runtime/WoolScene.cs
        IDisposable clock;
        Transform dragon;
```

**Sau**
```csharp
        IDisposable clock;
        IDisposable resultDelay;
        Transform dragon;
```

- [ ] **Bước 8: `WoolScene` — hủy timer khi disable/destroy**

**Trước**
```84:87:Assets/JamJam/Runtime/WoolScene.cs
        void OnDisable() { clock?.Dispose(); clock = null; }
        void OnDestroy()
        {
            foreach(var mesh in roadMeshes) if(mesh!=null) Destroy(mesh);
```

**Sau**
```csharp
        void OnDisable() { clock?.Dispose(); clock = null; CancelResult(); }
        void OnDestroy()
        {
            CancelResult();
            foreach(var mesh in roadMeshes) if(mesh!=null) Destroy(mesh);
```

- [ ] **Bước 9: `WoolScene.LoadLevel` — hủy timer (Replay/Next đi qua đây), xóa danh sách bóng**

**Trước**
```93:98:Assets/JamJam/Runtime/WoolScene.cs
            clock?.Dispose();
            clock = null;
            foreach (Transform child in World) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach(var mesh in roadMeshes) Destroy(mesh);
            roadMeshes.Clear();
            blockViews.Clear(); units.Clear(); spoolViews.Clear(); spoolLabels.Clear(); threads.Clear(); slots.Clear(); confetti.Clear(); confettiVelocity.Clear();
```

**Sau**
```csharp
            clock?.Dispose();
            clock = null;
            CancelResult();
            foreach (Transform child in World) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach(var mesh in roadMeshes) Destroy(mesh);
            roadMeshes.Clear();
            blockViews.Clear(); units.Clear(); spoolViews.Clear(); spoolLabels.Clear(); threads.Clear(); slots.Clear(); confetti.Clear(); confettiVelocity.Clear();
            spoolShadows.Clear();
```

- [ ] **Bước 10: `WoolScene.LoadLevel` — lấy `Counter/Shadow` (null khi prefab chưa có bóng)**

**Trước**
```146:146:Assets/JamJam/Runtime/WoolScene.cs
                spoolLabels.Add(spool.GetComponentInChildren<TextMesh>());
```

**Sau**
```csharp
                var label = spool.GetComponentInChildren<TextMesh>();
                spoolLabels.Add(label);
                var shadow = label.transform.Find("Shadow");
                spoolShadows.Add(shadow != null ? shadow.GetComponent<TextMesh>() : null);
```

- [ ] **Bước 11: `WoolScene.Home` — hủy timer**

**Trước**
```196:196:Assets/JamJam/Runtime/WoolScene.cs
        public void Home() { if (leaving) return; leaving = true; clock?.Dispose(); clock = null; SceneManager.LoadScene(WoolSession.LobbyScene); }
```

**Sau**
```csharp
        public void Home() { if (leaving) return; leaving = true; clock?.Dispose(); clock = null; CancelResult(); SceneManager.LoadScene(WoolSession.LobbyScene); }
```

- [ ] **Bước 12: `WoolScene.Tick` — hoãn panel kết quả** (`MarkCleared`/`Burst` vẫn ngay lập tức)

**Trước**
```209:220:Assets/JamJam/Runtime/WoolScene.cs
                lastStatus = State.Status;
                ResultPanel.SetActive(true);
                WonContent.SetActive(State.Status == RoundStatus.Won);
                LostContent.SetActive(State.Status == RoundStatus.Lost);
                NextButton.gameObject.SetActive(State.Status == RoundStatus.Won);
                if (State.Status == RoundStatus.Won)
                {
                    progress.MarkCleared(CurrentLevel);
                    Burst();
                }
            }
        }
```

**Sau**
```csharp
                lastStatus = State.Status;
                QueueResult(State.Status);
                if (State.Status == RoundStatus.Won)
                {
                    progress.MarkCleared(CurrentLevel);
                    Burst();
                }
            }
        }

        void QueueResult(RoundStatus status)
        {
            CancelResult();
            ResultQueuedAt = Time.timeAsDouble;
            var delay = status == RoundStatus.Won ? WinResultDelay : LoseResultDelay;
            resultDelay = Observable.Timer(TimeSpan.FromSeconds(delay), UnityTimeProvider.Update).Subscribe(_ => ShowResult(status));
        }

        void ShowResult(RoundStatus status)
        {
            CancelResult();
            ResultPanel.SetActive(true);
            WonContent.SetActive(status == RoundStatus.Won);
            LostContent.SetActive(status == RoundStatus.Lost);
            NextButton.gameObject.SetActive(status == RoundStatus.Won);
        }

        void CancelResult()
        {
            resultDelay?.Dispose();
            resultDelay = null;
        }
```

- [ ] **Bước 13: `WoolScene.RenderFrame` — đồng bộ text bóng**

**Trước**
```310:310:Assets/JamJam/Runtime/WoolScene.cs
                spoolLabels[i].text = spool.Remaining.ToString();
```

**Sau**
```csharp
                spoolLabels[i].text = spool.Remaining.ToString();
                if (spoolShadows[i] != null) spoolShadows[i].text = spoolLabels[i].text;
```

- [ ] **Bước 14: Compile** — `unity recompile`. Kỳ vọng 0 lỗi (RED Bước 2 hết), không cảnh báo mới.

- [ ] **Bước 15: GREEN Play Mode** — mở scene: `unity command eval --caller plugin --skill unity-cli --no-banner 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/WoolJam.unity"); return "open";'`, Game view hiển thị, **[PLAY START]**, đợi level nạp, rồi mỗi dòng một eval theo thứ tự (`E` = `unity command eval --caller plugin --skill unity-cli --no-banner`):

```bash
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
E 'return JamJam.Editor.WoolArtChecks.PlayFeel();'
E 'return JamJam.Editor.WoolArtChecks.PlayBlocks();'
```
Kỳ vọng lần lượt: `PLAY UI PASS / juice / pop-in / pulse`; `ARMED / Won`; `EARLY PASS / 0.xx s` (xx < 0.90); `PLAY RESULT PASS / Won / …` — nếu trả `ART CHECK FAILED: Wait longer: …` thì chạy lại đúng eval đó tới khi PASS; `ARMED / Lost`; `EARLY PASS …`; `PLAY RESULT PASS / Lost / …`; `ARMED / Won`; `EARLY PASS / … / replayed`; chạy `PlayResultQuiet` khi đã qua ≥ 1.2s thật (chạy lại nếu cần, kết quả phải luôn PASS) → `PLAY RESULT QUIET PASS`; `PLAY FEEL PASS / …`; `PLAY BLOCKS PASS / 24 blocks`. Nếu một eval `PlayResultEarly` FAIL, chạy `E 'UnityEngine.Time.timeScale = 1f; return "ts";'` trước khi làm lại từ `PlayResultArm`. **[CONSOLE ERRORS]** không có lỗi mới. **[PLAY STOP]**.

- [ ] **Bước 16: Khôi phục PlayerPrefs** — với `N` trong `cleared.txt`: `E 'UnityEngine.PlayerPrefs.SetInt("JamJam.Cleared", N); UnityEngine.PlayerPrefs.Save(); return UnityEngine.PlayerPrefs.GetInt("JamJam.Cleared", -1);'` (nếu `N = -1`: `DeleteKey("JamJam.Cleared")`). Kỳ vọng trả lại đúng `N`.

- [ ] **Bước 17: Kiểm tra** — `md5sum -c "$SCRATCH/ui-run/logic.md5"` → `OK`; harness như Bước 0 Task 1; `grep -rn "//\|/\*" Assets/JamJam/Runtime/WoolButtonJuice.cs Assets/JamJam/Runtime/WoolPopIn.cs Assets/JamJam/Runtime/WoolLevelCard.cs Assets/JamJam/Editor/WoolArtChecks.PlayUi.cs` → rỗng.

- [ ] **Bước 18: Dừng, báo user.**

---

### Task 3: `JamJam/Rebuild UI` cho `WoolJam.unity` + số ống chỉ qua `Rebuild Art`

**Files:**
- Create: `Assets/JamJam/Editor/WoolUiBuilder.cs`, `Assets/JamJam/Editor/WoolArtChecks.Ui.cs`
- Modify: `Assets/JamJam/Editor/WoolAssetsBuilder.cs:504-505, 635-637`
- Patch: `Assets/Scenes/WoolJam.unity`; sinh lại `Generated/Prefabs/Spool.prefab`, `SpoolSlot.prefab` (và mọi asset của `Rebuild Art`, giữ GUID)

**Interfaces — Consumes:** toàn bộ `WoolUiStyle`/`WoolUiTheme` (Task 1); `WoolButtonJuice`, `WoolPopIn` (Task 2).

**Interfaces — Produces:**
- `static partial class WoolUiBuilder` (namespace `JamJam.Editor`): `const string GameScene`; `Vector2 RetrySize (265,100), BadgeSize (150,124), TrackSize (790,36), FillSize (778,24)`; `[MenuItem("JamJam/Rebuild UI")] void Rebuild()`; `static partial void RebuildLobby()`; `void StyleGame(Transform canvas)`; `void Juice(Button)`; `void PopIn(Transform card, CanvasGroup fade)`; private `Panel(Transform parent, string path, Sprite cardSprite, out Transform card)`, `FindCanvas(out Transform)`, `Get<T>(Transform root, string path, out T)`.
- `WoolAssetsBuilder.CounterShadowOffset` (`Vector3(0.3, -0.45, 0.4)`), private `Shadow(TextMesh, out TextMesh)`.
- `WoolArtChecks.UiGame()` → `"UI GAME PASS / N texts / M buttons / spool counter shadow"`; helper `OpenForCheck`, `ScanUi(GameObject[], out int texts, out int buttons)`, `ExpectPanel(Transform)`, `Backdrop(Transform)`, `NodePath(Transform)` (Task 4 dùng lại).

Đường dẫn object được patch (đã đối chiếu `m_Name` trong `WoolJam.unity`, tương đối với `Canvas`): `Brand`, `Goal`, `LevelBadge`, `LevelBadge/LevelCaption`, `LevelBadge/LevelNumber`, `Pause`, `ProgressTrack`, `ProgressTrack/ProgressFill`, `ProgressNumber`, `TrayCaption`, `Instructions`, `Help`, `Retry`, `PausePanel`, `PausePanel/Card/{Title,Body,Resume}`, `PausePanel/Home`, `ResultPanel`, `ResultPanel/Card/WonContent/{Title,Subtitle}`, `ResultPanel/Card/LostContent/{Title,Subtitle}`, `ResultPanel/Card/{Next,PlayAgain}`, `ResultPanel/Home`; nhãn nút là con `Label`.

- [ ] **Bước 1: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.Ui.cs`**

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string UiGame()
        {
            Expect(!EditorApplication.isPlaying, "Exit Play Mode first");
            OpenForCheck(WoolUiBuilder.GameScene, out var scene, out var opened);
            int texts, buttons;
            try
            {
                ScanUi(scene.GetRootGameObjects(), out texts, out buttons);
                var game = Object.FindFirstObjectByType<WoolScene>();
                Expect(game != null, "WoolScene exists");
                ExpectPanel(game.PausePanel.transform);
                ExpectPanel(game.ResultPanel.transform);
                Expect(((RectTransform)game.RetryButton.transform).sizeDelta == WoolUiBuilder.RetrySize, "Retry is 265x100");
                Expect(game.ProgressFill.type == Image.Type.Filled && game.ProgressFill.sprite != null && game.ProgressFill.sprite.name == "UiBar", "Progress fill uses the UiBar sprite and stays Filled");
                var won = game.WonContent.transform.Find("Title").GetComponent<Text>();
                var lost = game.LostContent.transform.Find("Title").GetComponent<Text>();
                Expect(Near(won.color, WoolUiTheme.Primary) && Near(lost.color, WoolUiTheme.Danger), "Won title is Primary and lost title is Danger");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            LoadArt(out var art);
            WoolUiStyle.LoadFont(out var font, out _);
            var counter = art.Spool.GetComponentInChildren<TextMesh>();
            Expect(counter.name == "Counter" && counter.font == font && Near(counter.color, Color.white), "Spool counter uses the theme font in white");
            var shadow = counter.transform.Find("Shadow");
            Expect(shadow != null && shadow.GetComponent<TextMesh>().font == font && Near(shadow.GetComponent<TextMesh>().color, WoolUiTheme.TextOutline) && shadow.localPosition == WoolAssetsBuilder.CounterShadowOffset, "Spool counter has a navy shadow behind it");
            return "UI GAME PASS / " + texts + " texts / " + buttons + " buttons / spool counter shadow";
        }

        static void OpenForCheck(string path, out Scene scene, out bool opened)
        {
            scene = SceneManager.GetSceneByPath(path);
            opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        static void ScanUi(GameObject[] roots, out int texts, out int buttons)
        {
            WoolUiStyle.LoadFont(out var font, out var style);
            texts = 0;
            buttons = 0;
            foreach (var root in roots)
            {
                foreach (var text in root.GetComponentsInChildren<Text>(true))
                {
                    var name = NodePath(text.transform);
                    Expect(text.font == font && text.fontStyle == style, name + " uses the theme font");
                    var outline = text.GetComponent<Outline>();
                    var white = text.color.r > 0.99f && text.color.g > 0.99f && text.color.b > 0.99f;
                    Expect(!white || outline != null, name + " is white and has an Outline");
                    var back = outline != null ? outline.effectColor : Backdrop(text.transform);
                    var ratio = WoolUiTheme.Contrast(text.color, back);
                    Expect(ratio >= WoolUiTheme.MinContrast, name + " contrast " + ratio.ToString("F2") + " is at least 4.5");
                    texts++;
                }
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    var name = NodePath(button.transform);
                    var size = ((RectTransform)button.transform).rect.size;
                    Expect(button.GetComponent<WoolButtonJuice>() != null, name + " has WoolButtonJuice");
                    Expect(size.x >= WoolUiTheme.MinTouch && size.y >= WoolUiTheme.MinTouch, name + " is at least 96x96 but was " + size);
                    Expect(button.transition == Selectable.Transition.None && button.transform.Find("Face") != null, name + " is a raised button without tint");
                    buttons++;
                }
            }
        }

        static void ExpectPanel(Transform panel)
        {
            var overlay = panel.GetComponent<Image>();
            var group = panel.GetComponent<CanvasGroup>();
            Expect(overlay != null && overlay.sprite == null && Near(overlay.color, WoolUiTheme.Overlay) && Mathf.Abs(overlay.color.a - 0.72f) < 0.001f, panel.name + " overlay is #0A1230 at 0.72");
            var card = panel.Find("Card");
            var pop = card.GetComponent<WoolPopIn>();
            Expect(group != null && pop != null && pop.Fade == group, panel.name + " card pops in and fades its overlay");
            Expect(Mathf.Abs(pop.FromScale - 0.8f) < 1e-4f && Mathf.Abs(pop.ScaleTime - 0.3f) < 1e-4f && Mathf.Abs(pop.FadeTime - 0.2f) < 1e-4f, panel.name + " pop-in is 0.8 to 1 in 0.3 s with a 0.2 s fade");
            Expect(Near(card.GetComponent<Image>().color, WoolUiTheme.CardEdge) && card.Find("Face") != null && Near(card.Find("Face").GetComponent<Image>().color, WoolUiTheme.Card), panel.name + " card is cream with a navy border");
            Expect(card.localScale == Vector3.one, panel.name + " card is saved at scale 1");
        }

        static Color Backdrop(Transform node)
        {
            for (var parent = node.parent; parent != null; parent = parent.parent)
            {
                var face = parent.Find("Face");
                if (face != null && face != node && face.GetComponent<Image>() != null) return face.GetComponent<Image>().color;
                var image = parent.GetComponent<Image>();
                if (image != null && image.color.a > 0.5f) return image.color;
            }
            return WoolUiTheme.Navy;
        }

        static string NodePath(Transform node)
        {
            var path = node.name;
            for (var parent = node.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }
    }
}
```

- [ ] **Bước 2: Chạy RED** — `unity recompile`. Kỳ vọng `CS0103: The name 'WoolUiBuilder' does not exist in the current context` và `CS0117: 'WoolAssetsBuilder' does not contain a definition for 'CounterShadowOffset'`.

- [ ] **Bước 3: Tạo `Assets/JamJam/Editor/WoolUiBuilder.cs`**

```csharp
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolUiBuilder
    {
        public const string GameScene = "Assets/Scenes/WoolJam.unity";
        public static readonly Vector2 RetrySize = new Vector2(265, 100);
        public static readonly Vector2 BadgeSize = new Vector2(150, 124);
        public static readonly Vector2 TrackSize = new Vector2(790, 36);
        public static readonly Vector2 FillSize = new Vector2(778, 24);

        [MenuItem("JamJam/Rebuild UI")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding UI");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before rebuilding UI");
            WoolUiStyle.BuildSprites();
            var scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            FindCanvas(out var canvas);
            StyleGame(canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            RebuildLobby();
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("Wool UI rebuilt");
        }

        static partial void RebuildLobby();

        public static void StyleGame(Transform canvas)
        {
            WoolUiStyle.LoadSprite("UiPill", out var pill);
            WoolUiStyle.LoadSprite("UiCard", out var cardSprite);
            WoolUiStyle.LoadSprite("UiCircle", out var circle);
            WoolUiStyle.LoadSprite("UiBar", out var bar);
            Get(canvas, "Brand", out Text brand); WoolUiStyle.Light(brand, 56);
            Get(canvas, "Goal", out Text goal); WoolUiStyle.Light(goal, 22, WoolUiTheme.SoftAlpha);
            Get(canvas, "LevelBadge", out Image badge);
            WoolUiStyle.Raised(badge, pill, WoolUiTheme.Warm, WoolUiTheme.WarmEdge, new Vector4(0, WoolUiTheme.EdgeDepth, 0, 0), out _);
            badge.rectTransform.sizeDelta = BadgeSize;
            Get(canvas, "LevelBadge/LevelCaption", out Text caption); WoolUiStyle.Light(caption, 22); caption.rectTransform.anchoredPosition = new Vector2(0, 33);
            Get(canvas, "LevelBadge/LevelNumber", out Text number); WoolUiStyle.Light(number, 54); number.rectTransform.anchoredPosition = new Vector2(0, -13);
            Get(canvas, "Pause", out Button pause); WoolUiStyle.Button(pause, WoolUiTone.Warm, circle, 44); Juice(pause);
            Get(canvas, "ProgressTrack", out Image track); Get(canvas, "ProgressTrack/ProgressFill", out Image fill);
            WoolUiStyle.Bar(track, fill, pill, bar);
            track.rectTransform.sizeDelta = TrackSize; fill.rectTransform.sizeDelta = FillSize;
            Get(canvas, "ProgressNumber", out Text percent); WoolUiStyle.Light(percent, 30);
            Get(canvas, "TrayCaption", out Text tray); WoolUiStyle.Light(tray, 20, WoolUiTheme.SoftAlpha);
            Get(canvas, "Instructions", out Text instructions); WoolUiStyle.Light(instructions, 25, WoolUiTheme.SoftAlpha);
            Get(canvas, "Help", out Text help); WoolUiStyle.Light(help, 28, WoolUiTheme.SoftAlpha);
            Get(canvas, "Retry", out Button retry); WoolUiStyle.Button(retry, WoolUiTone.Secondary, pill, 34); Juice(retry);
            ((RectTransform)retry.transform).sizeDelta = RetrySize;
            Panel(canvas, "PausePanel", cardSprite, out var pauseCard);
            Get(pauseCard, "Title", out Text pauseTitle); WoolUiStyle.Dark(pauseTitle, 54);
            Get(pauseCard, "Body", out Text pauseBody); WoolUiStyle.Dark(pauseBody, 30);
            Get(pauseCard, "Resume", out Button resume); WoolUiStyle.Button(resume, WoolUiTone.Primary, pill, 36); Juice(resume);
            Get(canvas, "PausePanel/Home", out Button pauseHome); WoolUiStyle.Button(pauseHome, WoolUiTone.Secondary, pill, 34); Juice(pauseHome);
            Panel(canvas, "ResultPanel", cardSprite, out var resultCard);
            Get(resultCard, "WonContent/Title", out Text won); WoolUiStyle.Title(won, WoolUiTheme.Primary, 80);
            Get(resultCard, "WonContent/Subtitle", out Text wonBody); WoolUiStyle.Dark(wonBody, 31);
            Get(resultCard, "LostContent/Title", out Text lost); WoolUiStyle.Title(lost, WoolUiTheme.Danger, 74);
            Get(resultCard, "LostContent/Subtitle", out Text lostBody); WoolUiStyle.Dark(lostBody, 30);
            Get(resultCard, "Next", out Button next); WoolUiStyle.Button(next, WoolUiTone.Primary, pill, 36); Juice(next);
            Get(resultCard, "PlayAgain", out Button again); WoolUiStyle.Button(again, WoolUiTone.Secondary, pill, 34); Juice(again);
            Get(canvas, "ResultPanel/Home", out Button resultHome); WoolUiStyle.Button(resultHome, WoolUiTone.Secondary, pill, 34); Juice(resultHome);
        }

        public static void Juice(Button button)
        {
            if (button.GetComponent<WoolButtonJuice>() == null) button.gameObject.AddComponent<WoolButtonJuice>();
        }

        public static void PopIn(Transform card, CanvasGroup fade)
        {
            var pop = card.GetComponent<WoolPopIn>();
            if (pop == null) pop = card.gameObject.AddComponent<WoolPopIn>();
            pop.Fade = fade;
            pop.FromScale = WoolPopIn.DefaultFromScale;
            pop.ScaleTime = WoolPopIn.DefaultScaleTime;
            pop.FadeTime = WoolPopIn.DefaultFadeTime;
            EditorUtility.SetDirty(pop);
        }

        static void Panel(Transform parent, string path, Sprite cardSprite, out Transform card)
        {
            Get(parent, path, out Image overlay);
            WoolUiStyle.Overlay(overlay, out var group);
            Get(overlay.transform, "Card", out Image cardImage);
            WoolUiStyle.Card(cardImage, cardSprite);
            PopIn(cardImage.transform, group);
            card = cardImage.transform;
        }

        static void FindCanvas(out Transform canvas)
        {
            var found = GameObject.Find("Canvas");
            if (found == null) throw new InvalidOperationException("Canvas is missing from " + SceneManager.GetActiveScene().path);
            canvas = found.transform;
        }

        static void Get<T>(Transform root, string path, out T component) where T : Component
        {
            var node = root.Find(path);
            if (node == null) throw new InvalidOperationException(path + " is missing under " + root.name);
            component = node.GetComponent<T>();
            if (component == null) throw new InvalidOperationException(path + " has no " + typeof(T).Name);
        }
    }
}
```

- [ ] **Bước 4: `WoolAssetsBuilder.MakeSpool` — bóng cho số ống chỉ**

**Trước**
```504:505:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            Label(go.transform, "Counter", "8", new Vector3(0,-0.2f,-0.52f), 0.095f, white, out _);
            SavePrefab(go, "Spool", out prefab);
```

**Sau**
```csharp
            Label(go.transform, "Counter", "8", new Vector3(0,-0.2f,-0.52f), 0.095f, white, out var counter);
            Shadow(counter, out _);
            SavePrefab(go, "Spool", out prefab);
```

- [ ] **Bước 5: `WoolAssetsBuilder.Label` — font theme + helper bóng**

**Trước**
```635:637:Assets/JamJam/Editor/WoolAssetsBuilder.cs
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontStyle=FontStyle.Bold; label.color=color.GetColor("_BaseColor");
            go.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
        }
```

**Sau**
```csharp
            WoolUiStyle.LoadFont(out var font,out var style);
            label.font=font; label.fontStyle=style; label.color=color.GetColor("_BaseColor");
            go.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
        }

        public static readonly Vector3 CounterShadowOffset=new Vector3(0.3f,-0.45f,0.4f);

        static void Shadow(TextMesh label,out TextMesh shadow)
        {
            var go=new GameObject("Shadow"); go.transform.SetParent(label.transform,false); go.transform.localPosition=CounterShadowOffset;
            shadow=go.AddComponent<TextMesh>(); shadow.text=label.text; shadow.fontSize=label.fontSize; shadow.characterSize=label.characterSize; shadow.anchor=label.anchor; shadow.alignment=label.alignment;
            shadow.font=label.font; shadow.fontStyle=label.fontStyle; shadow.color=WoolUiTheme.TextOutline;
            go.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
        }
```

- [ ] **Bước 6: Compile + RED dữ liệu** — `unity recompile` (0 lỗi), rồi `E 'return JamJam.Editor.WoolArtChecks.UiGame();'`. Kỳ vọng `ART CHECK FAILED: … uses the theme font` (scene chưa patch).

- [ ] **Bước 7: Rebuild** (thoát Play Mode, lưu scene đang mở)
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolArtBuilder.Rebuild(); return "art";'
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolUiBuilder.Rebuild(); return "ui";'
```
Kỳ vọng `art`, `ui`; console có `Wool art rebuilt`, `Wool UI rebuilt`.

- [ ] **Bước 8: GREEN + chuỗi + hồi quy art**
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.UiGame();'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.UiAssets();'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.Tray() + " | " + JamJam.Editor.WoolArtChecks.Textures() + " | " + JamJam.Editor.WoolArtChecks.Feel();'
grep "m_Text:" Assets/Scenes/WoolJam.unity | sort | diff "$SCRATCH/ui-run/strings.WoolJam.unity.before.txt" - && echo STRINGS SAME
md5sum -c "$SCRATCH/ui-run/levels.md5"
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor Assets/Scenes --include=*.meta | sort > "$SCRATCH/ui-run/guids.now.txt"
comm -23 "$SCRATCH/ui-run/guids.before.txt" "$SCRATCH/ui-run/guids.now.txt"
```
Kỳ vọng: `UI GAME PASS / 21 texts / 7 buttons / spool counter shadow`; `UI ASSETS PASS …`; `TRAY PASS … | TEXTURES PASS … | FEEL PASS …`; `STRINGS SAME`; level `OK`; `comm` rỗng.

- [ ] **Bước 9: Play Mode** — mở `WoolJam.unity`, **[PLAY START]**, chạy lại chuỗi eval của Task 2 Bước 15 (lúc này `WoolButtonJuice`/`WoolPopIn`/`CanvasGroup` đã có sẵn trong scene nên check dùng bản đã patch), thêm: `E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(1); return "L2";'`, đợi ~1 giây, **[CAPTURE 1080×1920 → $SCRATCH/ui-run/t3-game.png]**, `E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().Pause(); return "paused";'`, đợi ~0.5 giây, **[CAPTURE 1080×1920 → $SCRATCH/ui-run/t3-pause.png]**. **[CONSOLE ERRORS]** không có lỗi mới. **[PLAY STOP]**, khôi phục PlayerPrefs như Task 2 Bước 16. Đọc hai ảnh bằng Read: chữ trắng viền navy, badge level cam, nút Pause tròn cam, thanh tiến độ navy/xanh, Retry kem nổi khối, số ống chỉ Lilita có bóng; thẻ Pause kem viền navy, Resume xanh, Home kem.

- [ ] **Bước 10: Kiểm tra** — `md5sum -c "$SCRATCH/ui-run/logic.md5"` → `OK`; `git status --short Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab` → rỗng (Task 3 chưa chạm lobby); `grep -rn "//\|/\*" Assets/JamJam/Editor/WoolUiBuilder.cs Assets/JamJam/Editor/WoolArtChecks.Ui.cs` → rỗng.

- [ ] **Bước 11: Dừng, báo user** (kèm hai ảnh scratch).

---

### Task 4: Lobby, prefab `LevelCard`, Settings + builder dùng chung theme

**Files:**
- Create: `Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs`, `Assets/JamJam/Editor/WoolArtChecks.UiLobby.cs`
- Modify: `Assets/JamJam/Editor/WoolSceneBuilder.cs:32-33, 52, 196-197`; `Assets/JamJam/Editor/WoolLobbyBuilder.cs:31, 48, 122`
- Patch: `Assets/Scenes/Lobby.unity`, `Assets/JamJam/Generated/Prefabs/LevelCard.prefab` (GUID giữ nguyên)

**Interfaces — Consumes:** `WoolUiBuilder.Get/Juice/Panel/FindCanvas` (Task 3, cùng partial class); `WoolArtChecks.OpenForCheck/ScanUi/ExpectPanel` (Task 3); `WoolLevelCard.Face/Edge/Pulsing` (Task 2).

**Interfaces — Produces:** `WoolUiBuilder.CardPath`; `static partial void RebuildLobby()` (thân); `void StyleLobby(Transform canvas, Camera camera)`; `void StyleCard(GameObject root)`; `WoolArtChecks.UiLobby()`, `WoolArtChecks.PlayLobby()`.

Đường dẫn (đối chiếu `m_Name` trong `Lobby.unity`/`LevelCard.prefab`): `LobbyPanel/{Settings,Brand,Tagline,Play,Levels}`, `LevelsPanel/{Back,Title}`, `SettingsPanel`, `SettingsPanel/Card/{Title,Sound,Music,Vibration,Reset,Close}`, `SettingsPanel/ConfirmPanel`, `SettingsPanel/ConfirmPanel/Card/{Title,Body,Yes,No}`; prefab: `Content`, `Content/{Number,Title,Check,Lock,Current}` (badge có con `Label`).

- [ ] **Bước 1: Check RED — tạo `Assets/JamJam/Editor/WoolArtChecks.UiLobby.cs`**

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string UiLobby()
        {
            Expect(!EditorApplication.isPlaying, "Exit Play Mode first");
            OpenForCheck(WoolLobbyBuilder.ScenePath, out var scene, out var opened);
            int texts, buttons;
            try
            {
                ScanUi(scene.GetRootGameObjects(), out texts, out buttons);
                var lobby = Object.FindFirstObjectByType<WoolLobby>();
                Expect(lobby != null, "WoolLobby exists");
                ExpectPanel(lobby.SettingsPanel.transform);
                ExpectPanel(lobby.ConfirmPanel.transform);
                foreach (var toggle in new[] { lobby.SoundToggle, lobby.MusicToggle, lobby.VibrationToggle })
                    Expect(Near(((Image)toggle.targetGraphic).color, WoolUiTheme.Off) && Near(((Image)toggle.graphic).color, WoolUiTheme.Primary) && toggle.transition == Selectable.Transition.None, toggle.name + " is a green on / navy grey off pill");
                Camera camera = null;
                foreach (var root in scene.GetRootGameObjects()) if (root.name == "Main Camera") camera = root.GetComponent<Camera>();
                Expect(camera != null && Near(camera.backgroundColor, WoolUiTheme.Navy), "Lobby camera clears to navy #14234A");
                Expect(lobby.Kitten != null && lobby.PlayLabel == lobby.PlayButton.transform.Find("Label").GetComponent<Text>(), "Lobby wiring is unchanged");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            var prefab = PrefabUtility.LoadPrefabContents(WoolUiBuilder.CardPath);
            int cardTexts, cardButtons;
            try
            {
                ScanUi(new[] { prefab }, out cardTexts, out cardButtons);
                var view = prefab.GetComponent<WoolLevelCard>();
                Expect(view.Face != null && view.Face.transform.parent == view.Content && view.Edge == view.Content.GetComponent<Image>(), "LevelCard face and edge are wired");
                Expect(view.Check.transform.Find("Icon").GetComponent<Image>().sprite.name == "UiCheck" && view.Lock.transform.Find("Icon").GetComponent<Image>().sprite.name == "UiLock", "Cleared shows a check and locked shows a padlock");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
            WoolUiTheme.CardColors(true, true, false, out var cleared, out _);
            WoolUiTheme.CardColors(true, false, true, out var current, out _);
            WoolUiTheme.CardColors(false, false, false, out var locked, out _);
            Expect(Near(cleared, WoolUiTheme.Primary) && Near(current, WoolUiTheme.Warm) && Near(locked, WoolUiTheme.Locked), "Card colors follow the token table");
            return "UI LOBBY PASS / " + (texts + cardTexts) + " texts / " + (buttons + cardButtons) + " buttons";
        }

        public static string PlayLobby()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var lobby = Object.FindFirstObjectByType<WoolLobby>();
            Expect(lobby != null, "Lobby is running");
            lobby.ShowLevels();
            var cards = lobby.CardContainer.GetComponentsInChildren<WoolLevelCard>();
            Expect(cards.Length == lobby.Levels.Length, "One card per level");
            var currentIndex = -1;
            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                Expect(card.Face != null && card.Button.GetComponent<WoolButtonJuice>() != null, "Card " + i + " is styled");
                card.Animate(0.3f);
                var scale = card.Content.localScale.x;
                if (card.Current.activeSelf)
                {
                    currentIndex = i;
                    Expect(card.Pulsing && Near(card.Face.color, WoolUiTheme.Warm) && scale >= 1 && scale <= 1.0401f, "Current card is warm and pulses within 1..1.04 but scale was " + scale);
                }
                else Expect(!card.Pulsing && scale == 1, "Card " + i + " does not pulse");
                if (card.Check.activeSelf) Expect(Near(card.Face.color, WoolUiTheme.Primary), "Cleared card " + i + " is green");
                if (card.Lock.activeSelf) Expect(Near(card.Face.color, WoolUiTheme.Locked), "Locked card " + i + " is navy");
            }
            lobby.OpenSettings();
            var pop = lobby.SettingsPanel.transform.Find("Card").GetComponent<WoolPopIn>();
            Expect(pop != null && pop.Animating, "Settings card pops in");
            pop.Step(1);
            Expect(Mathf.Abs(pop.transform.localScale.x - 1) < 1e-4f && pop.Fade.alpha == 1, "Settings card settles");
            var prefs = new WoolSettings(new WoolPlayerPrefs());
            var before = prefs.Vibration;
            lobby.VibrationToggle.isOn = !before;
            Expect(prefs.Vibration == !before, "Vibration toggle still saves");
            lobby.VibrationToggle.isOn = before;
            Expect(prefs.Vibration == before, "Vibration toggle restores");
            lobby.CloseSettings();
            lobby.ShowLobby();
            return "PLAY LOBBY PASS / " + cards.Length + " cards / current " + currentIndex;
        }
    }
}
```

- [ ] **Bước 2: Chạy RED** — `unity recompile`. Kỳ vọng `CS0117: 'WoolUiBuilder' does not contain a definition for 'CardPath'`.

- [ ] **Bước 3: Tạo `Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs`**

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolUiBuilder
    {
        public const string CardPath = WoolAssetsBuilder.Root + "/Prefabs/LevelCard.prefab";

        static partial void RebuildLobby()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPath);
            try
            {
                StyleCard(root);
                PrefabUtility.SaveAsPrefabAsset(root, CardPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            var scene = EditorSceneManager.OpenScene(WoolLobbyBuilder.ScenePath, OpenSceneMode.Single);
            FindCanvas(out var canvas);
            var cameraObject = GameObject.Find("Main Camera");
            if (cameraObject == null) throw new System.InvalidOperationException("Main Camera is missing from " + WoolLobbyBuilder.ScenePath);
            StyleLobby(canvas, cameraObject.GetComponent<Camera>());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void StyleLobby(Transform canvas, Camera camera)
        {
            WoolUiStyle.LoadSprite("UiPill", out var pill);
            WoolUiStyle.LoadSprite("UiCard", out var cardSprite);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = WoolUiTheme.Navy;
            EditorUtility.SetDirty(camera);
            Get(canvas, "LobbyPanel/Settings", out Button settings); WoolUiStyle.Button(settings, WoolUiTone.Secondary, pill, 32); Juice(settings);
            Get(canvas, "LobbyPanel/Brand", out Text brand); WoolUiStyle.Logo(brand, 130);
            Get(canvas, "LobbyPanel/Tagline", out Text tagline); WoolUiStyle.Light(tagline, 30, WoolUiTheme.SoftAlpha);
            Get(canvas, "LobbyPanel/Play", out Button play); WoolUiStyle.Button(play, WoolUiTone.Primary, pill, 60); Juice(play);
            Get(canvas, "LobbyPanel/Levels", out Button levels); WoolUiStyle.Button(levels, WoolUiTone.Secondary, pill, 44); Juice(levels);
            Get(canvas, "LevelsPanel/Back", out Button back); WoolUiStyle.Button(back, WoolUiTone.Secondary, pill, 32); Juice(back);
            Get(canvas, "LevelsPanel/Title", out Text title); WoolUiStyle.Light(title, 72);
            Panel(canvas, "SettingsPanel", cardSprite, out var card);
            Get(card, "Title", out Text settingsTitle); WoolUiStyle.Dark(settingsTitle, 60);
            foreach (var name in new[] { "Sound", "Music", "Vibration" })
            {
                Get(card, name, out Toggle toggle);
                WoolUiStyle.Toggle(toggle, pill, 34);
            }
            Get(card, "Reset", out Button reset); WoolUiStyle.Button(reset, WoolUiTone.Secondary, pill, 32); Juice(reset);
            Get(card, "Close", out Button close); WoolUiStyle.Button(close, WoolUiTone.Primary, pill, 36); Juice(close);
            Panel(canvas, "SettingsPanel/ConfirmPanel", cardSprite, out var confirm);
            Get(confirm, "Title", out Text confirmTitle); WoolUiStyle.Dark(confirmTitle, 50);
            Get(confirm, "Body", out Text confirmBody); WoolUiStyle.Dark(confirmBody, 30);
            Get(confirm, "Yes", out Button yes); WoolUiStyle.Button(yes, WoolUiTone.Danger, pill, 34); Juice(yes);
            Get(confirm, "No", out Button no); WoolUiStyle.Button(no, WoolUiTone.Secondary, pill, 34); Juice(no);
        }

        public static void StyleCard(GameObject root)
        {
            WoolUiStyle.LoadSprite("UiPill", out var pill);
            WoolUiStyle.LoadSprite("UiCard", out var cardSprite);
            WoolUiStyle.LoadSprite("UiCheck", out var check);
            WoolUiStyle.LoadSprite("UiLock", out var padlock);
            var view = root.GetComponent<WoolLevelCard>();
            var edge = view.Content.GetComponent<Image>();
            WoolUiStyle.Raised(edge, cardSprite, WoolUiTheme.Secondary, WoolUiTheme.SecondaryEdge, new Vector4(0, WoolUiTheme.EdgeDepth, 0, 0), out var face);
            edge.raycastTarget = true;
            view.Face = face;
            view.Edge = edge;
            view.Button.transition = Selectable.Transition.None;
            Juice(view.Button);
            WoolUiStyle.Light(view.Number, 64);
            WoolUiStyle.Light(view.Title, 38);
            WoolUiStyle.Badge(view.Check.GetComponent<Image>(), WoolUiTheme.Badge, pill, check, 26);
            WoolUiStyle.Badge(view.Lock.GetComponent<Image>(), WoolUiTheme.Badge, pill, padlock, 26);
            WoolUiStyle.Badge(view.Current.GetComponent<Image>(), WoolUiTheme.Primary, pill, null, 28);
            EditorUtility.SetDirty(view);
        }
    }
}
```

- [ ] **Bước 4: `WoolSceneBuilder` dùng font/sprite/style theme** (chỉ compile, không chạy)

**Trước**
```32:33:Assets/JamJam/Editor/WoolSceneBuilder.cs
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildPanelSprite();
```

**Sau**
```csharp
            WoolUiStyle.LoadFont(out font, out _);
            BuildPanelSprite();
            WoolUiStyle.BuildSprites();
```

**Trước**
```52:52:Assets/JamJam/Editor/WoolSceneBuilder.cs
            BuildCanvas(controller);
```

**Sau**
```csharp
            BuildCanvas(controller);
            WoolUiBuilder.StyleGame(controller.PausePanel.transform.parent);
```

**Trước**
```196:197:Assets/JamJam/Editor/WoolSceneBuilder.cs
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panelSprite=AssetDatabase.LoadAssetAtPath<Sprite>(WoolAssetsBuilder.Root+"/Textures/RoundedPanel.png");
```

**Sau**
```csharp
            WoolUiStyle.LoadFont(out font,out _);
            panelSprite=AssetDatabase.LoadAssetAtPath<Sprite>(WoolAssetsBuilder.Root+"/Textures/RoundedPanel.png");
```

- [ ] **Bước 5: `WoolLobbyBuilder` dùng style theme** (chỉ compile, không chạy)

**Trước**
```31:31:Assets/JamJam/Editor/WoolLobbyBuilder.cs
            WoolSceneBuilder.LoadUiResources();
```

**Sau**
```csharp
            WoolSceneBuilder.LoadUiResources();
            WoolUiStyle.BuildSprites();
```

**Trước**
```48:48:Assets/JamJam/Editor/WoolLobbyBuilder.cs
            BuildCanvas(lobby);
```

**Sau**
```csharp
            BuildCanvas(lobby);
            WoolUiBuilder.StyleLobby(lobby.LobbyPanel.transform.parent,camera);
```

**Trước**
```122:122:Assets/JamJam/Editor/WoolLobbyBuilder.cs
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,CardPath);
```

**Sau**
```csharp
            WoolUiBuilder.StyleCard(root.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,CardPath);
```

- [ ] **Bước 6: Compile + RED dữ liệu** — `unity recompile` (0 lỗi), `E 'return JamJam.Editor.WoolArtChecks.UiLobby();'` → `ART CHECK FAILED: … uses the theme font`.

- [ ] **Bước 7: Rebuild UI** (thoát Play Mode, lưu scene) — `E 'JamJam.Editor.WoolUiBuilder.Rebuild(); return "ui";'`. Kỳ vọng `ui`; scene đang mở sau lệnh là `WoolJam.unity`.

- [ ] **Bước 8: GREEN + chuỗi + GUID**
```bash
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.UiLobby();'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.UiGame();'
for f in Assets/Scenes/WoolJam.unity Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab; do grep "m_Text:" "$f" | sort | diff "$SCRATCH/ui-run/strings.$(basename "$f").before.txt" - && echo "SAME $(basename "$f")"; done
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor Assets/Scenes --include=*.meta | sort > "$SCRATCH/ui-run/guids.now.txt"
comm -23 "$SCRATCH/ui-run/guids.before.txt" "$SCRATCH/ui-run/guids.now.txt"
grep -n "WoolUiBuilder\.\|WoolUiStyle\." Assets/JamJam/Editor/WoolSceneBuilder.cs Assets/JamJam/Editor/WoolLobbyBuilder.cs
```
Kỳ vọng: `UI LOBBY PASS / 22 texts / 9 buttons` (scene 17 text + 8 nút, prefab 5 text + 1 nút); `UI GAME PASS …`; ba dòng `SAME …`; `comm` rỗng; `grep` ra 6 dòng (3 mỗi builder).

- [ ] **Bước 9: Play Mode Lobby** — `E 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity"); return "open";'`, **[PLAY START]**, rồi:
```bash
E 'return JamJam.Editor.WoolArtChecks.PlayLobby();'
```
Kỳ vọng `PLAY LOBBY PASS / 3 cards / current N` (N = `progress.Current` nếu chưa xong hết, -1 nếu đã xong cả 3). **[CAPTURE 1080×1920 → $SCRATCH/ui-run/t4-home.png]**. Luồng vào game như cũ:
```bash
E 'var l=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolLobby>(); l.ShowLevels(); l.Choose(0); return "choose 0";'
E 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); return s==null ? "loading" : s.CurrentLevel + " " + s.State.Status;'
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().Home(); return "home";'
E 'var l=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolLobby>(); return l==null ? "loading" : "lobby " + l.LobbyPanel.activeSelf;'
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolLobby>().Play(); return "play";'
E 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); return s==null ? "loading" : s.CurrentLevel + " " + s.State.Status;'
```
Kỳ vọng: `0 Playing` (chạy lại eval nếu trả `loading`); `lobby True`; lần cuối `<progress.Current> Playing`. **[CONSOLE ERRORS]** không có lỗi mới. **[PLAY STOP]**, khôi phục PlayerPrefs như Task 2 Bước 16. Đọc `t4-home.png`: nền navy, logo "WOOL JAM" trắng viền dày, PLAY xanh to, LEVELS/SETTINGS kem nổi khối.

- [ ] **Bước 10: Kiểm tra** — `md5sum -c "$SCRATCH/ui-run/logic.md5"`, `md5sum -c "$SCRATCH/ui-run/levels.md5"` → `OK`; harness như Task 1 Bước 0; `grep -rn "//\|/\*" Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs Assets/JamJam/Editor/WoolArtChecks.UiLobby.cs` → rỗng.

- [ ] **Bước 11: Dừng, báo user.**

---

### Task 5: Kiểm thử tổng, idempotent, ảnh, so sánh store, tinh chỉnh

**Files:** không sửa source ngoài các hằng trong bảng knob (nếu cần). Tạo `docs/ui-*.png`, `docs/ui-compare-*.png`, script tạm `$SCRATCH/ui-run/compare.py`.

**Interfaces — Consumes:** mọi thứ Task 1–4.

**Knob được phép chỉnh** (không đụng `WoolGame`, `WoolRouting`, level, chuỗi chữ, wiring):

| Nơi | Hằng | Ghi chú |
|---|---|---|
| `WoolUiTheme` | màu token, `SoftAlpha`, `OutlineSize`, `LogoOutlineSize`, `TitleInnerOutline`, `TitleOuterOutline`, `ShadowOffset`, `LogoShadowOffset`, `ShadowAlpha`, `EdgeDepth`, `CardBorder`, `CardDepth`, `LockedAlpha` | đổi màu chữ/nền phải giữ `UiAssets`/`UiGame`/`UiLobby` (≥ 4.5); `UiAssets` kiểm cứng offset Card `(6,18)` và Outline 3/Shadow (0,-4) theo spec |
| `WoolButtonJuice` | `PressedScale`, `PressTime`, `ReleaseTime` | `PlayUi` giả định `PressTime ≤ 0.08` và `ReleaseTime ≤ 0.29` |
| `WoolPopIn` | `DefaultFromScale`, `DefaultScaleTime`, `DefaultFadeTime` | `ExpectPanel` kiểm 0.8/0.3/0.2 theo spec |
| `WoolLevelCard` | `PulseScale`, `PulsePeriod` | spec 1↔1.04, 1.2s |
| `WoolScene` | `WinResultDelay`, `LoseResultDelay` | spec 0.9/0.6 |
| `WoolUiStyle` | `PillRadius`, `PillBorder`, `CardRadius`, `CardSpriteBorder`, `BarShade` | đổi xong chạy lại `Rebuild UI` |
| `WoolUiBuilder` | `RetrySize`, `BadgeSize`, `TrackSize`, `FillSize`, cỡ chữ trong `StyleGame`/`StyleLobby`/`StyleCard` | nút giữ ≥ 96×96 |
| `WoolAssetsBuilder` | `CounterShadowOffset` | đổi xong chạy lại `Rebuild Art` |

- [ ] **Bước 1: Hồi quy**
```bash
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
unity command eval --caller plugin --skill unity-cli --no-banner 'JamJam.Editor.WoolEditorChecks.ValidateLevels(); return System.IO.File.ReadAllText("Logs/WoolJam.verify.txt");'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.Textures() + " | " + JamJam.Editor.WoolArtChecks.Blocks() + " | " + JamJam.Editor.WoolArtChecks.Chevron() + " | " + JamJam.Editor.WoolArtChecks.Stage() + " | " + JamJam.Editor.WoolArtChecks.Tray() + " | " + JamJam.Editor.WoolArtChecks.Feel();'
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.UiAssets() + " | " + JamJam.Editor.WoolArtChecks.UiGame() + " | " + JamJam.Editor.WoolArtChecks.UiLobby();'
md5sum -c "$SCRATCH/ui-run/logic.md5"
md5sum -c "$SCRATCH/ui-run/levels.md5"
```
Kỳ vọng: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; 108 feel assertions`; file verify có `LEVEL 1/2/3 SOLUTION PASS`; sáu chuỗi `… PASS …`; ba chuỗi `UI … PASS`; mọi `OK`. Rồi Play Mode `WoolJam.unity`: chuỗi eval Task 2 Bước 15 + `E 'return JamJam.Editor.WoolArtChecks.PlayFinish(true);'` (`PLAY FINISH PASS / Won`), **[PLAY STOP]**; Play Mode `Lobby.unity`: `PlayLobby()`, **[PLAY STOP]**; khôi phục PlayerPrefs.

- [ ] **Bước 2: Idempotent — `Rebuild Art` + `Rebuild UI` hai lần** (thoát Play Mode, lưu scene)
```bash
R='JamJam.Editor.WoolArtBuilder.Rebuild(); JamJam.Editor.WoolUiBuilder.Rebuild(); return "rebuilt";'
unity command eval --caller plugin --skill unity-cli --no-banner "$R"
md5sum Assets/Scenes/WoolJam.unity Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab Assets/JamJam/Generated/Textures/UI/*.png Assets/JamJam/Generated/Textures/UI/*.meta Assets/JamJam/Generated/WoolArt.asset Assets/JamJam/Art/Fonts/* > "$SCRATCH/ui-run/rebuild1.md5"
unity command eval --caller plugin --skill unity-cli --no-banner "$R"
md5sum -c "$SCRATCH/ui-run/rebuild1.md5"
grep -r "^guid:" Assets/JamJam/Generated Assets/JamJam/Art Assets/JamJam/Shaders Assets/JamJam/Runtime Assets/JamJam/Editor Assets/Scenes --include=*.meta | sort > "$SCRATCH/ui-run/guids.now.txt"
comm -23 "$SCRATCH/ui-run/guids.before.txt" "$SCRATCH/ui-run/guids.now.txt"
unity command eval --caller plugin --skill unity-cli --no-banner 'return JamJam.Editor.WoolArtChecks.UiGame() + " | " + JamJam.Editor.WoolArtChecks.UiLobby();'
```
Kỳ vọng: mọi dòng `md5sum -c` là `OK`; `comm` rỗng; hai chuỗi PASS. (Prefab gameplay `Spool`/`Block`… được `Rebuild Art` dựng lại từ object mới nên không đưa vào md5, như plan phần 1–2; nội dung của chúng do `Tray()`/`UiGame()` kiểm.) Nếu một file đổi giữa hai lần, `git diff --no-index` bản lưu tạm để tìm trường không ổn định, sửa hàm style tương ứng cho tất định rồi lặp lại bước này.

- [ ] **Bước 3: Ảnh 1080×1920 và 1080×2400** — mỗi khoảnh khắc chụp hai kích thước: **[CAPTURE 1080×1920 → docs/ui-X.png]** và **[CAPTURE 1080×2400 → docs/ui-X-2400.png]**.

`WoolJam.unity`, **[PLAY START]**:
```bash
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().LoadLevel(1); return "L2";'
```
Đợi ~1 giây → chụp `game`.
```bash
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().Pause(); return "paused";'
```
Đợi ~0.5 giây → chụp `pause`.
```bash
E 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(1); var g=s.State; for(var f=0;f<30000&&g.Status==JamJam.RoundStatus.Playing;f++){ for(var b=0;b<g.Blocks.Count;b++) if(g.CanSelect(b)&&g.HasVisibleColor(g.Blocks[b].Definition.Color)){g.Select(b);break;} g.Step(0.02f);} return g.Status.ToString();'
```
Kỳ vọng `Won`. Đợi ~1.6 giây → chụp `won`.
```bash
E 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(1); var g=s.State; for(var f=0;f<30000&&g.Status==JamJam.RoundStatus.Playing;f++) g.Step(0.02f); return g.Status.ToString();'
```
Kỳ vọng `Lost`. Đợi ~1.2 giây → chụp `lost`. **[PLAY STOP]**.

`Lobby.unity` (mở bằng eval `OpenScene` như Task 4), **[PLAY START]**, đợi ~1 giây → chụp `home`.
```bash
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolLobby>().ShowLevels(); return "levels";'
```
Đợi ~0.5 giây → chụp `levels`.
```bash
E 'var l=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolLobby>(); l.ShowLobby(); l.OpenSettings(); return "settings";'
```
Đợi ~0.5 giây → chụp `settings`. **[PLAY STOP]**. Khôi phục PlayerPrefs. Mở lại `WoolJam.unity`. Kỳ vọng: `ls docs/ui-*.png | wc -l` → `14`.

- [ ] **Bước 4: Ghép so sánh với ảnh store** — tạo `$SCRATCH/ui-run/compare.py` (thư mục script riêng, ảnh store chỉ truyền bằng đường dẫn):

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
REF="$SCRATCH/ref/store"
"$PY" -I "$SCRATCH/ui-run/compare.py" docs/ui-compare-game.png "$REF/shot4.png" docs/ui-game.png docs/ui-game-2400.png
"$PY" -I "$SCRATCH/ui-run/compare.py" docs/ui-compare-won.png "$REF/shot5.png" docs/ui-won.png docs/ui-lost.png
"$PY" -I "$SCRATCH/ui-run/compare.py" docs/ui-compare-home.png "$REF/shot8.png" docs/ui-home.png docs/ui-levels.png docs/ui-settings.png
```
Kỳ vọng ba dòng in đường dẫn + kích thước. Đọc từng ảnh `docs/ui-*.png` và `docs/ui-compare-*.png` bằng Read, chấm checklist:
1. Mọi chữ đọc rõ trên navy (trắng viền navy; chữ tối chỉ nằm trên kem).
2. Nút có lip đậm hơn phía dưới (nổi khối), bo tròn kiểu pill; Pause tròn cam.
3. Thẻ Pause/Result/Settings kem viền navy, được chụp ở trạng thái đã bật lên xong (scale 1).
4. Không chữ/nút nào bị cắt hay chồng nhau ở cả 1920 và 2400 (đặc biệt `NEXT ADVENTURE`, `KEEP UNRAVELING`, `RESET PROGRESS`, `PLAY  0N`, tên level).
5. Tông màu/độ đậm chữ gần ảnh store (chữ tròn đậm trắng viền navy như logo `shot8`).
Nếu một mục không đạt: chỉnh knob ở bảng trên, chạy lại `Rebuild UI` (và `Rebuild Art` nếu đổi `CounterShadowOffset`), Bước 1 (phần check UI), Bước 2 và Bước 3–4.

- [ ] **Bước 5: Kiểm cuối**
```bash
git status --short
grep -rn "//\|/\*" Assets/JamJam/Runtime/WoolUiTheme.cs Assets/JamJam/Runtime/WoolButtonJuice.cs Assets/JamJam/Runtime/WoolPopIn.cs Assets/JamJam/Runtime/WoolLevelCard.cs Assets/JamJam/Editor/WoolUi*.cs Assets/JamJam/Editor/WoolArtChecks.Ui*.cs Assets/JamJam/Editor/WoolArtChecks.PlayUi.cs
grep -rn "StartCoroutine\|IEnumerator\|DOTween\|UniTask" Assets/JamJam/Runtime Assets/JamJam/Editor
```
Kỳ vọng: `git status` không có file bị xóa (` D`), không thay đổi ở `WoolGame.cs`, `WoolRouting.cs`, `WoolEditorChecks.cs`, `Generated/Levels`; hai `grep` rỗng. **[CONSOLE ERRORS]** không có lỗi mới.

- [ ] **Bước 6: Dừng, báo user** — gửi `docs/ui-compare-*.png` và 14 ảnh `docs/ui-*.png`, kết quả checklist, `font.txt`, danh sách knob đã chỉnh (nếu có). Không commit.

## Tóm tắt

- Font Lilita One (SIL OFL, tải bằng curl có kiểm magic `00 01 00 00`, fallback `LegacyRuntime` Bold) dùng cho mọi `Text` và số ống chỉ `TextMesh`; token màu trong `WoolUiTheme` (Runtime), 6 sprite procedural `UiPill/UiCard/UiCircle/UiCheck/UiLock/UiBar` và API style Editor `WoolUiStyle` (chữ trắng Outline 3px + Shadow (0,-4), tiêu đề hai viền, nút/thẻ nổi khối bằng con `Face`, overlay `#0A1230` α0.72, thanh tiến độ, toggle pill, badge có icon).
- `JamJam/Rebuild UI` (`WoolUiBuilder.Rebuild`) patch tại chỗ `WoolJam.unity`, `LevelCard.prefab`, `Lobby.unity` theo tên object có sẵn, idempotent, giữ chuỗi chữ/GUID/wiring; `WoolSceneBuilder`/`WoolLobbyBuilder` gọi cùng `StyleGame`/`StyleLobby`/`StyleCard` (chỉ compile). Lobby chuyển nền navy; thẻ level xanh/cam/navy theo trạng thái với ✓/ổ khóa; `Retry` lên 100px để chạm ≥ 96.
- Chuyển động bằng R3, subscribe chỉ khi animate: `WoolButtonJuice` (0.92 trong 0.06s, OutBack về 1 trong 0.18s, haptic Light theo toggle Vibration), `WoolPopIn` (0.8→1 OutBack 0.3s + overlay 0→1 trong 0.2s, bỏ qua timeScale), nhịp 1↔1.04/1.2s của thẻ current trong clock lobby sẵn có, panel kết quả hoãn 0.9s (thắng)/0.6s (thua) bằng `Observable.Timer`, hủy khi Replay/Next/Home/disable.
- Kiểm thử: `UiAssets` (RED→GREEN, kể cả nhánh fallback và áp style hai lần), `PlayUi` + `PlayResult*` (lún/bật/nhịp, trễ, Replay khi chờ), `UiGame`/`UiLobby` (font, viền chữ trắng, tương phản ≥ 4.5, nút có juice và ≥ 96×96, card có pop-in), `PlayLobby` + luồng PLAY/Level Select vào game, chuỗi `m_Text` không đổi, harness `153/4121/18/108`, `ValidateLevels`, các check art cũ, `PlayFeel`/`PlayFinish`/`PlayBlocks`, hai lần rebuild giống byte, GUID không mất, 14 ảnh + 3 ảnh ghép so với store.
