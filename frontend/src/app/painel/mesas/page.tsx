'use client';

import { useCallback, useEffect, useRef, useState } from 'react';
import { QRCodeCanvas } from 'qrcode.react';
import {
  getMesas,
  getDashboard,
  getChamadas,
  atenderChamada,
  getPainelToken,
  createMesa,
  renameMesa,
  regenerateMesaQr,
  liberarMesa,
  deleteMesa,
  type MesaData,
  type ChamadaData,
} from '@/lib/painelApi';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import { Plus, QrCode, RefreshCw, Trash2, DoorOpen, Copy, Check, BellRing } from 'lucide-react';

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
    /* áudio indisponível — ignora */
  }
}

export default function MesasPage() {
  const [mesas, setMesas] = useState<MesaData[]>([]);
  const [chamadas, setChamadas] = useState<ChamadaData[]>([]);
  const [slug, setSlug] = useState('');
  const [origin, setOrigin] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [newNumber, setNewNumber] = useState('');
  const [creating, setCreating] = useState(false);
  const [qrMesa, setQrMesa] = useState<MesaData | null>(null);
  const esRef = useRef<EventSource | null>(null);

  const load = useCallback(async () => {
    try {
      const [list, dash, calls] = await Promise.all([getMesas(), getDashboard(), getChamadas()]);
      setMesas(list);
      setSlug(dash.slug);
      setChamadas(calls);
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar as mesas');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    setOrigin(window.location.origin);
    load();
  }, [load]);

  // SSE — chamadas de garçom e atualizações de comanda em tempo real.
  useEffect(() => {
    const token = getPainelToken();
    if (!token) return;
    const es = new EventSource(`/painel-api/mesas/stream?token=${encodeURIComponent(token)}`);
    esRef.current = es;
    es.addEventListener('mesa-update', (e) => {
      try {
        const data = JSON.parse((e as MessageEvent).data);
        if (data?.type === 'waiter-call') playBeep();
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

  async function handleAtender(id: string) {
    setChamadas((prev) => prev.filter((c) => c.id !== id));
    try {
      await atenderChamada(id);
    } finally {
      await load();
    }
  }

  const mesaUrl = (token: string) => `${origin}/${slug}/mesa/${token}`;

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!newNumber.trim()) return;
    setCreating(true);
    setError(null);
    try {
      await createMesa(newNumber.trim());
      setNewNumber('');
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao criar a mesa');
    } finally {
      setCreating(false);
    }
  }

  async function handleLiberar(id: string) {
    if (!confirm('Liberar esta mesa? A comanda atual será encerrada.')) return;
    await liberarMesa(id);
    await load();
  }

  async function handleDelete(id: string) {
    if (!confirm('Excluir esta mesa permanentemente?')) return;
    try {
      await deleteMesa(id);
      setQrMesa(null);
      await load();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao excluir');
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando mesas…</p>;

  return (
    <div className="mx-auto max-w-5xl">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Mesas</h1>
          <p className="text-sm text-muted-foreground">
            Gere o QR Code de cada mesa e acompanhe as comandas abertas.
          </p>
        </div>
      </div>

      {chamadas.length > 0 && (
        <div className="mb-6 rounded-xl border-2 border-red-300 bg-red-50 p-4">
          <div className="mb-3 flex items-center gap-2 text-red-700">
            <BellRing className="h-5 w-5 animate-pulse" />
            <span className="font-bold">
              {chamadas.length} {chamadas.length === 1 ? 'chamada de garçom' : 'chamadas de garçom'}
            </span>
          </div>
          <div className="flex flex-col gap-2">
            {chamadas.map((c) => (
              <div
                key={c.id}
                className="flex items-center justify-between gap-3 rounded-lg bg-white px-3 py-2"
              >
                <div className="min-w-0">
                  <span className="font-semibold">Mesa {c.tableNumber}</span>
                  {c.reason && <span className="ml-2 text-sm text-gray-600">· {c.reason}</span>}
                  <span className="ml-2 text-xs text-gray-400">{elapsed(c.createdAt)}</span>
                </div>
                <Button
                  size="sm"
                  onClick={() => handleAtender(c.id)}
                  className="shrink-0 bg-red-500 text-white hover:bg-red-600"
                >
                  Atender
                </Button>
              </div>
            ))}
          </div>
        </div>
      )}

      <form onSubmit={handleCreate} className="mb-6 flex max-w-sm gap-2">
        <Input
          value={newNumber}
          onChange={(e) => setNewNumber(e.target.value)}
          placeholder="Número/nome da mesa (ex: 12)"
        />
        <Button type="submit" disabled={creating} className="shrink-0">
          <Plus className="mr-1 h-4 w-4" /> Nova mesa
        </Button>
      </form>

      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

      {mesas.length === 0 ? (
        <p className="text-muted-foreground">Nenhuma mesa cadastrada ainda.</p>
      ) : (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          {mesas.map((m) => {
            const occupied = m.status === 'Occupied';
            return (
              <div
                key={m.id}
                className={`rounded-xl border p-4 ${
                  m.hasPendingCall
                    ? 'border-red-300 bg-red-50 ring-2 ring-red-300'
                    : occupied
                      ? 'border-orange-300 bg-orange-50'
                      : 'bg-white'
                }`}
              >
                <div className="mb-2 flex items-center justify-between">
                  <span className="text-lg font-bold">Mesa {m.number}</span>
                  {m.hasPendingCall ? (
                    <Badge className="animate-pulse bg-red-500 text-white">Chamando</Badge>
                  ) : (
                    <Badge variant={occupied ? 'default' : 'secondary'}>
                      {occupied ? 'Ocupada' : 'Livre'}
                    </Badge>
                  )}
                </div>

                {occupied ? (
                  <div className="mb-3 text-sm text-gray-600">
                    <p>
                      {m.orderCount} {m.orderCount === 1 ? 'pedido' : 'pedidos'}
                    </p>
                    <p className="font-semibold text-orange-600">{brl(m.sessionTotal)}</p>
                  </div>
                ) : (
                  <p className="mb-3 text-sm text-muted-foreground">Sem comanda aberta</p>
                )}

                <div className="flex gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    className="flex-1"
                    onClick={() => setQrMesa(m)}
                  >
                    <QrCode className="mr-1 h-4 w-4" /> QR
                  </Button>
                  {occupied && (
                    <Button
                      size="sm"
                      variant="ghost"
                      className="text-gray-500 hover:text-gray-900"
                      onClick={() => handleLiberar(m.id)}
                      title="Liberar mesa"
                    >
                      <DoorOpen className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {qrMesa && (
        <MesaDialog
          mesa={qrMesa}
          url={mesaUrl(qrMesa.qrToken)}
          onClose={() => setQrMesa(null)}
          onChanged={load}
          onDelete={() => handleDelete(qrMesa.id)}
        />
      )}
    </div>
  );
}

function MesaDialog({
  mesa,
  url,
  onClose,
  onChanged,
  onDelete,
}: {
  mesa: MesaData;
  url: string;
  onClose: () => void;
  onChanged: () => Promise<void>;
  onDelete: () => void;
}) {
  const [number, setNumber] = useState(mesa.number);
  const [copied, setCopied] = useState(false);
  const [busy, setBusy] = useState(false);
  const [token, setToken] = useState(mesa.qrToken);
  const currentUrl = url.replace(mesa.qrToken, token);

  async function copy() {
    await navigator.clipboard.writeText(currentUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  async function saveName() {
    if (number.trim() && number.trim() !== mesa.number) {
      setBusy(true);
      try {
        await renameMesa(mesa.id, number.trim());
        await onChanged();
      } finally {
        setBusy(false);
      }
    }
  }

  async function regen() {
    if (!confirm('Gerar um novo QR? O QR antigo deixará de funcionar.')) return;
    setBusy(true);
    try {
      const res = await regenerateMesaQr(mesa.id);
      setToken(res.qrToken);
      await onChanged();
    } finally {
      setBusy(false);
    }
  }

  return (
    <Dialog open onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Mesa {mesa.number}</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col items-center gap-3">
          <div className="rounded-lg bg-white p-3">
            <QRCodeCanvas value={currentUrl} size={180} />
          </div>
          <div className="flex w-full items-center gap-2">
            <Input readOnly value={currentUrl} className="text-xs" />
            <Button size="icon" variant="outline" onClick={copy} title="Copiar link">
              {copied ? <Check className="h-4 w-4 text-green-600" /> : <Copy className="h-4 w-4" />}
            </Button>
          </div>
        </div>

        <div className="mt-2 flex items-end gap-2">
          <div className="flex-1">
            <label className="mb-1 block text-xs text-muted-foreground">Número/nome</label>
            <Input value={number} onChange={(e) => setNumber(e.target.value)} />
          </div>
          <Button variant="outline" onClick={saveName} disabled={busy}>
            Salvar
          </Button>
        </div>

        <DialogFooter className="mt-2 flex-row justify-between gap-2 sm:justify-between">
          <Button variant="ghost" onClick={regen} disabled={busy} className="text-gray-600">
            <RefreshCw className="mr-1 h-4 w-4" /> Novo QR
          </Button>
          <Button variant="ghost" onClick={onDelete} className="text-red-600 hover:text-red-700">
            <Trash2 className="mr-1 h-4 w-4" /> Excluir
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
