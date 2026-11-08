# Prompt sprite nhân vật Wool Jam

Tài liệu cho user tự tạo sprite nhân vật mới (AI image hoặc vẽ tay) rồi thả vào `Assets/JamJam/Art/Sprites/`. Builder (`JamJam/Rebuild Art`) tự dùng file mới nếu tồn tại; nếu chưa có thì giữ `Kitten-Wool.png` và `Dragon-Head-Wool.png`.

## Quy cách chung

- PNG 32-bit, nền trong suốt hoàn toàn, không bóng đổ xuống nền, không chữ, không viền trắng quanh nhân vật.
- 1024×1024 px, nhân vật nằm giữa, chừa lề trong suốt ~6% mỗi cạnh.
- Phong cách: đồ chơi len 3D dễ thương, bề mặt đan len/nỉ thấy rõ sợi, ánh sáng studio mềm từ trên-trái, màu bão hòa, đường viền sẫm mỏng giúp đọc rõ trên nền navy `#14234A`.
- Không dùng các màu len gameplay (đỏ `#E8352F`, vàng `#FFCE1F`, xanh lá `#3CC03A`, cyan `#22CFE0`, xanh dương `#2F74EA`, hồng `#F04FAE`) làm màu chủ đạo của nhân vật; nhân vật dùng tông cam/kem.
- Các trạng thái của cùng nhân vật phải cùng kích thước canvas, cùng tỉ lệ, cùng khung hình để khi đổi sprite không bị nhảy vị trí trên màn hình.

## Mèo con

Pivot `(0.5, 0.06)`: chân mèo chạm điểm cách đáy ảnh 6%, chính giữa theo chiều ngang. Mèo cao ~85% chiều cao ảnh. Builder hiển thị cao 1.5 đơn vị, bên trong bong bóng trong suốt.

| File | Trạng thái | Dùng ở |
|---|---|---|
| `Kitten-Idle.png` | ngồi, nhìn thẳng, mỉm cười nhẹ | phần 1 (idle) |
| `Kitten-Scared.png` | ngồi co lại, tai cụp, mắt to lo lắng | phần 2 |
| `Kitten-Happy.png` | hai chân trước giơ lên, mắt cười híp | phần 2 |

Prompt gốc (thêm mô tả trạng thái ở cuối):

> Cute chibi orange tabby kitten made of knitted wool yarn, visible knit stitches and soft felt texture, 3D toy render, sitting, front view, big glossy eyes, small pink nose, cream belly and paws, soft studio lighting from top left, saturated warm colors, thin dark outline, centered, full body, transparent background, no shadow, no text, 1024x1024

- Idle: `sitting calmly, gentle smile, looking at the viewer`
- Scared: `crouching, ears flattened, worried wide eyes, tiny sweat drop`
- Happy: `both front paws raised in joy, eyes closed in a big smile`

## Đầu rồng

Pivot `(0.35, 0.7)`: tâm đầu / điểm xoay gốc, khoảng phía sau mắt, cách mép trái 35% và cách đáy 70%. Mũi rồng hướng xuống-phải khoảng 45–55° dưới ngang, giống như `Dragon-Head-Wool.png` hiện tại — mở file đó lên để so khớp góc độ nghiêng. `WoolScene` cộng thêm 55° để bù lại. Builder hiển thị cao 2.1 đơn vị.

| File | Trạng thái | Dùng ở |
|---|---|---|
| `Dragon-Head-Idle.png` | ngậm miệng, mắt mở, nhìn về phía trước | phần 1 (idle) |
| `Dragon-Head-Fire.png` | há miệng, phun lửa nhỏ màu cam-vàng | phần 2 |

Prompt gốc:

> Cute chibi dragon head made of knitted orange wool yarn, visible knit stitches, small cream horns and yellow felt spikes, big friendly eyes, 3D toy render, three-quarter view, head tilted so the snout points to the lower right at about 50 degrees below horizontal, soft studio lighting from top left, saturated warm colors, thin dark outline, head and short neck only, transparent background, no shadow, no text, 1024x1024

- Idle: `mouth closed, calm confident expression`
- Fire: `mouth wide open breathing a small stylized orange and yellow flame toward the right`

## Kiểm tra sau khi thả file

1. Copy file đúng tên vào `Assets/JamJam/Art/Sprites/`.
2. Chạy menu `JamJam/Rebuild Art` (không chạy `Build Wool Jam Scene`).
3. Play `WoolJam.unity`: mèo đứng giữa bong bóng, chân ở đáy bong bóng; đầu rồng nối liền với đốt chevron đầu tiên và xoay mượt theo đường.
4. Chạy check `JamJam.Editor.WoolArtChecks.Tray()` — check này xác nhận bong bóng chứa trọn sprite mèo mới; nếu FAIL, chỉnh `BubbleScale`/`BubbleOffsetY` trong `WoolAssetsBuilder.cs`.
