# Block len cập nhật 2026-11-05

Asset đích: `Assets/JamJam/Art/Sprites/Block-Knit-Base.png`.

Tạo bằng built-in ImageGen; giữ nguyên PNG và alpha từ kết quả được chọn. Ảnh tham chiếu của người dùng là bảng block màu có mũi tên, mũi đan nổi và viền bện. Mô tả tạo hình: một block len vuông nhìn chính diện từ trên, len trắng xám để tint trong Unity, mũi đan chữ V dày, viền bện quanh mép, phần cạnh dưới có độ dày nhẹ, nền trong suốt, không chữ và không mũi tên. Bản tinh chỉnh thu sát khung để giảm khoảng trong suốt ngoài block.

Nguồn đã chọn: `C:/Users/doduc/.codex/generated_images/01a1a999-d8d2-7b61-bfea-9aa3af83b34f/exec-03911804-b2e0-4e85-9de6-e3287e7b0e3d.png`.

Unity dùng cùng sprite cho block vuông, ngang và dọc với DrawMode Tiled; tint theo màu Yarn hiện có, tăng saturation 1.25 và value 1.12 trong view. Mũi tên tiếp tục là sprite sinh từ polygon trong `WoolAssetsBuilder.WriteArrowSprite`, có trục và viền dày hơn.
