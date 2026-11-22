# Kết nối Neon và khởi tạo môi trường

## Bí mật cấu hình

Không ghi URL Neon, mật khẩu, JWT signing key, mật khẩu bootstrap hoặc token invitation vào source, issue, README hay ảnh chụp terminal. Repo có User Secrets ID; trong PowerShell đặt các giá trị vào secret store của .NET từ thư mục API:

```powershell
cd src/api/StreamOps.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet user-secrets set 'DATABASE_URL' '<Neon connection URL>'
dotnet user-secrets set 'JWT_SIGNING_KEY' '<random secret at least 32 bytes>'
dotnet user-secrets set 'JWT_ISSUER' 'streamops-api'
dotnet user-secrets set 'JWT_AUDIENCE' 'streamops-web'
dotnet user-secrets set 'WEB_ORIGIN' 'http://localhost:5173'
```

Tạo JWT key mới ngẫu nhiên, riêng với mật khẩu database. User Secrets chỉ dành cho phát triển trên máy cá nhân; môi trường triển khai dùng secret manager của nền tảng. Không đặt credential Neon vào `.env.example`.

API nhận `DATABASE_URL` dạng PostgreSQL URI, giải mã username/password an toàn và ép TLS cùng channel binding. Không bật `Trust Server Certificate` và không ghi connection string ra log.

Không dùng tài khoản owner Neon chạy trong ứng dụng. Tạo một role runtime riêng có mật khẩu riêng trong Neon; chạy `db/grant_runtime_permissions.sql` bằng owner sau khi tạo schema. Ứng dụng chỉ cần quyền đọc/ghi dữ liệu nghiệp vụ trong schema `streamops`, không cần quyền tạo database/role/schema. Đặt URL của role runtime vào User Secrets; giữ owner credential ngoài ứng dụng.

### Chạy với PostgreSQL local

Với Docker Compose, đặt mật khẩu local trong `.env`, chạy `docker compose up -d postgres`, sau đó khởi tạo schema trên database local:

```powershell
docker compose cp db/schema.sql postgres:/tmp/streamops-schema.sql
docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U streamops_app -d streamops -f /tmp/streamops-schema.sql
```

Hướng dẫn từng bước chạy ba thành phần ở local nằm trong [05-huong-dan-chay-do-an.md](05-huong-dan-chay-do-an.md).

## Tạo schema

`db/schema.sql` tạo riêng namespace `streamops`; script không xóa bảng hoặc schema hiện có. Đọc/review script trước khi áp dụng. Với Neon đang có dữ liệu, kiểm tra schema inventory và backup trước; chỉ chạy một lần cho database chưa có schema StreamOps. Không chạy `DROP`, `EnsureCreated` hay migration tự động khi ứng dụng khởi động.

Thực thi schema bằng công cụ quản trị SQL được kiểm soát. Sau khi chạy, xác minh `streamops.schema_version` có version 1, kiểm tra bảng ở schema `streamops`, rồi mới bootstrap tài khoản. Mọi thay đổi schema về sau phải thêm migration có version và review; không sửa script version 1 đã áp dụng.

## Bootstrap admin lần đầu

Sau khi schema được khởi tạo, đặt bốn giá trị bằng User Secrets: `BOOTSTRAP_ADMIN_EMAIL`, `BOOTSTRAP_ADMIN_PASSWORD`, `BOOTSTRAP_AGENCY_NAME`, `BOOTSTRAP_AGENCY_SLUG`. Với Neon, chỉ dùng owner URL tạm thời cho lệnh bootstrap chạy một lần tại máy quản trị; script bootstrap cần tạo role catalog và tài khoản Admin nên role runtime bị cấm thực hiện các thao tác này. Ngay sau khi chạy xong, đổi `DATABASE_URL` sang role runtime, xóa owner URL khỏi User Secrets, rồi xóa bốn giá trị bootstrap. Không khởi chạy web/API bình thường bằng owner URL. Mật khẩu Admin phải đạt policy Identity.

```powershell
dotnet run -- --bootstrap-admin
```

Lệnh chỉ chạy nếu bảng user chưa có tài khoản; không có endpoint HTTP để tạo admin. Sau thành công, xóa bốn secrets bootstrap khỏi secret store. Tài khoản tạo tiếp theo đi qua API mời; role `Admin` bị loại khỏi danh sách role có thể mời để tránh tự nâng quyền.

## Khởi chạy

```powershell
dotnet run --urls http://localhost:5080
```

Frontend hiện chạy bằng Vite ở `http://localhost:5173`. API endpoint công khai gồm liveness, đăng nhập, nhận lời mời, form ứng tuyển và form yêu cầu hợp tác. Các API nội bộ bắt buộc bearer token và agency claim.

## Trạng thái bảo mật

- Có: JWT ngắn hạn 15 phút, issuer/audience/signature validation, login lockout, role allowlist cho invitation, token mời 256-bit chỉ lưu hash và hết hạn 24 giờ, rate limit cho login/public form, độ dài form giới hạn và kiểm tra tenant ở endpoint đã viết.
- Cần hoàn thiện trước production: email gửi invitation, thu hồi access token trước hạn, MFA, RLS PostgreSQL có kiểm thử pooling, retention job cho form PII, kiểm tra file upload, scope chi tiết cho mọi module, audit bất biến và diễn tập khôi phục backup.
- Schema version 1 là script khởi tạo thủ công; bản hiện tại đã áp dụng trên PostgreSQL local, chưa áp dụng lên Neon. Project nhắm .NET 10; cài .NET 10 SDK để build và chạy trên máy khác.

## Trạng thái triển khai của bản dựng

Đây là prototype đang phát triển. Giao diện đăng nhập và một số module gọi API; dashboard vẫn có số liệu/lịch minh họa. Bản local đã build, PostgreSQL schema đã áp dụng, Admin khởi tạo và trang đăng nhập/API liveness đã mở. Chưa áp dụng bất kỳ schema hay dữ liệu nào lên Neon. Các endpoint chính sách agency không thay thế cho kiểm thử bảo mật độc lập. Không nhập dữ liệu nhân sự, hợp đồng hoặc tài chính thật cho đến khi hoàn tất review quyền, migration, backup/restore và kiểm thử.

Script schema có trigger chỉ-ghi-đè cho audit, khóa giao dịch sau khi ghi sổ, bất biến dòng đối soát và settlement đã khóa. Đây là guardrail bổ sung, không phải bằng chứng hệ thống không thể bị xâm nhập. Script cấp quyền runtime vẫn cần review theo đúng role trong Neon trước khi sử dụng.
