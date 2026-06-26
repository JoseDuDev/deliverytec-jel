'use client';
import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { getEstabelecimentoDetail, toggleStatus, EstabelecimentoDetail } from '@/lib/adminApi';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Separator } from '@/components/ui/separator';

function fmt(n: number) {
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
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

  if (loading) return <p className="text-muted-foreground">Carregando...</p>;
  if (error)   return <p className="text-destructive">{error}</p>;
  if (!detail) return null;

  return (
    <div className="max-w-4xl space-y-6">
      {/* Back */}
      <Button variant="ghost" size="sm" className="text-muted-foreground -ml-2" onClick={() => router.back()}>
        ← Voltar
      </Button>

      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{detail.name}</h1>
          <p className="text-sm text-muted-foreground font-mono mt-0.5">{detail.slug}</p>
        </div>
        <div className="flex items-center gap-3">
          <Badge
            variant={detail.isActive ? 'default' : 'destructive'}
            className={detail.isActive ? 'bg-emerald-500 hover:bg-emerald-500' : ''}
          >
            {detail.isActive ? 'Ativo' : 'Inativo'}
          </Badge>
          <Button
            variant="outline"
            size="sm"
            onClick={handleToggle}
            disabled={toggling}
          >
            {toggling ? '...' : detail.isActive ? 'Desativar' : 'Ativar'}
          </Button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-4">
        <Card>
          <CardHeader className="pb-1 pt-4 px-5">
            <CardTitle className="text-sm font-medium text-muted-foreground">Total de pedidos</CardTitle>
          </CardHeader>
          <CardContent className="px-5 pb-4">
            <p className="text-2xl font-bold">{detail.stats.totalOrders}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1 pt-4 px-5">
            <CardTitle className="text-sm font-medium text-muted-foreground">Receita total</CardTitle>
          </CardHeader>
          <CardContent className="px-5 pb-4">
            <p className="text-2xl font-bold">{fmt(detail.stats.totalRevenue)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1 pt-4 px-5">
            <CardTitle className="text-sm font-medium text-muted-foreground">Receita (30 dias)</CardTitle>
          </CardHeader>
          <CardContent className="px-5 pb-4">
            <p className="text-2xl font-bold">{fmt(detail.stats.revenueLastMonth)}</p>
          </CardContent>
        </Card>
      </div>

      {/* Catalog info */}
      {detail.catalog && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Cardápio</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex items-center gap-4">
              {detail.catalog.logoUrl && (
                <img src={detail.catalog.logoUrl} alt="logo" className="h-12 w-12 rounded-lg object-cover" />
              )}
              <div>
                <p className="font-medium">{detail.catalog.name}</p>
                {detail.catalog.description && (
                  <p className="text-sm text-muted-foreground">{detail.catalog.description}</p>
                )}
                <Badge
                  variant={detail.catalog.isOpen ? 'default' : 'secondary'}
                  className={`mt-1 ${detail.catalog.isOpen ? 'bg-emerald-500 hover:bg-emerald-500' : ''}`}
                >
                  {detail.catalog.isOpen ? 'Aberto' : 'Fechado'}
                </Badge>
              </div>
            </div>
            <Separator />
            <p className="text-xs text-muted-foreground">
              Ver cardápio:{' '}
              <Link href={`/${detail.slug}`} target="_blank" className="text-orange-500 hover:underline">
                /{detail.slug}
              </Link>
            </p>
          </CardContent>
        </Card>
      )}

      {/* Recent orders */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Últimos pedidos</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {detail.recentOrders.length === 0 ? (
            <p className="text-center text-muted-foreground py-8 text-sm">Nenhum pedido ainda.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>ID</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Itens</TableHead>
                  <TableHead className="text-right">Total</TableHead>
                  <TableHead className="text-right">Data</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {detail.recentOrders.map((o) => (
                  <TableRow key={o.id}>
                    <TableCell className="font-mono text-muted-foreground">
                      #{o.id.slice(0, 8).toUpperCase()}
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary">{o.status}</Badge>
                    </TableCell>
                    <TableCell className="text-right">{o.itemCount}</TableCell>
                    <TableCell className="text-right font-medium">{fmt(o.total)}</TableCell>
                    <TableCell className="text-right text-muted-foreground text-sm">
                      {new Date(o.createdAt).toLocaleString('pt-BR')}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
