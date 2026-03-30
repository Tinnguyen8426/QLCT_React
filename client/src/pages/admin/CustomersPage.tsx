import { useEffect, useState } from 'react';
import api from '../../api/client';
import Modal from '../../components/Modal';
import { useToast, ToastContainer } from '../../components/Toast';

const emptyForm = { fullName: '', phone: '', email: '', gender: '', birthday: '', note: '' };

export default function AdminCustomersPage() {
  const [data, setData] = useState<any>({ items: [], total: 0 });
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<any>(null);
  const [form, setForm] = useState({ ...emptyForm });
  const [saving, setSaving] = useState(false);
  const { toasts, show } = useToast();

  const load = () => { api.get('/api/admin/customers', { params: { search: search || undefined, page } }).then(r => setData(r.data)); };
  useEffect(() => { load(); }, [page, search]);

  const openCreate = () => { setEditing(null); setForm({ ...emptyForm }); setModalOpen(true); };
  const openEdit = (c: any) => {
    setEditing(c);
    setForm({
      fullName: c.fullName || '', phone: c.phone || '', email: c.email || '',
      gender: c.gender || '', birthday: c.birthday ? c.birthday.substring(0, 10) : '', note: c.note || ''
    });
    setModalOpen(true);
  };
  const close = () => { setModalOpen(false); setEditing(null); };

  const save = async () => {
    if (!form.fullName.trim() || !form.phone.trim()) { show('Vui lòng nhập họ tên và số điện thoại', 'error'); return; }
    setSaving(true);
    try {
      const payload = { ...form, birthday: form.birthday || null };
      if (editing?.customerId) {
        await api.put(`/api/admin/customers/${editing.customerId}`, payload);
        show('Cập nhật khách hàng thành công');
      } else {
        await api.post('/api/admin/customers', payload);
        show('Thêm khách hàng thành công');
      }
      close(); load();
    } catch { show('Có lỗi xảy ra', 'error'); }
    finally { setSaving(false); }
  };

  const remove = async (id: number) => {
    if (!confirm('Xoá khách hàng này?')) return;
    await api.delete(`/api/admin/customers/${id}`);
    show('Đã xoá khách hàng'); load();
  };

  return (
    <>
      <ToastContainer toasts={toasts} />
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold mb-0">Quản Lý Khách Hàng</h2>
        <div className="d-flex gap-2">
          <input className="form-control form-control-sm" style={{ width: 220 }} placeholder="🔍 Tìm kiếm..." value={search} onChange={e => setSearch(e.target.value)} />
          <button className="btn btn-accent btn-sm" onClick={openCreate}><i className="fas fa-plus me-1"></i>Thêm</button>
        </div>
      </div>

      <Modal isOpen={modalOpen} onClose={close} title={editing ? 'Sửa khách hàng' : 'Thêm khách hàng mới'}>
        <div className="row g-3">
          <div className="col-12">
            <label className="form-label small text-secondary">Họ tên <span className="text-danger">*</span></label>
            <input className="form-control" value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Điện thoại <span className="text-danger">*</span></label>
            <input className="form-control" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Email</label>
            <input type="email" className="form-control" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Giới tính</label>
            <select className="form-select" value={form.gender} onChange={e => setForm({ ...form, gender: e.target.value })}>
              <option value="">— Chọn —</option>
              <option value="Nam">Nam</option>
              <option value="Nữ">Nữ</option>
              <option value="Khác">Khác</option>
            </select>
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Ngày sinh</label>
            <input type="date" className="form-control" value={form.birthday} onChange={e => setForm({ ...form, birthday: e.target.value })} />
          </div>
          <div className="col-12">
            <label className="form-label small text-secondary">Ghi chú</label>
            <textarea className="form-control" rows={2} value={form.note} onChange={e => setForm({ ...form, note: e.target.value })} />
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
            <thead><tr><th>#</th><th>Họ tên</th><th>Điện thoại</th><th>Email</th><th>Giới tính</th><th>Ngày tạo</th><th>Thao tác</th></tr></thead>
            <tbody>
              {data.items.length === 0 && <tr><td colSpan={7} className="text-center text-secondary py-4"><i className="fas fa-users fa-2x mb-2 d-block"></i>Chưa có khách hàng nào</td></tr>}
              {data.items.map((c: any) => (
                <tr key={c.customerId}>
                  <td>{c.customerId}</td>
                  <td className="fw-bold">{c.fullName}</td>
                  <td>{c.phone}</td>
                  <td>{c.email || '—'}</td>
                  <td>{c.gender || '—'}</td>
                  <td>{c.createdAt ? new Date(c.createdAt).toLocaleDateString('vi-VN') : '—'}</td>
                  <td>
                    <button className="btn btn-outline-warning btn-sm me-1" title="Sửa" onClick={() => openEdit(c)}><i className="fas fa-edit"></i></button>
                    <button className="btn btn-outline-danger btn-sm" title="Xoá" onClick={() => remove(c.customerId)}><i className="fas fa-trash"></i></button>
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
