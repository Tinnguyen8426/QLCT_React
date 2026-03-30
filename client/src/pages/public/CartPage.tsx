import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useCartStore } from '../../stores/cartStore';
import api from '../../api/client';
import { useAuthStore } from '../../stores/authStore';

export default function CartPage() {
  const { items, totalQuantity, totalAmount, fetchCart, updateQuantity, removeItem, clearCart } = useCartStore();
  const { user } = useAuthStore();
  const [form, setForm] = useState({ fullName: '', phone: '', address: '', method: 'COD' });
  const [qrModal, setQrModal] = useState<{ show: boolean, invoiceId: number | null }>({ show: false, invoiceId: null });
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  useEffect(() => { fetchCart(); }, []);

  useEffect(() => {
    if (user) {
      setForm(prev => ({ ...prev, fullName: user.fullName || '', phone: user.phone || '' }));
    }
  }, [user]);

  const checkout = async () => {
    setLoading(true);
    try {
      const res = await api.post('/api/cart/checkout', form);
      if (form.method === 'VietQR') {
        setQrModal({ show: true, invoiceId: res.data.invoiceId });
      } else {
        alert('Đặt hàng thành công!');
        navigate('/');
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi đặt hàng');
    } finally { setLoading(false); }
  };

  return (
    <div className="container py-5">
      <h2 className="fw-bold mb-4"><i className="fas fa-shopping-cart me-2" style={{ color: 'var(--accent)' }}></i>Giỏ Hàng</h2>

      {items.length === 0 ? (
        <div className="card-dark p-5 text-center">
          <i className="fas fa-shopping-basket fa-3x text-secondary mb-3"></i>
          <h5>Giỏ hàng trống</h5>
          <Link to="/store" className="btn btn-accent mt-3">Tiếp tục mua sắm</Link>
        </div>
      ) : (
        <div className="row g-4">
          <div className="col-lg-8">
            <div className="card-dark p-4">
              <div className="d-flex justify-content-between mb-3">
                <h5 className="fw-bold mb-0">{totalQuantity} sản phẩm</h5>
                <button className="btn btn-outline-danger btn-sm" onClick={clearCart}><i className="fas fa-trash me-1"></i>Xoá tất cả</button>
              </div>
              {items.map(item => (
                <div key={item.productId} className="d-flex gap-3 py-3" style={{ borderBottom: '1px solid var(--border-color)' }}>
                  {item.imageUrl ? (
                    <img src={item.imageUrl} alt="" className="rounded" style={{ width: 80, height: 80, objectFit: 'cover' }} />
                  ) : (
                    <div className="rounded d-flex align-items-center justify-content-center" style={{ width: 80, height: 80, background: 'var(--bg-input)' }}>
                      <i className="fas fa-box text-secondary"></i>
                    </div>
                  )}
                  <div className="flex-grow-1">
                    <h6 className="fw-bold mb-1">{item.productName}</h6>
                    <div className="text-secondary mb-2">{item.unitPrice.toLocaleString('vi-VN')}đ</div>
                    <div className="d-flex align-items-center gap-2">
                      <button className="btn btn-outline-secondary btn-sm" onClick={() => updateQuantity(item.productId, item.quantity - 1)}>-</button>
                      <span className="px-2">{item.quantity}</span>
                      <button className="btn btn-outline-secondary btn-sm" onClick={() => updateQuantity(item.productId, item.quantity + 1)}>+</button>
                    </div>
                  </div>
                  <div className="text-end">
                    <div className="fw-bold" style={{ color: 'var(--accent)' }}>{item.lineTotal.toLocaleString('vi-VN')}đ</div>
                    <button className="btn btn-link text-danger btn-sm p-0 mt-2" onClick={() => removeItem(item.productId)}>
                      <i className="fas fa-times"></i> Xoá
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="col-lg-4">
            <div className="card-dark p-4 position-sticky" style={{ top: '1rem' }}>
              <h5 className="fw-bold mb-3">Thanh toán</h5>
              <div className="mb-3">
                <label className="form-label small text-secondary">Họ tên *</label>
                <input className="form-control" value={form.fullName} onChange={e => setForm({ ...form, fullName: e.target.value })} readOnly={!!user?.fullName} style={{ background: user?.fullName ? 'rgba(255,255,255,0.05)' : '' }} />
              </div>
              <div className="mb-3">
                <label className="form-label small text-secondary">Số điện thoại *</label>
                <input className="form-control" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} readOnly={!!user?.phone} style={{ background: user?.phone ? 'rgba(255,255,255,0.05)' : '' }} />
              </div>
              <div className="mb-3">
                <label className="form-label small text-secondary">Địa chỉ giao hàng</label>
                <input className="form-control" value={form.address} onChange={e => setForm({ ...form, address: e.target.value })} />
              </div>

              <div className="mb-3">
                <label className="form-label small text-secondary">Phương thức thanh toán</label>
                <div className="form-check mb-2 p-3 rounded" style={{ background: form.method === 'COD' ? 'rgba(201,168,76,0.1)' : 'var(--bg-input)', border: form.method === 'COD' ? '1px solid var(--accent)' : '1px solid transparent', cursor: 'pointer' }} onClick={() => setForm({ ...form, method: 'COD' })}>
                  <input className="form-check-input ms-1" type="radio" name="paymentMethod" checked={form.method === 'COD'} onChange={() => {}} />
                  <label className="form-check-label ms-2 d-flex flex-column" style={{ cursor: 'pointer' }}>
                    <span className="fw-bold">Thanh toán khi nhận hàng (COD)</span>
                    <small className="text-secondary">Thanh toán bằng tiền mặt khi nhận được hàng</small>
                  </label>
                </div>
                <div className="form-check p-3 rounded" style={{ background: form.method === 'VietQR' ? 'rgba(201,168,76,0.1)' : 'var(--bg-input)', border: form.method === 'VietQR' ? '1px solid var(--accent)' : '1px solid transparent', cursor: 'pointer' }} onClick={() => setForm({ ...form, method: 'VietQR' })}>
                  <input className="form-check-input ms-1" type="radio" name="paymentMethod" checked={form.method === 'VietQR'} onChange={() => {}} />
                  <label className="form-check-label ms-2 d-flex flex-column" style={{ cursor: 'pointer' }}>
                    <span className="fw-bold">Chuyển khoản VietQR</span>
                    <small className="text-secondary">Quét mã QR để thanh toán nhanh</small>
                  </label>
                </div>
              </div>

              <hr style={{ borderColor: 'var(--border-color)' }} />
              <div className="d-flex justify-content-between mb-3 fw-bold" style={{ fontSize: '1.1rem' }}>
                <span>Tổng cộng</span>
                <span style={{ color: 'var(--accent)' }}>{totalAmount.toLocaleString('vi-VN')}đ</span>
              </div>
              <button className="btn btn-accent w-100" onClick={checkout} disabled={loading || !form.fullName || !form.phone}>
                {loading ? 'Đang xử lý...' : 'Đặt hàng'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* QR Modal */}
      {qrModal.show && (
        <div className="modal fade show d-block" style={{ background: 'rgba(0,0,0,0.8)' }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content" style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)' }}>
              <div className="modal-header border-0">
                <h5 className="modal-title fw-bold">Thanh toán qua mã QR</h5>
                <button type="button" className="btn-close btn-close-white" onClick={() => {
                  setQrModal({ show: false, invoiceId: null });
                  navigate('/');
                }}></button>
              </div>
              <div className="modal-body text-center pb-5">
                <p className="text-secondary mb-4">Vui lòng quét mã bên dưới bằng ứng dụng ngân hàng để hoàn tất thanh toán.</p>
                <div className="bg-white p-3 rounded d-inline-block mb-4">
                  <img 
                    src={`https://img.vietqr.io/image/techcombank-102406200499-compact2.png?amount=${totalAmount}&addInfo=Thanh toan don hang ${qrModal.invoiceId}&accountName=NGUYEN THANH TIN`} 
                    alt="VietQR" 
                    style={{ width: '250px', height: '250px', objectFit: 'contain' }} 
                  />
                </div>
                <div className="bg-dark p-3 rounded text-start mb-4 mx-auto" style={{ maxWidth: '300px', fontSize: '14px' }}>
                  <div className="d-flex justify-content-between mb-2">
                    <span className="text-secondary">Ngân hàng:</span>
                    <span className="fw-bold text-white">Techcombank</span>
                  </div>
                  <div className="d-flex justify-content-between mb-2">
                    <span className="text-secondary">Số tài khoản:</span>
                    <span className="fw-bold" style={{ color: 'var(--accent)' }}>102406200499</span>
                  </div>
                  <div className="d-flex justify-content-between mb-2">
                    <span className="text-secondary">Chủ tài khoản:</span>
                    <span className="fw-bold text-white text-uppercase">NGUYEN THANH TIN</span>
                  </div>
                  <div className="d-flex justify-content-between mb-2">
                    <span className="text-secondary">Số tiền:</span>
                    <span className="fw-bold" style={{ color: 'var(--accent)' }}>{totalAmount.toLocaleString('vi-VN')}đ</span>
                  </div>
                  <div className="d-flex justify-content-between">
                    <span className="text-secondary">Nội dung:</span>
                    <span className="fw-bold text-white">Thanh toan don hang {qrModal.invoiceId}</span>
                  </div>
                </div>
                <button className="btn btn-accent px-5 py-2" onClick={() => {
                  alert('Cảm ơn bạn đã mua sắm!');
                  setQrModal({ show: false, invoiceId: null });
                  navigate('/');
                }}>
                  Tôi đã chuyển khoản
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
