import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../api/client';
import { useAuthStore } from '../../stores/authStore';
interface Service { serviceId: number; serviceName: string; price: number; duration?: number; category?: string; }
interface Staff { userId: number; fullName: string; }

export default function BookingPage() {
  const [services, setServices] = useState<Service[]>([]);
  const [staffList, setStaffList] = useState<Staff[]>([]);
  const [selected, setSelected] = useState<number[]>([]);
  const [form, setForm] = useState({ fullName: '', phone: '', date: '', time: '', staffId: '', note: '' });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const { user } = useAuthStore();
  const navigate = useNavigate();

  useEffect(() => {
    if (user) {
      setForm(prev => ({ ...prev, fullName: user.fullName || '', phone: user.phone || '' }));
    }
  }, [user]);

  useEffect(() => {
    api.get('/api/services').then(r => setServices(r.data.services));
    api.get('/api/booking/staff').then(r => setStaffList(r.data));
  }, []);

  const total = services.filter(s => selected.includes(s.serviceId)).reduce((a, s) => a + s.price, 0);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true); setError('');
    try {
      await api.post('/api/booking', {
        ...form,
        staffId: form.staffId ? parseInt(form.staffId) : null,
        serviceIds: selected
      });
      navigate('/booking/success');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Đặt lịch thất bại.');
    } finally { setLoading(false); }
  };

  return (
    <div className="container py-5">
      <div className="page-header text-center">
        <h2><i className="fas fa-calendar-plus me-2" style={{ color: 'var(--accent)' }}></i>Đặt Lịch Hẹn</h2>
      </div>

      <form onSubmit={submit}>
        <div className="row g-4">
          <div className="col-lg-7">
            <div className="card-dark p-4 mb-4">
              <h5 className="fw-bold mb-3"><i className="fas fa-user me-2"></i>Thông tin của bạn</h5>
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="form-label small text-secondary">Họ tên *</label>
                  <input className="form-control" value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} required readOnly={!!user?.fullName} style={{ background: user?.fullName ? 'rgba(255,255,255,0.05)' : '' }} />
                </div>
                <div className="col-md-6">
                  <label className="form-label small text-secondary">Số điện thoại *</label>
                  <input className="form-control" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} required readOnly={!!user?.phone} style={{ background: user?.phone ? 'rgba(255,255,255,0.05)' : '' }} />
                </div>
                <div className="col-md-4">
                  <label className="form-label small text-secondary">Ngày *</label>
                  <input type="date" className="form-control" value={form.date} onChange={e => setForm({ ...form, date: e.target.value })} required />
                </div>
                <div className="col-md-4">
                  <label className="form-label small text-secondary">Giờ *</label>
                  <input type="time" className="form-control" value={form.time} onChange={e => setForm({ ...form, time: e.target.value })} required />
                </div>
                <div className="col-md-4">
                  <label className="form-label small text-secondary">Nhân viên</label>
                  <select className="form-select" value={form.staffId} onChange={e => setForm({ ...form, staffId: e.target.value })}>
                    <option value="">Tự động phân công</option>
                    {staffList.map(s => <option key={s.userId} value={s.userId}>{s.fullName}</option>)}
                  </select>
                </div>
                <div className="col-12">
                  <label className="form-label small text-secondary">Ghi chú</label>
                  <textarea className="form-control" rows={2} placeholder="Bạn có yêu cầu đặc biệt nào không?" value={form.note} onChange={e => setForm({ ...form, note: e.target.value })} />
                </div>
              </div>
            </div>

            <div className="card-dark p-4">
              <h5 className="fw-bold mb-3"><i className="fas fa-cut me-2"></i>Chọn dịch vụ *</h5>
              <div className="row g-3">
                {services.map(s => (
                  <div key={s.serviceId} className="col-md-6">
                    <div
                      className="p-3 rounded-3 d-flex justify-content-between align-items-center cursor-pointer"
                      style={{
                        background: selected.includes(s.serviceId) ? 'rgba(201,168,76,0.15)' : 'var(--bg-input)',
                        border: selected.includes(s.serviceId) ? '2px solid var(--accent)' : '2px solid transparent',
                        cursor: 'pointer'
                      }}
                      onClick={() => setSelected(prev =>
                        prev.includes(s.serviceId) ? prev.filter(id => id !== s.serviceId) : [...prev, s.serviceId]
                      )}
                    >
                      <div>
                        <div className="fw-bold">{s.serviceName}</div>
                        {s.duration && <small className="text-secondary">{s.duration} phút</small>}
                      </div>
                      <span style={{ color: 'var(--accent)', fontWeight: 600 }}>{s.price.toLocaleString('vi-VN')}đ</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="col-lg-5">
            <div className="card-dark p-4 position-sticky" style={{ top: '1rem' }}>
              <h5 className="fw-bold mb-3"><i className="fas fa-receipt me-2"></i>Tóm tắt</h5>
              {selected.length === 0 ? (
                <p className="text-secondary">Chưa chọn dịch vụ nào</p>
              ) : (
                <>
                  {services.filter(s => selected.includes(s.serviceId)).map(s => (
                    <div key={s.serviceId} className="d-flex justify-content-between mb-2">
                      <span>{s.serviceName}</span>
                      <span>{s.price.toLocaleString('vi-VN')}đ</span>
                    </div>
                  ))}
                  <hr style={{ borderColor: 'var(--border-color)' }} />
                  <div className="d-flex justify-content-between fw-bold" style={{ fontSize: '1.1rem' }}>
                    <span>Tổng cộng</span>
                    <span style={{ color: 'var(--accent)' }}>{total.toLocaleString('vi-VN')}đ</span>
                  </div>
                </>
              )}

              {error && <div className="alert alert-danger mt-3 mb-0">{error}</div>}

              <button type="submit" className="btn btn-accent w-100 mt-4" disabled={loading || selected.length === 0}>
                {loading ? <><span className="spinner-border spinner-border-sm me-2"></span>Đang xử lý...</> : <><i className="fas fa-check me-2"></i>Xác nhận đặt lịch</>}
              </button>
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}
