import { create } from 'zustand';
import api from '../api/client';

interface User {
  userId: number;
  fullName: string;
  email: string;
  phone?: string;
  avatarUrl?: string;
  role: string;
}

interface AuthState {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>;
  register: (data: { fullName: string; email: string; phone?: string; password: string; confirmPassword: string }) => Promise<{ success: boolean; errors?: Record<string, string> }>;
  logout: () => void;
  loadUser: () => Promise<void>;
  isAdmin: () => boolean;
  isStaff: () => boolean;
  isCustomer: () => boolean;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: JSON.parse(localStorage.getItem('user') || 'null'),
  token: localStorage.getItem('token'),
  isLoading: false,

  login: async (email, password) => {
    try {
      const res = await api.post('/api/auth/login', { email, password });
      const { token, user } = res.data;
      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(user));
      set({ token, user });
      return { success: true };
    } catch (err: any) {
      return { success: false, error: err.response?.data?.message || 'Đăng nhập thất bại.' };
    }
  },

  register: async (data) => {
    try {
      await api.post('/api/auth/register', data);
      return { success: true };
    } catch (err: any) {
      return { success: false, errors: err.response?.data?.errors };
    }
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    set({ user: null, token: null });
  },

  loadUser: async () => {
    const token = localStorage.getItem('token');
    if (!token) return;
    set({ isLoading: true });
    try {
      const res = await api.get('/api/auth/me');
      const user = res.data;
      if (user && user.role) {
        localStorage.setItem('user', JSON.stringify(user));
        set({ user, isLoading: false });
      } else {
        throw new Error('Invalid user data');
      }
    } catch {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      set({ user: null, token: null, isLoading: false });
    }
  },

  isAdmin: () => {
    const { user } = get();
    return user?.role === 'Admin' || user?.role === 'Administrator';
  },

  isStaff: () => get().user?.role === 'Staff',
  isCustomer: () => {
    const role = get().user?.role;
    return role === 'Customer' || role === 'KhachHang' || role === 'User';
  }
}));
