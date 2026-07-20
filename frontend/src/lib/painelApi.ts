export type OrderItemData = {
  productName: string;
  quantity: number;
  unitPrice: number;
};

export type OrderData = {
  id: string;
  status: string;
  /** 'Delivery' | 'Dinein' */
  type: string;
  /** Número da mesa quando type === 'Dinein'; null em delivery */
  tableNumber: string | null;
  /** Mesa já fechou a conta, mas o pedido ainda não saiu — a comida ainda precisa ser feita */
  sessionPaid: boolean;
  subtotal: number;
  deliveryFee: number;
  total: number;
  createdAt: string;
  customerNote: string | null;
  items: OrderItemData[];
};

export type ProductData = {
  id: string;
  categoryId: string;
  name: string;
  description: string | null;
  price: number;
  photoUrl: string | null;
  isAvailable: boolean;
  /** Sai numa seção "Destaques" no topo do cardápio, além da categoria de origem. */
  isFeatured: boolean;
  /** Posição dentro dos destaques, definida pelo lojista. */
  featuredOrder: number;
};

export type CategoryData = {
  id: string;
  establishmentId: string;
  name: string;
  order: number;
  isActive: boolean;
  products: ProductData[];
};

export type CardapioData = {
  estId: string;
  categories: CategoryData[];
};

export type DashboardData = {
  id: string;
  name: string;
  slug: string;
  isOpen: boolean;
  description: string | null;
  logoUrl: string | null;
  deliveryFee: number;
  serviceFeeEnabled: boolean;
  serviceFeePercent: number;
  ordersToday: number;
  revenueToday: number;
};

export function getPainelToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('delify_painel_token');
}

export function savePainelToken(token: string): void {
  localStorage.setItem('delify_painel_token', token);
}

function painelHeaders(): HeadersInit {
  const token = getPainelToken();
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

async function handleResponse<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `HTTP ${res.status}`);
  }
  return res.json();
}

export async function painelLogin(
  email: string,
  password: string,
): Promise<{ token: string; expiresAt: string }> {
  const res = await fetch('/painel-api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  return handleResponse(res);
}

export async function getDashboard(): Promise<DashboardData> {
  const res = await fetch('/painel-api/me', { headers: painelHeaders() });
  return handleResponse(res);
}

export async function toggleEstabelecimentoStatus(): Promise<{ id: string; isOpen: boolean }> {
  const res = await fetch('/painel-api/me/status', {
    method: 'PATCH',
    headers: painelHeaders(),
  });
  return handleResponse(res);
}

// ── Pedidos ───────────────────────────────────────────────────────────────────

export async function getOrders(status = 'active'): Promise<OrderData[]> {
  const res = await fetch(`/painel-api/pedidos/?status=${status}`, { headers: painelHeaders() });
  return handleResponse(res);
}

async function orderAction(id: string, action: string): Promise<OrderData> {
  const res = await fetch(`/painel-api/pedidos/${id}/${action}`, {
    method: 'PATCH',
    headers: painelHeaders(),
  });
  return handleResponse(res);
}

export const acceptOrder        = (id: string) => orderAction(id, 'accept');
export const startDeliveryOrder = (id: string) => orderAction(id, 'start-delivery');
export const completeOrder      = (id: string) => orderAction(id, 'complete');
export const cancelOrder        = (id: string) => orderAction(id, 'cancel');
/** Dine-in: vai direto de "em preparo" para "entregue", sem leg de entrega. */
export const serveOrder         = (id: string) => orderAction(id, 'servir');

// ── Cardápio ─────────────────────────────────────────────────────────────────

export async function getCardapio(): Promise<CardapioData> {
  const res = await fetch('/painel-api/cardapio/', { headers: painelHeaders() });
  return handleResponse(res);
}

export async function createCategoria(name: string, order = 0): Promise<CategoryData> {
  const res = await fetch('/painel-api/cardapio/categorias', {
    method: 'POST',
    headers: painelHeaders(),
    body: JSON.stringify({ name, order }),
  });
  return handleResponse(res);
}

export async function updateCategoria(
  id: string, name: string, order: number, isActive: boolean,
): Promise<CategoryData> {
  const res = await fetch(`/painel-api/cardapio/categorias/${id}`, {
    method: 'PATCH',
    headers: painelHeaders(),
    body: JSON.stringify({ name, order, isActive }),
  });
  return handleResponse(res);
}

export async function deleteCategoria(id: string): Promise<void> {
  const res = await fetch(`/painel-api/cardapio/categorias/${id}`, {
    method: 'DELETE',
    headers: painelHeaders(),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `HTTP ${res.status}`);
  }
}

export async function createProduto(
  categoriaId: string,
  data: { name: string; price: number; description?: string; photoUrl?: string },
): Promise<ProductData> {
  const res = await fetch(`/painel-api/cardapio/categorias/${categoriaId}/produtos`, {
    method: 'POST',
    headers: painelHeaders(),
    body: JSON.stringify(data),
  });
  return handleResponse(res);
}

export async function updateProduto(
  id: string,
  // PATCH é substituição completa: quem chama precisa reenviar TUDO, senão o
  // campo omitido é zerado. É o que faz o toggle de disponibilidade apagar o
  // destaque se isFeatured/featuredOrder não forem repassados.
  data: {
    name: string; price: number; isAvailable: boolean;
    description?: string; photoUrl?: string;
    isFeatured?: boolean; featuredOrder?: number;
  },
): Promise<ProductData> {
  const res = await fetch(`/painel-api/cardapio/produtos/${id}`, {
    method: 'PATCH',
    headers: painelHeaders(),
    body: JSON.stringify(data),
  });
  return handleResponse(res);
}

export async function deleteProduto(id: string): Promise<void> {
  const res = await fetch(`/painel-api/cardapio/produtos/${id}`, {
    method: 'DELETE',
    headers: painelHeaders(),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `HTTP ${res.status}`);
  }
}

// ── Mesas ──────────────────────────────────────────────────────────────────────

export type MesaData = {
  id: string;
  number: string;
  qrToken: string;
  status: string;
  sessionId: string | null;
  openedAt: string | null;
  orderCount: number;
  sessionTotal: number;
  hasPendingCall: boolean;
};

export type ChamadaData = {
  id: string;
  tableId: string;
  tableNumber: string;
  reason: string | null;
  createdAt: string;
};

export async function getChamadas(): Promise<ChamadaData[]> {
  const res = await fetch('/painel-api/mesas/chamadas', { headers: painelHeaders() });
  return handleResponse(res);
}

export async function atenderChamada(id: string): Promise<void> {
  const res = await fetch(`/painel-api/mesas/chamadas/${id}/atender`, {
    method: 'POST',
    headers: painelHeaders(),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `HTTP ${res.status}`);
  }
}

export async function getMesas(): Promise<MesaData[]> {
  const res = await fetch('/painel-api/mesas/', { headers: painelHeaders() });
  return handleResponse(res);
}

export async function createMesa(number: string): Promise<MesaData> {
  const res = await fetch('/painel-api/mesas/', {
    method: 'POST',
    headers: painelHeaders(),
    body: JSON.stringify({ number }),
  });
  return handleResponse(res);
}

export async function renameMesa(id: string, number: string): Promise<MesaData> {
  const res = await fetch(`/painel-api/mesas/${id}`, {
    method: 'PATCH',
    headers: painelHeaders(),
    body: JSON.stringify({ number }),
  });
  return handleResponse(res);
}

export async function regenerateMesaQr(id: string): Promise<{ id: string; qrToken: string }> {
  const res = await fetch(`/painel-api/mesas/${id}/qr`, {
    method: 'POST',
    headers: painelHeaders(),
  });
  return handleResponse(res);
}

export async function liberarMesa(id: string): Promise<void> {
  const res = await fetch(`/painel-api/mesas/${id}/liberar`, {
    method: 'POST',
    headers: painelHeaders(),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `HTTP ${res.status}`);
  }
}

export async function deleteMesa(id: string): Promise<void> {
  const res = await fetch(`/painel-api/mesas/${id}`, {
    method: 'DELETE',
    headers: painelHeaders(),
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `HTTP ${res.status}`);
  }
}

// ── Garçons ────────────────────────────────────────────────────────────────────

export type GarcomData = { id: string; name: string; email: string; createdAt: string };

export async function getGarcons(): Promise<GarcomData[]> {
  const res = await fetch('/painel-api/garcons/', { headers: painelHeaders() });
  return handleResponse(res);
}

export async function createGarcom(name: string, email: string, password: string): Promise<GarcomData> {
  const res = await fetch('/painel-api/garcons/', {
    method: 'POST',
    headers: painelHeaders(),
    body: JSON.stringify({ name, email, password }),
  });
  return handleResponse(res);
}

export async function updateGarcom(
  id: string,
  data: { name?: string; password?: string },
): Promise<GarcomData> {
  const res = await fetch(`/painel-api/garcons/${id}`, {
    method: 'PATCH',
    headers: painelHeaders(),
    body: JSON.stringify(data),
  });
  return handleResponse(res);
}

export async function deleteGarcom(id: string): Promise<void> {
  const res = await fetch(`/painel-api/garcons/${id}`, { method: 'DELETE', headers: painelHeaders() });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(text || `HTTP ${res.status}`);
  }
}

// ─────────────────────────────────────────────────────────────────────────────

export async function updateEstabelecimento(data: {
  name: string;
  description: string | null;
  logoUrl: string | null;
  deliveryFee: number;
  serviceFeeEnabled: boolean;
  serviceFeePercent: number;
}): Promise<DashboardData> {
  const res = await fetch('/painel-api/me', {
    method: 'PATCH',
    headers: painelHeaders(),
    body: JSON.stringify(data),
  });
  return handleResponse(res);
}
