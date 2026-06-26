'use client';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import { getEstabelecimentos, toggleStatus, EstabelecimentoSummary } from '@/lib/adminApi';

function fmt(n: number) {
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export default function EstabelecimentosPage() {
  const [rows, setRows] = useState<EstabelecimentoSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toggling, setToggling] = useState<string | null>(null);

  useEffect(() => {
    getEstabelecimentos()
      .then(setRows)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  async function handleToggle(id: string) {
    setToggling(id);
    try {
      const updated = await toggleStatus(id);
      setRows((prev) =>
        prev.map((r) => (r.id === updated.id ? { ...r, isActive: updated.isActive } : r)),
      );
    } catch (e: unknown) {
      alert('Erro ao alterar status: ' + (e instanceof Error ? e.message : String(e)));
    } finally {
      setToggling(null);
    }
  }

  if (loading) return <p className="text-gray-500">Carregando...</p>;
  if (error)   return <p className="text-red-500">{error}</p>;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold text-gray-800">Estabelecimentos</h1>

      <div className="rounded-xl bg-white shadow overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 text-gray-600 uppercase text-xs tracking-wider">
            <tr>
              {['Nome', 'Slug', 'Status', 'Pedidos', 'Receita Total', 'Ações'].map((h) => (
                <th key={h} className="px-4 py-3 text-left">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {rows.map((row) => (
              <tr key={row.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 font-medium text-gray-900">{row.name}</td>
                <td className="px-4 py-3 text-gray-500 font-mono">{row.slug}</td>
                <td className="px-4 py-3">
                  <span
                    className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${
                      row.isActive
                        ? 'bg-green-100 text-green-700'
                        : 'bg-red-100 text-red-600'
                    }`}
                  >
                    {row.isActive ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td className="px-4 py-3 text-gray-700">{row.totalOrders}</td>
                <td className="px-4 py-3 text-gray-700">{fmt(row.totalRevenue)}</td>
                <td className="px-4 py-3 flex gap-2">
                  <Link
                    href={`/admin/estabelecimentos/${row.id}`}
                    className="text-orange-500 hover:underline font-medium"
                  >
                    Detalhes
                  </Link>
                  <button
                    onClick={() => handleToggle(row.id)}
                    disabled={toggling === row.id}
                    className="text-gray-500 hover:text-gray-800 disabled:opacity-40"
                  >
                    {toggling === row.id ? '...' : row.isActive ? 'Desativar' : 'Ativar'}
                  </button>
                </td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-gray-400">
                  Nenhum estabelecimento encontrado.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
