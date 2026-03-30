import { useEffect, useState } from 'react';
import api from '../../api/client';
import Modal from '../../components/Modal';
import { useToast, ToastContainer } from '../../components/Toast';

export default function StaffAppointmentsPage() {
  const [data, setData] = useState<any>({ items: [], totalItems: 0, totalPages: 1, page: 1 });
  const [statusFilter, setStatusFilter] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [dateFilter, setDateFilter] = useState('');
  const [page, setPage] = useState(1);
  const [detail, setDetail] = useState<any>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const { toasts, show } = useToast();

  const loadAppointments = () => {
    api.get('/api/staff/appointments', {
      params: {
        page, pageSize: 10,
        status: statusFilter || undefined,
        searchTerm: searchTerm || undefined,
        date: dateFilter || undefined
      }
    }).then(r => setData(r.data)).catch(console.error);
  };

  useEffect(() => { loadAppointments(); }, [page, statusFilter, searchTerm, dateFilter]);

  const updateStatus = async (id: number, status: string) => {
    if (!confirm(`Xác nhận chuyển sang trạng thái: ${status}?`)) return;
    try {
      await api.put(`/api/staff/appointments/${id}/status`, { status });
      show('Đã cập nhật trạng thái');
      loadAppointments();
      if (detail?.appointmentId === id) viewDetail(id);
    } catch { show('Lỗi cập nhật trạng thái', 'error'); }
  };

  const viewDetail = async (id: number) => {
    try {
      const r = await api.get(`/api/staff/appointments/${id}`);
      setDetail(r.data);
      setDetailOpen(true);
    } catch { show('Không thể tải chi tiết', 'error'); }
  };

  const createInvoice = async (id: number) => {
    if (!confirm('Tạo hoá đơn cho lịch hẹn này?')) return;
    try {
      const r = await api.post(`/api/staff/appointments/${id}/invoice`, { paymentMethod: 'Tiền mặt' });
      show(`Hoá đơn #${r.data.invoiceId} đã được tạo — ${(r.data.finalAmount || 0).toLocaleString('vi-VN')}đ`);
      loadAppointments();
      if (detail) viewDetail(id);
    } catch (err: any) {
      show(err.response?.data?.message || 'Lỗi tạo hoá đơn', 'error');
    }
  };

  const statuses = ['Pending', 'Confirmed', 'Completed', 'Cancelled', 'No-show'];

  return (
    <>
      <ToastContainer toasts={toasts} />
      <h2 className="fw-bold mb-4">Quản Lý Lịch Hẹn</h2>

      {/* Status Overview */}
      {data.statusOverview && (
        <div className="d-flex gap-2 mb-3 flex-wrap">
          {Object.entries(data.statusOverview).map(([s, count]: any) => (
            <button key={s}
              className={`btn btn-sm ${statusFilter === s ? 'btn-accent' : 'btn-outline-secondary'}`}
              onClick={() => setStatusFilter(statusFilter === s ? '' : s)}>
              {s} <span className="badge bg-dark ms-1">{count}</span>
            </button>
          ))}
        </div>
      )}

      {/* Filters */}
      <div className="d-flex gap-2 mb-4 flex-wrap">
        <input className="form-control form-control-sm" style={{ width: 220 }} placeholder="🔍 Tìm khách hàng, dịch vụ..." value={searchTerm} onChange={e => { setSearchTerm(e.target.value); setPage(1); }} />
        <input type="date" className="form-control form-control-sm" style={{ width: 180 }} value={dateFilter} onChange={e => { setDateFilter(e.target.value); setPage(1); }} />
        {(searchTerm || dateFilter || statusFilter) && (
          <button className="btn btn-outline-secondary btn-sm" onClick={() => { setSearchTerm(''); setDateFilter(''); setStatusFilter(''); setPage(1); }}>
            <i className="fas fa-times me-1"></i>Xoá bộ lọc
          </button>
        )}
      </div>

      {/* Detail Modal */}
      <Modal isOpen={detailOpen} onClose={() => setDetailOpen(false)} title={`Chi tiết lịch hẹn #${detail?.appointmentId || ''}`} size="lg">
        {detail && (
          <div>
            <div className="row g-3 mb-3">
              <div className="col-md-6">
                <label className="form-label small text-secondary">Khách hàng</label>
                <div className="fw-bold">{detail.customer?.fullName || '—'}</div>
                <small className="text-secondary">{detail.customer?.phone || ''}</small>
              </div>
              <div className="col-md-3">
                <label className="form-label small text-secondary">Ngày</label>
                <div>{detail.appointmentDate}</div>
              </div>
              <div className="col-md-3">
                <label className="form-label small text-secondary">Giờ</label>
                <div>{detail.appointmentTime}</div>
              </div>
              <div className="col-md-6">
                <label className="form-label small text-secondary">Trạng thái</label>
                <div><span className={`badge-status badge-${(detail.status || 'pending').toLowerCase()}`}>{detail.status}</span></div>
              </div>
              {detail.note && (
                <div className="col-12">
                  <label className="form-label small text-secondary">Ghi chú</label>
                  <div>{detail.note}</div>
                </div>
              )}
            </div>

            <h6 className="fw-bold mt-3 mb-2">Dịch vụ</h6>
            <table className="table table-dark-custom mb-3">
              <thead><tr><th>Tên</th><th>Đơn giá</th><th>SL</th></tr></thead>
              <tbody>
                {(detail.services || []).map((s: any, i: number) => (
                  <tr key={i}>
                    <td>{s.serviceName}</td>
                    <td>{(s.price || 0).toLocaleString('vi-VN')}đ</td>
                    <td>{s.quantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Invoices */}
            {detail.invoices && detail.invoices.length > 0 ? (
              <div className="mt-3">
                <span className="badge bg-success"><i className="fas fa-check me-1"></i>Đã có hoá đơn #{detail.invoices[0].invoiceId}</span>
                <div className="mt-1 small text-secondary">
                  Tổng: {(detail.invoices[0].finalAmount || 0).toLocaleString('vi-VN')}đ — {detail.invoices[0].paymentMethod}
                </div>
              </div>
            ) : (
              detail.status === 'Completed' && (
                <button className="btn btn-accent btn-sm mt-2" onClick={() => createInvoice(detail.appointmentId)}>
                  <i className="fas fa-file-invoice me-1"></i>Tạo hoá đơn
                </button>
              )
            )}
          </div>
        )}
      </Modal>

      {/* Table */}
      <div className="card-dark overflow-hidden">
        <div className="table-responsive">
          <table className="table table-dark-custom mb-0">
            <thead>
              <tr><th>Mã</th><th>Khách hàng</th><th>Ngày</th><th>Giờ</th><th>Dịch vụ</th><th>Trạng thái</th><th>Thao tác</th></tr>
            </thead>
            <tbody>
              {(data.items || []).length === 0 && (
                <tr><td colSpan={7} className="text-center text-secondary py-4"><i className="fas fa-calendar-alt fa-2x mb-2 d-block"></i>Không có lịch hẹn nào</td></tr>
              )}
              {(data.items || []).map((a: any) => (
                <tr key={a.appointmentId}>
                  <td>{a.appointmentId}</td>
                  <td className="fw-bold">{a.customer?.fullName}</td>
                  <td>{a.appointmentDate}</td>
                  <td>{a.appointmentTime}</td>
                  <td><small>{a.services?.map((s: any) => s.serviceName).join(', ')}</small></td>
                  <td><span className={`badge-status badge-${(a.status || 'pending').toLowerCase()}`}>{a.status || 'Pending'}</span></td>
                  <td>
                    <div className="d-flex gap-1">
                      <button className="btn btn-outline-info btn-sm" title="Xem chi tiết" onClick={() => viewDetail(a.appointmentId)}><i className="fas fa-eye"></i></button>
                      {a.status !== 'Completed' && a.status !== 'Cancelled' && (
                        <select className="form-select form-select-sm" style={{ width: 120 }} value={a.status || 'Pending'} onChange={e => updateStatus(a.appointmentId, e.target.value)}>
                          {statuses.map(s => <option key={s} value={s}>{s}</option>)}
                        </select>
                      )}
                      {a.status === 'Completed' && !a.hasInvoice && (
                        <button className="btn btn-outline-success btn-sm" title="Tạo hoá đơn" onClick={() => createInvoice(a.appointmentId)}>
                          <i className="fas fa-file-invoice"></i>
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      <div className="d-flex justify-content-between mt-3">
        <small className="text-secondary">Tổng: {data.totalItems} — Trang {data.page}/{data.totalPages}</small>
        <div className="d-flex gap-2">
          <button className="btn btn-outline-secondary btn-sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>← Trước</button>
          <button className="btn btn-outline-secondary btn-sm" disabled={page >= data.totalPages} onClick={() => setPage(p => p + 1)}>Sau →</button>
        </div>
      </div>
    </>
  );
}
