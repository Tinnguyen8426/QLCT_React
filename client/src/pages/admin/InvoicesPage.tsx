import { useEffect, useState } from 'react';
import api from '../../api/client';
import Modal from '../../components/Modal';

export default function AdminInvoicesPage() {
  const [data, setData] = useState<any>({ items: [], total: 0 });
  const [page, setPage] = useState(1);
  const [detail, setDetail] = useState<any>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  useEffect(() => { api.get('/api/admin/invoices', { params: { page } }).then(r => setData(r.data)); }, [page]);

  const viewDetail = async (id: number) => {
    const r = await api.get(`/api/admin/invoices/${id}`);
    setDetail(r.data);
    setDetailOpen(true);
  };

  return (
    <>
      <h2 className="fw-bold mb-4">Quản Lý Hoá Đơn</h2>

      {/* Detail Modal */}
      <Modal isOpen={detailOpen} onClose={() => setDetailOpen(false)} title={`Hoá đơn #${detail?.invoiceId || ''}`} size="lg">
        {detail && (
          <div>
            <div className="row g-3 mb-3">
              <div className="col-md-6">
                <label className="form-label small text-secondary">Khách hàng</label>
                <div className="fw-bold">{detail.customerName || '—'}</div>
              </div>
              <div className="col-md-6">
                <label className="form-label small text-secondary">Nhân viên</label>
                <div className="fw-bold">{detail.staffName || '—'}</div>
              </div>
              <div className="col-md-4">
                <label className="form-label small text-secondary">Tổng</label>
                <div>{(detail.total || 0).toLocaleString('vi-VN')}đ</div>
              </div>
              <div className="col-md-4">
                <label className="form-label small text-secondary">Giảm giá</label>
                <div>{(detail.discount || 0).toLocaleString('vi-VN')}đ</div>
              </div>
              <div className="col-md-4">
                <label className="form-label small text-secondary">Thanh toán</label>
                <div className="fw-bold" style={{ color: 'var(--accent)' }}>{(detail.finalAmount || 0).toLocaleString('vi-VN')}đ</div>
              </div>
              <div className="col-md-6">
                <label className="form-label small text-secondary">Hình thức thanh toán</label>
                <div>{detail.paymentMethod || '—'}</div>
              </div>
              <div className="col-md-6">
                <label className="form-label small text-secondary">Ngày tạo</label>
                <div>{detail.createdAt ? new Date(detail.createdAt).toLocaleString('vi-VN') : '—'}</div>
              </div>
            </div>
            <h6 className="fw-bold mt-3 mb-2">Chi tiết hoá đơn</h6>
            <table className="table table-dark-custom mb-0">
              <thead><tr><th>Loại</th><th>Tên</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead>
              <tbody>
                {(detail.details || []).map((d: any, i: number) => (
                  <tr key={i}>
                    <td>{d.serviceName ? 'Dịch vụ' : 'Sản phẩm'}</td>
                    <td className="fw-bold">{d.serviceName || d.productName || '—'}</td>
                    <td>{d.quantity}</td>
                    <td>{(d.unitPrice || 0).toLocaleString('vi-VN')}đ</td>
                    <td style={{ color: 'var(--accent)' }}>{(d.subtotal || 0).toLocaleString('vi-VN')}đ</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Modal>

      <div className="card-dark overflow-hidden">
        <div className="table-responsive">
          <table className="table table-dark-custom mb-0">
            <thead><tr><th>#</th><th>Khách hàng</th><th>Nhân viên</th><th>Tổng</th><th>Giảm giá</th><th>Thanh toán</th><th>HTTT</th><th>Ngày</th><th></th></tr></thead>
            <tbody>
              {data.items.length === 0 && <tr><td colSpan={9} className="text-center text-secondary py-4"><i className="fas fa-file-invoice fa-2x mb-2 d-block"></i>Chưa có hoá đơn nào</td></tr>}
              {data.items.map((i: any) => (
                <tr key={i.invoiceId}>
                  <td>{i.invoiceId}</td>
                  <td>{i.customerName || '—'}</td>
                  <td>{i.staffName || '—'}</td>
                  <td>{(i.total || 0).toLocaleString('vi-VN')}đ</td>
                  <td>{(i.discount || 0).toLocaleString('vi-VN')}đ</td>
                  <td style={{ color: 'var(--accent)' }}>{(i.finalAmount || 0).toLocaleString('vi-VN')}đ</td>
                  <td>{i.paymentMethod || '—'}</td>
                  <td>{i.createdAt ? new Date(i.createdAt).toLocaleDateString('vi-VN') : '—'}</td>
                  <td><button className="btn btn-outline-info btn-sm" title="Xem chi tiết" onClick={() => viewDetail(i.invoiceId)}><i className="fas fa-eye"></i></button></td>
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
