import { useEffect, useState } from 'react';
import api from '../../api/client';

export default function CustomerInvoicesPage() {
  const [data, setData] = useState<any>({ invoices: [], total: 0 });

  useEffect(() => { api.get('/api/customer/invoices').then(r => setData(r.data)); }, []);

  return (
    <div className="container py-5">
      <h2 className="fw-bold mb-4"><i className="fas fa-file-invoice me-2" style={{ color: 'var(--accent)' }}></i>Hoá Đơn Của Tôi</h2>
      {data.invoices.length === 0 ? (
        <div className="card-dark p-5 text-center"><p className="text-secondary">Chưa có hoá đơn.</p></div>
      ) : (
        <div className="card-dark overflow-hidden">
          <div className="table-responsive">
            <table className="table table-dark-custom mb-0">
              <thead><tr><th>#</th><th>Ngày</th><th>Nhân viên</th><th>Tổng</th><th>Giảm giá</th><th>Thanh toán</th></tr></thead>
              <tbody>
                {data.invoices.map((inv: any) => (
                  <tr key={inv.invoiceId}>
                    <td>{inv.invoiceId}</td>
                    <td>{inv.createdAt ? new Date(inv.createdAt).toLocaleDateString('vi-VN') : '—'}</td>
                    <td>{inv.staffName || '—'}</td>
                    <td>{(inv.total || 0).toLocaleString('vi-VN')}đ</td>
                    <td>{(inv.discount || 0).toLocaleString('vi-VN')}đ</td>
                    <td style={{ color: 'var(--accent)' }}>{(inv.finalAmount || 0).toLocaleString('vi-VN')}đ</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
