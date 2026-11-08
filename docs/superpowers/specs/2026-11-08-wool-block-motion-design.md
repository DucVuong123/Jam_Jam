# Khối trên bảng: kích thước, khoảng cách, đường đi, đi song song

Ngày: 2026-11-08. Yêu cầu user: "các khối hiện tại đang quá to, đứng sát nhau quá, lúc đi ra tự dưng bị scale nhỏ lại nhìn rất dở hơi, hiện tại chỉ có thể cho một khối đi ra chờ khối đó đi xong mới dc bấm khối khác, như thế là sai logic". Tham khảo: `youtube.com/shorts/02_LZPnZ4js` (car-jam: xe nhỏ có khe hở, chạy thẳng ra khỏi bãi theo mũi tên ở nguyên kích thước, vòng ra ngoài đến ô đỗ; nhiều xe chạy cùng lúc). User cho phép chạy thẳng không cần duyệt từng bước.

Thay luật đã khóa trước đây (nudge 0.5 ô + scale 0.45, FIFO một khối một lúc).

## Luật mới

1. **Chọn được** khi: đang Playing, có slot mở trống, và toàn bộ làn mũi tên từ khối tới mép bảng trống — tính cả khối đang đứng (Phase 0) và khối đang chạy (Phase 1). Khối Phase 1 còn chồng lên bảng chiếm cả **vùng quét** từ vị trí hiện tại tới mép bảng theo mũi tên của nó (cắt theo bảng), nên hai khối đang chạy không cắt nhau trong bảng. Giữ nguyên quy tắc chọn slot (slot mở trống đầu tiên) và đặt chỗ slot ngay khi chọn.
2. **Đường đi** (thay BFS co nhỏ): từ tâm khối đi thẳng theo mũi tên tới khi toàn bộ khối ra khỏi mép bảng + `ExitMargin`, rồi đi theo **vành đai ngoài bảng** (hình chữ nhật bao bảng giãn `ExitMargin`) về cạnh trên (phía khay) theo hướng ngắn hơn, rồi theo **làn trên theo từng khối** (`đỉnh bảng + RingOffset(khối)`) ngang tới X của slot, rồi vào slot. Mọi đoạn sau khi ra khỏi làn đều nằm ngoài bảng nên không cần tìm đường né khối. Chỉ đoạn thẳng góc (Manhattan) như hiện tại.
3. **Không scale**: khối giữ nguyên kích thước suốt đường đi (bỏ `ScaleAt`/`TravelScale`/`ArrowDistance` nudge). `ExitDistance` = độ dài đoạn thẳng ra khỏi bảng.
4. **Đi song song**: mọi khối Phase 1 cùng tiến mỗi `Step` (bỏ hàng đợi `departures`). Mỗi spool có `Arrival` riêng = thời gian còn lại của khối của nó. Thứ tự ưu tiên hút len của spool (`spoolOrder`) giữ nguyên.
5. **Tốc độ**: `Speed` 6 → 9 đơn vị/giây (đường vòng dài hơn); con số chốt trong plan sau khi đo độ dài đường của 3 level (mục tiêu: chuyến xa nhất ≤ 2.5s).

## Kích thước và khe

- Thêm `WoolRouting.CellSize` (đơn vị thế giới cho mỗi ô) = **0.7**; mọi chuyển đổi ô → thế giới (tâm khối, điểm đường, mép bảng) nhân `CellSize`. Logic va chạm vẫn theo đơn vị ô.
- Khe giữa khối: mesh khối inset mỗi cạnh **0.09 ô** (khe ≈ 0.126 thế giới giữa hai khối kề nhau), bo góc nhỏ hơn tương ứng; collider theo kích thước hiển thị.
- Giới hạn làn khay: `WoolGame.Validate` thêm lỗi "Board too tall for the tray lane" (một lần) khi với một khối nào đó `đỉnh bảng + RingOffset + max(w,h)*CellSize/2 > TrayZ - 0.41 - 0.05` (0.41 = nửa độ sâu pad slot). Bảng cao ≈ 7 hàng trở lên có khối dài bị từ chối; cả ba level hiện tại đều hợp lệ.
- Mũi tên, độ dày 0.35, vân len, MPB giữ nguyên tỉ lệ theo kích thước mới.
- Board cushion co theo kích thước bảng của level (`Columns×Rows×CellSize` + lề 0.5), không còn kích thước cố định.
- Khung hình: toàn bộ bảng, vành đai đường đi và khay nằm trong khung ở 1080×1920 và 1080×2400 (mở rộng check `Stage` để bao cả điểm vành đai của mọi khối trong 3 level).

## Hiển thị chuyển động

- Bỏ nhấc cung khi bay (`FlightLift`) — xe chạy phẳng trên mặt bàn như tham khảo; giữ pop nhẹ lúc bắt đầu (`LaunchPop` 1.18 → 1.08) và Puff bụi ở vị trí xuất phát.
- Khối xoay theo hướng chạy (thân + mũi tên) để đầu xe hướng về phía đang đi; mũi tên giữ hướng chạy hiện tại.
- Các khối đang chạy không cần né nhau trên vành đai (chấp nhận chồng nhẹ như tham khảo).

## Không đổi

Level data, kích thước lưới, luật màu/spool/len, khay, FIFO hút len theo `spoolOrder`, game feel/UI khác. Không commit; không comment; quy tắc code như cũ; không chạy `WoolSceneBuilder.Build`.

## Kiểm thử

- Harness .NET: viết lại các check FIFO/scale/BFS cũ thành check luật mới:
  - hai khối chọn liên tiếp đều có `Travel` tăng cùng lúc ngay frame đầu; mỗi spool `Arrival` giảm độc lập;
  - đường đi: điểm đầu = tâm khối, đoạn đầu cùng hướng mũi tên, mọi điểm sau đoạn đầu nằm ngoài hình chữ nhật bảng, điểm cuối = tâm slot, toàn Manhattan;
  - khối bị chặn bởi khối đang chạy ngang làn → không chọn được; khi khối kia rời làn → chọn được;
  - không còn `ScaleAt` (không còn API).
  - Dòng PASS mới (số assertion thay đổi, ghi lại).
- `Validate All Levels`: cả ba level còn lời giải với luật mới.
- Play Mode: chọn hai khối hợp lệ liên tiếp → cả hai cùng di chuyển trong cùng frame; scale khối = 1 suốt đường; khối tới đúng slot; thắng/thua; `PlayFeel`, `PlayUi`, `PlayBlocks` cập nhật theo luật mới.
- Ảnh 1080×1920: đầu level và lúc 2–3 khối đang chạy cùng lúc; ghép so với khung hình tham khảo `scratchpad/ref/yt2/oar2.jpg` → `docs/motion-compare.png`.
