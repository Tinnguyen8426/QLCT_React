import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';

export default function RegisterPage() {
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', password: '', confirmPassword: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const { register } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true); setErrors({});
    const result = await register(form);
    setLoading(false);
    if (result.success) {
      navigate('/login');
    } else if (result.errors) {
      setErrors(result.errors);
    }
  };

  const f = (name: string) => ({
    value: (form as any)[name],
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => setForm({ ...form, [name]: e.target.value }),
    className: `form-control ${errors[name] ? 'is-invalid' : ''}`
  });

  return (
    <div className="container py-5 d-flex justify-content-center">
      <div className="card-dark p-5 animate-in" style={{ maxWidth: 480, width: '100%' }}>
        <div className="text-center mb-4">
          <i className="fas fa-user-plus fa-2x mb-2" style={{color:'var(--accent)'}}></i>
          <h3 className="fw-bold">Đăng ký tài khoản</h3>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <label className="form-label small text-secondary">Họ tên *</label>
            <input {...f('fullName')} required />
            {errors.fullName && <div className="invalid-feedback">{errors.fullName}</div>}
          </div>
          <div className="mb-3">
            <label className="form-label small text-secondary">Email *</label>
            <input type="email" {...f('email')} required />
            {errors.email && <div className="invalid-feedback">{errors.email}</div>}
          </div>
          <div className="mb-3">
            <label className="form-label small text-secondary">Số điện thoại *</label>
            <input {...f('phone')} required />
            {errors.phone && <div className="invalid-feedback">{errors.phone}</div>}
          </div>
          <div className="mb-3">
            <label className="form-label small text-secondary">Mật khẩu *</label>
            <input type="password" {...f('password')} required />
            {errors.password && <div className="invalid-feedback">{errors.password}</div>}
          </div>
          <div className="mb-3">
            <label className="form-label small text-secondary">Xác nhận mật khẩu *</label>
            <input type="password" {...f('confirmPassword')} required />
            {errors.confirmPassword && <div className="invalid-feedback">{errors.confirmPassword}</div>}
          </div>
          <button type="submit" className="btn btn-accent w-100 mb-3" disabled={loading}>
            {loading ? <span className="spinner-border spinner-border-sm me-2"></span> : <i className="fas fa-user-plus me-2"></i>}
            Đăng ký
          </button>
        </form>

        <p className="text-center mb-0 small">Đã có tài khoản? <Link to="/login" style={{color:'var(--accent)'}}>Đăng nhập</Link></p>
      </div>
    </div>
  );
}
