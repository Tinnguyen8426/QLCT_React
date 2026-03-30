# Kế hoạch vận hành tính năng bán mỹ phẩm

## 1. Kiến trúc & thành phần chính
- **StorefrontComposer service** gom dữ liệu mỹ phẩm, giỏ hàng và đề xuất cho trang chủ, trang cửa hàng và trung tâm khách hàng. Tất cả controllers tái sử dụng cùng một nguồn tính toán để bảo đảm thống nhất.
- **CartController + session helpers** quản lý giỏ hàng trong session phía khách, cung cấp API JSON (cho AJAX) và fallback chuyển hướng để đảm bảo hoạt động ngay cả khi JavaScript không khả dụng.
- **View models mới** (`StorefrontViewModel`, `CartViewModel`, `CustomerProductsViewModel`) miêu tả rõ các trạng thái cần render và giúp kiểm soát đầu vào của View.
- **JS tăng cường (progressive enhancement)** ở `wwwroot/js/storefront.js` chỉ xử lý nâng cao (toast, cập nhật badge). Nếu lỗi, form tự submit truyền thống.

## 2. Luồng người dùng
1. **Khách truy cập Home** ➜ thấy hero + sản phẩm nổi bật ➜ thêm sản phẩm vào giỏ ngay trên landing.
2. **Xem Store** ➜ lọc theo danh mục/từ khóa ➜ thêm nhiều sản phẩm với form có chọn số lượng.
3. **Giỏ hàng** ➜ chỉnh sửa số lượng, xoá, dọn giỏ ➜ chuẩn bị thanh toán offline hoặc giao hàng.
4. **Customer Portal** ➜ mục “Mỹ phẩm của tôi” hiển thị lịch sử, đề xuất và giỏ thu nhỏ để đặt lại nhanh.

## 3. Ổn định & theo dõi
- **Session health**: bật `AddDistributedMemoryCache` + `AddSession` với `IdleTimeout` 60 phút; badge ở layout đọc trực tiếp từ session để đồng bộ.
- **Thông báo lỗi**: tất cả action trả `TempData` message và JSON `success/message` cho JS; log server vẫn đi qua middleware mặc định.
- **Tính nhất quán dữ liệu**: mọi thay đổi giỏ đều diễn ra qua `CartSessionExtensions`, tránh trùng lặp logic ở controller/view.
- **Fallback**: nếu JS hỏng ➜ form vẫn post bình thường; nếu session rỗng ➜ view hiển thị thông báo có hướng dẫn hành động.

## 4. Backlog gợi ý cho giai đoạn tiếp theo
1. Tích hợp phương thức thanh toán/giao hàng (API đối tác).
2. Áp dụng khuyến mãi/theo dõi tồn kho thời gian thực (signalR hoặc background jobs).
3. Thêm unit test cho `StorefrontComposer` (mock DbContext).
4. Triển khai page theo dõi đơn hàng và lịch sử mua mỹ phẩm chi tiết.
