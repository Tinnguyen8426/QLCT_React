import { useEffect, useState } from 'react';
import api from '../../api/client';

export default function CustomerDashboardPage() {
  const [data, setData] = useState<any>(null);

  useEffect(() => { api.get('/api/customer/dashboard').then(r => setData(r.data)); }, []);

  if (!data) return <div className="container py-5 text-center"><span className="spinner-border"></span></div>;

  return (
    <div className="container py-5">
      <h2 className="fw-bold mb-4">Xin chào, <span style={{ color: 'var(--accent)' }}>{data.userName}</span>!</h2>

      <div className="row g-4 mb-4">
        {[
          { label: 'Tổng lịch hẹn', value: data.totalAppointments, icon: 'fas fa-calendar', color: '#17a2b8' },
          { label: 'Sắp tới', value: data.upcomingAppointments, icon: 'fas fa-clock', color: '#ffc107' },
          { label: 'Tổng chi tiêu', value: `${(data.totalSpending || 0).toLocaleString('vi-VN')}đ`, icon: 'fas fa-wallet', color: '#28a745' }
        ].map((s, i) => (
          <div key={i} className="col-md-4">
            <div className="stat-card d-flex align-items-center gap-3">
              <div className="rounded-circle d-flex align-items-center justify-content-center" style={{ width: 48, height: 48, background: `${s.color}20`, color: s.color }}>
                <i className={s.icon}></i>
              </div>
              <div>
                <div className="stat-value">{s.value}</div>
                <div className="stat-label">{s.label}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="row mb-4">
        <div className="col-12">
          <div className="card-dark p-3 d-flex justify-content-between align-items-center" style={{ border: '1px solid var(--accent)' }}>
            <div className="d-flex align-items-center gap-3">
              <div className="rounded-circle d-flex align-items-center justify-content-center" style={{ width: 48, height: 48, background: 'rgba(255, 193, 7, 0.2)', color: 'var(--accent)' }}>
                <i className="fas fa-box-open fs-4"></i>
              </div>
              <div>
                <h5 className="fw-bold mb-1">Tủ Mỹ Phẩm Cá Nhân</h5>
                <small className="text-secondary">Xem lại các sản phẩm đã mua và đặt hàng nhanh chóng.</small>
              </div>
            </div>
            <a href="/customer/products" className="btn btn-accent px-4">Xem ngay</a>
          </div>
        </div>
      </div>

      <div className="row g-4">
        <div className="col-lg-7">
          <div className="card-dark p-4">
            <h5 className="fw-bold mb-3"><i className="fas fa-calendar-alt me-2"></i>Lịch hẹn sắp tới</h5>
            {data.nextAppointments.length === 0 ? <p className="text-secondary">Không có lịch hẹn sắp tới.</p> :
              data.nextAppointments.map((a: any) => (
                <div key={a.appointmentId} className="d-flex justify-content-between align-items-center py-2" style={{ borderBottom: '1px solid var(--border-color)' }}>
                  <div>
                    <div className="fw-bold">{a.date} - {a.time}</div>
                    <small className="text-secondary">{a.services.join(', ')} • {a.staffName}</small>
                  </div>
                  <span className={`badge-status badge-${(a.status || 'pending').toLowerCase()}`}>{a.status || 'Pending'}</span>
                </div>
              ))
            }
          </div>
        </div>
        <div className="col-lg-5">
          <div className="card-dark p-4">
            <h5 className="fw-bold mb-3"><i className="fas fa-file-invoice me-2"></i>Hoá đơn gần đây</h5>
            {data.recentInvoices.length === 0 ? <p className="text-secondary">Chưa có hoá đơn.</p> :
              data.recentInvoices.map((inv: any) => (
                <div key={inv.invoiceId} className="d-flex justify-content-between py-2" style={{ borderBottom: '1px solid var(--border-color)' }}>
                  <span>#{inv.invoiceId}</span>
                  <span style={{ color: 'var(--accent)' }}>{(inv.finalAmount || 0).toLocaleString('vi-VN')}đ</span>
                </div>
              ))
            }
          </div>
        </div>
      </div>
    </div>
  );
}
