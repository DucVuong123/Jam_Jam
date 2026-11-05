# Wool Jam

Mở `Assets/Scenes/WoolJam.unity` và nhấn Play. Scene gameplay đã được nối reference; không cần chạy script tạo scene khi mở project lần đầu.

## Cách chơi

- Chạm/click khối mũi tên có đường thoát trống. Khối giữ một ô khay ngay khi được chọn.
- Ống cuộn tự hút len cùng màu đã lộ trên đường đi. Ống vào trước được ưu tiên, kể cả khi ô thấp vừa được dùng lại.
- Ống đủ dung lượng tự biến mất. Nếu khay đầy, chờ ống hút xong hoặc chạm ô `+` để mở thêm chỗ.
- Thu hết len để cứu mèo. Rồng đi tới vùng mèo thì thua.
- Có pause/resume, retry và next. Màn đã mở được lưu bằng PlayerPrefs `JamJam.Level`.

## Dữ liệu và asset

- Ba màn tại `Assets/JamJam/Generated/Levels` chứa vị trí/kích thước/hướng khối, màu và dung lượng ống, thứ tự len, đường đi và tốc độ.
- Tổng dung lượng ống theo từng màu phải bằng tổng len cùng màu. Vị trí khối không được chồng nhau.
- Model, mesh, material và texture nằm tại `Assets/JamJam/Generated`. Prefab và chữ UI được lưu thành asset/scene; có thể sửa bằng Inspector.
- Luật chơi nằm tại `Runtime/WoolGame.cs`; hiển thị, input và subscription R3 nằm tại `Runtime/WoolScene.cs`.
- R3/R3.Unity được pin ở 1.3.1. DLL NuGet và license đặt trong `Plugins/R3`.

## Công cụ Editor

- `JamJam/Validate All Levels`: kiểm tra dữ liệu và tìm một lời giải bằng chính luật gameplay. Đây là bộ giải ưu tiên màu đang lộ, không phải phép chứng minh một màn bất kỳ là vô nghiệm.
- `JamJam/Build Wool Jam Scene`: tái tạo model, texture, prefab, ba màn và scene. Lệnh này đặt lại các asset được tạo; chỉ dùng khi muốn tái tạo bản mặc định. Dừng Play và lưu mọi scene trước khi chạy.
- Công cụ kiểm chứng chỉ tự chạy khi được bật cho phiên kiểm chứng; không tự chụp ảnh trong các scene khác.

## Kiểm tra logic

Chạy từ thư mục project:

```text
dotnet run --project Tools/WoolJamChecks/WoolJamChecks.csproj
```

Yêu cầu .NET SDK 10. Gameplay trong Unity không phụ thuộc SDK này.

## Phạm vi

Đây là ba màn thiết kế lại theo cơ chế và phong cách tham chiếu. Không sao chép nguyên trạng từng ô của level 10–12, không tích hợp quảng cáo/IAP/backend. Chưa có bằng chứng chạy Android; kết quả compile, render và runtime kiểm chứng được ghi riêng trong tài liệu bàn giao.
