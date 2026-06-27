'use client';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import {
  getEstabelecimentos,
  toggleStatus,
  createEstabelecimento,
  EstabelecimentoSummary,
} from '@/lib/adminApi';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
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

function toSlug(name: string) {
  return name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');
}

const EMPTY_FORM = {
  name: '',
  slug: '',
  ownerName: '',
  ownerEmail: '',
  ownerPassword: '',
};

export default function EstabelecimentosPage() {
  const [rows, setRows] = useState<EstabelecimentoSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [toggling, setToggling] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [slugEdited, setSlugEdited] = useState(false);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

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

  function handleNameChange(name: string) {
    setForm((f) => ({ ...f, name, slug: slugEdited ? f.slug : toSlug(name) }));
  }

  function handleSlugChange(slug: string) {
    setSlugEdited(true);
    setForm((f) => ({ ...f, slug }));
  }

  function openDialog() {
    setForm(EMPTY_FORM);
    setSlugEdited(false);
    setFormError(null);
    setDialogOpen(true);
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setFormError(null);
    setSaving(true);
    try {
      const created = await createEstabelecimento(form);
      setRows((prev) => [
        {
          id: created.id,
          name: created.name,
          slug: created.slug,
          isActive: true,
          createdAt: new Date().toISOString(),
          totalOrders: 0,
          totalRevenue: 0,
        },
        ...prev,
      ]);
      setDialogOpen(false);
    } catch (e: unknown) {
      setFormError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>;
  if (error)   return <p className="text-destructive">{error}</p>;

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight">Estabelecimentos</h1>
        <Button onClick={openDialog} className="bg-orange-500 hover:bg-orange-600 text-white">
          + Novo Estabelecimento
        </Button>
      </div>

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
                      <Badge
                        variant={row.isActive ? 'default' : 'destructive'}
                        className={row.isActive ? 'bg-emerald-500 hover:bg-emerald-500' : ''}
                      >
                        {row.isActive ? 'Ativo' : 'Inativo'}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">{row.totalOrders}</TableCell>
                    <TableCell className="text-right">{fmt(row.totalRevenue)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Link
                          href={`/admin/estabelecimentos/${row.id}`}
                          className="text-sm text-orange-500 hover:underline"
                        >
                          Detalhes
                        </Link>
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

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Novo Estabelecimento</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleCreate} className="space-y-4">
            <div className="space-y-1">
              <Label htmlFor="est-name">Nome do estabelecimento *</Label>
              <Input
                id="est-name"
                required
                value={form.name}
                onChange={(e) => handleNameChange(e.target.value)}
                placeholder="Hamburgueria do Chef"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="est-slug">Slug (URL) *</Label>
              <Input
                id="est-slug"
                required
                value={form.slug}
                onChange={(e) => handleSlugChange(e.target.value)}
                placeholder="hamburgueria-do-chef"
                className="font-mono text-sm"
              />
              <p className="text-xs text-muted-foreground">
                Acesso em: <span className="font-mono">/{form.slug || 'slug'}</span>
              </p>
            </div>

            <div className="border-t pt-4 space-y-3">
              <p className="text-sm font-medium text-muted-foreground">Dados do proprietário</p>
              <div className="space-y-1">
                <Label htmlFor="owner-name">Nome completo *</Label>
                <Input
                  id="owner-name"
                  required
                  value={form.ownerName}
                  onChange={(e) => setForm((f) => ({ ...f, ownerName: e.target.value }))}
                  placeholder="João da Silva"
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="owner-email">E-mail *</Label>
                <Input
                  id="owner-email"
                  type="email"
                  required
                  value={form.ownerEmail}
                  onChange={(e) => setForm((f) => ({ ...f, ownerEmail: e.target.value }))}
                  placeholder="joao@hamburgueria.com"
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="owner-password">Senha *</Label>
                <Input
                  id="owner-password"
                  type="password"
                  required
                  minLength={6}
                  value={form.ownerPassword}
                  onChange={(e) => setForm((f) => ({ ...f, ownerPassword: e.target.value }))}
                  placeholder="Mínimo 6 caracteres"
                />
              </div>
            </div>

            {formError && (
              <p className="text-sm text-destructive">{formError}</p>
            )}

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={saving}
                className="bg-orange-500 hover:bg-orange-600 text-white"
              >
                {saving ? 'Criando...' : 'Criar Estabelecimento'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
