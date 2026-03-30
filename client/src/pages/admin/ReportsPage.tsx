import { useEffect, useState } from 'react';
import api from '../../api/client';

export default function AdminReportsPage() {
  const [data, setData] = useState<any>(null);

  useEffect(() => { api.get('/api/admin/reports').then(r => setData(r.data)); }, []);

  if (!data) return <div className="text-center py-5"><span className="spinner-border text-light"></span></div>;

  return (
    <>
      <h2 className="fw-bold mb-4">Báo Cáo & Thống Kê</h2>

      <div className="row g-4">
        <div className="col-lg-7">
          <div className="card-dark p-4">
            <h5 className="fw-bold mb-3"><i className="fas fa-chart-bar me-2"></i>Doanh thu theo tháng</h5>
            <div className="table-responsive">
              <table className="table table-dark-custom mb-0">
                <thead><tr><th>Tháng</th><th>Số hoá đơn</th><th>Doanh thu</th></tr></thead>
                <tbody>
                  {(data.revenueByMonth || []).map((r: any, i: number) => (
                    <tr key={i}>
                      <td>{r.month}/{r.year}</td>
                      <td>{r.count}</td>
                      <td style={{ color: 'var(--accent)' }}>{r.revenue.toLocaleString('vi-VN')}đ</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <div className="col-lg-5">
          <div className="card-dark p-4">
            <h5 className="fw-bold mb-3"><i className="fas fa-trophy me-2"></i>Dịch vụ phổ biến nhất</h5>
            {(data.topServices || []).map((s: any, i: number) => (
              <div key={i} className="d-flex justify-content-between py-2" style={{ borderBottom: '1px solid var(--border-color)' }}>
                <div>
                  <span className="fw-bold">{i + 1}. {s.serviceName}</span>
                  <small className="text-secondary d-block">{s.count} lần sử dụng</small>
                </div>
                <span style={{ color: 'var(--accent)' }}>{s.totalRevenue.toLocaleString('vi-VN')}đ</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </>
  );
}
