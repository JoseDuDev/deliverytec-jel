'use client';
import { useState } from 'react';
import { useCart } from '@/store/cart';
import { guestSession, placeOrder, saveToken, PlaceOrderResponse } from '@/lib/api';
import GuestForm, { CheckoutFormData } from '@/components/checkout/GuestForm';
import PixPanel from '@/components/checkout/PixPanel';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';

export default function CheckoutPage() {
  const { items, establishmentId, subtotal, total, deliveryFee, clear } = useCart();
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
    <main className="mx-auto max-w-lg px-4 py-8 space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Checkout</h1>
        <p className="text-muted-foreground text-sm mt-1">
          {items.length} {items.length === 1 ? 'item' : 'itens'} ·{' '}
          R$ {total().toFixed(2).replace('.', ',')}
        </p>
      </div>


      {error && (
        <div className="rounded-md bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Resumo do pedido</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2 pb-4">
          {items.map((item) => (
            <div key={item.productId} className="flex justify-between text-sm">
              <span className="text-muted-foreground">
                {item.quantity}× {item.name}
              </span>
              <span className="font-medium">
                R$ {((item.price + item.complementsTotal) * item.quantity).toFixed(2).replace('.', ',')}
              </span>
            </div>
          ))}
          <Separator />
          {deliveryFee > 0 && (
            <>
              <div className="flex justify-between text-sm text-muted-foreground">
                <span>Subtotal</span>
                <span>R$ {subtotal().toFixed(2).replace('.', ',')}</span>
              </div>
              <div className="flex justify-between text-sm text-muted-foreground">
                <span>Taxa de entrega</span>
                <span>R$ {deliveryFee.toFixed(2).replace('.', ',')}</span>
              </div>
              <Separator />
            </>
          )}
          <div className="flex justify-between font-bold">
            <span>Total</span>
            <span>R$ {total().toFixed(2).replace('.', ',')}</span>
          </div>
        </CardContent>
      </Card>

      <GuestForm onSubmit={handleSubmit} loading={loading} />
    </main>
  );
}
