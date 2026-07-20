// frontend/src/lib/garcomApi.ts — cliente do app do garçom.

export type GarcomTable = {
  id: string;
  number: string;
  status: string;
  sessionId: string | null;
  openedAt: string | null;
  openedByWaiterId: string | null;
  openedByName: string | null;
  isMine: boolean;
  orderCount: number;
  sessionTotal: number;
  hasPendingCall: boolean;
};

export type GComplement = { id: string; name: string; price: number };
export type GProduct = {
  id: string;
  name: string;
  price: number;
  description: string | null;
  imageUrl: string | null;
  complements: GComplement[];
};
export type GCategory = { id: string; name: string; order: number; products: GProduct[] };
export type GarcomMenu = {
  establishmentId: string;
  establishmentName: string;
  isOpen: boolean;
  categories: GCategory[];
};

export type ComandaItem = { productName: string; quantity: number; unitPrice: number; total: number };
export type Comanda = { sessionId: string | null; openedAt: string | null; items: ComandaItem[]; total: number };
export type GarcomComanda = {
  tableId: string;
  tableNumber: string;
  openedByName: string | null;
  comanda: Comanda;
};
export type GarcomChamada = {
  id: string;
  tableId: string;
  tableNumber: string;
  reason: string | null;
  createdAt: string;
};

export type OrderItemInput = { productId: string; quantity: number; complementIds: string[] };

export function getGarcomToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('delify_garcom_token');
}

export function getGarcomName(): string {
  if (typeof window === 'undefined') return '';
  return localStorage.getItem('delify_garcom_name') ?? '';
}

export function saveGarcomSession(token: string, name: string): void {
  localStorage.setItem('delify_garcom_token', token);
  localStorage.setItem('delify_garcom_name', name);
}

export function clearGarcomSession(): void {
  localStorage.removeItem('delify_garcom_token');
  localStorage.removeItem('delify_garcom_name');
}

function headers(): HeadersInit {
  const token = getGarcomToken();
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

async function handle<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    let msg = text;
    try {
      msg = JSON.parse(text)?.error ?? text;
    } catch {
      /* keep raw text */
    }
    throw new Error(msg || `HTTP ${res.status}`);
  }
  return res.json();
}

export async function garcomLogin(
  email: string,
  password: string,
): Promise<{ token: string; name: string; role: string }> {
  const res = await fetch('/garcom-api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new Error('E-mail ou senha inválidos');
  return res.json();
}

export async function getGarcomMesas(): Promise<GarcomTable[]> {
  return handle(await fetch('/garcom-api/mesas', { headers: headers() }));
}

export async function getGarcomCardapio(): Promise<GarcomMenu> {
  return handle(await fetch('/garcom-api/cardapio', { headers: headers() }));
}

export async function abrirComanda(tableId: string): Promise<{ sessionId: string; created: boolean }> {
  return handle(await fetch(`/garcom-api/mesas/${tableId}/abrir`, { method: 'POST', headers: headers() }));
}

export async function getComanda(tableId: string): Promise<GarcomComanda> {
  return handle(await fetch(`/garcom-api/mesas/${tableId}/comanda`, { headers: headers() }));
}

export async function lancarPedido(
  tableId: string,
  items: OrderItemInput[],
  note?: string,
): Promise<{ orderId: string; sessionId: string; orderTotal: number; comanda: Comanda }> {
  return handle(
    await fetch(`/garcom-api/mesas/${tableId}/pedido`, {
      method: 'POST',
      headers: headers(),
      body: JSON.stringify({ items, note: note || null }),
    }),
  );
}

export async function liberarMesaGarcom(tableId: string): Promise<void> {
  const res = await fetch(`/garcom-api/mesas/${tableId}/liberar`, { method: 'POST', headers: headers() });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
}

export async function getGarcomChamadas(): Promise<GarcomChamada[]> {
  return handle(await fetch('/garcom-api/chamadas', { headers: headers() }));
}

export async function atenderChamadaGarcom(id: string): Promise<void> {
  const res = await fetch(`/garcom-api/chamadas/${id}/atender`, { method: 'POST', headers: headers() });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
}
