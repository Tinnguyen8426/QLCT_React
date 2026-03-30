import { useEffect, useState } from 'react';
import api from '../../api/client';
import { useCartStore } from '../../stores/cartStore';

interface Product { productId: number; productName: string; description?: string; category?: string; brand?: string; imageUrl?: string; price: number; stockQuantity: number; isLowStock: boolean; }

export default function StorePage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [cat, setCat] = useState('');
  const [keyword, setKeyword] = useState('');
  const [toast, setToast] = useState('');
  const { addToCart } = useCartStore();

  useEffect(() => {
    api.get('/api/products', { params: { keyword: keyword || undefined, category: cat || undefined } })
      .then(r => { setProducts(r.data.products); setCategories(r.data.categories); });
  }, [cat, keyword]);

  const handleAdd = async (id: number) => {
    const msg = await addToCart(id);
    setToast(msg);
    setTimeout(() => setToast(''), 3000);
  };

  return (
    <div className="container py-5">
      <div className="page-header text-center">
        <h2><i className="fas fa-store me-2" style={{ color: 'var(--accent)' }}></i>Cửa Hàng Mỹ Phẩm</h2>
      </div>

      {/* Filters */}
      <div className="row g-3 mb-4">
        <div className="col-md-6">
          <input className="form-control" placeholder="Tìm kiếm sản phẩm..." value={keyword} onChange={e => setKeyword(e.target.value)} />
        </div>
        <div className="col-md-6">
          <select className="form-select" value={cat} onChange={e => setCat(e.target.value)}>
            <option value="">Tất cả danh mục</option>
            {categories.map(c => <option key={c} value={c}>{c}</option>)}
          </select>
        </div>
      </div>

      {/* Products grid */}
      <div className="row g-4">
        {products.map(p => (
          <div key={p.productId} className="col-6 col-md-4 col-lg-3 animate-in">
            <div className="card-dark h-100 overflow-hidden d-flex flex-column">
              {p.imageUrl ? (
                <img src={p.imageUrl} alt={p.productName} className="w-100" style={{ height: 180, objectFit: 'cover' }} />
              ) : (
                <div className="d-flex align-items-center justify-content-center" style={{ height: 180, background: 'var(--bg-input)' }}>
                  <i className="fas fa-box fa-3x text-secondary"></i>
                </div>
              )}
              <div className="p-3 d-flex flex-column flex-grow-1">
                <h6 className="fw-bold mb-1">{p.productName}</h6>
                {p.brand && <small className="text-secondary mb-2">{p.brand}</small>}
                <div className="mt-auto d-flex justify-content-between align-items-center">
                  <span className="fw-bold" style={{ color: 'var(--accent)' }}>{p.price.toLocaleString('vi-VN')}đ</span>
                  <button className="btn btn-accent btn-sm" onClick={() => handleAdd(p.productId)} disabled={p.stockQuantity <= 0}>
                    <i className="fas fa-cart-plus"></i>
                  </button>
                </div>
                {p.isLowStock && p.stockQuantity > 0 && <small className="text-warning mt-1"><i className="fas fa-exclamation-triangle me-1"></i>Còn {p.stockQuantity}</small>}
                {p.stockQuantity <= 0 && <small className="text-danger mt-1">Hết hàng</small>}
              </div>
            </div>
          </div>
        ))}
      </div>

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
