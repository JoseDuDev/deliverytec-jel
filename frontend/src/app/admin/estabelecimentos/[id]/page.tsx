'use client';
import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { getEstabelecimentoDetail, toggleStatus, EstabelecimentoDetail } from '@/lib/adminApi';

function fmt(n: number) {
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl bg-white shadow p-5">
      <p className="text-sm text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-800">{value}</p>
    </div>
  );
}

export default function EstabelecimentoDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [detail, setDetail] = useState<EstabelecimentoDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toggling, setToggling] = useState(false);

  useEffect(() => {
    getEstabelecimentoDetail(id)
      .then(setDetail)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  async function handleToggle() {
    if (!detail) return;
    setToggling(true);
    try {
      const updated = await toggleStatus(id);
      setDetail((d) => d && { ...d, isActive: updated.isActive });
    } catch (e: unknown) {
      alert('Erro: ' + (e instanceof Error ? e.message : String(e)));
    } finally {
      setToggling(false);
    }
  }

  if (loading) return <p className="text-gray-500">Carregando...</p>;
  if (error)   return <p className="text-red-500">{error}</p>;
  if (!detail) return null;

  const statusColor = detail.isActive
    ? 'bg-green-100 text-green-700'
    : 'bg-red-100 text-red-600';

  return (
    <div className="max-w-4xl">
      <div className="mb-6 flex items-center gap-3">
        <button
          onClick={() => router.back()}
          className="text-gray-400 hover:text-gray-700 text-sm"
        >
          ← Voltar
        </button>
      </div>

      {/* Header */}
      <div className="mb-6 flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">{detail.name}</h1>
          <p className="text-sm text-gray-500 font-mono mt-0.5">{detail.slug}</p>
        </div>
        <div className="flex items-center gap-3">
          <span className={`rounded-full px-3 py-1 text-xs font-semibold ${statusColor}`}>
            {detail.isActive ? 'Ativo' : 'Inativo'}
          </span>
          <button
            onClick={handleToggle}
            disabled={toggling}
            className="rounded-full border border-gray-300 px-4 py-1.5 text-sm font-medium text-gray-600 hover:bg-gray-50 disabled:opacity-40"
          >
            {toggling ? '...' : detail.isActive ? 'Desativar' : 'Ativar'}
          </button>
        </div>
      </div>

      {/* Stats */}
      <div className="mb-6 grid grid-cols-3 gap-4">
        <StatCard label="Total de pedidos" value={String(detail.stats.totalOrders)} />
        <StatCard label="Receita total" value={fmt(detail.stats.totalRevenue)} />
        <StatCard label="Receita (30 dias)" value={fmt(detail.stats.revenueLastMonth)} />
      </div>

      {/* Catalog info */}
      {detail.catalog && (
        <div className="mb-6 rounded-xl bg-white shadow p-5">
          <h2 className="mb-3 font-semibold text-gray-700">Cardápio</h2>
          <div className="flex items-center gap-4">
            {detail.catalog.logoUrl && (
              <img src={detail.catalog.logoUrl} alt="logo" className="h-12 w-12 rounded-lg object-cover" />
            )}
            <div>
              <p className="font-medium text-gray-800">{detail.catalog.name}</p>
              {detail.catalog.description && (
                <p className="text-sm text-gray-500">{detail.catalog.description}</p>
              )}
              <span
                className={`mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-medium ${
                  detail.catalog.isOpen ? 'bg-green-100 text-green-600' : 'bg-gray-100 text-gray-500'
                }`}
              >
                {detail.catalog.isOpen ? 'Aberto' : 'Fechado'}
              </span>
            </div>
          </div>
          <p className="mt-3 text-xs text-gray-400">
            Ver cardápio:{' '}
            <Link
              href={`/${detail.slug}`}
              target="_blank"
              className="text-orange-500 hover:underline"
            >
              /{detail.slug}
            </Link>
          </p>
        </div>
      )}

      {/* Recent orders */}
      <div className="rounded-xl bg-white shadow overflow-hidden">
        <div className="px-5 py-4 border-b">
          <h2 className="font-semibold text-gray-700">Últimos pedidos</h2>
        </div>
        {detail.recentOrders.length === 0 ? (
          <p className="px-5 py-8 text-center text-sm text-gray-400">Nenhum pedido ainda.</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-xs text-gray-500 uppercase tracking-wider">
              <tr>
                {['ID', 'Status', 'Itens', 'Total', 'Data'].map((h) => (
                  <th key={h} className="px-4 py-3 text-left">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {detail.recentOrders.map((o) => (
                <tr key={o.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono text-gray-500">
                    #{o.id.slice(0, 8).toUpperCase()}
                  </td>
                  <td className="px-4 py-3 text-gray-700">{o.status}</td>
                  <td className="px-4 py-3 text-gray-600">{o.itemCount}</td>
                  <td className="px-4 py-3 font-medium text-gray-800">{fmt(o.total)}</td>
                  <td className="px-4 py-3 text-gray-500">
                    {new Date(o.createdAt).toLocaleString('pt-BR')}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
