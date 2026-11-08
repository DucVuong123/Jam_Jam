# Art pass gameplay (phần 1/3 của đợt làm lại art + game feel)

Ngày: 2026-11-08. Trạng thái: chờ user duyệt spec.

## Mục tiêu

Làm lại toàn bộ art của màn gameplay `WoolJam.unity` theo phong cách game tham khảo "Wool Crush™ - Yarn Car Jam" (Play Store `wool.match.color.sort.jam.puzzle`, video `youtube.com/watch?v=rKGCayefHnw`): len bão hòa, thân rồng bện chevron, block 3D mặt đan len, ống chỉ 3D, nền navy vân vải. Không sao chép asset của game gốc; chỉ học phong cách.

Đợt làm lại chia 3 phần, mỗi phần spec/plan riêng: (1) art gameplay — tài liệu này; (2) game feel: juice chuyển động, VFX, camera shake, haptic (không âm thanh); (3) UI reskin.

## Quyết định đã chốt với user

- Nguồn art: procedural bằng code/shader trong Unity. Nhân vật (mèo, đầu rồng) giữ sprite hiện tại; Claude viết tài liệu prompt/spec để user tự tạo sprite mới và thả vào.
- Theme: nền navy vân vải (theo ảnh store), không phải nền cỏ.
- Không đổi luật chơi, level data, kích thước lưới, vị trí slot, đường đi của level. Booster/coin của game gốc ngoài phạm vi.
- Không âm thanh trong toàn bộ đợt làm lại.
- Không stage/commit; giữ .meta/GUID; không comment trong source; method trả reference dùng interface/abstract; factory void + out; timer lặp dùng R3.

## Hướng hình ảnh

### Palette len (6 index màu dùng trong level)

| Index | Màu | Base | Shade (mặt bên/kẽ, ~70% value) |
|---|---|---|---|
| 0 | Đỏ | `#E8352F` | tối hơn ~30% |
| 1 | Vàng | `#FFCE1F` | tối hơn ~30% |
| 2 | Xanh lá | `#3CC03A` | tối hơn ~30% |
| 3 | Cyan | `#22CFE0` | tối hơn ~30% |
| 4 | Xanh dương | `#2F74EA` | tối hơn ~30% |
| 5 | Hồng | `#F04FAE` | tối hơn ~30% |

Cam dành cho đầu rồng/mèo; không dùng cho len. Material `Yarn_i`/`Thread_i` hiện có được cập nhật màu tại chỗ (giữ GUID).

### Môi trường

- Nền: mặt phẳng navy `#14234A` với texture vân vải đan lặp, vignette nhẹ ở viền màn hình.
- Đường rồng: dải nỉ xám bạc `#C9CFDA`, mép bo, hai đường chỉ khâu đứt nét trắng chạy dọc hai bên (texture UV theo chiều dài đường).
- Pad khay: ô bo góc xanh xám nhạt `#A9B6CF`; ô khóa hiện dấu `+` mờ.
- Board cushion dưới block đổi sang tông navy sáng hơn nền một bậc để tách vùng chơi.

### Camera và ánh sáng

- Camera orthographic giữ vị trí khung hình hiện có, góc pitch ~62° (hiện ~53°) để mặt trên block chiếm phần lớn và vẫn lộ mặt bên dày. orthographicSize chỉnh để toàn bộ đường, khay và board vẫn nằm trong khung ở 1080×1920 và 1080×2400.
- Ánh sáng: directional mềm, ambient cao, bóng mềm cường độ thấp (cảm giác đồ chơi).
- Post-processing qua Volume hiện có: Color Adjustments saturation tăng nhẹ, Bloom thấp (threshold cao, chỉ viền bóng), Tonemapping Neutral.

## Model

### Thân rồng (đơn vị len trên đường)

- Mỗi đơn vị len = một mảnh chevron chữ V dày, bo tròn; xếp liên tiếp dọc đường thành dải bện xương cá liền mạch. Rộng ~1.0 đơn vị.
- Texture vân đan sinh bằng code: albedo trắng để tint, normal map sợi len; kẽ giữa mảnh tối hơn.
- Giữ quy ước renderer tên bắt đầu `Yarn` để `WoolScene.Dye` nhuộm màu; giữ prefab `DragonSegment` (GUID).

### Block mũi tên (3D thay sprite 2D)

- Mesh hộp bo góc sinh theo kích thước block (W×H ô), độ dày ~0.35; cache theo cặp (W,H).
- Mặt trên: vân đan len + viền bện quanh mép. Mặt bên: shade của cùng màu, vân sọc đan dọc.
- Mũi tên trắng ngà `#FFF8EC` nổi nhẹ trên mặt trên, viền sẫm mỏng; xoay theo hướng như hiện tại.
- Tint qua MaterialPropertyBlock, dùng chung material để giữ batching.
- Giữ prefab `ArrowBlock` (GUID), `WoolBlockView` (Body, Arrow), `BoxCollider` cùng kích thước như hiện tại.
- `WoolScene` sửa phần hiển thị: thay chỗ đặt `SpriteRenderer.size/color/sortingOrder` bằng chọn mesh theo kích thước + MaterialPropertyBlock. Không đổi chọn block, nudge, ScaleAt, route, FIFO.

### Ống chỉ

- Ống chỉ nằm ngang: hai đầu tròn kem, lõi gỗ sáng, cuộn len quấn dày dần theo tiến độ (giữ cơ chế `YarnWrap` scale/rotation hiện có).
- Số đếm trắng đậm phía dưới (TextMesh hiện có, đổi font size/màu/viền nếu cần).
- Giữ prefab `Spool`, `SpoolSlot` (GUID).

### Sợi len vào ống chỉ

- LineRenderer dày hơn hiện tại, texture xoắn sợi với UV chạy theo chiều dài (`LineTextureMode.Tile`), cùng màu len. Giữ đường cong võng hiện có; độ căng/rung để phần 2.

### Nhân vật và bong bóng

- Đầu rồng, mèo: giữ sprite `Dragon-Head-Wool.png`, `Kitten-Wool.png`.
- Bong bóng quanh mèo: quả cầu trong suốt procedural với viền fresnel sáng + vệt highlight (shader URP transparent nhỏ trong `Assets/JamJam/Shaders/`).
- Tài liệu `docs/wool-character-prompts.md`: prompt, kích thước, pivot, nền trong suốt, tên file cho từng trạng thái — mèo ngồi (`Kitten-Idle.png`), sợ (`Kitten-Scared.png`), vui (`Kitten-Happy.png`); đầu rồng ngậm miệng (`Dragon-Head-Idle.png`), há miệng phun lửa (`Dragon-Head-Fire.png`). Builder dùng file mới nếu tồn tại tại `Assets/JamJam/Art/Sprites/`, ngược lại dùng sprite hiện tại. Trạng thái mới chỉ được dùng ở phần 2; phần 1 chỉ cần idle.

## Lệnh build

- Menu mới `JamJam/Rebuild Art`: chạy dựng texture/mesh/material/prefab và patch camera, ánh sáng, nền, Volume vào `WoolJam.unity` đang có. Không chạy `WoolSceneBuilder.Build`, không ghi level asset, không đổi object UI.
- `WoolSceneBuilder.Build` (rebuild đầy đủ) cũng được cập nhật để tạo cùng camera/ánh sáng/nền mới, giữ đồng bộ.
- Idempotent: chạy nhiều lần cho cùng kết quả, giữ GUID asset hiện có.

## Kiểm thử

### Gameplay không đổi
- Harness .NET PASS 153 gameplay / 4121 route / 18 progress.
- `JamJam/Validate All Levels` tìm được lời giải cả ba level.
- Play Mode qua Unity CLI: click block bị chặn rung; click hợp lệ bay về khay; ống chỉ hút len; thắng/thua; collider block giữ kích thước.

### Asset
- `Rebuild Art` hai lần liên tiếp không lỗi; GUID prefab/material hiện có không đổi (so .meta).
- `git diff` level asset rỗng; trong `WoolJam.unity` chỉ đổi object camera/ánh sáng/nền/Volume.
- Không shader lỗi (`ShaderUtil.ShaderHasError`), không lỗi console mới (lỗi SearchDatabase startup có sẵn tách riêng).

### So sánh hình ảnh
- Chụp Play Mode 1080×1920 và 1080×2400: đầu level, giữa level (đang hút len, có sợi chỉ), thắng.
- Ghép cạnh ảnh store tham khảo thành `docs/art-compare-*.png`.
- Checklist tự đánh giá: len bão hòa đọc rõ trên navy; chevron thấy được ở cỡ điện thoại; block lộ độ dày; mũi tên trắng tương phản; số ống chỉ đọc được. User duyệt cuối.

### Hiệu năng (sơ bộ, Editor)
- Ghi draw call/batches trước và sau; mục tiêu không tăng quá ~2×. Chưa đo trên Android.

## Ngoài phạm vi

Juice chuyển động, VFX, haptic (phần 2); UI reskin (phần 3); sprite nhân vật mới (user cung cấp); âm thanh; booster/coin; thay đổi level hoặc luật chơi.
