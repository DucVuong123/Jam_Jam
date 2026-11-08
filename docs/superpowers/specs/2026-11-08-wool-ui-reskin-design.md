# UI reskin (phần 3/3 của đợt làm lại art + game feel)

Ngày: 2026-11-08. Trạng thái: user cho phép chạy thẳng không cần duyệt ("từ phần 2 ko cần hỏi tôi duyệt đâu chạy hết các phần luôn đi").

## Mục tiêu

Đổi giao diện màn `WoolJam.unity` (HUD, Pause, Result) và `Lobby.unity` (Home, Level Select, Settings) sang phong cách casual tươi sáng giống "Wool Crush™ - Yarn Car Jam": chữ tròn đậm trắng viền navy, nút bo tròn "nổi khối" có cạnh dưới đậm hơn, thẻ panel kem, đọc rõ trên nền navy. Thêm chuyển động UI: panel bật lên, nút lún khi nhấn, panel kết quả trễ để hiệu ứng thắng (phần 2) được nhìn thấy. Không âm thanh. Không đổi luồng màn hình, logic lobby/progress, nội dung chữ (chuỗi), luật chơi.

## Quyết định

- Font: **Lilita One** (Google Fonts, giấy phép SIL OFL 1.1), tải `LilitaOne-Regular.ttf` + `OFL.txt` vào `Assets/JamJam/Art/Fonts/`. Dùng cho mọi `UnityEngine.UI.Text` và `TextMesh` số ống chỉ. Nếu tải thất bại, dùng `LegacyRuntime.ttf` + Bold và ghi lại.
- Reskin bằng patch tại chỗ, không dựng lại scene: menu mới `JamJam/Rebuild UI` mở `WoolJam.unity` rồi `Lobby.unity`, tìm object UI theo tên/đường dẫn hiện có và áp theme (font, màu, sprite, Outline/Shadow, kích thước nút nếu cần). Giữ nguyên chuỗi chữ (scene-owned text), GUID, wiring button. Idempotent.
- `WoolSceneBuilder.Build` và `WoolLobbyBuilder` dùng chung theme (cùng hàm áp style) để rebuild đầy đủ ra cùng giao diện; KHÔNG chạy `WoolSceneBuilder.Build` trên project.
- Timer/animation UI: R3 (`Observable.EveryUpdate`/`Interval`/`Timer` với `UnityTimeProvider.UpdateIgnoreTimeScale`), dispose khi disable/destroy. Không dùng coroutine, không thêm DOTween/UniTask.
- Quy tắc code giữ nguyên: không comment, method trả reference dùng interface/abstract, factory void + out, không commit.

## Theme (token)

| Token | Giá trị | Dùng cho |
|---|---|---|
| `TextLight` | `#FFFFFF` | chữ HUD/nhãn trên nền navy |
| `TextOutline` | `#0E1836`, Outline 3px + Shadow (0,-4) alpha 0.5 | mọi chữ trắng |
| `TextDark` | `#1E2B4F` | chữ trên thẻ kem/nút kem |
| `Primary` / `PrimaryEdge` | `#3CC03A` / `#23862A` | nút chính (Play, Next, Resume) |
| `Secondary` / `SecondaryEdge` | `#FFF3D6` / `#D9B77A` | nút phụ (Retry, Play Again, Home, Back, Settings) |
| `Warm` / `WarmEdge` | `#FFB21F` / `#C77A0A` | badge level, nút Pause |
| `Danger` | `#F0603A` | tiêu đề thua |
| `Card` / `CardEdge` | `#FFF8EC` / `#1E2B4F` viền 6px | thẻ Pause/Result/Settings |
| `Overlay` | `#0A1230` alpha 0.72 | nền mờ sau panel |
| `Track` / `Fill` | `#0E1836` / `#3CC03A` có vạch sáng trên | thanh tiến độ |
| Thẻ level | cleared `#3CC03A` + dấu ✓, current `#FFB21F` (nhịp nhẹ), locked `#2A3A66` + ổ khóa | Level Select |

Sprite procedural mới (sinh trong `WoolAssetsBuilder` hoặc builder UI, PNG trong `Generated/Textures/UI/`, sliced 9-slice): `UiPill` (bo tròn lớn; nút "nổi khối" = 2 Image dùng `UiPill`: con `Edge` màu cạnh lệch xuống 10px nằm sau + thân màu chính), `UiCard` (bo 32px viền), `UiCircle`, `UiCheck`, `UiLock`. Kích thước chạm tối thiểu 96×96 px ở canvas 1080×1920.

## Màn chơi (`WoolJam.unity`)

- Tiêu đề "WOOL JAM" và dòng "UNWIND THE DRAGON…" : chữ trắng viền, nhỏ lại để không cạnh tranh với gameplay; "THREAD TRAY", "TAP A CLEAR ARROW…", "Need room? Tap a + slot": trắng alpha 0.85 có viền.
- Badge level: pill `Warm` với số trắng viền; nút Pause tròn `Warm`.
- Thanh tiến độ: track navy đậm bo tròn, fill xanh có highlight, % trắng viền.
- Nút Retry: `Secondary` nổi khối.
- Pause: overlay + thẻ `Card` bật lên (scale 0.8→1 OutBack 0.3s, overlay alpha 0→0.72 trong 0.2s); nút Resume `Primary`, Home `Secondary`.
- Result: thắng → hiện panel sau **0.9s** (để sparkle/zoom phần 2 thấy được), thua → sau **0.6s**; thẻ bật lên như Pause; tiêu đề thắng xanh `Primary` viền trắng dày, thua `Danger`; nút Next `Primary`, Play Again/Home `Secondary`. Trễ dùng R3 `Observable.Timer`, hủy khi LoadLevel/Replay/Next/Home.
- Số ống chỉ (TextMesh): font Lilita One, trắng, viền giả bằng một TextMesh bóng navy lệch nhẹ phía sau (trong prefab Spool qua `WoolAssetsBuilder`, chạy `Rebuild Art`).

## Lobby (`Lobby.unity`)

- Home: logo "WOOL JAM" lớn trắng viền navy dày + bóng; nút PLAY `Primary` to; Settings `Secondary` tròn/pill.
- Level Select: thẻ level theo bảng token; nút Back `Secondary`.
- Settings: thẻ `Card` bật lên, toggle Sound/Music/Vibration dạng pill on (xanh)/off (xám navy), nút đóng.
- Giữ prefab `LevelCard.prefab` (GUID), sửa style trong prefab tại chỗ.

## Chuyển động UI (runtime)

- `WoolButtonJuice` (MonoBehaviour, `IPointerDownHandler`, `IPointerUpHandler`, `IPointerExitHandler`): nhấn → scale 0.92 trong 0.06s, thả → về 1 với OutBack 0.18s; chạy bằng R3 EveryUpdate bỏ qua timeScale, chỉ subscribe khi đang animate; dùng `WoolEase` (phần 2). Thêm vào mọi Button bởi `Rebuild UI`.
- `WoolPopIn` (MonoBehaviour): khi `OnEnable` chạy scale 0.8→1 OutBack và CanvasGroup alpha 0→1 (overlay), thời lượng cấu hình; gắn vào card Pause/Result/Settings.
- Thẻ level current: nhịp scale 1↔1.04 chu kỳ 1.2s (R3 Interval/EveryUpdate, dispose khi disable).
- Haptic nhẹ khi nhấn nút (dùng `WoolHapticPattern` mức Light qua `WoolHaptics.Create(WoolSettings.Vibration)`); tôn trọng toggle Vibration.

## Kiểm thử

- Editor check `WoolUiChecks` (partial hoặc class mới trong Editor): sau `Rebuild UI`, trong cả hai scene: mọi `Text` dùng font theme; mọi Text trắng có `Outline`; độ tương phản chữ/nền theo token ≥ 4.5 (tính từ token, check bảng ghép cặp); mọi `Button` có `WoolButtonJuice` và rect ≥ 96×96; card Pause/Result/Settings có `WoolPopIn`; chuỗi chữ giữ nguyên so với trước (so danh sách `m_Text` trước/sau).
- Play Mode: thắng → ResultPanel chưa active ở +0.3s, active ở +1.2s, card scale về 1 (±0.01) sau 0.5s nữa; Replay khi đang chờ → panel không hiện; nhấn nút → scale < 1 rồi về 1.
- Lobby Play Mode: PLAY → Level Select → chọn level vào game hoạt động như cũ; Settings toggle lưu như cũ.
- Hồi quy: harness (`153/4121/18/108`), `Validate All Levels`, `PlayBlocks`, `PlayFeel`, `Rebuild Art` + `Rebuild UI` hai lần idempotent, GUID/level không đổi.
- Ảnh 1080×1920 và 1080×2400: `docs/ui-game.png`, `docs/ui-pause.png`, `docs/ui-won.png`, `docs/ui-lost.png`, `docs/ui-home.png`, `docs/ui-levels.png`, `docs/ui-settings.png`; ghép so với ảnh store thành `docs/ui-compare-*.png`. Checklist: mọi chữ đọc rõ trên navy, nút nổi khối, panel bật lên, không bị cắt ở hai tỉ lệ.

## Ngoài phạm vi

Âm thanh; booster/coin/shop của game gốc; đổi luồng màn hình hay luật chơi; TextMeshPro migration.
