import { useEffect, useState } from 'react';
import api from '../../api/client';

export default function AdminDashboardPage() {
  const [data, setData] = useState<any>(null);
  const [recent, setRecent] = useState<any[]>([]);

  useEffect(() => {
    api.get('/api/admin/dashboard').then(r => setData(r.data));
    api.get('/api/admin/dashboard/recent-appointments').then(r => setRecent(r.data));
  }, []);

  if (!data) return <div className="text-center py-5"><span className="spinner-border text-light"></span></div>;

  const stats = [
    { label: 'Lịch hẹn hôm nay', value: data.appointmentsToday, icon: 'fas fa-calendar-day', color: '#17a2b8' },
    { label: 'Hoàn thành hôm nay', value: data.completedToday, icon: 'fas fa-check-circle', color: '#28a745' },
    { label: 'Chờ xác nhận', value: data.pendingAppointments, icon: 'fas fa-clock', color: '#ffc107' },
    { label: 'Hoá đơn hôm nay', value: data.invoicesToday, icon: 'fas fa-file-invoice', color: '#6f42c1' },
    { label: 'Doanh thu tháng', value: `${(data.revenueThisMonth || 0).toLocaleString('vi-VN')}đ`, icon: 'fas fa-chart-line', color: '#c9a84c' },
    { label: 'Tổng khách hàng', value: data.totalCustomers, icon: 'fas fa-users', color: '#20c997' },
    { label: 'KH mới tháng này', value: data.newCustomersThisMonth, icon: 'fas fa-user-plus', color: '#e83e8c' },
    { label: 'Tổng doanh thu', value: `${(data.totalRevenue || 0).toLocaleString('vi-VN')}đ`, icon: 'fas fa-coins', color: '#fd7e14' },
  ];

  return (
    <>
      <h2 className="fw-bold mb-4">Dashboard</h2>

      <div className="row g-3 mb-4">
        {stats.map((s, i) => (
          <div key={i} className="col-6 col-lg-3">
            <div className="stat-card">
              <div className="d-flex align-items-center gap-3">
                <div className="rounded-circle d-flex align-items-center justify-content-center"
                  style={{ width: 42, height: 42, background: `${s.color}15`, color: s.color, flexShrink: 0 }}>
                  <i className={s.icon}></i>
                </div>
                <div>
                  <div className="stat-value" style={{ fontSize: '1.3rem' }}>{s.value}</div>
                  <div className="stat-label">{s.label}</div>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="card-dark p-4">
        <h5 className="fw-bold mb-3"><i className="fas fa-history me-2"></i>Lịch hẹn gần đây</h5>
        <div className="table-responsive">
          <table className="table table-dark-custom mb-0">
            <thead><tr><th>#</th><th>Khách hàng</th><th>Ngày</th><th>Giờ</th><th>Trạng thái</th></tr></thead>
            <tbody>
              {recent.map((a: any) => (
                <tr key={a.appointmentId}>
                  <td>{a.appointmentId}</td>
                  <td>{a.customerName}</td>
                  <td>{a.date}</td>
                  <td>{a.time}</td>
                  <td><span className={`badge-status badge-${(a.status || 'pending').toLowerCase()}`}>{a.status || 'Pending'}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
