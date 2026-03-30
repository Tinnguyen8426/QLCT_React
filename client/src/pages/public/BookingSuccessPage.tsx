import { Link } from 'react-router-dom';

export default function BookingSuccessPage() {
  return (
    <div className="container py-5 text-center">
      <div className="card-dark p-5 mx-auto animate-in" style={{ maxWidth: 500 }}>
        <div style={{ fontSize: '4rem', color: 'var(--success)' }}><i className="fas fa-check-circle"></i></div>
        <h3 className="fw-bold mt-3 mb-2">Đặt lịch thành công!</h3>
        <p className="text-secondary mb-4">Cảm ơn bạn đã đặt lịch. Chúng tôi sẽ xác nhận lịch hẹn sớm nhất.</p>
        <div className="d-flex justify-content-center gap-3">
          <Link to="/" className="btn btn-outline-secondary">Về trang chủ</Link>
          <Link to="/customer/appointments" className="btn btn-accent">Xem lịch hẹn</Link>
        </div>
      </div>
    </div>
  );
}
