# Lobby và màn chọn level

Ngày: 2026-11-08. Trạng thái: chờ user duyệt spec.

## Mục tiêu

Thêm màn Lobby và màn chọn level trước gameplay Wool Jam. Người chơi mở game vào Lobby, bấm PLAY để vào level đang tới, hoặc bấm LEVELS để chọn level đã mở khóa. Có popup Settings lưu toggle âm thanh/rung (chưa có tác dụng thực) và reset tiến độ. Không thay đổi luật gameplay đã chốt (làn mũi tên, nudge 0.5, FIFO, spool).

## Quyết định đã chốt với user

- Lobby tối giản + Settings: logo, mèo trang trí, PLAY, LEVELS, nút Settings.
- Mở khóa tuần tự: level 1 mở sẵn; thắng N mở N+1; level khóa hiện ổ khóa, không vào được; level đã thắng có dấu tích, chơi lại được.
- Settings: toggle Sound, Music, Vibration lưu PlayerPrefs, chưa nối audio/rung; nút Reset progress có xác nhận.
- Kiến trúc hai scene: `Lobby.unity` (Lobby + Level Select + Settings là panel) và `WoolJam.unity` (gameplay hiện có).
- Gameplay thêm nút Home ở PausePanel và ResultPanel. Next ở level cuối về Lobby thay vì quay vòng level 1.
- Không tự commit; spec chỉ ghi file.

## Dữ liệu

### IWoolPrefs

Interface `GetInt(string key, int fallback)`, `SetInt(string key, int value)`, `HasKey(string key)`, `DeleteKey(string key)`, `Save()`. Bản Unity `WoolPlayerPrefs` gọi `PlayerPrefs`. Harness dùng bản giả trong bộ nhớ.

### WoolProgress

Pure C#, constructor nhận `IWoolPrefs` và `int levelCount`.

- Key mới `JamJam.Cleared`: số level đã thắng liên tiếp tính từ level 1, kẹp trong `[0, levelCount]`.
- Migrate: nếu chưa có `JamJam.Cleared` mà có key cũ `JamJam.Level = k` thì `Cleared = k`, ghi key mới, xóa key cũ. Chỉ chạy một lần. Key cũ bị kẹp ở level cuối nên người đã thắng level cuối trước đây sẽ thấy level cuối là "đang tới" chưa có dấu tích; chấp nhận.
- `Cleared`, `IsUnlocked(i)` ⇔ `0 <= i <= Cleared && i < levelCount`, `IsCleared(i)` ⇔ `i < Cleared`.
- `Current` (level PLAY vào) = `min(Cleared, levelCount - 1)`.
- `MarkCleared(i)`: chỉ khi `i == Cleared` thì tăng `Cleared` và lưu; thắng lại level cũ hoặc level đang khóa không đổi gì.
- `Reset()`: xóa `JamJam.Cleared`, không đụng key Settings.

### WoolSettings

Pure C#, constructor nhận `IWoolPrefs`. Thuộc tính `Sound`, `Music`, `Vibration` (bool, mặc định true) lưu key `JamJam.Sound`, `JamJam.Music`, `JamJam.Vibration` dạng 0/1.

### WoolSession

Static: `int SelectedLevel` (-1 = chưa chọn), hằng `LobbyScene = "Lobby"`, `GameScene = "WoolJam"`. `Clear()` đặt về -1.

## Luồng

1. Build Settings: `Assets/Scenes/Lobby.unity` index 0, `Assets/Scenes/WoolJam.unity` index 1, giữ SampleScene sau cùng nếu đang có.
2. Lobby PLAY: `WoolSession.SelectedLevel = progress.Current`, `SceneManager.LoadScene(GameScene)`.
3. Level Select: thẻ mở khóa đặt `SelectedLevel = i` rồi load GameScene; thẻ khóa rung nhẹ, không load.
4. `WoolScene.Start()`: nếu `SelectedLevel >= 0` thì load level đó và `Clear()`; ngược lại load `progress.Current` (giữ hành vi bấm Play thẳng trong WoolJam).
5. Khi thắng: `progress.MarkCleared(CurrentLevel)` thay cho ghi `JamJam.Level`.
6. `Home()`: `SceneManager.LoadScene(LobbyScene)`. `Next()` khi thắng: nếu `CurrentLevel` là level cuối thì `Home()`, ngược lại `LoadLevel(CurrentLevel + 1)`.

## UI

Canvas Screen Space Overlay, `ScaleWithScreenSize` 1080×1920, match 0.5, font `LegacyRuntime.ttf`, palette và kiểu nút bo góc lấy từ `WoolSceneBuilder`. EventSystem dùng `InputSystemUIInputModule`.

### Lobby panel

Nút Settings góc phải trên; chữ "WOOL JAM"; Image sprite `Kitten-Wool.png` nhún nhẹ bằng R3 (theo quy tắc timer); nút PLAY lớn hiển thị số level `Current + 1` dạng `00`; nút LEVELS.

### Level Select panel

Header có nút Back và chữ "LEVELS". `ScrollRect` + `VerticalLayoutGroup` + `ContentSizeFitter`; thẻ sinh lúc runtime từ `WoolLevel[]` bằng prefab `LevelCard`. Thẻ hiển thị số `00`, `Title`, và trạng thái: đã thắng (dấu tích), đang tới (viền nổi bật), khóa (mờ + ổ khóa). Danh sách làm mới mỗi lần mở panel.

### Settings popup

Overlay mờ chặn raycast, card giữa: ba hàng toggle Sound/Music/Vibration, nút Reset progress mở hộp xác nhận Yes/No, nút Close. Reset xong làm mới nhãn PLAY và danh sách.

### Gameplay

Nút Home trong PausePanel và ResultPanel, cùng kiểu nút có sẵn, không đổi chữ/vị trí các phần tử scene-owned hiện có.

## File

Runtime (`Assets/JamJam/Runtime/`): `IWoolPrefs.cs`, `WoolPlayerPrefs.cs`, `WoolProgress.cs`, `WoolSettings.cs`, `WoolSession.cs`, `WoolLobby.cs`, `WoolLevelCard.cs`. Sửa `WoolScene.cs` (Start, thắng, Next, Home, field `HomeButtons`).

Editor (`Assets/JamJam/Editor/`):
- `WoolLobbyBuilder.cs`, menu `JamJam/Build Lobby Scene`: tạo `Lobby.unity`, prefab `Generated/Prefabs/LevelCard.prefab`, nối `WoolLevel[]` và sprite, đặt Build Settings. Chỉ ghi Lobby, LevelCard và Build Settings.
- Menu `JamJam/Add Home Buttons`: mở WoolJam, chèn hai nút Home nếu chưa có, nối vào `WoolScene`, lưu scene. Idempotent.
- Sửa `WoolSceneBuilder.cs` để rebuild sau này cũng tạo nút Home. KHÔNG chạy `WoolSceneBuilder` trên project chính.

Harness: thêm `ProgressChecks.cs`, đưa `IWoolPrefs.cs`, `WoolProgress.cs`, `WoolSettings.cs` vào `WoolJamChecks.csproj`.

Ràng buộc code: không comment; method trả reference dùng interface/abstract; factory void + out; timer dùng R3 có Dispose; giữ .meta/GUID.

## Kiểm thử

### Harness .NET (TDD, RED trước GREEN)

- Lần đầu: chỉ level 0 mở, `Cleared == 0`, `Current == 0`.
- Thắng N mở N+1; thắng lại level cũ không đổi; `MarkCleared` level khóa không đổi.
- Thắng level cuối: `IsCleared(last)`, `Current == last`, `Cleared == levelCount`.
- Migrate `JamJam.Level = k` → `Cleared == k`, key cũ bị xóa, migrate không chạy lại.
- `Reset()` về ban đầu, Settings giữ nguyên.
- Settings mặc định true, ghi/đọc lại đúng.
- Toàn bộ check cũ vẫn pass.

### Unity (Editor đang mở qua Unity CLI)

- Compile không lỗi mới; SearchDatabase ArgumentOutOfRangeException startup có sẵn được tách riêng.
- Build Settings: Lobby index 0, WoolJam index 1.
- Play Mode qua `eval` gọi `onClick`: PLAY vào đúng level; LEVELS đủ số thẻ; thẻ khóa không load; thẻ mở load đúng level; toggle lưu; Reset có xác nhận; Home ở Pause và Result về Lobby; thắng tăng `Cleared`; Next ở level cuối về Lobby; Play thẳng trong WoolJam vẫn chạy.
- Chụp Lobby, Level Select, Settings ở 1080×1920 và kiểm thêm 1080×2400 trong Game view.

Ngoài phạm vi: thiết bị Android thật, audio/rung thực, coin/kinh tế, sao.
