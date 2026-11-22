# Hướng dẫn chạy đồ án StreamOps (Streamer Operations & Analytics Management System)

Tài liệu này hướng dẫn cách khởi chạy và trải nghiệm đồ án StreamOps một cách nhanh chóng, dễ dàng nhất với **dữ liệu thật từ Neon Cloud PostgreSQL**.

---

## 🚀 Cách 1: Khởi động 1-Click (Khuyên dùng - Siêu dễ)

Tại thư mục gốc của dự án `Streamer Operations & Analytics Management System`:

1. **Nhấp đúp chuột vào file `start-streamops.bat`** (hoặc mở cmd/PowerShell gõ `.\start-streamops.bat`).
2. Script sẽ tự động:
   - Khởi chạy Backend API (.NET 9) tại cổng `5081`.
   - Khởi chạy Frontend Web (React + Vite) tại cổng `5174`.
   - Tự động mở trình duyệt web tại địa chỉ: **`http://127.0.0.1:5174/`**.
3. Muốn tắt toàn bộ hệ thống: Nhấp đúp chuột vào file **`stop-streamops.bat`**.

---

## 🌐 Trạng thái hệ thống đang chạy trực tiếp

| Thành phần | Địa chỉ / Cổng | Ghi chú |
|---|---|---|
| **Web Dashboard** | [http://127.0.0.1:5174/](http://127.0.0.1:5174/) | Giao diện quản trị React + Vite thời gian thực |
| **Backend API** | [http://127.0.0.1:5081/api/v1](http://127.0.0.1:5081/api/v1) | .NET 9 Web API RESTful |
| **Kiểm tra Health** | [http://127.0.0.1:5081/health/live](http://127.0.0.1:5081/health/live) | Trả về `Healthy` |
| **Cơ sở dữ liệu** | **Neon Cloud PostgreSQL (branch `production`)** | Schema `streamops`, 33 bảng, dữ liệu nghiệp vụ thật |

---

## 🔑 Danh sách tài khoản đăng nhập (Phân quyền RBAC)

Toàn bộ các tài khoản nhân sự phòng ban đã được khởi tạo sẵn trong cơ sở dữ liệu Neon. Mật khẩu dùng chung cho tất cả tài khoản:

> **Mật khẩu:** `StreamOps@2026!`

| Vai trò (Role) | Email đăng nhập | Quyền hạn và phạm vi nghiệp vụ |
|---|---|---|
| **Admin (Toàn quyền)** | `admin@streamops.local` | Quản trị toàn bộ agency, cấu hình phân quyền, phân bổ đội ngũ, xem audit logs |
| **Talent Manager** | `talent@streamops.local` | Quản lý hồ sơ streamer, hợp đồng độc quyền, quy tắc chia doanh thu, phân công quản lý |
| **Operations Manager** | `operations@streamops.local` | Lập lịch livestream, giám sát phiên live thời gian thực, đo lường metrics (peak viewer, GMV) |
| **Booking Specialist** | `booking@streamops.local` | Tiếp nhận booking nhãn hàng (Shopee, Samsung...), phân công streamer, nghiệm thu deliverables |
| **Finance Specialist** | `finance@streamops.local` | Quản lý dòng tiền, ghi nhận doanh thu booking, lập kỳ đối soát hoa hồng cho streamer |
| **Finance Approver** | `approver@streamops.local` | Phê duyệt kỳ đối soát tài chính, xác nhận ủy nhiệm chi thanh toán (Payout) |
| **Data Analyst** | `analyst@streamops.local` | Xem báo cáo phân tích doanh thu theo nguồn, hiệu suất streamer theo nền tảng |

---

## 📊 Dữ liệu nghiệp vụ thật trong hệ thống

Hệ thống **không sử dụng dữ liệu tĩnh (mock)**. Toàn bộ thông tin trên màn hình được truy vấn thời gian thực từ Neon PostgreSQL:

1. **Agency & Nhân sự:**
   - Agency: `Nova Creator House`.
   - 7 Đội ngũ phòng ban chuyên trách và 8 nhân sự RBAC.
2. **Streamers & Kênh phát sóng:**
   - 6 Streamer hàng đầu: *Linh Gaming, Mixi Gaming, MisThy Stream, ViruSs Official, Cris Devil Gamer, Độ Phùng Live*.
   - 6 Kênh chính thức trên YouTube, TikTok, Twitch.
   - Hợp đồng & Quy tắc chia doanh thu (70/30, 80/20...).
3. **Lịch livestream & Phiên phát thực tế:**
   - 8 Lịch livestream được xếp theo ca.
   - 4 Phiên live hoàn tất kèm các chỉ số đo lường: người xem đồng thời đỉnh (Peak CCU), người xem trung bình (Avg CCU), tổng doanh thu bán hàng (GMV).
4. **Khách hàng Brand & Chiến dịch Booking:**
   - 3 Khách hàng lớn: *Shopee Vietnam, Samsung Vietnam, VNGGames Publishing*.
   - 3 Booking chiến dịch thực tế: Megasale 11.11, Ra mắt Galaxy Z Series, Livestream giới thiệu Game kiếm hiệp.
   - 5 Hạng mục công việc (Deliverables): Video shorts teaser, Livestream 2 giờ, Bài đăng cộng đồng kèm trạng thái nghiệm thu.
5. **Dòng tiền & Đối soát tài chính:**
   - Doanh thu thực tế: **182.300.000 VNĐ**.
   - 2 Kỳ đối soát hoa hồng: Tháng 10/2026 và Tháng 11/2026.
   - Phiếu thanh toán Payout đã giải ngân kèm mã ủy nhiệm chi (UNC-202611-0012).
6. **Báo cáo phân tích chuyên sâu:**
   - Tab **Báo cáo & Phân tích** trực quan: Tỷ trọng doanh thu theo nguồn (Brand Booking, Nền tảng, Quà tặng Donate), Top streamer doanh thu cao nhất, Tổng giờ livestream và Tỷ lệ hoàn thành công việc.

---

## 🛠️ Cách chạy thủ công từng phần (Nếu muốn)

Nếu bạn muốn mở từng cửa sổ console để theo dõi log chi tiết:

### 1. Chạy Backend API (.NET 9):
```powershell
dotnet run --project src/api/StreamOps.Api --urls http://127.0.0.1:5081
```

### 2. Chạy Frontend Web (Vite):
```powershell
cd src/web
npm run dev -- --host 127.0.0.1 --port 5174
```

### 3. Nạp lại dữ liệu nghiệp vụ lên database (Nếu muốn reset dữ liệu):
```powershell
dotnet run --project src/api/StreamOps.Api --no-build -- --seed-data
```

---

## 🎯 Kịch bản Demo báo cáo đồ án

Khi trình bày đồ án với giảng viên hoặc hội đồng đánh giá:

1. **Tổng quan điều hành (Live Dashboard):**
   - Đăng nhập `admin@streamops.local`.
   - Giới thiệu các chỉ số KPI động: Số streamer hoạt động (4/6), Lịch phát sóng hôm nay (4 ca), Booking đang chạy (3), Doanh thu lũy kế (182.3M VND).
   - Xem lịch livestream hôm nay, danh sách streamer dẫn đầu doanh thu, danh sách công việc cần xử lý.
2. **Quy trình Quản lý Streamer & Kênh:**
   - Mở module **Streamers**: Xem hồ sơ streamer, trạng thái hợp đồng, ngày ký và kênh phát sóng liên kết.
   - Thử chức năng thêm streamer hoặc cập nhật trạng thái hợp đồng.
3. **Quy trình Booking nhãn hàng & Nghiệm thu (Deliverables):**
   - Mở module **Bookings**: Xem chiến dịch Shopee 11.11, Samsung Galaxy Z Fold.
   - Bấm **"Chi tiết"** của một booking: Xem danh sách deliverable, thực hiện nghiệm thu công việc trực tiếp trên modal.
4. **Quy trình Tài chính & Phê duyệt Đối soát (Settlement & Payout):**
   - Mở module **Kỳ đối soát & Payout**: Xem danh sách đối soát theo tháng, xem phiếu chi trả kèm mã ủy nhiệm chi ngân hàng.
   - Đăng xuất và đăng nhập bằng tài khoản `approver@streamops.local` để thử tính năng phê duyệt đối soát.
5. **Báo cáo phân tích (Analytics):**
   - Mở tab **Báo cáo & Phân tích**: Trình bày biểu đồ phân bổ doanh thu theo nguồn và bảng tổng hợp hiệu suất vận hành của từng streamer.
