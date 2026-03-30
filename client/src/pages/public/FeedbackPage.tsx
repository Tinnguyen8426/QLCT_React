import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../../api/client';
import { useAuthStore } from '../../stores/authStore';
import { useToast, ToastContainer } from '../../components/Toast';

export default function FeedbackPage() {
  const [data, setData] = useState<any>({ feedbacks: [] });
  const [completedAppointments, setCompletedAppointments] = useState<any[]>([]);
  const { user } = useAuthStore();
  const { toasts, show } = useToast();

  const [form, setForm] = useState({ appointmentId: '', rating: 5, comment: '' });
  const [submitting, setSubmitting] = useState(false);

  const loadFeedbacks = () => { api.get('/api/feedback').then(r => setData(r.data)); };

  useEffect(() => {
    loadFeedbacks();
    if (user) {
      api.get('/api/feedback/completed-appointments').then(r => setCompletedAppointments(r.data));
    }
  }, [user]);

  const submitFeedback = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.rating) { show('Vui lòng chọn số sao đánh giá', 'error'); return; }
    setSubmitting(true);
    try {
      const selectedAppt = completedAppointments.find(a => a.appointmentId.toString() === form.appointmentId);
      
      await api.post('/api/feedback', {
        serviceId: selectedAppt ? selectedAppt.serviceId : null,
        staffId: selectedAppt ? selectedAppt.staffId : null,
        rating: form.rating,
        comment: form.comment
      });
      show('Cảm ơn bạn đã gửi đánh giá!');
      setForm({ appointmentId: '', rating: 5, comment: '' });
      loadFeedbacks();
    } catch (err: any) {
      show(err.response?.data?.message || 'Có lỗi xảy ra', 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const renderStarsList = (currentRating: number, setRating?: (r: number) => void) => {
    return (
      <div className="d-flex gap-1" style={{ fontSize: '1.5rem', color: 'var(--accent)', cursor: setRating ? 'pointer' : 'default' }}>
        {[1, 2, 3, 4, 5].map(star => (
          <i key={star} className={star <= currentRating ? "fas fa-star" : "far fa-star"} 
             onClick={() => setRating && setRating(star)}></i>
        ))}
      </div>
    );
  };

  return (
    <div className="container py-5">
      <ToastContainer toasts={toasts} />
      <div className="page-header text-center mb-5">
        <h2><i className="fas fa-star me-2" style={{ color: 'var(--accent)' }}></i>Đánh Giá & Phản Hồi</h2>
        <p className="text-secondary">Hãy cho chúng tôi biết trải nghiệm của bạn tại BarberShop!</p>
      </div>

      <div className="row g-5">
        {/* Form Submission Section */}
        <div className="col-lg-5">
          <div className="card-dark p-4 position-sticky" style={{ top: '2rem' }}>
            <h5 className="fw-bold mb-4"><i className="fas fa-pen me-2"></i>Gửi đánh giá</h5>
            {user ? (
              completedAppointments.length > 0 ? (
                <form onSubmit={submitFeedback}>
                  <div className="mb-3">
                    <label className="form-label small text-secondary">Đánh giá sao *</label>
                    {renderStarsList(form.rating, (r) => setForm({ ...form, rating: r }))}
                  </div>
                  <div className="mb-3">
                    <label className="form-label small text-secondary">Chọn lịch đã hoàn thành *</label>
                    <select className="form-select" value={form.appointmentId} onChange={e => setForm({ ...form, appointmentId: e.target.value })} required>
                      <option value="">-- Chọn lịch cắt --</option>
                      {completedAppointments.map(a => (
                        <option key={a.appointmentId} value={a.appointmentId}>
                          {`Ngày ${a.date} ${a.time} | ${a.serviceName} (${a.staffName})`}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="mb-4">
                    <label className="form-label small text-secondary">Bình luận của bạn</label>
                    <textarea className="form-control" rows={4} placeholder="Chia sẻ thêm về trải nghiệm..." 
                      value={form.comment} onChange={e => setForm({ ...form, comment: e.target.value })}></textarea>
                  </div>
                  <button type="submit" className="btn btn-accent w-100" disabled={submitting}>
                    {submitting ? <span className="spinner-border spinner-border-sm me-2"></span> : <i className="fas fa-paper-plane me-2"></i>}
                    Gửi Đánh Giá
                  </button>
                </form>
              ) : (
                <div className="text-center py-4">
                  <i className="fas fa-calendar-times fa-3x text-secondary mb-3"></i>
                  <p>Bạn chưa có lịch cắt nào đã hoàn thành để đánh giá.</p>
                  <Link to="/booking" className="btn btn-outline-light">Đặt lịch ngay</Link>
                </div>
              )
            ) : (
              <div className="text-center py-4">
                <i className="fas fa-user-lock fa-3x text-secondary mb-3"></i>
                <p>Bạn cần đăng nhập để gửi đánh giá dịch vụ.</p>
                <Link to="/login" className="btn btn-outline-light">Đăng nhập ngay</Link>
              </div>
            )}
          </div>
        </div>

        {/* Feedback List Section */}
        <div className="col-lg-7">
          <h5 className="fw-bold mb-4"><i className="fas fa-comments me-2"></i>Đánh giá gần đây</h5>
          {data.feedbacks.length === 0 ? (
            <div className="text-center text-secondary py-5 card-dark"><i className="fas fa-inbox fa-2x mb-2 d-block"></i>Chưa có đánh giá nào</div>
          ) : (
            <div className="d-flex flex-column gap-3">
              {data.feedbacks.map((f: any) => (
                <div key={f.feedbackId} className="card-dark p-4 animate-in">
                  <div className="d-flex justify-content-between align-items-start mb-3">
                    <div className="d-flex align-items-center gap-3">
                      <div className="rounded-circle d-flex align-items-center justify-content-center" 
                           style={{ width: 44, height: 44, background: 'rgba(201,168,76,0.15)', color: 'var(--accent)', fontSize: '1.2rem' }}>
                        {f.customerName.charAt(0).toUpperCase()}
                      </div>
                      <div>
                        <div className="fw-bold" style={{ fontSize: '1.1rem' }}>{f.customerName}</div>
                        <small className="text-muted">{f.createdAt ? new Date(f.createdAt).toLocaleDateString('vi-VN') : ''}</small>
                      </div>
                    </div>
                    {renderStarsList(f.rating || 0)}
                  </div>
                  
                  {(f.serviceName || f.staffName) && (
                    <div className="d-flex gap-2 flex-wrap mb-2">
                      {f.serviceName && <span className="badge bg-secondary"><i className="fas fa-cut me-1"></i>{f.serviceName}</span>}
                      {f.staffName && <span className="badge" style={{ background: '#17a2b822', color: '#17a2b8' }}><i className="fas fa-user-tie me-1"></i>{f.staffName}</span>}
                    </div>
                  )}
                  {f.comment && <p className="mb-0 text-secondary" style={{ fontSize: '0.95rem', lineHeight: 1.6 }}>"{f.comment}"</p>}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
