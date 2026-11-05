# Rút đường vào khay và tăng tốc

- Giữ điều kiện toàn làn mũi tên phải thông để chọn. Block nhích 0.5 ô theo mũi tên, thu nhỏ 1→0.45 trong đoạn này, rồi tìm đường ngắn trên lưới nửa ô. Nếu góc rẽ chưa đủ chỗ, tiếp tục thẳng đến chỗ rẽ an toàn. Bước ngay sau nudge không được quay ngược mũi tên.
- Planner xét footprint thu gọn; view và gate của blocker đang chạy cùng dùng ScaleAt. Block queued chưa chạy vẫn có scale 1 và chắn như trước. FIFO và thời điểm spool hút giữ nguyên.
- Speed tăng từ 5 lên 6 (+20%); không đổi tốc độ rồng, thời gian cuốn len hoặc level data.
- ExitDistance hiện lưu đoạn nhích bắt buộc 0.5, không còn là khoảng cách phải ra hết mép bàn.

## Kiểm chứng

- RED trên code cũ: `Initial arrow move stops after half a cell before safe routing`.
- GREEN: 153 gameplay assertions và 4121 route assertions, gồm đường rẽ sớm, tiếp tục thẳng khi chưa đủ khoảng trống, bốn hướng × ba kích thước × sáu slot, collision toàn đường và vận tốc đều. Số assertion gồm nhiều điểm lấy mẫu.
- Unity 6000.3.19f1 D3D11, bản sao cô lập, exit 0: compile/prefab/shader; click bị chặn/hợp lệ; pause/resume/retry/loss; cả ba màn chạy thắng. Kiểm tra nudge trong Play Mode: INITIAL ARROW MOVE PASS / 185 samples / all four directions.
- Model solve 11.22 / 22.08 / 41.22 giây, so với 14.98 / 31.82 / 61.22 giây của lượt arrow-first-exit trước đó. Đây là thời gian giải tự động cả màn, không phải benchmark riêng mỗi block.
- Source và ba level asset khớp byte với bản được kiểm tra. Không sửa scene, asset art hoặc GUID trong lần này.

Log: `Logs/QuickTrayValidation/`. GIF render camera Play Mode thật: `docs/quick-tray-motion.gif` (30 frame, interval 0.1 giây).

Unity SearchDatabase vẫn ghi ArgumentOutOfRangeException lúc startup ngoài gameplay như các lượt trước. Chưa test Android/thiết bị. Chưa stage/commit.
