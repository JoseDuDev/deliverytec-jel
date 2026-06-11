'use client';
import { useState } from 'react';
import { useCart } from '@/store/cart';
import { guestSession, placeOrder, saveToken, PlaceOrderResponse } from '@/lib/api';
import GuestForm, { CheckoutFormData } from '@/components/checkout/GuestForm';
import PixPanel from '@/components/checkout/PixPanel';

export default function CheckoutPage() {
  const { items, establishmentId, total, clear } = useCart();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [orderResult, setOrderResult] = useState<PlaceOrderResponse | null>(null);

  async function handleSubmit(data: CheckoutFormData) {
    if (!establishmentId || items.length === 0) return;
    setLoading(true);
    setError(null);

    try {
      if (!localStorage.getItem('delify_token')) {
        const session = await guestSession(data.name, data.phone);
        saveToken(session.token);
      }

      const result = await placeOrder({
        establishmentId,
        items: items.map((i) => ({
          productId: i.productId,
          quantity: i.quantity,
          complementIds: i.complementIds,
        })),
        customer: { name: data.name, phone: data.phone, cpf: data.cpf },
        address: {
          street: data.street,
          number: data.number,
          neighborhood: data.neighborhood,
          city: data.city,
          complement: data.complement,
        },
      });

      clear();
      setOrderResult(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro desconhecido');
    } finally {
      setLoading(false);
    }
  }

  if (orderResult) {
    return (
      <main className="mx-auto max-w-lg px-4 py-8">
        <PixPanel
          orderId={orderResult.orderId}
          qrCode={orderResult.pix.qrCode}
          copyPaste={orderResult.pix.copyPaste}
          expiresAt={orderResult.pix.expiresAt}
        />
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-lg px-4 py-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Checkout</h1>
        <p className="text-gray-500">
          {items.length} {items.length === 1 ? 'item' : 'itens'} ·{' '}
          R$ {total().toFixed(2).replace('.', ',')}
        </p>
      </div>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 p-4 text-sm text-red-600">{error}</div>
      )}

      <GuestForm onSubmit={handleSubmit} loading={loading} />
    </main>
  );
}
