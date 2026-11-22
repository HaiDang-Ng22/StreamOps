import { useEffect, useState, useCallback, useMemo } from 'react';
import { apiRequest, setAccessToken } from '../api/client';
import LoginView from './LoginView';
import ModuleWorkspace from './ModuleWorkspace';
import ReportsView from './ReportsView';

export type Session = { displayName: string; roles: string[]; email?: string; agencyName?: string };

type DashboardSummary = {
  activeStreamers: number;
  totalStreamers: number;
  sessionsThisWeek: number;
  sessionsToday: number;
  activeBookings: number;
  pendingDeliverables: number;
  openTasks: number;
  pendingSettlements: number;
  pendingApplications: number;
  pendingPartnerships: number;
  totalRevenue: number;
  currency: string;
};

type ScheduleItem = {
  id: string;
  time: string;
  name: string;
  platform: string;
  title: string;
  team: string;
  state: string;
  tone: string;
};

type TopStreamer = {
  id: string;
  rank: string;
  name: string;
  category: string;
  views: string;
  growth: string;
};

type RecentActivity = {
  id: string;
  action: string;
  summary: string;
  actorName: string;
  timeAgo: string;
};

type MyTask = {
  id: string;
  title: string;
  priority: string;
  status: string;
  teamName: string;
  ownerName: string;
  dueDisplay: string;
};

type RoleConfig = {
  key: string;
  roleName: string;
  badgeClass: string;
  eyebrow: string;
  title: string;
  description: string;
  primaryAction: { label: string; page: string };
  secondaryAction?: { label: string; page: string };
  allowedPages: string[];
  teamPages: string[];
};

const ROLE_CONFIGS: Record<string, RoleConfig> = {
  All: {
    key: 'All',
    roleName: 'Toàn quyền Admin',
    badgeClass: 'admin',
    eyebrow: 'TOÀN QUYỀN HỆ THỐNG · ADMIN CONTROL CENTER',
    title: 'Bảng điều khiển Tổng giám đốc & Admin',
    description: 'Giám sát toàn diện hoạt động vận hành, hợp đồng creator, booking nhãn hàng và tài chính.',
    primaryAction: { label: '＋ Thêm Streamer', page: 'streamers' },
    secondaryAction: { label: '＋ Tạo Booking', page: 'bookings' },
    allowedPages: ['overview', 'streamers', 'channels', 'schedules', 'sessions', 'tasks', 'bookings', 'transactions', 'reports', 'audit'],
    teamPages: ['teams', 'employees', 'customers', 'applications', 'partnerships']
  },
  TalentManager: {
    key: 'TalentManager',
    roleName: 'Quản lý Talent & Creator',
    badgeClass: 'talent',
    eyebrow: 'PHÒNG QUẢN LÝ CREATOR · TALENT MANAGEMENT',
    title: 'Không gian làm việc Quản lý Streamer',
    description: 'Quản lý hồ sơ creator, kênh livestream, hợp đồng độc quyền và xét duyệt đơn ứng tuyển.',
    primaryAction: { label: '＋ Thêm Streamer', page: 'streamers' },
    secondaryAction: { label: '📋 Duyệt đơn Creator', page: 'applications' },
    allowedPages: ['overview', 'streamers', 'channels', 'share-rules', 'tasks'],
    teamPages: ['applications', 'customers']
  },
  OperationsManager: {
    key: 'OperationsManager',
    roleName: 'Vận hành Live (Live Ops)',
    badgeClass: 'operations',
    eyebrow: 'PHÒNG VẬN HÀNH PHÁT SÓNG · LIVE OPERATIONS',
    title: 'Không gian làm việc Điều phối Livestream',
    description: 'Xếp lịch phát sóng, giám sát phiên live thời gian thực, đo lường CCU và xử lý sự cố kỹ thuật.',
    primaryAction: { label: '＋ Lập lịch Live', page: 'schedules' },
    secondaryAction: { label: '▶ Giám sát Phiên live', page: 'sessions' },
    allowedPages: ['overview', 'schedules', 'sessions', 'channels', 'streamers', 'tasks'],
    teamPages: []
  },
  Booking: {
    key: 'Booking',
    roleName: 'Chuyên viên Booking & Brand',
    badgeClass: 'booking',
    eyebrow: 'PHÒNG KINH DOANH & NHÃN HÀNG · BRAND PARTNERSHIPS',
    title: 'Không gian làm việc Booking & Chiến dịch',
    description: 'Quản lý quan hệ đối tác tài trợ, tiếp nhận booking, phân bổ streamer và nghiệm thu deliverables.',
    primaryAction: { label: '＋ Tạo Booking mới', page: 'bookings' },
    secondaryAction: { label: '🏢 Khách hàng & Brand', page: 'customers' },
    allowedPages: ['overview', 'bookings', 'streamers', 'tasks'],
    teamPages: ['customers', 'partnerships']
  },
  Finance: {
    key: 'Finance',
    roleName: 'Kế toán & Tài chính',
    badgeClass: 'finance',
    eyebrow: 'PHÒNG TÀI CHÍNH KẾ TOÁN · FINANCE & SETTLEMENT',
    title: 'Không gian làm việc Kế toán & Đối soát',
    description: 'Ghi nhận doanh thu chi phí, áp dụng quy tắc chia tỷ lệ, lập kỳ đối soát hoa hồng cho creator.',
    primaryAction: { label: '＋ Ghi nhận giao dịch', page: 'transactions' },
    secondaryAction: { label: '⚖ Lập kỳ đối soát', page: 'settlements' },
    allowedPages: ['overview', 'transactions', 'settlements', 'payouts', 'share-rules', 'reports', 'tasks'],
    teamPages: []
  },
  FinanceApprover: {
    key: 'FinanceApprover',
    roleName: 'Lãnh đạo Phê duyệt Chi',
    badgeClass: 'finance',
    eyebrow: 'BAN KIỂM SOÁT TÀI CHÍNH · FINANCE APPROVAL',
    title: 'Không gian Phê duyệt & Ký lệnh Chi',
    description: 'Thẩm định kỳ đối soát hoa hồng streamer, phê duyệt giải ngân và ký ủy nhiệm chi ngân hàng.',
    primaryAction: { label: '✓ Phê duyệt đối soát', page: 'settlements' },
    secondaryAction: { label: '🧾 Duyệt Payout', page: 'payouts' },
    allowedPages: ['overview', 'settlements', 'payouts', 'transactions', 'reports', 'audit'],
    teamPages: []
  },
  Analyst: {
    key: 'Analyst',
    roleName: 'Phân tích Dữ liệu (Analyst)',
    badgeClass: 'analyst',
    eyebrow: 'PHÒNG DỮ LIỆU & PHÂN TÍCH · DATA ANALYTICS',
    title: 'Không gian Phân tích Hiệu suất & Doanh thu',
    description: 'Đánh giá chỉ số tăng trưởng streamer, xu hướng giờ live, cơ cấu nguồn thu và tỷ lệ hoàn thành KPI.',
    primaryAction: { label: '📊 Xem Báo cáo đầy đủ', page: 'reports' },
    secondaryAction: { label: '▶ Xem số liệu Phiên live', page: 'sessions' },
    allowedPages: ['overview', 'reports', 'sessions', 'streamers', 'channels'],
    teamPages: []
  },
  HRManager: {
    key: 'HRManager',
    roleName: 'Quản lý Nhân sự (HR)',
    badgeClass: 'hr',
    eyebrow: 'PHÒNG HÀNH CHÍNH NHÂN SỰ · HUMAN RESOURCES',
    title: 'Không gian Quản lý Nhân sự & Tổ chức',
    description: 'Quản lý cấu trúc phòng ban agency, hồ sơ nhân sự, phân công đội ngũ và nhật ký hoạt động.',
    primaryAction: { label: '＋ Thêm Nhân viên', page: 'employees' },
    secondaryAction: { label: '🏢 Cơ cấu đội ngũ', page: 'teams' },
    allowedPages: ['overview', 'teams', 'employees', 'tasks', 'audit'],
    teamPages: ['teams', 'employees']
  }
};

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [page, setPage] = useState('overview');

  useEffect(() => {
    const expire = () => { setSession(null); setPage('overview'); };
    window.addEventListener('streamops:unauthorized', expire);
    return () => window.removeEventListener('streamops:unauthorized', expire);
  }, []);

  if (!session) return <LoginView onSignedIn={setSession} />;
  return (
    <DashboardShell
      session={session}
      page={page}
      navigate={setPage}
      onSignOut={() => {
        setAccessToken(null);
        setSession(null);
        setPage('overview');
      }}
    />
  );
}

function DashboardShell({
  session,
  page,
  navigate,
  onSignOut,
}: {
  session: Session;
  page: string;
  navigate: (page: string) => void;
  onSignOut: () => void;
}) {
  const [agencyName, setAgencyName] = useState<string>(session.agencyName ?? 'Nova Creator House');
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [schedules, setSchedules] = useState<ScheduleItem[]>([]);
  const [topStreamers, setTopStreamers] = useState<TopStreamer[]>([]);
  const [activities, setActivities] = useState<RecentActivity[]>([]);
  const [tasks, setTasks] = useState<MyTask[]>([]);
  const [loading, setLoading] = useState(false);
  const [completingTaskId, setCompletingTaskId] = useState<string | null>(null);

  // Determine initial role view
  const userPrimaryRole = useMemo(() => {
    if (session.roles.includes('Admin')) return 'All';
    return session.roles[0] || 'TalentManager';
  }, [session.roles]);

  const [activeRoleView, setActiveRoleView] = useState<string>(userPrimaryRole);
  const currentRole = ROLE_CONFIGS[activeRoleView] ?? ROLE_CONFIGS.All;

  const loadDashboardData = useCallback(async () => {
    setLoading(true);
    try {
      const [meRes, sumRes, schedRes, topRes, actRes, taskRes] = await Promise.allSettled([
        apiRequest<{ agencyName?: string }>('/directory/me'),
        apiRequest<DashboardSummary>('/dashboard/summary'),
        apiRequest<ScheduleItem[]>('/dashboard/schedules-today'),
        apiRequest<TopStreamer[]>('/dashboard/top-streamers'),
        apiRequest<RecentActivity[]>('/dashboard/recent-activities'),
        apiRequest<MyTask[]>('/dashboard/my-tasks')
      ]);

      if (meRes.status === 'fulfilled' && meRes.value.agencyName) {
        setAgencyName(meRes.value.agencyName);
      }
      if (sumRes.status === 'fulfilled') setSummary(sumRes.value);
      if (schedRes.status === 'fulfilled') setSchedules(schedRes.value);
      if (topRes.status === 'fulfilled') setTopStreamers(topRes.value);
      if (actRes.status === 'fulfilled') setActivities(actRes.value);
      if (taskRes.status === 'fulfilled') setTasks(taskRes.value);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadDashboardData();
  }, [loadDashboardData]);

  const handleCompleteTask = async (taskId: string) => {
    setCompletingTaskId(taskId);
    try {
      await apiRequest(`/tasks/${taskId}/status`, {
        method: 'POST',
        body: JSON.stringify({ status: 'done' })
      });
      setTasks(prev => prev.filter(t => t.id !== taskId));
      if (summary) setSummary({ ...summary, openTasks: Math.max(0, summary.openTasks - 1) });
    } catch {
      // ignore
    } finally {
      setCompletingTaskId(null);
    }
  };

  const formattedRevenue = summary?.totalRevenue != null
    ? new Intl.NumberFormat('vi-VN').format(summary.totalRevenue) + ' ₫'
    : '0 ₫';

  // Navigation items mapping
  const navItems = [
    { id: 'overview', icon: '▦', label: 'Tổng quan' },
    { id: 'streamers', icon: '◉', label: 'Streamer & Creator', badge: summary ? summary.activeStreamers : undefined },
    { id: 'channels', icon: '☵', label: 'Kênh phát sóng' },
    { id: 'schedules', icon: '▣', label: 'Lịch livestream' },
    { id: 'sessions', icon: '▶', label: 'Phiên live thực tế' },
    { id: 'tasks', icon: '✓', label: 'Công việc', badge: summary && summary.openTasks > 0 ? summary.openTasks : undefined },
    { id: 'bookings', icon: '◇', label: 'Booking & Chiến dịch' },
    { id: 'transactions', icon: '₫', label: 'Tài chính & Thu chi' },
    { id: 'share-rules', icon: '⚖', label: 'Quy tắc chia DT' },
    { id: 'reports', icon: '📊', label: 'Báo cáo doanh thu & KPI' },
    { id: 'audit', icon: '▤', label: 'Nhật ký kiểm toán' },
  ];

  const teamNavItems = [
    { id: 'teams', dot: 'purple', label: 'Cơ cấu đội ngũ' },
    { id: 'employees', dot: 'green', label: 'Hồ sơ nhân sự (HR)' },
    { id: 'customers', dot: 'orange', label: 'Khách hàng & Nhãn hàng' },
    { id: 'applications', dot: 'blue', label: 'Đơn ứng tuyển Creator' },
    { id: 'partnerships', dot: 'purple', label: 'Yêu cầu hợp tác Brand' },
  ];

  // Filter based on active role
  const visibleNavItems = navItems.filter(item => currentRole.allowedPages.includes(item.id));
  const visibleTeamItems = teamNavItems.filter(item => currentRole.teamPages.includes(item.id));

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="#home" aria-label="StreamOps trang chủ">
          <span className="brand-mark">S</span>
          <span>stream<span className="brand-light">ops</span></span>
        </a>

        <div className="workspace">
          <span className="workspace-avatar">{agencyName.slice(0, 1).toUpperCase()}</span>
          <span>
            <b>{agencyName}</b>
            <small>Cổng điều hành agency</small>
          </span>
          <span className="chevron">⌄</span>
        </div>

        {/* Role Switcher for Admin */}
        {session.roles.includes('Admin') && (
          <div className="role-switcher">
            <div className="role-switcher-title">
              <span>GÓC NHÌN VAI TRÒ (ROLE)</span>
              <span style={{ fontSize: '10px' }}>⚡</span>
            </div>
            <select
              value={activeRoleView}
              onChange={e => {
                setActiveRoleView(e.target.value);
                navigate('overview');
              }}
              title="Chuyển đổi góc nhìn vai trò nghiệp vụ"
            >
              <option value="All">👑 Toàn quyền Admin (Tất cả)</option>
              <option value="TalentManager">🎯 Talent Manager (Quản lý Streamer)</option>
              <option value="OperationsManager">⚡ Live Operations (Vận hành Live)</option>
              <option value="Booking">💼 Booking & Nhãn hàng</option>
              <option value="Finance">💰 Kế toán & Tài chính</option>
              <option value="FinanceApprover">🛡️ Lãnh đạo Phê duyệt Chi</option>
              <option value="Analyst">📈 Phân tích Dữ liệu (Analyst)</option>
              <option value="HRManager">👥 Quản lý Nhân sự (HR)</option>
            </select>
          </div>
        )}

        <div className="nav-caption">KHÔNG GIAN LÀM VIỆC</div>
        <nav className="nav-list" aria-label="Điều hướng chính">
          {visibleNavItems.map(item => (
            <a
              key={item.id}
              className={`nav-item ${page === item.id ? 'active' : ''}`}
              href={`#${item.id}`}
              onClick={e => { e.preventDefault(); navigate(item.id); }}
            >
              <span>{item.icon}</span>{item.label}
              {item.badge != null && <em className="badge">{item.badge}</em>}
            </a>
          ))}
        </nav>

        {visibleTeamItems.length > 0 && (
          <>
            <div className="nav-caption teams-caption">TỔ CHỨC & ĐỐI TÁC</div>
            <nav className="nav-list" aria-label="Đội nghiệp vụ">
              {visibleTeamItems.map(item => (
                <a
                  key={item.id}
                  className={`nav-item team-link ${page === item.id ? 'active' : ''}`}
                  href={`#${item.id}`}
                  onClick={e => { e.preventDefault(); navigate(item.id); }}
                >
                  <i className={`team-dot ${item.dot}`} />{item.label}
                </a>
              ))}
            </nav>
          </>
        )}

        <div className="sidebar-bottom">
          {currentRole.allowedPages.includes('settlements') && (
            <a
              className={`nav-item ${page === 'settlements' ? 'active' : ''}`}
              href="#settlements"
              onClick={e => { e.preventDefault(); navigate('settlements'); }}
            >
              <span>₫</span>Kỳ đối soát hoa hồng
            </a>
          )}
          {currentRole.allowedPages.includes('payouts') && (
            <a
              className={`nav-item ${page === 'payouts' ? 'active' : ''}`}
              href="#payouts"
              onClick={e => { e.preventDefault(); navigate('payouts'); }}
            >
              <span>🧾</span>Phiếu chi trả (Payout)
            </a>
          )}
          {session.roles.includes('Admin') && (
            <a
              className={`nav-item ${page === 'invitations' ? 'active' : ''}`}
              href="#invitations"
              onClick={e => { e.preventDefault(); navigate('invitations'); }}
            >
              <span>＋</span>Mời tài khoản
            </a>
          )}
          <div className="profile">
            <div className="profile-avatar">{session.displayName.slice(0, 2).toUpperCase()}</div>
            <span>
              <b>{session.displayName}</b>
              <small>{session.roles.join(', ')}</small>
            </span>
            <button aria-label="Đăng xuất" onClick={onSignOut} title="Đăng xuất">↪</button>
          </div>
        </div>
      </aside>

      <main className="main-content" id="overview">
        <header className="topbar">
          <div className="breadcrumbs">
            <span>{agencyName}</span>
            <span>/</span>
            <span style={{ color: '#8892a4' }}>{currentRole.roleName}</span>
            <span>/</span>
            <b>{page === 'overview' ? 'Bảng điều khiển' : page}</b>
          </div>
          <div className="top-actions">
            <button className="icon-button" onClick={() => void loadDashboardData()} title="Làm mới dữ liệu thật" aria-label="Làm mới">
              ↻
            </button>
            <div className="top-divider" />
            <span className="today">
              {new Intl.DateTimeFormat('vi-VN', {
                weekday: 'long',
                day: 'numeric',
                month: 'long',
                year: 'numeric'
              }).format(new Date())}
            </span>
          </div>
        </header>

        {page === 'overview' ? (
          <>
            <section className="welcome-row">
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '6px' }}>
                  <span className={`role-badge ${currentRole.badgeClass}`}>
                    {currentRole.roleName.toUpperCase()}
                  </span>
                  <span className="eyebrow">{currentRole.eyebrow}</span>
                </div>
                <h1>{currentRole.title} <span>✦</span></h1>
                <p>{currentRole.description}</p>
              </div>
              <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                <button className="secondary-button" onClick={() => void loadDashboardData()} disabled={loading}>
                  {loading ? 'Đang tải…' : '↻ Đồng bộ'}
                </button>
                {currentRole.secondaryAction && (
                  <button className="secondary-button" onClick={() => navigate(currentRole.secondaryAction!.page)}>
                    {currentRole.secondaryAction.label}
                  </button>
                )}
                <button className="primary-button" onClick={() => navigate(currentRole.primaryAction.page)}>
                  {currentRole.primaryAction.label}
                </button>
              </div>
            </section>

            {/* Role-Specific Metric Cards */}
            <section className="stat-grid" aria-label="Chỉ số tổng quan">
              {/* Card 1 */}
              <article className="stat-card">
                <div className="stat-icon violet">
                  {currentRole.key === 'Booking' ? '◇' : currentRole.key === 'OperationsManager' ? '▣' : '◉'}
                </div>
                <div className="stat-label">
                  {currentRole.key === 'Booking'
                    ? 'Chiến dịch Booking đang chạy'
                    : currentRole.key === 'OperationsManager'
                    ? 'Ca livestream hôm nay'
                    : currentRole.key === 'Finance' || currentRole.key === 'FinanceApprover'
                    ? 'Tổng doanh thu đã ghi nhận'
                    : 'Streamer đang hợp tác'}
                </div>
                <div className="stat-value">
                  {currentRole.key === 'Booking'
                    ? (summary ? summary.activeBookings : '…')
                    : currentRole.key === 'OperationsManager'
                    ? (summary ? summary.sessionsToday : '…')
                    : currentRole.key === 'Finance' || currentRole.key === 'FinanceApprover'
                    ? formattedRevenue
                    : (summary ? summary.activeStreamers : '…')}
                </div>
                <div className="stat-note violet">
                  {currentRole.key === 'Booking'
                    ? '3 Nhãn hàng đối tác'
                    : currentRole.key === 'OperationsManager'
                    ? 'Đã xếp lịch & sẵn sàng phát sóng'
                    : currentRole.key === 'Finance' || currentRole.key === 'FinanceApprover'
                    ? 'Từ nguồn booking và nền tảng'
                    : `Tổng ${summary ? summary.totalStreamers : '…'} streamer trực thuộc`}
                </div>
                <div className="sparkline violet">
                  <svg viewBox="0 0 120 38" role="img" aria-label="Biểu đồ hoạt động">
                    <path d="M2 30 C14 28 15 18 27 22 S42 29 52 17 S68 21 77 14 S94 19 103 8 S113 13 119 4" />
                  </svg>
                </div>
              </article>

              {/* Card 2 */}
              <article className="stat-card">
                <div className="stat-icon blue">◷</div>
                <div className="stat-label">
                  {currentRole.key === 'Booking'
                    ? 'Hạng mục cần nghiệm thu'
                    : currentRole.key === 'TalentManager'
                    ? 'Đơn ứng tuyển Creator'
                    : currentRole.key === 'Finance' || currentRole.key === 'FinanceApprover'
                    ? 'Kỳ đối soát hoa hồng'
                    : 'Phiên live tuần này'}
                </div>
                <div className="stat-value">
                  {currentRole.key === 'Booking'
                    ? (summary ? summary.pendingDeliverables : '…')
                    : currentRole.key === 'TalentManager'
                    ? (summary ? summary.pendingApplications : '…')
                    : currentRole.key === 'Finance' || currentRole.key === 'FinanceApprover'
                    ? (summary ? summary.pendingSettlements : '…')
                    : (summary ? summary.sessionsThisWeek : '…')}
                </div>
                <div className="stat-note blue">
                  {currentRole.key === 'Booking'
                    ? 'Deliverables đang chờ kiểm duyệt'
                    : currentRole.key === 'TalentManager'
                    ? 'Hồ sơ chờ Talent Manager phỏng vấn'
                    : currentRole.key === 'Finance' || currentRole.key === 'FinanceApprover'
                    ? 'Đang chờ thẩm định & phê duyệt'
                    : `${summary ? summary.sessionsToday : 0} phiên đã lên lịch hôm nay`}
                </div>
                <div className="sparkline blue">
                  <svg viewBox="0 0 120 38" role="img" aria-label="Biểu đồ phiên live">
                    <path d="M2 32 C18 30 25 24 38 25 S55 12 70 18 S90 8 105 14 S115 6 119 4" />
                  </svg>
                </div>
              </article>

              {/* Card 3 */}
              <article className="stat-card">
                <div className="stat-icon orange">✓</div>
                <div className="stat-label">
                  {currentRole.key === 'TalentManager'
                    ? 'Kênh livestream liên kết'
                    : currentRole.key === 'Booking'
                    ? 'Yêu cầu hợp tác Brand mới'
                    : 'Công việc cần xử lý'}
                </div>
                <div className="stat-value">
                  {currentRole.key === 'TalentManager'
                    ? '6 kênh'
                    : currentRole.key === 'Booking'
                    ? (summary ? summary.pendingPartnerships : '…')
                    : (summary ? summary.openTasks : '…')}
                </div>
                <div className="stat-note orange">
                  {currentRole.key === 'TalentManager'
                    ? 'YouTube, TikTok Live & Twitch'
                    : currentRole.key === 'Booking'
                    ? 'Cần Booking Specialist liên hệ'
                    : 'Ưu tiên giải quyết trong ngày'}
                </div>
                <div className="sparkline orange">
                  <svg viewBox="0 0 120 38" role="img" aria-label="Biểu đồ booking">
                    <path d="M2 28 C20 25 32 30 45 20 S72 22 85 15 S102 12 119 5" />
                  </svg>
                </div>
              </article>

              {/* Card 4 */}
              <article className="stat-card">
                <div className="stat-icon green">₫</div>
                <div className="stat-label">
                  {currentRole.key === 'OperationsManager'
                    ? 'Streamer đang lên sóng'
                    : currentRole.key === 'HRManager'
                    ? 'Nhân sự & Đội ngũ'
                    : 'Doanh thu tích lũy'}
                </div>
                <div className="stat-value" style={{ fontSize: currentRole.key === 'OperationsManager' || currentRole.key === 'HRManager' ? '22px' : '20px' }}>
                  {currentRole.key === 'OperationsManager'
                    ? `${summary ? summary.activeStreamers : 4} creator`
                    : currentRole.key === 'HRManager'
                    ? '8 nhân sự'
                    : formattedRevenue}
                </div>
                <div className="stat-note green">
                  {currentRole.key === 'OperationsManager'
                    ? 'Đang phát trực tiếp ca hôm nay'
                    : currentRole.key === 'HRManager'
                    ? '7 phòng ban trực thuộc'
                    : `${summary ? summary.pendingSettlements : 0} kỳ đối soát chờ duyệt`}
                </div>
                <div className="sparkline green">
                  <svg viewBox="0 0 120 38" role="img" aria-label="Biểu đồ doanh thu">
                    <path d="M2 34 C15 32 30 20 45 24 S65 18 80 12 S98 10 119 2" />
                  </svg>
                </div>
              </article>
            </section>

            {/* Schedule & Live Operations */}
            <section className="content-grid">
              <article className="panel schedule-panel" id="schedule">
                <div className="panel-heading">
                  <div>
                    <div className="eyebrow">LỊCH VẬN HÀNH LIVESTREAM</div>
                    <h2>Lịch livestream hôm nay</h2>
                  </div>
                  <button className="text-link" onClick={() => navigate('schedules')}>
                    Xem tất cả lịch <span>→</span>
                  </button>
                </div>
                <div className="schedule-table">
                  <div className="table-head">
                    <span>THỜI GIAN</span>
                    <span>STREAMER & NỘI DUNG</span>
                    <span>ĐỘI PHỤ TRÁCH</span>
                    <span>TRẠNG THÁI</span>
                  </div>
                  {schedules.length === 0 ? (
                    <div style={{ padding: '24px', textAlign: 'center', color: '#8892a4' }}>
                      Chưa có lịch livestream phát sóng hôm nay.
                    </div>
                  ) : (
                    schedules.map(item => (
                      <div className="schedule-row" key={item.id}>
                        <div className="time-cell">
                          <span className="time-dot" />
                          {item.time}
                        </div>
                        <div className="streamer-cell">
                          <div className="streamer-avatar">{item.name.slice(0, 1)}</div>
                          <div>
                            <b>{item.name}</b>
                            <small>{item.platform} <span>·</span> {item.title}</small>
                          </div>
                        </div>
                        <div className="team-cell">{item.team}</div>
                        <div>
                          <span className={`status-pill ${item.tone}`}>
                            <i />{item.state}
                          </span>
                        </div>
                      </div>
                    ))
                  )}
                </div>
                <div className="panel-foot">
                  <span className="live-indicator"><i /> {schedules.length} phiên trong danh sách</span>
                  <button className="text-link" onClick={() => navigate('schedules')}>
                    + Thêm lịch phát sóng <span>→</span>
                  </button>
                </div>
              </article>

              {/* Recent Activities */}
              <article className="panel activity-panel">
                <div className="panel-heading">
                  <div>
                    <div className="eyebrow">NHẬT KÝ VẬN HÀNH</div>
                    <h2>Hoạt động gần đây</h2>
                  </div>
                  <button className="more-button" aria-label="Xem thêm hoạt động" onClick={() => navigate('audit')}>•••</button>
                </div>
                <div className="activity-list">
                  {activities.length === 0 ? (
                    <div style={{ padding: '24px', textAlign: 'center', color: '#8892a4' }}>
                      Chưa ghi nhận hoạt động nào.
                    </div>
                  ) : (
                    activities.map((act, idx) => (
                      <div className="activity-item" key={act.id}>
                        <div className={`activity-icon ${['lavender', 'mint', 'peach', 'sky'][idx % 4]}`}>
                          {['◉', '✓', '◇', '▤'][idx % 4]}
                        </div>
                        <div>
                          <p>
                            <b>{act.actorName}</b> {act.summary}
                          </p>
                          <small>{act.timeAgo} <span>·</span> {act.action}</small>
                        </div>
                      </div>
                    ))
                  )}
                </div>
                <a className="activity-link" href="#audit" onClick={e => { e.preventDefault(); navigate('audit'); }}>
                  Xem toàn bộ nhật ký kiểm toán <span>→</span>
                </a>
              </article>
            </section>

            {/* Streamer Performance & Tasks */}
            <section className="bottom-grid">
              <article className="panel performance-panel">
                <div className="panel-heading">
                  <div>
                    <div className="eyebrow">HIỆU SUẤT CREATOR</div>
                    <h2>Top streamer nổi bật</h2>
                  </div>
                  <button className="select-button" onClick={() => navigate('reports')}>
                    Xem báo cáo <span>▼</span>
                  </button>
                </div>
                <div className="performance-list">
                  {topStreamers.length === 0 ? (
                    <div style={{ padding: '20px', textAlign: 'center', color: '#8892a4' }}>
                      Đang tải danh sách streamer…
                    </div>
                  ) : (
                    topStreamers.map(item => (
                      <div className="performer" key={item.id}>
                        <span className={`rank ${item.rank === '1' ? 'gold' : item.rank === '2' ? 'silver' : 'bronze'}`}>
                          0{item.rank}
                        </span>
                        <div className={`performer-avatar p${item.rank}`}>{item.name.slice(0, 1)}</div>
                        <div className="performer-name">
                          <b>{item.name}</b>
                          <small>{item.category}</small>
                        </div>
                        <div className="performance-metric">
                          <b>{item.views}</b>
                          <small>Lượt xem</small>
                        </div>
                        <span className="performance-change">{item.growth}</span>
                      </div>
                    ))
                  )}
                </div>
              </article>

              <article className="panel tasks-panel">
                <div className="panel-heading">
                  <div>
                    <div className="eyebrow">CÔNG VIỆC CẦN XỬ LÝ</div>
                    <h2>Nhiệm vụ liên phòng ban <span className="task-count">{tasks.length}</span></h2>
                  </div>
                  <button className="text-link" onClick={() => navigate('tasks')}>
                    Xem tất cả <span>→</span>
                  </button>
                </div>
                <div className="task-list">
                  {tasks.length === 0 ? (
                    <div style={{ padding: '20px', textAlign: 'center', color: '#8892a4' }}>
                      Không có công việc nào cần xử lý.
                    </div>
                  ) : (
                    tasks.map(t => (
                      <label className="task-item" key={t.id}>
                        <input
                          type="checkbox"
                          checked={completingTaskId === t.id}
                          disabled={completingTaskId === t.id}
                          onChange={() => void handleCompleteTask(t.id)}
                          title="Đánh dấu hoàn thành"
                        />
                        <span className="task-copy">
                          <b>{t.title}</b>
                          <small>
                            <i className="task-tag finance-tag">{t.teamName}</i>
                            {' '}Hạn: {t.dueDisplay} ({t.ownerName})
                          </small>
                        </span>
                        <span className="task-avatar">{t.ownerName.slice(0, 2).toUpperCase()}</span>
                      </label>
                    ))
                  )}
                </div>
              </article>
            </section>

            <footer className="footer">
              <span>© 2026 {agencyName} · Hệ thống vận hành StreamOps</span>
              <span><i className="secure-dot" /> Dữ liệu trực tiếp từ PostgreSQL và xác thực JWT</span>
            </footer>
          </>
        ) : page === 'reports' ? (
          <ReportsView />
        ) : (
          <ModuleWorkspace page={page} />
        )}
      </main>
    </div>
  );
}

export default App;
