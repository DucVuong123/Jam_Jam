# Game feel gameplay (phần 2/3 của đợt làm lại art + game feel)

Ngày: 2026-11-08. Trạng thái: user cho phép chạy thẳng không cần duyệt từng bước ("từ phần 2 ko cần hỏi tôi duyệt đâu chạy hết các phần luôn đi").

## Mục tiêu

Mỗi hành động trong màn `WoolJam.unity` phải có phản hồi rõ: chuyển động có đà, VFX đúng màu len, rung camera và rung máy (haptic). Phong cách lấy từ game tham khảo "Wool Crush™ - Yarn Car Jam": phản hồi nhanh, nảy, vui mắt. **Không âm thanh.** Không đổi luật chơi, level, route, FIFO, thời lượng logic (`WoolGame`, `WoolRouting` không sửa).

## Quyết định

- Toàn bộ là lớp hiển thị. Sự kiện suy ra bằng cách so trạng thái `WoolGame` giữa hai frame, không thêm callback vào `WoolGame`.
- Logic thuần (không phụ thuộc UnityEngine) tách riêng để harness .NET kiểm được: dò sự kiện, đường cong easing, toán rung camera, mẫu haptic.
- VFX dùng `ParticleSystem.Emit(EmitParams)` trên một hệ hạt cho mỗi loại hiệu ứng (không pool GameObject), texture hạt procedural.
- Haptic qua interface `IWoolHaptics`; Android dùng `Vibrator` + `VibrationEffect` (API 26+), máy cũ dùng `vibrate(ms)`; Editor/nền khác dùng bản rỗng. Tôn trọng `WoolSettings.Vibration`.
- Không tạo timer mới: mọi thứ chạy trong `Tick`/`RenderFrame` hiện có (R3 Interval sẵn có).
- Quy tắc code giữ nguyên: không comment, method trả reference dùng interface/abstract, factory void + out, giữ GUID, không commit, không chạy `WoolSceneBuilder.Build`.

## Thành phần

| File | Vai trò |
|---|---|
| `Runtime/WoolFeelEvent.cs` | `enum WoolFeelKind { BlockLaunched, BlockBlocked, SpoolArrived, YarnConsumed, SpoolCompleted, SlotUnlocked, TrayFull, Danger, Won, Lost }`; `struct WoolFeelEvent { Kind, Index, Color }`. Thuần. |
| `Runtime/WoolFeelTracker.cs` | Giữ snapshot trước (phase block, arrival/remaining/block của spool, consumed của yarn, OpenSlots, trạng thái khay đầy, ngưỡng nguy hiểm, Status). `Reset(WoolGame)` và `Collect(WoolGame, List<WoolFeelEvent>)` thêm sự kiện mới vào list. `Blocked(int index, int color, List<WoolFeelEvent>)` cho click bị chặn. Thuần. |
| `Runtime/WoolEase.cs` | Hàm thuần: `OutBack(t)`, `Punch(t)` (0→đỉnh→0, có nảy), `Arc(t)` = sin(πt). Kẹp t∈[0,1]. |
| `Runtime/WoolShake.cs` | Rung camera kiểu trauma: `Add(amount)` (kẹp ≤1), `Step(delta, out x, out y, out roll)`; biên độ = trauma², trauma giảm tuyến tính ~1.6/s; nhiễu giả ngẫu nhiên tất định (hàm băm sin, không dùng UnityEngine.Random). Thuần. |
| `Runtime/WoolHapticPattern.cs` | Map `WoolFeelKind` → mảng timing (ms) và amplitude (1–255) hoặc rỗng nếu không rung. Thuần. |
| `Runtime/IWoolHaptics.cs` + `Runtime/WoolHaptics.cs` | `IWoolHaptics { void Play(long[] timings, int[] amplitudes); }`; `WoolHaptics.Create(bool enabled, out IWoolHaptics haptics)` chọn Android/rỗng. Android: `AndroidJavaObject` lấy `vibrator`, `VibrationEffect.createWaveform(timings, amplitudes, -1)` khi SDK ≥ 26, ngược lại `vibrate(tổng ms)`; tham chiếu `Handheld.Vibrate` trong nhánh không gọi được để Unity tự thêm quyền `VIBRATE`. Bắt mọi exception JNI, không làm crash game. |
| `Runtime/WoolFeel.cs` | MonoBehaviour do `WoolScene` thêm lúc chạy (không đổi scene/serialized field của WoolScene). Giữ tracker, shake, haptics, hai hệ hạt (Puff, Sparkle) instantiate từ `WoolArt`, và bộ đếm thời gian pop cho từng block/spool/slot/kitten/dragon. API: `Begin(WoolScene, Camera)`, `Reset(WoolGame)`, `Blocked(int index)`, `Step(WoolGame, float delta)` (gom sự kiện, phát phản hồi, cập nhật rung camera), các getter hệ số `BlockScale(i)`, `BlockLift(i)`, `BlockFlash(i)`, `SpoolScale(i)`, `SlotScale(i)`, `SlotFlash(i)`, `DragonScale`, `CameraZoom`. |
| `Editor/WoolAssetsBuilder.cs` | Thêm 2 texture hạt procedural (`SoftDot.png` tròn mềm, `Star.png` sao 4 cánh), material hạt URP Particles Unlit (additive cho Sparkle, alpha cho Puff), 2 prefab `FeelPuff.prefab`, `FeelSparkle.prefab` (ParticleSystem không tự phát, `simulationSpace = World`, `playOnAwake=false`, `emission.enabled=false`, max 256 hạt), gán vào `WoolArt.Puff`, `WoolArt.Sparkle`. |
| `Runtime/WoolArt.cs` | Thêm `public GameObject Puff; public GameObject Sparkle;`. |
| `Runtime/WoolScene.cs` | Thêm WoolFeel lúc `Start`, gọi `Reset` khi `LoadLevel`, `Blocked` khi click bị chặn, `Step` trong `Tick` (cả khi thắng/thua để hiệu ứng chạy hết); trong `RenderFrame` nhân hệ số juice vào scale/position hiện có. Camera: lưu vị trí/size gốc, cộng offset rung + zoom mỗi frame, trả về đúng gốc khi trauma = 0. |

## Bảng phản hồi

| Sự kiện (cách dò) | Chuyển động | VFX | Camera | Haptic |
|---|---|---|---|---|
| BlockLaunched (phase 0→1) | pop scale 1→1.18→1 trong 0.18s (`Punch`); khi bay nhấc cung `Arc(Travel/Duration)*0.7` theo y | Puff trắng 10 hạt tại block | — | nhẹ: 12ms, 90 |
| BlockBlocked (Select trả false) | shake ngang hiện có + chớp đỏ: MPB `_BaseColor` lerp tới `#FF5A5A` 45% rồi về trong 0.3s | — | trauma +0.18 | cảnh báo: 0/18/40/18 ms, amp 0/140/0/140 |
| SpoolArrived (Arrival >0 → ≤0, Block ≥0) | spool pop từ 0 lên 1 bằng `OutBack` trong 0.28s (thay hiện tức thì) | Puff màu len 14 hạt tại slot | trauma +0.05 | nhẹ: 15ms, 110 |
| YarnConsumed (Consumed false→true) | đầu rồng scale 1→1.08 (`Punch`, 0.15s) | Puff màu len 5 hạt tại vị trí đơn vị len | — | không (quá dày) |
| SpoolCompleted (Remaining >0 → 0) | spool punch 1→1.25 trong 0.2s trước khi co (Finish hiện có) | Sparkle màu len + trắng 12+12 hạt | trauma +0.08 | vừa: 30ms, 170 |
| SlotUnlocked (OpenSlots tăng) | pad pop 1→1.2→1 0.25s | Sparkle trắng 16 hạt | — | nhẹ: 15ms, 110 |
| TrayFull (mọi slot mở đều có spool, false→true) | pad mở chớp đỏ nhẹ 0.5s (MPB) | — | — | cảnh báo |
| Danger (Head/PathLength vượt 0.8 đi lên) | mèo run hiện có | — | trauma +0.25 | cảnh báo |
| Won (Status → Won) | confetti hiện có + mèo nhảy hiện có | Sparkle 60 hạt quanh mèo | zoom vào 6% (`Punch`, 0.6s) | thắng: 30/60/30/60/60 ms, amp 160/0/200/0/255 |
| Lost (Status → Lost) | đầu rồng phồng 1→1.3 (`Punch`, 0.4s) | Puff đỏ cam 28 hạt tại mèo | trauma +0.6 | mạnh: 90ms, 255 |

Rung camera: offset tối đa 0.35 đơn vị theo trục right/up của camera, roll tối đa 2°, tần số nhiễu ~18Hz. Chỉ dùng thời gian thực của `Tick` (delta), tạm dừng (Paused) thì đóng băng.

## Kiểm thử

### Harness .NET (thêm vào `WoolJamChecks.csproj`)
- Tracker: level mẫu → Select → Step đến khi xong; chuỗi sự kiện chứa đúng thứ tự BlockLaunched → SpoolArrived → YarnConsumed ×Capacity → SpoolCompleted → Won; mỗi sự kiện đúng Index/Color; không phát lặp ở frame kế tiếp; `Reset` xóa trạng thái; UnlockSlot → SlotUnlocked; level để thua → Danger trước Lost, mỗi cái đúng một lần; TrayFull khi mọi slot mở bị chiếm.
- Ease: biên t=0/1, kẹp ngoài [0,1], `Punch(0)=Punch(1)=0`, đỉnh dương.
- Shake: trauma 0 → offset 0; sau đủ thời gian về 0; offset luôn trong biên; tất định với cùng chuỗi delta.
- Haptic pattern: mọi kind có độ dài timing = amplitude; amplitude ∈[0,255]; YarnConsumed rỗng.
- Dòng PASS mới: `PASS: 153 gameplay assertions; 4121 route assertions; 18 progress assertions; N feel assertions` (các số cũ không đổi).

### Editor (Unity CLI)
- Check `WoolFeelChecks`: prefab Puff/Sparkle có ParticleSystem đúng cấu hình, material không lỗi shader, `WoolArt` gán đủ.
- Play Mode: chọn block hợp lệ → hạt Puff tăng, block có lift > 0 khi bay; click block bị chặn → camera lệch khỏi gốc rồi trở về đúng gốc (sai số < 1e-4) sau 1.5s; thắng level → Sparkle phát và camera size về gốc; thua → trauma > 0.
- Haptic: trong Editor dùng bản rỗng; check xác nhận factory trả bản rỗng khi `enabled=false` và trong Editor.
- Gameplay không đổi: harness, `Validate All Levels`, `PlayBlocks` level 3, `Rebuild Art` idempotent, GUID/level không đổi.
- Chụp GIF-like: chuỗi ảnh 1080×1920 (`docs/feel-*.png`) lúc block bay (lift + puff), spool hoàn thành (sparkle), thắng.
- Hiệu năng: batches không tăng quá 10% so với cuối phần 1 (L2 302) khi không có hạt.

## Ngoài phạm vi

Âm thanh; UI reskin và animation UI (phần 3); trạng thái sprite mới của mèo/rồng (chưa có file); đổi luật chơi/level.
