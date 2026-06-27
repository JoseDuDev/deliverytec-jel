export type OrderItemData = {
  productName: string;
  quantity: number;
  unitPrice: number;
};

export type OrderData = {
  id: string;
  status: string;
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
  data: { name: string; price: number; isAvailable: boolean; description?: string; photoUrl?: string },
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

// ─────────────────────────────────────────────────────────────────────────────

export async function updateEstabelecimento(data: {
  name: string;
  description: string | null;
  logoUrl: string | null;
  deliveryFee: number;
}): Promise<DashboardData> {
  const res = await fetch('/painel-api/me', {
    method: 'PATCH',
    headers: painelHeaders(),
    body: JSON.stringify(data),
  });
  return handleResponse(res);
}
