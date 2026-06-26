'use client';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import { getEstabelecimentos, toggleStatus, EstabelecimentoSummary } from '@/lib/adminApi';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

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

  if (loading) return <p className="text-muted-foreground">Carregando...</p>;
  if (error)   return <p className="text-destructive">{error}</p>;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold tracking-tight">Estabelecimentos</h1>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base font-medium text-muted-foreground">
            {rows.length} estabelecimento{rows.length !== 1 ? 's' : ''} cadastrado{rows.length !== 1 ? 's' : ''}
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Nome</TableHead>
                <TableHead>Slug</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Pedidos</TableHead>
                <TableHead className="text-right">Receita Total</TableHead>
                <TableHead className="text-right">Ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground py-8">
                    Nenhum estabelecimento encontrado.
                  </TableCell>
                </TableRow>
              ) : (
                rows.map((row) => (
                  <TableRow key={row.id}>
                    <TableCell className="font-medium">{row.name}</TableCell>
                    <TableCell className="font-mono text-muted-foreground text-sm">{row.slug}</TableCell>
                    <TableCell>
                      <Badge variant={row.isActive ? 'default' : 'destructive'}
                        className={row.isActive ? 'bg-emerald-500 hover:bg-emerald-500' : ''}>
                        {row.isActive ? 'Ativo' : 'Inativo'}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">{row.totalOrders}</TableCell>
                    <TableCell className="text-right">{fmt(row.totalRevenue)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button variant="link" size="sm" className="text-orange-500 px-0 h-auto" asChild>
                          <Link href={`/admin/estabelecimentos/${row.id}`}>Detalhes</Link>
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          disabled={toggling === row.id}
                          onClick={() => handleToggle(row.id)}
                        >
                          {toggling === row.id ? '...' : row.isActive ? 'Desativar' : 'Ativar'}
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
