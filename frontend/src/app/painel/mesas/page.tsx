'use client';

import { useCallback, useEffect, useState } from 'react';
import { QRCodeCanvas } from 'qrcode.react';
import {
  getMesas,
  getDashboard,
  createMesa,
  renameMesa,
  regenerateMesaQr,
  liberarMesa,
  deleteMesa,
  type MesaData,
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
import { Plus, QrCode, RefreshCw, Trash2, DoorOpen, Copy, Check } from 'lucide-react';

const brl = (n: number) => `R$ ${n.toFixed(2).replace('.', ',')}`;

export default function MesasPage() {
  const [mesas, setMesas] = useState<MesaData[]>([]);
  const [slug, setSlug] = useState('');
  const [origin, setOrigin] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [newNumber, setNewNumber] = useState('');
  const [creating, setCreating] = useState(false);
  const [qrMesa, setQrMesa] = useState<MesaData | null>(null);

  const load = useCallback(async () => {
    try {
      const [list, dash] = await Promise.all([getMesas(), getDashboard()]);
      setMesas(list);
      setSlug(dash.slug);
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
                  occupied ? 'border-orange-300 bg-orange-50' : 'bg-white'
                }`}
              >
                <div className="mb-2 flex items-center justify-between">
                  <span className="text-lg font-bold">Mesa {m.number}</span>
                  <Badge variant={occupied ? 'default' : 'secondary'}>
                    {occupied ? 'Ocupada' : 'Livre'}
                  </Badge>
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
