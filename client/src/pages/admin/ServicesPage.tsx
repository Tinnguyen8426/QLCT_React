import { useEffect, useState } from 'react';
import api from '../../api/client';
import Modal from '../../components/Modal';
import ImageUpload from '../../components/ImageUpload';
import { useToast, ToastContainer } from '../../components/Toast';

const emptyForm = { serviceName: '', description: '', price: 0, duration: 0, category: '', imageUrl: '' };

export default function AdminServicesPage() {
  const [services, setServices] = useState<any[]>([]);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<any>(null);
  const [form, setForm] = useState({ ...emptyForm });
  const [saving, setSaving] = useState(false);
  const { toasts, show } = useToast();

  const load = () => { api.get('/api/admin/services').then(r => setServices(r.data)); };
  useEffect(() => { load(); }, []);

  const openCreate = () => { setEditing(null); setForm({ ...emptyForm }); setModalOpen(true); };
  const openEdit = (s: any) => {
    setEditing(s);
    setForm({ serviceName: s.serviceName, description: s.description || '', price: s.price, duration: s.duration || 0, category: s.category || '', imageUrl: s.imageUrl || '' });
    setModalOpen(true);
  };
  const close = () => { setModalOpen(false); setEditing(null); };

  const save = async () => {
    if (!form.serviceName.trim()) { show('Vui lòng nhập tên dịch vụ', 'error'); return; }
    setSaving(true);
    try {
      if (editing?.serviceId) {
        await api.put(`/api/admin/services/${editing.serviceId}`, form);
        show('Cập nhật dịch vụ thành công');
      } else {
        await api.post('/api/admin/services', form);
        show('Thêm dịch vụ thành công');
      }
      close(); load();
    } catch { show('Có lỗi xảy ra', 'error'); }
    finally { setSaving(false); }
  };

  const del = async (id: number) => {
    if (!confirm('Ẩn dịch vụ này?')) return;
    await api.delete(`/api/admin/services/${id}`);
    show('Đã ẩn dịch vụ');
    load();
  };

  return (
    <>
      <ToastContainer toasts={toasts} />
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold mb-0">Quản Lý Dịch Vụ</h2>
        <button className="btn btn-accent btn-sm" onClick={openCreate}>
          <i className="fas fa-plus me-1"></i>Thêm dịch vụ
        </button>
      </div>

      {/* Modal Form */}
      <Modal isOpen={modalOpen} onClose={close} title={editing ? 'Sửa dịch vụ' : 'Thêm dịch vụ mới'} size="lg">
        <div className="row g-3">
          <div className="col-12">
            <label className="form-label small text-secondary">Tên dịch vụ <span className="text-danger">*</span></label>
            <input className="form-control" value={form.serviceName} onChange={e => setForm({ ...form, serviceName: e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Giá (VNĐ) <span className="text-danger">*</span></label>
            <input type="number" className="form-control" value={form.price} onChange={e => setForm({ ...form, price: +e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Thời gian (phút)</label>
            <input type="number" className="form-control" value={form.duration} onChange={e => setForm({ ...form, duration: +e.target.value })} />
          </div>
          <div className="col-12">
            <label className="form-label small text-secondary">Mô tả</label>
            <textarea className="form-control" rows={3} value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} />
          </div>
          <div className="col-md-6">
            <label className="form-label small text-secondary">Danh mục</label>
            <input className="form-control" value={form.category} onChange={e => setForm({ ...form, category: e.target.value })} />
          </div>
          <div className="col-md-6">
            <ImageUpload label="Hình ảnh" value={form.imageUrl} onChange={url => setForm({ ...form, imageUrl: url })} />
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

      {/* Table */}
      <div className="card-dark overflow-hidden">
        <div className="table-responsive">
          <table className="table table-dark-custom mb-0">
            <thead><tr><th>#</th><th>Tên</th><th>Danh mục</th><th>Giá</th><th>Thời gian</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
            <tbody>
              {services.length === 0 && <tr><td colSpan={7} className="text-center text-secondary py-4"><i className="fas fa-inbox fa-2x mb-2 d-block"></i>Chưa có dịch vụ nào</td></tr>}
              {services.map(s => (
                <tr key={s.serviceId}>
                  <td>{s.serviceId}</td>
                  <td className="fw-bold">{s.serviceName}</td>
                  <td>{s.category || '—'}</td>
                  <td>{s.price.toLocaleString('vi-VN')}đ</td>
                  <td>{s.duration ? `${s.duration} phút` : '—'}</td>
                  <td>{s.isActive !== false ? <span className="badge bg-success">Hoạt động</span> : <span className="badge bg-secondary">Ẩn</span>}</td>
                  <td>
                    <button className="btn btn-outline-warning btn-sm me-1" title="Sửa" onClick={() => openEdit(s)}><i className="fas fa-edit"></i></button>
                    <button className="btn btn-outline-danger btn-sm" title="Ẩn" onClick={() => del(s.serviceId)}><i className="fas fa-eye-slash"></i></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
