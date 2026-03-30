import { useEffect, useState } from 'react';
import api from '../../api/client';
import { useAuthStore } from '../../stores/authStore';

export default function StaffDashboardPage() {
  const [data, setData] = useState<any>(null);
  const { user } = useAuthStore();

  useEffect(() => {
    api.get('/api/staff/dashboard').then(r => setData(r.data)).catch(console.error);
  }, []);

  if (!data) return <div className="text-center py-5"><span className="spinner-border text-light"></span></div>;

  const stats = [
    { label: 'Lịch hẹn hôm nay', value: data.totalToday, icon: 'fas fa-calendar-day', color: '#17a2b8' },
    { label: 'Đã xác nhận', value: data.confirmedToday, icon: 'fas fa-check', color: '#20c997' },
    { label: 'Hoàn thành', value: data.completedToday, icon: 'fas fa-check-circle', color: '#28a745' },
    { label: 'Chờ xác nhận', value: data.pendingToday, icon: 'fas fa-clock', color: '#ffc107' },
  ];

  return (
    <>
      <h2 className="fw-bold mb-4">Xin chào, {user?.fullName}!</h2>

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

      {/* Next Appointment */}
      {data.nextAppointment && (
        <div className="card-dark p-4 mb-4" style={{ borderLeft: '4px solid var(--accent)' }}>
          <h6 className="fw-bold mb-2"><i className="fas fa-bell me-2" style={{ color: 'var(--accent)' }}></i>Lịch hẹn tiếp theo</h6>
          <div className="d-flex gap-4 flex-wrap">
            <div><small className="text-secondary">Khách hàng</small><div className="fw-bold">{data.nextAppointment.customer?.fullName}</div></div>
            <div><small className="text-secondary">Giờ</small><div>{data.nextAppointment.appointmentTime}</div></div>
            <div><small className="text-secondary">Dịch vụ</small><div>{data.nextAppointment.services?.map((s: any) => s.serviceName).join(', ')}</div></div>
            <div><small className="text-secondary">Trạng thái</small><div><span className={`badge-status badge-${(data.nextAppointment.status || 'pending').toLowerCase()}`}>{data.nextAppointment.status}</span></div></div>
          </div>
        </div>
      )}

      {/* Upcoming */}
      <div className="card-dark p-4 mb-4">
        <h5 className="fw-bold mb-3"><i className="fas fa-calendar-alt me-2"></i>Lịch hẹn sắp tới</h5>
        <div className="table-responsive">
          <table className="table table-dark-custom mb-0">
            <thead><tr><th>Mã</th><th>Khách hàng</th><th>Ngày</th><th>Giờ</th><th>Dịch vụ</th><th>Trạng thái</th></tr></thead>
            <tbody>
              {(!data.upcomingAppointments || data.upcomingAppointments.length === 0) && (
                <tr><td colSpan={6} className="text-center text-secondary py-3"><i className="fas fa-inbox me-1"></i>Không có lịch hẹn sắp tới</td></tr>
              )}
              {(data.upcomingAppointments || []).map((a: any) => (
                <tr key={a.appointmentId}>
                  <td>{a.appointmentId}</td>
                  <td className="fw-bold">{a.customer?.fullName}</td>
                  <td>{a.appointmentDate}</td>
                  <td>{a.appointmentTime}</td>
                  <td><small>{a.services?.map((s: any) => s.serviceName).join(', ')}</small></td>
                  <td><span className={`badge-status badge-${(a.status || 'pending').toLowerCase()}`}>{a.status}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Recent Completed */}
      <div className="card-dark p-4">
        <h5 className="fw-bold mb-3"><i className="fas fa-check-circle me-2 text-success"></i>Hoàn thành gần đây</h5>
        <div className="table-responsive">
          <table className="table table-dark-custom mb-0">
            <thead><tr><th>Mã</th><th>Khách hàng</th><th>Ngày</th><th>Dịch vụ</th></tr></thead>
            <tbody>
              {(!data.recentCompletedAppointments || data.recentCompletedAppointments.length === 0) && (
                <tr><td colSpan={4} className="text-center text-secondary py-3"><i className="fas fa-inbox me-1"></i>Chưa có lịch hẹn hoàn thành</td></tr>
              )}
              {(data.recentCompletedAppointments || []).map((a: any) => (
                <tr key={a.appointmentId}>
                  <td>{a.appointmentId}</td>
                  <td className="fw-bold">{a.customer?.fullName}</td>
                  <td>{a.appointmentDate}</td>
                  <td><small>{a.services?.map((s: any) => s.serviceName).join(', ')}</small></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
