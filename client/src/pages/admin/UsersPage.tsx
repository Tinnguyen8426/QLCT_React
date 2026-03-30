import { useEffect, useState } from 'react';
import api from '../../api/client';
import Modal from '../../components/Modal';
import { useToast, ToastContainer } from '../../components/Toast';

const emptyForm = { fullName: '', email: '', phone: '', roleId: 0, password: '', isActive: true };

export default function AdminUsersPage() {
  const [data, setData] = useState<any>({ items: [], total: 0 });
  const [page, setPage] = useState(1);
  const [roles, setRoles] = useState<any[]>([]);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<any>(null);
  const [form, setForm] = useState({ ...emptyForm });
  const [saving, setSaving] = useState(false);
  const { toasts, show } = useToast();

  const load = () => { api.get('/api/admin/users', { params: { page } }).then(r => setData(r.data)); };
  useEffect(() => { load(); }, [page]);
  useEffect(() => { api.get('/api/admin/roles').then(r => setRoles(r.data)); }, []);

  const openCreate = () => {
    setEditing(null);
    setForm({ ...emptyForm, roleId: roles.length > 0 ? roles[0].roleId : 0 });
    setModalOpen(true);
  };
  const openEdit = (u: any) => {
    setEditing(u);
    const rId = roles.find(r => r.roleName === u.role)?.roleId || 0;
    setForm({ fullName: u.fullName || '', email: u.email || '', phone: u.phone || '', roleId: rId, password: '', isActive: u.isActive ?? true });
    setModalOpen(true);
  };
  const close = () => { setModalOpen(false); setEditing(null); };

  const save = async () => {
    if (!form.fullName.trim() || !form.email.trim()) { show('Vui lòng nhập họ tên và email', 'error'); return; }
    if (!editing && !form.password) { show('Vui lòng nhập mật khẩu cho tài khoản mới', 'error'); return; }
    setSaving(true);
    try {
      const payload = { ...form, password: form.password || undefined };
      if (editing?.userId) {
        await api.put(`/api/admin/users/${editing.userId}`, payload);
        show('Cập nhật tài khoản thành công');
      } else {
        await api.post('/api/admin/users', payload);
        show('Tạo tài khoản thành công');
      }
      close(); load();
    } catch (err: any) {
      show(err.response?.data?.message || 'Có lỗi xảy ra', 'error');
    } finally { setSaving(false); }
  };

  const toggle = async (id: number) => {
    if (!confirm('Vô hiệu hoá tài khoản này?')) return;
    await api.delete(`/api/admin/users/${id}`);
    show('Đã vô hiệu hoá tài khoản'); load();
  };

  return (
    <>
      <ToastContainer toasts={toasts} />
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold mb-0">Quản Lý Tài Khoản</h2>
        <button className="btn btn-accent btn-sm" onClick={openCreate}><i className="fas fa-plus me-1"></i>Thêm tài khoản</button>
      </div>

      <Modal isOpen={modalOpen} onClose={close} title={editing ? 'Sửa tài khoản' : 'Tạo tài khoản mới'}>
        <div className="row g-3">
          <div className="col-12">
            <label className="form-label small text-secondary">Họ tên <span className="text-danger">*</span></label>
            <input className="form-control" value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Email <span className="text-danger">*</span></label>
            <input type="email" className="form-control" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Điện thoại</label>
            <input className="form-control" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Vai trò <span className="text-danger">*</span></label>
            <select className="form-select" value={form.roleId} onChange={e => setForm({ ...form, roleId: +e.target.value })}>
              {roles.map(r => <option key={r.roleId} value={r.roleId}>{r.roleName}</option>)}
            </select>
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Mật khẩu {!editing && <span className="text-danger">*</span>}</label>
            <input type="password" className="form-control" placeholder={editing ? 'Bỏ trống để giữ nguyên' : ''} value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} />
          </div>
        </div>
        <div className="mt-4 d-flex gap-2 justify-content-end" style={{ borderTop: '1px solid var(--border-color)', paddingTop: 16 }}>
          <button className="btn btn-outline-secondary btn-sm" onClick={close}>Huỷ</button>
          <button className="btn btn-accent btn-sm" onClick={save} disabled={saving}>
            {saving ? <span className="spinner-border spinner-border-sm me-1"></span> : <i className="fas fa-save me-1"></i>}
            {editing ? 'Cập nhật' : 'Tạo mới'}
          </button>
        </div>
      </Modal>

      <div className="card-dark overflow-hidden">
        <div className="table-responsive">
          <table className="table table-dark-custom mb-0">
            <thead><tr><th>#</th><th>Họ tên</th><th>Email</th><th>Điện thoại</th><th>Vai trò</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
            <tbody>
              {data.items.length === 0 && <tr><td colSpan={7} className="text-center text-secondary py-4"><i className="fas fa-user-cog fa-2x mb-2 d-block"></i>Chưa có tài khoản nào</td></tr>}
              {data.items.map((u: any) => (
                <tr key={u.userId}>
                  <td>{u.userId}</td>
                  <td className="fw-bold">{u.fullName}</td>
                  <td>{u.email}</td>
                  <td>{u.phone || '—'}</td>
                  <td><span className="badge" style={{ background: u.role === 'Admin' ? '#c9a84c22' : '#17a2b822', color: u.role === 'Admin' ? '#c9a84c' : '#17a2b8' }}>{u.role}</span></td>
                  <td>{u.isActive ? <span className="badge bg-success">Hoạt động</span> : <span className="badge bg-danger">Vô hiệu</span>}</td>
                  <td>
                    <button className="btn btn-outline-warning btn-sm me-1" title="Sửa" onClick={() => openEdit(u)}><i className="fas fa-edit"></i></button>
                    {u.isActive && <button className="btn btn-outline-danger btn-sm" title="Vô hiệu hoá" onClick={() => toggle(u.userId)}><i className="fas fa-ban"></i></button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      <div className="d-flex justify-content-between mt-3">
        <small className="text-secondary">Tổng: {data.total}</small>
        <div className="d-flex gap-2">
          <button className="btn btn-outline-secondary btn-sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Trước</button>
          <button className="btn btn-outline-secondary btn-sm" disabled={data.items.length < 15} onClick={() => setPage(p => p + 1)}>Sau →</button>
        </div>
      </div>
    </>
  );
}
