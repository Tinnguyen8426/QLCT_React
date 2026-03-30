import { useEffect, useState } from 'react';
import api from '../../api/client';

interface Service {
  serviceId: number;
  serviceName: string;
  description?: string;
  price: number;
  duration?: number;
  category?: string;
  imageUrl?: string;
}

export default function ServicesPage() {
  const [services, setServices] = useState<Service[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [selected, setSelected] = useState('');

  useEffect(() => {
    api.get('/api/services', { params: selected ? { category: selected } : {} })
      .then(res => { setServices(res.data.services); setCategories(res.data.categories); });
  }, [selected]);

  return (
    <div className="container py-5">
      <div className="page-header text-center">
        <h2><i className="fas fa-cut me-2" style={{ color: 'var(--accent)' }}></i>Dịch Vụ Của Chúng Tôi</h2>
        <p style={{ color: 'var(--text-secondary)' }}>Đa dạng dịch vụ phù hợp với mọi phong cách</p>
      </div>

      {categories.length > 0 && (
        <div className="d-flex justify-content-center flex-wrap gap-2 mb-4">
          <button className={`btn btn-sm ${!selected ? 'btn-accent' : 'btn-outline-secondary'}`} onClick={() => setSelected('')}>
            Tất cả
          </button>
          {categories.map(c => (
            <button key={c} className={`btn btn-sm ${selected === c ? 'btn-accent' : 'btn-outline-secondary'}`} onClick={() => setSelected(c)}>
              {c}
            </button>
          ))}
        </div>
      )}

      <div className="row g-4">
        {services.map(s => (
          <div key={s.serviceId} className="col-md-6 col-lg-4 animate-in">
            <div className="card-dark h-100 overflow-hidden">
              {s.imageUrl && (
                <img src={s.imageUrl} alt={s.serviceName} className="w-100" style={{ height: 200, objectFit: 'cover' }} />
              )}
              <div className="p-4">
                <div className="d-flex justify-content-between align-items-start mb-2">
                  <h5 className="fw-bold mb-0">{s.serviceName}</h5>
                  <span className="badge" style={{ background: 'rgba(201,168,76,0.15)', color: 'var(--accent)' }}>
                    {s.price.toLocaleString('vi-VN')}đ
                  </span>
                </div>
                {s.category && <span className="badge bg-secondary mb-2">{s.category}</span>}
                {s.description && <p className="mb-2" style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>{s.description}</p>}
                {s.duration && (
                  <p className="mb-0" style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                    <i className="fas fa-clock me-1"></i>{s.duration} phút
                  </p>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
