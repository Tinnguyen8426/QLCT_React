import { useEffect, useState } from 'react';
import api from '../../api/client';
import { useCartStore } from '../../stores/cartStore';

interface PurchasedProduct {
  productId: number;
  productName: string;
  imageUrl?: string;
  description?: string;
  price: number;
  purchasedQuantity: number;
  lastPurchasedAt: string;
}

interface RecommendedProduct {
  productId: number;
  productName: string;
  imageUrl?: string;
  description?: string;
  price: number;
}

export default function CustomerProductsPage() {
  const [data, setData] = useState<{
    purchasedProducts: PurchasedProduct[];
    recommendedProducts: RecommendedProduct[];
  }>({
    purchasedProducts: [],
    recommendedProducts: []
  });
  const [toast, setToast] = useState('');
  const { addToCart } = useCartStore();

  useEffect(() => {
    api.get('/api/customer/products').then(r => setData(r.data));
  }, []);

  const handleAdd = async (id: number) => {
    try {
      const msg = await addToCart(id);
      setToast(msg);
      setTimeout(() => setToast(''), 3000);
    } catch {
      setToast('Không thể thêm vào giỏ hàng');
      setTimeout(() => setToast(''), 3000);
    }
  };

  const formatDate = (dateStr: string) => {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleDateString('vi-VN');
  };

  return (
    <div className="container py-5">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h2 className="fw-bold mb-1"><i className="fas fa-box-open me-2" style={{ color: 'var(--accent)' }}></i>Tủ Mỹ Phẩm Của Tôi</h2>
          <small className="text-secondary">Theo dõi các sản phẩm đã từng sử dụng tại salon và đặt lại chỉ bằng một chạm.</small>
        </div>
      </div>

      <div className="row g-4 mb-5">
        <div className="col-12">
          <div className="card-dark p-4">
            <h5 className="fw-bold mb-4">Đã dùng gần đây</h5>
            {data.purchasedProducts.length === 0 ? (
              <p className="text-secondary small mb-0">Chưa có sản phẩm nào trong lịch sử hóa đơn.</p>
            ) : (
              <div className="d-flex flex-column gap-3">
                {data.purchasedProducts.slice(0, 5).map(p => (
                  <div key={p.productId} className="d-flex justify-content-between border-bottom border-secondary pb-3">
                    <div>
                      <div className="fw-semibold">{p.productName}</div>
                      <small className="text-secondary">Mua lần cuối: {formatDate(p.lastPurchasedAt)}</small>
                    </div>
                    <div className="text-end">
                      <div className="fw-bold" style={{ color: 'var(--accent)' }}>{p.price.toLocaleString('vi-VN')}đ</div>
                      <small className="text-secondary">Đã mua {p.purchasedQuantity} lần</small>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="mb-4">
        <h4 className="fw-bold mb-1">Đặt lại nhanh</h4>
        <small className="text-secondary">Các sản phẩm bạn từng dùng sẽ được ưu tiên đề xuất.</small>
      </div>

      {data.purchasedProducts.length === 0 ? (
        <div className="text-secondary mb-5">Bạn chưa sử dụng mỹ phẩm nào tại salon. Hãy khám phá cửa hàng để được tư vấn.</div>
      ) : (
        <div className="row g-4 mb-5">
          {data.purchasedProducts.map(p => (
            <div key={p.productId} className="col-md-4 animate-in">
              <div className="card-dark h-100 overflow-hidden d-flex flex-column">
                {p.imageUrl ? (
                  <img src={p.imageUrl} alt={p.productName} className="w-100" style={{ height: 200, objectFit: 'cover' }} />
                ) : (
                  <div className="d-flex align-items-center justify-content-center" style={{ height: 200, background: 'var(--bg-input)' }}>
                    <i className="fas fa-box fa-3x text-secondary"></i>
                  </div>
                )}
                <div className="p-3 d-flex flex-column flex-grow-1">
                  <h6 className="fw-bold mb-1">{p.productName}</h6>
                  <small className="text-secondary mb-2">Mua lần cuối: {formatDate(p.lastPurchasedAt)}</small>
                  {p.description && <p className="text-secondary small flex-grow-1">{p.description.substring(0, 100)}...</p>}
                  <div className="mt-auto d-flex justify-content-between align-items-center pt-3">
                    <span className="fw-bold" style={{ color: 'var(--accent)' }}>{p.price.toLocaleString('vi-VN')}đ</span>
                    <button className="btn btn-accent btn-sm" onClick={() => handleAdd(p.productId)}>
                      <i className="fas fa-cart-plus me-1"></i>Đặt lại
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="mb-4">
        <h4 className="fw-bold mb-1">Gợi ý cho bạn</h4>
        <small className="text-secondary">Dựa trên xu hướng bán chạy tại salon</small>
      </div>

      {data.recommendedProducts.length === 0 ? (
        <div className="text-secondary mb-5">Chúng tôi đang thu thập thêm dữ liệu để gợi ý chính xác hơn.</div>
      ) : (
        <div className="row g-4 mb-5">
          {data.recommendedProducts.map(p => (
            <div key={p.productId} className="col-md-4 animate-in">
              <div className="card-dark h-100 overflow-hidden d-flex flex-column">
                {p.imageUrl ? (
                  <img src={p.imageUrl} alt={p.productName} className="w-100" style={{ height: 200, objectFit: 'cover' }} />
                ) : (
                  <div className="d-flex align-items-center justify-content-center" style={{ height: 200, background: 'var(--bg-input)' }}>
                    <i className="fas fa-box fa-3x text-secondary"></i>
                  </div>
                )}
                <div className="p-3 d-flex flex-column flex-grow-1">
                  <h6 className="fw-bold mb-1">{p.productName}</h6>
                  {p.description && <p className="text-secondary small flex-grow-1">{p.description.substring(0, 100)}...</p>}
                  <div className="mt-auto d-flex justify-content-between align-items-center pt-3">
                    <span className="fw-bold" style={{ color: 'var(--accent)' }}>{p.price.toLocaleString('vi-VN')}đ</span>
                    <button className="btn btn-accent btn-sm" onClick={() => handleAdd(p.productId)}>
                      <i className="fas fa-cart-plus"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Toast */}
      {toast && (
        <div className="toast-container">
          <div className="alert alert-success d-flex align-items-center gap-2 animate-in">
            <i className="fas fa-check-circle"></i> {toast}
          </div>
        </div>
      )}
    </div>
  );
}
