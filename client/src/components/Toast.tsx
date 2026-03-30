import { useState, useCallback, useEffect } from 'react';

interface ToastItem {
  id: number;
  message: string;
  type: 'success' | 'error';
}

let toastIdCounter = 0;

export function useToast() {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const show = useCallback((message: string, type: 'success' | 'error' = 'success') => {
    const id = ++toastIdCounter;
    setToasts(prev => [...prev, { id, message, type }]);
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 3000);
  }, []);

  return { toasts, show };
}

export function ToastContainer({ toasts }: { toasts: ToastItem[] }) {
  return (
    <div style={{ position: 'fixed', top: 16, right: 16, zIndex: 9999, display: 'flex', flexDirection: 'column', gap: 8 }}>
      {toasts.map(t => (
        <ToastMessage key={t.id} item={t} />
      ))}
    </div>
  );
}

function ToastMessage({ item }: { item: ToastItem }) {
  const [visible, setVisible] = useState(false);
  useEffect(() => { requestAnimationFrame(() => setVisible(true)); }, []);

  const bg = item.type === 'success' ? 'rgba(40,167,69,0.9)' : 'rgba(220,53,69,0.9)';
  const icon = item.type === 'success' ? 'fas fa-check-circle' : 'fas fa-exclamation-circle';

  return (
    <div style={{
      background: bg, color: '#fff', padding: '12px 20px', borderRadius: 8,
      fontSize: '0.9rem', display: 'flex', alignItems: 'center', gap: 10,
      boxShadow: '0 4px 16px rgba(0,0,0,0.3)', minWidth: 280,
      opacity: visible ? 1 : 0, transform: visible ? 'translateX(0)' : 'translateX(40px)',
      transition: 'all 0.3s ease'
    }}>
      <i className={icon}></i>
      <span>{item.message}</span>
    </div>
  );
}
