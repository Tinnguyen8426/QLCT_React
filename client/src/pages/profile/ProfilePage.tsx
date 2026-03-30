import { useEffect, useState } from 'react';
import api from '../../api/client';
import { useAuthStore } from '../../stores/authStore';

export default function ProfilePage() {
  const { user, loadUser } = useAuthStore();
  const [form, setForm] = useState({ fullName: '', phone: '' });
  const [pwForm, setPwForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const [msg, setMsg] = useState('');

  useEffect(() => {
    if (user) setForm({ fullName: user.fullName, phone: user.phone || '' });
  }, [user]);

  const saveProfile = async () => {
    await api.put('/api/profile', form);
    await loadUser();
    setMsg('Cập nhật thành công!');
    setTimeout(() => setMsg(''), 3000);
  };

  const changePassword = async () => {
    try {
      await api.put('/api/profile/password', pwForm);
      setMsg('Đổi mật khẩu thành công!');
      setPwForm({ currentPassword: '', newPassword: '', confirmPassword: '' });
    } catch (err: any) {
      setMsg(err.response?.data?.message || 'Lỗi đổi mật khẩu.');
    }
  };

  return (
    <div className="container py-5">
      <h2 className="fw-bold mb-4"><i className="fas fa-user me-2" style={{ color: 'var(--accent)' }}></i>Hồ Sơ Cá Nhân</h2>

      {msg && <div className="alert alert-success animate-in">{msg}</div>}

      <div className="row g-4">
        <div className="col-lg-6">
          <div className="card-dark p-4">
            <h5 className="fw-bold mb-3">Thông tin cá nhân</h5>
            <div className="mb-3">
              <label className="form-label small text-secondary">Email</label>
              <input className="form-control" value={user?.email || ''} disabled />
            </div>
            <div className="mb-3">
              <label className="form-label small text-secondary">Họ tên</label>
              <input className="form-control" value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} />
            </div>
            <div className="mb-3">
              <label className="form-label small text-secondary">Số điện thoại</label>
              <input className="form-control" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} />
            </div>
            <button className="btn btn-accent" onClick={saveProfile}><i className="fas fa-save me-1"></i>Lưu thay đổi</button>
          </div>
        </div>
        <div className="col-lg-6">
          <div className="card-dark p-4">
            <h5 className="fw-bold mb-3">Đổi mật khẩu</h5>
            <div className="mb-3">
              <label className="form-label small text-secondary">Mật khẩu hiện tại</label>
              <input type="password" className="form-control" value={pwForm.currentPassword} onChange={e => setPwForm({ ...pwForm, currentPassword: e.target.value })} />
            </div>
            <div className="mb-3">
              <label className="form-label small text-secondary">Mật khẩu mới</label>
              <input type="password" className="form-control" value={pwForm.newPassword} onChange={e => setPwForm({ ...pwForm, newPassword: e.target.value })} />
            </div>
            <div className="mb-3">
              <label className="form-label small text-secondary">Xác nhận mật khẩu mới</label>
              <input type="password" className="form-control" value={pwForm.confirmPassword} onChange={e => setPwForm({ ...pwForm, confirmPassword: e.target.value })} />
            </div>
            <button className="btn btn-accent" onClick={changePassword}><i className="fas fa-key me-1"></i>Đổi mật khẩu</button>
          </div>
        </div>
      </div>
    </div>
  );
}
