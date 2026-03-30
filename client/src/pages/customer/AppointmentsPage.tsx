import { useEffect, useState } from 'react';
import api from '../../api/client';
import Modal from '../../components/Modal';
import { useToast, ToastContainer } from '../../components/Toast';

export default function CustomerAppointmentsPage() {
  const [data, setData] = useState<any>({ appointments: [], total: 0 });
  const [page, setPage] = useState(1);
  const [feedbackOpen, setFeedbackOpen] = useState(false);
  const [selectedAppt, setSelectedAppt] = useState<any>(null);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const { toasts, show } = useToast();

  useEffect(() => { api.get('/api/customer/appointments', { params: { page } }).then(r => setData(r.data)); }, [page]);

  const openFeedback = (appt: any) => {
    setSelectedAppt(appt);
    setRating(5);
    setComment('');
    setFeedbackOpen(true);
  };

  const submitFeedback = async () => {
    if (!selectedAppt) return;
    setSubmitting(true);
    try {
      await api.post('/api/feedback', {
        serviceId: selectedAppt.services?.[0]?.serviceId || null,
        staffId: selectedAppt.staffId,
        rating,
        comment
      });
      show('Cảm ơn bạn đã gửi đánh giá!');
      setFeedbackOpen(false);
    } catch {
      show('Có lỗi xảy ra', 'error');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="container py-5">
      <ToastContainer toasts={toasts} />
      <h2 className="fw-bold mb-4"><i className="fas fa-calendar me-2" style={{ color: 'var(--accent)' }}></i>Lịch Hẹn Của Tôi</h2>

      {data.appointments.length === 0 ? (
        <div className="card-dark p-5 text-center"><p className="text-secondary">Chưa có lịch hẹn nào.</p></div>
      ) : (
        <div className="card-dark overflow-hidden">
          <div className="table-responsive">
            <table className="table table-dark-custom mb-0">
              <thead><tr><th>#</th><th>Ngày</th><th>Giờ</th><th>Dịch vụ</th><th>Nhân viên</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
              <tbody>
                {data.appointments.map((a: any) => (
                  <tr key={a.appointmentId}>
                    <td>{a.appointmentId}</td>
                    <td>{a.date}</td>
                    <td>{a.time}</td>
                    <td>{a.services.map((s: any) => s.serviceName).join(', ')}</td>
                    <td>{a.staffName || '—'}</td>
                    <td><span className={`badge-status badge-${(a.status || 'pending').toLowerCase()}`}>{a.status || 'Pending'}</span></td>
                    <td>
                      {a.status === 'Completed' && (
                        <button className="btn btn-outline-warning btn-sm" onClick={() => openFeedback(a)}>
                          <i className="fas fa-star me-1"></i>Đánh giá
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
      <div className="d-flex justify-content-between mt-3">
        <small className="text-secondary">Tổng: {data.total}</small>
        <div className="d-flex gap-2">
          <button className="btn btn-outline-secondary btn-sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Trước</button>
          <button className="btn btn-outline-secondary btn-sm" onClick={() => setPage(p => p + 1)}>Sau →</button>
        </div>
      </div>

      <Modal isOpen={feedbackOpen} onClose={() => setFeedbackOpen(false)} title={`Đánh giá Dịch vụ #${selectedAppt?.appointmentId || ''}`}>
        <div className="row g-3">
          <div className="col-12 text-center mb-3">
            <div className="d-flex justify-content-center gap-2">
              {[1, 2, 3, 4, 5].map(star => (
                <i key={star} className={`fas fa-star fs-2 ${star <= rating ? 'text-warning cursor-pointer' : 'text-secondary cursor-pointer'}`}
                  style={{ cursor: 'pointer' }} onClick={() => setRating(star)}></i>
              ))}
            </div>
            <div className="text-warning fw-bold mt-2">{rating} Sao</div>
          </div>
          <div className="col-12">
            <label className="form-label small text-secondary">Nhận xét của bạn</label>
            <textarea className="form-control" rows={3} placeholder="Chia sẻ trải nghiệm của bạn..." value={comment} onChange={e => setComment(e.target.value)}></textarea>
          </div>
        </div>
        <div className="mt-4 d-flex justify-content-end gap-2">
          <button className="btn btn-outline-secondary btn-sm" onClick={() => setFeedbackOpen(false)}>Huỷ</button>
          <button className="btn btn-accent btn-sm" onClick={submitFeedback} disabled={submitting}>
            {submitting ? <span className="spinner-border spinner-border-sm me-1"></span> : <i className="fas fa-paper-plane me-1"></i>}
            Gửi đánh giá
          </button>
        </div>
      </Modal>
    </div>
  );
}
