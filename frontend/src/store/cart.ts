// frontend/src/store/cart.ts
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type CartItem = {
  productId: string;
  name: string;
  price: number;
  quantity: number;
  complementIds: string[];
  complementsTotal: number;
};

type CartState = {
  establishmentId: string | null;
  slug: string | null;
  items: CartItem[];
  addItem: (item: CartItem, establishmentId: string, slug: string) => void;
  removeItem: (productId: string) => void;
  updateQuantity: (productId: string, quantity: number) => void;
  clear: () => void;
  total: () => number;
};

export const useCart = create<CartState>()(
  persist(
    (set, get) => ({
      establishmentId: null,
      slug: null,
      items: [],

      addItem: (item, establishmentId, slug) => {
        const state = get();
        if (state.establishmentId && state.establishmentId !== establishmentId) {
          set({ items: [item], establishmentId, slug });
          return;
        }
        const existing = state.items.find((i) => i.productId === item.productId);
        if (existing) {
          set({
            items: state.items.map((i) =>
              i.productId === item.productId
                ? { ...i, quantity: i.quantity + item.quantity }
                : i,
            ),
          });
        } else {
          set({ items: [...state.items, item], establishmentId, slug });
        }
      },

      removeItem: (productId) =>
        set((s) => ({ items: s.items.filter((i) => i.productId !== productId) })),

      updateQuantity: (productId, quantity) =>
        set((s) => ({
          items:
            quantity <= 0
              ? s.items.filter((i) => i.productId !== productId)
              : s.items.map((i) =>
                  i.productId === productId ? { ...i, quantity } : i,
                ),
        })),

      clear: () => set({ items: [], establishmentId: null, slug: null }),

      total: () =>
        get().items.reduce(
          (sum, i) => sum + (i.price + i.complementsTotal) * i.quantity,
          0,
        ),
    }),
    { name: 'delify-cart' },
  ),
);
