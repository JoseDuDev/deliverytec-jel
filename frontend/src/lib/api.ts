// frontend/src/lib/api.ts

export type MenuResponse = {
  establishmentId: string;
  name: string;
  slug: string;
  categories: {
    id: string;
    name: string;
    order: number;
    products: {
      id: string;
      name: string;
      price: number;
      description: string | null;
      imageUrl: string | null;
      complements: { id: string; name: string; price: number }[];
    }[];
  }[];
};

export type PlaceOrderResponse = {
  orderId: string;
  total: number;
  pix: { qrCode: string; copyPaste: string; expiresAt: string };
};

export type OrderStatusEvent = {
  orderId: string;
  status: string;
  label: string;
  at: string;
};

export type TokenResponse = { token: string; expiresAt: string };

function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('delify_token');
}

function authHeaders(): HeadersInit {
  const token = getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function fetchMenu(slug: string): Promise<MenuResponse> {
  const res = await fetch(`/bff/menu/${slug}`);
  if (!res.ok) throw new Error('Cardápio não encontrado');
  return res.json();
}

export async function guestSession(name: string, phone: string): Promise<TokenResponse> {
  const res = await fetch('/bff/auth/guest', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, phone }),
  });
  if (!res.ok) throw new Error('Erro ao criar sessão');
  return res.json();
}

export async function register(
  name: string, email: string, password: string, phone: string
): Promise<TokenResponse> {
  const res = await fetch('/bff/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, email, password, phone }),
  });
  if (!res.ok) {
    const err = await res.json();
    throw new Error(Array.isArray(err) ? err.join(', ') : 'Erro no cadastro');
  }
  return res.json();
}

export async function login(email: string, password: string): Promise<TokenResponse> {
  const res = await fetch('/bff/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new Error('Email ou senha inválidos');
  return res.json();
}

export async function placeOrder(body: {
  establishmentId: string;
  items: { productId: string; quantity: number; complementIds: string[] }[];
  customer: { name: string; phone: string; cpf: string };
  address: { street: string; number: string; neighborhood: string; city: string; complement?: string };
  note?: string;
}): Promise<PlaceOrderResponse> {
  const res = await fetch('/bff/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error('Erro ao criar pedido');
  return res.json();
}

export function saveToken(token: string): void {
  localStorage.setItem('delify_token', token);
}
