# Nối 35 ảnh UI vào Wool Jam

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `JamJam/Rebuild UI` dùng 35 ảnh của user trong `Assets/JamJam/Art/UI/` cho HUD/Pause/Result của `WoolJam.unity`, cho Home/Levels/Settings của `Lobby.unity` và prefab `LevelCard`. Thêm chuyển động sống động (tia sáng xoay, nhân vật nảy/lắc, logo thở, PLAY nhịp). Mỗi file ảnh thiếu thì riêng phần đó giữ style procedural hiện tại.

**Kiến trúc:** `WoolUiArt` (Editor) đặt import settings và 9-slice đo từ pixel thật qua `TextureImporter`, nạp sprite theo từng file (`TryLoad`, có cờ giả lập thiếu file) và có helper `Skin`/`Button`/`Round`/`Icon`/`Card`/`Picture`/`Background`. Lớp ảnh chạy **sau** style procedural: `ArtGame`, `ArtLobby` và `ArtCard` (partial của `WoolUiBuilder`) được gọi ở cuối `StyleGame`, `StyleLobby` và `StyleCard`, trước `RoundPills`. Vì vậy nhánh fallback chính là code hiện tại, và `WoolSceneBuilder`/`WoolLobbyBuilder` (vốn gọi chính các hàm `Style*` đó) ra cùng kết quả mà không phải chạy. Runtime thêm `WoolUiMotion` (R3, `UnityTimeProvider.UpdateIgnoreTimeScale`), `WoolScene.ShowProgress` (fill theo chiều rộng) và `WoolScene.ResultRays`. `WoolLevelCard` chọn sprite thẻ theo trạng thái lúc `Bind`.

**Tech Stack:** Unity 6000.3.19f1, URP 17.3, C# 9, uGUI legacy `Text` + `Image` (Sliced/Simple, `pixelsPerUnitMultiplier`, `AspectRatioFitter`), R3 1.3.1 (`Observable.Interval(TimeSpan, UnityTimeProvider.UpdateIgnoreTimeScale)`), Python+PIL (đo border, ghép ảnh so sánh).

**Spec:** `docs/superpowers/specs/2026-11-08-wool-ui-art-integration-design.md`

## Ràng buộc chung

- Plan bằng tiếng Việt; code, identifier, lệnh giữ tiếng Anh. Không comment trong code (`//`, `/* */`, `///`).
- Method trả reference dùng interface/abstract (`IReadOnlyCollection<string>`), factory/tìm kiếm dùng `void` + `out`. Timer/animation chỉ R3; UI dùng `UnityTimeProvider.UpdateIgnoreTimeScale`. Không coroutine, DOTween, UniTask.
- Không `git add`/`git commit`. Mỗi task kết thúc bằng "Dừng, báo user".
- KHÔNG chạy `WoolSceneBuilder.Build`, `WoolLobbyBuilder.Build`, `WoolEditorChecks.RunBatch`. Hai builder chỉ sửa code (thêm `WoolUiArt.Import()`) và phải compile.
- Patch chỉ bằng `JamJam/Rebuild UI` = `JamJam.Editor.WoolUiBuilder.Rebuild()` (và `JamJam.Editor.WoolArtBuilder.Rebuild()` ở bước idempotent). Trước mỗi lần rebuild: thoát Play Mode, lưu mọi scene.
- Ảnh của user: giữ GUID, chỉ đổi import settings qua `TextureImporter`, không sửa pixel (`md5sum` các `*.png` phải giữ nguyên). `Preview.png`, `README.txt`, `prompts.json`, `validation.json` không động tới.
- Không đổi chuỗi `m_Text` scene/prefab (được phép tắt `Text.enabled`), không đổi wiring nút, gameplay, level, `WoolGame.cs`, `WoolRouting.cs`, `WoolEditorChecks.cs`, `Generated/Levels/*.asset`.
- C# 9: không file-scoped namespace, không global using. Với `UnityEngine.Object` luôn so `== null`, không dùng `?.`/`??`.
- Số dòng trong header **Trước** là của file hiện tại (2026-11-08, đã kiểm từng khối). Khi làm task sau, tìm theo nội dung **Trước**.
- Lệnh (Git Bash):
  ```bash
  cd "/d/Unity Project/Jam_Jam"
  SCRATCH="C:/Users/Admin/AppData/Local/Temp/claude/D--Unity-Project-Jam-Jam/3e22d55a-6761-4000-b253-1a1ac99571b1/scratchpad"
  RUN="$SCRATCH/art-run"
  PY="$USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe"
  U="--caller plugin --skill unity-cli --no-banner"
  E(){ unity command eval $U "$1"; }
  CAP(){ unity command game_view $U --source camera --overlay true --width 1080 --height "$2" --output-path "$1"; }
  ```
  `unity recompile` (không cờ `--caller`); nếu trả HTTP 400 thì xác nhận bằng `E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'` → `False False`. Play Mode: `unity command set_run_in_background --enabled true $U`, `unity command start $U`, `unity command stop $U`; cuối task trả `set_run_in_background --enabled false`. Python luôn chạy `"$PY" -I`.
- Check `WoolArtChecks.*` ném `InvalidOperationException("ART CHECK FAILED: …")` khi sai. Kỳ vọng ghi dưới lệnh là chuỗi trả về khi đúng.
- Harness: `dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet` → giữ nguyên `PASS: 418 gameplay assertions; 16296 route assertions; 18 progress assertions; 108 feel assertions` (plan không thêm test thuần).

## Border 9-slice đo từ pixel

Đo bằng PIL (`$SCRATCH/art-plan/measure2.py`, `measure3.py`, `verify_borders.py`). Toạ độ gốc trên-trái. `spriteBorder` theo thứ tự Unity `(left, bottom, right, top)`. "Đủ cao/đủ rộng" nghĩa là cột/hàng alpha > 0.5 đã bằng cột/hàng giữa (±2 px).

| Ảnh | Kích thước | Số đo | `spriteBorder` |
|---|---|---|---|
| `Badge-Level` | 384×256 | cuộn len x25–78, y29–81; thân đủ cao x37…332, đủ rộng y80…186; lip đáy tới y235 | `(96, 72, 64, 96)` |
| `Banner-Ribbon-Green` / `-Red` | 1024×256 | đuôi + nếp gấp hết ở x213/216 (trái), bắt đầu x810/807 (phải); thân giữa cao 177/187 px không đổi | `(224, 0, 224, 0)` (dọc co giãn đều, không slice) |
| `Button-Cream/Green/Orange/Red` | 512×192 | pill đủ cao x70…440; đủ rộng y83…108 | `(88, 88, 88, 88)` |
| `Panel-Card` | 1024×1024 | cụm cuộn len + lá: trái tới x≈272, phải từ x≈752, xuống tới y≈240; góc dưới bo từ y865 (đáy 980); viền đủ cao x154…870 | `(280, 160, 280, 248)` |
| `Progress-Track` | 1024×96 | đầu bo x51/x972; đủ rộng y37…58 | `(56, 40, 56, 40)` |
| `Progress-Fill` | 1024×96 | đầu bo x58/x965; đủ rộng y36…59 | `(64, 40, 64, 40)` |
| `LevelCard-Cleared` | 1024×256 | hoa + lá trái tới x≈144; sao + lá phải từ x≈848; đường chỉ trên tới y≈75; thân y35…220 | `(160, 48, 184, 80)` |
| `LevelCard-Current` | 1024×256 | cuộn len + ren trái tới x≈176; góc phải bo từ x931; thân y57…198 | `(192, 64, 104, 64)` |
| `LevelCard-Locked` | 1024×256 | thân x99…926, y35…220, không trang trí | `(104, 48, 104, 48)` |
| Nút tròn, icon, logo, rays, nhân vật, 2 background | — | Simple + `preserveAspect` (background dùng `AspectRatioFitter`) | `0` |

`UiArt()` kiểm lại bằng chính pixel: mọi cột giữa hai border trái/phải (và mọi hàng giữa đáy/đỉnh khi có) có độ phủ alpha bằng cột/hàng tâm ±3 px. Với 4 ảnh có trang trí, tỉ lệ pixel lệch màu thân (|ΔRGB|₁/255 > 0.45) của mỗi cột/hàng đó không vượt tâm quá 0.1. Đã chạy cùng thuật toán bằng Python trên 13 ảnh: 0 cột/hàng sai. Biên tối thiểu qua được: Badge L69/R50/B67/T76, Panel B155/T232, Cleared L142/R167/B46/T75, Current L175/R92/B53/T64.

## Quyết định (spec để ngỏ hoặc phải lệch)

| Chủ đề | Quyết định |
|---|---|
| Lớp ảnh | `ArtGame`/`ArtLobby`/`ArtCard` chạy sau style procedural, trước `RoundPills`. `WoolUiArt.Skin` xóa con `Face` mà `Raised` vừa tạo, đặt sprite ảnh, `color = white`, `Sliced` nếu sprite có border (ngược lại `Simple` + `preserveAspect`). Nhờ vậy fallback đúng là code hiện tại, và trạng thái đích giống nhau mỗi lần rebuild. |
| Tone → ảnh nút | `Primary` → `Button-Green`, `Secondary` → `Button-Cream`, `Danger` → `Button-Red`, `Warm` → không có (procedural). Chữ giữ style theo tone: kem = chữ navy (`Dark`), xanh/đỏ = trắng viền. Riêng `Reset` (tone `Secondary`) dùng `Button-Red` theo spec nên đổi chữ sang `Light`. |
| Hệ số ppu | Nút, badge, track, fill, banner: `pixelsPerUnitMultiplier = sprite.rect.height / rect.height` (`FitHeight`), nên cả chiều cao co giãn đều và góc giữ tỉ lệ. `Panel-Card` dùng hằng `CardPixelsPerUnit = 1.6` (cụm trang trí ≈170×155 px trên thẻ; thẻ nhỏ nhất 760×460 vẫn đủ chỗ cho tổng border 350×255). LevelCard dùng `ArtScale = 256/170`. |
| Icon trong nút | Con `Icon` (Image Simple, `preserveAspect`, không raycast). Nút dài: cạnh `0.62·H`, tâm `x = −W/2 + H/2` (giữa đầu bo trái). `Label` co lại từ `icon + 6` tới `W/2 − 0.3·H`, chữ căn giữa vùng đó, `y = 5`. Nút tròn: icon `0.56·H` ở giữa, tắt `Label.enabled`. Khi icon mất: xóa `Icon`, `Label` về `(0, 5)` và `size − (25, 10)`, bật lại. |
| Pause | Icon chỉ hiện khi có `Button-Round-Orange` (icon trên hình tròn procedural nhìn lệch). Thiếu nút tròn thì giữ "II". |
| Settings | Có `Button-Round-Cream` → nút tròn 120×120 ở `(400, 830)`, `Icon-Settings`, ẩn chữ. Thiếu → pill 240×100 ở `(340, 830)` + `Button-Cream` (nếu có) + `Icon-Settings` + chữ. |
| Thanh tiến độ | Track 780×56, fill neo trái-giữa (`anchor = pivot = (0, 0.5)`), `anchoredPosition = (20, 0)`, cao 40. `WoolScene.ShowProgress(fill)` đặt `sizeDelta.x = clamp01(fill) · (track.width − 2·20)`. Nếu `Image.type == Filled` (fallback) thì vẫn đặt `fillAmount`. **Lệch spec:** chiều rộng đặt qua `sizeDelta` với pivot trái, không qua `anchorMax`, để giữ lề 20 px mà không thêm field serialized. Track và fill fallback **theo cặp**: thiếu một trong hai file thì cả hai procedural (fill Filled 778×24 không vừa rãnh track ảnh). |
| Result | `Banner` (680×170, `y = 150` trong `WonContent`/`LostContent`, tức ≈10 px dưới mép trên thẻ) và `Characters` (420×420, `y = 330`) là con của `WonContent`/`LostContent`, nên `ShowResult` bật/tắt chúng sẵn. Thứ tự vẽ: `Characters`(0) → `Banner`(1) → `Title` → `Subtitle`. `Title` 480×100 ở `y = 160`, `WoolUiStyle.Title(text, TextLight, size)` (trắng, viền navy 5 px + bóng). `Subtitle` về `y = −20`. Thiếu banner thì `Title` về 760×100. |
| Rays | `ResultPanel/Rays` (sibling 0, trước `Card`, 1000×1000, tâm = mép trên thẻ + 140), lưu ở trạng thái inactive. Thêm field `WoolScene.ResultRays`; `ShowResult` gọi `SetActive(status == Won)`. **Lệch spec:** thêm một field runtime mới. |
| Chuyển động | `WoolUiMotion { Kind, Amount, Period }`: `Spin` góc = `−Amount·t` (20°/s theo chiều kim đồng hồ); `Bob` lệch `y = Amount·(1 − cos 2πt/T)/2` (nảy 0→12 px lên rồi về, chu kỳ 1.6 s, **không** xuống dưới vị trí gốc để khỏi lún vào banner); `Sway` góc `Amount·sin 2πt/T` (±4°, 2 s); `Breathe` scale `1 + Amount·(1 − cos)/2` (logo 1↔1.03/2.4 s, PLAY 1↔1.05/1.2 s). `Observable.Interval(1/60 s, UnityTimeProvider.UpdateIgnoreTimeScale)`, delta lấy từ `Time.unscaledTimeAsDouble` và kẹp 0.1 s. Dispose và trả pose về gốc ở `OnDisable`. |
| PLAY nhịp | Bọc `Play` trong `LobbyPanel/PlayPulse` (RectTransform cùng anchor/size/vị trí, `Play` về `(0, 0)` local). `WoolUiMotion` nằm trên wrapper; `WoolButtonJuice` vẫn trên `Play`. Áp dụng cả khi không có ảnh (chuyển động không phụ thuộc file). `StyleLobby` tìm `Play` theo đường dẫn mới nếu wrapper có sẵn. |
| Lobby | `Background` là con sibling 0 của `LobbyPanel`/`LevelsPanel`: `AspectRatioFitter.EnvelopeParent` (tỉ lệ sprite) + `pivot (0.5, 0)` → phủ màn, giữ tỉ lệ, neo đáy (1080×1920 cắt 480 px trời, 1080×2400 vừa khít). Có background thì `Kitten.GetComponent<Image>().enabled = false` (RectTransform vẫn nhún, wiring giữ). `Logo` 700×350 ở `(0, 760)` ngay sau `Brand`, `Brand.enabled = false`, `Tagline` về `(0, 600)` (fallback `(0, 470)`). |
| Thẻ level | Thêm field `WoolLevelCard.ClearedArt/CurrentArt/LockedArt/Plain/ArtScale`. `Bind`: `art = cleared ? ClearedArt : unlocked ? CurrentArt : LockedArt`. Có ảnh: `Edge.sprite = art`, trắng, `ppu = ArtScale`, `Face.enabled = false`. Không có: `Edge.sprite = Plain` (`UiCard`), màu `CardColors`, `ppu 1`, `Face` bật. Fallback từng trạng thái. Trạng thái "mở khóa, chưa qua, không phải current" không xảy ra với `WoolProgress` nên dùng `CurrentArt`. Bố cục khi có ảnh: số `(−260, 0)` 120×110, tên `(−10, 0)` 340×90, badge `x = 250` (né cuộn len trái và sao phải). Badge dùng `Icon-Check`/`Icon-Lock`/`Icon-Play` 52×52. `StyleCard` truyền `Icon-Play` thẳng vào `Badge` của `Current` để không xóa/tạo lại `Icon` mỗi lần rebuild (fileID prefab ổn định). |
| Settings | `Panel-Card` cho `SettingsPanel/Card` và `ConfirmPanel/Card`. Mỗi hàng toggle có `Icon` 64×64 ở `x = −260`, `Label` dời `(−110, 0)` → `(−40, 0)`. `Close` `Button-Green`, `Yes` `Button-Red`, `No`/`Levels`/`Back` `Button-Cream`. |
| Import | `WoolUiArt.Import()` chỉ `SaveAndReimport` khi khác cấu hình đích (idempotent, `.meta` ổn định). Sprite Single, không mipmap, `alphaIsTransparency`, Clamp, Bilinear, max 2048 cho 2 background (1080×2400 → 922×2048, đúng spec), 1024 cho phần còn lại (không ảnh slice nào vượt 1024 nên border tính theo pixel gốc). Gọi trong `Rebuild()`, `WoolSceneBuilder.Build()`, `WoolLobbyBuilder.Build()`. |
| Giả lập thiếu file | `WoolUiArt.SimulateMissing` (static bool). Check bật nó rồi chạy `StyleGame`/`StyleLobby` trên **bản sao Canvas trong scene tạm additive**, chạy `StyleCard` trên `LoadPrefabContents`. Đóng/unload không lưu, `finally` trả cờ về `false`. Scene thật không bị dirty. |
| Check | `UiArt()` (T1: import, border đo bằng pixel, helper + fallback), `UiArtGame()`/`PlayArt()`/`PlayArtResult(bool)` (T2), `UiArtLobby()`/`PlayArtLobby()` (T3). **Lệch spec:** tách `UiArt` thành 3 hàm để mỗi task có check riêng. `ScanUi`/`ExpectPanel`/`UiGame`/`UiLobby` chấp nhận cả procedural lẫn ảnh. |
| Không dùng | `Button-Orange`, `Icon-Star`, `Icon-YarnBall`, `Lobby-Characters` vẫn được import (đúng cấu hình) nhưng không gán. |

## Review Focus

- User xóa hoặc đổi tên một PNG (ví dụ `Button-Cream.png`) rồi chạy `Rebuild UI` → riêng các nút kem về procedural, phần khác vẫn dùng ảnh, không exception. Test: nhánh `SimulateMissing` trong `UiArt()` (T1), `ExpectGameFallback` (T2), `ExpectLobbyFallback` + `StyleCard` giả lập trong `UiArtLobby()` (T3).
- `Rebuild UI` chạy hai lần, hoặc sau `Rebuild Art` → không nhân đôi `Icon`/`Banner`/`Characters`/`Rays`/`Background`/`Logo`/`PlayPulse`, file scene/prefab giống hệt byte. Test: gọi `WoolUiArt.Button` hai lần và đếm `Icon` trong `UiArt()` (T1), md5 sau hai lần rebuild (T4 Bước 2).
- Tiến độ ở 0 %, 100 % và giá trị vượt ngưỡng → fill không âm, không tràn khỏi track, mép trái đứng yên. Test: `PlayArt()` (T2) kiểm 0/0.5/1/2.
- Màn 1080×2400 so với 1080×1920 → background phủ kín (không viền navy), sàn gỗ ở đáy, HUD và nút không bị cắt. Test: `ExpectBackground` kiểm `EnvelopeParent` + pivot đáy (T3), ảnh hai kích thước (T4 Bước 3).
- Thắng → Replay → thua → về Home → vào lại → tia sáng chỉ hiện khi thắng, mọi `WoolUiMotion` dừng khi panel ẩn (không rò subscription). Test: `PlayArtResult(true)` rồi `PlayArtResult(false)` (T2), `PlayArtLobby()` kiểm pulse dừng và về scale 1 khi `ShowLevels` (T3).

## Bản đồ file

| File | Việc |
|---|---|
| `Assets/JamJam/Editor/WoolUiArt.cs` | Tạo — import, border, `TryLoad`, helper skin (T1) |
| `Assets/JamJam/Editor/WoolArtChecks.UiArt.cs` | Tạo — `UiArt()` (T1) |
| `Assets/JamJam/Editor/WoolUiBuilder.cs` | Sửa — `Import()` trong `Rebuild` (T1), gọi `ArtGame` (T2) |
| `Assets/JamJam/Runtime/WoolUiMotion.cs` | Tạo — chuyển động R3 (T2) |
| `Assets/JamJam/Runtime/WoolScene.cs` | Sửa — `ResultRays`, `ShowProgress` (T2) |
| `Assets/JamJam/Editor/WoolUiBuilder.ArtGame.cs` | Tạo — `ArtGame`, `Motion`, hằng màn chơi (T2) |
| `Assets/JamJam/Editor/WoolArtChecks.UiArtGame.cs` | Tạo — `UiArtGame()`, `PlayArt()`, `PlayArtResult(bool)` (T2) |
| `Assets/JamJam/Editor/WoolArtChecks.Ui.cs` | Sửa — chấp nhận nút/thẻ/fill/tiêu đề ảnh (T2) |
| `Assets/JamJam/Editor/WoolSceneBuilder.cs`, `WoolLobbyBuilder.cs` | Sửa — `WoolUiArt.Import()` (T2, T3) |
| `Assets/JamJam/Runtime/WoolLevelCard.cs` | Sửa — sprite theo trạng thái (T3) |
| `Assets/JamJam/Editor/WoolUiBuilder.ArtLobby.cs` | Tạo — `ArtLobby`, `ArtCard`, hằng lobby (T3) |
| `Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs` | Sửa — đường dẫn `Play`, gọi `ArtLobby`/`ArtCard`, icon `Current` (T3) |
| `Assets/JamJam/Editor/WoolArtChecks.UiArtLobby.cs` | Tạo — `UiArtLobby()`, `PlayArtLobby()` (T3) |
| `Assets/JamJam/Editor/WoolArtChecks.UiLobby.cs` | Sửa — icon badge ảnh (T3) |
| Patch tại chỗ | `Art/UI/*.png.meta` (T1), `Assets/Scenes/WoolJam.unity` (T2), `Assets/Scenes/Lobby.unity`, `Generated/Prefabs/LevelCard.prefab` (T3) |
| Ảnh | `docs/ui-art-*.png` (14), `docs/ui-art-compare.png` (T4) |

Đã xác minh mọi file "Tạo" chưa tồn tại. Đã áp toàn bộ khối **Trước→Sau** và file mới của từng task lên bản sao `Runtime/*.cs` + `Editor/*.cs` hiện tại, theo ba mốc cộng dồn (sau T1, sau T2, sau T3), rồi compile với `LangVersion 9.0`, `UNITY_EDITOR`, DLL Unity 6000.3.19f1 + `Library/ScriptAssemblies` (UGUI, R3.Unity, URP, Input System) + `R3.dll`: 0 lỗi, 0 cảnh báo ở cả ba mốc.

---

### Task 1: Import settings, border đo được, loader fallback từng file

**Files:**
- Create: `Assets/JamJam/Editor/WoolUiArt.cs`, `Assets/JamJam/Editor/WoolArtChecks.UiArt.cs`
- Modify: `Assets/JamJam/Editor/WoolUiBuilder.cs:30`
- Patch: `Assets/JamJam/Art/UI/*.png.meta` trừ `Preview.png.meta` (qua `Rebuild UI`)

**Interfaces — Consumes:** `WoolUiTone`, `WoolUiStyle.Button/LoadSprite` (có sẵn), `WoolUiTheme.EdgeDepth`, `WoolUiBuilder.CardPath`, `WoolArtChecks.Expect/Near`.

**Interfaces — Produces** (`static class WoolUiArt`, namespace `JamJam.Editor`):
- `const string Folder, PreviewName`; `const int BackgroundMaxSize 2048, SpriteMaxSize 1024`; `const float CardPixelsPerUnit 1.6, IconScale 0.62, RoundIconScale 0.56, IconGap 6, LabelTail 0.3`; `Vector2 LabelPad (25,10)`; `static bool SimulateMissing`; `string[] Names` (35), `Backgrounds`, `Decorated`; `IReadOnlyCollection<string> Sliced`.
- `string PathOf(string)`; `void Settings(string, out Vector4 border, out int maxSize)`; `void Import()`; `bool Matches(TextureImporter, Vector4, int)`; `bool TryLoad(string, out Sprite)`; `bool IsArt(Sprite)`; `bool IsArt(Sprite, string)`; `float FitHeight(Image, Sprite)`; `void Skin(Image, Sprite, float multiplier)`; `bool Button(Button, WoolUiTone, string icon)`; `bool Round(Button, string round, string icon)`; `void Icon(Button, string icon, bool round)`; `bool Card(Image)`; `void Child(Transform, string, out RectTransform, out Image)`; `void Picture(Transform, string name, string art, Vector2 position, Vector2 size, out RectTransform)` (rect `null` khi thiếu ảnh); `void Background(Transform panel, string art)`; `void Remove(Transform, string)`.
- `WoolArtChecks.UiArt()` → `"UI ART PASS / 35 images / 13 sliced"`.

- [ ] **Bước 0: Baseline** (Editor đang mở project, không Play Mode)

```bash
mkdir -p "$RUN"
grep -r "^guid:" Assets/JamJam Assets/Scenes --include=*.meta | sort > "$RUN/guids.before.txt"
md5sum Assets/JamJam/Art/UI/*.png Assets/JamJam/Art/UI/Preview.png.meta Assets/JamJam/Art/UI/README.txt Assets/JamJam/Art/UI/*.json > "$RUN/art.md5"
md5sum Assets/JamJam/Generated/Levels/*.asset Assets/JamJam/Runtime/WoolGame.cs Assets/JamJam/Runtime/WoolRouting.cs Assets/JamJam/Editor/WoolEditorChecks.cs > "$RUN/logic.md5"
md5sum Assets/Scenes/WoolJam.unity Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab > "$RUN/ui.before.md5"
for f in Assets/Scenes/WoolJam.unity Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab; do grep "m_Text:" "$f" | sort > "$RUN/strings.$(basename "$f").txt"; done
wc -l "$RUN"/strings.*.txt
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
E 'return UnityEngine.PlayerPrefs.GetInt("JamJam.Cleared", -1);'
```
Kỳ vọng: `strings` 21 dòng (WoolJam), 17 (Lobby), 5 (LevelCard); harness `PASS: 418 gameplay assertions; 16296 route assertions; 18 progress assertions; 108 feel assertions`. Ghi giá trị `JamJam.Cleared` vào `$RUN/cleared.txt` (các task sau khôi phục về giá trị này).

- [ ] **Bước 1: Tạo `Assets/JamJam/Editor/WoolUiArt.cs`**

Tạo `Assets/JamJam/Editor/WoolUiArt.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static class WoolUiArt
    {
        public const string Folder = "Assets/JamJam/Art/UI";
        public const string PreviewName = "Preview";
        public const int BackgroundMaxSize = 2048;
        public const int SpriteMaxSize = 1024;
        public const float CardPixelsPerUnit = 1.6f;
        public const float IconScale = 0.62f;
        public const float RoundIconScale = 0.56f;
        public const float IconGap = 6f;
        public const float LabelTail = 0.3f;
        public static readonly Vector2 LabelPad = new Vector2(25, 10);
        public static bool SimulateMissing;
        public static readonly string[] Names =
        {
            "Badge-Level", "Banner-Ribbon-Green", "Banner-Ribbon-Red", "Button-Cream", "Button-Green", "Button-Orange", "Button-Red",
            "Button-Round-Cream", "Button-Round-Orange", "Icon-Back", "Icon-Check", "Icon-Home", "Icon-Lock", "Icon-Music", "Icon-Pause",
            "Icon-Play", "Icon-Retry", "Icon-Settings", "Icon-Sound", "Icon-Star", "Icon-Vibration", "Icon-YarnBall", "LevelCard-Cleared",
            "LevelCard-Current", "LevelCard-Locked", "Levels-Background", "Lobby-Background", "Lobby-Characters", "Logo-WoolJam",
            "Panel-Card", "Progress-Fill", "Progress-Track", "Result-Lose-Kitten", "Result-Rays", "Result-Win-Characters"
        };
        public static readonly string[] Backgrounds = { "Lobby-Background", "Levels-Background" };
        public static readonly string[] Decorated = { "Badge-Level", "Panel-Card", "LevelCard-Cleared", "LevelCard-Current" };
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "Badge-Level", new Vector4(96, 72, 64, 96) },
            { "Banner-Ribbon-Green", new Vector4(224, 0, 224, 0) },
            { "Banner-Ribbon-Red", new Vector4(224, 0, 224, 0) },
            { "Button-Cream", new Vector4(88, 88, 88, 88) },
            { "Button-Green", new Vector4(88, 88, 88, 88) },
            { "Button-Orange", new Vector4(88, 88, 88, 88) },
            { "Button-Red", new Vector4(88, 88, 88, 88) },
            { "Panel-Card", new Vector4(280, 160, 280, 248) },
            { "Progress-Track", new Vector4(56, 40, 56, 40) },
            { "Progress-Fill", new Vector4(64, 40, 64, 40) },
            { "LevelCard-Cleared", new Vector4(160, 48, 184, 80) },
            { "LevelCard-Current", new Vector4(192, 64, 104, 64) },
            { "LevelCard-Locked", new Vector4(104, 48, 104, 48) }
        };

        public static IReadOnlyCollection<string> Sliced => Borders.Keys;

        public static string PathOf(string name)
        {
            return Folder + "/" + name + ".png";
        }

        public static void Settings(string name, out Vector4 border, out int maxSize)
        {
            if (!Borders.TryGetValue(name, out border)) border = Vector4.zero;
            maxSize = Array.IndexOf(Backgrounds, name) >= 0 ? BackgroundMaxSize : SpriteMaxSize;
        }

        public static void Import()
        {
            foreach (var name in Names)
            {
                var path = PathOf(name);
                if (!File.Exists(path)) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                Settings(name, out var border, out var maxSize);
                if (Matches(importer, border, maxSize)) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = maxSize;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        public static bool Matches(TextureImporter importer, Vector4 border, int maxSize)
        {
            return importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled
                && importer.alphaIsTransparency && importer.wrapMode == TextureWrapMode.Clamp && importer.filterMode == FilterMode.Bilinear
                && importer.maxTextureSize == maxSize && importer.spriteBorder == border;
        }

        public static bool TryLoad(string name, out Sprite sprite)
        {
            sprite = null;
            if (SimulateMissing) return false;
            var path = PathOf(name);
            if (!File.Exists(path)) return false;
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null;
        }

        public static bool IsArt(Sprite sprite)
        {
            return sprite != null && AssetDatabase.GetAssetPath(sprite).StartsWith(Folder + "/", StringComparison.Ordinal);
        }

        public static bool IsArt(Sprite sprite, string name)
        {
            return IsArt(sprite) && AssetDatabase.GetAssetPath(sprite) == PathOf(name);
        }

        public static float FitHeight(Image image, Sprite art)
        {
            var height = image.rectTransform.rect.height;
            return height > 0 ? Mathf.Max(0.01f, art.rect.height / height) : 1f;
        }

        public static void Skin(Image image, Sprite art, float multiplier)
        {
            var face = image.transform.Find("Face");
            if (face != null) UnityEngine.Object.DestroyImmediate(face.gameObject);
            var sliced = art.border != Vector4.zero;
            image.sprite = art;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.fillCenter = true;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = multiplier;
        }

        public static bool Button(Button button, WoolUiTone tone, string icon)
        {
            var name = tone == WoolUiTone.Primary ? "Button-Green" : tone == WoolUiTone.Danger ? "Button-Red" : tone == WoolUiTone.Secondary ? "Button-Cream" : null;
            var host = button.GetComponent<Image>();
            Sprite art = null;
            var skinned = name != null && TryLoad(name, out art);
            if (skinned) Skin(host, art, FitHeight(host, art));
            Icon(button, icon, false);
            return skinned;
        }

        public static bool Round(Button button, string round, string icon)
        {
            var host = button.GetComponent<Image>();
            var skinned = TryLoad(round, out var art);
            if (skinned) Skin(host, art, 1);
            Icon(button, icon, skinned);
            return skinned;
        }

        public static void Icon(Button button, string icon, bool round)
        {
            var rect = (RectTransform)button.transform;
            var labelNode = rect.Find("Label");
            var label = labelNode != null ? labelNode.GetComponent<Text>() : null;
            var node = rect.Find("Icon");
            var size = rect.sizeDelta;
            var lift = WoolUiTheme.EdgeDepth * 0.5f;
            if (icon == null || !TryLoad(icon, out var sprite))
            {
                if (node == null) return;
                UnityEngine.Object.DestroyImmediate(node.gameObject);
                if (label == null) return;
                label.enabled = true;
                label.rectTransform.anchoredPosition = new Vector2(0, lift);
                label.rectTransform.sizeDelta = size - LabelPad;
                return;
            }
            Child(rect, "Icon", out var iconRect, out var image);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            if (round)
            {
                iconRect.anchoredPosition = new Vector2(0, lift);
                iconRect.sizeDelta = Vector2.one * size.y * RoundIconScale;
                if (label != null) label.enabled = false;
                return;
            }
            var side = size.y * IconScale;
            var center = -size.x * 0.5f + size.y * 0.5f;
            iconRect.anchoredPosition = new Vector2(center, lift);
            iconRect.sizeDelta = Vector2.one * side;
            if (label == null) return;
            label.enabled = true;
            var left = center + side * 0.5f + IconGap;
            var right = size.x * 0.5f - size.y * LabelTail;
            label.rectTransform.anchoredPosition = new Vector2((left + right) * 0.5f, lift);
            label.rectTransform.sizeDelta = new Vector2(right - left, size.y - LabelPad.y);
        }

        public static bool Card(Image card)
        {
            if (!TryLoad("Panel-Card", out var art)) return false;
            Skin(card, art, CardPixelsPerUnit);
            return true;
        }

        public static void Child(Transform parent, string name, out RectTransform rect, out Image image)
        {
            var node = parent.Find(name);
            if (node == null)
            {
                rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                rect.SetParent(parent, false);
            }
            else rect = (RectTransform)node;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            image = rect.GetComponent<Image>();
            if (image == null) image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
        }

        public static void Picture(Transform parent, string name, string art, Vector2 position, Vector2 size, out RectTransform rect)
        {
            rect = null;
            if (!TryLoad(art, out var sprite))
            {
                Remove(parent, name);
                return;
            }
            Child(parent, name, out rect, out var image);
            Skin(image, sprite, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Background(Transform panel, string art)
        {
            if (!TryLoad(art, out var sprite))
            {
                Remove(panel, "Background");
                return;
            }
            Child(panel, "Background", out var rect, out var image);
            rect.SetSiblingIndex(0);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            var fitter = rect.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        public static void Remove(Transform parent, string name)
        {
            var node = parent.Find(name);
            if (node != null) UnityEngine.Object.DestroyImmediate(node.gameObject);
        }
    }
}
```

- [ ] **Bước 2: Tạo check `Assets/JamJam/Editor/WoolArtChecks.UiArt.cs`**

Probe dùng badge `Current` trong nội dung prefab `LevelCard` (có `Label`, không có `Icon`), nạp bằng `LoadPrefabContents` rồi unload, không lưu.

Tạo `Assets/JamJam/Editor/WoolArtChecks.UiArt.cs`:
```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public const int SliceCoverTolerance = 3;
        public const float SliceColorCut = 0.45f;
        public const float SliceDecorTolerance = 0.1f;

        public static string UiArt()
        {
            var used = 0;
            var sliced = 0;
            foreach (var name in WoolUiArt.Names)
            {
                var path = WoolUiArt.PathOf(name);
                if (!File.Exists(path)) continue;
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                WoolUiArt.Settings(name, out var border, out var maxSize);
                Expect(WoolUiArt.Matches(importer, border, maxSize), name + " imports as a clamped single sprite without mipmaps at max " + maxSize + " with border " + border);
                Expect(WoolUiArt.TryLoad(name, out var sprite) && sprite.border == border && WoolUiArt.IsArt(sprite, name), name + " loads as a sprite with its border");
                used++;
                if (border == Vector4.zero) continue;
                ExpectSlices(name, border, Array.IndexOf(WoolUiArt.Decorated, name) >= 0);
                sliced++;
            }
            var preview = (TextureImporter)AssetImporter.GetAtPath(WoolUiArt.PathOf(WoolUiArt.PreviewName));
            Expect(preview == null || preview.textureType != TextureImporterType.Sprite, "Preview.png keeps its import settings");
            foreach (var name in WoolUiArt.Sliced)
                if (File.Exists(WoolUiArt.PathOf(name))) Expect(WoolUiArt.TryLoad(name, out var art) && art.border != Vector4.zero, name + " has a 9-slice border");
            var root = PrefabUtility.LoadPrefabContents(WoolUiBuilder.CardPath);
            try
            {
                WoolUiStyle.LoadSprite("UiPill", out var pill);
                var view = root.GetComponent<WoolLevelCard>();
                var badge = view.Current.GetComponent<Image>();
                var probe = view.Current.AddComponent<Button>();
                var rect = (RectTransform)probe.transform;
                var label = rect.Find("Label").GetComponent<Text>();
                WoolUiArt.SimulateMissing = true;
                Expect(!WoolUiArt.TryLoad("Button-Cream", out _), "SimulateMissing hides every art file");
                WoolUiStyle.Button(probe, WoolUiTone.Secondary, pill, 30);
                Expect(!WoolUiArt.Button(probe, WoolUiTone.Secondary, "Icon-Retry") && rect.Find("Face") != null && rect.Find("Icon") == null && badge.sprite == pill, "Without art the button stays procedural");
                WoolUiArt.SimulateMissing = false;
                if (WoolUiArt.TryLoad("Button-Cream", out var cream) && WoolUiArt.TryLoad("Icon-Retry", out var retry))
                {
                    WoolUiArt.Button(probe, WoolUiTone.Secondary, "Icon-Retry");
                    WoolUiArt.Button(probe, WoolUiTone.Secondary, "Icon-Retry");
                    var icon = (RectTransform)rect.Find("Icon");
                    var icons = 0;
                    foreach (Transform child in rect) if (child.name == "Icon") icons++;
                    Expect(badge.sprite == cream && badge.type == Image.Type.Sliced && Near(badge.color, Color.white) && rect.Find("Face") == null, "Art button uses Button-Cream sliced in white without a Face");
                    Expect(Mathf.Abs(badge.pixelsPerUnitMultiplier - cream.rect.height / rect.rect.height) < 1e-4f, "Art button scales its corners to its height");
                    Expect(icons == 1 && icon.GetComponent<Image>().sprite == retry && !icon.GetComponent<Image>().raycastTarget && icon.GetComponent<Image>().preserveAspect, "Art button has one Icon-Retry child");
                    Expect(icon.anchoredPosition.x + icon.sizeDelta.x * 0.5f <= label.rectTransform.anchoredPosition.x - label.rectTransform.sizeDelta.x * 0.5f + 0.01f && label.enabled, "Icon sits left of the label");
                    WoolUiArt.SimulateMissing = true;
                    WoolUiStyle.Button(probe, WoolUiTone.Secondary, pill, 30);
                    WoolUiArt.Button(probe, WoolUiTone.Secondary, "Icon-Retry");
                    Expect(rect.Find("Icon") == null && rect.Find("Face") != null && label.rectTransform.anchoredPosition == new Vector2(0, WoolUiTheme.EdgeDepth * 0.5f) && label.rectTransform.sizeDelta == rect.sizeDelta - WoolUiArt.LabelPad, "Losing the art restores the procedural button and label");
                    WoolUiArt.SimulateMissing = false;
                }
                if (WoolUiArt.TryLoad("Button-Round-Orange", out var round) && WoolUiArt.TryLoad("Icon-Pause", out var pause))
                {
                    WoolUiArt.Round(probe, "Button-Round-Orange", "Icon-Pause");
                    var icon = (RectTransform)rect.Find("Icon");
                    Expect(badge.sprite == round && badge.type == Image.Type.Simple && badge.preserveAspect && !label.enabled && icon.anchoredPosition.x == 0 && icon.GetComponent<Image>().sprite == pause, "Round art button centres its icon and hides the label");
                }
                if (WoolUiArt.TryLoad("Panel-Card", out var panel))
                {
                    var host = view.Content.GetComponent<Image>();
                    Expect(WoolUiArt.Card(host) && host.sprite == panel && host.transform.Find("Face") == null && Mathf.Abs(host.pixelsPerUnitMultiplier - WoolUiArt.CardPixelsPerUnit) < 1e-4f, "Panel-Card replaces the raised card");
                }
            }
            finally
            {
                WoolUiArt.SimulateMissing = false;
                PrefabUtility.UnloadPrefabContents(root);
            }
            return "UI ART PASS / " + used + " images / " + sliced + " sliced";
        }

        static void ExpectSlices(string name, Vector4 border, bool decorated)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(WoolUiArt.PathOf(name)));
            var width = image.width;
            var height = image.height;
            var pixels = image.GetPixels32();
            UnityEngine.Object.DestroyImmediate(image);
            var cx = width / 2;
            var cy = height / 2;
            var body = pixels[cy * width + cx];
            var cols = new int[width];
            var rows = new int[height];
            var farCols = new int[width];
            var farRows = new int[height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var p = pixels[y * width + x];
                    if (p.a <= 127) continue;
                    cols[x]++;
                    rows[y]++;
                    if ((Mathf.Abs(p.r - body.r) + Mathf.Abs(p.g - body.g) + Mathf.Abs(p.b - body.b)) / 255f <= SliceColorCut) continue;
                    farCols[x]++;
                    farRows[y]++;
                }
            for (var x = (int)border.x; x < width - (int)border.z; x++)
            {
                Expect(Mathf.Abs(cols[x] - cols[cx]) <= SliceCoverTolerance, name + " column " + x + " between the borders matches the centre silhouette");
                Expect(!decorated || farCols[x] / (float)height <= farCols[cx] / (float)height + SliceDecorTolerance, name + " column " + x + " between the borders has no decoration");
            }
            if (border.y + border.w == 0) return;
            for (var y = (int)border.y; y < height - (int)border.w; y++)
            {
                Expect(Mathf.Abs(rows[y] - rows[cy]) <= SliceCoverTolerance, name + " row " + y + " between the borders matches the centre silhouette");
                Expect(!decorated || farRows[y] / (float)width <= farRows[cy] / (float)width + SliceDecorTolerance, name + " row " + y + " between the borders has no decoration");
            }
        }
    }
}
```

- [ ] **Bước 3: Compile**

```bash
unity recompile
E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'
```
Kỳ vọng `False False`.

- [ ] **Bước 4: RED**

```bash
E 'return JamJam.Editor.WoolArtChecks.UiArt();'
```
Kỳ vọng FAIL bắt đầu bằng `ART CHECK FAILED: Badge-Level imports as a clamped single sprite without mipmaps at max 1024 with border` (ảnh đang là Default texture có mipmap).

- [ ] **Bước 5: `Rebuild()` gọi `WoolUiArt.Import()`**

**Trước**
```30:30:Assets/JamJam/Editor/WoolUiBuilder.cs
            WoolUiStyle.BuildSprites();
```

**Sau**
```csharp
            WoolUiStyle.BuildSprites();
            WoolUiArt.Import();
```

- [ ] **Bước 6: Compile + Rebuild UI** (lưu mọi scene, không Play Mode)

```bash
unity recompile
E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'
E 'JamJam.Editor.WoolUiBuilder.Rebuild(); return "rebuilt";'
```
Kỳ vọng `False False`, `rebuilt`.

- [ ] **Bước 7: GREEN + không hồi quy**

```bash
E 'return JamJam.Editor.WoolArtChecks.UiArt();'
E 'return JamJam.Editor.WoolArtChecks.UiAssets() + " | " + JamJam.Editor.WoolArtChecks.UiGame() + " | " + JamJam.Editor.WoolArtChecks.UiLobby();'
md5sum -c "$RUN/art.md5"
grep -r "^guid:" Assets/JamJam Assets/Scenes --include=*.meta | sort > "$RUN/guids.t1.txt"; comm -23 "$RUN/guids.before.txt" "$RUN/guids.t1.txt"
grep -c "textureType: 8" Assets/JamJam/Art/UI/*.png.meta | grep -v ":1" 
md5sum -c "$RUN/ui.before.md5"
```
Kỳ vọng: `UI ART PASS / 35 images / 13 sliced`; ba chuỗi `UI … PASS …`; mọi dòng `art.md5` là `OK` (pixel và `Preview.png.meta` không đổi); `comm` rỗng (không mất GUID); `grep -c … | grep -v ":1"` chỉ in `Assets/JamJam/Art/UI/Preview.png.meta:0`. `ui.before.md5` nên `OK` (Task 1 chưa gán ảnh vào scene). Nếu `FAILED`, chạy `git diff --stat` cho ba file: chấp nhận khi diff chỉ là giá trị float do lần rebuild trước, ghi vào báo cáo.

- [ ] **Bước 8: Kiểm tra code** — `grep -n "//\|/\*" Assets/JamJam/Editor/WoolUiArt.cs Assets/JamJam/Editor/WoolArtChecks.UiArt.cs` → rỗng.

- [ ] **Bước 9: Dừng, báo user.**

---

### Task 2: Màn chơi — HUD, Pause, Result, chuyển động

**Files:**
- Create: `Assets/JamJam/Runtime/WoolUiMotion.cs`, `Assets/JamJam/Editor/WoolUiBuilder.ArtGame.cs`, `Assets/JamJam/Editor/WoolArtChecks.UiArtGame.cs`
- Modify: `Assets/JamJam/Runtime/WoolScene.cs:28, 247-248, 261`; `Assets/JamJam/Editor/WoolUiBuilder.cs:89-90`; `Assets/JamJam/Editor/WoolArtChecks.Ui.cs:27-30, 77, 104`; `Assets/JamJam/Editor/WoolSceneBuilder.cs:34`
- Patch: `Assets/Scenes/WoolJam.unity`

**Interfaces — Consumes:** toàn bộ `WoolUiArt` (Task 1); `WoolUiBuilder.Get/StyleGame/FillSize/GameScene` (có sẵn); `WoolArtChecks.OpenForCheck/Near/Expect`; `WoolArtChecks.PlayResultArm/Early/Late/Quiet`.

**Interfaces — Produces:**
- `enum WoolUiMotionKind { Spin, Bob, Sway, Breathe }`; `sealed class WoolUiMotion : MonoBehaviour` — field `Kind`, `float Amount`, `float Period`; `bool Running`; `float Elapsed`; `void Step(float delta)`; `static void Pose(WoolUiMotionKind, float amount, float period, float time, out Vector2 offset, out float angle, out float scale)`.
- `WoolScene.ResultRays` (`GameObject`); `void WoolScene.ShowProgress(float fill)`.
- `WoolUiBuilder` (partial): `Vector2 TrackArtSize (780,56), BannerSize (680,170), BannerTitleSize (480,100), ResultTitleSize (760,100)`; `const float FillArtHeight 40, FillInset 20, BannerY 150, BannerTitleLift 10, ResultSubtitleY -20, CharactersSize 420, CharactersY 330, RaysSize 1000, RaysLift 140, RaysSpeed 20, BobHeight 12, BobPeriod 1.6, SwayAngle 4, SwayPeriod 2`; `static void Motion(GameObject, WoolUiMotionKind, float amount, float period)` (private, Task 3 dùng lại).
- `WoolArtChecks.UiArtGame()` → `"UI ART GAME PASS / 13 skinned"`; `PlayArt()` → `"PLAY ART PASS / width 740 / motion"`; `PlayArtResult(bool win)` → `"PLAY ART RESULT PASS / Won|Lost"`. Helper dùng lại ở Task 3: `ExpectSkin(Transform, string path, string art)` → `int`, `ExpectIcon(Transform, string path, string icon, bool round)`.

- [ ] **Bước 1: Tạo `Assets/JamJam/Runtime/WoolUiMotion.cs`**

Tạo `Assets/JamJam/Runtime/WoolUiMotion.cs`:
```csharp
using System;
using R3;
using UnityEngine;

namespace JamJam
{
    public enum WoolUiMotionKind { Spin, Bob, Sway, Breathe }

    public sealed class WoolUiMotion : MonoBehaviour
    {
        public WoolUiMotionKind Kind;
        public float Amount;
        public float Period = 1;
        IDisposable clock;
        RectTransform rect;
        Vector2 rest;
        double last;

        public bool Running => clock != null;
        public float Elapsed { get; private set; }

        void OnEnable()
        {
            rect = (RectTransform)transform;
            rest = rect.anchoredPosition;
            Elapsed = 0;
            Apply();
            last = Time.unscaledTimeAsDouble;
            clock?.Dispose();
            clock = Observable.Interval(TimeSpan.FromSeconds(1d / 60d), UnityTimeProvider.UpdateIgnoreTimeScale).Subscribe(_ => Tick());
        }

        void Tick()
        {
            var now = Time.unscaledTimeAsDouble;
            Step(Mathf.Min((float)(now - last), 0.1f));
            last = now;
        }

        public void Step(float delta)
        {
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            Elapsed += delta;
            Apply();
        }

        void Apply()
        {
            Pose(Kind, Amount, Period, Elapsed, out var offset, out var angle, out var scale);
            rect.anchoredPosition = rest + offset;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            rect.localScale = Vector3.one * scale;
        }

        void OnDisable()
        {
            clock?.Dispose();
            clock = null;
            Elapsed = 0;
            if (rect == null) return;
            rect.anchoredPosition = rest;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        public static void Pose(WoolUiMotionKind kind, float amount, float period, float time, out Vector2 offset, out float angle, out float scale)
        {
            offset = Vector2.zero;
            angle = 0;
            scale = 1;
            var phase = period > 0 ? Mathf.Repeat(time, period) / period : 0;
            var rise = 0.5f * (1 - Mathf.Cos(phase * Mathf.PI * 2));
            switch (kind)
            {
                case WoolUiMotionKind.Spin: angle = -Mathf.Repeat(amount * time, 360); return;
                case WoolUiMotionKind.Bob: offset = new Vector2(0, amount * rise); return;
                case WoolUiMotionKind.Sway: angle = amount * Mathf.Sin(phase * Mathf.PI * 2); return;
                default: scale = 1 + amount * rise; return;
            }
        }
    }
}
```

- [ ] **Bước 2: `WoolScene` — field `ResultRays`**

**Trước**
```28:28:Assets/JamJam/Runtime/WoolScene.cs
        public Button[] HomeButtons;
```

**Sau**
```csharp
        public Button[] HomeButtons;
        public GameObject ResultRays;
```

- [ ] **Bước 3: `WoolScene` — rays theo kết quả + `ShowProgress`**

**Trước**
```247:248:Assets/JamJam/Runtime/WoolScene.cs
            NextButton.gameObject.SetActive(status == RoundStatus.Won);
        }
```

**Sau**
```csharp
            NextButton.gameObject.SetActive(status == RoundStatus.Won);
            if (ResultRays != null) ResultRays.SetActive(status == RoundStatus.Won);
        }

        public void ShowProgress(float fill)
        {
            fill = Mathf.Clamp01(fill);
            if (ProgressFill.type == Image.Type.Filled)
            {
                ProgressFill.fillAmount = fill;
                return;
            }
            var rect = ProgressFill.rectTransform;
            var track = rect.parent as RectTransform;
            var span = track != null ? track.rect.width - 2 * rect.anchoredPosition.x : 0;
            rect.sizeDelta = new Vector2(Mathf.Max(0, span) * fill, rect.sizeDelta.y);
        }
```

- [ ] **Bước 4: `WoolScene.RenderFrame` dùng `ShowProgress`**

**Trước**
```261:261:Assets/JamJam/Runtime/WoolScene.cs
            ProgressFill.fillAmount = fill;
```

**Sau**
```csharp
            ShowProgress(fill);
```

- [ ] **Bước 5: Tạo `Assets/JamJam/Editor/WoolUiBuilder.ArtGame.cs`** (chưa được gọi; Bước 9 mới nối vào `StyleGame`)

Tạo `Assets/JamJam/Editor/WoolUiBuilder.ArtGame.cs`:
```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolUiBuilder
    {
        public static readonly Vector2 TrackArtSize = new Vector2(780, 56);
        public const float FillArtHeight = 40f;
        public const float FillInset = 20f;
        public static readonly Vector2 BannerSize = new Vector2(680, 170);
        public const float BannerY = 150f;
        public static readonly Vector2 BannerTitleSize = new Vector2(480, 100);
        public static readonly Vector2 ResultTitleSize = new Vector2(760, 100);
        public const float BannerTitleLift = 10f;
        public const float ResultSubtitleY = -20f;
        public const float CharactersSize = 420f;
        public const float CharactersY = 330f;
        public const float RaysSize = 1000f;
        public const float RaysLift = 140f;
        public const float RaysSpeed = 20f;
        public const float BobHeight = 12f;
        public const float BobPeriod = 1.6f;
        public const float SwayAngle = 4f;
        public const float SwayPeriod = 2f;

        static void ArtGame(Transform canvas)
        {
            Get(canvas, "LevelBadge", out Image badge);
            if (WoolUiArt.TryLoad("Badge-Level", out var badgeArt)) WoolUiArt.Skin(badge, badgeArt, WoolUiArt.FitHeight(badge, badgeArt));
            Get(canvas, "Pause", out Button pause); WoolUiArt.Round(pause, "Button-Round-Orange", "Icon-Pause");
            Get(canvas, "ProgressTrack", out Image track); Get(canvas, "ProgressTrack/ProgressFill", out Image fill);
            ArtBar(track, fill);
            Get(canvas, "Retry", out Button retry); WoolUiArt.Button(retry, WoolUiTone.Secondary, "Icon-Retry");
            Get(canvas, "PausePanel/Card", out Image pauseCard); ArtPanel(pauseCard);
            Get(pauseCard.transform, "Resume", out Button resume); WoolUiArt.Button(resume, WoolUiTone.Primary, "Icon-Play");
            Get(pauseCard.transform, "Home", out Button pauseHome); WoolUiArt.Button(pauseHome, WoolUiTone.Secondary, "Icon-Home");
            Get(canvas, "ResultPanel/Card", out Image resultCard); ArtPanel(resultCard);
            Get(resultCard.transform, "Next", out Button next); WoolUiArt.Button(next, WoolUiTone.Primary, "Icon-Play");
            Get(resultCard.transform, "PlayAgain", out Button again); WoolUiArt.Button(again, WoolUiTone.Secondary, "Icon-Retry");
            Get(resultCard.transform, "Home", out Button resultHome); WoolUiArt.Button(resultHome, WoolUiTone.Secondary, "Icon-Home");
            ArtResult(resultCard.transform, "WonContent", "Banner-Ribbon-Green", "Result-Win-Characters", WoolUiMotionKind.Bob, BobHeight, BobPeriod);
            ArtResult(resultCard.transform, "LostContent", "Banner-Ribbon-Red", "Result-Lose-Kitten", WoolUiMotionKind.Sway, SwayAngle, SwayPeriod);
            ArtRays(canvas, resultCard.rectTransform);
        }

        static void ArtPanel(Image card)
        {
            if (!WoolUiArt.Card(card)) card.pixelsPerUnitMultiplier = 1;
        }

        static void ArtBar(Image track, Image fill)
        {
            var rect = fill.rectTransform;
            if (WoolUiArt.TryLoad("Progress-Track", out var trackArt) && WoolUiArt.TryLoad("Progress-Fill", out var fillArt))
            {
                track.rectTransform.sizeDelta = TrackArtSize;
                WoolUiArt.Skin(track, trackArt, WoolUiArt.FitHeight(track, trackArt));
                rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
                rect.pivot = new Vector2(0, 0.5f);
                rect.anchoredPosition = new Vector2(FillInset, 0);
                rect.sizeDelta = new Vector2(0, FillArtHeight);
                WoolUiArt.Skin(fill, fillArt, WoolUiArt.FitHeight(fill, fillArt));
                return;
            }
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        static void ArtResult(Transform card, string content, string banner, string figure, WoolUiMotionKind kind, float amount, float period)
        {
            var root = card.Find(content);
            Get(root, "Title", out Text title);
            Get(root, "Subtitle", out Text subtitle);
            WoolUiArt.Picture(root, "Characters", figure, new Vector2(0, CharactersY), Vector2.one * CharactersSize, out var figureRect);
            if (figureRect != null)
            {
                figureRect.SetSiblingIndex(0);
                Motion(figureRect.gameObject, kind, amount, period);
            }
            WoolUiArt.Picture(root, "Banner", banner, new Vector2(0, BannerY), BannerSize, out var bannerRect);
            if (bannerRect == null)
            {
                title.rectTransform.sizeDelta = ResultTitleSize;
                return;
            }
            bannerRect.SetSiblingIndex(figureRect != null ? 1 : 0);
            var image = bannerRect.GetComponent<Image>();
            image.pixelsPerUnitMultiplier = WoolUiArt.FitHeight(image, image.sprite);
            WoolUiStyle.Title(title, WoolUiTheme.TextLight, title.fontSize);
            title.rectTransform.anchoredPosition = new Vector2(0, BannerY + BannerTitleLift);
            title.rectTransform.sizeDelta = BannerTitleSize;
            subtitle.rectTransform.anchoredPosition = new Vector2(0, ResultSubtitleY);
        }

        static void ArtRays(Transform canvas, RectTransform card)
        {
            var top = card.anchoredPosition.y + card.rect.height * 0.5f;
            WoolUiArt.Picture(card.parent, "Rays", "Result-Rays", new Vector2(0, top + RaysLift), Vector2.one * RaysSize, out var rays);
            if (rays != null)
            {
                rays.SetSiblingIndex(0);
                Motion(rays.gameObject, WoolUiMotionKind.Spin, RaysSpeed, 1);
                rays.gameObject.SetActive(false);
            }
            FindGame(canvas, out var game);
            if (game == null) return;
            game.ResultRays = rays != null ? rays.gameObject : null;
            EditorUtility.SetDirty(game);
        }

        static void Motion(GameObject target, WoolUiMotionKind kind, float amount, float period)
        {
            var motion = target.GetComponent<WoolUiMotion>();
            if (motion == null) motion = target.AddComponent<WoolUiMotion>();
            motion.Kind = kind;
            motion.Amount = amount;
            motion.Period = period;
            EditorUtility.SetDirty(motion);
        }

        static void FindGame(Transform canvas, out WoolScene game)
        {
            game = null;
            foreach (var root in canvas.gameObject.scene.GetRootGameObjects())
            {
                game = root.GetComponentInChildren<WoolScene>(true);
                if (game != null) return;
            }
        }
    }
}
```

- [ ] **Bước 6: Tạo check `Assets/JamJam/Editor/WoolArtChecks.UiArtGame.cs`**

`ExpectGameFallback` nhân bản Canvas thật vào scene tạm additive (scene tạm là active scene nên `Instantiate` không làm dirty `WoolJam.unity`). Nó chạy `StyleGame` với `SimulateMissing = true` rồi `false`, sau đó đóng scene tạm không lưu. `FindGame` chỉ tìm `WoolScene` trong scene của Canvas, nên bản sao không đụng `ResultRays` của scene thật.

Tạo `Assets/JamJam/Editor/WoolArtChecks.UiArtGame.cs`:
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
        public static string UiArtGame()
        {
            Expect(!EditorApplication.isPlaying, "Exit Play Mode first");
            OpenForCheck(WoolUiBuilder.GameScene, out var scene, out var opened);
            var skinned = 0;
            try
            {
                var game = Object.FindFirstObjectByType<WoolScene>();
                Expect(game != null, "WoolScene exists");
                var canvas = game.PausePanel.transform.parent;
                skinned += ExpectSkin(canvas, "LevelBadge", "Badge-Level");
                var round = ExpectSkin(canvas, "Pause", "Button-Round-Orange");
                skinned += round;
                ExpectIcon(canvas, "Pause", round == 1 ? "Icon-Pause" : null, true);
                skinned += ExpectSkin(canvas, "Retry", "Button-Cream");
                ExpectIcon(canvas, "Retry", "Icon-Retry", false);
                skinned += ExpectSkin(canvas, "PausePanel/Card", "Panel-Card");
                skinned += ExpectSkin(canvas, "PausePanel/Card/Resume", "Button-Green");
                ExpectIcon(canvas, "PausePanel/Card/Resume", "Icon-Play", false);
                skinned += ExpectSkin(canvas, "PausePanel/Card/Home", "Button-Cream");
                ExpectIcon(canvas, "PausePanel/Card/Home", "Icon-Home", false);
                skinned += ExpectSkin(canvas, "ResultPanel/Card", "Panel-Card");
                skinned += ExpectSkin(canvas, "ResultPanel/Card/Next", "Button-Green");
                ExpectIcon(canvas, "ResultPanel/Card/Next", "Icon-Play", false);
                skinned += ExpectSkin(canvas, "ResultPanel/Card/PlayAgain", "Button-Cream");
                ExpectIcon(canvas, "ResultPanel/Card/PlayAgain", "Icon-Retry", false);
                skinned += ExpectSkin(canvas, "ResultPanel/Card/Home", "Button-Cream");
                ExpectIcon(canvas, "ResultPanel/Card/Home", "Icon-Home", false);
                var bar = WoolUiArt.TryLoad("Progress-Track", out _) && WoolUiArt.TryLoad("Progress-Fill", out _);
                if (bar)
                {
                    skinned += ExpectSkin(canvas, "ProgressTrack", "Progress-Track") + ExpectSkin(canvas, "ProgressTrack/ProgressFill", "Progress-Fill");
                    var rect = game.ProgressFill.rectTransform;
                    Expect(((RectTransform)rect.parent).sizeDelta == WoolUiBuilder.TrackArtSize && rect.anchorMin == new Vector2(0, 0.5f) && rect.anchorMax == new Vector2(0, 0.5f) && rect.pivot == new Vector2(0, 0.5f) && rect.anchoredPosition == new Vector2(WoolUiBuilder.FillInset, 0) && rect.sizeDelta.y == WoolUiBuilder.FillArtHeight, "Progress fill grows from the left of the track");
                }
                else Expect(game.ProgressFill.type == Image.Type.Filled && game.ProgressFill.rectTransform.pivot == new Vector2(0.5f, 0.5f), "Progress fill stays the procedural Filled bar");
                ExpectResult(game.WonContent.transform, "Banner-Ribbon-Green", "Result-Win-Characters", WoolUiMotionKind.Bob, WoolUiBuilder.BobHeight, WoolUiBuilder.BobPeriod);
                ExpectResult(game.LostContent.transform, "Banner-Ribbon-Red", "Result-Lose-Kitten", WoolUiMotionKind.Sway, WoolUiBuilder.SwayAngle, WoolUiBuilder.SwayPeriod);
                if (WoolUiArt.TryLoad("Result-Rays", out var rays))
                {
                    var node = game.ResultRays;
                    Expect(node != null && node.transform.parent == game.ResultPanel.transform && node.transform.GetSiblingIndex() == 0 && !node.activeSelf && node.GetComponent<Image>().sprite == rays, "Rays sit behind the result card, hidden until a win");
                    var spin = node.GetComponent<WoolUiMotion>();
                    Expect(spin != null && spin.Kind == WoolUiMotionKind.Spin && spin.Amount == WoolUiBuilder.RaysSpeed, "Rays spin at 20 degrees per second");
                    skinned++;
                }
                else Expect(game.ResultRays == null && game.ResultPanel.transform.Find("Rays") == null, "No rays without Result-Rays");
                ExpectGameFallback(canvas);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            return "UI ART GAME PASS / " + skinned + " skinned";
        }

        public static string PlayArt()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null, "WoolScene is playing");
            var fill = scene.ProgressFill;
            string mode;
            if (fill.type == Image.Type.Filled)
            {
                scene.ShowProgress(0);
                var empty = fill.fillAmount;
                scene.ShowProgress(0.5f);
                var half = fill.fillAmount;
                scene.ShowProgress(1);
                Expect(empty == 0 && Mathf.Abs(half - 0.5f) < 1e-4f && fill.fillAmount == 1, "Filled bar shows 0, 50 and 100 percent");
                mode = "filled";
            }
            else
            {
                var rect = fill.rectTransform;
                var span = ((RectTransform)rect.parent).rect.width - 2 * rect.anchoredPosition.x;
                var left = rect.anchoredPosition.x;
                scene.ShowProgress(0);
                Expect(rect.rect.width == 0, "0 percent shows no fill");
                scene.ShowProgress(0.5f);
                Expect(Mathf.Abs(rect.rect.width - span * 0.5f) < 0.5f && rect.anchoredPosition.x == left, "50 percent fills half the track from the left");
                scene.ShowProgress(1);
                Expect(Mathf.Abs(rect.rect.width - span) < 0.5f && rect.anchoredPosition.x == left, "100 percent fills the whole track");
                scene.ShowProgress(2);
                Expect(Mathf.Abs(rect.rect.width - span) < 0.5f, "Progress above 100 percent is clamped");
                mode = "width " + span.ToString("F0");
            }
            WoolUiMotion.Pose(WoolUiMotionKind.Spin, WoolUiBuilder.RaysSpeed, 1, 1, out _, out var spin, out _);
            WoolUiMotion.Pose(WoolUiMotionKind.Bob, WoolUiBuilder.BobHeight, WoolUiBuilder.BobPeriod, WoolUiBuilder.BobPeriod * 0.5f, out var top, out _, out _);
            WoolUiMotion.Pose(WoolUiMotionKind.Bob, WoolUiBuilder.BobHeight, WoolUiBuilder.BobPeriod, WoolUiBuilder.BobPeriod, out var rest, out _, out _);
            WoolUiMotion.Pose(WoolUiMotionKind.Sway, WoolUiBuilder.SwayAngle, WoolUiBuilder.SwayPeriod, WoolUiBuilder.SwayPeriod * 0.25f, out _, out var sway, out _);
            Expect(Mathf.Abs(spin + 20) < 1e-3f, "Rays turn 20 degrees clockwise in one second but turned " + spin);
            Expect(Mathf.Abs(top.y - 12) < 1e-3f && rest.magnitude < 1e-3f, "Characters bob 12 px over 1.6 s");
            Expect(Mathf.Abs(sway - 4) < 1e-3f, "Lost kitten sways 4 degrees");
            Expect(scene.ResultRays == null || !scene.ResultRays.activeInHierarchy, "Rays are hidden while playing");
            return "PLAY ART PASS / " + mode + " / motion";
        }

        public static string PlayArtResult(bool win)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene.ResultPanel.activeSelf, "Run PlayResultLate first");
            if (scene.ResultRays != null)
            {
                Expect(scene.ResultRays.activeSelf == win, "Rays show only on a win");
                var spin = scene.ResultRays.GetComponent<WoolUiMotion>();
                if (win) Expect(spin.Running && spin.Elapsed > 0 && Mathf.Abs(Mathf.DeltaAngle(scene.ResultRays.transform.localEulerAngles.z, -WoolUiBuilder.RaysSpeed * spin.Elapsed)) < 0.5f, "Rays spin while the win card is open");
            }
            var content = win ? scene.WonContent : scene.LostContent;
            var figure = content.transform.Find("Characters");
            if (figure != null)
            {
                var motion = figure.GetComponent<WoolUiMotion>();
                Expect(motion.Running && motion.Kind == (win ? WoolUiMotionKind.Bob : WoolUiMotionKind.Sway), (win ? "Win characters bob" : "Lost kitten sways") + " while the card is open");
            }
            return "PLAY ART RESULT PASS / " + (win ? "Won" : "Lost");
        }

        static int ExpectSkin(Transform root, string path, string art)
        {
            var node = root.Find(path);
            Expect(node != null, path + " exists");
            var image = node.GetComponent<Image>();
            if (!WoolUiArt.TryLoad(art, out var sprite))
            {
                Expect(!WoolUiArt.IsArt(image.sprite), path + " stays procedural without " + art);
                return 0;
            }
            Expect(image.sprite == sprite && Near(image.color, Color.white) && node.Find("Face") == null, path + " uses " + art + " without a Face");
            Expect(image.type == (sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple), path + " slices " + art + " only when it has a border");
            return 1;
        }

        static void ExpectIcon(Transform root, string path, string icon, bool round)
        {
            var node = root.Find(path);
            var iconNode = (RectTransform)node.Find("Icon");
            var label = node.Find("Label").GetComponent<Text>();
            if (icon == null || !WoolUiArt.TryLoad(icon, out var sprite))
            {
                Expect(iconNode == null && label.enabled, path + " has no icon and shows its label");
                return;
            }
            Expect(iconNode != null && iconNode.GetComponent<Image>().sprite == sprite && !iconNode.GetComponent<Image>().raycastTarget, path + " shows " + icon);
            if (round) Expect(!label.enabled && iconNode.anchoredPosition.x == 0, path + " centres " + icon + " and hides its label");
            else Expect(label.enabled && iconNode.anchoredPosition.x + iconNode.sizeDelta.x * 0.5f <= label.rectTransform.anchoredPosition.x - label.rectTransform.sizeDelta.x * 0.5f + 0.01f, path + " shows " + icon + " left of its label");
        }

        static void ExpectResult(Transform content, string banner, string figure, WoolUiMotionKind kind, float amount, float period)
        {
            var title = content.Find("Title").GetComponent<Text>();
            var bannerNode = content.Find("Banner");
            if (!WoolUiArt.TryLoad(banner, out var bannerArt)) Expect(bannerNode == null && title.rectTransform.sizeDelta == WoolUiBuilder.ResultTitleSize, content.name + " has no banner without " + banner);
            else
            {
                Expect(bannerNode != null && bannerNode.GetComponent<Image>().sprite == bannerArt && bannerNode.GetComponent<Image>().type == Image.Type.Sliced, content.name + " title sits on " + banner);
                Expect(bannerNode.GetSiblingIndex() < title.transform.GetSiblingIndex() && Near(title.color, WoolUiTheme.TextLight) && title.GetComponent<Outline>() != null, content.name + " title is white with a navy outline over the banner");
                Expect(Mathf.Abs(title.rectTransform.anchoredPosition.y - (WoolUiBuilder.BannerY + WoolUiBuilder.BannerTitleLift)) < 0.01f, content.name + " title is centred on the banner");
            }
            var figureNode = content.Find("Characters");
            if (!WoolUiArt.TryLoad(figure, out var figureArt))
            {
                Expect(figureNode == null, content.name + " has no characters without " + figure);
                return;
            }
            Expect(figureNode != null && figureNode.GetComponent<Image>().sprite == figureArt && figureNode.GetSiblingIndex() == 0 && ((RectTransform)figureNode).sizeDelta.y == WoolUiBuilder.CharactersSize, content.name + " shows " + figure + " behind the banner");
            var motion = figureNode.GetComponent<WoolUiMotion>();
            Expect(motion != null && motion.Kind == kind && motion.Amount == amount && motion.Period == period, content.name + " characters move as " + kind);
        }

        static void ExpectGameFallback(Transform canvas)
        {
            var active = SceneManager.GetActiveScene();
            var temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(temp);
                var clone = Object.Instantiate(canvas.gameObject).transform;
                clone.name = "Canvas";
                WoolUiArt.SimulateMissing = true;
                WoolUiBuilder.StyleGame(clone);
                var pause = clone.Find("Pause");
                Expect(pause.Find("Face") != null && pause.Find("Icon") == null && pause.Find("Label").GetComponent<Text>().enabled && !WoolUiArt.IsArt(pause.GetComponent<Image>().sprite), "Without art Pause is the procedural circle with its label");
                var fill = clone.Find("ProgressTrack/ProgressFill").GetComponent<Image>();
                Expect(fill.type == Image.Type.Filled && fill.sprite.name == "UiBar" && fill.rectTransform.pivot == new Vector2(0.5f, 0.5f) && fill.rectTransform.sizeDelta == WoolUiBuilder.FillSize, "Without art the progress fill is the procedural Filled bar");
                var card = clone.Find("ResultPanel/Card");
                Expect(card.Find("Face") != null && Near(card.GetComponent<Image>().color, WoolUiTheme.CardEdge) && card.GetComponent<Image>().pixelsPerUnitMultiplier == 1, "Without art the result card is the raised cream card");
                Expect(clone.Find("ResultPanel/Rays") == null && card.Find("WonContent/Banner") == null && card.Find("WonContent/Characters") == null && card.Find("LostContent/Banner") == null, "Without art the result card has no rays, banner or characters");
                Expect(Near(card.Find("WonContent/Title").GetComponent<Text>().color, WoolUiTheme.Primary) && clone.Find("Retry/Icon") == null && clone.Find("LevelBadge/Face") != null, "Without art the titles, Retry and badge are procedural");
                WoolUiArt.SimulateMissing = false;
                WoolUiBuilder.StyleGame(clone);
                if (WoolUiArt.TryLoad("Button-Round-Orange", out var round)) Expect(pause.GetComponent<Image>().sprite == round && pause.Find("Face") == null, "Art comes back after the fallback");
            }
            finally
            {
                WoolUiArt.SimulateMissing = false;
                SceneManager.SetActiveScene(active);
                EditorSceneManager.CloseScene(temp, true);
            }
        }
    }
}
```

- [ ] **Bước 7: Check cũ chấp nhận ảnh — `WoolArtChecks.Ui.cs`**

**Trước**
```27:30:Assets/JamJam/Editor/WoolArtChecks.Ui.cs
                Expect(game.ProgressFill.type == Image.Type.Filled && game.ProgressFill.sprite != null && game.ProgressFill.sprite.name == "UiBar", "Progress fill uses the UiBar sprite and stays Filled");
                var won = game.WonContent.transform.Find("Title").GetComponent<Text>();
                var lost = game.LostContent.transform.Find("Title").GetComponent<Text>();
                Expect(Near(won.color, WoolUiTheme.Primary) && Near(lost.color, WoolUiTheme.Danger), "Won title is Primary and lost title is Danger");
```

**Sau**
```csharp
                var fill = game.ProgressFill;
                Expect((fill.type == Image.Type.Filled && fill.sprite != null && fill.sprite.name == "UiBar") || (fill.type == Image.Type.Sliced && WoolUiArt.IsArt(fill.sprite, "Progress-Fill")), "Progress fill is the Filled UiBar or the sliced Progress-Fill art");
                var won = game.WonContent.transform.Find("Title").GetComponent<Text>();
                var lost = game.LostContent.transform.Find("Title").GetComponent<Text>();
                var wonBanner = game.WonContent.transform.Find("Banner") != null;
                var lostBanner = game.LostContent.transform.Find("Banner") != null;
                Expect(Near(won.color, wonBanner ? WoolUiTheme.TextLight : WoolUiTheme.Primary) && Near(lost.color, lostBanner ? WoolUiTheme.TextLight : WoolUiTheme.Danger), "Result titles are white on a banner, otherwise Primary and Danger");
```

**Trước**
```77:77:Assets/JamJam/Editor/WoolArtChecks.Ui.cs
                    Expect(button.transition == Selectable.Transition.None && button.targetGraphic.transform.Find("Face") != null, name + " is a raised button without tint");
```

**Sau**
```csharp
                    Expect(button.transition == Selectable.Transition.None && (button.targetGraphic.transform.Find("Face") != null || WoolUiArt.IsArt(((Image)button.targetGraphic).sprite)), name + " is a raised or art button without tint");
```

**Trước**
```104:104:Assets/JamJam/Editor/WoolArtChecks.Ui.cs
            Expect(Near(card.GetComponent<Image>().color, WoolUiTheme.CardEdge) && card.Find("Face") != null && Near(card.Find("Face").GetComponent<Image>().color, WoolUiTheme.Card), panel.name + " card is cream with a navy border");
```

**Sau**
```csharp
            var cardImage = card.GetComponent<Image>();
            if (WoolUiArt.IsArt(cardImage.sprite)) Expect(WoolUiArt.IsArt(cardImage.sprite, "Panel-Card") && Near(cardImage.color, Color.white) && card.Find("Face") == null, panel.name + " card uses the Panel-Card art");
            else Expect(Near(cardImage.color, WoolUiTheme.CardEdge) && card.Find("Face") != null && Near(card.Find("Face").GetComponent<Image>().color, WoolUiTheme.Card), panel.name + " card is cream with a navy border");
```

- [ ] **Bước 8: Compile + RED**

```bash
unity recompile
E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'
E 'return JamJam.Editor.WoolArtChecks.UiArtGame();'
```
Kỳ vọng `False False`, rồi FAIL `ART CHECK FAILED: LevelBadge uses Badge-Level without a Face`.

- [ ] **Bước 9: Nối `ArtGame` vào `StyleGame`**

**Trước**
```89:90:Assets/JamJam/Editor/WoolUiBuilder.cs
            Stack(resultCard);
            WoolUiStyle.RoundPills(canvas);
```

**Sau**
```csharp
            Stack(resultCard);
            ArtGame(canvas);
            WoolUiStyle.RoundPills(canvas);
```

- [ ] **Bước 10: Builder đầy đủ cùng import** (chỉ sửa code, không chạy)

**Trước**
```34:34:Assets/JamJam/Editor/WoolSceneBuilder.cs
            WoolUiStyle.BuildSprites();
```

**Sau**
```csharp
            WoolUiStyle.BuildSprites();
            WoolUiArt.Import();
```

`WoolSceneBuilder.Build` gọi `WoolUiBuilder.StyleGame(...)` (dòng 56), nên `ArtGame` (skin, banner, nhân vật, rays, gán `ResultRays` qua `FindGame` trong scene mới) chạy y như `Rebuild UI`.

- [ ] **Bước 11: Compile + Rebuild UI + GREEN**

```bash
unity recompile
E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'
E 'JamJam.Editor.WoolUiBuilder.Rebuild(); return "rebuilt";'
E 'return JamJam.Editor.WoolArtChecks.UiArtGame();'
E 'return JamJam.Editor.WoolArtChecks.UiArt() + " | " + JamJam.Editor.WoolArtChecks.UiAssets() + " | " + JamJam.Editor.WoolArtChecks.UiGame() + " | " + JamJam.Editor.WoolArtChecks.UiLobby();'
for f in WoolJam.unity Lobby.unity; do grep "m_Text:" "Assets/Scenes/$f" | sort | diff - "$RUN/strings.$f.txt" && echo "$f strings OK"; done
E 'return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty.ToString();'
```
Kỳ vọng: `UI ART GAME PASS / 13 skinned` (badge, Pause, Retry, 2 thẻ, Resume, 2 Home, Next, PlayAgain, track, fill, rays); chuỗi `UI ART PASS … | UI ASSETS PASS … | UI GAME PASS … | UI LOBBY PASS …`; hai dòng `strings OK`; `False` (check không làm dirty scene).

- [ ] **Bước 12: Play Mode** — mở scene, chạy, mỗi dòng một eval:

```bash
E 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/WoolJam.unity"); return "open";'
unity command set_run_in_background --enabled true $U
unity command start $U
E 'return JamJam.Editor.WoolArtChecks.PlayArt();'
E 'return JamJam.Editor.WoolArtChecks.PlayUi();'
E 'return JamJam.Editor.WoolArtChecks.PlayResultArm(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultEarly(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultLate(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayArtResult(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultArm(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultEarly(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultLate(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayArtResult(false);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultArm(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultEarly(true);'
E 'return JamJam.Editor.WoolArtChecks.PlayResultQuiet();'
E 'return JamJam.Editor.WoolArtChecks.PlayArt();'
E 'return JamJam.Editor.WoolArtChecks.PlayFeel();'
unity command stop $U
```
Đợi ~2 giây sau `start` cho level nạp. Kỳ vọng lần lượt: `PLAY ART PASS / width 740 / motion`; `PLAY UI PASS / juice / pop-in / pulse`; `ARMED / Won`; `EARLY PASS / 0.xx s`; `PLAY RESULT PASS / Won / …` (nếu `Wait longer` thì chạy lại đúng eval đó); `PLAY ART RESULT PASS / Won`; `ARMED / Lost`; `EARLY PASS …`; `PLAY RESULT PASS / Lost / …`; `PLAY ART RESULT PASS / Lost` (rays tắt khi thua sau một lần thắng); `ARMED / Won`; `EARLY PASS / … / replayed`; `PLAY RESULT QUIET PASS` (chạy khi đã qua ≥ 1.2 s thật); `PLAY ART PASS …` (rays đã ẩn lại khi chơi); `PLAY FEEL PASS / …`. Nếu một `PlayResultEarly` FAIL: `E 'UnityEngine.Time.timeScale = 1f; return "ts";'` rồi làm lại từ `PlayResultArm`. Lỗi console: `unity command --caller plugin --skill unity-cli --no-banner --no-pager` để tìm lệnh đọc console; không có lỗi mới.

- [ ] **Bước 13: Khôi phục** — với `N` trong `$RUN/cleared.txt`: `E 'UnityEngine.PlayerPrefs.SetInt("JamJam.Cleared", N); UnityEngine.PlayerPrefs.Save(); return UnityEngine.PlayerPrefs.GetInt("JamJam.Cleared", -1);'` (nếu `N = -1` thì `DeleteKey`); `unity command set_run_in_background --enabled false $U`. Kỳ vọng trả `N`.

- [ ] **Bước 14: Kiểm tra** — `md5sum -c "$RUN/logic.md5"` → mọi `OK`; harness → dòng PASS như Bước 0 Task 1; `git status --short Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab` không đổi so với đầu task; `grep -n "//\|/\*" Assets/JamJam/Runtime/WoolUiMotion.cs Assets/JamJam/Editor/WoolUiBuilder.ArtGame.cs Assets/JamJam/Editor/WoolArtChecks.UiArtGame.cs` → rỗng; `grep -rn "StartCoroutine\|IEnumerator\|DOTween\|UniTask" Assets/JamJam/Runtime Assets/JamJam/Editor` → rỗng.

- [ ] **Bước 15: Dừng, báo user.**

---

### Task 3: Lobby — nền, logo, nút, icon, thẻ level, Settings

**Files:**
- Create: `Assets/JamJam/Editor/WoolUiBuilder.ArtLobby.cs`, `Assets/JamJam/Editor/WoolArtChecks.UiArtLobby.cs`
- Modify: `Assets/JamJam/Runtime/WoolLevelCard.cs:18-19, 35-36`; `Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs:43, 60-61, 95-96`; `Assets/JamJam/Editor/WoolArtChecks.UiLobby.cs:48`; `Assets/JamJam/Editor/WoolLobbyBuilder.cs:32`
- Patch: `Assets/Scenes/Lobby.unity`, `Assets/JamJam/Generated/Prefabs/LevelCard.prefab`

**Interfaces — Consumes:** `WoolUiArt` (Task 1); `WoolUiMotion`, `WoolUiMotionKind`, `WoolUiBuilder.Motion`, `ArtPanel`, `WoolArtChecks.ExpectSkin/ExpectIcon` (Task 2); `WoolUiStyle.Badge/Light/LoadSprite`, `WoolUiBuilder.StyleLobby/StyleCard/CardPath`, `WoolLobbyBuilder.ScenePath` (có sẵn).

**Interfaces — Produces:**
- `WoolLevelCard`: field `Sprite ClearedArt, CurrentArt, LockedArt, Plain`; `float ArtScale = 1`.
- `WoolUiBuilder` (partial): `Vector2 LogoPosition (0,760), LogoSize (700,350), TaglineArtPosition (0,600), TaglinePosition (0,470), SettingsRoundPosition (400,830), SettingsRoundSize (120,120), SettingsPillPosition (340,830), SettingsPillSize (240,100), ToggleLabelArtPosition (-40,0), ToggleLabelPosition (-110,0), CardNumberArtPosition (-260,0), CardNumberArtSize (120,110), CardNumberPosition (-330,0), CardNumberSize (140,110), CardTitleArtPosition (-10,0), CardTitleArtSize (340,90), CardTitlePosition (-20,0), CardTitleSize (440,90)`; `const float LogoBreath 0.03, LogoBreathPeriod 2.4, PlayPulseScale 0.05, PlayPulsePeriod 1.2, ToggleIconX -260, ToggleIconSize 64, CardHeight 170, CardIconSize 52, CardBadgeArtX 250, CardBadgeX 310`.
- `WoolArtChecks.UiArtLobby()` → `"UI ART LOBBY PASS / 14 skinned"`; `PlayArtLobby()` → `"PLAY ART LOBBY PASS / 3 art cards / pulse / breath"`.

- [ ] **Bước 1: `WoolLevelCard` — field ảnh**

**Trước**
```18:19:Assets/JamJam/Runtime/WoolLevelCard.cs
        public Image Face;
        public Image Edge;
```

**Sau**
```csharp
        public Image Face;
        public Image Edge;
        public Sprite ClearedArt;
        public Sprite CurrentArt;
        public Sprite LockedArt;
        public Sprite Plain;
        public float ArtScale=1;
```

- [ ] **Bước 2: `WoolLevelCard.Bind` — chọn sprite theo trạng thái**

**Trước**
```35:36:Assets/JamJam/Runtime/WoolLevelCard.cs
            if(Face!=null) Face.color=face;
            if(Edge!=null) Edge.color=edge;
```

**Sau**
```csharp
            var art=cleared?ClearedArt:unlocked?CurrentArt:LockedArt;
            if(Edge!=null)
            {
                if(art!=null) { Edge.sprite=art; Edge.color=Color.white; Edge.pixelsPerUnitMultiplier=ArtScale; }
                else { if(Plain!=null) Edge.sprite=Plain; Edge.color=edge; Edge.pixelsPerUnitMultiplier=1; }
            }
            if(Face!=null) { Face.color=face; Face.enabled=art==null; }
```

`Face.color` vẫn được tô theo `CardColors` khi ẩn nên `PlayLobby()` cũ giữ nguyên.

- [ ] **Bước 3: Tạo `Assets/JamJam/Editor/WoolUiBuilder.ArtLobby.cs`** (chưa được gọi; Bước 7 mới nối)

Tạo `Assets/JamJam/Editor/WoolUiBuilder.ArtLobby.cs`:
```csharp
using UnityEngine;
using UnityEngine.UI;

namespace JamJam.Editor
{
    public static partial class WoolUiBuilder
    {
        public static readonly Vector2 LogoPosition = new Vector2(0, 760);
        public static readonly Vector2 LogoSize = new Vector2(700, 350);
        public static readonly Vector2 TaglineArtPosition = new Vector2(0, 600);
        public static readonly Vector2 TaglinePosition = new Vector2(0, 470);
        public const float LogoBreath = 0.03f;
        public const float LogoBreathPeriod = 2.4f;
        public const float PlayPulseScale = 0.05f;
        public const float PlayPulsePeriod = 1.2f;
        public static readonly Vector2 SettingsRoundPosition = new Vector2(400, 830);
        public static readonly Vector2 SettingsRoundSize = new Vector2(120, 120);
        public static readonly Vector2 SettingsPillPosition = new Vector2(340, 830);
        public static readonly Vector2 SettingsPillSize = new Vector2(240, 100);
        public const float ToggleIconX = -260f;
        public const float ToggleIconSize = 64f;
        public static readonly Vector2 ToggleLabelArtPosition = new Vector2(-40, 0);
        public static readonly Vector2 ToggleLabelPosition = new Vector2(-110, 0);
        public const float CardHeight = 170f;
        public const float CardIconSize = 52f;
        public const float CardBadgeArtX = 250f;
        public const float CardBadgeX = 310f;
        public static readonly Vector2 CardNumberArtPosition = new Vector2(-260, 0);
        public static readonly Vector2 CardNumberArtSize = new Vector2(120, 110);
        public static readonly Vector2 CardNumberPosition = new Vector2(-330, 0);
        public static readonly Vector2 CardNumberSize = new Vector2(140, 110);
        public static readonly Vector2 CardTitleArtPosition = new Vector2(-10, 0);
        public static readonly Vector2 CardTitleArtSize = new Vector2(340, 90);
        public static readonly Vector2 CardTitlePosition = new Vector2(-20, 0);
        public static readonly Vector2 CardTitleSize = new Vector2(440, 90);

        static void ArtLobby(Transform canvas)
        {
            Get(canvas, "LobbyPanel", out RectTransform home);
            Get(canvas, "LevelsPanel", out RectTransform levels);
            WoolUiArt.Background(home, "Lobby-Background");
            WoolUiArt.Background(levels, "Levels-Background");
            Get(home, "Kitten", out Image kitten);
            kitten.enabled = home.Find("Background") == null;
            Get(home, "Brand", out Text brand);
            Get(home, "Tagline", out Text tagline);
            WoolUiArt.Picture(home, "Logo", "Logo-WoolJam", LogoPosition, LogoSize, out var logo);
            brand.enabled = logo == null;
            tagline.rectTransform.anchoredPosition = logo != null ? TaglineArtPosition : TaglinePosition;
            if (logo != null)
            {
                logo.SetSiblingIndex(brand.transform.GetSiblingIndex() + 1);
                Motion(logo.gameObject, WoolUiMotionKind.Breathe, LogoBreath, LogoBreathPeriod);
            }
            Get(home, "Settings", out Button settings);
            var settingsRect = (RectTransform)settings.transform;
            settingsRect.anchoredPosition = SettingsRoundPosition;
            settingsRect.sizeDelta = SettingsRoundSize;
            if (!WoolUiArt.Round(settings, "Button-Round-Cream", "Icon-Settings"))
            {
                settingsRect.anchoredPosition = SettingsPillPosition;
                settingsRect.sizeDelta = SettingsPillSize;
                WoolUiArt.Button(settings, WoolUiTone.Secondary, "Icon-Settings");
            }
            PlayPulse(home, out var play);
            WoolUiArt.Button(play, WoolUiTone.Primary, "Icon-Play");
            Get(home, "Levels", out Button levelsButton); WoolUiArt.Button(levelsButton, WoolUiTone.Secondary, null);
            Get(levels, "Back", out Button back); WoolUiArt.Button(back, WoolUiTone.Secondary, "Icon-Back");
            Get(canvas, "SettingsPanel/Card", out Image card); ArtPanel(card);
            ArtToggle(card.transform, "Sound", "Icon-Sound");
            ArtToggle(card.transform, "Music", "Icon-Music");
            ArtToggle(card.transform, "Vibration", "Icon-Vibration");
            Get(card.transform, "Reset", out Button reset);
            if (WoolUiArt.Button(reset, WoolUiTone.Danger, null)) { Get(reset.transform, "Label", out Text resetLabel); WoolUiStyle.Light(resetLabel, 32); }
            Get(card.transform, "Close", out Button close); WoolUiArt.Button(close, WoolUiTone.Primary, null);
            Get(canvas, "SettingsPanel/ConfirmPanel/Card", out Image confirm); ArtPanel(confirm);
            Get(confirm.transform, "Yes", out Button yes); WoolUiArt.Button(yes, WoolUiTone.Danger, null);
            Get(confirm.transform, "No", out Button no); WoolUiArt.Button(no, WoolUiTone.Secondary, null);
        }

        static void PlayPulse(Transform home, out Button play)
        {
            var wrapper = home.Find("PlayPulse");
            if (wrapper == null)
            {
                Get(home, "Play", out RectTransform loose);
                var rect = new GameObject("PlayPulse", typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(home, false);
                rect.SetSiblingIndex(loose.GetSiblingIndex());
                rect.anchorMin = loose.anchorMin;
                rect.anchorMax = loose.anchorMax;
                rect.pivot = loose.pivot;
                rect.anchoredPosition = loose.anchoredPosition;
                rect.sizeDelta = loose.sizeDelta;
                loose.SetParent(rect, false);
                loose.anchoredPosition = Vector2.zero;
                wrapper = rect;
            }
            Get(wrapper, "Play", out play);
            Motion(wrapper.gameObject, WoolUiMotionKind.Breathe, PlayPulseScale, PlayPulsePeriod);
        }

        static void ArtToggle(Transform card, string name, string icon)
        {
            Get(card, name, out RectTransform row);
            Get(row, "Label", out Text label);
            if (!WoolUiArt.TryLoad(icon, out var sprite))
            {
                WoolUiArt.Remove(row, "Icon");
                label.rectTransform.anchoredPosition = ToggleLabelPosition;
                return;
            }
            WoolUiArt.Child(row, "Icon", out var rect, out var image);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            rect.anchoredPosition = new Vector2(ToggleIconX, 0);
            rect.sizeDelta = Vector2.one * ToggleIconSize;
            label.rectTransform.anchoredPosition = ToggleLabelArtPosition;
        }

        static void ArtCard(WoolLevelCard view, Sprite plain, Sprite pill)
        {
            WoolUiArt.TryLoad("LevelCard-Cleared", out var cleared);
            WoolUiArt.TryLoad("LevelCard-Current", out var current);
            WoolUiArt.TryLoad("LevelCard-Locked", out var locked);
            view.ClearedArt = cleared;
            view.CurrentArt = current;
            view.LockedArt = locked;
            view.Plain = plain;
            var sample = current != null ? current : cleared != null ? cleared : locked;
            var art = sample != null;
            view.ArtScale = art ? sample.rect.height / CardHeight : 1;
            view.Edge.pixelsPerUnitMultiplier = 1;
            view.Face.enabled = true;
            if (current != null)
            {
                view.Edge.sprite = current;
                view.Edge.color = Color.white;
                view.Edge.pixelsPerUnitMultiplier = view.ArtScale;
                view.Face.enabled = false;
            }
            view.Number.rectTransform.anchoredPosition = art ? CardNumberArtPosition : CardNumberPosition;
            view.Number.rectTransform.sizeDelta = art ? CardNumberArtSize : CardNumberSize;
            view.Title.rectTransform.anchoredPosition = art ? CardTitleArtPosition : CardTitlePosition;
            view.Title.rectTransform.sizeDelta = art ? CardTitleArtSize : CardTitleSize;
            foreach (var badge in new[] { view.Check, view.Lock, view.Current })
            {
                var rect = (RectTransform)badge.transform;
                rect.anchoredPosition = new Vector2(art ? CardBadgeArtX : CardBadgeX, rect.anchoredPosition.y);
            }
            ArtBadge(view.Check, WoolUiTheme.Badge, pill, "Icon-Check", 26);
            ArtBadge(view.Lock, WoolUiTheme.Badge, pill, "Icon-Lock", 26);
            ArtBadge(view.Current, WoolUiTheme.Primary, pill, "Icon-Play", 28);
        }

        static void ArtBadge(GameObject badge, Color color, Sprite pill, string icon, int labelSize)
        {
            if (!WoolUiArt.TryLoad(icon, out var sprite)) return;
            WoolUiStyle.Badge(badge.GetComponent<Image>(), color, pill, sprite, labelSize);
            Get(badge.transform, "Icon", out RectTransform rect);
            rect.sizeDelta = Vector2.one * CardIconSize;
        }
    }
}
```

- [ ] **Bước 4: Tạo check `Assets/JamJam/Editor/WoolArtChecks.UiArtLobby.cs`**

Tạo `Assets/JamJam/Editor/WoolArtChecks.UiArtLobby.cs`:
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
        public static string UiArtLobby()
        {
            Expect(!EditorApplication.isPlaying, "Exit Play Mode first");
            OpenForCheck(WoolLobbyBuilder.ScenePath, out var scene, out var opened);
            var skinned = 0;
            try
            {
                var lobby = Object.FindFirstObjectByType<WoolLobby>();
                Expect(lobby != null, "WoolLobby exists");
                var canvas = lobby.LobbyPanel.transform.parent;
                var home = lobby.LobbyPanel.transform;
                skinned += ExpectBackground(home, "Lobby-Background") + ExpectBackground(lobby.LevelsPanel.transform, "Levels-Background");
                Expect(lobby.Kitten.GetComponent<Image>().enabled == (home.Find("Background") == null), "Procedural kitten shows only without the lobby background");
                var brand = home.Find("Brand").GetComponent<Text>();
                var logo = home.Find("Logo");
                if (WoolUiArt.TryLoad("Logo-WoolJam", out var logoArt))
                {
                    Expect(logo != null && logo.GetComponent<Image>().sprite == logoArt && logo.GetComponent<Image>().preserveAspect && !brand.enabled && brand.text == "WOOL JAM", "Logo-WoolJam replaces the hidden WOOL JAM text");
                    var breath = logo.GetComponent<WoolUiMotion>();
                    Expect(breath != null && breath.Kind == WoolUiMotionKind.Breathe && breath.Amount == WoolUiBuilder.LogoBreath && breath.Period == WoolUiBuilder.LogoBreathPeriod, "Logo breathes 1 to 1.03 over 2.4 s");
                    Expect(((RectTransform)home.Find("Tagline")).anchoredPosition == WoolUiBuilder.TaglineArtPosition, "Tagline sits under the logo");
                    skinned++;
                }
                else Expect(logo == null && brand.enabled, "WOOL JAM text shows without the logo");
                var pulse = home.Find("PlayPulse");
                Expect(pulse != null && lobby.PlayButton.transform.parent == pulse && lobby.PlayButton.GetComponent<WoolButtonJuice>() != null && pulse.GetComponent<WoolButtonJuice>() == null, "PLAY pulses on a wrapper so its juice keeps its own transform");
                var motion = pulse.GetComponent<WoolUiMotion>();
                Expect(motion != null && motion.Kind == WoolUiMotionKind.Breathe && motion.Amount == WoolUiBuilder.PlayPulseScale && motion.Period == WoolUiBuilder.PlayPulsePeriod, "PLAY pulses 1 to 1.05 over 1.2 s");
                skinned += ExpectSkin(home, "PlayPulse/Play", "Button-Green");
                ExpectIcon(home, "PlayPulse/Play", "Icon-Play", false);
                skinned += ExpectSkin(home, "Levels", "Button-Cream");
                ExpectIcon(home, "Levels", null, false);
                var round = ExpectSkin(home, "Settings", "Button-Round-Cream");
                skinned += round;
                var settings = (RectTransform)home.Find("Settings");
                if (round == 1)
                {
                    Expect(settings.sizeDelta == WoolUiBuilder.SettingsRoundSize && settings.anchoredPosition == WoolUiBuilder.SettingsRoundPosition, "Settings is a round button in the top right");
                    ExpectIcon(home, "Settings", "Icon-Settings", true);
                }
                else Expect(settings.sizeDelta == WoolUiBuilder.SettingsPillSize && settings.anchoredPosition == WoolUiBuilder.SettingsPillPosition, "Settings stays a pill without round art");
                skinned += ExpectSkin(lobby.LevelsPanel.transform, "Back", "Button-Cream");
                ExpectIcon(lobby.LevelsPanel.transform, "Back", "Icon-Back", false);
                skinned += ExpectSkin(canvas, "SettingsPanel/Card", "Panel-Card");
                skinned += ExpectSkin(canvas, "SettingsPanel/Card/Close", "Button-Green");
                var red = ExpectSkin(canvas, "SettingsPanel/Card/Reset", "Button-Red");
                skinned += red;
                var resetLabel = canvas.Find("SettingsPanel/Card/Reset/Label").GetComponent<Text>();
                Expect(red == 0 || (Near(resetLabel.color, WoolUiTheme.TextLight) && resetLabel.GetComponent<Outline>() != null), "Reset on red uses white text with a navy outline");
                skinned += ExpectSkin(canvas, "SettingsPanel/ConfirmPanel/Card", "Panel-Card");
                skinned += ExpectSkin(canvas, "SettingsPanel/ConfirmPanel/Card/Yes", "Button-Red");
                skinned += ExpectSkin(canvas, "SettingsPanel/ConfirmPanel/Card/No", "Button-Cream");
                foreach (var (toggle, icon) in new[] { (lobby.SoundToggle, "Icon-Sound"), (lobby.MusicToggle, "Icon-Music"), (lobby.VibrationToggle, "Icon-Vibration") })
                {
                    var node = (RectTransform)toggle.transform.Find("Icon");
                    var label = (RectTransform)toggle.transform.Find("Label");
                    if (!WoolUiArt.TryLoad(icon, out var sprite)) { Expect(node == null && label.anchoredPosition == WoolUiBuilder.ToggleLabelPosition, toggle.name + " has no icon without " + icon); continue; }
                    Expect(node != null && node.GetComponent<Image>().sprite == sprite && node.anchoredPosition.x + node.sizeDelta.x * 0.5f <= label.anchoredPosition.x - label.sizeDelta.x * 0.5f + 0.01f, toggle.name + " shows " + icon + " left of its label");
                }
                ExpectLobbyFallback(canvas);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            var root = PrefabUtility.LoadPrefabContents(WoolUiBuilder.CardPath);
            try
            {
                var view = root.GetComponent<WoolLevelCard>();
                WoolUiStyle.LoadSprite("UiCard", out var plain);
                Expect(view.Plain == plain, "LevelCard keeps the procedural card for states without art");
                ExpectCardArt(view.ClearedArt, "LevelCard-Cleared");
                ExpectCardArt(view.CurrentArt, "LevelCard-Current");
                ExpectCardArt(view.LockedArt, "LevelCard-Locked");
                var art = view.ClearedArt != null || view.CurrentArt != null || view.LockedArt != null;
                Expect(view.Number.rectTransform.anchoredPosition == (art ? WoolUiBuilder.CardNumberArtPosition : WoolUiBuilder.CardNumberPosition), "Card number clears the left decoration");
                Expect(!art || Mathf.Abs(view.ArtScale - 256f / WoolUiBuilder.CardHeight) < 1e-3f, "Card art scales its corners to the 170 px card");
                view.Bind(0, "First Threads", true, true, false);
                ExpectBound(view, view.ClearedArt, WoolUiTheme.PrimaryEdge, "Cleared");
                view.Bind(1, "Color Traffic", true, false, true);
                ExpectBound(view, view.CurrentArt, WoolUiTheme.WarmEdge, "Current");
                view.Bind(2, "The Great Unravel", false, false, false);
                ExpectBound(view, view.LockedArt, WoolUiTheme.LockedEdge, "Locked");
                view.ClearedArt = null;
                view.Bind(0, "First Threads", true, true, false);
                ExpectBound(view, null, WoolUiTheme.PrimaryEdge, "Cleared without art");
                ExpectBadgeIcon(view.Check, "Icon-Check", "UiCheck");
                ExpectBadgeIcon(view.Lock, "Icon-Lock", "UiLock");
                ExpectBadgeIcon(view.Current, "Icon-Play", null);
                WoolUiArt.SimulateMissing = true;
                WoolUiBuilder.StyleCard(root);
                Expect(view.ClearedArt == null && view.CurrentArt == null && view.LockedArt == null && view.Face.enabled && view.Edge.sprite == plain && view.Number.rectTransform.anchoredPosition == WoolUiBuilder.CardNumberPosition, "Without art the level card is procedural");
                Expect(view.Check.transform.Find("Icon").GetComponent<Image>().sprite.name == "UiCheck" && view.Current.transform.Find("Icon") == null, "Without art the badges use the procedural icons");
                WoolUiArt.SimulateMissing = false;
                WoolUiBuilder.StyleCard(root);
                if (WoolUiArt.TryLoad("LevelCard-Current", out var current)) Expect(view.CurrentArt == current && view.Edge.sprite == current && !view.Face.enabled, "Card art comes back after the fallback");
            }
            finally
            {
                WoolUiArt.SimulateMissing = false;
                PrefabUtility.UnloadPrefabContents(root);
            }
            return "UI ART LOBBY PASS / " + skinned + " skinned";
        }

        public static string PlayArtLobby()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var lobby = Object.FindFirstObjectByType<WoolLobby>();
            Expect(lobby != null, "Lobby is running");
            lobby.ShowLobby();
            var pulse = lobby.PlayButton.transform.parent.GetComponent<WoolUiMotion>();
            Expect(pulse != null && pulse.Running, "PLAY pulse runs while the home panel is open");
            pulse.enabled = false;
            pulse.enabled = true;
            pulse.Step(WoolUiBuilder.PlayPulsePeriod * 0.5f);
            Expect(Mathf.Abs(pulse.transform.localScale.x - (1 + WoolUiBuilder.PlayPulseScale)) < 1e-3f && lobby.PlayButton.transform.localScale.x == 1, "PLAY wrapper reaches 1.05 while the button keeps scale 1");
            var logo = lobby.LobbyPanel.transform.Find("Logo");
            if (logo != null)
            {
                var breath = logo.GetComponent<WoolUiMotion>();
                Expect(breath.Running, "Logo breathes while the home panel is open");
                breath.enabled = false;
                breath.enabled = true;
                breath.Step(WoolUiBuilder.LogoBreathPeriod * 0.5f);
                Expect(Mathf.Abs(logo.localScale.x - (1 + WoolUiBuilder.LogoBreath)) < 1e-3f, "Logo reaches 1.03 at half period");
            }
            lobby.ShowLevels();
            Expect(!pulse.Running && pulse.transform.localScale == Vector3.one, "PLAY pulse stops and resets when the home panel hides");
            var cards = lobby.CardContainer.GetComponentsInChildren<WoolLevelCard>();
            var bound = 0;
            foreach (var card in cards)
            {
                var art = card.Check.activeSelf ? card.ClearedArt : card.Lock.activeSelf ? card.LockedArt : card.CurrentArt;
                if (art == null) continue;
                Expect(card.Edge.sprite == art && !card.Face.enabled && Near(card.Edge.color, Color.white), card.name + " shows " + art.name);
                bound++;
            }
            lobby.ShowLobby();
            return "PLAY ART LOBBY PASS / " + bound + " art cards / pulse / breath";
        }

        static int ExpectBackground(Transform panel, string art)
        {
            var node = panel.Find("Background");
            if (!WoolUiArt.TryLoad(art, out var sprite))
            {
                Expect(node == null, panel.name + " has no background without " + art);
                return 0;
            }
            Expect(node != null, panel.name + " has a Background");
            var rect = (RectTransform)node;
            var fitter = node.GetComponent<AspectRatioFitter>();
            Expect(node.GetSiblingIndex() == 0 && node.GetComponent<Image>().sprite == sprite && !node.GetComponent<Image>().raycastTarget, panel.name + " draws " + art + " behind everything");
            Expect(fitter != null && fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent && Mathf.Abs(fitter.aspectRatio - sprite.rect.width / sprite.rect.height) < 1e-4f && rect.pivot == new Vector2(0.5f, 0), panel.name + " background covers the screen anchored at the bottom");
            return 1;
        }

        static void ExpectCardArt(Sprite assigned, string art)
        {
            if (WoolUiArt.TryLoad(art, out var sprite)) Expect(assigned == sprite, "LevelCard uses " + art);
            else Expect(assigned == null, "LevelCard has no " + art);
        }

        static void ExpectBound(WoolLevelCard view, Sprite art, Color edge, string state)
        {
            if (art != null) Expect(view.Edge.sprite == art && Near(view.Edge.color, Color.white) && !view.Face.enabled && Mathf.Abs(view.Edge.pixelsPerUnitMultiplier - view.ArtScale) < 1e-4f, state + " card shows " + art.name);
            else Expect(view.Edge.sprite == view.Plain && Near(view.Edge.color, edge) && view.Face.enabled && view.Edge.pixelsPerUnitMultiplier == 1, state + " card falls back to the procedural colours");
        }

        static void ExpectBadgeIcon(GameObject badge, string art, string plain)
        {
            var node = badge.transform.Find("Icon");
            if (WoolUiArt.TryLoad(art, out var sprite)) Expect(node != null && node.GetComponent<Image>().sprite == sprite && ((RectTransform)node).sizeDelta == Vector2.one * WoolUiBuilder.CardIconSize, badge.name + " badge shows " + art);
            else if (plain == null) Expect(node == null, badge.name + " badge has no icon");
            else Expect(node != null && node.GetComponent<Image>().sprite.name == plain, badge.name + " badge keeps " + plain);
        }

        static void ExpectLobbyFallback(Transform canvas)
        {
            var active = SceneManager.GetActiveScene();
            var temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(temp);
                var clone = Object.Instantiate(canvas.gameObject).transform;
                clone.name = "Canvas";
                var camera = new GameObject("Probe Camera").AddComponent<Camera>();
                WoolUiArt.SimulateMissing = true;
                WoolUiBuilder.StyleLobby(clone, camera);
                var home = clone.Find("LobbyPanel");
                var settings = (RectTransform)home.Find("Settings");
                Expect(home.Find("Background") == null && clone.Find("LevelsPanel/Background") == null && home.Find("Kitten").GetComponent<Image>().enabled, "Without art the lobby has no background and shows the procedural kitten");
                Expect(home.Find("Logo") == null && home.Find("Brand").GetComponent<Text>().enabled, "Without art the WOOL JAM text shows");
                Expect(settings.sizeDelta == WoolUiBuilder.SettingsPillSize && settings.anchoredPosition == WoolUiBuilder.SettingsPillPosition && settings.Find("Icon") == null && settings.Find("Face") != null && settings.Find("Label").GetComponent<Text>().enabled, "Without art Settings is the procedural pill");
                Expect(clone.Find("SettingsPanel/Card/Face") != null && clone.Find("SettingsPanel/Card/Sound/Icon") == null && home.Find("PlayPulse/Play/Face") != null, "Without art the settings card, toggles and PLAY are procedural");
                WoolUiArt.SimulateMissing = false;
                WoolUiBuilder.StyleLobby(clone, camera);
                if (WoolUiArt.TryLoad("Button-Round-Cream", out var round)) Expect(settings.GetComponent<Image>().sprite == round && settings.sizeDelta == WoolUiBuilder.SettingsRoundSize, "Lobby art comes back after the fallback");
            }
            finally
            {
                WoolUiArt.SimulateMissing = false;
                SceneManager.SetActiveScene(active);
                EditorSceneManager.CloseScene(temp, true);
            }
        }
    }
}
```

- [ ] **Bước 5: `UiLobby` chấp nhận icon badge ảnh**

**Trước**
```48:48:Assets/JamJam/Editor/WoolArtChecks.UiLobby.cs
                Expect(view.Check.transform.Find("Icon").GetComponent<Image>().sprite.name == "UiCheck" && view.Lock.transform.Find("Icon").GetComponent<Image>().sprite.name == "UiLock", "Cleared shows a check and locked shows a padlock");
```

**Sau**
```csharp
                var checkIcon = view.Check.transform.Find("Icon").GetComponent<Image>().sprite.name;
                var lockIcon = view.Lock.transform.Find("Icon").GetComponent<Image>().sprite.name;
                Expect((checkIcon == "UiCheck" || checkIcon == "Icon-Check") && (lockIcon == "UiLock" || lockIcon == "Icon-Lock"), "Cleared shows a check and locked shows a padlock");
```

- [ ] **Bước 6: Compile + RED**

```bash
unity recompile
E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'
E 'return JamJam.Editor.WoolArtChecks.UiArtLobby();'
```
Kỳ vọng `False False`, rồi FAIL `ART CHECK FAILED: LobbyPanel has a Background`.

- [ ] **Bước 7: Nối `ArtLobby`/`ArtCard` — `WoolUiBuilder.Lobby.cs`**

**Trước**
```43:43:Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs
            Get(canvas, "LobbyPanel/Play", out Button play); WoolUiStyle.Button(play, WoolUiTone.Primary, pill, 60); Juice(play);
```

**Sau**
```csharp
            Get(canvas, canvas.Find("LobbyPanel/PlayPulse") != null ? "LobbyPanel/PlayPulse/Play" : "LobbyPanel/Play", out Button play); WoolUiStyle.Button(play, WoolUiTone.Primary, pill, 60); Juice(play);
```

**Trước**
```60:61:Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs
            Get(confirm, "No", out Button no); WoolUiStyle.Button(no, WoolUiTone.Secondary, pill, 34); Juice(no);
            WoolUiStyle.RoundPills(canvas);
```

**Sau**
```csharp
            Get(confirm, "No", out Button no); WoolUiStyle.Button(no, WoolUiTone.Secondary, pill, 34); Juice(no);
            ArtLobby(canvas);
            WoolUiStyle.RoundPills(canvas);
```

**Trước**
```95:96:Assets/JamJam/Editor/WoolUiBuilder.Lobby.cs
            WoolUiStyle.Badge(view.Current.GetComponent<Image>(), WoolUiTheme.Primary, pill, null, 28);
            WoolUiStyle.RoundPills(root.transform);
```

**Sau**
```csharp
            WoolUiArt.TryLoad("Icon-Play", out var play);
            WoolUiStyle.Badge(view.Current.GetComponent<Image>(), WoolUiTheme.Primary, pill, play, 28);
            ArtCard(view, cardSprite, pill);
            WoolUiStyle.RoundPills(root.transform);
```

- [ ] **Bước 8: Builder lobby cùng import** (chỉ sửa code, không chạy)

**Trước**
```32:32:Assets/JamJam/Editor/WoolLobbyBuilder.cs
            WoolUiStyle.BuildSprites();
```

**Sau**
```csharp
            WoolUiStyle.BuildSprites();
            WoolUiArt.Import();
```

`WoolLobbyBuilder.Build` tạo `Play` trực tiếp dưới `LobbyPanel` rồi gọi `StyleLobby` (dòng 50), nên `ArtLobby` bọc `PlayPulse`, thêm nền/logo/icon giống `Rebuild UI`. `BuildCard` gọi `StyleCard` (dòng 124) nên `ArtCard` gán sprite thẻ trước `SaveAsPrefabAsset`.

- [ ] **Bước 9: Compile + Rebuild UI + GREEN**

```bash
unity recompile
E 'return UnityEditor.EditorApplication.isCompiling + " " + UnityEditor.EditorUtility.scriptCompilationFailed;'
E 'JamJam.Editor.WoolUiBuilder.Rebuild(); return "rebuilt";'
E 'return JamJam.Editor.WoolArtChecks.UiArtLobby();'
E 'return JamJam.Editor.WoolArtChecks.UiArt() + " | " + JamJam.Editor.WoolArtChecks.UiArtGame() + " | " + JamJam.Editor.WoolArtChecks.UiAssets() + " | " + JamJam.Editor.WoolArtChecks.UiGame() + " | " + JamJam.Editor.WoolArtChecks.UiLobby();'
for f in WoolJam.unity Lobby.unity; do grep "m_Text:" "Assets/Scenes/$f" | sort | diff - "$RUN/strings.$f.txt" && echo "$f strings OK"; done
grep "m_Text:" Assets/JamJam/Generated/Prefabs/LevelCard.prefab | sort | diff - "$RUN/strings.LevelCard.prefab.txt" && echo "card strings OK"
```
Kỳ vọng: `UI ART LOBBY PASS / 14 skinned` (2 nền, logo, Play, Levels, Settings, Back, 2 thẻ, Close, Reset, Yes, No); năm chuỗi PASS; ba dòng `strings OK`.

- [ ] **Bước 10: Play Mode Lobby**

```bash
E 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity"); return "open";'
unity command set_run_in_background --enabled true $U
unity command start $U
E 'return JamJam.Editor.WoolArtChecks.PlayArtLobby();'
E 'return JamJam.Editor.WoolArtChecks.PlayLobby();'
unity command stop $U
E 'UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/WoolJam.unity"); return "back";'
```
Đợi ~2 giây sau `start`. Kỳ vọng: `PLAY ART LOBBY PASS / 3 art cards / pulse / breath`; `PLAY LOBBY PASS / 3 cards / current N`. Không có lỗi console mới. Khôi phục PlayerPrefs và `set_run_in_background --enabled false` như Task 2 Bước 13.

- [ ] **Bước 11: Kiểm tra** — `md5sum -c "$RUN/logic.md5"`, `md5sum -c "$RUN/art.md5"` → mọi `OK`; harness như Task 1 Bước 0; `grep -n "//\|/\*" Assets/JamJam/Runtime/WoolLevelCard.cs Assets/JamJam/Editor/WoolUiBuilder.ArtLobby.cs Assets/JamJam/Editor/WoolArtChecks.UiArtLobby.cs` → rỗng.

- [ ] **Bước 12: Dừng, báo user.**

---

### Task 4: Kiểm thử tổng, idempotent, ảnh, so sánh, tinh chỉnh

**Files:** không sửa source ngoài hằng trong bảng knob (nếu cần). Tạo `docs/ui-art-*.png`, `docs/ui-art-compare.png`, script tạm `$RUN/compare.py`.

**Interfaces — Consumes:** mọi thứ Task 1–3.

**Knob được phép chỉnh** (không đụng gameplay, level, chuỗi chữ, wiring, pixel ảnh):

| Nơi | Hằng | Ràng buộc |
|---|---|---|
| `WoolUiArt` | `Borders` (từng ảnh) | phải giữ `UiArt()` PASS (border đo được là cận dưới ở mục "Border 9-slice") |
| `WoolUiArt` | `CardPixelsPerUnit`, `IconScale`, `RoundIconScale`, `IconGap`, `LabelTail` | thẻ 760×460 phải chứa tổng border/`CardPixelsPerUnit`; icon không đè chữ (`ExpectIcon`) |
| `WoolUiBuilder.ArtGame` | `TrackArtSize`, `FillArtHeight`, `FillInset`, `BannerSize`, `BannerY`, `BannerTitleSize`, `BannerTitleLift`, `ResultSubtitleY`, `CharactersSize`, `CharactersY`, `RaysSize`, `RaysLift` | ProgressNumber (x 405…505) không đè track; nhân vật không vượt mép trên canvas 1920 |
| `WoolUiBuilder.ArtGame` | `RaysSpeed`, `BobHeight`, `BobPeriod`, `SwayAngle`, `SwayPeriod` | spec ~20°/s, 12 px/1.6 s; `PlayArt` kiểm các giá trị spec |
| `WoolUiBuilder.ArtLobby` | `LogoPosition`, `LogoSize`, `TaglineArtPosition`, `SettingsRound*`, `ToggleIconX`, `ToggleIconSize`, `ToggleLabelArtPosition`, `CardNumberArt*`, `CardTitleArt*`, `CardBadgeArtX`, `CardIconSize` | nút ≥ 96×96; Settings tròn trong canvas 966 px ngang (1080×2400) |
| `WoolUiBuilder.ArtLobby` | `LogoBreath`, `LogoBreathPeriod`, `PlayPulseScale`, `PlayPulsePeriod` | spec 1.03/2.4 s, 1.05/1.2 s |

- [ ] **Bước 1: Hồi quy**

```bash
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj --verbosity quiet
E 'JamJam.Editor.WoolEditorChecks.ValidateLevels(); return System.IO.File.ReadAllText("Logs/WoolJam.verify.txt");'
E 'return JamJam.Editor.WoolArtChecks.Textures() + " | " + JamJam.Editor.WoolArtChecks.Blocks() + " | " + JamJam.Editor.WoolArtChecks.Chevron() + " | " + JamJam.Editor.WoolArtChecks.Stage() + " | " + JamJam.Editor.WoolArtChecks.Tray() + " | " + JamJam.Editor.WoolArtChecks.Feel();'
E 'return JamJam.Editor.WoolArtChecks.UiArt() + " | " + JamJam.Editor.WoolArtChecks.UiArtGame() + " | " + JamJam.Editor.WoolArtChecks.UiArtLobby();'
E 'return JamJam.Editor.WoolArtChecks.UiAssets() + " | " + JamJam.Editor.WoolArtChecks.UiGame() + " | " + JamJam.Editor.WoolArtChecks.UiLobby();'
md5sum -c "$RUN/logic.md5"; md5sum -c "$RUN/art.md5"
```
Kỳ vọng: harness `PASS: 418 gameplay assertions; 16296 route assertions; 18 progress assertions; 108 feel assertions`; file verify có `LEVEL 1/2/3 SOLUTION PASS`; sáu chuỗi PASS cũ; ba chuỗi `UI ART … PASS`; ba chuỗi `UI … PASS`; mọi `OK`. Rồi Play Mode `WoolJam.unity`: chuỗi eval Task 2 Bước 12, thêm `E 'return JamJam.Editor.WoolArtChecks.PlayBlocks();'` và `E 'return JamJam.Editor.WoolArtChecks.PlayFinish(true);'` (`PLAY FINISH PASS / Won`), stop. Play Mode `Lobby.unity`: Task 3 Bước 10, stop. Khôi phục PlayerPrefs.

- [ ] **Bước 2: Idempotent — `Rebuild Art` + `Rebuild UI` hai lần** (thoát Play Mode, lưu scene, giữ nguyên kích thước Game view giữa hai lần vì `AspectRatioFitter` ghi `sizeDelta` theo canvas)

```bash
R='JamJam.Editor.WoolArtBuilder.Rebuild(); JamJam.Editor.WoolUiBuilder.Rebuild(); return "rebuilt";'
E "$R"
md5sum Assets/Scenes/WoolJam.unity Assets/Scenes/Lobby.unity Assets/JamJam/Generated/Prefabs/LevelCard.prefab Assets/JamJam/Art/UI/*.meta Assets/JamJam/Generated/Textures/UI/* > "$RUN/rebuild1.md5"
E "$R"
md5sum -c "$RUN/rebuild1.md5"
grep -r "^guid:" Assets/JamJam Assets/Scenes --include=*.meta | sort > "$RUN/guids.now.txt"; comm -23 "$RUN/guids.before.txt" "$RUN/guids.now.txt"
md5sum -c "$RUN/art.md5"
E 'return JamJam.Editor.WoolArtChecks.UiArtGame() + " | " + JamJam.Editor.WoolArtChecks.UiArtLobby();'
```
Kỳ vọng: mọi dòng `OK`; `comm` rỗng; hai chuỗi PASS. Nếu một file đổi giữa hai lần: lưu bản đầu, `git diff --no-index` tìm trường không ổn định (thường là object bị xóa/tạo lại → đổi sang tìm-hoặc-tạo), sửa rồi lặp lại bước này.

- [ ] **Bước 3: Ảnh 1080×1920 và 1080×2400** — mỗi khoảnh khắc chụp hai lần: `CAP docs/ui-art-X.png 1920` và `CAP docs/ui-art-X-2400.png 2400`.

`WoolJam.unity`: `set_run_in_background --enabled true`, `start`, đợi ~2 giây.
```bash
E 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(1); var g=s.State; for(var f=0;f<400;f++){ for(var b=0;b<g.Blocks.Count;b++) if(g.CanSelect(b)&&g.HasVisibleColor(g.Blocks[b].Definition.Color)){g.Select(b);break;} g.Step(0.02f);} return g.Status + " " + (1f-(float)g.Remaining/g.Total);'
```
Kỳ vọng `Playing 0.x` (thanh tiến độ đang ở giữa). Đợi ~1 giây → chụp `game`.
```bash
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>().Pause(); return "paused";'
```
Đợi ~0.5 giây → chụp `pause`.
```bash
E 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(1); var g=s.State; for(var f=0;f<30000&&g.Status==JamJam.RoundStatus.Playing;f++){ for(var b=0;b<g.Blocks.Count;b++) if(g.CanSelect(b)&&g.HasVisibleColor(g.Blocks[b].Definition.Color)){g.Select(b);break;} g.Step(0.02f);} return g.Status.ToString();'
```
Kỳ vọng `Won`. Đợi ~2 giây (0.9 s trễ + pop-in + rays đã xoay) → chụp `won`.
```bash
E 'var s=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolScene>(); s.LoadLevel(1); var g=s.State; for(var f=0;f<30000&&g.Status==JamJam.RoundStatus.Playing;f++) g.Step(0.02f); return g.Status.ToString();'
```
Kỳ vọng `Lost`. Đợi ~1.5 giây → chụp `lost`. `stop`.

`Lobby.unity` (mở bằng eval `OpenScene`), `start`, đợi ~2 giây → chụp `home`.
```bash
E 'UnityEngine.Object.FindFirstObjectByType<JamJam.WoolLobby>().ShowLevels(); return "levels";'
```
Đợi ~0.5 giây → chụp `levels`.
```bash
E 'var l=UnityEngine.Object.FindFirstObjectByType<JamJam.WoolLobby>(); l.ShowLobby(); l.OpenSettings(); return "settings";'
```
Đợi ~0.5 giây → chụp `settings`. `stop`, khôi phục PlayerPrefs, `set_run_in_background --enabled false`, mở lại `WoolJam.unity`. Kỳ vọng `ls docs/ui-art-*.png | wc -l` → `14`.

- [ ] **Bước 4: Ghép so sánh** — tạo `$RUN/compare.py` (ảnh tham chiếu chỉ truyền bằng đường dẫn):

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
"$PY" -I "$RUN/compare.py" docs/ui-art-compare.png "C:/Users/Admin/Downloads/videoframe_190.png" docs/ui-art-home.png docs/ui-art-levels.png docs/ui-art-game.png docs/ui-art-won.png
```
Kỳ vọng in `docs/ui-art-compare.png (…, 1280)`. Đọc từng `docs/ui-art-*.png` và ảnh ghép bằng Read, chấm:
1. Trang trí không méo: cuộn len trên badge, trên thẻ current và hai góc `Panel-Card` tròn đều; đuôi ruy băng nguyên vẹn; đầu thanh tiến độ bo tròn ở 1920 và 2400.
2. Result: tia sáng sau thẻ (chỉ ở `won`), nhân vật ngồi trên mép thẻ sau banner, tiêu đề trắng viền nằm giữa thân ruy băng; `lost` có mèo buồn và banner đỏ, không có tia.
3. Home: nền phủ kín cả hai tỉ lệ, sàn gỗ ở đáy, logo không bị cắt, tagline đọc được, PLAY xanh có icon play, Settings tròn góc trên phải; không còn mèo procedural.
4. Levels/Settings: thẻ xanh/cam/navy đúng trạng thái, số và tên không đè trang trí, badge có icon ảnh; hàng toggle có icon bên trái nhãn; Reset đỏ chữ trắng.
5. Không chữ/nút nào bị cắt hay chồng ở 1080×2400 (đặc biệt `NEXT ADVENTURE`, `KEEP UNRAVELING`, `RESET PROGRESS`, `PLAY  0N`, tên level); cảm giác màu/độ sống gần `videoframe_190.png`.
Mục nào không đạt: chỉnh knob, chạy lại `Rebuild UI`, Bước 1 (phần check UI), Bước 2, Bước 3–4.

- [ ] **Bước 5: Kiểm cuối**

```bash
git status --short
grep -rn "//\|/\*" Assets/JamJam/Runtime/WoolUiMotion.cs Assets/JamJam/Runtime/WoolLevelCard.cs Assets/JamJam/Runtime/WoolScene.cs Assets/JamJam/Editor/WoolUiArt.cs Assets/JamJam/Editor/WoolUiBuilder*.cs Assets/JamJam/Editor/WoolArtChecks.UiArt*.cs
grep -rn "StartCoroutine\|IEnumerator\|DOTween\|UniTask" Assets/JamJam/Runtime Assets/JamJam/Editor
```
Kỳ vọng: không file bị xóa (` D`); không đổi `WoolGame.cs`, `WoolRouting.cs`, `WoolEditorChecks.cs`, `Generated/Levels`, `Art/UI/*.png`; hai `grep` rỗng; không lỗi console mới.

- [ ] **Bước 6: Dừng, báo user** — gửi `docs/ui-art-compare.png`, 14 ảnh `docs/ui-art-*.png`, kết quả checklist, danh sách knob đã chỉnh (nếu có). Không commit.

## Tóm tắt

- `WoolUiArt` import 35 PNG (Sprite Single, không mipmap, Clamp, Bilinear, 2048 cho hai nền, 1024 còn lại) chỉ qua `TextureImporter`, giữ GUID và pixel. 13 ảnh slice có border đo từ pixel thật: badge `(96,72,64,96)`, banner `(224,0,224,0)`, nút `(88)`, thẻ `(280,160,280,248)`, track `(56,40)`, fill `(64,40)`, thẻ level cleared `(160,48,184,80)` / current `(192,64,104,64)` / locked `(104,48)`. `UiArt()` kiểm lại bằng phân tích pixel. `TryLoad` fallback từng file, `SimulateMissing` để test.
- Lớp ảnh chạy sau style procedural (`ArtGame`/`ArtLobby`/`ArtCard` trong `StyleGame`/`StyleLobby`/`StyleCard`), nên fallback là code hiện tại và builder đầy đủ ra cùng kết quả (chỉ thêm `WoolUiArt.Import()`). HUD: badge, Pause tròn cam + icon, track/fill ảnh (fill theo chiều rộng qua `ShowProgress`), Retry kem + icon. Pause/Result: `Panel-Card`, nút xanh/kem + icon, banner xanh/đỏ với tiêu đề trắng viền, nhân vật nảy / mèo lắc, tia sáng xoay 20°/s chỉ khi thắng (`WoolScene.ResultRays`).
- Lobby: nền phủ neo đáy (`AspectRatioFitter`), logo thở 1↔1.03, PLAY nhịp 1↔1.05 trên wrapper `PlayPulse`, Settings tròn kem + icon, Back/Levels kem, Settings card + icon toggle + Reset đỏ. Thẻ level chọn `LevelCard-Cleared/Current/Locked` theo trạng thái lúc `Bind`, badge dùng `Icon-Check/Lock/Play`.
- Chuyển động bằng `WoolUiMotion` (R3 `Observable.Interval` + `UpdateIgnoreTimeScale`, dispose và trả pose ở `OnDisable`). Kiểm thử: `UiArt`/`UiArtGame`/`UiArtLobby` (RED → GREEN, gồm fallback trên scene tạm/prefab không lưu), `PlayArt` (0/50/100 %, pose), `PlayArtResult`, `PlayArtLobby`, các check cũ cập nhật để chấp nhận ảnh, harness `418/16296/18/108`, chuỗi `m_Text` không đổi, rebuild hai lần giống byte, 14 ảnh + ảnh ghép với `videoframe_190.png`.
- Lệch spec: fill đặt bằng `sizeDelta` + pivot trái thay vì `anchorMax`; track và fill fallback theo cặp; icon Pause chỉ hiện khi có nút tròn; thêm field `WoolScene.ResultRays`; `UiArt` tách thành 3 check; `Play` được bọc trong `PlayPulse`; nhân vật nảy 0→12 px lên trên thay vì ±12.
