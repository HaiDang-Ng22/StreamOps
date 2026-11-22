import { FormEvent, useEffect, useMemo, useState } from 'react';
import { apiRequest } from '../api/client';

type Row = Record<string, unknown>;
type Field = {
  key: string;
  label: string;
  type?: 'text' | 'number' | 'date' | 'datetime-local' | 'email' | 'select' | 'multi';
  required?: boolean;
  options?: { label: string; value: string }[];
};

type ModuleDefinition = {
  title: string;
  eyebrow: string;
  description: string;
  path: string;
  columns: { key: string; label: string }[];
  fields?: Field[];
};

const definitions: Record<string, ModuleDefinition> = {
  streamers: {
    title: 'Streamer & Creator',
    eyebrow: 'TALENT MANAGEMENT',
    description: 'Hồ sơ, trạng thái hợp tác và thông tin phân công quản lý creator.',
    path: '/streamers',
    columns: [
      { key: 'code', label: 'Mã' },
      { key: 'stageName', label: 'Nghệ danh' },
      { key: 'status', label: 'Trạng thái' },
      { key: 'timeZone', label: 'Múi giờ' }
    ],
    fields: [
      { key: 'code', label: 'Mã streamer (ví dụ: STM07)', required: true },
      { key: 'stageName', label: 'Nghệ danh', required: true },
      { key: 'timeZone', label: 'Múi giờ', required: true }
    ]
  },
  channels: {
    title: 'Kênh phát sóng',
    eyebrow: 'PLATFORMS & CHANNELS',
    description: 'Danh sách kênh livestream TikTok Live, YouTube, Twitch của các streamer.',
    path: '/channels',
    columns: [
      { key: 'streamerName', label: 'Streamer' },
      { key: 'platformCode', label: 'Nền tảng' },
      { key: 'handle', label: 'Handle / Kênh' },
      { key: 'url', label: 'Đường dẫn' },
      { key: 'status', label: 'Trạng thái' }
    ]
  },
  schedules: {
    title: 'Lịch livestream',
    eyebrow: 'LIVE OPERATIONS',
    description: 'Lập lịch phát sóng theo streamer, kiểm tra trùng khung giờ tự động.',
    path: '/schedules',
    columns: [
      { key: 'title', label: 'Nội dung' },
      { key: 'streamerId', label: 'Streamer ID' },
      { key: 'startAt', label: 'Bắt đầu' },
      { key: 'endAt', label: 'Kết thúc' },
      { key: 'status', label: 'Trạng thái' }
    ],
    fields: [
      { key: 'streamerId', label: 'Streamer', required: true },
      { key: 'title', label: 'Tên phiên livestream', required: true },
      { key: 'plannedStartAt', label: 'Thời gian bắt đầu', type: 'datetime-local', required: true },
      { key: 'plannedEndAt', label: 'Thời gian kết thúc', type: 'datetime-local', required: true }
    ]
  },
  sessions: {
    title: 'Phiên live thực tế',
    eyebrow: 'RECORDED SESSIONS',
    description: 'Ghi nhận thời gian phát sóng thực tế và số liệu phân tích phiên live.',
    path: '/sessions',
    columns: [
      { key: 'streamerName', label: 'Streamer' },
      { key: 'actualStartAt', label: 'Bắt đầu thực tế' },
      { key: 'actualEndAt', label: 'Kết thúc thực tế' },
      { key: 'status', label: 'Trạng thái' },
      { key: 'source', label: 'Nguồn ghi nhận' }
    ]
  },
  teams: {
    title: 'Đội ngũ nghiệp vụ',
    eyebrow: 'ORGANIZATION',
    description: 'Cơ cấu phòng ban agency: Talent, Live Ops, Booking, Finance, HR, Creative.',
    path: '/teams',
    columns: [
      { key: 'code', label: 'Mã đội' },
      { key: 'name', label: 'Tên đội' },
      { key: 'parentTeamId', label: 'Đội cấp trên' }
    ],
    fields: [
      { key: 'code', label: 'Mã đội (ví dụ: MKT)', required: true },
      { key: 'name', label: 'Tên đội', required: true }
    ]
  },
  employees: {
    title: 'Hồ sơ nhân sự (HR)',
    eyebrow: 'HUMAN RESOURCES',
    description: 'Quản lý nhân viên, chức danh công việc và ngày gia nhập agency.',
    path: '/employees',
    columns: [
      { key: 'employeeCode', label: 'Mã nhân viên' },
      { key: 'jobTitle', label: 'Chức danh' },
      { key: 'hireDate', label: 'Ngày vào làm' },
      { key: 'status', label: 'Trạng thái' }
    ],
    fields: [
      { key: 'userId', label: 'Tài khoản nhân viên', required: true },
      { key: 'employeeCode', label: 'Mã nhân viên (ví dụ: EMP-009)', required: true },
      { key: 'jobTitle', label: 'Chức danh', required: true },
      { key: 'hireDate', label: 'Ngày vào làm', type: 'date', required: true }
    ]
  },
  tasks: {
    title: 'Công việc liên phòng ban',
    eyebrow: 'CROSS-FUNCTIONAL WORKFLOWS',
    description: 'Theo dõi tiến độ công việc giữa các đội phụ trách streamer và booking.',
    path: '/tasks',
    columns: [
      { key: 'title', label: 'Tên công việc' },
      { key: 'priority', label: 'Mức ưu tiên' },
      { key: 'status', label: 'Trạng thái' },
      { key: 'dueAt', label: 'Thời hạn' },
      { key: 'ownerUserId', label: 'Người phụ trách ID' }
    ],
    fields: [
      { key: 'teamId', label: 'Đội phụ trách', required: true },
      { key: 'ownerUserId', label: 'Người phụ trách', required: true },
      { key: 'streamerId', label: 'Streamer liên quan' },
      { key: 'bookingId', label: 'Booking liên quan' },
      { key: 'title', label: 'Tiêu đề công việc', required: true },
      {
        key: 'priority',
        label: 'Mức ưu tiên',
        type: 'select',
        required: true,
        options: [
          { label: 'Thấp (Low)', value: 'low' },
          { label: 'Bình thường (Normal)', value: 'normal' },
          { label: 'Cao (High)', value: 'high' },
          { label: 'Khẩn cấp (Urgent)', value: 'urgent' }
        ]
      },
      { key: 'dueAt', label: 'Thời hạn hoàn thành', type: 'datetime-local' }
    ]
  },
  customers: {
    title: 'Khách hàng & Nhãn hàng',
    eyebrow: 'COMMERCIAL DIRECTORY',
    description: 'Danh bạ đối tác thương mại, nhà tài trợ và thương hiệu hợp tác booking.',
    path: '/customers',
    columns: [
      { key: 'name', label: 'Tên tổ chức' },
      { key: 'contactName', label: 'Người liên hệ' },
      { key: 'email', label: 'Email liên hệ' }
    ],
    fields: [
      { key: 'name', label: 'Tên tổ chức/doanh nghiệp', required: true },
      { key: 'contactName', label: 'Tên người liên hệ' },
      { key: 'email', label: 'Email', type: 'email' }
    ]
  },
  bookings: {
    title: 'Booking & Chiến dịch',
    eyebrow: 'COMMERCIAL CAMPAIGNS',
    description: 'Quản lý hợp đồng thương mại, streamer tham gia, nghiệm thu deliverable.',
    path: '/bookings',
    columns: [
      { key: 'title', label: 'Chiến dịch' },
      { key: 'customerId', label: 'Khách hàng ID' },
      { key: 'grossAmount', label: 'Giá trị hợp đồng' },
      { key: 'currency', label: 'Tiền tệ' },
      { key: 'status', label: 'Trạng thái' }
    ],
    fields: [
      { key: 'customerId', label: 'Khách hàng', required: true },
      { key: 'streamerIds', label: 'Streamer tham gia', type: 'multi' },
      { key: 'title', label: 'Tên chiến dịch booking', required: true },
      { key: 'grossAmount', label: 'Giá trị hợp đồng (VND)', type: 'number', required: true },
      { key: 'currency', label: 'Tiền tệ', required: true }
    ]
  },
  transactions: {
    title: 'Giao dịch tài chính',
    eyebrow: 'TRANSACTIONS',
    description: 'Ghi nhận doanh thu quà tặng, tài trợ và chi phí vận hành trước khi đối soát.',
    path: '/finance/transactions',
    columns: [
      { key: 'type', label: 'Loại' },
      { key: 'category', label: 'Danh mục' },
      { key: 'amount', label: 'Số tiền' },
      { key: 'currency', label: 'Tiền tệ' },
      { key: 'occurredAt', label: 'Ngày phát sinh' },
      { key: 'status', label: 'Trạng thái' }
    ],
    fields: [
      {
        key: 'type',
        label: 'Loại giao dịch',
        type: 'select',
        required: true,
        options: [
          { label: 'Doanh thu (Revenue)', value: 'revenue' },
          { label: 'Chi phí (Expense)', value: 'expense' }
        ]
      },
      { key: 'category', label: 'Danh mục (donation, booking, production...)', required: true },
      { key: 'amount', label: 'Số tiền', type: 'number', required: true },
      { key: 'currency', label: 'Mã tiền tệ (VND)', required: true },
      { key: 'occurredAt', label: 'Ngày phát sinh', type: 'datetime-local', required: true },
      { key: 'streamerId', label: 'Streamer liên quan' }
    ]
  },
  'share-rules': {
    title: 'Quy tắc chia doanh thu',
    eyebrow: 'REVENUE SHARE RULES',
    description: 'Tỷ lệ phân chia doanh thu giữa Creator và Agency theo thỏa thuận hợp đồng.',
    path: '/finance/share-rules',
    columns: [
      { key: 'streamerName', label: 'Streamer' },
      { key: 'streamerPercent', label: '% Streamer' },
      { key: 'agencyPercent', label: '% Agency' },
      { key: 'validFrom', label: 'Từ ngày' },
      { key: 'validTo', label: 'Đến ngày' },
      { key: 'status', label: 'Trạng thái' }
    ]
  },
  settlements: {
    title: 'Kỳ đối soát',
    eyebrow: 'SETTLEMENT WORKFLOW',
    description: 'Tạo kỳ đối soát từ các giao dịch đã ghi sổ và quy tắc chia đã duyệt.',
    path: '/finance/settlements',
    columns: [
      { key: 'streamerId', label: 'Streamer ID' },
      { key: 'periodStart', label: 'Từ ngày' },
      { key: 'periodEnd', label: 'Đến ngày' },
      { key: 'grossRevenue', label: 'Doanh thu gộp' },
      { key: 'streamerShare', label: 'Phần streamer' },
      { key: 'status', label: 'Trạng thái' }
    ],
    fields: [
      { key: 'streamerId', label: 'Streamer', required: true },
      { key: 'periodStart', label: 'Từ ngày', type: 'date', required: true },
      { key: 'periodEnd', label: 'Đến ngày', type: 'date', required: true }
    ]
  },
  payouts: {
    title: 'Phiếu chi trả (Payouts)',
    eyebrow: 'PAYOUTS',
    description: 'Lịch sử thanh toán thu nhập đối soát cho Creator qua ngân hàng.',
    path: '/finance/payouts',
    columns: [
      { key: 'streamerName', label: 'Streamer' },
      { key: 'period', label: 'Kỳ đối soát' },
      { key: 'amount', label: 'Số tiền' },
      { key: 'currency', label: 'Tiền tệ' },
      { key: 'status', label: 'Trạng thái' },
      { key: 'paymentReference', label: 'Mã UNC/GD' },
      { key: 'paidAt', label: 'Ngày chi trả' }
    ]
  },
  audit: {
    title: 'Nhật ký kiểm toán (Audit Logs)',
    eyebrow: 'SECURITY & COMPLIANCE',
    description: 'Lịch sử bất biến ghi nhận mọi thao tác phân quyền, tài chính và hệ thống.',
    path: '/audit-logs',
    columns: [
      { key: 'action', label: 'Thao tác' },
      { key: 'entityType', label: 'Đối tượng' },
      { key: 'actorUserId', label: 'Người thực hiện ID' },
      { key: 'occurredAt', label: 'Thời điểm' },
      { key: 'summary', label: 'Chi tiết sự kiện' }
    ]
  },
  applications: {
    title: 'Đơn ứng tuyển Creator',
    eyebrow: 'PUBLIC INTAKE',
    description: 'Hồ sơ đăng ký từ trang công khai chờ Talent Manager xem xét và tuyển dụng.',
    path: '/intake/streamer-applications',
    columns: [
      { key: 'applicantName', label: 'Ứng viên' },
      { key: 'email', label: 'Email' },
      { key: 'platformLinks', label: 'Kênh/Link' },
      { key: 'note', label: 'Ghi chú' },
      { key: 'status', label: 'Trạng thái' },
      { key: 'receivedAt', label: 'Thời gian gửi' }
    ]
  },
  partnerships: {
    title: 'Yêu cầu hợp tác Brand',
    eyebrow: 'PARTNER INTAKE',
    description: 'Yêu cầu booking và tài trợ từ các nhãn hàng gửi qua cổng công khai.',
    path: '/intake/partnership-requests',
    columns: [
      { key: 'organizationName', label: 'Tổ chức/Brand' },
      { key: 'contactName', label: 'Người đại diện' },
      { key: 'email', label: 'Email' },
      { key: 'summary', label: 'Nhu cầu chiến dịch' },
      { key: 'status', label: 'Trạng thái' }
    ]
  }
};

type Props = { page: string };

export default function ModuleWorkspace({ page }: Props) {
  const config = definitions[page] ?? definitions.streamers;
  const [rows, setRows] = useState<Row[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [formOpen, setFormOpen] = useState(false);
  const [form, setForm] = useState<Record<string, string>>({});
  const [lookup, setLookup] = useState<Record<string, { id: string; label: string }[]>>({});

  // Deliverables modal state for bookings
  const [deliverableModalBooking, setDeliverableModalBooking] = useState<{ id: string; title: string } | null>(null);
  const [deliverables, setDeliverables] = useState<Row[]>([]);
  const [newDeliverableDesc, setNewDeliverableDesc] = useState('');

  async function reload() {
    setLoading(true);
    setError('');
    try {
      const result = await apiRequest<Row[]>(config.path);
      setRows(result);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không tải được dữ liệu.');
      setRows([]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (page === 'invitations') return;
    void reload();
    setForm({});
    setFormOpen(false);
    setNotice('');
    setDeliverableModalBooking(null);

    const keys = config.fields?.map(field => field.key) ?? [];
    const lookups: Record<string, string> = {};
    if (keys.some(key => key === 'streamerId' || key === 'streamerIds')) {
      lookups.streamerId = '/directory/streamers';
      lookups.streamerIds = '/directory/streamers';
    }
    if (keys.some(key => key === 'teamId' || key === 'parentTeamId')) {
      lookups.teamId = '/directory/teams';
      lookups.parentTeamId = '/directory/teams';
    }
    if (keys.some(key => key === 'ownerUserId' || key === 'userId')) {
      lookups.ownerUserId = '/directory/users';
      lookups.userId = '/directory/users';
    }
    if (keys.some(key => key === 'customerId')) lookups.customerId = '/customers';
    if (keys.some(key => key === 'bookingId')) lookups.bookingId = '/bookings';

    void Promise.all(
      Object.entries(lookups).map(async ([key, path]) => {
        try {
          const result = await apiRequest<Record<string, unknown>[]>(path);
          setLookup(previous => ({
            ...previous,
            [key]: result.map(item => ({
              id: String(item.id),
              label: String(item.label ?? item.name ?? item.title ?? item.id)
            }))
          }));
        } catch {
          setLookup(previous => ({ ...previous, [key]: [] }));
        }
      })
    );
  }, [config.path, page]);

  const canCreate = Boolean(config.fields?.length);
  const emptyMessage = useMemo(
    () => (loading ? 'Đang tải dữ liệu từ database…' : error ? 'Không thể hiển thị dữ liệu.' : 'Chưa có bản ghi nào trong hệ thống.'),
    [loading, error]
  );

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const values: Row = {};
      for (const field of config.fields ?? []) {
        const raw = form[field.key] ?? '';
        if (!raw) continue;
        if (field.type === 'number') values[field.key] = Number(raw);
        else if (field.type === 'datetime-local') values[field.key] = new Date(raw).toISOString();
        else if (field.type === 'date') values[field.key] = raw;
        else if (field.type === 'multi') values[field.key] = raw.split(',').filter(Boolean);
        else values[field.key] = raw;
      }
      await apiRequest(config.path, { method: 'POST', body: JSON.stringify(values) });
      setNotice('Đã thêm dữ liệu thành công vào database.');
      setForm({});
      setFormOpen(false);
      await reload();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không lưu được dữ liệu.');
    } finally {
      setBusy(false);
    }
  }

  async function act(row: Row, action: string, status?: string) {
    setError('');
    setNotice('');
    try {
      if (page === 'transactions') {
        await apiRequest(`/finance/transactions/${row.id}/${action}`, { method: 'POST' });
      } else if (page === 'settlements') {
        await apiRequest(`/finance/settlements/${row.id}/${action}`, { method: 'POST' });
      } else if (page === 'tasks') {
        await apiRequest(`/tasks/${row.id}/status`, { method: 'POST', body: JSON.stringify({ status: status ?? 'done' }) });
      } else if (page === 'applications') {
        await apiRequest(`/intake/streamer-applications/${row.id}/status`, { method: 'POST', body: JSON.stringify({ status: status ?? 'reviewing' }) });
      } else if (page === 'partnerships') {
        await apiRequest(`/intake/partnership-requests/${row.id}/status`, { method: 'POST', body: JSON.stringify({ status: status ?? 'reviewing' }) });
      } else if (page === 'bookings') {
        await apiRequest(`/bookings/${row.id}/${action}`, { method: 'POST' });
      } else if (page === 'streamers') {
        if (action === 'onboarding') {
          await apiRequest(`/streamers/${row.id}/onboarding`, { method: 'POST' });
        } else if (action === 'activate') {
          await apiRequest(`/streamers/${row.id}/activate`, { method: 'POST' });
        }
      } else if (page === 'payouts') {
        const ref = window.prompt('Nhập mã giao dịch chuyển khoản ngân hàng (UNC):', 'VCB-TRANS-' + Date.now().toString().slice(-6));
        if (!ref) return;
        await apiRequest(`/finance/payouts/${row.id}/mark-paid`, { method: 'POST', body: JSON.stringify({ paymentReference: ref }) });
      }
      setNotice('Đã cập nhật trạng thái nghiệp vụ thành công.');
      await reload();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không cập nhật được.');
    }
  }

  async function openDeliverablesModal(booking: Row) {
    setDeliverableModalBooking({ id: String(booking.id), title: String(booking.title) });
    try {
      const list = await apiRequest<Row[]>(`/bookings/${booking.id}/deliverables`);
      setDeliverables(list);
    } catch {
      setDeliverables([]);
    }
  }

  async function acceptDeliverable(deliverableId: string) {
    if (!deliverableModalBooking) return;
    try {
      await apiRequest(`/bookings/${deliverableModalBooking.id}/deliverables/${deliverableId}/accept`, { method: 'POST' });
      const updated = await apiRequest<Row[]>(`/bookings/${deliverableModalBooking.id}/deliverables`);
      setDeliverables(updated);
      setNotice('Đã nghiệm thu deliverable thành công.');
      await reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lỗi nghiệm thu');
    }
  }

  async function addDeliverable(e: FormEvent) {
    e.preventDefault();
    if (!deliverableModalBooking || !newDeliverableDesc.trim()) return;
    try {
      await apiRequest(`/bookings/${deliverableModalBooking.id}/deliverables`, {
        method: 'POST',
        body: JSON.stringify({ description: newDeliverableDesc.trim() })
      });
      setNewDeliverableDesc('');
      const updated = await apiRequest<Row[]>(`/bookings/${deliverableModalBooking.id}/deliverables`);
      setDeliverables(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lỗi thêm deliverable');
    }
  }

  if (page === 'invitations') return <InvitationWorkspace />;

  return (
    <section className="workspace-module">
      <div className="module-heading">
        <div>
          <div className="eyebrow">{config.eyebrow}</div>
          <h1>{config.title}</h1>
          <p>{config.description}</p>
        </div>
        <div className="module-actions">
          <button className="secondary-button" onClick={() => void reload()}>↻ Làm mới</button>
          {canCreate && (
            <button className="primary-button" onClick={() => setFormOpen(value => !value)}>
              <span>＋</span> Tạo mới
            </button>
          )}
        </div>
      </div>

      {notice && <div className="notice success" role="status">{notice}</div>}
      {error && <div className="notice error" role="alert">{error}</div>}

      {formOpen && (
        <form className="module-form" onSubmit={submit}>
          <div className="form-grid">
            {config.fields?.map(field => (
              <label key={field.key}>
                {field.label}
                {field.type === 'select' ? (
                  <select
                    required={field.required}
                    value={form[field.key] ?? ''}
                    onChange={event => setForm({ ...form, [field.key]: event.target.value })}
                  >
                    <option value="">Chọn</option>
                    {field.options?.map(option => (
                      <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                ) : lookup[field.key] ? (
                  <select
                    required={field.required}
                    multiple={field.type === 'multi'}
                    value={field.type === 'multi' ? (form[field.key] ?? '').split(',').filter(Boolean) : form[field.key] ?? ''}
                    onChange={event =>
                      setForm({
                        ...form,
                        [field.key]: field.type === 'multi'
                          ? Array.from(event.target.selectedOptions).map(option => option.value).join(',')
                          : event.target.value
                      })
                    }
                  >
                    <option value="">Chọn {field.label.toLowerCase()}</option>
                    {lookup[field.key].map(option => (
                      <option key={option.id} value={option.id}>{option.label}</option>
                    ))}
                  </select>
                ) : (
                  <input
                    required={field.required}
                    type={field.type === 'number' || field.type === 'date' || field.type === 'datetime-local' || field.type === 'email' ? field.type : 'text'}
                    step={field.type === 'number' ? '0.01' : undefined}
                    value={form[field.key] ?? ''}
                    onChange={event => setForm({ ...form, [field.key]: event.target.value })}
                  />
                )}
              </label>
            ))}
          </div>
          <div className="form-actions">
            <button type="button" className="secondary-button" onClick={() => setFormOpen(false)}>Hủy</button>
            <button className="primary-button" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu'}</button>
          </div>
        </form>
      )}

      <div className="module-table-wrap">
        <table className="module-table">
          <thead>
            <tr>
              {config.columns.map(column => (
                <th key={column.key}>{column.label}</th>
              ))}
              {['transactions', 'settlements', 'tasks', 'applications', 'partnerships', 'bookings', 'streamers', 'payouts'].includes(page) && (
                <th>Tác vụ nghiệp vụ</th>
              )}
            </tr>
          </thead>
          <tbody>
            {rows.map((row, index) => (
              <tr key={String(row.id ?? index)}>
                {config.columns.map(column => (
                  <td key={column.key}>{formatValue(row[column.key], column.key)}</td>
                ))}
                {page === 'transactions' && (
                  <td>
                    {row.status === 'draft' ? (
                      <button className="row-action" onClick={() => void act(row, 'post')}>Ghi sổ (Post)</button>
                    ) : (
                      <span className="status-pill green">Đã ghi sổ</span>
                    )}
                  </td>
                )}
                {page === 'settlements' && (
                  <td>
                    {row.status === 'draft' && (
                      <button className="row-action" onClick={() => void act(row, 'submit')}>Gửi duyệt</button>
                    )}
                    {row.status === 'submitted' && (
                      <button className="row-action" onClick={() => void act(row, 'approve')}>Duyệt & Khóa</button>
                    )}
                    {row.status === 'locked' && (
                      <span className="status-pill green">Đã khóa kỳ</span>
                    )}
                  </td>
                )}
                {page === 'payouts' && (
                  <td>
                    {row.status === 'pending' ? (
                      <button className="row-action" onClick={() => void act(row, 'mark-paid')}>Xác nhận chi trả</button>
                    ) : (
                      <span className="status-pill green">Đã chi trả</span>
                    )}
                  </td>
                )}
                {page === 'tasks' && (
                  <td>
                    {row.status !== 'done' && row.status !== 'cancelled' ? (
                      <button className="row-action" onClick={() => void act(row, '', 'done')}>Hoàn tất</button>
                    ) : (
                      <span className="status-pill green">Đã xong</span>
                    )}
                  </td>
                )}
                {(page === 'applications' || page === 'partnerships') && (
                  <td>
                    {row.status === 'received' && (
                      <button className="row-action" onClick={() => void act(row, '', 'reviewing')}>Nhận xử lý</button>
                    )}
                    {row.status === 'reviewing' && (
                      <button className="row-action" onClick={() => void act(row, '', 'shortlisted')}>Đưa vào vòng phỏng vấn</button>
                    )}
                  </td>
                )}
                {page === 'streamers' && (
                  <td>
                    {row.status === 'lead' && (
                      <button className="row-action" onClick={() => void act(row, 'onboarding')}>Bắt đầu Onboarding</button>
                    )}
                    {row.status === 'onboarding' && (
                      <button className="row-action" onClick={() => void act(row, 'activate')}>Kích hoạt</button>
                    )}
                    {row.status === 'active' && (
                      <span className="status-pill green">Đang hoạt động</span>
                    )}
                  </td>
                )}
                {page === 'bookings' && (
                  <td style={{ display: 'flex', gap: '6px' }}>
                    {row.status === 'proposed' && (
                      <button className="row-action" onClick={() => void act(row, 'confirm')}>Xác nhận</button>
                    )}
                    {row.status === 'confirmed' && (
                      <button className="row-action" onClick={() => void act(row, 'start')}>Bắt đầu</button>
                    )}
                    <button className="row-action secondary" onClick={() => void openDeliverablesModal(row)}>
                      Deliverables
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        {rows.length === 0 && <div className="module-empty">{emptyMessage}</div>}
      </div>

      <div className="module-foot">
        <span>{rows.length} bản ghi thực tế được tải từ cơ sở dữ liệu PostgreSQL</span>
        <span>Phân quyền và cô lập dữ liệu theo Agency ID server-side</span>
      </div>

      {deliverableModalBooking && (
        <div className="modal-backdrop" onClick={() => setDeliverableModalBooking(null)}>
          <div className="modal-card" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <div>
                <div className="eyebrow">BOOKING DELIVERABLES</div>
                <h2>{deliverableModalBooking.title}</h2>
              </div>
              <button className="close-btn" onClick={() => setDeliverableModalBooking(null)}>✕</button>
            </div>
            <div className="modal-body">
              <table className="module-table" style={{ marginBottom: '16px' }}>
                <thead>
                  <tr>
                    <th>Hạng mục bàn giao</th>
                    <th>Thời hạn</th>
                    <th>Trạng thái</th>
                    <th>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {deliverables.length === 0 ? (
                    <tr>
                      <td colSpan={4} style={{ textAlign: 'center', padding: '16px' }}>Chưa có deliverable.</td>
                    </tr>
                  ) : (
                    deliverables.map(d => (
                      <tr key={String(d.id)}>
                        <td><b>{String(d.description)}</b></td>
                        <td>{formatValue(d.dueAt)}</td>
                        <td>
                          <span className={`status-pill ${d.status === 'accepted' ? 'green' : 'orange'}`}>
                            {d.status === 'accepted' ? 'Đã nghiệm thu' : 'Chờ nghiệm thu'}
                          </span>
                        </td>
                        <td>
                          {d.status === 'pending' && (
                            <button className="row-action" onClick={() => acceptDeliverable(String(d.id))}>
                              Nghiệm thu
                            </button>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>

              <form onSubmit={addDeliverable} style={{ display: 'flex', gap: '8px' }}>
                <input
                  type="text"
                  placeholder="Thêm hạng mục bàn giao mới..."
                  required
                  value={newDeliverableDesc}
                  onChange={e => setNewDeliverableDesc(e.target.value)}
                  style={{ flex: 1 }}
                />
                <button className="primary-button" type="submit">Thêm hạng mục</button>
              </form>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}

function InvitationWorkspace() {
  const [email, setEmail] = useState('');
  const [roleCode, setRoleCode] = useState('HRStaff');
  const [streamerId, setStreamerId] = useState('');
  const [streamers, setStreamers] = useState<{ id: string; label: string }[]>([]);
  const [token, setToken] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    void apiRequest<{ id: string; label: string }[]>('/directory/streamers')
      .then(setStreamers)
      .catch(() => setStreamers([]));
  }, []);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    setToken('');
    try {
      const result = await apiRequest<{ activationToken: string; expiresAt: string }>('/auth/invitations', {
        method: 'POST',
        body: JSON.stringify({ email, roleCode, streamerId: roleCode === 'Streamer' ? streamerId : null })
      });
      setToken(result.activationToken);
      setEmail('');
      setStreamerId('');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Không tạo được lời mời.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="workspace-module">
      <div className="module-heading">
        <div>
          <div className="eyebrow">ACCESS CONTROL</div>
          <h1>Mời tài khoản nội bộ</h1>
          <p>Admin mời nhân viên hoặc streamer. Lời mời không cấp quyền Admin và chỉ dùng được một lần qua liên kết an toàn.</p>
        </div>
      </div>
      {error && <div className="notice error" role="alert">{error}</div>}
      <form className="module-form" onSubmit={submit}>
        <div className="form-grid">
          <label>
            Email nhân viên
            <input type="email" required maxLength={254} value={email} onChange={event => setEmail(event.target.value)} />
          </label>
          <label>
            Vai trò cấp quyền
            <select value={roleCode} onChange={event => { setRoleCode(event.target.value); setToken(''); }}>
              <option value="HRManager">HR Manager</option>
              <option value="HRStaff">HR Staff</option>
              <option value="TalentManager">Talent Manager</option>
              <option value="OperationsManager">Operations Manager</option>
              <option value="Booking">Booking</option>
              <option value="Marketing">Marketing</option>
              <option value="Finance">Finance</option>
              <option value="FinanceApprover">Finance Approver</option>
              <option value="Analyst">Analyst</option>
              <option value="Streamer">Streamer</option>
            </select>
          </label>
          {roleCode === 'Streamer' && (
            <label>
              Hồ sơ streamer
              <select required value={streamerId} onChange={event => setStreamerId(event.target.value)}>
                <option value="">Chọn hồ sơ</option>
                {streamers.map(item => (
                  <option key={item.id} value={item.id}>{item.label}</option>
                ))}
              </select>
            </label>
          )}
        </div>
        <div className="form-actions">
          <button className="primary-button" disabled={busy}>{busy ? 'Đang tạo…' : 'Tạo lời mời'}</button>
        </div>
      </form>
      {token && (
        <div className="notice success" role="status">
          <b>Lời mời đã được tạo trong cơ sở dữ liệu.</b>
          <p>Token chỉ hiển thị trong phiên này. Gửi riêng cho đúng người nhận qua kênh đã xác minh; hết hạn sau 24 giờ.</p>
          <code className="invite-token">{token}</code>
          <button className="secondary-button" onClick={() => { void navigator.clipboard?.writeText(token); }}>
            Sao chép token
          </button>
        </div>
      )}
    </section>
  );
}

function formatValue(value: unknown, key?: string) {
  if (value === null || value === undefined || value === '') return '—';
  if (typeof value === 'number') {
    if (key?.toLowerCase().includes('amount') || key?.toLowerCase().includes('revenue') || key?.toLowerCase().includes('share')) {
      return new Intl.NumberFormat('vi-VN').format(value) + ' ₫';
    }
    return new Intl.NumberFormat('vi-VN').format(value);
  }
  if (typeof value === 'string' && /^\d{4}-\d\d-\d\dT/.test(value)) {
    return new Date(value).toLocaleString('vi-VN');
  }
  if (typeof value === 'string' && value.startsWith('http')) {
    return <a href={value} target="_blank" rel="noreferrer" className="text-link">{value}</a>;
  }
  return String(value);
}
