export type EstabelecimentoSummary = {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAt: string;
  totalOrders: number;
  totalRevenue: number;
};

export type EstabelecimentoDetail = {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  createdAt: string;
  catalog: {
    id: string;
    name: string;
    description: string | null;
    logoUrl: string | null;
    isOpen: boolean;
  } | null;
  stats: {
    totalOrders: number;
    totalRevenue: number;
    revenueLastMonth: number;
  };
  recentOrders: {
    id: string;
    status: string;
    total: number;
    itemCount: number;
    createdAt: string;
  }[];
};

export function getAdminToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('delify_admin_token');
}

export function saveAdminToken(token: string): void {
  localStorage.setItem('delify_admin_token', token);
}

function adminHeaders(): HeadersInit {
  const token = getAdminToken();
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

export async function adminLogin(
  email: string,
  password: string,
): Promise<{ token: string; expiresAt: string }> {
  const res = await fetch('/admin/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  return handleResponse(res);
}

export async function getEstabelecimentos(): Promise<EstabelecimentoSummary[]> {
  const res = await fetch('/admin/estabelecimentos', { headers: adminHeaders() });
  return handleResponse(res);
}

export async function toggleStatus(id: string): Promise<{ id: string; isActive: boolean }> {
  const res = await fetch(`/admin/estabelecimentos/${id}/status`, {
    method: 'PATCH',
    headers: adminHeaders(),
  });
  return handleResponse(res);
}

export async function getEstabelecimentoDetail(id: string): Promise<EstabelecimentoDetail> {
  const res = await fetch(`/admin/estabelecimentos/${id}`, { headers: adminHeaders() });
  return handleResponse(res);
}
