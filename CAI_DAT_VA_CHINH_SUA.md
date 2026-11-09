# Backend Tiệm Bá Đường — Ghi chú cài đặt

> Đây là code backend viết thêm trên source `TiemBaDuongtest-master.zip`. **Chưa được build và chưa chạy thử** (môi trường viết code không có Visual Studio và SQL Server). Làm theo thứ tự dưới đây, và gửi lại lỗi build nếu có.

## 1. Kiểm tra kết nối database (kết quả)

- Connection string `mystoreEntities` trong `Web.config`: đúng cấu trúc EntityClient, trỏ tới `Model1.csdl/ssdl/msl` khớp tên `Model1.edmx`. Dùng Windows Authentication, không có mật khẩu.
- Database tên `mystore` (không phân biệt hoa thường trên SQL Server mặc định, nên `MyStore` và `mystore` là một). Server `ENTEL\SQLEXPRESS` là máy của bạn, máy khác phải đổi.
- `Model1.edmx` khớp với schema trong `database2024 (1).txt`: đủ 6 bảng, đúng kiểu và độ dài cột, đúng khóa ngoại. Có thêm bảng `sysdiagrams` do SSMS tạo khi dùng sơ đồ; không ảnh hưởng, có thể giữ hoặc xóa.
- Các lỗi đã phát hiện và đã sửa trong bản này: `Model1.Context.cs` rỗng (không có `DbContext`), `packages.config` thiếu EntityFramework, `ProductRepository.cs` tham chiếu entity cũ đã đổi, `Web.config` thiếu cấu hình Forms Authentication.
- Chưa làm được: kiểm tra dữ liệu thực trong DB (chạy các truy vấn mục 5 trong `DATABASE_GAPS.md`).

## 2. Cài EntityFramework (bắt buộc)

Package Manager Console (Tools → NuGet Package Manager → Package Manager Console):

    Install-Package EntityFramework -Version 6.4.4 -ProjectName TiemBaDuong

Lệnh này tự thêm reference và file cần thiết. Không sửa tay csproj cho EF.

Nếu `Model1.Context.tt` hoặc `Model1.tt` báo lỗi "EF6.Utility.CS.ttinclude not found": chuột phải `Model1.edmx` → *Add Code Generation Item* → *ADO.NET EntityFramework 6.x DbContext Generator*. Nếu bạn chạy lại "Run Custom Tool" thì `Model1.Context.cs` sẽ bị ghi đè bằng bản sinh tự động (nội dung tương đương).

## 3. Chạy migration (bản test trước)

Chạy `MyStore_migration_approved.sql` (file gửi kèm riêng) trên DB test. Chạy các truy vấn kiểm tra trong `DATABASE_GAPS.md` mục 5 trước khi chạy trên DB thật. Script thêm các cột: `Product.AudioDemoUrl`, `Product.CreatedDate`, `Product.IsActive`, `Order.OrderStatus`, `Order.DeliveryMethod`, `Order.PaymentMethod`; đổi `User.Password` sang `NVARCHAR(255)`; seed 4 danh mục và 10 sản phẩm.

## 4. Cập nhật EDMX sau migration (bắt buộc)

Mở `Model1.edmx` → chuột phải nền thiết kế → *Update Model from Database* → tab *Refresh* → chọn đủ 6 bảng → Finish. Kiểm tra `Product` có `AudioDemoUrl`, `CreatedDate`, `IsActive`; `Order` có `OrderStatus`, `DeliveryMethod`, `PaymentMethod`. Build chỉ thành công sau bước này, vì code đã dùng các thuộc tính đó.

## 5. Build

Build → Rebuild Solution. Nếu lỗi, gửi nguyên văn thông báo trong Error List. Các điểm dễ lỗi nhất:
- Lỗi `Product`/`User` không khớp tên: kiểm tra đã làm bước 4.
- Lỗi "Web.Debug.config/Web.Release.config connection string ReleaseSQLServer": chỉ xuất hiện khi publish, sửa hoặc xóa khối `connectionStrings` trong hai file đó.

## 6. Dữ liệu cũ và mật khẩu

- Mật khẩu hash mới dạng `PBKDF2-SHA256$...`. Mật khẩu plaintext cũ (nếu có) sẽ không đăng nhập được, phải reset (xem `DATABASE_GAPS.md` G2).
- Admin đầu tiên chưa có cách tạo tự động. Cách đơn giản: tạm thời chạy một lần trong `Application_Start` hoặc viết một trang riêng có khóa, băm mật khẩu bằng `PasswordService.Hash` rồi xóa đoạn đó. Bản này không tự tạo Admin.

## 7. Giá trị tạm thời chờ bạn chốt

File `Models/Services/Options.cs`:
- `DeliveryMethods`, `PaymentMethods`: giá trị tạm (Q4, Q5), thay bằng danh sách thật.
- `ShippingFee = 0` (Q12).
- Nhãn trạng thái đơn trong `OrderStatuses` theo PPT.

Các hằng số khác trong `CatalogService` (số sản phẩm/trang) theo Q15 = 6, MyOrder = 10 (chưa có quy định, ghi trong IMPLEMENTATION_NOTES).

## 8. Những gì đã có trong bản này

- Khách hàng: trang chủ (Featured bán chạy, New Products 20 sản phẩm phân trang 6), danh mục, chi tiết (số đã bán, sản phẩm cùng danh mục, bán chạy phân trang 3), tìm kiếm nâng cao (từ khóa, khoảng giá, sắp xếp, giữ điều kiện khi chuyển trang).
- Tài khoản: đăng ký (User + Customer trong transaction), đăng nhập/đăng xuất (Forms Authentication, mật khẩu PBKDF2), hồ sơ, đổi mật khẩu.
- Giỏ hàng trong Session: thêm/sửa/xóa/xóa hết, chọn mua theo checkbox, tổng tiền chỉ tính phần đã chọn, nhóm theo danh mục, sản phẩm tương tự, giá luôn lấy từ DB.
- Đặt hàng: checkout yêu cầu đăng nhập, token một lần chống gửi trùng, giá và tổng tiền do server tính trong transaction, đặt xong chỉ xóa các sản phẩm đã đặt.
- Xem đơn: OrderSuccess (kiểm tra quyền sở hữu, trả 404 nếu không phải chủ đơn), lịch sử đơn (lọc trạng thái, tìm mã đơn/tên sản phẩm, phân trang), mua lại (giá mới từ DB).
- Admin: đăng nhập/đăng xuất, kiểm tra role Admin trên server ở mọi request (`AdminAuthorize`), dashboard thống kê.

## 9. Chưa có trong bản này (làm bước sau)

- Admin CRUD: Category, Product (kèm upload ảnh an toàn), tài khoản Admin, danh sách/chi tiết khách hàng, đơn của khách.
- PayPal (Phase 7, cần credential sandbox).
- Trang Liên hệ và Giới thiệu (không có action trong code gốc).
- Test tự động và test thủ công theo mục J1–J3 trong `HUONG_DAN_BACKEND.md`.
- Các view Admin CRUD và các mục "theo PPT" chưa đối chiếu được vì không có slide.

## 10. Thay đổi so với source gốc (tóm tắt)

| File | Thay đổi |
|---|---|
| `Model1.Context.cs` | Viết lại `mystoreEntities` (trước đó rỗng) |
| `ProductRepository.cs` | Đã xóa, dữ liệu mẫu chuyển vào DB (script migration) |
| `Controllers/HomeController.cs` | Đọc từ DB, có Search, ProductList, ProductDetail |
| `Controllers/AccountController.cs`, `CartController.cs`, `OrderController.cs` | Mới |
| `Models/*` | ViewModel, CartItem, service (mật khẩu, catalog, giỏ, đặt hàng) |
| `Security/AdminAuthorizeAttribute.cs`, `Areas/Admin/*` | Mới |
| `App_Start/RouteConfig.cs` | Thêm `namespaces` cho route mặc định |
| `Views/Shared/_Layout.cshtml` | Bỏ `asp-append-version` và giỏ hàng cứng `3`; thêm menu cá nhân, thông báo, script nghe thử |
| `Views/Home/Index.cshtml` | Form tìm kiếm GET, danh mục từ DB, Featured từ controller, link chi tiết |
| `Web.config` | Thêm `authentication`, `sessionState`, `customErrors` |
| `TiemBaDuong.csproj` | Thêm Compile/Content cho file mới, bỏ `ProductRepository.cs` |
