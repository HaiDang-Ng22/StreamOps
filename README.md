# StreamOps

**Streamer Operations & Analytics Management System** là hệ thống web hỗ trợ công ty quản lý nhiều streamer, nhiều đội nghiệp vụ và nhiều nền tảng livestream. Sản phẩm kết hợp quản lý vận hành với phân tích hiệu quả kinh doanh.

> Đồ án: **Xây dựng hệ thống quản lý và phân tích hoạt động kinh doanh Streamer (StreamOps)**

## Mục tiêu

- Quản lý hồ sơ streamer, nhân viên, đội nhóm và phân công phụ trách.
- Lập lịch, theo dõi phiên livestream, chiến dịch, booking và công việc.
- Ghi nhận doanh thu, chi phí, quy tắc chia sẻ và kỳ đối soát.
- Theo dõi chỉ số hiệu suất và xuất báo cáo cho quản lý.
- Áp dụng phân quyền theo vai trò kết hợp phạm vi streamer/đội nhóm.

## Định hướng kỹ thuật

| Thành phần | Đề xuất |
|---|---|
| Frontend | React + TypeScript, ứng dụng SPA |
| Backend | ASP.NET Core Web API |
| Cơ sở dữ liệu | PostgreSQL |
| Truy cập dữ liệu | Entity Framework Core |
| Xác thực | ASP.NET Core Identity hoặc JWT/OIDC |
| Phân quyền | RBAC + kiểm tra phạm vi dữ liệu ở API |
| Triển khai đồ án | Docker Compose (API, web, database) |

## Các đội nghiệp vụ

Agency có thể tổ chức thành các đội: Quản lý streamer (Talent/Creator), Vận hành livestream, Nội dung & Sáng tạo, Booking & Đối tác, Marketing, Tài chính & Kế toán, Nhân sự (HR), Phân tích dữ liệu và Ban điều hành. Một nhân viên có thể thuộc một đội chính và tham gia đội/dự án khác qua bảng thành viên đội.

## Bắt đầu đọc tài liệu

- [Đặc tả yêu cầu, nghiệp vụ, Use Case và phân quyền](docs/01-yeu-cau-va-nghiep-vu.md)
- [Thiết kế cơ sở dữ liệu và quy tắc dữ liệu](docs/02-thiet-ke-co-so-du-lieu.md)
- [Lộ trình triển khai, API và tiêu chí nghiệm thu](docs/03-thiet-ke-va-lo-trinh.md)
- [Hướng dẫn chạy đồ án trên máy local](docs/05-huong-dan-chay-do-an.md)

## Phạm vi đồ án đề xuất

Hoàn thành đăng nhập và phân quyền, quản lý đội/nhân viên/streamer, lịch livestream và chiến dịch, nhập số liệu phiên, quản lý booking và giao dịch tài chính, quy trình duyệt đối soát, dashboard và nhật ký kiểm toán. Tích hợp API trực tiếp với nền tảng livestream, chấm công/pháp lý điện tử và dự báo nâng cao để ở giai đoạn mở rộng.

## Chính sách tài khoản và website công khai

- Không có đăng ký tự do để trở thành admin hoặc nhân viên. Tài khoản nội bộ do admin khởi tạo/mời; người được mời kích hoạt bằng liên kết một lần, có hạn dùng.
- Streamer không tự tạo tài khoản quản trị. Công ty xét duyệt hồ sơ hợp tác trước, sau đó gửi lời mời vào cổng streamer giới hạn theo hồ sơ của chính họ.
- Trang công khai có thể nhận đơn ứng tuyển streamer và yêu cầu hợp tác thương hiệu. Đây là hồ sơ chờ xử lý, không phải tài khoản đăng nhập và không cấp quyền vào hệ thống.
- Admin đầu tiên chỉ tạo trong quy trình triển khai ban đầu, một lần, có bí mật bootstrap riêng và bị vô hiệu hóa ngay sau khi dùng.
- MVP chỉ phục vụ một agency đã cấu hình; mô hình dữ liệu vẫn gắn `agency_id` để cô lập dữ liệu và có đường mở rộng multi-agency có kiểm soát.

## Nguyên tắc nghiệp vụ cốt lõi

1. Dữ liệu thuộc một agency; mọi truy vấn đều giới hạn theo agency hiện tại và kiểm tra tenant ở server/database.
2. Vai trò quyết định được làm gì; phạm vi đội/streamer quyết định được làm trên dữ liệu nào.
3. HR quản lý hồ sơ nhân viên và quy trình nhân sự, không mặc định xem doanh thu streamer hoặc dữ liệu ngân hàng.
4. Giao dịch đã đưa vào kỳ đối soát không bị sửa/xóa trực tiếp; điều chỉnh bằng bút toán đảo/điều chỉnh có lý do và người duyệt.
5. Số liệu hiệu suất phải có nguồn, thời điểm ghi nhận và đơn vị đo.
6. Hệ thống là công cụ quản lý; không tự thay thế tư vấn pháp lý, thuế hoặc kế toán chính thức.

## Trạng thái

Đã dựng khung React, API ASP.NET Core và schema PostgreSQL; có các luồng nền tảng cho đăng nhập, mời/nhận tài khoản, tiếp nhận đơn công khai, HR/team, streamer, lịch/phiên, booking, giao dịch, đối soát và audit. Một số trang module đã nối API. Trang tổng quan và lịch mẫu vẫn là dữ liệu minh họa. Đây là bản dựng đang phát triển, chưa phải sản phẩm hoàn chỉnh hoặc được chứng nhận an toàn. Bản local hiện đã build thành công, schema đã nạp vào PostgreSQL local và trang đăng nhập/API liveness đang chạy; chưa kết nối Neon. Xem [hướng dẫn kết nối và khởi tạo](docs/04-ket-noi-va-khoi-tao.md) trước khi cấu hình database.

### Phạm vi bản dựng hiện tại

| Phần | Đã có trong mã nguồn | Còn cần làm trước nghiệm thu |
|---|---|---|
| Truy cập | Đăng nhập JWT, lockout, bootstrap Admin một lần, lời mời có hạn và token lưu dạng hash | MFA, gửi email, thu hồi phiên/token, validate đầu vào toàn diện, rà soát threat model |
| HR và đội nhóm | Hồ sơ nhân viên cơ bản, tạo đội, thành viên có thời hạn, bộ lọc theo vai trò | Hồ sơ HR đầy đủ, vòng đời nhân viên, nhật ký xem dữ liệu nhạy cảm, UI quản trị thành viên |
| Vận hành streamer | Hồ sơ, phân công, lịch, phiên và nhập metric | Kênh/nền tảng, báo cáo phân tích thật, dashboard API, upload/tài liệu hợp đồng |
| Booking | Khách hàng, booking, streamer tham gia và deliverable API | UI quản lý deliverable/nghiệm thu, trạng thái đóng/hủy, tìm kiếm và phân trang |
| Tài chính | Giao dịch nháp/ghi sổ, quy tắc chia có duyệt, đối soát và payout API | UI tạo/duyệt quy tắc chia và payout, bút toán điều chỉnh, đối soát nghiệp vụ/kế toán |
| Vận hành hệ thống | Schema version 1, Docker PostgreSQL local, script cấp quyền runtime | Migration có phiên bản tiếp theo, backup/restore, giám sát, kiểm tra bảo mật và triển khai |

Các endpoint kiểm soát agency ở server; đây chưa phải bảo đảm rằng hệ thống không thể có lỗ hổng. Cần hoàn thiện kiểm tra phạm vi theo từng chức năng và kiểm chứng bằng review/bài kiểm thử bảo mật trước khi đưa dữ liệu thật vào.

## Chạy bản khung local

Các bước đầy đủ để tạo database local, khởi tạo Admin và chạy API + giao diện được ghi tại [Hướng dẫn chạy đồ án](docs/05-huong-dan-chay-do-an.md).

Không triển khai API ra Internet trước khi hoàn thiện gửi email invitation, MFA, migrations được kiểm tra, scope đầy đủ cho từng chức năng và rà soát bảo mật.
