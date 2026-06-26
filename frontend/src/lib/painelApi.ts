export type DashboardData = {
  id: string;
  name: string;
  slug: string;
  isOpen: boolean;
  description: string | null;
  logoUrl: string | null;
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

export async function updateEstabelecimento(data: {
  name: string;
  description: string | null;
  logoUrl: string | null;
}): Promise<DashboardData> {
  const res = await fetch('/painel-api/me', {
    method: 'PATCH',
    headers: painelHeaders(),
    body: JSON.stringify(data),
  });
  return handleResponse(res);
}
