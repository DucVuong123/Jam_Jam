# Nối ảnh UI do user tạo vào game

Ngày: 2026-11-08. User đã tạo 35 ảnh theo `docs/wool-ui-art-prompts.md` và thả vào `Assets/JamJam/Art/UI/` (kèm `Preview.png`, `README.txt`, `prompts.json`, `validation.json` — không dùng, để nguyên). Yêu cầu gốc: "các UX UI bây giờ vẫn đang hơi chán không có sống động gì cả". Chạy thẳng không cần duyệt (theo cách user làm việc từ phần 2).

## Nguyên tắc

- `JamJam/Rebuild UI` dùng ảnh nếu file tồn tại; file nào thiếu thì giữ style procedural hiện tại (mỗi ảnh fallback độc lập). Không xóa sprite procedural.
- Không đổi chuỗi chữ scene-owned (có thể ẩn Text khi ảnh thay thế, ví dụ chữ "II" của Pause hay chữ "WOOL JAM" khi có logo — chuỗi vẫn giữ trong scene), không đổi wiring nút, logic lobby/progress, gameplay, level.
- Giữ GUID file của user (chỉ đổi import settings trong .meta qua `TextureImporter`). Không sửa pixel ảnh của user.
- Quy tắc code: không comment, interface/abstract cho method trả reference, factory void + out, timer/animation R3, không commit, không chạy `WoolSceneBuilder.Build`/`RunBatch`/build Lobby đầy đủ. Builder đầy đủ được cập nhật để cùng kết quả.

## Import

- Mọi PNG trong `Art/UI` (trừ `Preview.png`): Sprite (Single), không mipmap, `alphaIsTransparency`, Clamp, Bilinear, Max Size 2048 cho hai background, 1024 cho phần còn lại.
- 9-slice: đặt `spriteBorder` cho nút dài, nút tròn (không slice, dùng Simple), panel, banner, badge, thanh tiến độ, thẻ level — giá trị đo từ ảnh thật (góc trang trí + bán kính bo), ghi thành hằng trong code import. Thẻ/banner có trang trí ở đầu (cuộn len, đuôi ruy băng) phải nằm trọn trong border.
- `pixelsPerUnitMultiplier` theo chiều cao hiển thị để góc giữ tỉ lệ (như helper `RoundPills` hiện có).

## Áp dụng theo màn hình

### Màn chơi
- Badge level: `Badge-Level` (sliced), số và chữ LEVEL giữ.
- Nút Pause: `Button-Round-Orange` + `Icon-Pause` (ẩn Text "II").
- Thanh tiến độ: `Progress-Track` (sliced) + `Progress-Fill` (sliced); fill điều khiển bằng chiều rộng (anchor) thay `fillAmount` để không méo đầu bo — đổi chỗ cập nhật trong `WoolScene`.
- Nút Retry: `Button-Cream` + `Icon-Retry` bên trái chữ.
- Pause: thẻ `Panel-Card`; Resume `Button-Green` + `Icon-Play`; Home `Button-Cream` + `Icon-Home`.
- Kết quả:
  - Thẻ `Panel-Card`; tiêu đề nằm trên `Banner-Ribbon-Green` (thắng) / `Banner-Ribbon-Red` (thua) đặt chồng mép trên thẻ, chữ tiêu đề trắng viền navy.
  - `Result-Rays` sau thẻ, xoay chậm (~20°/s, R3, thời gian bỏ qua timeScale) khi thắng; ẩn khi thua.
  - `Result-Win-Characters` (thắng) / `Result-Lose-Kitten` (thua) ngồi trên mép trên thẻ, phía sau banner, cao ~420 px.
  - Next `Button-Green` + `Icon-Play`, Play Again `Button-Cream` + `Icon-Retry`, Home `Button-Cream` + `Icon-Home`.

### Lobby
- Home: `Lobby-Background` full màn hình (phủ, giữ tỉ lệ, neo dưới — sàn gỗ ở dưới) sau mọi UI; `Logo-WoolJam` thay chữ "WOOL JAM" (ẩn Text), phụ đề trắng viền navy dưới logo; ẩn sprite mèo procedural nếu có background; PLAY `Button-Green` + `Icon-Play`; LEVELS `Button-Cream`; Settings `Button-Round-Cream` + `Icon-Settings` (ẩn chữ nếu nút thành tròn, hoặc giữ pill cream + icon — chọn tròn ở góc trên phải như ảnh mẫu).
- Chọn level: `Levels-Background`; thẻ `LevelCard-Cleared/Current/Locked` (sliced) theo trạng thái; `Icon-Check`, `Icon-Lock`, `Icon-Play` thay sprite procedural trong badge; Back `Button-Cream` + `Icon-Back`.
- Settings: thẻ `Panel-Card`; mỗi hàng toggle có `Icon-Sound` / `Icon-Music` / `Icon-Vibration` bên trái nhãn; Reset `Button-Red`; Close `Button-Green`.

### Không dùng ở lần này
`Button-Orange`, `Icon-Star`, `Icon-YarnBall`, `Lobby-Characters` (background đã có nhân vật) — giữ trong thư mục cho sau.

## Chuyển động thêm (cho "sống động")
- Rays xoay (trên).
- Nhân vật thắng nảy nhẹ lên xuống (biên độ 12 px, chu kỳ 1.6 s), mèo thua lắc nhẹ — R3, bỏ qua timeScale, dispose khi disable.
- Logo Home "thở" scale 1↔1.03 chu kỳ 2.4 s.
- Nút PLAY Home nhịp scale 1↔1.05 chu kỳ 1.2 s (không đụng `WoolButtonJuice` — đặt trên transform khác).

## Kiểm thử
- Editor check `UiArt()`: mọi file dùng được import đúng (Sprite, border ≠ 0 cho ảnh slice), `Rebuild UI` gán đúng sprite vào đúng object ở hai scene + LevelCard prefab; khi tạm "không có ảnh" (tham số check giả lập thiếu file) builder vẫn rơi về procedural.
- `UiGame`, `UiLobby`, `UiAssets` cập nhật: ràng buộc chữ/contrast giữ; touch target ≥ 96.
- Chuỗi chữ giữ nguyên (so `m_Text` trước/sau); GUID ảnh user không đổi; `Rebuild UI` hai lần idempotent.
- Play Mode: `PlayUi`, `PlayResult*`, `PlayLobby`, `PlayFeel` PASS; tiến độ 0/50/100% hiển thị đúng.
- Ảnh 1080×1920 và 1080×2400: Home, Levels, Settings, game, Pause, Won, Lost → `docs/ui-art-*.png`, ghép so với `videoframe_190.png` (user cung cấp) → `docs/ui-art-compare.png`.
