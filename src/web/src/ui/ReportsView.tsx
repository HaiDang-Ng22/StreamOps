import { useEffect, useState } from 'react';
import { apiRequest } from '../api/client';

type RevenueReport = {
  totalRevenue: number;
  totalExpense: number;
  netIncome: number;
  currency: string;
  byCategory: { type: string; category: string; total: number; count: number }[];
  byStreamer: { streamerId: string; streamerName: string; revenue: number; expense: number }[];
};

type StreamerPerformance = {
  id: string;
  code: string;
  stageName: string;
  status: string;
  totalSessions: number;
  totalViews: number;
  peakCcu: number;
  totalDonations: number;
  currency: string;
};

export default function ReportsView() {
  const [revenue, setRevenue] = useState<RevenueReport | null>(null);
  const [performances, setPerformances] = useState<StreamerPerformance[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadData = async () => {
    setLoading(true);
    setError('');
    try {
      const [revRes, perfRes] = await Promise.all([
        apiRequest<RevenueReport>('/reports/revenue'),
        apiRequest<StreamerPerformance[]>('/reports/streamer-performance')
      ]);
      setRevenue(revRes);
      setPerformances(perfRes);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không tải được báo cáo.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const formatVND = (val: number) => new Intl.NumberFormat('vi-VN').format(val) + ' ₫';

  return (
    <section className="workspace-module">
      <div className="module-heading">
        <div>
          <div className="eyebrow">ANALYTICS & FINANCIAL REPORTING</div>
          <h1>Báo cáo Doanh thu & Hiệu suất Streamer</h1>
          <p>Dữ liệu tổng hợp từ các giao dịch tài chính đã ghi sổ và chỉ số ghi nhận từ các phiên livestream.</p>
        </div>
        <div className="module-actions">
          <button className="secondary-button" onClick={() => void loadData()} disabled={loading}>
            {loading ? 'Đang tải…' : '↻ Làm mới số liệu'}
          </button>
        </div>
      </div>

      {error && <div className="notice error">{error}</div>}

      {revenue && (
        <section className="stat-grid" style={{ marginBottom: '24px' }}>
          <article className="stat-card">
            <div className="stat-icon green">₫</div>
            <div className="stat-label">Tổng doanh thu hệ thống</div>
            <div className="stat-value" style={{ fontSize: '20px' }}>{formatVND(revenue.totalRevenue)}</div>
            <div className="stat-note green">Thu từ quà tặng & booking</div>
          </article>

          <article className="stat-card">
            <div className="stat-icon orange">▼</div>
            <div className="stat-label">Tổng chi phí vận hành</div>
            <div className="stat-value" style={{ fontSize: '20px' }}>{formatVND(revenue.totalExpense)}</div>
            <div className="stat-note orange">Chi phí sản xuất, thiết bị, bản quyền</div>
          </article>

          <article className="stat-card">
            <div className="stat-icon blue">▲</div>
            <div className="stat-label">Lợi nhuận ròng (Net Income)</div>
            <div className="stat-value" style={{ fontSize: '20px' }}>{formatVND(revenue.netIncome)}</div>
            <div className="stat-note blue">Sau khi trừ các chi phí ghi nhận</div>
          </article>
        </section>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '20px', marginBottom: '24px' }}>
        <article className="panel" style={{ padding: '20px' }}>
          <div className="panel-heading" style={{ marginBottom: '16px' }}>
            <div>
              <div className="eyebrow">CƠ CẤU DOANH THU & CHI PHÍ</div>
              <h2 style={{ fontSize: '18px' }}>Theo danh mục giao dịch</h2>
            </div>
          </div>
          <table className="module-table">
            <thead>
              <tr>
                <th>Loại</th>
                <th>Danh mục</th>
                <th>Số giao dịch</th>
                <th style={{ textAlign: 'right' }}>Tổng số tiền</th>
              </tr>
            </thead>
            <tbody>
              {revenue?.byCategory.map((cat, i) => (
                <tr key={i}>
                  <td>
                    <span className={`status-pill ${cat.type === 'revenue' ? 'green' : 'orange'}`}>
                      {cat.type === 'revenue' ? 'Doanh thu' : 'Chi phí'}
                    </span>
                  </td>
                  <td><b>{cat.category}</b></td>
                  <td>{cat.count} GD</td>
                  <td style={{ textAlign: 'right', fontWeight: 600 }}>{formatVND(cat.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </article>

        <article className="panel" style={{ padding: '20px' }}>
          <div className="panel-heading" style={{ marginBottom: '16px' }}>
            <div>
              <div className="eyebrow">DOANH THU THEO CREATOR</div>
              <h2 style={{ fontSize: '18px' }}>Xếp hạng doanh thu Streamer</h2>
            </div>
          </div>
          <table className="module-table">
            <thead>
              <tr>
                <th>Streamer</th>
                <th style={{ textAlign: 'right' }}>Doanh thu</th>
                <th style={{ textAlign: 'right' }}>Chi phí</th>
                <th style={{ textAlign: 'right' }}>Chênh lệch</th>
              </tr>
            </thead>
            <tbody>
              {revenue?.byStreamer.map(st => (
                <tr key={st.streamerId}>
                  <td><b>{st.streamerName}</b></td>
                  <td style={{ textAlign: 'right', color: 'var(--green-accent, #10b981)', fontWeight: 600 }}>
                    {formatVND(st.revenue)}
                  </td>
                  <td style={{ textAlign: 'right', color: 'var(--orange-accent, #f59e0b)' }}>
                    {formatVND(st.expense)}
                  </td>
                  <td style={{ textAlign: 'right', fontWeight: 700 }}>
                    {formatVND(st.revenue - st.expense)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </article>
      </div>

      <article className="panel" style={{ padding: '20px' }}>
        <div className="panel-heading" style={{ marginBottom: '16px' }}>
          <div>
            <div className="eyebrow">CHỈ SỐ TƯƠNG TÁC LIVESTREAM</div>
            <h2 style={{ fontSize: '18px' }}>Bảng theo dõi KPI hiệu suất Creator</h2>
          </div>
        </div>
        <table className="module-table">
          <thead>
            <tr>
              <th>Mã</th>
              <th>Nghệ danh</th>
              <th>Trạng thái</th>
              <th style={{ textAlign: 'right' }}>Số phiên</th>
              <th style={{ textAlign: 'right' }}>Tổng lượt xem</th>
              <th style={{ textAlign: 'right' }}>CCU Đỉnh</th>
              <th style={{ textAlign: 'right' }}>Doanh thu quà tặng</th>
            </tr>
          </thead>
          <tbody>
            {performances.map(p => (
              <tr key={p.id}>
                <td><code>{p.code}</code></td>
                <td><b>{p.stageName}</b></td>
                <td>
                  <span className={`status-pill ${p.status === 'active' ? 'green' : 'orange'}`}>
                    {p.status}
                  </span>
                </td>
                <td style={{ textAlign: 'right' }}>{p.totalSessions}</td>
                <td style={{ textAlign: 'right', fontWeight: 600 }}>
                  {new Intl.NumberFormat('vi-VN').format(p.totalViews)}
                </td>
                <td style={{ textAlign: 'right', color: '#2563eb', fontWeight: 600 }}>
                  {new Intl.NumberFormat('vi-VN').format(p.peakCcu)}
                </td>
                <td style={{ textAlign: 'right', color: '#10b981', fontWeight: 600 }}>
                  {formatVND(p.totalDonations)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </article>
    </section>
  );
}
