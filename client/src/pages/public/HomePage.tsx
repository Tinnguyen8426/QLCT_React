import { Link } from 'react-router-dom';

export default function HomePage() {
  return (
    <>
      <section className="hero-section text-center">
        <div className="container animate-in">
          <h1 className="display-4 fw-bold mb-3">
            Phong Cách <span style={{ color: 'var(--accent)' }}>Đẳng Cấp</span>
          </h1>
          <p className="lead mb-4" style={{ color: 'var(--text-secondary)', maxWidth: 600, margin: '0 auto' }}>
            Trải nghiệm dịch vụ cắt tóc chuyên nghiệp với đội ngũ barber hàng đầu.
            Đặt lịch ngay để có diện mạo mới!
          </p>
          <div className="d-flex justify-content-center gap-3">
            <Link to="/booking" className="btn btn-accent btn-lg px-4">
              <i className="fas fa-calendar-plus me-2"></i>Đặt lịch ngay
            </Link>
            <Link to="/services" className="btn btn-outline-light btn-lg px-4">
              <i className="fas fa-cut me-2"></i>Xem dịch vụ
            </Link>
          </div>
        </div>
      </section>

      <section className="py-5">
        <div className="container">
          <h2 className="text-center fw-bold mb-5">
            Tại sao chọn <span style={{ color: 'var(--accent)' }}>chúng tôi</span>?
          </h2>
          <div className="row g-4">
            {[
              { icon: 'fas fa-award', title: 'Chuyên nghiệp', desc: 'Đội ngũ barber được đào tạo bài bản, nhiều năm kinh nghiệm.' },
              { icon: 'fas fa-clock', title: 'Tiết kiệm thời gian', desc: 'Đặt lịch online, không cần chờ đợi. Đến đúng giờ, về đúng lúc.' },
              { icon: 'fas fa-gem', title: 'Sản phẩm cao cấp', desc: 'Sử dụng sản phẩm chăm sóc tóc hàng đầu, an toàn cho mọi loại tóc.' },
              { icon: 'fas fa-star', title: 'Đánh giá cao', desc: 'Được hàng nghìn khách hàng tin tưởng và đánh giá 5 sao.' }
            ].map((item, i) => (
              <div key={i} className="col-md-6 col-lg-3">
                <div className="card-dark p-4 text-center h-100">
                  <div className="mb-3" style={{ fontSize: '2.5rem', color: 'var(--accent)' }}>
                    <i className={item.icon}></i>
                  </div>
                  <h5 className="fw-bold mb-2">{item.title}</h5>
                  <p className="mb-0" style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>{item.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="py-5" style={{ background: 'var(--bg-card)' }}>
        <div className="container text-center">
          <h2 className="fw-bold mb-3">Sẵn sàng thay đổi diện mạo?</h2>
          <p className="mb-4" style={{ color: 'var(--text-secondary)' }}>
            Đặt lịch ngay hôm nay để trải nghiệm dịch vụ tuyệt vời của chúng tôi.
          </p>
          <Link to="/booking" className="btn btn-accent btn-lg">
            <i className="fas fa-calendar-plus me-2"></i>Đặt lịch ngay
          </Link>
        </div>
      </section>
    </>
  );
}
