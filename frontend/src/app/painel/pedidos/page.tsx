'use client';
import { useCallback, useEffect, useRef, useState } from 'react';
import {
  getOrders, acceptOrder, startDeliveryOrder, completeOrder, cancelOrder,
  getPainelToken, OrderData,
} from '@/lib/painelApi';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { RefreshCw } from 'lucide-react';

// ── Helpers ──────────────────────────────────────────────────────────────────

function fmt(n: number) {
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function timeAgo(iso: string) {
  const diffMs = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diffMs / 60_000);
  if (mins < 1) return 'agora';
  if (mins < 60) return `${mins} min atrás`;
  return `${Math.floor(mins / 60)}h atrás`;
}

const STATUS_LABEL: Record<string, string> = {
  AwaitingConfirmation: 'Aguardando confirmação',
  InPreparation:        'Em preparo',
  InDelivery:           'Saiu para entrega',
};

const STATUS_COLOR: Record<string, string> = {
  AwaitingConfirmation: 'bg-yellow-100 text-yellow-800 border-yellow-200',
  InPreparation:        'bg-blue-100 text-blue-800 border-blue-200',
  InDelivery:           'bg-purple-100 text-purple-800 border-purple-200',
};

const STATUS_ORDER = ['AwaitingConfirmation', 'InPreparation', 'InDelivery'];

// ── Order Card ───────────────────────────────────────────────────────────────

function OrderCard({
  order,
  onAction,
}: {
  order: OrderData;
  onAction: (id: string, action: string) => Promise<void>;
}) {
  const [busy, setBusy] = useState(false);

  async function handle(action: string) {
    setBusy(true);
    try { await onAction(order.id, action); }
    finally { setBusy(false); }
  }

  return (
    <Card className={`border ${STATUS_COLOR[order.status] ?? ''}`}>
      <CardHeader className="pb-2 pt-4 px-4 flex flex-row items-center justify-between space-y-0">
        <div className="flex items-center gap-2">
          <CardTitle className="text-sm font-mono text-muted-foreground">
            #{order.id.slice(0, 8).toUpperCase()}
          </CardTitle>
          <span className="text-xs text-muted-foreground">{timeAgo(order.createdAt)}</span>
        </div>
        <span className="text-base font-bold">{fmt(order.total)}</span>
      </CardHeader>

      <CardContent className="px-4 pb-4 space-y-3">
        {/* Items */}
        <ul className="text-sm space-y-0.5">
          {order.items.map((item, i) => (
            <li key={i} className="flex justify-between">
              <span>{item.quantity}× {item.productName}</span>
              <span className="text-muted-foreground">{fmt(item.unitPrice)}</span>
            </li>
          ))}
        </ul>

        {/* Customer note */}
        {order.customerNote && (
          <p className="text-xs text-muted-foreground italic border-t pt-2">
            Obs: {order.customerNote}
          </p>
        )}

        {/* Actions */}
        <div className="flex gap-2 pt-1 border-t flex-wrap">
          {order.status === 'AwaitingConfirmation' && (
            <>
              <Button size="sm" disabled={busy} onClick={() => handle('accept')}
                className="bg-emerald-500 hover:bg-emerald-600 text-white">
                ✓ Aceitar
              </Button>
              <Button size="sm" variant="outline" disabled={busy} onClick={() => handle('cancel')}
                className="text-destructive hover:bg-destructive/10 border-destructive/30">
                Cancelar
              </Button>
            </>
          )}
          {order.status === 'InPreparation' && (
            <>
              <Button size="sm" disabled={busy} onClick={() => handle('start-delivery')}
                className="bg-blue-500 hover:bg-blue-600 text-white">
                🛵 Saiu para entrega
              </Button>
              <Button size="sm" variant="outline" disabled={busy} onClick={() => handle('cancel')}
                className="text-destructive hover:bg-destructive/10 border-destructive/30">
                Cancelar
              </Button>
            </>
          )}
          {order.status === 'InDelivery' && (
            <Button size="sm" disabled={busy} onClick={() => handle('complete')}
              className="bg-emerald-500 hover:bg-emerald-600 text-white">
              ✓ Marcar como entregue
            </Button>
          )}
          {busy && <span className="text-xs text-muted-foreground self-center">Salvando...</span>}
        </div>
      </CardContent>
    </Card>
  );
}

// ── Main Page ────────────────────────────────────────────────────────────────

export default function PedidosPage() {
  const [orders, setOrders]       = useState<OrderData[]>([]);
  const [loading, setLoading]     = useState(true);
  const [newCount, setNewCount]   = useState(0);
  const esRef = useRef<EventSource | null>(null);

  const fetchOrders = useCallback(async () => {
    try {
      const data = await getOrders('active');
      setOrders(data);
    } catch (e) {
      console.error('Erro ao buscar pedidos', e);
    } finally {
      setLoading(false);
    }
  }, []);

  // Initial load
  useEffect(() => { fetchOrders(); }, [fetchOrders]);

  // SSE connection
  useEffect(() => {
    const token = getPainelToken();
    if (!token) return;

    const url = `/painel-api/pedidos/stream?token=${encodeURIComponent(token)}`;
    const es = new EventSource(url);
    esRef.current = es;

    es.addEventListener('order-update', () => {
      setNewCount((n) => n + 1);
      fetchOrders();
    });

    es.onerror = () => {
      // EventSource auto-reconnects; nothing to do
    };

    return () => { es.close(); esRef.current = null; };
  }, [fetchOrders]);

  async function handleAction(orderId: string, action: string) {
    const actionMap: Record<string, (id: string) => Promise<OrderData>> = {
      accept:         acceptOrder,
      'start-delivery': startDeliveryOrder,
      complete:       completeOrder,
      cancel:         cancelOrder,
    };

    const fn = actionMap[action];
    if (!fn) return;

    const updated = await fn(orderId);
    setOrders((prev) => {
      // Remove from active list if completed/cancelled, otherwise update
      if (updated.status === 'Delivered' || updated.status === 'Cancelled')
        return prev.filter((o) => o.id !== orderId);
      return prev.map((o) => o.id === orderId ? updated : o);
    });
  }

  const grouped = STATUS_ORDER.reduce<Record<string, OrderData[]>>((acc, s) => {
    acc[s] = orders.filter((o) => o.status === s);
    return acc;
  }, {});

  const totalActive = orders.length;

  if (loading) return <p className="text-muted-foreground">Carregando...</p>;

  return (
    <div className="space-y-6 max-w-4xl">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Pedidos</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            {totalActive === 0
              ? 'Nenhum pedido ativo'
              : `${totalActive} pedido${totalActive > 1 ? 's' : ''} ativo${totalActive > 1 ? 's' : ''}`}
            {newCount > 0 && (
              <span className="ml-2 text-orange-500 font-medium">
                +{newCount} novo{newCount > 1 ? 's' : ''} via SSE
              </span>
            )}
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={() => { setNewCount(0); fetchOrders(); }}
          className="gap-1.5">
          <RefreshCw className="h-3.5 w-3.5" />
          Atualizar
        </Button>
      </div>

      {totalActive === 0 && (
        <div className="rounded-lg border-2 border-dashed border-muted p-16 text-center">
          <p className="text-muted-foreground text-lg">Nenhum pedido ativo no momento.</p>
          <p className="text-sm text-muted-foreground mt-1">Novos pedidos aparecerão aqui automaticamente.</p>
        </div>
      )}

      {/* Grouped by status */}
      {STATUS_ORDER.map((status) => {
        const group = grouped[status];
        if (!group || group.length === 0) return null;
        return (
          <section key={status}>
            <div className="flex items-center gap-2 mb-3">
              <h2 className="font-semibold text-sm uppercase tracking-wide text-muted-foreground">
                {STATUS_LABEL[status]}
              </h2>
              <Badge variant="secondary" className="text-xs">{group.length}</Badge>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {group.map((order) => (
                <OrderCard key={order.id} order={order} onAction={handleAction} />
              ))}
            </div>
          </section>
        );
      })}
    </div>
  );
}
