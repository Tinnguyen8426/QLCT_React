import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true); setError('');
    const result = await login(email, password);
    setLoading(false);
    if (result.success) {
      const u = useAuthStore.getState().user;
      if (u?.role === 'Admin' || u?.role === 'Administrator') navigate('/admin');
      else if (u?.role === 'Staff') navigate('/staff');
      else navigate('/');
    } else {
      setError(result.error || 'Đăng nhập thất bại.');
    }
  };

  return (
    <div className="container py-5 d-flex justify-content-center">
      <div className="card-dark p-5 animate-in" style={{ maxWidth: 440, width: '100%' }}>
        <div className="text-center mb-4">
          <i className="fas fa-cut fa-2x mb-2" style={{ color: 'var(--accent)' }}></i>
          <h3 className="fw-bold">Đăng nhập</h3>
          <p className="text-secondary small">Chào mừng bạn quay lại!</p>
        </div>

        {error && <div className="alert alert-danger">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label className="form-label small text-secondary">Email</label>
            <input type="email" className="form-control" value={email} onChange={e => setEmail(e.target.value)} required />
          </div>
          <div className="mb-3">
            <label className="form-label small text-secondary">Mật khẩu</label>
            <input type="password" className="form-control" value={password} onChange={e => setPassword(e.target.value)} required />
          </div>
          <button type="submit" className="btn btn-accent w-100 mb-3" disabled={loading}>
            {loading ? <span className="spinner-border spinner-border-sm me-2"></span> : <i className="fas fa-sign-in-alt me-2"></i>}
            Đăng nhập
          </button>
        </form>

        <div className="text-center">
          <Link to="/forgot-password" className="small text-secondary">Quên mật khẩu?</Link>
          <hr style={{ borderColor: 'var(--border-color)' }} />
          <p className="mb-0 small">Chưa có tài khoản? <Link to="/register" style={{ color: 'var(--accent)' }}>Đăng ký ngay</Link></p>
        </div>
      </div>
    </div>
  );
}
