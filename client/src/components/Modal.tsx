import { useEffect, useRef } from 'react';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  size?: 'sm' | 'md' | 'lg';
}

export default function Modal({ isOpen, onClose, title, children, size = 'md' }: ModalProps) {
  const backdropRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isOpen) document.body.style.overflow = 'hidden';
    else document.body.style.overflow = '';
    return () => { document.body.style.overflow = ''; };
  }, [isOpen]);

  if (!isOpen) return null;

  const widths = { sm: 440, md: 600, lg: 800 };

  return (
    <div ref={backdropRef}
      onClick={e => { if (e.target === backdropRef.current) onClose(); }}
      style={{
        position: 'fixed', inset: 0, zIndex: 9998,
        background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(4px)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        animation: 'fadeIn 0.2s ease'
      }}>
      <div style={{
        background: 'var(--bg-card)', border: '1px solid var(--border-color)',
        borderRadius: 'var(--radius)', width: '90%', maxWidth: widths[size],
        maxHeight: '85vh', display: 'flex', flexDirection: 'column',
        animation: 'slideUp 0.25s ease'
      }}>
        {/* Header */}
        <div className="d-flex justify-content-between align-items-center px-4 py-3"
          style={{ borderBottom: '1px solid var(--border-color)' }}>
          <h5 className="fw-bold mb-0">{title}</h5>
          <button className="btn btn-sm text-secondary" onClick={onClose}
            style={{ fontSize: '1.25rem', lineHeight: 1 }}>
            <i className="fas fa-times"></i>
          </button>
        </div>
        {/* Body */}
        <div className="px-4 py-3" style={{ overflowY: 'auto', flex: 1 }}>
          {children}
        </div>
      </div>
    </div>
  );
}
