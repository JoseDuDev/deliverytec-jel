'use client';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { getMyOrders, getToken, MyOrderSummary } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

const STATUS_LABEL: Record<string, string> = {
  PendingPayment:        'Aguardando pagamento',
  AwaitingConfirmation:  'Confirmado',
  InPreparation:         'Em preparo',
  InDelivery:            'Saiu para entrega',
  Delivered:             'Entregue',
  Cancelled:             'Cancelado',
};

const STATUS_VARIANT: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  PendingPayment:       'outline',
  AwaitingConfirmation: 'secondary',
  InPreparation:        'secondary',
  InDelivery:           'default',
  Delivered:            'default',
  Cancelled:            'destructive',
};

function fmt(n: number) {
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

export default function ContaPage() {
  const router = useRouter();
  const [orders, setOrders] = useState<MyOrderSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!getToken()) {
      router.replace('/conta/login');
      return;
    }
    getMyOrders()
      .then(setOrders)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [router]);

  function handleLogout() {
    localStorage.removeItem('delify_token');
    router.push('/');
  }

  if (loading) return <main className="min-h-screen flex items-center justify-center"><p className="text-muted-foreground">Carregando...</p></main>;

  return (
    <main className="mx-auto max-w-lg px-4 py-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight">Minha conta</h1>
        <Button variant="outline" size="sm" onClick={handleLogout}>
          Sair
        </Button>
      </div>

      {error && (
        <div className="rounded-md bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      <div>
        <h2 className="text-base font-semibold mb-3">Meus pedidos</h2>

        {orders.length === 0 ? (
          <div className="rounded-lg border-2 border-dashed border-muted p-10 text-center">
            <p className="text-muted-foreground">Nenhum pedido encontrado.</p>
            <p className="text-sm text-muted-foreground mt-1">Seus pedidos aparecerão aqui após a compra.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {orders.map((order) => (
              <Card key={order.id}>
                <CardHeader className="pb-2 pt-4 px-4 flex flex-row items-start justify-between space-y-0">
                  <div>
                    <CardTitle className="text-sm font-semibold">
                      {order.establishment?.name ?? 'Estabelecimento'}
                    </CardTitle>
                    <p className="text-xs text-muted-foreground mt-0.5">{fmtDate(order.createdAt)}</p>
                  </div>
                  <Badge variant={STATUS_VARIANT[order.status] ?? 'outline'}>
                    {STATUS_LABEL[order.status] ?? order.status}
                  </Badge>
                </CardHeader>
                <CardContent className="px-4 pb-4 space-y-2">
                  <ul className="text-sm text-muted-foreground space-y-0.5">
                    {order.items.map((item, i) => (
                      <li key={i}>{item.quantity}× {item.productName}</li>
                    ))}
                  </ul>
                  <div className="flex items-center justify-between pt-1 border-t">
                    <span className="text-sm font-bold">{fmt(order.total)}</span>
                    <Link
                      href={`/pedido/${order.id}`}
                      className="text-xs text-orange-500 hover:underline"
                    >
                      Rastrear pedido →
                    </Link>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </main>
  );
}
