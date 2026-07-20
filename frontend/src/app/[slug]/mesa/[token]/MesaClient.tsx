'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  fetchMesa,
  placeMesaOrder,
  callWaiter,
  fecharConta,
  getContaStatus,
  simularPagamentoComanda,
  type MesaResponse,
  type MesaProduct,
  type CloseBillResponse,
} from '@/lib/mesaApi';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Separator } from '@/components/ui/separator';
import { Input } from '@/components/ui/input';
import { Bell } from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';

const IS_DEV = process.env.NODE_ENV === 'development';

const brl = (n: number) => `R$ ${n.toFixed(2).replace('.', ',')}`;

const CALL_REASONS = ['Chamar garçom', 'Pedir a conta', 'Talheres / guardanapos', 'Outro'];

type RoundLine = {
  key: string;
  product: MesaProduct;
  quantity: number;
  complementIds: string[];
  unitPrice: number;
};

function lineKey(productId: string, complementIds: string[]) {
  return `${productId}::${[...complementIds].sort().join(',')}`;
}

export default function MesaClient({ token }: { token: string }) {
  const [data, setData] = useState<MesaResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [round, setRound] = useState<RoundLine[]>([]);
  const [picking, setPicking] = useState<MesaProduct | null>(null);
  const [showComanda, setShowComanda] = useState(false);
  const [showBill, setShowBill] = useState(false);

  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const [sentFlash, setSentFlash] = useState(false);

  const [showCall, setShowCall] = useState(false);
  const [calling, setCalling] = useState(false);
  const [callFlash, setCallFlash] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setData(await fetchMesa(token));
      setError(null);
    } catch {
      setError('Mesa não encontrada. Confira o QR Code.');
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    load();
  }, [load]);

  const addLine = useCallback(
    (product: MesaProduct, quantity: number, complementIds: string[]) => {
      const unitPrice =
        product.price +
        product.complements
          .filter((c) => complementIds.includes(c.id))
          .reduce((s, c) => s + c.price, 0);
      const key = lineKey(product.id, complementIds);
      setRound((prev) => {
        const existing = prev.find((l) => l.key === key);
        if (existing) {
          return prev.map((l) =>
            l.key === key ? { ...l, quantity: l.quantity + quantity } : l,
          );
        }
        return [...prev, { key, product, quantity, complementIds, unitPrice }];
      });
    },
    [],
  );

  const changeQty = (key: string, delta: number) =>
    setRound((prev) =>
      prev
        .map((l) => (l.key === key ? { ...l, quantity: l.quantity + delta } : l))
        .filter((l) => l.quantity > 0),
    );

  const roundTotal = useMemo(
    () => round.reduce((s, l) => s + l.unitPrice * l.quantity, 0),
    [round],
  );
  const roundCount = useMemo(
    () => round.reduce((s, l) => s + l.quantity, 0),
    [round],
  );

  async function handleSend() {
    if (!data || round.length === 0) return;
    setSending(true);
    setSendError(null);
    try {
      const resp = await placeMesaOrder(
        token,
        round.map((l) => ({
          productId: l.product.id,
          quantity: l.quantity,
          complementIds: l.complementIds,
        })),
      );
      setRound([]);
      setData({ ...data, comanda: resp.comanda });
      setSentFlash(true);
      setTimeout(() => setSentFlash(false), 3500);
    } catch (e) {
      setSendError(e instanceof Error ? e.message : 'Erro ao enviar o pedido');
    } finally {
      setSending(false);
    }
  }

  async function handleCall(reason?: string) {
    setShowCall(false);
    setCalling(true);
    try {
      const r = await callWaiter(token, reason);
      setCallFlash(
        r.alreadyPending
          ? 'O garçom já foi chamado — já estamos a caminho! 🙋'
          : 'Garçom a caminho! 🙋',
      );
      setTimeout(() => setCallFlash(null), 4000);
    } catch (e) {
      setCallFlash(e instanceof Error ? e.message : 'Erro ao chamar o garçom');
      setTimeout(() => setCallFlash(null), 4000);
    } finally {
      setCalling(false);
    }
  }

  if (loading) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-gray-50">
        <p className="text-gray-500">Carregando cardápio…</p>
      </main>
    );
  }

  if (error || !data) {
    return (
      <main className="flex min-h-screen flex-col items-center justify-center gap-2 bg-gray-50 px-6 text-center">
        <p className="text-lg font-semibold text-gray-800">Ops!</p>
        <p className="text-gray-500">{error}</p>
      </main>
    );
  }

  const comandaCount = data.comanda.items.reduce((s, i) => s + i.quantity, 0);

  return (
    <main className="min-h-screen bg-gray-50 pb-28">
      <header className="bg-orange-500 px-4 py-5 text-white">
        <div className="flex items-start justify-between gap-3">
          <div>
            <p className="text-sm/none opacity-90">Mesa {data.tableNumber}</p>
            <h1 className="mt-1 text-2xl font-bold">{data.establishmentName}</h1>
          </div>
          <button
            onClick={() => setShowCall(true)}
            disabled={calling}
            className="flex shrink-0 items-center gap-1.5 rounded-full bg-white/20 px-3 py-2 text-sm font-semibold backdrop-blur transition-colors hover:bg-white/30 disabled:opacity-60"
          >
            <Bell className="h-4 w-4" />
            {calling ? 'Chamando…' : 'Garçom'}
          </button>
        </div>
      </header>

      {!data.isOpen && (
        <div className="bg-gray-800 px-4 py-3 text-center text-sm font-medium text-white">
          Estabelecimento fechado no momento — pedidos não estão sendo aceitos.
        </div>
      )}

      {comandaCount > 0 && (
        <button
          onClick={() => setShowComanda(true)}
          className="flex w-full items-center justify-between bg-white px-4 py-3 text-left shadow-sm"
        >
          <span className="text-sm font-medium text-gray-700">
            Sua conta · {comandaCount} {comandaCount === 1 ? 'item' : 'itens'}
          </span>
          <span className="text-sm font-bold text-orange-600">
            {brl(data.comanda.total)} ›
          </span>
        </button>
      )}

      {sentFlash && (
        <div className="mx-4 mt-3 rounded-lg bg-green-50 px-4 py-2 text-sm font-medium text-green-700">
          Pedido enviado para a cozinha! 🍳
        </div>
      )}

      {callFlash && (
        <div className="mx-4 mt-3 rounded-lg bg-blue-50 px-4 py-2 text-sm font-medium text-blue-700">
          {callFlash}
        </div>
      )}

      <div className="mx-auto max-w-2xl px-4 py-4">
        {data.categories.map((cat) => (
          <section key={cat.id} className="mb-8">
            <h2 className="mb-3 text-lg font-bold text-gray-800">{cat.name}</h2>
            <div className="flex flex-col gap-3">
              {cat.products.map((product) => (
                <div
                  key={product.id}
                  className="flex items-center justify-between gap-3 rounded-xl bg-white p-3 shadow-sm"
                >
                  <div className="min-w-0">
                    <p className="font-medium text-gray-800">{product.name}</p>
                    {product.description && (
                      <p className="line-clamp-2 text-sm text-gray-500">
                        {product.description}
                      </p>
                    )}
                    <p className="mt-1 text-sm font-semibold text-orange-600">
                      {brl(product.price)}
                    </p>
                  </div>
                  <Button
                    size="sm"
                    disabled={!data.isOpen}
                    onClick={() =>
                      product.complements.length > 0
                        ? setPicking(product)
                        : addLine(product, 1, [])
                    }
                    className="shrink-0 rounded-full bg-orange-500 px-4 text-white hover:bg-orange-600 disabled:opacity-50"
                  >
                    Adicionar
                  </Button>
                </div>
              ))}
            </div>
          </section>
        ))}
      </div>

      {/* Barra da rodada atual */}
      {round.length > 0 && (
        <div className="fixed inset-x-0 bottom-0 z-20 border-t bg-white px-4 py-3 shadow-[0_-2px_8px_rgba(0,0,0,0.06)]">
          <div className="mx-auto flex max-w-2xl items-center gap-3">
            <div className="flex-1">
              <p className="text-xs text-gray-500">
                {roundCount} {roundCount === 1 ? 'item' : 'itens'} nesta rodada
              </p>
              {sendError && <p className="text-xs text-red-600">{sendError}</p>}
            </div>
            <Button
              onClick={handleSend}
              disabled={sending || !data.isOpen}
              className="rounded-full bg-orange-500 px-6 text-white hover:bg-orange-600 disabled:opacity-50"
            >
              {sending ? 'Enviando…' : `Enviar pedido · ${brl(roundTotal)}`}
            </Button>
          </div>
        </div>
      )}

      {/* Modal de complementos */}
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

      {/* Sheet de chamar garçom */}
      <Sheet open={showCall} onOpenChange={setShowCall}>
        <SheetContent side="bottom" className="mx-auto max-w-lg rounded-t-2xl px-6 pb-8">
          <SheetHeader className="mb-4 text-left">
            <SheetTitle>Chamar garçom</SheetTitle>
          </SheetHeader>
          <div className="flex flex-col gap-2">
            {CALL_REASONS.map((reason) => (
              <button
                key={reason}
                onClick={() => handleCall(reason)}
                className="rounded-xl border px-4 py-3 text-left text-sm font-medium text-gray-700 transition-colors hover:border-orange-300 hover:bg-orange-50"
              >
                {reason}
              </button>
            ))}
          </div>
        </SheetContent>
      </Sheet>

      {/* Painel da comanda (conta corrente) */}
      <Sheet open={showComanda} onOpenChange={setShowComanda}>
        <SheetContent side="bottom" className="mx-auto max-w-lg rounded-t-2xl px-6 pb-8">
          <SheetHeader className="text-left">
            <SheetTitle>Sua conta — Mesa {data.tableNumber}</SheetTitle>
          </SheetHeader>
          <div className="mt-4 flex flex-col gap-2">
            {data.comanda.items.length === 0 ? (
              <p className="text-sm text-gray-500">Nenhum item ainda.</p>
            ) : (
              data.comanda.items.map((i, idx) => (
                <div key={idx} className="flex justify-between text-sm">
                  <span className="text-gray-700">
                    {i.quantity}× {i.productName}
                  </span>
                  <span className="text-gray-600">{brl(i.total)}</span>
                </div>
              ))
            )}
          </div>
          <Separator className="my-4" />
          <div className="flex justify-between font-semibold">
            <span>Total parcial</span>
            <span className="text-orange-600">{brl(data.comanda.total)}</span>
          </div>
          <Button
            onClick={() => {
              setShowComanda(false);
              setShowBill(true);
            }}
            disabled={data.comanda.items.length === 0}
            className="mt-4 w-full rounded-full bg-orange-500 text-white hover:bg-orange-600 disabled:opacity-50"
          >
            Fechar conta e pagar
          </Button>
        </SheetContent>
      </Sheet>

      {showBill && (
        <FecharContaSheet
          token={token}
          subtotal={data.comanda.total}
          serviceFeeEnabled={data.serviceFeeEnabled}
          serviceFeePercent={data.serviceFeePercent}
          onClose={() => setShowBill(false)}
          onPaid={() => {
            setShowBill(false);
            load();
          }}
        />
      )}
    </main>
  );
}

function ComplementPicker({
  product,
  onClose,
  onAdd,
}: {
  product: MesaProduct;
  onClose: () => void;
  onAdd: (quantity: number, complementIds: string[]) => void;
}) {
  const [quantity, setQuantity] = useState(1);
  const [selected, setSelected] = useState<string[]>([]);

  const complementsTotal = product.complements
    .filter((c) => selected.includes(c.id))
    .reduce((s, c) => s + c.price, 0);
  const unitTotal = (product.price + complementsTotal) * quantity;

  const toggle = (id: string) =>
    setSelected((prev) =>
      prev.includes(id) ? prev.filter((c) => c !== id) : [...prev, id],
    );

  return (
    <Sheet open onOpenChange={(open) => !open && onClose()}>
      <SheetContent side="bottom" className="mx-auto max-w-lg rounded-t-2xl px-6 pb-8">
        <SheetHeader className="mb-4 text-left">
          <SheetTitle>{product.name}</SheetTitle>
        </SheetHeader>

        {product.description && (
          <p className="mb-4 text-sm text-muted-foreground">{product.description}</p>
        )}

        <p className="mb-3 text-sm font-semibold">Adicionais</p>
        <div className="mb-4 flex flex-col gap-2">
          {product.complements.map((c) => (
            <label
              key={c.id}
              className="flex cursor-pointer items-center justify-between py-2"
            >
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
            <Button
              variant="outline"
              size="icon"
              className="h-8 w-8 rounded-full"
              onClick={() => setQuantity((q) => Math.max(1, q - 1))}
            >
              −
            </Button>
            <span className="w-6 text-center font-semibold">{quantity}</span>
            <Button
              variant="outline"
              size="icon"
              className="h-8 w-8 rounded-full"
              onClick={() => setQuantity((q) => q + 1)}
            >
              +
            </Button>
          </div>
          <Button
            onClick={() => onAdd(quantity, selected)}
            className="rounded-full bg-orange-500 px-6 text-white hover:bg-orange-600"
          >
            Adicionar · {brl(unitTotal)}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}

function FecharContaSheet({
  token,
  subtotal,
  serviceFeeEnabled,
  serviceFeePercent,
  onClose,
  onPaid,
}: {
  token: string;
  subtotal: number;
  serviceFeeEnabled: boolean;
  serviceFeePercent: number;
  onClose: () => void;
  onPaid: () => void;
}) {
  const [step, setStep] = useState<'form' | 'pix' | 'paid'>('form');
  const [cpf, setCpf] = useState('');
  const [name, setName] = useState('');
  const [waive, setWaive] = useState(false);
  const [bill, setBill] = useState<CloseBillResponse | null>(null);
  const [busy, setBusy] = useState(false);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const round2 = (n: number) => Math.round(n * 100) / 100;
  const serviceFee = serviceFeeEnabled && !waive ? round2((subtotal * serviceFeePercent) / 100) : 0;
  const total = subtotal + serviceFee;

  useEffect(() => {
    if (step !== 'pix' || !bill) return;
    const id = setInterval(async () => {
      try {
        const s = await getContaStatus(bill.sessionId);
        if (s.paid) {
          clearInterval(id);
          setStep('paid');
        }
      } catch {
        /* segue tentando */
      }
    }, 3000);
    return () => clearInterval(id);
  }, [step, bill]);

  async function generate() {
    if (!cpf.trim() || !name.trim()) {
      setError('Informe nome e CPF para gerar o PIX.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const r = await fecharConta(token, { cpf: cpf.trim(), name: name.trim(), waiveServiceFee: waive });
      setBill(r);
      setStep('pix');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao fechar a conta');
    } finally {
      setBusy(false);
    }
  }

  function copyPix() {
    if (!bill) return;
    navigator.clipboard.writeText(bill.pix.copyPaste);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <Sheet open onOpenChange={(open) => !open && onClose()}>
      <SheetContent side="bottom" className="mx-auto max-w-lg rounded-t-2xl px-6 pb-8">
        <SheetHeader className="mb-4 text-left">
          <SheetTitle>{step === 'paid' ? 'Conta paga 🎉' : 'Fechar conta'}</SheetTitle>
        </SheetHeader>

        {step === 'form' && (
          <>
            <div className="mb-4 rounded-xl bg-gray-50 p-3 text-sm">
              <div className="flex justify-between py-0.5">
                <span className="text-gray-600">Subtotal</span>
                <span>{brl(subtotal)}</span>
              </div>
              {serviceFeeEnabled && (
                <label className="flex cursor-pointer items-center justify-between py-0.5">
                  <div className="flex items-center gap-2">
                    <Checkbox
                      checked={!waive}
                      onCheckedChange={(v) => setWaive(!v)}
                      className="border-orange-300 data-[state=checked]:border-orange-500 data-[state=checked]:bg-orange-500"
                    />
                    <span className="text-gray-600">Taxa de serviço ({serviceFeePercent}%)</span>
                  </div>
                  <span>{brl(serviceFee)}</span>
                </label>
              )}
              <Separator className="my-2" />
              <div className="flex justify-between py-0.5 font-semibold">
                <span>Total</span>
                <span className="text-orange-600">{brl(total)}</span>
              </div>
            </div>

            <div className="mb-2">
              <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Seu nome" />
            </div>
            <div className="mb-3">
              <Input value={cpf} onChange={(e) => setCpf(e.target.value)} placeholder="CPF (para o PIX)" inputMode="numeric" />
            </div>

            {error && <p className="mb-2 text-sm text-red-600">{error}</p>}

            <Button onClick={generate} disabled={busy} className="w-full rounded-full bg-orange-500 text-white hover:bg-orange-600">
              {busy ? 'Gerando PIX…' : `Gerar PIX · ${brl(total)}`}
            </Button>
          </>
        )}

        {step === 'pix' && bill && (
          <div className="flex flex-col items-center gap-4">
            <p className="text-sm text-muted-foreground">Pague {brl(bill.total)} com PIX</p>
            <div className="rounded-2xl border-4 border-orange-100 p-3">
              <QRCodeSVG value={bill.pix.copyPaste} size={190} />
            </div>
            <div className="flex w-full gap-2">
              <Input readOnly value={bill.pix.copyPaste} className="text-xs text-muted-foreground" />
              <Button onClick={copyPix} className="shrink-0 bg-orange-500 text-white hover:bg-orange-600">
                {copied ? 'Copiado!' : 'Copiar'}
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">Aguardando confirmação do pagamento…</p>
            {IS_DEV && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => simularPagamentoComanda(bill.sessionId)}
                className="w-full border-dashed border-yellow-400 text-yellow-700 hover:bg-yellow-50"
              >
                ⚡ Simular pagamento (dev)
              </Button>
            )}
          </div>
        )}

        {step === 'paid' && (
          <div className="flex flex-col items-center gap-3 py-4 text-center">
            <div className="text-5xl">✅</div>
            <p className="font-semibold text-gray-800">Pagamento confirmado!</p>
            <p className="text-sm text-muted-foreground">Obrigado pela visita. A mesa foi liberada.</p>
            <Button onClick={onPaid} className="mt-2 w-full rounded-full bg-orange-500 text-white hover:bg-orange-600">
              Concluir
            </Button>
          </div>
        )}
      </SheetContent>
    </Sheet>
  );
}
