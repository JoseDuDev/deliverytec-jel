'use client';
import { useEffect, useState } from 'react';
import { getDashboard, toggleEstabelecimentoStatus, DashboardData } from '@/lib/painelApi';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ShoppingBag, DollarSign, Store } from 'lucide-react';

function fmt(n: number) {
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export default function PainelDashboard() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toggling, setToggling] = useState(false);

  useEffect(() => {
    getDashboard()
      .then(setData)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  async function handleToggle() {
    if (!data) return;
    setToggling(true);
    try {
      const updated = await toggleEstabelecimentoStatus();
      setData((d) => d && { ...d, isOpen: updated.isOpen });
    } catch (e: unknown) {
      alert('Erro: ' + (e instanceof Error ? e.message : String(e)));
    } finally {
      setToggling(false);
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>;
  if (error)   return <p className="text-destructive">{error}</p>;
  if (!data)   return <p className="text-muted-foreground">Estabelecimento não configurado.</p>;

  return (
    <div className="space-y-6 max-w-3xl">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">{data.name}</h1>
          <p className="text-sm text-muted-foreground font-mono mt-0.5">/{data.slug}</p>
        </div>
        <div className="flex items-center gap-3">
          <Badge
            variant={data.isOpen ? 'default' : 'secondary'}
            className={data.isOpen ? 'bg-emerald-500 hover:bg-emerald-500' : ''}
          >
            {data.isOpen ? 'Aberto' : 'Fechado'}
          </Badge>
          <Button
            variant="outline"
            size="sm"
            onClick={handleToggle}
            disabled={toggling}
          >
            {toggling ? '...' : data.isOpen ? 'Fechar cardápio' : 'Abrir cardápio'}
          </Button>
        </div>
      </div>

      {/* Stats cards */}
      <div className="grid grid-cols-3 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-1 pt-4 px-5">
            <CardTitle className="text-sm font-medium text-muted-foreground">Pedidos hoje</CardTitle>
            <ShoppingBag className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent className="px-5 pb-4">
            <p className="text-3xl font-bold">{data.ordersToday}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-1 pt-4 px-5">
            <CardTitle className="text-sm font-medium text-muted-foreground">Receita hoje</CardTitle>
            <DollarSign className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent className="px-5 pb-4">
            <p className="text-3xl font-bold">{fmt(data.revenueToday)}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-1 pt-4 px-5">
            <CardTitle className="text-sm font-medium text-muted-foreground">Status</CardTitle>
            <Store className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent className="px-5 pb-4">
            <p className={`text-3xl font-bold ${data.isOpen ? 'text-emerald-500' : 'text-muted-foreground'}`}>
              {data.isOpen ? 'Aberto' : 'Fechado'}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Quick info */}
      {(data.description) && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Sobre o estabelecimento</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">{data.description}</p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
