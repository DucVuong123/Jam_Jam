# Prompt ảnh UI Wool Jam

Bộ prompt để bạn tự tạo ảnh UI bằng AI rồi thả vào game. Mục tiêu là giao diện sống động như ảnh mẫu `videoframe_190.png`: nền minh họa có nhân vật, nút bóng bẩy nổi khối, icon to tròn có viền đậm, banner ruy băng. Prompt viết bằng tiếng Anh vì các tool tạo ảnh hiểu tiếng Anh tốt nhất.

Thả file vào `Assets/JamJam/Art/UI/` với **đúng tên** trong bảng. Khi có file, mình sẽ nối vào `JamJam/Rebuild UI`. File nào chưa có thì giao diện giữ bản procedural hiện tại.

## Quy tắc chung

- **Không có chữ trong ảnh nút, panel, banner.** Chữ do game vẽ bằng font Lilita One, để còn đổi nội dung và dịch được. Riêng logo được phép có chữ.
- **Nền trong suốt** (PNG 32-bit) cho mọi thứ trừ ảnh nền. Nếu tool không xuất được nền trong suốt, tạo trên nền xanh lá thuần `#00FF00` rồi xóa nền.
- **Nút, panel, banner, thanh tiến độ phải kéo giãn được (9-slice):** bốn góc trang trí, các cạnh và phần giữa phải trơn, đều màu, không có họa tiết lệch về một phía. Giữa ảnh để trống, không có biểu tượng.
- **Cùng một phong cách cho cả bộ:** luôn dán đoạn **Style chung** vào đầu mỗi prompt. Nếu tool hỗ trợ ảnh tham chiếu (style reference), dùng ảnh đầu tiên bạn ưng làm tham chiếu cho tất cả ảnh sau.
- **Bảng màu của game:**

  | Vai trò | Mã màu |
  |---|---|
  | Nền màn chơi (navy) | `#14234A` |
  | Viền tối, chữ trên nền sáng | `#0E1836` |
  | Nút chính (xanh lá) | `#3CC03A`, cạnh dưới `#23862A` |
  | Nút nổi bật, badge (cam vàng) | `#FFB21F`, cạnh dưới `#C77A0A` |
  | Nút phụ, thẻ panel (kem) | `#FFF3D6`, cạnh dưới `#D9B77A` |
  | Màu thua (đỏ cam) | `#F0603A` |

### Style chung (dán vào đầu mọi prompt)

> Casual mobile puzzle game UI asset, glossy cartoon 3D style like top-grossing match games, soft knitted wool and felt texture accents, chunky rounded shapes, thick dark navy outline (#0E1836), soft top-left studio lighting with a bright highlight on top and a darker bottom lip, vibrant saturated colors, clean edges, high detail, centered, no text, no letters, no watermark

### Negative prompt (nếu tool có ô này)

> text, letters, numbers, watermark, logo, signature, blurry, jpeg artifacts, photorealistic, harsh shadow on background, cropped edges, extra objects

## 1. Ảnh nền

| File | Kích thước | Dùng ở |
|---|---|---|
| `Lobby-Background.png` | 1080×2400, không trong suốt | màn Home |
| `Levels-Background.png` | 1080×2400, không trong suốt | màn chọn level |
| `Result-Rays.png` | 1024×1024, trong suốt | tia sáng xoay sau thẻ thắng |

**Lobby-Background**: cảnh minh họa có nhân vật, giống vai trò con thuyền trong ảnh mẫu. Để trống khoảng 18% phía trên (chỗ logo và nút Settings) và 30% phía dưới (chỗ nút PLAY, LEVELS). Nhân vật nằm ở khoảng giữa.

> [Style chung] Portrait mobile game home screen background illustration, 1080x2400. A cozy sunny knitting workshop by a big round window with blue sky and fluffy clouds outside, giant colorful yarn balls (red, yellow, green, cyan, blue, pink) piled around, a cute orange tabby kitten made of knitted wool sitting on top of a big yarn ball and waving, a friendly orange knitted wool dragon peeking out from behind a knitting basket holding knitting needles, a long rainbow yarn strand looping across the scene, warm cheerful lighting, soft depth of field. Keep the top 18% and the bottom 30% calm and simple (soft sky and wooden floor) so UI can sit on top. No text.

**Levels-Background**: nền dịu hơn, không có nhân vật lớn, để các thẻ level đọc rõ.

> [Style chung] Portrait mobile game level map background, 1080x2400, soft and low contrast. A winding knitted yarn path made of chunky stitches curving from bottom to top over a cozy felt patchwork quilt landscape in muted navy and teal tones, small decorative yarn balls, buttons and knitting needles scattered at the edges, gentle vignette, plenty of calm empty space in the middle for cards. No characters, no text.

**Result-Rays**:

> [Style chung] Radial sunburst light rays, alternating soft cream-yellow and transparent wedges, 16 rays, glowing center, perfectly centered, seamless when rotated, transparent background.

## 2. Logo

| File | Kích thước | Dùng ở |
|---|---|---|
| `Logo-WoolJam.png` | 1200×600, trong suốt | màn Home, thay chữ "WOOL JAM" |

> [Style chung, but text allowed] Game logo with the exact words "WOOL JAM" in big chunky bubbly letters made of knitted wool yarn with visible stitches, letters colored red, yellow, green, cyan, blue and pink, thick dark navy outline, glossy highlights, a small yarn ball with a needle as the dot accent, a cute orange kitten paw peeking over the letter J, slight playful tilt, transparent background.

AI hay viết sai chữ. Nếu sai, tạo lại vài lần, hoặc ghi rõ thêm `spelled W-O-O-L J-A-M`.

## 3. Nút (không chữ, kéo giãn được)

| File | Kích thước | Màu | Dùng cho |
|---|---|---|---|
| `Button-Green.png` | 512×192 | xanh lá `#3CC03A` | PLAY, NEXT, Resume, Close |
| `Button-Cream.png` | 512×192 | kem `#FFF3D6` | Retry, Home, Levels, Back, Settings |
| `Button-Orange.png` | 512×192 | cam vàng `#FFB21F` | nút nổi bật |
| `Button-Red.png` | 512×192 | đỏ cam `#F0603A` | Reset progress |
| `Button-Round-Orange.png` | 256×256 | cam vàng | nút Pause trong màn chơi |
| `Button-Round-Cream.png` | 256×256 | kem | nút Settings ở Home |

Prompt cho nút dài (đổi `<COLOR>` theo bảng):

> [Style chung] Empty mobile game button, wide pill shape 512x192, glossy <COLOR> face with a soft white highlight band across the top third, a thick darker 3D bottom lip about 14% of the height, thick dark navy outline, small subtle knitted stitch line running along the inside edge, flat even color in the middle so it can be stretched, no icon, no text, transparent background, front view.

Prompt cho nút tròn:

> [Style chung] Empty round mobile game button, perfect circle 256x256, glossy <COLOR> face with a white highlight on the top left, thick darker 3D bottom lip, thick dark navy outline, no icon, no text, transparent background, front view.

## 4. Icon

Icon 256×256, nền trong suốt, nằm giữa với lề khoảng 10%. Prompt mẫu (đổi `<SUBJECT>`):

> [Style chung] Single game icon of <SUBJECT>, chunky and rounded, glossy cream white fill with a soft shadow inside, thick dark navy outline, slight 3D bevel, centered, transparent background, readable at small size.

| File | `<SUBJECT>` |
|---|---|
| `Icon-Pause.png` | two thick rounded pause bars |
| `Icon-Play.png` | a thick rounded play triangle |
| `Icon-Settings.png` | a chunky gear with six rounded teeth |
| `Icon-Home.png` | a cute little house with a round door and a yarn ball chimney |
| `Icon-Retry.png` | a thick circular arrow looping clockwise |
| `Icon-Back.png` | a thick rounded arrow pointing left |
| `Icon-Lock.png` | a chunky padlock, golden yellow body |
| `Icon-Check.png` | a thick bold check mark, bright green fill |
| `Icon-Sound.png` | a speaker with two sound waves |
| `Icon-Music.png` | a double music note |
| `Icon-Vibration.png` | a smartphone with small shake lines on both sides |
| `Icon-Star.png` | a plump five-point star, golden yellow fill |
| `Icon-YarnBall.png` | a round yarn ball with a loose strand, orange fill |

## 5. Panel, banner, thanh tiến độ (kéo giãn được)

| File | Kích thước | Dùng ở |
|---|---|---|
| `Panel-Card.png` | 1024×1024 | thẻ Pause, Result, Settings |
| `Banner-Ribbon-Green.png` | 1024×256 | banner tiêu đề khi thắng |
| `Banner-Ribbon-Red.png` | 1024×256 | banner tiêu đề khi thua |
| `Badge-Level.png` | 384×256 | ô "LEVEL 02" góc trái màn chơi |
| `Progress-Track.png` | 1024×96 | rãnh thanh tiến độ |
| `Progress-Fill.png` | 1024×96 | phần lấp đầy thanh tiến độ |

**Panel-Card**:

> [Style chung] Empty popup card panel, rounded rectangle with 64px corner radius, warm cream (#FFF8EC) center, thick dark navy outer border, an inner border of white running stitches like a quilted fabric patch, small yarn ball decorations only at the two top corners, flat empty center so it can be stretched, no text, transparent background, front view.

**Banner-Ribbon** (đổi `<COLOR>` thành `bright green #3CC03A` hoặc `orange red #F0603A`):

> [Style chung] Empty title ribbon banner, horizontal, 1024x256, glossy <COLOR> fabric with folded tail ends on both sides, white stitched edge, thick dark navy outline, flat even middle area for text, no text, transparent background.

**Badge-Level**:

> [Style chung] Empty rounded badge plate, 384x256, glossy orange yellow (#FFB21F) with a thick darker bottom lip, a small knitted yarn ball attached at the top-left corner, thick dark navy outline, flat empty center for a number, no text, transparent background.

**Progress-Track**:

> [Style chung] Empty horizontal progress bar track, 1024x96, deep navy (#0E1836) rounded capsule with an inner shadow, thin light stitched line along the inside edge, flat middle so it can be stretched, transparent background.

**Progress-Fill**:

> [Style chung] Horizontal progress bar fill, 1024x96, rounded capsule made of twisted bright green knitted yarn with visible stitches, glossy highlight along the top, flat repeating texture so it can be stretched, transparent background.

## 6. Thẻ level

Thẻ 1024×256, kéo giãn được, không chữ. Số level và tên level do game vẽ.

| File | Trạng thái | `<LOOK>` |
|---|---|---|
| `LevelCard-Cleared.png` | đã qua | glossy bright green with a darker green bottom lip and a few tiny gold stars at the right end |
| `LevelCard-Current.png` | đang chơi | glossy orange yellow with a darker bottom lip, soft outer glow, a small knitted yarn ball at the left end |
| `LevelCard-Locked.png` | đang khóa | matte dusty navy (#2A3A66), slightly darker bottom lip, subtle felt texture |

> [Style chung] Empty wide level card plate, 1024x256, rounded rectangle, <LOOK>, thick dark navy outline, white stitched inner edge, flat empty middle for text, no text, transparent background, front view.

## 7. Nhân vật cho màn kết quả và lobby

Ảnh 1024×1024, nền trong suốt, cùng phong cách với `docs/wool-character-prompts.md` (mèo cam đan len, rồng cam đan len).

| File | Cảnh |
|---|---|
| `Result-Win-Characters.png` | mèo và rồng ôm nhau ăn mừng, xung quanh có len và confetti |
| `Result-Lose-Kitten.png` | mèo ngồi buồn, ôm cuộn len rối |
| `Lobby-Characters.png` | mèo và rồng đứng cạnh nhau vẫy tay, dùng khi chưa có `Lobby-Background` |

> [Style chung] Cute chibi orange tabby kitten made of knitted wool and a friendly orange knitted wool dragon <SCENE>, 3D toy render, visible knit stitches, big glossy eyes, soft studio lighting, saturated warm colors, thin dark outline, centered full body, transparent background, no text.

`<SCENE>` lần lượt:
- `hugging and jumping with joy, colorful yarn confetti around them`
- `the kitten sitting alone looking sad and hugging a tangled ball of yarn, small tear` (chỉ có mèo, bỏ phần rồng ở prompt)
- `standing side by side waving hello at the viewer`

## Thứ tự nên tạo

Nếu muốn thấy thay đổi rõ nhất với ít ảnh nhất, tạo theo thứ tự này:

1. `Lobby-Background.png`
2. `Logo-WoolJam.png`
3. `Button-Green.png` và `Button-Cream.png`
4. `Panel-Card.png`
5. `Banner-Ribbon-Green.png` và `Result-Rays.png`
6. Bộ icon
7. Phần còn lại

## Sau khi thả file

1. Copy file đúng tên vào `Assets/JamJam/Art/UI/`.
2. Báo mình. Mình sẽ cho `JamJam/Rebuild UI` dùng ảnh nếu có (ảnh nền, logo, sprite 9-slice cho nút, panel, banner, icon cạnh chữ, tia sáng xoay sau thẻ thắng, nhân vật ở màn kết quả), rồi chụp lại toàn bộ màn hình để so sánh.
3. Ảnh nào chưa có thì giữ nguyên giao diện procedural hiện tại.
