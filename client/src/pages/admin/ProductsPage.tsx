import { useEffect, useState } from 'react';
import api from '../../api/client';
import Modal from '../../components/Modal';
import ImageUpload from '../../components/ImageUpload';
import { useToast, ToastContainer } from '../../components/Toast';

const emptyForm = { productName: '', description: '', price: 0, category: '', brand: '', imageUrl: '', stockQuantity: 0, unit: '', costPrice: 0 };

export default function AdminProductsPage() {
  const [data, setData] = useState<any>({ items: [], total: 0 });
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<any>(null);
  const [form, setForm] = useState({ ...emptyForm });
  const [saving, setSaving] = useState(false);
  const { toasts, show } = useToast();

  const load = () => { api.get('/api/admin/products', { params: { search: search || undefined, page } }).then(r => setData(r.data)); };
  useEffect(() => { load(); }, [page, search]);

  const openCreate = () => { setEditing(null); setForm({ ...emptyForm }); setModalOpen(true); };
  const openEdit = (p: any) => {
    setEditing(p);
    setForm({
      productName: p.productName || '', description: p.description || '', price: p.price || 0,
      category: p.category || '', brand: p.brand || '', imageUrl: p.imageUrl || '',
      stockQuantity: p.stockQuantity || 0, unit: p.unit || '', costPrice: p.costPrice || 0
    });
    setModalOpen(true);
  };
  const close = () => { setModalOpen(false); setEditing(null); };

  const save = async () => {
    if (!form.productName.trim()) { show('Vui lòng nhập tên sản phẩm', 'error'); return; }
    setSaving(true);
    try {
      if (editing?.productId) {
        await api.put(`/api/admin/products/${editing.productId}`, form);
        show('Cập nhật sản phẩm thành công');
      } else {
        await api.post('/api/admin/products', form);
        show('Thêm sản phẩm thành công');
      }
      close(); load();
    } catch { show('Có lỗi xảy ra', 'error'); }
    finally { setSaving(false); }
  };

  const del = async (id: number) => {
    if (!confirm('Ẩn sản phẩm này?')) return;
    await api.delete(`/api/admin/products/${id}`);
    show('Đã ẩn sản phẩm'); load();
  };

  return (
    <>
      <ToastContainer toasts={toasts} />
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold mb-0">Quản Lý Sản Phẩm</h2>
        <div className="d-flex gap-2">
          <input className="form-control form-control-sm" style={{ width: 220 }} placeholder="🔍 Tìm kiếm..." value={search} onChange={e => setSearch(e.target.value)} />
          <button className="btn btn-accent btn-sm" onClick={openCreate}><i className="fas fa-plus me-1"></i>Thêm</button>
        </div>
      </div>

      <Modal isOpen={modalOpen} onClose={close} title={editing ? 'Sửa sản phẩm' : 'Thêm sản phẩm mới'} size="lg">
        <div className="row g-3">
          <div className="col-12">
            <label className="form-label small text-secondary">Tên sản phẩm <span className="text-danger">*</span></label>
            <input className="form-control" value={form.productName} onChange={e => setForm({ ...form, productName: e.target.value })} />
          </div>
          <div className="col-md-4">
            <label className="form-label small text-secondary">Giá bán (VNĐ) <span className="text-danger">*</span></label>
            <input type="number" className="form-control" value={form.price} onChange={e => setForm({ ...form, price: +e.target.value })} />
          </div>
          <div className="col-md-4">
            <label className="form-label small text-secondary">Giá vốn</label>
            <input type="number" className="form-control" value={form.costPrice} onChange={e => setForm({ ...form, costPrice: +e.target.value })} />
          </div>
          <div className="col-md-4">
            <label className="form-label small text-secondary">Tồn kho <span className="text-danger">*</span></label>
            <input type="number" className="form-control" value={form.stockQuantity} onChange={e => setForm({ ...form, stockQuantity: +e.target.value })} />
          </div>
          <div className="col-md-4">
            <label className="form-label small text-secondary">Danh mục</label>
            <input className="form-control" value={form.category} onChange={e => setForm({ ...form, category: e.target.value })} />
          </div>
          <div className="col-md-4">
            <label className="form-label small text-secondary">Thương hiệu</label>
            <input className="form-control" value={form.brand} onChange={e => setForm({ ...form, brand: e.target.value })} />
          </div>
          <div className="col-md-4">
            <label className="form-label small text-secondary">Đơn vị</label>
            <input className="form-control" placeholder="chai, hộp, tuýp..." value={form.unit} onChange={e => setForm({ ...form, unit: e.target.value })} />
          </div>
          <div className="col-12">
            <label className="form-label small text-secondary">Mô tả</label>
            <textarea className="form-control" rows={2} value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} />
          </div>
          <div className="col-12">
            <ImageUpload label="Hình ảnh sản phẩm" value={form.imageUrl} onChange={url => setForm({ ...form, imageUrl: url })} />
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
            <thead><tr><th>#</th><th>Tên</th><th>Danh mục</th><th>Thương hiệu</th><th>Giá</th><th>Tồn kho</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
            <tbody>
              {data.items.length === 0 && <tr><td colSpan={8} className="text-center text-secondary py-4"><i className="fas fa-box-open fa-2x mb-2 d-block"></i>Chưa có sản phẩm nào</td></tr>}
              {data.items.map((p: any) => (
                <tr key={p.productId}>
                  <td>{p.productId}</td>
                  <td className="fw-bold">{p.productName}</td>
                  <td>{p.category || '—'}</td>
                  <td>{p.brand || '—'}</td>
                  <td>{p.price.toLocaleString('vi-VN')}đ</td>
                  <td>{p.stockQuantity <= 5 ? <span className="text-warning fw-bold">{p.stockQuantity}</span> : p.stockQuantity}</td>
                  <td>{p.isActive !== false ? <span className="badge bg-success">Hoạt động</span> : <span className="badge bg-secondary">Ẩn</span>}</td>
                  <td>
                    <button className="btn btn-outline-warning btn-sm me-1" title="Sửa" onClick={() => openEdit(p)}><i className="fas fa-edit"></i></button>
                    <button className="btn btn-outline-danger btn-sm" title="Ẩn" onClick={() => del(p.productId)}><i className="fas fa-eye-slash"></i></button>
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
