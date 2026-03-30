import { Outlet, Link, NavLink } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';
import { useCartStore } from '../stores/cartStore';
import { useEffect } from 'react';

export default function PublicLayout() {
  const { user, logout } = useAuthStore();
  const { totalQuantity, fetchCart } = useCartStore();

  useEffect(() => { fetchCart(); }, []);

  return (
    <div className="d-flex flex-column min-vh-100" style={{ background: 'var(--bg-dark)' }}>
      {/* Navbar */}
      <nav className="navbar navbar-expand-lg navbar-dark px-4 py-3" style={{ background: 'var(--bg-card)', borderBottom: '1px solid var(--border-color)' }}>
        <div className="container">
          <Link to="/" className="navbar-brand d-flex align-items-center gap-2">
            <i className="fas fa-cut" style={{ color: 'var(--accent)' }}></i>
            <span className="fw-bold" style={{ fontSize: '1.25rem' }}>BarberShop</span>
          </Link>

          <button className="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#mainNav">
            <span className="navbar-toggler-icon"></span>
          </button>

          <div className="collapse navbar-collapse" id="mainNav">
            <ul className="navbar-nav me-auto">
              <li className="nav-item">
                <NavLink to="/" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
                  <i className="fas fa-home me-1"></i> Trang chủ
                </NavLink>
              </li>
              <li className="nav-item">
                <NavLink to="/services" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
                  <i className="fas fa-cut me-1"></i> Dịch vụ
                </NavLink>
              </li>
              <li className="nav-item">
                <NavLink to="/booking" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
                  <i className="fas fa-calendar-plus me-1"></i> Đặt lịch
                </NavLink>
              </li>
              <li className="nav-item">
                <NavLink to="/store" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
                  <i className="fas fa-store me-1"></i> Cửa hàng
                </NavLink>
              </li>
              <li className="nav-item">
                <NavLink to="/feedback" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
                  <i className="fas fa-star me-1"></i> Đánh giá
                </NavLink>
              </li>
            </ul>

            <ul className="navbar-nav ms-auto align-items-center gap-2">
              <li className="nav-item">
                <Link to="/cart" className="nav-link position-relative">
                  <i className="fas fa-shopping-cart"></i>
                  {totalQuantity > 0 && (
                    <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill"
                      style={{ background: 'var(--accent)', fontSize: '0.65rem' }}>
                      {totalQuantity}
                    </span>
                  )}
                </Link>
              </li>

              {user ? (
                <li className="nav-item dropdown">
                  <a className="nav-link dropdown-toggle d-flex align-items-center gap-2" href="#" role="button"
                    data-bs-toggle="dropdown">
                    {user.avatarUrl
                      ? <img src={user.avatarUrl} alt="" className="rounded-circle" width="28" height="28" />
                      : <i className="fas fa-user-circle" style={{ fontSize: '1.5rem' }}></i>
                    }
                    <span>{user.fullName}</span>
                  </a>
                  <ul className="dropdown-menu dropdown-menu-end" style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)' }}>
                    {(user.role === 'Admin' || user.role === 'Administrator') ? (
                      <li><Link to="/admin" className="dropdown-item text-light" style={{ color: 'var(--accent) !important' }}><i className="fas fa-shield-alt me-2"></i>Quản trị Admin</Link></li>
                    ) : (user.role === 'Staff' || user.role === 'NhanVien') ? (
                      <li><Link to="/staff" className="dropdown-item text-light" style={{ color: 'var(--accent) !important' }}><i className="fas fa-id-badge me-2"></i>Staff Panel</Link></li>
                    ) : (
                      <li><Link to="/customer/dashboard" className="dropdown-item text-light"><i className="fas fa-tachometer-alt me-2"></i>Dashboard</Link></li>
                    )}
                    <li><Link to="/customer/appointments" className="dropdown-item text-light"><i className="fas fa-calendar me-2"></i>Lịch hẹn</Link></li>
                    <li><Link to="/customer/invoices" className="dropdown-item text-light"><i className="fas fa-file-invoice me-2"></i>Hoá đơn</Link></li>
                    <li><Link to="/customer/products" className="dropdown-item text-light"><i className="fas fa-box-open me-2"></i>Tủ mỹ phẩm</Link></li>
                    <li><Link to="/profile" className="dropdown-item text-light"><i className="fas fa-user me-2"></i>Hồ sơ</Link></li>
                    <li><hr className="dropdown-divider" style={{ borderColor: 'var(--border-color)' }} /></li>
                    <li>
                      <button className="dropdown-item text-danger" onClick={() => { logout(); window.location.href = '/'; }}>
                        <i className="fas fa-sign-out-alt me-2"></i>Đăng xuất
                      </button>
                    </li>
                  </ul>
                </li>
              ) : (
                <>
                  <li className="nav-item">
                    <Link to="/login" className="btn btn-outline-light btn-sm">Đăng nhập</Link>
                  </li>
                  <li className="nav-item">
                    <Link to="/register" className="btn btn-sm" style={{ background: 'var(--accent)', color: '#fff' }}>Đăng ký</Link>
                  </li>
                </>
              )}
            </ul>
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="flex-grow-1">
        <Outlet />
      </main>

      {/* Footer */}
      <footer className="py-4 text-center" style={{ background: 'var(--bg-card)', borderTop: '1px solid var(--border-color)', color: 'var(--text-muted)' }}>
        <div className="container">
          <p className="mb-1">© 2024 BarberShop. Tất cả quyền được bảo lưu.</p>
          <div className="d-flex justify-content-center gap-3">
            <a href="#" className="text-muted"><i className="fab fa-facebook"></i></a>
            <a href="#" className="text-muted"><i className="fab fa-instagram"></i></a>
            <a href="#" className="text-muted"><i className="fab fa-tiktok"></i></a>
          </div>
        </div>
      </footer>
    </div>
  );
}
