'use client';
import { useState } from 'react';
import { useCart } from '@/store/cart';
import CartDrawer from './CartDrawer';

export default function CartButton({ slug }: { slug: string }) {
  const [open, setOpen] = useState(false);
  const items = useCart((s) => s.items);
  const total = useCart((s) => s.total);
  const count = items.reduce((sum, i) => sum + i.quantity, 0);

  if (count === 0) return null;

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="fixed bottom-6 left-1/2 z-40 flex -translate-x-1/2 items-center gap-3 rounded-full bg-orange-500 px-6 py-3 text-white shadow-lg hover:bg-orange-600"
      >
        <span className="flex h-6 w-6 items-center justify-center rounded-full bg-white text-sm font-bold text-orange-500">
          {count}
        </span>
        <span className="font-semibold">Ver carrinho</span>
        <span className="font-bold">R$ {total().toFixed(2).replace('.', ',')}</span>
      </button>

      {open && <CartDrawer slug={slug} onClose={() => setOpen(false)} />}
    </>
  );
}
