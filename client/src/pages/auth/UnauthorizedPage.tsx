import { Link } from 'react-router-dom';

export default function UnauthorizedPage() {
  return (
    <div className="container py-5 d-flex justify-content-center align-items-center" style={{ minHeight: '80vh' }}>
      <div className="card-dark p-5 text-center animate-in" style={{ maxWidth: 500, width: '100%', border: '1px solid rgba(220, 53, 69, 0.2)' }}>
        <div className="mb-4">
          <i className="fas fa-exclamation-triangle fa-4x text-danger mb-3"></i>
          <h2 className="fw-bold text-white">Truy cập bị từ chối</h2>
          <div className="accent-line mx-auto mb-4" style={{ width: 60, height: 3, background: 'var(--accent)' }}></div>
        </div>
        
        <p className="text-secondary mb-4 fs-5">
          Bạn không có quyền truy cập vào khu vực này. Trang web này chỉ dành cho quản trị viên và nhân viên được ủy quyền.
        </p>

        <div className="d-grid gap-3">
          <Link to="/" className="btn btn-accent py-2">
            <i className="fas fa-home me-2"></i> Quay về trang chủ
          </Link>
          <Link to="/login" className="btn btn-outline-secondary py-2">
            <i className="fas fa-sign-in-alt me-2"></i> Đăng nhập bằng tài khoản khác
          </Link>
        </div>
      </div>
    </div>
  );
}
