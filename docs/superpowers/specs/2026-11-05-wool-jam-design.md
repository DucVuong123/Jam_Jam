# Thiết kế gameplay Wool Jam trong JamJam

Ngày: 2026-11-05
Trạng thái: Người dùng đã duyệt thiết kế và yêu cầu triển khai ngày 2026-11-05. Kết quả thực hiện được ghi trong plan và tài liệu bàn giao.

## Mục tiêu đã xác nhận

Dựng gameplay của game tại hai link tham chiếu vào project Unity JamJam. Người dùng chọn ưu tiên cả gameplay và hình ảnh len sát bản gốc ngay, thay vì bản chơi được với model đơn giản. Sản phẩm cần chạy được trực tiếp trong Unity, có bàn chơi, animation, điều kiện thắng/thua và hình ảnh 3D thống nhất.

Nguồn tham chiếu:

- Google Play: https://play.google.com/store/apps/details?id=wool.match.color.sort.jam.puzzle&hl=en-US
- Video: https://www.youtube.com/watch?v=rKGCayefHnw
- Metadata video đọc trực tiếp từ YouTube: “10 - 12 Level Wool Crush yarn car jam game 2026”, thời lượng 629 giây.

## Hiện trạng đã kiểm tra

- Unity 6000.3.19f1, URP 17.3.0, Input System 1.19.0, UGUI 2.0.0 và Unity Test Framework 1.6.0.
- Project mới, chỉ có SampleScene và script giới thiệu template. Chưa có model, texture, prefab hoặc logic gameplay để dùng lại.
- SampleScene có Main Camera, Directional Light và Global Volume. Build Settings đang trỏ vào scene này.
- Active Input Handling đang là Input System. Project chưa có R3 hoặc UniTask.
- Git sạch khi bắt đầu khảo sát, commit hiện tại c7cadb5.

## Mức độ xác nhận tham chiếu

Đã đọc mô tả store, xem hai ảnh store có độ phân giải cao, thumbnail và các bảng storyboard tại các khoảng 0–40, 45–85, 180–220, 360–400, 585–625 giây của chính video được cung cấp. Chưa phát liên tục được video; tải luồng MP4 trả về HTTP 403. Storyboard chỉ lấy một khung mỗi 5 giây, vì vậy không chứng minh được chính xác tốc độ, thứ tự xử lý trong cùng một frame hay mọi trạng thái trung gian.

Hình tham chiếu thể hiện khối có mũi tên trên bàn, các ống cuộn ở khay trung gian, dây len nối từ thân rồng đến ống cuộn, rồng đi trên đường cong và mèo ở vùng đích. Vì vậy cơ chế cần dựng là đưa khối ra khỏi bàn rồi cuốn len, thay vì người chơi tự ngắm và bắn đạn theo cách mô tả marketing của store.

Video còn thể hiện các biến thể bàn xếp chéo, bàn hàng dọc, các khối tối màu và hàng chuyển động ở phía dưới. Chưa đủ bằng chứng để xác nhận toàn bộ quy tắc của các biến thể này. Bản đầu tập trung làm đầy đủ vòng chơi khối mũi tên và cuộn len; không khẳng định tái tạo nguyên trạng từng ô của level 10–12.

## Phương án kỹ thuật đề xuất

Dùng mesh và material dựng trong Unity, cùng scene/prefab được lưu thành asset chỉnh được trong Editor. Dựng thân rồng theo đường cong bằng các đốt có nhiều lớp vảy len, đầu rồng và mèo bằng mesh tạo hình; tạo hoa văn sợi đan bằng texture/normal và shader URP. Khối và ống cuộn cũng dùng vật liệu cùng phong cách. Đây là phương án đề xuất của người triển khai, chưa phải chi tiết đã được người dùng duyệt.

Cách này phù hợp với project trống và cho phép điều chỉnh tỉ lệ, màu, animation để bám tham chiếu. Dùng primitive trơn cho toàn bộ phần hiển thị không đáp ứng lựa chọn hình ảnh của người dùng. Phương án dùng bộ model sản xuất sẵn chưa khả thi vì project chưa cung cấp bộ asset tương ứng. Không đưa ảnh store hoặc storyboard vào làm hình nền thay cho gameplay 3D.

## Vòng chơi

1. Người chơi mở màn dọc, thấy rồng len trên đường ở nửa trên, khay ống cuộn ở giữa và bàn khối mũi tên ở nửa dưới.
2. Chạm/click một khối. Khối chỉ được rời bàn khi đường theo hướng mũi tên không bị khối khác chiếm và khay còn chỗ.
3. Một lần chọn hợp lệ giữ chỗ khay ngay lập tức, rồi khối trượt ra theo hướng của nó và chuyển thành ống cuộn cùng màu. Các lần chạm nhanh không được nhận trùng một khối hoặc vượt sức chứa khay.
4. Các ống cuộn tự hút bất kỳ đơn vị len cùng màu đang lộ, không bắt buộc màu đó ở đầu rồng và không bị chặn bởi màu khác đứng trước. Vùng lộ là phần thân đã vượt cổng xuất hiện ở đầu DragonPath và còn nằm trên đoạn đường hữu hạn đến vùng mèo; đơn vị chưa qua cổng không được hút. Khi có nhiều đơn vị hợp lệ, ưu tiên từ đầu rồng về đuôi; nhiều ống cùng màu được xử lý theo thứ tự vào khay và phải giữ quyền sử dụng đơn vị len ngay khi bắt đầu hút để không trừ trùng. Đây là quy tắc triển khai đề xuất để có hành vi xác định, không phải kết luận về mọi frame của game gốc.
5. Len rút về ống qua dây nối có chuyển động; số lượng còn cần cuộn giảm theo cùng một nguồn trạng thái. Ống đủ dung lượng biến mất và trả chỗ khay sau animation hoàn tất.
6. Rồng tiến về mèo theo đường đã đặt trong màn. Khi len bị hút, thân ngắn lại và đầu rồng lùi tương ứng; phần di chuyển và phần tiêu thụ len không được tạo hai bộ đếm riêng mâu thuẫn.
7. Thắng khi toàn bộ len mục tiêu đã được thu. Thua khi rồng chạm vùng mèo. Khay đầy chặn thao tác lấy thêm khối; nếu vẫn có ống có thể hút thì game tiếp tục chờ, không báo thua ngay.
8. Có chơi lại, màn tiếp theo, pause/resume và lưu màn đã mở. Chơi lại hoặc chuyển màn hủy sạch trạng thái và công việc còn chạy của màn cũ.

Các giá trị sức chứa khay, dung lượng ống, tốc độ rồng, tốc độ hút và thời điểm animation là dữ liệu của màn để cân chỉnh bằng playtest. Giá trị đề xuất ban đầu: 4 ô khay khả dụng, có thể mở thêm 2 ô bằng thao tác hỗ trợ trong bản thử; đây không phải con số đã xác minh chính xác từ video. Không gắn quảng cáo hay mua hàng vào bản gameplay này.

## Hình ảnh cần đạt

- Camera orthographic nhìn nghiêng nhẹ, bố cục dọc 9:16, màu nền kem/pastel, bóng mềm và màu khối bão hòa như tham chiếu.
- Rồng có đầu nhận diện được: mắt, mõm, sừng, tai, bờm; thân là nhiều lớp vảy len có hoa văn sợi đan, có đổ bóng và độ dày 3D. Không dùng chuỗi quả cầu trơn làm hình ảnh cuối.
- Mèo có tai, mắt, mõm, chân và đuôi; có idle, phản ứng nguy hiểm và mừng khi thắng.
- Khối có kích thước dài/ngắn, bề mặt dệt và mũi tên trắng nổi rõ; hướng nhìn phải khớp hướng thoát được logic sử dụng.
- Ống cuộn có lõi, hai vành và lớp dây len tăng dần khi hút; thể hiện số dung lượng còn lại.
- Dây hút len có màu khớp thân/ống, đầu dây bám đúng hai đối tượng kể cả khi chúng đang chuyển động.
- UI có level, tiến độ, pause, retry, next và trạng thái kết quả; chữ được lưu trong scene/prefab.
- Có phản hồi khi khối bị chặn, hiệu ứng hoàn tất ống và ăn mừng khi cứu mèo. Hiệu ứng giữ được độ dễ đọc của bàn.
- Đánh giá bằng ảnh chụp Game View cạnh ảnh tham chiếu. Mục tiêu là bám phong cách và bố cục; chưa có cơ sở cam kết độ giống từng chi tiết vì không có asset sản xuất gốc.

## Cấu trúc scene và quyền sở hữu

Scene gameplay riêng WoolJam chứa Camera, Light, Volume, GameRoot, DragonPath, Dragon, Kitten, Board, SpoolTray và Canvas. Các model, material, shader, prefab, dữ liệu màn và source đặt dưới Assets/JamJam. SampleScene được giữ lại; scene gameplay trở thành scene chạy chính trong Build Settings.

Dữ liệu màn giữ bố trí khối, hướng, màu, dung lượng, thứ tự len, đường đi, sức chứa khay và thông số tốc độ. Có ba màn thủ công tăng độ khó: màn đầu dạy thoát khối và cuộn đúng màu, màn hai tăng phụ thuộc đường đi và sức chứa, màn ba tăng áp lực rồng và số màu. Đây là ba màn thiết kế lại theo cơ chế tham chiếu, không gắn nhãn bản sao chính xác level 10–12.

Trạng thái gameplay là nguồn duy nhất cho bàn, khay và len. Phần hiển thị chỉ phản ánh trạng thái và gửi yêu cầu thao tác. Mỗi vòng đời màn có cơ chế hủy riêng; pause ngừng mô phỏng, nhưng UI pause vẫn hoạt động.

## Ràng buộc code của người dùng

- Không thêm comment vào source, shader, script hoặc file cấu hình mới/chỉnh sửa.
- Method trả về đối tượng sử dụng interface hoặc abstract class; áp dụng cả kiểu kết quả async. Struct, enum và primitive giữ kiểu giá trị tương ứng.
- Bộ đếm và công việc lặp theo thời gian dùng R3 Observable.Timer/Observable.Interval, mỗi subscription có Dispose hoặc TakeUntil token.
- Dùng rõ UnityTimeProvider.UpdateIgnoreTimeScale cho phần thực sự cần bỏ qua timeScale. Tick mô phỏng gameplay tuân theo pause.
- Trước triển khai cần thêm R3 phiên bản tương thích Unity 6000.3, kiểm tra từ nguồn chính thức; không thêm UniTask nếu không cần.
- Mọi animation/công việc async phải có quyền sở hữu và hủy theo màn; không để công việc cũ tiếp tục sửa state sau reset.
- Giữ file .meta và GUID ổn định. Các đối tượng và UI hiển thị chính được lưu thành asset/scene, không chỉ tồn tại khi code tự khởi tạo lúc Play.

## Kiểm chứng cần thực hiện khi triển khai

Logic: khối bị chặn theo mọi hướng, khối kích thước dài/ngắn, thao tác khay đầy, giữ chỗ khi chạm nhanh, hai ống cùng màu không hút trùng, trả chỗ sau khi hoàn tất, phần len chưa lộ, thắng/thua, reset và pause.

Dữ liệu: tổng dung lượng ống theo từng màu khớp tổng len của màn; bố trí không chồng ô; mỗi màn có ít nhất một chuỗi chọn hợp lệ dẫn đến thắng. Kiểm tra lời giải thực hiện trên cùng logic gameplay, không viết bộ luật riêng trong test.

Unity: import shader/material không lỗi, compile assemblies, test logic qua Unity Test Framework hoặc harness tương đương có ý nghĩa. Nếu Editor đang mở JamJam, không khởi chạy batch trên cùng project đồng thời.

Runtime: chạy màn, chọn khối thật, kiểm tra dây len/ống/rồng, hoàn tất ít nhất một màn và thử trạng thái thua/retry. Chụp ảnh Game View ở 9:16 để kiểm chứng bố cục và hình ảnh. Tách kết quả compile, test và playtest, không dùng một loại làm bằng chứng cho loại khác.

Chưa bao gồm build APK hoặc kiểm thử thiết bị Android; chỉ báo đã chạy trên thiết bị khi thực sự có bằng chứng.

## Tiêu chí bàn giao

- Mở scene WoolJam và nhấn Play để chơi được, không cần ráp thêm reference thủ công.
- Ba màn có dữ liệu và lời giải hợp lệ, retry và next hoạt động.
- Hình ảnh có hoa văn len, rồng/mèo/ống cuộn tạo hình, ánh sáng và UI hoàn chỉnh theo các tiêu chí ở trên.
- Không có compile error hoặc exception trong những luồng đã kiểm tra; trình bày rõ những phần chưa được chạy thử.
- Có hướng dẫn mở scene và danh sách phần đã triển khai; không mô tả là đã clone hoàn tất nếu chỉ mới có thiết kế hoặc code chưa chạy.

## Chưa thuộc phạm vi được đề xuất

Quảng cáo, IAP, đăng nhập, backend, live events, kinh tế nhiều tầng và hàng trăm màn. Các cơ chế nâng cao chưa xác minh rõ từ storyboard cần thêm bằng chứng trước khi đưa vào source. Chưa sửa file gameplay trước khi thiết kế được duyệt; plan code trước/sau sẽ lập bằng tiếng Việt theo đúng mẫu người dùng sau bước này.
## Kết quả rà soát thiết kế

Một agent Unity độc lập đã đọc tài liệu và yêu cầu làm rõ khả năng hút màu bất kỳ, thứ tự chọn mục tiêu và cách xác định vùng len đang lộ. Các điểm này đã được bổ sung tại bước 4 của vòng chơi dưới dạng quyết định kỹ thuật đề xuất; không phát sinh yêu cầu cần người dùng cung cấp thêm để lập plan.
