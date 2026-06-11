'use client';
import { useRouter } from 'next/navigation';
import { useCart } from '@/store/cart';

export default function CartDrawer({
  slug,
  onClose,
}: {
  slug: string;
  onClose: () => void;
}) {
  const router = useRouter();
  const { items, updateQuantity, removeItem, total } = useCart();

  function goToCheckout() {
    onClose();
    router.push(`/${slug}/checkout`);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40">
      <div className="w-full max-w-lg rounded-t-2xl bg-white">
        <div className="flex items-center justify-between border-b p-4">
          <h2 className="text-lg font-bold">Seu pedido</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">✕</button>
        </div>

        <div className="max-h-80 overflow-y-auto p-4">
          {items.map((item) => (
            <div key={item.productId} className="flex items-center justify-between py-3">
              <div className="flex-1">
                <p className="font-medium">{item.name}</p>
                <p className="text-sm text-orange-500">
                  R$ {((item.price + item.complementsTotal) * item.quantity).toFixed(2).replace('.', ',')}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => updateQuantity(item.productId, item.quantity - 1)}
                  className="flex h-7 w-7 items-center justify-center rounded-full border"
                >
                  −
                </button>
                <span className="w-5 text-center text-sm font-semibold">{item.quantity}</span>
                <button
                  onClick={() => updateQuantity(item.productId, item.quantity + 1)}
                  className="flex h-7 w-7 items-center justify-center rounded-full border"
                >
                  +
                </button>
                <button
                  onClick={() => removeItem(item.productId)}
                  className="ml-1 text-gray-400 hover:text-red-500"
                >
                  🗑
                </button>
              </div>
            </div>
          ))}
        </div>

        <div className="border-t p-4">
          <div className="mb-4 flex items-center justify-between font-bold">
            <span>Total</span>
            <span>R$ {total().toFixed(2).replace('.', ',')}</span>
          </div>
          <button
            onClick={goToCheckout}
            className="w-full rounded-full bg-orange-500 py-3 font-semibold text-white hover:bg-orange-600"
          >
            Ir para o checkout
          </button>
        </div>
      </div>
    </div>
  );
}
