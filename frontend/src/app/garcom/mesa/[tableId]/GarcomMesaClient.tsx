'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  getComanda,
  getGarcomCardapio,
  lancarPedido,
  liberarMesaGarcom,
  type GarcomComanda,
  type GarcomMenu,
  type GProduct,
} from '@/lib/garcomApi';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Separator } from '@/components/ui/separator';
import { ArrowLeft, DoorOpen } from 'lucide-react';

const brl = (n: number) => `R$ ${n.toFixed(2).replace('.', ',')}`;

type RoundLine = { key: string; product: GProduct; quantity: number; complementIds: string[]; unitPrice: number };

function lineKey(productId: string, ids: string[]) {
  return `${productId}::${[...ids].sort().join(',')}`;
}

export default function GarcomMesaClient({ tableId }: { tableId: string }) {
  const router = useRouter();
  const [comanda, setComanda] = useState<GarcomComanda | null>(null);
  const [menu, setMenu] = useState<GarcomMenu | null>(null);
  const [loading, setLoading] = useState(true);
  const [round, setRound] = useState<RoundLine[]>([]);
  const [picking, setPicking] = useState<GProduct | null>(null);
  const [sending, setSending] = useState(false);
  const [flash, setFlash] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [c, m] = await Promise.all([getComanda(tableId), getGarcomCardapio()]);
      setComanda(c);
      setMenu(m);
    } finally {
      setLoading(false);
    }
  }, [tableId]);

  useEffect(() => {
    load();
  }, [load]);

  const addLine = useCallback((product: GProduct, quantity: number, complementIds: string[]) => {
    const unitPrice =
      product.price + product.complements.filter((c) => complementIds.includes(c.id)).reduce((s, c) => s + c.price, 0);
    const key = lineKey(product.id, complementIds);
    setRound((prev) => {
      const existing = prev.find((l) => l.key === key);
      if (existing) return prev.map((l) => (l.key === key ? { ...l, quantity: l.quantity + quantity } : l));
      return [...prev, { key, product, quantity, complementIds, unitPrice }];
    });
  }, []);

  const changeQty = (key: string, delta: number) =>
    setRound((prev) => prev.map((l) => (l.key === key ? { ...l, quantity: l.quantity + delta } : l)).filter((l) => l.quantity > 0));

  const roundTotal = useMemo(() => round.reduce((s, l) => s + l.unitPrice * l.quantity, 0), [round]);
  const roundCount = useMemo(() => round.reduce((s, l) => s + l.quantity, 0), [round]);

  async function handleSend() {
    if (round.length === 0) return;
    setSending(true);
    try {
      const resp = await lancarPedido(
        tableId,
        round.map((l) => ({ productId: l.product.id, quantity: l.quantity, complementIds: l.complementIds })),
      );
      setRound([]);
      setComanda((prev) => (prev ? { ...prev, comanda: resp.comanda } : prev));
      setFlash('Pedido lançado na cozinha! 🍳');
      setTimeout(() => setFlash(null), 3000);
    } catch (e) {
      setFlash(e instanceof Error ? e.message : 'Erro ao lançar pedido');
      setTimeout(() => setFlash(null), 3000);
    } finally {
      setSending(false);
    }
  }

  async function handleLiberar() {
    if (!confirm('Liberar a mesa? A comanda será encerrada.')) return;
    await liberarMesaGarcom(tableId);
    router.push('/garcom');
  }

  if (loading) return <p className="text-muted-foreground">Carregando…</p>;

  return (
    <div className="pb-24">
      <div className="mb-3 flex items-center gap-2">
        <button onClick={() => router.push('/garcom')} className="rounded-full p-1.5 text-gray-500 hover:bg-gray-100">
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div className="flex-1">
          <h1 className="text-xl font-bold">Mesa {comanda?.tableNumber}</h1>
          {comanda?.openedByName && <p className="text-xs text-gray-500">Aberta por {comanda.openedByName}</p>}
        </div>
        <Button variant="ghost" size="sm" className="text-gray-500" onClick={handleLiberar} title="Liberar mesa">
          <DoorOpen className="mr-1 h-4 w-4" /> Liberar
        </Button>
      </div>

      {flash && <div className="mb-3 rounded-lg bg-green-50 px-3 py-2 text-sm font-medium text-green-700">{flash}</div>}

      {/* Comanda atual */}
      <div className="mb-4 rounded-xl border bg-white p-3">
        <p className="mb-2 text-sm font-semibold text-gray-700">Comanda</p>
        {comanda && comanda.comanda.items.length > 0 ? (
          <>
            {comanda.comanda.items.map((i, idx) => (
              <div key={idx} className="flex justify-between py-0.5 text-sm">
                <span className="text-gray-700">{i.quantity}× {i.productName}</span>
                <span className="text-gray-600">{brl(i.total)}</span>
              </div>
            ))}
            <Separator className="my-2" />
            <div className="flex justify-between font-semibold">
              <span>Total</span>
              <span className="text-orange-600">{brl(comanda.comanda.total)}</span>
            </div>
          </>
        ) : (
          <p className="text-sm text-muted-foreground">Nenhum item ainda.</p>
        )}
      </div>

      {/* Cardápio para lançar */}
      <p className="mb-2 text-sm font-semibold text-gray-700">Adicionar itens</p>
      {menu?.categories.map((cat) => (
        <section key={cat.id} className="mb-5">
          <h2 className="mb-2 text-sm font-bold text-gray-800">{cat.name}</h2>
          <div className="flex flex-col gap-2">
            {cat.products.map((p) => (
              <div key={p.id} className="flex items-center justify-between gap-3 rounded-lg bg-white p-2.5 shadow-sm">
                <div className="min-w-0">
                  <p className="text-sm font-medium text-gray-800">{p.name}</p>
                  <p className="text-xs font-semibold text-orange-600">{brl(p.price)}</p>
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  className="shrink-0"
                  onClick={() => (p.complements.length > 0 ? setPicking(p) : addLine(p, 1, []))}
                >
                  + Add
                </Button>
              </div>
            ))}
          </div>
        </section>
      ))}

      {/* Rodada atual */}
      {round.length > 0 && (
        <div className="fixed inset-x-0 bottom-0 z-20 border-t bg-white px-4 py-3">
          <div className="mx-auto flex max-w-lg items-center gap-3">
            <div className="flex-1 text-xs text-gray-500">
              {roundCount} {roundCount === 1 ? 'item' : 'itens'} para lançar
            </div>
            <Button onClick={handleSend} disabled={sending} className="rounded-full bg-orange-500 px-6 text-white hover:bg-orange-600">
              {sending ? 'Lançando…' : `Lançar · ${brl(roundTotal)}`}
            </Button>
          </div>
        </div>
      )}

      {picking && (
        <ComplementPicker
          product={picking}
          onClose={() => setPicking(null)}
          onAdd={(qty, ids) => {
            addLine(picking, qty, ids);
            setPicking(null);
          }}
        />
      )}
    </div>
  );
}

function ComplementPicker({
  product,
  onClose,
  onAdd,
}: {
  product: GProduct;
  onClose: () => void;
  onAdd: (quantity: number, complementIds: string[]) => void;
}) {
  const [quantity, setQuantity] = useState(1);
  const [selected, setSelected] = useState<string[]>([]);
  const complementsTotal = product.complements.filter((c) => selected.includes(c.id)).reduce((s, c) => s + c.price, 0);
  const unitTotal = (product.price + complementsTotal) * quantity;
  const toggle = (id: string) => setSelected((prev) => (prev.includes(id) ? prev.filter((c) => c !== id) : [...prev, id]));

  return (
    <Sheet open onOpenChange={(open) => !open && onClose()}>
      <SheetContent side="bottom" className="mx-auto max-w-lg rounded-t-2xl px-6 pb-8">
        <SheetHeader className="mb-4 text-left">
          <SheetTitle>{product.name}</SheetTitle>
        </SheetHeader>
        <p className="mb-3 text-sm font-semibold">Adicionais</p>
        <div className="mb-4 flex flex-col gap-2">
          {product.complements.map((c) => (
            <label key={c.id} className="flex cursor-pointer items-center justify-between py-2">
              <div className="flex items-center gap-3">
                <Checkbox
                  checked={selected.includes(c.id)}
                  onCheckedChange={() => toggle(c.id)}
                  className="border-orange-300 data-[state=checked]:border-orange-500 data-[state=checked]:bg-orange-500"
                />
                <span className="text-sm">{c.name}</span>
              </div>
              <span className="text-sm text-orange-500">+{brl(c.price)}</span>
            </label>
          ))}
        </div>
        <Separator className="mb-4" />
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Button variant="outline" size="icon" className="h-8 w-8 rounded-full" onClick={() => setQuantity((q) => Math.max(1, q - 1))}>−</Button>
            <span className="w-6 text-center font-semibold">{quantity}</span>
            <Button variant="outline" size="icon" className="h-8 w-8 rounded-full" onClick={() => setQuantity((q) => q + 1)}>+</Button>
          </div>
          <Button onClick={() => onAdd(quantity, selected)} className="rounded-full bg-orange-500 px-6 text-white hover:bg-orange-600">
            Adicionar · {brl(unitTotal)}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
