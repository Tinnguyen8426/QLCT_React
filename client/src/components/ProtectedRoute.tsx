import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';
import UnauthorizedPage from '../pages/auth/UnauthorizedPage';

interface Props {
  role?: string;
  children?: React.ReactNode;
}

export default function ProtectedRoute({ role, children }: Props) {
  const { user } = useAuthStore();

  if (!user) return <Navigate to="/login" replace />;

  if (role) {
    const roleMap: Record<string, string[]> = {
      Admin: ['Admin', 'Administrator'],
      Staff: ['Staff'],
      Customer: ['Customer', 'KhachHang', 'User']
    };
    const allowed = roleMap[role] || [role];
    if (!allowed.includes(user.role)) {
      return <UnauthorizedPage />;
    }
  }

  return children ? <>{children}</> : <Outlet />;
}
