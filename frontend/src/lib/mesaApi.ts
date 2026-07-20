// frontend/src/lib/mesaApi.ts
// Cliente da experiência de mesa (cardápio na mesa via QR).

export type MesaComplement = { id: string; name: string; price: number };

export type MesaProduct = {
  id: string;
  name: string;
  price: number;
  description: string | null;
  imageUrl: string | null;
  complements: MesaComplement[];
};

export type MesaCategory = {
  id: string;
  name: string;
  order: number;
  products: MesaProduct[];
};

export type ComandaItem = {
  productName: string;
  quantity: number;
  unitPrice: number;
  total: number;
};

export type Comanda = {
  sessionId: string | null;
  openedAt: string | null;
  items: ComandaItem[];
  total: number;
};

export type MesaResponse = {
  tableId: string;
  tableNumber: string;
  establishmentId: string;
  establishmentName: string;
  slug: string;
  isOpen: boolean;
  serviceFeeEnabled: boolean;
  serviceFeePercent: number;
  categories: MesaCategory[];
  comanda: Comanda;
};

export type Pix = { qrCode: string; copyPaste: string; expiresAt: string };
export type CloseBillResponse = {
  sessionId: string;
  subtotal: number;
  serviceFee: number;
  total: number;
  serviceFeeApplied: boolean;
  pix: Pix;
};
export type ContaStatus = { paid: boolean; sessionStatus: string; total: number };

export type PlaceMesaOrderItem = {
  productId: string;
  quantity: number;
  complementIds: string[];
};

export type PlaceMesaOrderResponse = {
  orderId: string;
  sessionId: string;
  orderTotal: number;
  comanda: Comanda;
};

export async function fetchMesa(token: string): Promise<MesaResponse> {
  const res = await fetch(`/bff/mesa/${token}`, { cache: 'no-store' });
  if (!res.ok) throw new Error('Mesa não encontrada');
  return res.json();
}

export async function placeMesaOrder(
  token: string,
  items: PlaceMesaOrderItem[],
  note?: string,
): Promise<PlaceMesaOrderResponse> {
  const res = await fetch(`/bff/mesa/${token}/pedido`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ items, note: note || null }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.error || 'Erro ao enviar o pedido');
  }
  return res.json();
}

export type CallWaiterResponse = { callId: string; alreadyPending: boolean };

export async function callWaiter(token: string, reason?: string): Promise<CallWaiterResponse> {
  const res = await fetch(`/bff/mesa/${token}/garcom`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ reason: reason || null }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.error || 'Erro ao chamar o garçom');
  }
  return res.json();
}

export async function fecharConta(
  token: string,
  data: { cpf: string; name: string; waiveServiceFee: boolean },
): Promise<CloseBillResponse> {
  const res = await fetch(`/bff/mesa/${token}/conta`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ cpf: data.cpf, name: data.name, waiveServiceFee: data.waiveServiceFee }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.error || 'Erro ao fechar a conta');
  }
  return res.json();
}

export async function getContaStatus(sessionId: string): Promise<ContaStatus> {
  const res = await fetch(`/bff/conta/${sessionId}/status`, { cache: 'no-store' });
  if (!res.ok) throw new Error('Erro ao consultar o pagamento');
  return res.json();
}

// Apenas em dev: simula a confirmação do PIX da comanda.
export async function simularPagamentoComanda(sessionId: string): Promise<void> {
  await fetch(`/bff/dev/simulate-payment-session/${sessionId}`, { method: 'POST' });
}
