import { useEffect, useState } from 'react';
import api from '../../api/client';
import Modal from '../../components/Modal';

export default function AdminAppointmentsPage() {
  const [data, setData] = useState<any>({ items: [], total: 0 });
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [detail, setDetail] = useState<any>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  const load = () => {
    api.get('/api/admin/appointments', { params: { page, status: statusFilter || undefined } })
      .then(r => setData(r.data));
  };
  useEffect(load, [page, statusFilter]);

  const updateStatus = async (id: number, status: string) => {
    await api.put(`/api/admin/appointments/${id}/status`, { status });
    load();
    if (detail?.appointmentId === id) viewDetail(id);
  };

  const viewDetail = async (id: number) => {
    const r = await api.get(`/api/admin/appointments/${id}`);
    setDetail(r.data);
    setDetailOpen(true);
  };

  const statuses = ['Pending', 'Confirmed', 'Completed', 'Cancelled', 'No-show'];

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="fw-bold mb-0">Quản Lý Lịch Hẹn</h2>
        <select className="form-select form-select-sm" style={{ width: 'auto' }} value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
          <option value="">Tất cả trạng thái</option>
          {statuses.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      {/* Detail Modal */}
      <Modal isOpen={detailOpen} onClose={() => setDetailOpen(false)} title={`Chi tiết lịch hẹn #${detail?.appointmentId || ''}`} size="lg">
        {detail && (
          <div>
            <div className="row g-3 mb-3">
              <div className="col-md-6">
                <label className="form-label small text-secondary">Khách hàng</label>
                <div className="fw-bold">{detail.customerName || '—'}</div>
                <small className="text-secondary">{detail.customerPhone || ''}</small>
              </div>
              <div className="col-md-6">
                <label className="form-label small text-secondary">Nhân viên</label>
                <div className="fw-bold">{detail.staffName || '—'}</div>
              </div>
              <div className="col-md-4">
                <label className="form-label small text-secondary">Ngày</label>
                <div>{detail.date}</div>
              </div>
              <div className="col-md-4">
                <label className="form-label small text-secondary">Giờ</label>
                <div>{detail.time}</div>
              </div>
              <div className="col-md-4">
                <label className="form-label small text-secondary">Trạng thái</label>
                <div><span className={`badge-status badge-${(detail.status || 'pending').toLowerCase()}`}>{detail.status || 'Pending'}</span></div>
              </div>
              {detail.note && (
                <div className="col-12">
                  <label className="form-label small text-secondary">Ghi chú</label>
                  <div>{detail.note}</div>
                </div>
              )}
            </div>
            <h6 className="fw-bold mt-3 mb-2">Dịch vụ</h6>
            <table className="table table-dark-custom mb-0">
              <thead><tr><th>Tên</th><th>Đơn giá</th><th>SL</th></tr></thead>
              <tbody>
                {(detail.services || []).map((s: any, i: number) => (
                  <tr key={i}>
                    <td>{s.serviceName}</td>
                    <td>{(s.unitPrice || 0).toLocaleString('vi-VN')}đ</td>
                    <td>{s.quantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {detail.hasInvoice && <div className="mt-2"><span className="badge bg-success"><i className="fas fa-check me-1"></i>Đã có hoá đơn</span></div>}
          </div>
        )}
      </Modal>

      <div className="card-dark overflow-hidden">
        <div className="table-responsive">
          <table className="table table-dark-custom mb-0">
            <thead><tr><th>#</th><th>Khách hàng</th><th>Nhân viên</th><th>Ngày</th><th>Giờ</th><th>Dịch vụ</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
            <tbody>
              {data.items.length === 0 && <tr><td colSpan={8} className="text-center text-secondary py-4"><i className="fas fa-calendar-alt fa-2x mb-2 d-block"></i>Không có lịch hẹn nào</td></tr>}
              {data.items.map((a: any) => (
                <tr key={a.appointmentId}>
                  <td>{a.appointmentId}</td>
                  <td>{a.customerName || '—'}</td>
                  <td>{a.staffName || '—'}</td>
                  <td>{a.date}</td>
                  <td>{a.time}</td>
                  <td><small>{(a.services || []).join(', ')}</small></td>
                  <td><span className={`badge-status badge-${(a.status || 'pending').toLowerCase()}`}>{a.status || 'Pending'}</span></td>
                  <td className="d-flex gap-1">
                    <button className="btn btn-outline-info btn-sm" title="Xem chi tiết" onClick={() => viewDetail(a.appointmentId)}><i className="fas fa-eye"></i></button>
                    <select className="form-select form-select-sm" style={{ width: 120 }} value={a.status || 'Pending'} onChange={e => updateStatus(a.appointmentId, e.target.value)}>
                      {statuses.map(s => <option key={s} value={s}>{s}</option>)}
                    </select>
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
