# Màn chơi bố cục mới theo ảnh mẫu (sân rồng trên, khay giữa, bảng khối to dưới)

Ngày: 2026-11-08. Ảnh mẫu user cung cấp: `C:\Users\Admin\Downloads\videoframe_11278.png`. Yêu cầu: "tôi muốn thiết kế lại UX UI feeling trong game play thật tốt hơn, UI UX như ở trong ảnh". Phạm vi user chốt: **chỉ hình ảnh + feel, thêm nút tốc độ x1/x2**. Không booster, không khối xích/khóa số, không đổi luật chơi, không đổi dữ liệu level. User duyệt thiết kế ("duyệt") và cho chạy thẳng tới hết.

## Nguyên tắc

- Logic `WoolGame`/`WoolRouting` giữ nguyên luật: chọn khối, lane, route vòng, đi song song, nhường, spool/FIFO, PathLength, tốc độ rồng. Chỉ đổi hằng số hình học (`CellSize`, `BoardCenterZ`, `TrayZ`, `SlotSpacing`) nếu cần để bố cục khớp, và các check/validator đi theo.
- Không sửa level asset (`Generated/Levels/*.asset`). Đường rồng chỉ được biến đổi khi hiển thị.
- Chuỗi chữ scene-owned giữ nguyên: chữ không còn dùng trên màn chơi (tiêu đề, phụ đề, hướng dẫn, "THREAD TRAY") bị ẩn (`SetActive(false)`), không xóa. Đối tượng UI mới (nút tốc độ, nút Unlock) do `JamJam/Rebuild UI` tạo và sở hữu.
- Quy tắc code: không comment, method trả reference dùng interface/abstract, factory void + out, timer/animation R3, giữ GUID/.meta, không commit, không chạy `WoolSceneBuilder.Build`/`RunBatch`. Menu `JamJam/Rebuild Art` và `JamJam/Rebuild UI` cập nhật để tạo ra kết quả mới, chạy hai lần cho ra file giống hệt.

## 1. Bố cục (1080×1920, cả 1080×2400)

| Vùng | Tỉ lệ chiều cao (từ trên) | Nội dung |
|---|---|---|
| HUD | 0–8% | Trái: nút tròn bánh răng (là `PauseButton` cũ, đổi icon `Icon-Settings`, mở Pause), dưới nó chữ "Level N" (`LevelNumber`) và dòng "● NN%" (`ProgressNumber` + chấm cuộn len nhỏ). Phải: nút tốc độ pill "▶▶ x1". |
| Sân rồng | ~8–40% | Nền cỏ xanh bạc hà, đường cát viền khâu nét đứt, rồng, mèo trong bong bóng. |
| Khay | ~40–52% | Kệ gỗ sáng chạy ngang có các ô; spool nằm trên ô, số len còn lại dưới ô. Ô khóa có nút "Unlock +" ngay dưới ô (bấm = mở ô như cơ chế hiện tại). |
| Bảng khối | ~52–95% | Nền kem/be. Bảng khối rộng ~80% chiều ngang màn hình ở level 8 cột; level nhỏ hơn thì khối to như nhau, bảng nhỏ hơn và căn giữa. |

- Bỏ khỏi màn chơi (ẩn): tiêu đề WOOL JAM, phụ đề, thanh tiến độ dài (`ProgressTrack`, `ProgressFill` vẫn tồn tại và vẫn được cập nhật nhưng ẩn), dòng "TAP A CLEAR ARROW…", dòng "Need room? Tap a + slot", chữ "THREAD TRAY".
- `RetryButton` chuyển vào thẻ Pause, giữa Resume và Home (giữ object, wiring, chữ). Thứ tự: Resume, Retry, Home.
- Hai tỉ lệ màn hình: HUD neo trên, phần thế giới căn theo camera; ở 1080×2400 dư chiều cao thì nới nền (cỏ trên, kem dưới), không cắt nội dung.

## 2. Biến đổi đường rồng khi hiển thị

- Thêm `WoolStage` (thuần, có trong harness): `PathScale`, `PathOffset` (x, z) và `ToWorld(Vector3 levelPoint)`; `WoolScene` dùng nó khi dựng mesh đường, đặt mèo, đặt rồng và các đơn vị len. Logic khoảng cách trên đường không đổi (vẫn tính theo đơn vị level); chỉ vị trí hiển thị được co lại.
- Giá trị scale/offset chốt trong plan sau khi đo, sao cho đường (rộng 1.32 sau scale) và mèo nằm trọn trong vùng sân rồng ở cả hai tỉ lệ.
- Độ rộng đường, kích thước đơn vị len, đầu rồng và mèo co theo cùng `PathScale` (để không chồng lên nhau khi đường co).

## 3. Camera, khối, bảng

- Camera giữ orthographic; góc nghiêng `CameraPitch` 62° → khoảng 50° để thấy mặt bên khối. `CameraSize`/`CameraTarget` chốt trong plan theo bố cục mục 1. `WoolFeel` (rung, zoom) không đổi cách làm.
- `WoolBlockMesh.Thickness` 0.35 → khoảng 0.6 ô (theo `CellSize`); mặt bên tối hơn mặt trên (vertex color hoặc hệ số trong shader/material hiện có), vân len phủ cả mặt bên; mũi tên trắng nổi trên mặt trên giữ nguyên.
- `CellSize` 0.7 tăng lên để bảng 8 cột rộng ~80% màn hình; route vòng ngoài bảng, làn trên và khay vẫn nằm trong khung (check `Stage` mở rộng). Nếu làn khay không đủ chỗ thì dời `TrayZ`/`BoardCenterZ` thay vì thu nhỏ khối. Validator "Board too tall for the tray lane" vẫn giữ, cả 3 level phải hợp lệ.
- Tấm nỉ navy dưới bảng (`Board Cushion`) đổi thành tấm kem nhạt mỏng, viền khâu (hoặc ẩn nếu nhìn rối hơn — chốt bằng ảnh chụp trong plan).

## 4. Khay và sợi len

- Kệ gỗ procedural: thanh gỗ màu mật ong sáng dưới các ô; ô mở màu be nhạt; ô khóa xám be, có nút UI "Unlock +" (pill nâu nhạt, dấu + xanh) bám theo vị trí ô trên màn hình (tính bằng camera mỗi khi layout/level đổi, không mỗi frame). Bấm nút gọi `WoolScene.Unlock()` (luật mở ô không đổi; chỉ ô khóa đầu tiên hiện nút bấm được, các ô khóa sau hiện nút mờ không bấm được — giống luật mở tuần tự hiện tại).
- Spool nằm ngang (trục X) trên ô; số len còn lại hiển thị dưới ô (dùng `spoolLabels` hiện có, đổi vị trí).
- Sợi len: dày hơn (độ rộng LineRenderer khoảng gấp đôi), võng xuống theo trọng lực giữa spool và đơn vị len đang kéo, nối từ spool tới đúng đơn vị len đang bị gỡ. Giữ hiện liên tục khi spool còn len: giữa hai lần kéo sợi trôi mượt (lerp) tới đơn vị kế tiếp thay vì tắt/bật; ẩn khi spool hết len hoặc không còn đơn vị cùng màu.

## 5. Nút tốc độ x1/x2

- `WoolScene.Speed` (1 hoặc 2), `ToggleSpeed()`; `Tick` nhân `delta` với `Speed` trước khi gọi `State.Step`, `feel.Step`, `RenderFrame`. `Tick` vẫn kẹp delta thực ở 0.1 giây như cũ; nếu delta·Speed > 0.1 thì `State.Step` được gọi nhiều lần, mỗi lần ≤ 0.1 giây, để x2 không bị mất thời gian do kẹp trong `WoolGame.Step`.
- Giữ giá trị trong phiên (static), không lưu PlayerPrefs; mở lại game là x1. Đang Pause thì Step không chạy như cũ. Màn kết quả (`WinResultDelay`/`LoseResultDelay`) dùng thời gian thực, không nhân tốc độ.
- Nhãn nút "x1"/"x2" cập nhật khi bấm, có `WoolButtonJuice`.

## 6. Màu, ánh sáng, nền

- Nền thế giới: mặt đất chia hai vùng — cỏ xanh bạc hà (#BFE8C8 làm gốc, vân nỉ nhẹ) phía sau sân rồng, kem/be (#F6E7CF) phía bảng và khay; ranh giới mềm (gradient trong texture procedural, `Ground.mat`). Camera clear color kem.
- Đường: màu cát (#E9C99A) viền nâu nhạt nét khâu trắng (thay xám).
- Post: giảm vignette navy; vignette màu nâu ấm nhẹ hoặc tắt; giữ bloom/tonemapping.
- Bộ ảnh UI của user (badge, nút tròn, card, ruy băng, icon) tiếp tục dùng; chữ HUD dùng màu navy đậm trên nền sáng (đảm bảo contrast ≥ 4.5).

## Ngoài phạm vi

Booster, khối xích/khóa số, ô VIP, quảng cáo, đổi level/luật. Ảnh nền cỏ/kệ gỗ vẽ tay (sẽ đưa prompt sau nếu user muốn).

## Kiểm thử

- Harness .NET: cộng check `WoolStage` (`ToWorld` của điểm đầu/cuối, tỉ lệ khoảng cách = `PathScale`); toàn bộ check cũ PASS (cập nhật số assertion nếu hình học đổi, ghi lại dòng PASS mới).
- `Validate All Levels`: cả 3 level hợp lệ và có lời giải.
- Editor: `Stage` mở rộng — HUD, sân rồng (đường + mèo), khay (ô + nút Unlock), bảng + route vòng của mọi khối của 3 level nằm trong vùng của mình và không chồng nhau ở 1080×1920 và 1080×2400. `UiGame`/`UiArtGame` cập nhật: chữ bị ẩn vẫn còn đúng chuỗi; Retry nằm trong thẻ Pause; nút tốc độ và nút Unlock ≥ 96 px chạm.
- Play Mode: x2 làm `State.Elapsed` tăng gấp đôi trong cùng thời gian thực; Pause chặn cả x2; sợi len hiện liên tục suốt một spool (không tắt giữa hai lần kéo) và ẩn khi spool xong; nút Unlock mở ô khóa đầu tiên; `PlayBlocks`, `PlayMotion`, `PlayFeel`, `PlayUi`, `PlayResult*`, `PlayArt*` PASS.
- `Rebuild Art` + `Rebuild UI` hai lần idempotent; GUID/level không đổi.
- Ảnh 1080×1920 và 1080×2400: đầu level, đang chạy (2–3 khối + sợi len), Pause → `docs/layout-*.png`; ghép với ảnh mẫu → `docs/layout-compare.png`.
