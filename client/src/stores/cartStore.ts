import { create } from 'zustand';
import api from '../api/client';

interface CartItem {
  productId: number;
  productName: string;
  imageUrl?: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

interface CartState {
  items: CartItem[];
  totalQuantity: number;
  totalAmount: number;
  loading: boolean;
  fetchCart: () => Promise<void>;
  addToCart: (productId: number, quantity?: number) => Promise<string>;
  updateQuantity: (productId: number, quantity: number) => Promise<void>;
  removeItem: (productId: number) => Promise<void>;
  clearCart: () => Promise<void>;
}

export const useCartStore = create<CartState>((set) => ({
  items: [],
  totalQuantity: 0,
  totalAmount: 0,
  loading: false,

  fetchCart: async () => {
    set({ loading: true });
    try {
      const res = await api.get('/api/cart');
      set({ items: res.data.items, totalQuantity: res.data.totalQuantity, totalAmount: res.data.totalAmount, loading: false });
    } catch {
      set({ loading: false });
    }
  },

  addToCart: async (productId, quantity = 1) => {
    const res = await api.post('/api/cart/add', { productId, quantity });
    set({ totalQuantity: res.data.cartQuantity });
    return res.data.message;
  },

  updateQuantity: async (productId, quantity) => {
    await api.put('/api/cart/update', { productId, quantity });
    const cart = await api.get('/api/cart');
    set({ items: cart.data.items, totalQuantity: cart.data.totalQuantity, totalAmount: cart.data.totalAmount });
  },

  removeItem: async (productId) => {
    await api.delete(`/api/cart/remove/${productId}`);
    const cart = await api.get('/api/cart');
    set({ items: cart.data.items, totalQuantity: cart.data.totalQuantity, totalAmount: cart.data.totalAmount });
  },

  clearCart: async () => {
    await api.delete('/api/cart/clear');
    set({ items: [], totalQuantity: 0, totalAmount: 0 });
  }
}));
