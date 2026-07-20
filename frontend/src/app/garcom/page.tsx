'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  getGarcomMesas,
  getGarcomChamadas,
  atenderChamadaGarcom,
  abrirComanda,
  getGarcomToken,
  type GarcomTable,
  type GarcomChamada,
} from '@/lib/garcomApi';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { BellRing, Plus, RefreshCw } from 'lucide-react';

const brl = (n: number) => `R$ ${n.toFixed(2).replace('.', ',')}`;

function elapsed(iso: string) {
  const mins = Math.floor((Date.now() - new Date(iso).getTime()) / 60_000);
  if (mins < 1) return 'agora';
  if (mins < 60) return `${mins} min`;
  return `${Math.floor(mins / 60)}h${mins % 60}`;
}

function playBeep() {
  try {
    const Ctx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    const ctx = new Ctx();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.frequency.value = 880;
    gain.gain.setValueAtTime(0.15, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.4);
    osc.start();
    osc.stop(ctx.currentTime + 0.4);
    osc.onended = () => ctx.close();
  } catch {
    /* ignora */
  }
}

export default function GarcomHome() {
  const router = useRouter();
  const [mesas, setMesas] = useState<GarcomTable[]>([]);
  const [chamadas, setChamadas] = useState<GarcomChamada[]>([]);
  const [tab, setTab] = useState<'minhas' | 'todas'>('todas');
  const [loading, setLoading] = useState(true);
  const [openingId, setOpeningId] = useState<string | null>(null);
  const esRef = useRef<EventSource | null>(null);

  const load = useCallback(async () => {
    try {
      const [m, c] = await Promise.all([getGarcomMesas(), getGarcomChamadas()]);
      setMesas(m);
      setChamadas(c);
    } catch {
      /* ignora refresh */
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    const token = getGarcomToken();
    if (!token) return;
    const es = new EventSource(`/garcom-api/stream?token=${encodeURIComponent(token)}`);
    esRef.current = es;
    es.addEventListener('mesa-update', (e) => {
      try {
        if (JSON.parse((e as MessageEvent).data)?.type === 'waiter-call') playBeep();
      } catch {
        /* ignora */
      }
      load();
    });
    return () => {
      es.close();
      esRef.current = null;
    };
  }, [load]);

  async function handleAbrir(tableId: string) {
    setOpeningId(tableId);
    try {
      await abrirComanda(tableId);
      router.push(`/garcom/mesa/${tableId}`);
    } catch {
      setOpeningId(null);
      await load();
    }
  }

  async function handleAtender(id: string) {
    setChamadas((prev) => prev.filter((c) => c.id !== id));
    try {
      await atenderChamadaGarcom(id);
    } finally {
      await load();
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando mesas…</p>;

  const minhas = mesas.filter((m) => m.isMine);
  const visible = tab === 'minhas' ? minhas : mesas;

  return (
    <div>
      {/* Chamadas de garçom */}
      {chamadas.length > 0 && (
        <div className="mb-4 rounded-xl border-2 border-red-300 bg-red-50 p-3">
          <div className="mb-2 flex items-center gap-2 text-red-700">
            <BellRing className="h-5 w-5 animate-pulse" />
            <span className="font-bold text-sm">
              {chamadas.length} {chamadas.length === 1 ? 'chamada' : 'chamadas'}
            </span>
          </div>
          <div className="flex flex-col gap-2">
            {chamadas.map((c) => (
              <div key={c.id} className="flex items-center justify-between gap-2 rounded-lg bg-white px-3 py-2">
                <div className="min-w-0 text-sm">
                  <span className="font-semibold">Mesa {c.tableNumber}</span>
                  {c.reason && <span className="ml-1 text-gray-600">· {c.reason}</span>}
                  <span className="ml-1 text-xs text-gray-400">{elapsed(c.createdAt)}</span>
                </div>
                <Button size="sm" onClick={() => handleAtender(c.id)} className="shrink-0 bg-red-500 text-white hover:bg-red-600">
                  Atender
                </Button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tabs */}
      <div className="mb-4 flex items-center justify-between">
        <div className="inline-flex rounded-full bg-gray-200 p-1 text-sm">
          <button
            onClick={() => setTab('todas')}
            className={`rounded-full px-4 py-1.5 font-medium ${tab === 'todas' ? 'bg-white shadow' : 'text-gray-600'}`}
          >
            Todas as mesas
          </button>
          <button
            onClick={() => setTab('minhas')}
            className={`rounded-full px-4 py-1.5 font-medium ${tab === 'minhas' ? 'bg-white shadow' : 'text-gray-600'}`}
          >
            Minhas ({minhas.length})
          </button>
        </div>
        <button onClick={load} className="rounded-full p-2 text-gray-500 hover:bg-gray-100" title="Atualizar">
          <RefreshCw className="h-4 w-4" />
        </button>
      </div>

      {/* Grid de mesas */}
      {visible.length === 0 ? (
        <p className="text-muted-foreground">
          {tab === 'minhas' ? 'Você ainda não abriu comandas.' : 'Nenhuma mesa cadastrada.'}
        </p>
      ) : (
        <div className="grid grid-cols-2 gap-3">
          {visible.map((m) => {
            const occupied = m.status === 'Occupied';
            return (
              <div
                key={m.id}
                className={`rounded-xl border p-3 ${
                  m.hasPendingCall ? 'border-red-300 bg-red-50 ring-2 ring-red-300' : occupied ? 'border-orange-300 bg-orange-50' : 'bg-white'
                }`}
              >
                <div className="mb-1 flex items-center justify-between">
                  <span className="text-lg font-bold">Mesa {m.number}</span>
                  {m.hasPendingCall ? (
                    <Badge className="animate-pulse bg-red-500 text-white">Chamando</Badge>
                  ) : m.isMine ? (
                    <Badge className="bg-orange-500 text-white">Minha</Badge>
                  ) : (
                    <Badge variant={occupied ? 'default' : 'secondary'}>{occupied ? 'Ocupada' : 'Livre'}</Badge>
                  )}
                </div>

                {occupied ? (
                  <>
                    <p className="text-xs text-gray-500">{m.openedByName ?? '—'}</p>
                    <p className="mb-2 font-semibold text-orange-600">{brl(m.sessionTotal)}</p>
                    <Button size="sm" variant="outline" className="w-full" onClick={() => router.push(`/garcom/mesa/${m.id}`)}>
                      Ver comanda
                    </Button>
                  </>
                ) : (
                  <>
                    <p className="mb-2 text-xs text-muted-foreground">Livre</p>
                    <Button
                      size="sm"
                      className="w-full bg-orange-500 text-white hover:bg-orange-600"
                      disabled={openingId === m.id}
                      onClick={() => handleAbrir(m.id)}
                    >
                      <Plus className="mr-1 h-4 w-4" />
                      {openingId === m.id ? 'Abrindo…' : 'Abrir comanda'}
                    </Button>
                  </>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
