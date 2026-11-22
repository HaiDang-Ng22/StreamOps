# StreamOps — SRS và nghiệp vụ

**Phiên bản:** 1.1 · **Trạng thái:** baseline cập nhật theo mô hình tài khoản kín · **Ngày:** 21/11/2026

## 1. Tổng quan

StreamOps là ứng dụng web nội bộ cho agency quản lý nhiều streamer, đội nhân sự và hoạt động thương mại. Hệ thống gom thông tin vận hành, lịch, chiến dịch, hiệu suất và tài chính vào một nơi; cung cấp quyền truy cập theo vai trò và phạm vi phụ trách.

### 1.1 Vấn đề cần giải quyết

Thông tin streamer, lịch phát sóng, booking, chi phí và số liệu thường nằm rải rác; trách nhiệm giữa các đội khó theo dõi; số liệu tổng hợp thiếu nguồn gốc; việc chia doanh thu và duyệt khoản chi dễ nhầm lẫn; nhân viên mới/đổi đội làm dữ liệu khó kiểm soát.

### 1.2 Mục tiêu đo lường

- Có thể tìm hồ sơ streamer, lịch, hợp đồng và chiến dịch trong một hệ thống.
- Mỗi booking/công việc có người chịu trách nhiệm, trạng thái và thời hạn.
- Đối chiếu được tổng doanh thu, chi phí, khoản phải trả theo kỳ và nguồn dữ liệu.
- Mọi truy cập nhạy cảm và thay đổi trạng thái tài chính được ghi nhận.
- Quản lý xem KPI theo thời gian, đội, streamer và nền tảng với định nghĩa nhất quán.

### 1.3 Ngoài phạm vi bản đồ án

Thu thập livestream realtime từ mọi nền tảng; trả lương và thuế chính thức; ký hợp đồng điện tử; xử lý thanh toán ngân hàng; quản lý kho; mạng xã hội công khai; ứng dụng mobile native; dự báo AI. Có thể mô phỏng dữ liệu hoặc nhập CSV ở bản đầu.

## 2. Tác nhân và vai trò

Vai trò là tập quyền. Người dùng có thể có nhiều vai trò và tư cách thành viên đội. Quyền thực tế là giao của **quyền thao tác** và **phạm vi dữ liệu**; mọi API đều phải xác minh cả hai.

| Vai trò | Trách nhiệm | Phạm vi mặc định |
|---|---|---|
| Chủ hệ thống (Platform Admin) | Cấu hình nền tảng, hỗ trợ tenant | Toàn nền tảng; không tự động có quyền nghiệp vụ agency |
| Chủ agency / Agency Admin | Cấu hình agency, người dùng, đội, chính sách | Agency; dữ liệu nhạy cảm chỉ theo quyền cấp |
| Ban điều hành | Mục tiêu, duyệt ngoại lệ, xem báo cáo tổng hợp | Agency hoặc đơn vị được giao |
| HR Manager | Hồ sơ nhân viên, tuyển dụng, onboarding/offboarding, đánh giá | Nhân sự agency; không xem tài chính streamer mặc định |
| HR Staff | Cập nhật hồ sơ và quy trình HR được giao | Hồ sơ/đợt tuyển dụng được phân công; trường nhạy cảm bị giới hạn |
| Talent/Streamer Manager | Chăm sóc và phát triển streamer, phối hợp đội | Streamer được phân công |
| Operations Manager | Lịch livestream, quy trình, sự cố, SLA | Đội vận hành và streamer liên quan |
| Content/Creative | Kế hoạch nội dung, tài nguyên sáng tạo, lịch sản xuất | Chiến dịch/tài sản được giao |
| Booking/Partnership | Khách hàng, cơ hội, booking, giao nhận quyền lợi | Cơ hội/booking được giao |
| Marketing | Chiến dịch, ngân sách marketing, hiệu quả | Chiến dịch/đội được giao |
| Finance/Accountant | Ghi nhận, đối soát, chi phí, payout, báo cáo tài chính | Giao dịch tài chính trong agency |
| Analyst | Chỉ số, dashboard, xuất dữ liệu đã cấp | Phạm vi đội/streamer được cấp; đọc là chính |
| Streamer | Xem lịch, nhiệm vụ, số liệu và khoản chia của mình | Chỉ hồ sơ và dữ liệu của chính mình |
| Auditor/Read-only | Kiểm tra và xem lịch sử | Phạm vi được ủy quyền; không sửa |

**Lưu ý HR:** hồ sơ nhân viên tách khỏi tài khoản đăng nhập. Trường nhạy cảm (định danh, lương, ghi chú kỷ luật) cần quyền riêng, che/mask khi không cần và ghi audit khi truy cập. HR không tự duyệt payout hay tự duyệt thay đổi lương do chính mình tạo.

## 2.1 Nhóm người dùng và chính sách đăng ký

StreamOps là hệ thống nghiệp vụ của công ty, không phải mạng xã hội. Có ba vùng tách biệt:

| Nhóm | Cách được ghi nhận | Có tài khoản đăng nhập? | Phạm vi |
|---|---|---:|---|
| Nhân viên nội bộ | Admin mời/tạo sau khi xác minh quan hệ lao động | Có, sau khi kích hoạt | Quyền theo role, đội và nhiệm vụ |
| Streamer/talent | Nhân viên tạo hồ sơ sau khi xét duyệt hợp tác | Có thể có cổng streamer sau lời mời | Chỉ dữ liệu cá nhân được công ty công bố |
| Khách hàng/nhãn hàng/nhà tài trợ | Nhân viên tạo CRM; bên ngoài gửi form hợp tác công khai | Không trong MVP | Không truy cập hệ thống nội bộ |
| Ứng viên streamer | Gửi form ứng tuyển công khai | Không | Chỉ gửi và tra cứu yêu cầu bằng mã tiếp nhận tùy chọn |
| Người xem livestream | Không cần đăng ký StreamOps | Không | Không có quyền vào hệ thống |

**Chính sách chốt cho đồ án:** không tự đăng ký nhân viên. Admin tạo lời mời có email, vai trò ban đầu tối thiểu, đội/phạm vi dự kiến và thời hạn; người nhận xác minh email, đặt mật khẩu, đồng ý chính sách rồi kích hoạt. Vai trò nhạy cảm và quyền duyệt phải do admin/approver cấp riêng sau khi kích hoạt. Không cho người mời cấp quyền cao hơn quyền của mình.

Admin đầu tiên được tạo duy nhất trong quy trình cài đặt: deployment secret dùng một lần, hết hạn, chỉ chạy khi chưa có admin; sau khi bootstrap phải xóa secret/đóng endpoint. Không có public endpoint tạo admin. Nếu hệ thống mất admin thì dùng quy trình phục hồi vận hành có xác minh và audit, không tự mở lại bootstrap.

Form ứng tuyển/hợp tác công khai chỉ ghi vào bảng yêu cầu chờ xử lý. Form không nhận role, agency ID, trạng thái duyệt hay quyền do client đề xuất; server tự gán trạng thái `received`, giới hạn tần suất/kích cỡ, xác thực dữ liệu và lưu tối thiểu thông tin cần thiết. Không trả thông tin nội bộ hoặc xác nhận email đã tồn tại.

### Trạng thái tài khoản và lời mời

- Tài khoản: `invited`, `active`, `suspended`, `deactivated`; yêu cầu đăng ký cũ nếu giữ lại là thực thể riêng `pending_review`, không phải tài khoản có quyền.
- Lời mời: `pending`, `accepted`, `expired`, `revoked`; chỉ dùng một lần, gắn một email chuẩn hóa và agency, hết hạn, token ngẫu nhiên đủ mạnh chỉ lưu hash.
- Người chưa `active` không được gọi API nghiệp vụ. Từ chối/khóa phải thu hồi phiên và refresh token.
- Không tự chọn role khi tạo tài khoản. Role là assignment do người có quyền cấp; quyền mới có hiệu lực sau khi kiểm tra scope và audit.

## 3. Cấu trúc tổ chức nhiều đội

Đội mẫu gồm: Talent Management, Live Operations, Content & Creative, Booking & Partnerships, Marketing, Finance & Accounting, Human Resources, Data & Analytics, Executive. Agency có thể đổi tên, phân cấp cha-con và gán đội trưởng.

- Người dùng thuộc một agency; quan hệ đội-nhân viên có ngày bắt đầu/kết thúc, chức năng và trạng thái.
- Một streamer có thể có một manager chính và nhiều nhóm hỗ trợ; assignment có vai trò, ngày hiệu lực và người phân công.
- Công việc/booking có thể liên phòng ban; người sở hữu chịu trách nhiệm cuối, các đội phối hợp được gán riêng.
- Chuyển đội không xóa lịch sử assignment và quyền cũ phải chấm dứt theo ngày hiệu lực.

## 4. Phạm vi chức năng

### F01. Đăng nhập và tài khoản

Đăng nhập/đăng xuất, mời tài khoản, kích hoạt, đặt lại mật khẩu, gán vai trò, quản lý phiên và trạng thái. Không có đăng ký nhân viên tự do ở MVP. Khôi phục mật khẩu không tiết lộ việc email có tồn tại. Hệ thống không dùng tài khoản chung cho nhiều nhân viên.

### F01a. Tiếp nhận yêu cầu công khai

Trang public cho ứng tuyển streamer và yêu cầu hợp tác thương hiệu. Yêu cầu được chống spam, có thời hạn lưu, trạng thái xử lý và người phụ trách; đính kèm file (nếu bật) được kiểm tra loại/kích cỡ và cách ly. Chuyển đơn ứng tuyển thành hồ sơ streamer là thao tác nội bộ được phân quyền, tạo bản ghi mới và giữ liên kết nguồn; không tự tạo user.

### F02. Tổ chức và HR

Quản lý đội và cấp bậc; hồ sơ nhân viên, chức danh, quản lý trực tiếp, ngày vào/nghỉ, trạng thái; tuyển dụng, onboarding/offboarding, tài liệu và đánh giá hiệu suất. Chỉ lưu trường cá nhân cần thiết cho đồ án; cấu hình quyền từng nhóm trường.

### F03. Hồ sơ streamer

Thông tin nghệ danh/tên hiển thị, trạng thái quan hệ, liên hệ, nền tảng, kênh, mã kênh, ngôn ngữ, múi giờ, ngày bắt đầu, manager, ghi chú, tài liệu/hợp đồng và lịch sử thay đổi. Trạng thái: Lead → Đang onboarding → Đang hoạt động → Tạm ngưng → Đã kết thúc; chuyển trạng thái có điều kiện và lý do.

### F04. Lịch và phiên livestream

Lập lịch theo streamer, kênh, nền tảng, thời gian bắt đầu/kết thúc dự kiến, mục tiêu và chiến dịch; phát hiện trùng lịch; trạng thái dự kiến/đã xác nhận/đang live/hoàn thành/hủy/vắng. Phiên hoàn thành lưu thời gian thực tế, dữ liệu hiệu suất, nguồn, người nhập và ghi chú.

### F05. Công việc và phối hợp

Tạo việc gắn streamer/booking/chiến dịch, đội chủ trì, người phụ trách, người phối hợp, ưu tiên, hạn, checklist, tệp và bình luận. Lưu lịch sử trạng thái; việc quá hạn có cảnh báo.

### F06. Khách hàng, cơ hội và booking

Quản lý khách hàng/đối tác, cơ hội, đề xuất, booking, streamer tham gia, deliverable, thời hạn, giá trị dự kiến/thực tế, chi phí, điều khoản và trạng thái. Không cho đánh dấu hoàn thành khi deliverable bắt buộc chưa được nghiệm thu.

### F07. Tài chính và đối soát

Ghi nhận nguồn doanh thu (quà tặng, quảng cáo, booking, tài trợ, affiliate/khác), chi phí, tiền tệ, ngày phát sinh, streamer/booking, chứng từ và nguồn. Hỗ trợ cấu hình quy tắc chia theo hợp đồng và thời hạn hiệu lực. Tạo kỳ đối soát, xem trước, gửi duyệt, duyệt/từ chối, khóa kỳ, ghi nhận khoản phải trả và đánh dấu đã thanh toán bằng tham chiếu chứng từ.

### F08. Phân tích và báo cáo

Dashboard theo thời gian, streamer, đội, nền tảng, booking: số giờ live, số phiên, lượt xem, người xem trung bình/cao nhất, tương tác, doanh thu gộp, chi phí, lợi nhuận, tỷ lệ hoàn thành booking. Chỉ hiển thị KPI khi có định nghĩa, đơn vị, nguồn và khoảng thời gian. Xuất CSV/PDF theo quyền.

### F09. Thông báo và kiểm toán

Thông báo trong ứng dụng về lịch, hạn việc, duyệt tài chính, đổi assignment. Audit log lưu ai, hành động, đối tượng, thời gian, giá trị trước/sau phù hợp, request ID. Không ghi mật khẩu, token hoặc dữ liệu bí mật vào log.

## 5. Quy trình nghiệp vụ chính

### 5.0 Khởi tạo hệ thống và cấp tài khoản

1. Người triển khai cấu hình agency, domain/email gửi thư, secret và tài khoản bootstrap từ môi trường bảo mật; không nhúng secret trong mã nguồn hoặc repository.
2. Bootstrap chỉ tạo một admin khi chưa tồn tại admin và tự đóng sau lần chạy thành công; sự kiện được ghi vào log vận hành/audit.
3. Admin mời nhân viên bằng email đã xác minh nhu cầu; hệ thống tạo invitation token một lần, chỉ lưu hash, gửi URL ngắn hạn.
4. Người nhận xác minh quyền sở hữu email, chấp nhận lời mời, tạo credential và được kích hoạt với quyền tối thiểu được duyệt.
5. Admin phân đội/role cụ thể; hành động cấp quyền có audit. Thu hồi/đình chỉ sẽ vô hiệu hóa phiên hiện hành.

### 5.0a Ứng tuyển streamer và hợp tác thương hiệu

1. Người ngoài gửi form không cần tài khoản.
2. API public áp dụng rate limit, honeypot/kiểm tra chống bot, validation, chống nội dung độc hại và giới hạn upload; chỉ lưu dữ liệu tối thiểu.
3. Người phụ trách nội bộ xem và đổi trạng thái (received, reviewing, shortlisted, accepted/rejected, closed) kèm audit.
4. Sau chấp thuận, nhân viên tạo streamer profile/CRM record và hợp đồng; streamer được mời riêng vào cổng cá nhân nếu cần.
5. Yêu cầu hợp tác của brand thành prospect/customer nội bộ; không phát hành tài khoản đối tác trong MVP.

### 5.1 Tiếp nhận streamer

1. Talent Manager tạo hồ sơ Lead và ghi nguồn giới thiệu.
2. Quản lý đánh giá, ghi nhận nền tảng/kênh và điều kiện hợp tác.
3. Khi chấp nhận: tạo hồ sơ hợp đồng/điều khoản, checklist onboarding và assignment cho manager/đội.
4. HR chỉ quản lý nhân sự nội bộ; không sở hữu hồ sơ streamer trừ khi được phân công theo nghiệp vụ.
5. Khi checklist bắt buộc hoàn tất và hợp đồng có hiệu lực, chuyển sang Đang hoạt động; ghi người duyệt và thời điểm.

### 5.2 Lập lịch và ghi nhận phiên

1. Manager/Operations tạo lịch, chọn nền tảng và kiểm tra trùng lặp.
2. Streamer xác nhận hoặc yêu cầu đổi lịch; Operations chốt lịch.
3. Sau buổi live, nhập thời gian thực tế và metric thủ công/CSV/API.
4. Hệ thống kiểm tra đơn vị, khoảng giá trị, nguồn và chống ghi trùng theo external ID (nếu có).
5. Metric đã đưa vào báo cáo được điều chỉnh bằng bản ghi mới hoặc phiên bản điều chỉnh có audit.

### 5.3 Booking thương mại

1. Booking team tạo cơ hội và gán khách hàng.
2. Đề xuất streamer, deliverables, thời hạn, phí và chi phí dự kiến.
3. Sau phê duyệt điều khoản, chuyển booking sang Đã xác nhận; tạo công việc và lịch.
4. Team cập nhật tiến độ; người có thẩm quyền nghiệm thu deliverable.
5. Finance ghi nhận doanh thu theo chứng từ/điều kiện hợp đồng và đối chiếu chi phí.

### 5.4 Đối soát chia doanh thu

1. Finance chọn kỳ, streamer và nguồn giao dịch đủ điều kiện.
2. Hệ thống tính tổng doanh thu, khoản khấu trừ hợp lệ, tỷ lệ chia có hiệu lực, phần streamer và phần agency; lưu snapshot quy tắc dùng để tính.
3. Finance rà soát sai lệch, đính kèm chứng từ và gửi duyệt.
4. Người duyệt độc lập duyệt hoặc từ chối kèm lý do.
5. Kỳ được khóa; payout chuyển qua chờ thanh toán, đã thanh toán hoặc thất bại.
6. Sai sót sau khóa tạo điều chỉnh kỳ tiếp theo hoặc bút toán đảo có liên kết; không sửa trực tiếp số đã duyệt.

### 5.5 Vòng đời nhân viên

HR tạo hồ sơ nhân sự và tài khoản liên kết riêng; onboarding cấp thiết bị/quyền theo chức năng; trưởng đội giao việc; HR cập nhật đánh giá/ngày nghỉ theo chính sách; offboarding vô hiệu hóa quyền truy cập, chuyển giao công việc và lưu hồ sơ theo thời hạn lưu trữ. Không xóa lịch sử assignment/audit.

## 6. Use Case

| ID | Use Case | Tác nhân chính | Kết quả |
|---|---|---|---|
| UC01 | Đăng nhập | Mọi người dùng | Phiên hợp lệ theo tài khoản và agency |
| UC02 | Tạo/cập nhật đội và thành viên | Admin, HR Manager | Cơ cấu và lịch sử membership được lưu |
| UC03 | Onboard nhân viên | HR | Hồ sơ, checklist và tài khoản liên kết |
| UC04 | Tạo hồ sơ streamer | Talent Manager | Hồ sơ ở Lead hoặc onboarding |
| UC05 | Gán đội phụ trách streamer | Admin, Team Lead | Assignment có hiệu lực và phạm vi quyền |
| UC06 | Lập lịch live | Operations, Manager | Lịch được lưu sau kiểm tra xung đột |
| UC07 | Ghi nhận kết quả phiên | Operations, Analyst | Metric có nguồn và người nhập |
| UC08 | Quản lý booking | Booking team | Booking, deliverables và trạng thái |
| UC09 | Ghi nhận giao dịch | Finance | Giao dịch có loại, chứng từ và liên kết |
| UC10 | Tạo và gửi đối soát | Finance | Snapshot kỳ chờ phê duyệt |
| UC11 | Duyệt/từ chối đối soát | Finance Approver, Executive | Quyết định có lý do và audit |
| UC12 | Xem dashboard/xuất báo cáo | Executive, Analyst, Finance | Báo cáo theo quyền và bộ lọc |
| UC13 | Xem thông tin cá nhân | Streamer, Employee | Chỉ dữ liệu được cấp |
| UC14 | Tra cứu nhật ký | Auditor, Admin | Lịch sử thao tác không bị sửa |
| UC15 | Mời và kích hoạt nhân viên | Admin, nhân viên được mời | Tài khoản nội bộ active, không tự gán quyền |
| UC16 | Gửi đơn ứng tuyển/hợp tác | Người ngoài | Yêu cầu chờ xử lý, không có account |
| UC17 | Xét duyệt đơn công khai | Talent/Booking, approver | Hồ sơ CRM nội bộ được tạo có nguồn |
| UC18 | Mời streamer vào cổng cá nhân | Talent Manager, streamer | Streamer chỉ truy cập dữ liệu của mình |

### Đặc tả UC10–11: đối soát

**Tiền điều kiện:** người dùng có quyền Finance, đúng agency, giao dịch hợp lệ và kỳ chưa khóa.  
**Luồng chính:** chọn kỳ → chọn phạm vi → tính thử → xử lý ngoại lệ → gửi duyệt → người duyệt độc lập xem snapshot và chứng từ → duyệt → khóa kỳ.  
**Ngoại lệ:** không có tỷ lệ chia hiệu lực, thiếu chứng từ, giao dịch trùng, số âm không có loại điều chỉnh, người tạo tự duyệt, hoặc kỳ đã khóa thì dừng và nêu lỗi cụ thể.  
**Hậu điều kiện:** lưu trạng thái, snapshot quy tắc, tổng tiền theo đơn vị tiền tệ, người tạo/duyệt và thời gian; phát sinh payout nếu duyệt.

## 7. Ma trận quyền rút gọn

Ký hiệu: **CRUD** tạo/đọc/sửa/xóa nháp; **R** đọc; **A** duyệt. Xóa dữ liệu tài chính/HR đã phát sinh bị cấm; dùng trạng thái ngừng hoạt động.

| Nghiệp vụ | Admin | HR | Talent/Ops | Booking/Marketing | Finance | Analyst | Streamer |
|---|---:|---:|---:|---:|---:|---:|---:|
| Tài khoản/đội | CRUD | R, HR fields | — | — | — | — | — |
| Hồ sơ nhân viên | R* | CRUD HR | — | — | — | — | — |
| Hồ sơ streamer | CRUD | — | CRUD assigned | R assigned | R cần thiết | R assigned | R bản thân |
| Lịch/phiên | CRUD | — | CRUD assigned | R liên quan | R cần thiết | R assigned | R bản thân |
| Booking | CRUD | — | R liên quan | CRUD assigned | R tài chính | R assigned | R bản thân |
| Giao dịch/đối soát | Cấu hình | — | — | — | CRUD, A độc lập | R ẩn trường nhạy cảm | R phần mình |
| Dashboard | R tổng hợp* | HR metrics | Phạm vi đội | Phạm vi chiến dịch | Tài chính | Phạm vi cấp | Cá nhân |
| Audit log | R* | HR events | — | — | Tài chính events | — | — |

`*` Admin/HR chỉ thấy trường và scope được cấp; tài khoản Admin nền tảng không mặc định được đọc dữ liệu khách hàng agency.

### Quy tắc scope

- **Agency scope:** mọi bản ghi nghiệp vụ có `agency_id`; API lấy agency từ claim phiên, không tin agency do client gửi.
- **Team scope:** nhân viên chỉ xem/sửa đối tượng của đội mà họ đang là thành viên, theo policy của vai trò.
- **Assigned scope:** Streamer Manager chỉ thao tác với streamer còn assignment hiệu lực.
- **Self scope:** streamer chỉ xem hồ sơ, lịch, metric và payout của mình; không thấy ghi chú nội bộ.
- **Field scope:** lương, định danh, tài khoản nhận tiền và ghi chú HR được bảo vệ riêng.
- **Separation of duties:** người lập đối soát không được duyệt chính đối soát đó; cấu hình tỷ lệ chia cần người thứ hai duyệt.

## 8. Quy tắc nghiệp vụ

1. Mọi thời điểm được lưu UTC; hiển thị theo múi giờ của agency/streamer.
2. Tiền luôn có currency ISO 4217; không cộng tổng khác tiền tệ khi chưa quy đổi theo tỷ giá và ngày tỷ giá.
3. Doanh thu gộp, phí nền tảng, thuế/khấu trừ, chi phí, lợi nhuận và payout là khái niệm riêng; không dùng lẫn.
4. Quy tắc chia có ngày hiệu lực, phiên bản và người duyệt. Giao dịch dùng snapshot quy tắc tại thời điểm áp dụng.
5. Trạng thái được chuyển qua lệnh nghiệp vụ hợp lệ; không cho client tùy ý cập nhật chuỗi trạng thái.
6. Xóa mềm cho dữ liệu danh mục; giao dịch đã ghi sổ, hợp đồng, audit và đối soát được lưu lịch sử.
7. Một streamer có thể hiện diện trên nhiều nền tảng; định danh kênh duy nhất theo nền tảng và agency.
8. Chỉ số thủ công phải ghi người nhập, thời điểm nhập, nguồn và ghi chú; import phải có file/batch tham chiếu.
9. Không có public registration để cấp tài khoản nhân viên, admin hay đối tác trong MVP; public form chỉ tạo application/request.
10. Admin đầu tiên chỉ được tạo một lần trong bootstrap an toàn; mọi admin tiếp theo phải được admin hiện hữu mời và cấp quyền có audit.
11. Token mời/reset lưu dạng hash, có hạn và dùng một lần; không ghi token vào log/analytics. Phản hồi public không tiết lộ email/account tồn tại.
12. Dữ liệu form public bị giới hạn truy cập theo queue/assignment; có chính sách thời gian lưu và xóa/ẩn danh khi hết mục đích.
13. API chỉ tin `agency_id` lấy từ identity/session đã xác thực; mọi object được tải phải kiểm tra cùng agency và scope trước khi trả hoặc sửa.
14. Nếu dùng multi-agency, không cho người dùng tự tạo agency/tenant hoặc chọn tenant tùy ý; việc onboard tenant do platform operator có quy trình kiểm soát.
15. Vai trò platform admin tách với vai trò nghiệp vụ agency; hỗ trợ có quyền tạm thời, lý do, thời hạn và audit khi cần truy cập.

## 9. Yêu cầu phi chức năng

- **Bảo mật:** mật khẩu băm bởi thư viện chuẩn; HTTPS; xác thực; kiểm soát quyền phía server; chống truy cập chéo agency; bảo vệ CSRF/XSS/SQL injection; bí mật qua biến môi trường/secret store.
- **Riêng tư:** tối thiểu hóa dữ liệu HR và streamer; mask dữ liệu nhạy cảm; ghi audit truy cập; có quy định lưu trữ/xóa theo chính sách agency.
- **Hiệu năng mục tiêu đồ án:** API danh sách thường dưới 2 giây với 50 người dùng đồng thời và bộ dữ liệu mẫu; phân trang, lọc server-side, chỉ mục phù hợp.
- **Toàn vẹn:** giao dịch tài chính dùng transaction DB, ràng buộc FK/check/unique; thao tác duyệt có optimistic concurrency.
- **Khả dụng:** thông báo lỗi có mã truy vết; health check; backup PostgreSQL và hướng dẫn khôi phục.
- **Khả dụng tiếp cận:** giao diện responsive, nhãn form rõ, điều hướng bàn phím cơ bản, tiếng Việt là ngôn ngữ mặc định.
- **Quan sát:** structured logs, request ID, audit events; không log secret.
- **Mở rộng:** tích hợp nền tảng qua adapter/import batch; API versioning; tách module theo miền nghiệp vụ trước khi cân nhắc microservices.

## 10. Giả định và quyết định cần chốt

- Đồ án MVP phục vụ một agency đã cấu hình; `agency_id` được giữ để cô lập tenant. SaaS nhiều agency là giai đoạn sau và cần quy trình tạo tenant riêng.
- Chính sách tài khoản đã chốt: admin mời nhân viên; ứng viên/brand chỉ gửi public form; streamer được mời sau khi hợp tác được chấp thuận.
- Múi giờ mặc định và tiền tệ báo cáo (đề xuất Asia/Ho_Chi_Minh và VND).
- Công thức chia theo từng nền tảng/hợp đồng, khấu trừ nào hợp lệ, cách xử lý hoàn tiền.
- KPI nào lấy tự động và KPI nào nhập thủ công; định nghĩa “view”, “viewer trung bình”, “doanh thu”.
- Thời hạn lưu hồ sơ HR, hợp đồng và chứng từ; cần chốt theo yêu cầu giảng viên/đơn vị.
- Phê duyệt chi phí/đối soát cần bao nhiêu cấp và ngưỡng tiền.
