import { Link, Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';

const adminMenuItems = [
  { path: '/admin', icon: 'fas fa-tachometer-alt', label: 'Dashboard', exact: true },
  { path: '/admin/appointments', icon: 'fas fa-calendar-alt', label: 'Lịch hẹn' },
  { path: '/admin/customers', icon: 'fas fa-users', label: 'Khách hàng' },
  { path: '/admin/services', icon: 'fas fa-cut', label: 'Dịch vụ' },
  { path: '/admin/products', icon: 'fas fa-box', label: 'Sản phẩm' },
  { path: '/admin/invoices', icon: 'fas fa-file-invoice-dollar', label: 'Hoá đơn' },
  { path: '/admin/users', icon: 'fas fa-user-cog', label: 'Tài khoản' },
  { path: '/admin/reports', icon: 'fas fa-chart-bar', label: 'Báo cáo' },
];

const staffMenuItems = [
  { path: '/staff', icon: 'fas fa-tachometer-alt', label: 'Dashboard', exact: true },
  { path: '/staff/appointments', icon: 'fas fa-calendar-alt', label: 'Lịch hẹn' },
];

export default function AdminLayout() {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();
  const isAdmin = user?.role === 'Admin' || user?.role === 'Administrator';
  const menuItems = isAdmin ? adminMenuItems : staffMenuItems;

  return (
    <div className="d-flex" style={{ minHeight: '100vh', background: '#1a1d21' }}>
      {/* Sidebar */}
      <aside className="d-flex flex-column" style={{ width: 260, background: '#12151a', borderRight: '1px solid rgba(255,255,255,0.06)' }}>
        <div className="p-3 text-center" style={{ borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
          <Link to={isAdmin ? "/admin" : "/staff"} className="text-decoration-none">
            <h5 className="mb-0 text-white">
              <i className="fas fa-cut me-2" style={{ color: '#c9a84c' }}></i>{isAdmin ? 'Admin Panel' : 'Staff Panel'}
            </h5>
          </Link>
        </div>

        <nav className="flex-grow-1 py-3">
          {menuItems.map(item => (
            <NavLink
              key={item.path}
              to={item.path}
              end={item.exact}
              className={({ isActive }) =>
                `d-flex align-items-center gap-3 px-4 py-2 text-decoration-none transition-all ${isActive
                  ? 'text-white'
                  : 'text-secondary'
                }`
              }
              style={({ isActive }) => ({
                background: isActive ? 'rgba(201,168,76,0.12)' : 'transparent',
                borderLeft: isActive ? '3px solid #c9a84c' : '3px solid transparent',
                fontSize: '0.9rem'
              })}
            >
              <i className={item.icon} style={{ width: 20, textAlign: 'center' }}></i>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="p-3" style={{ borderTop: '1px solid rgba(255,255,255,0.06)' }}>
          <div className="d-flex align-items-center gap-2 mb-2 px-2">
            <i className="fas fa-user-circle text-secondary" style={{ fontSize: '1.5rem' }}></i>
            <div>
              <div className="text-white small fw-bold">{user?.fullName}</div>
              <div className="text-secondary" style={{ fontSize: '0.75rem' }}>{user?.role}</div>
            </div>
          </div>
          <button
            className="btn btn-outline-danger btn-sm w-100"
            onClick={() => { logout(); navigate('/login'); }}
          >
            <i className="fas fa-sign-out-alt me-1"></i> Đăng xuất
          </button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex-grow-1 d-flex flex-column">
        <header className="px-4 py-3 d-flex justify-content-between align-items-center"
          style={{ background: '#12151a', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
          <h6 className="mb-0 text-white">Quản Lý Tiệm Cắt Tóc</h6>
          <div className="d-flex gap-3">
            <Link to="/" className="btn btn-outline-secondary btn-sm">
              <i className="fas fa-external-link-alt me-1"></i> Xem trang chủ
            </Link>
          </div>
        </header>

        <main className="flex-grow-1 p-4" style={{ overflowY: 'auto', maxHeight: 'calc(100vh - 60px)' }}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
