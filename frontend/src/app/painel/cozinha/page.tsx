'use client';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  getOrders, acceptOrder, startDeliveryOrder, completeOrder, serveOrder,
  getPainelToken, OrderData,
} from '@/lib/painelApi';
import { Maximize2, Minimize2, ArrowLeft, RefreshCw } from 'lucide-react';

// ── Helpers ───────────────────────────────────────────────────────────────────

function fmt(n: number) {
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function elapsed(iso: string) {
  const mins = Math.floor((Date.now() - new Date(iso).getTime()) / 60_000);
  if (mins < 1) return '< 1 min';
  if (mins < 60) return `${mins} min`;
  return `${Math.floor(mins / 60)}h ${mins % 60}min`;
}

function useNow() {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  return now;
}

// ── Column config ─────────────────────────────────────────────────────────────

const COLUMNS = [
  {
    key: 'AwaitingConfirmation',
    label: 'Aguardando',
    bg: 'bg-yellow-400',
    text: 'text-yellow-900',
    card: 'border-yellow-300 bg-yellow-50',
    action: { label: '✓ Aceitar', fn: acceptOrder, cls: 'bg-yellow-500 hover:bg-yellow-600 text-white' },
  },
  {
    key: 'InPreparation',
    label: 'Em Preparo',
    bg: 'bg-blue-500',
    text: 'text-white',
    card: 'border-blue-200 bg-blue-50',
    action: { label: '🛵 Saiu', fn: startDeliveryOrder, cls: 'bg-blue-500 hover:bg-blue-600 text-white' },
    // Pedido de mesa não tem entrega: o runner leva o prato e encerra o pedido.
    dineinAction: { label: '✅ Entregue na mesa', fn: serveOrder, cls: 'bg-purple-600 hover:bg-purple-700 text-white' },
  },
  {
    key: 'InDelivery',
    label: 'Saiu para Entrega',
    bg: 'bg-emerald-500',
    text: 'text-white',
    card: 'border-emerald-200 bg-emerald-50',
    action: { label: '✓ Entregue', fn: completeOrder, cls: 'bg-emerald-500 hover:bg-emerald-600 text-white' },
  },
] as const;

// ── Order Card ────────────────────────────────────────────────────────────────

function KitchenCard({
  order,
  action,
  cardCls,
  onDone,
}: {
  order: OrderData;
  action: { label: string; fn: (id: string) => Promise<OrderData>; cls: string };
  cardCls: string;
  onDone: (updated: OrderData) => void;
}) {
  const [busy, setBusy] = useState(false);
  const age = elapsed(order.createdAt);

  async function handle() {
    setBusy(true);
    try { onDone(await action.fn(order.id)); }
    finally { setBusy(false); }
  }

  return (
    <div className={`rounded-2xl border-2 ${cardCls} p-4 flex flex-col gap-3`}>
      {/* Header */}
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2 min-w-0">
          {order.tableNumber ? (
            <span className="shrink-0 rounded-md bg-purple-600 px-2 py-0.5 text-base font-black uppercase tracking-wide text-white">
              🍽 Mesa {order.tableNumber}
            </span>
          ) : (
            <span className="shrink-0 rounded-md bg-gray-700 px-2 py-0.5 text-xs font-bold uppercase tracking-wide text-white">
              🛵 Entrega
            </span>
          )}
          <span className="font-mono text-sm font-bold tracking-widest text-muted-foreground truncate">
            #{order.id.slice(0, 6).toUpperCase()}
          </span>
        </div>
        <span className="shrink-0 text-sm font-semibold text-muted-foreground bg-white/70 rounded-full px-3 py-0.5">
          {age}
        </span>
      </div>

      {/* Items */}
      <ul className="space-y-1">
        {order.items.map((item, i) => (
          <li key={i} className="flex gap-2 text-base font-medium leading-tight">
            <span className="text-2xl font-bold w-8 shrink-0 text-center">{item.quantity}</span>
            <span className="pt-0.5">{item.productName}</span>
          </li>
        ))}
      </ul>

      {/* Note */}
      {order.customerNote && (
        <p className="text-sm italic text-muted-foreground border-t pt-2">
          Obs: {order.customerNote}
        </p>
      )}

      {/* Footer */}
      <div className="flex items-center justify-between border-t pt-2">
        <span className="text-sm text-muted-foreground font-medium">{fmt(order.total)}</span>
        <button
          onClick={handle}
          disabled={busy}
          className={`rounded-full px-4 py-1.5 text-sm font-semibold transition-opacity ${action.cls} disabled:opacity-50`}
        >
          {busy ? '...' : action.label}
        </button>
      </div>
    </div>
  );
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export default function CozinhaPage() {
  const router = useRouter();
  const now = useNow();
  const [orders, setOrders] = useState<OrderData[]>([]);
  const [loading, setLoading] = useState(true);
  const [fullscreen, setFullscreen] = useState(false);
  const esRef = useRef<EventSource | null>(null);

  const fetchOrders = useCallback(async () => {
    try { setOrders(await getOrders('active')); }
    catch { /* silently ignore refresh errors */ }
    finally { setLoading(false); }
  }, []);

  useEffect(() => {
    if (!getPainelToken()) { router.replace('/painel/login'); return; }
    fetchOrders();
  }, [fetchOrders, router]);

  // SSE
  useEffect(() => {
    const token = getPainelToken();
    if (!token) return;
    const es = new EventSource(`/painel-api/pedidos/stream?token=${encodeURIComponent(token)}`);
    esRef.current = es;
    es.addEventListener('order-update', fetchOrders);
    return () => { es.close(); esRef.current = null; };
  }, [fetchOrders]);

  // Fullscreen API
  function toggleFullscreen() {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen();
      setFullscreen(true);
    } else {
      document.exitFullscreen();
      setFullscreen(false);
    }
  }

  useEffect(() => {
    const handler = () => setFullscreen(!!document.fullscreenElement);
    document.addEventListener('fullscreenchange', handler);
    return () => document.removeEventListener('fullscreenchange', handler);
  }, []);

  function handleDone(updated: OrderData) {
    setOrders((prev) => {
      if (updated.status === 'Delivered' || updated.status === 'Cancelled')
        return prev.filter((o) => o.id !== updated.id);
      return prev.map((o) => o.id === updated.id ? updated : o);
    });
  }

  const grouped = Object.fromEntries(
    COLUMNS.map((col) => [col.key, orders.filter((o) => o.status === col.key)])
  );

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-900 flex items-center justify-center">
        <p className="text-white text-xl">Carregando...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-900 flex flex-col select-none">
      {/* Top bar */}
      <header className="bg-gray-800 border-b border-gray-700 px-4 py-3 flex items-center justify-between shrink-0">
        <div className="flex items-center gap-3">
          <button
            onClick={() => router.push('/painel')}
            className="text-gray-400 hover:text-white transition-colors p-1 rounded"
          >
            <ArrowLeft className="h-5 w-5" />
          </button>
          <span className="text-orange-400 font-bold text-lg">Delify</span>
          <span className="text-gray-400 text-sm">· Cozinha</span>
        </div>

        <div className="flex items-center gap-3">
          <span className="text-white font-mono text-xl font-semibold tabular-nums">
            {now.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}
          </span>
          <button
            onClick={() => { setLoading(true); fetchOrders(); }}
            className="text-gray-400 hover:text-white transition-colors p-1.5 rounded"
            title="Atualizar"
          >
            <RefreshCw className="h-4 w-4" />
          </button>
          <button
            onClick={toggleFullscreen}
            className="text-gray-400 hover:text-white transition-colors p-1.5 rounded"
            title={fullscreen ? 'Sair do fullscreen' : 'Fullscreen'}
          >
            {fullscreen ? <Minimize2 className="h-4 w-4" /> : <Maximize2 className="h-4 w-4" />}
          </button>
        </div>
      </header>

      {/* Columns */}
      <div className="flex-1 grid grid-cols-3 gap-0 min-h-0">
        {COLUMNS.map((col) => {
          const colOrders = grouped[col.key] ?? [];
          return (
            <div key={col.key} className="flex flex-col border-r border-gray-700 last:border-r-0">
              {/* Column header */}
              <div className={`${col.bg} ${col.text} px-4 py-3 flex items-center justify-between shrink-0`}>
                <span className="font-bold text-base uppercase tracking-wide">{col.label}</span>
                <span className={`text-2xl font-black`}>{colOrders.length}</span>
              </div>

              {/* Cards */}
              <div className="flex-1 overflow-y-auto p-3 space-y-3">
                {colOrders.length === 0 ? (
                  <div className="flex h-32 items-center justify-center">
                    <p className="text-gray-600 text-sm">Nenhum pedido</p>
                  </div>
                ) : (
                  colOrders.map((order) => (
                    <KitchenCard
                      key={order.id}
                      order={order}
                      action={
                        order.type === 'Dinein' && 'dineinAction' in col
                          ? col.dineinAction
                          : col.action
                      }
                      cardCls={col.card}
                      onDone={handleDone}
                    />
                  ))
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
