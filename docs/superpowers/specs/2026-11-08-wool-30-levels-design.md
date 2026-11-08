# 30 level, bảng trộn khối thẳng + khối chéo, 5 kiểu đường rồng

Ngày: 2026-11-08. Yêu cầu: "làm cho tôi 30 level chơi dc feeling tốt, tôi thấy 2 link tham khảo tôi gửi bọn nó xếp các khối chéo dc nữa". Ảnh tham khảo: `C:\Users\Admin\Downloads\videoframe_11278.png`.

User chốt:
- **Trộn khối thẳng và khối chéo trong cùng một bảng.**
- **Vài kiểu đường rồng.**

User duyệt thiết kế ("duyệt") và cho chạy thẳng tới hết.

## Nguyên tắc

- Luật chơi giữ nguyên ý nghĩa:
  - Khối chọn được khi cả làn mũi tên tới ra khỏi bảng trống.
  - Khối đang chạy chiếm vùng quét của nó.
  - Khối chạy thẳng ra, rồi theo vành đai về ô khay.
  - Nhiều khối chạy cùng lúc, có nhường đường.
  - Thứ tự hút len giữ nguyên.
  - Luật spool/khay/rồng không đổi.
- Chỉ mở rộng **hình học**: khối có thể xoay 45°, vị trí cho phép lẻ nửa ô.
- Với khối thẳng, mọi kết quả (chọn được/không, đường đi) phải giống hệt luật lưới cũ.
- Quy tắc code:
  - Không comment.
  - Method trả reference dùng interface/abstract; factory dùng void + out.
  - Timer chỉ dùng R3.
  - Giữ GUID; giữ chuỗi scene-owned.
  - Không commit.
  - Không chạy `WoolSceneBuilder.Build`/`WoolLobbyBuilder.Build`/`RunBatch`.
  - `WoolSceneBuilder` sửa cho cùng kết quả (dùng generator), chỉ cần compile.
- Level asset được phép ghi lại (user yêu cầu 30 level mới).
  - `Level_1…3` giữ đường dẫn và GUID, ghi đè nội dung.
  - `Level_4…30` tạo mới.

## 1. Dữ liệu và hình học khối

- `ExitDirection` thêm `UpRight = 4, DownRight = 5, DownLeft = 6, UpLeft = 7` (giữ 0–3 cũ).
  - Vector hướng theo đơn vị ô (x phải, y lên): (1,1)/√2, (1,−1)/√2, (−1,−1)/√2, (−1,1)/√2.
- `BlockDefinition.X`, `Y` đổi kiểu `int` → `float`. Unity đọc được dữ liệu cũ dạng số nguyên.
- Footprint của mọi khối là hình chữ nhật `Width × Height`, tâm `(X + Width/2, Y + Height/2)`, xoay góc θ quanh tâm:
  - θ = 0 với 4 hướng thẳng;
  - θ = +45° (ngược kim đồng hồ) với `UpRight`/`DownLeft`;
  - θ = −45° với `UpLeft`/`DownRight`.
  - Vì vậy với `Width > Height`, mũi tên chéo nằm dọc cạnh dài.
- Khối thẳng: `X`, `Y` là số nguyên như cũ. Khối chéo: tâm nằm trên lưới nửa ô.
- Bảng: hình chữ nhật `[0, Columns] × [0, Rows]` theo đơn vị ô. Footprint phải nằm trọn trong bảng (sai số 1e-4).
- Thêm `WoolShape` (thuần, có trong harness):
  - footprint dạng đa giác lồi 4 đỉnh;
  - vùng quét (bao lồi của footprint ở vị trí đầu và cuối);
  - kiểm cắt nhau SAT với epsilon 1e-4 (chạm cạnh không tính là cắt);
  - khoảng cách quét cần để footprint ra trọn khỏi bảng.
- `WoolGame` dùng `WoolShape` cho:
  - làn trống (khối Phase 0 = footprint, khối Phase 1 còn trên bảng = vùng quét còn lại);
  - `Validate` (chồng nhau, ra ngoài bảng, hướng hợp lệ 0–7);
  - suy vị trí của khối đang chạy.
- Lưới cũ (`IsClear`) bị thay thế. Harness so hai cách trên ≥ 2000 bảng ngẫu nhiên chỉ có khối thẳng: `CanSelect` phải trùng 100%.

## 2. Đường đi

- Khối thẳng: như cũ.
- Khối chéo:
  - chạy thẳng theo hướng chéo cho tới khi tâm chạm hình chữ nhật vành đai (bảng giãn `RingOffset`);
  - với khối chéo, `MaxHalf` = nửa đường chéo footprint × `CellSize`;
  - sau đó đi dọc vành đai về cạnh trên theo hướng ngắn hơn, rồi theo làn trên tới X của ô, vào ô.
- Mọi điểm sau đoạn đầu nằm ngoài bảng. Validate "Board too tall for the tray lane" giữ nguyên, dùng `MaxHalf` mới.
- Nhường đường trên vành đai giữ luật hiện có.

## 3. Hiển thị

- Khối chéo khi đứng yên xoay yaw theo θ, mũi tên chỉ theo hướng chéo.
- Khi chạy, khối quay đầu theo hướng di chuyển (công thức heading dùng góc của hướng 0–7 thay cho `Direction*90`).
- Collider xoay theo khối.

## 4. Đường rồng

- 5 hình: `S` (đường hiện tại), `U`, `Spiral`, `Zigzag`, `Hook`.
- Mỗi hình là danh sách điểm điều khiển, nội suy Catmull-Rom như hiện tại.
- Mọi điểm nằm trong vùng sân cỏ: x ∈ [−4.0, 3.8], z ∈ [0.67, 8.09], cùng hộp với đường hiện tại.
- Độ dài đường 26–40.
- Check `Stage` bao cả 5 hình và mèo ở cuối đường.

## 5. Generator 30 level

- Thêm menu `JamJam/Generate Levels`, tất định theo seed (cùng code ra cùng asset).
  - Ghi `Level_1…30`.
  - Gán `WoolScene.Levels` trong `WoolJam.unity` và `WoolLobby.Levels` trong `Lobby.unity`, theo thứ tự.
  - Chạy hai lần ra file giống hệt.
- Bảng độ khó (tham số theo level, ghi thành bảng hằng):

  | Level | Bảng (cột×hàng tối đa) | Số khối | Màu | Tỉ lệ khối chéo | Mục tiêu đường đi khi bộ giải thắng |
  |---|---|---|---|---|---|
  | 1–3 | 4×4 → 6×4 | 6–10 | 2–3 | 0 | 0.40–0.50 |
  | 4–8 | 6×5 → 8×5 | 12–18 | 4 | 0 | 0.50–0.62 |
  | 9–12 | 8×5 → 8×6 | 16–20 | 4–5 | 0.15–0.25 | 0.55–0.68 |
  | 13–24 | 8×6 | 18–24 | 5–6 | 0.25–0.45 | 0.62–0.80 |
  | 25–30 | 8×6 | 22–26 | 6 | 0.40–0.50 | 0.78–0.88 |

  - Level 5, 10, 15, 20, 25 là level "nghỉ": mục tiêu thấp hơn level trước 0.08, số khối ít hơn khoảng 20%.
  - Đường rồng luân phiên `S, U, Zigzag, Hook, Spiral` theo level.
- Xếp khối theo thứ tự gỡ ngược:
  - Mỗi khối mới chỉ được đặt nếu footprint không chồng khối đã đặt **và** làn của nó (vùng quét ra khỏi bảng) không cắt footprint khối đã đặt.
  - Ứng viên lấy mẫu theo seed trên lưới nửa ô (khối thẳng trên lưới nguyên), chọn ứng viên có tổng độ dài cạnh tiếp xúc với khối khác lớn nhất (bảng dày).
  - Khối 2×1 hoặc 1×2.
- Màu và len:
  - Màu chia đều cho khối.
  - Capacity 3–6.
  - `Yarn` = len của các khối theo thứ tự gỡ, xáo trong cửa sổ `k` (k = 0 ở level 1, tăng tới 6 ở level 30).
- Chỉnh độ khó:
  - Bộ giải tham lam (như `ValidateLevels`) chơi level, đo `max(Head/PathLength)` khi thắng.
  - Tìm `Speed` bằng chia đôi để tỉ lệ này lệch mục tiêu ≤ 0.03.
  - Bộ giải không thắng ở tốc độ tối thiểu 0.12 thì đổi seed (tối đa 50 seed). Hết seed thì báo lỗi và không ghi.
  - `InitialHead`, `Spacing` lấy theo bảng.
- Tên level: 30 tên tiếng Anh riêng (theo phong cách "First Threads", "Color Traffic"…).

## 6. Màn chọn level

- `LevelsPanel` dùng ScrollRect dọc, 30 thẻ trong content có layout dọc.
- Mở màn thì tự cuộn tới level hiện tại.
- Thẻ, trạng thái và tiến độ như cũ. `WoolProgress` đã hỗ trợ số level bất kỳ.

## Kiểm thử

- Harness .NET:
  - `WoolShape` (SAT, vùng quét, khoảng cách ra khỏi bảng);
  - so luật lưới cũ ↔ mới trên khối thẳng;
  - làn chéo bị chặn hoặc trống đúng trên bảng mẫu;
  - đường chéo: đoạn đầu cùng hướng chéo, sau đó mọi điểm ngoài bảng, điểm cuối = tâm ô;
  - generator (phần thuần) tất định và mọi bảng có lời giải theo thứ tự gỡ;
  - dòng PASS mới ghi lại.
- `Validate All Levels` cho cả 30 level:
  - hợp lệ;
  - bộ giải thắng;
  - tỉ lệ đường đi trong ±0.05 mục tiêu;
  - level 9+ có khối chéo;
  - có đủ 5 hình đường.
- Editor: `Stage` cho 30 level × mọi khối × mọi ô ở 3 tỉ lệ; Generate hai lần ra file giống hệt.
- Play Mode:
  - level có khối chéo: chọn khối chéo hợp lệ thì đoạn đầu chạy chéo, tới đúng ô, xoay đúng;
  - khối chéo bị chặn thì không đi;
  - Lobby cuộn được, tự cuộn tới level hiện tại, bấm thẻ 30 (khi mở khóa) vào đúng level;
  - các check cũ PASS.
- Ảnh level 1, 10, 20, 30 ở 1080×1920 → `docs/levels-*.png`, ghép với ảnh mẫu → `docs/levels-compare.png`.
