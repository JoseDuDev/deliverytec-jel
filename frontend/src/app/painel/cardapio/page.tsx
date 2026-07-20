'use client';
import { useEffect, useState } from 'react';
import {
  getCardapio, createCategoria, updateCategoria, deleteCategoria,
  createProduto, updateProduto, deleteProduto,
  CategoryData, ProductData,
} from '@/lib/painelApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Checkbox } from '@/components/ui/checkbox';
import { Pencil, Trash2, Plus, ChevronDown, ChevronRight } from 'lucide-react';

function fmt(n: number) {
  return n.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

// ── Product Sheet ────────────────────────────────────────────────────────────

type ProductSheetProps = {
  open: boolean;
  onClose: () => void;
  categoryId: string;
  editing: ProductData | null;
  onSaved: (p: ProductData, isNew: boolean) => void;
};

function ProductSheet({ open, onClose, categoryId, editing, onSaved }: ProductSheetProps) {
  const [name, setName]           = useState('');
  const [description, setDesc]    = useState('');
  const [price, setPrice]         = useState('');
  const [photoUrl, setPhotoUrl]   = useState('');
  const [imgError, setImgError]   = useState(false);
  const [isAvailable, setAvail]   = useState(true);
  const [saving, setSaving]       = useState(false);
  const [error, setError]         = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setName(editing?.name ?? '');
      setDesc(editing?.description ?? '');
      setPrice(editing ? String(editing.price) : '');
      setPhotoUrl(editing?.photoUrl ?? '');
      setImgError(false);
      setAvail(editing?.isAvailable ?? true);
      setError(null);
    }
  }, [open, editing]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const priceNum = parseFloat(price.replace(',', '.'));
    if (isNaN(priceNum) || priceNum < 0) { setError('Preço inválido'); return; }
    setSaving(true);
    setError(null);
    try {
      if (editing) {
        const updated = await updateProduto(editing.id, {
          name, price: priceNum, isAvailable,
          description: description || undefined,
          photoUrl: photoUrl || undefined,
        });
        onSaved(updated, false);
      } else {
        const created = await createProduto(categoryId, {
          name, price: priceNum,
          description: description || undefined,
          photoUrl: photoUrl || undefined,
        });
        onSaved(created, true);
      }
      onClose();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Sheet open={open} onOpenChange={(v) => !v && onClose()}>
      <SheetContent side="right" className="w-full sm:max-w-md overflow-y-auto">
        <SheetHeader>
          <SheetTitle>{editing ? 'Editar produto' : 'Novo produto'}</SheetTitle>
        </SheetHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4 mt-6">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="p-name">Nome *</Label>
            <Input id="p-name" value={name} onChange={(e) => setName(e.target.value)} required placeholder="Ex: X-Burguer" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="p-price">Preço (R$) *</Label>
            <Input id="p-price" value={price} onChange={(e) => setPrice(e.target.value)} required placeholder="0,00" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="p-desc">Descrição</Label>
            <Textarea id="p-desc" value={description} onChange={(e) => setDesc(e.target.value)} rows={2} placeholder="Ingredientes, alergênicos..." />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="p-photo">URL da foto</Label>
            <Input
              id="p-photo"
              type="url"
              value={photoUrl}
              onChange={(e) => { setPhotoUrl(e.target.value); setImgError(false); }}
              placeholder="https://exemplo.com/foto.jpg"
            />
            {photoUrl && !imgError && (
              // key força um <img> novo a cada URL: sem isso o React reaproveita o
              // elemento e uma imagem que falhou antes não volta a ser tentada.
              <img
                key={photoUrl}
                src={photoUrl}
                alt="preview"
                className="h-24 w-24 rounded object-cover border mt-1"
                onError={() => setImgError(true)}
              />
            )}
            {photoUrl && imgError && (
              <p className="mt-1 text-xs text-amber-700">
                Não consegui carregar essa imagem. O link precisa apontar direto para o
                arquivo (terminando em .jpg, .png, .webp…), não para a página onde ela
                aparece. Na imagem, use “Copiar endereço da imagem”.
              </p>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Checkbox id="p-avail" checked={isAvailable} onCheckedChange={(v) => setAvail(Boolean(v))} />
            <Label htmlFor="p-avail">Disponível</Label>
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
          <div className="flex gap-2 pt-2">
            <Button type="submit" disabled={saving} className="bg-orange-500 hover:bg-orange-600 text-white flex-1">
              {saving ? 'Salvando...' : 'Salvar'}
            </Button>
            <Button type="button" variant="outline" onClick={onClose}>Cancelar</Button>
          </div>
        </form>
      </SheetContent>
    </Sheet>
  );
}

// ── Category Sheet ───────────────────────────────────────────────────────────

type CategorySheetProps = {
  open: boolean;
  onClose: () => void;
  editing: CategoryData | null;
  nextOrder: number;
  onSaved: (c: CategoryData, isNew: boolean) => void;
};

function CategorySheet({ open, onClose, editing, nextOrder, onSaved }: CategorySheetProps) {
  const [name, setName]   = useState('');
  const [order, setOrder] = useState('0');
  const [saving, setSaving] = useState(false);
  const [error, setError]   = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setName(editing?.name ?? '');
      setOrder(editing ? String(editing.order) : String(nextOrder));
      setError(null);
    }
  }, [open, editing, nextOrder]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      if (editing) {
        const updated = await updateCategoria(editing.id, name, Number(order), editing.isActive);
        onSaved({ ...updated, products: editing.products }, false);
      } else {
        const created = await createCategoria(name, Number(order));
        onSaved({ ...created, products: [] }, true);
      }
      onClose();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Sheet open={open} onOpenChange={(v) => !v && onClose()}>
      <SheetContent side="right" className="w-full sm:max-w-sm">
        <SheetHeader>
          <SheetTitle>{editing ? 'Editar categoria' : 'Nova categoria'}</SheetTitle>
        </SheetHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4 mt-6">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="c-name">Nome *</Label>
            <Input id="c-name" value={name} onChange={(e) => setName(e.target.value)} required placeholder="Ex: Lanches" />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="c-order">Ordem de exibição</Label>
            <Input id="c-order" type="number" value={order} onChange={(e) => setOrder(e.target.value)} min={0} />
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
          <div className="flex gap-2 pt-2">
            <Button type="submit" disabled={saving} className="bg-orange-500 hover:bg-orange-600 text-white flex-1">
              {saving ? 'Salvando...' : 'Salvar'}
            </Button>
            <Button type="button" variant="outline" onClick={onClose}>Cancelar</Button>
          </div>
        </form>
      </SheetContent>
    </Sheet>
  );
}

// ── Main Page ────────────────────────────────────────────────────────────────

export default function CardapioPage() {
  const [categories, setCategories] = useState<CategoryData[]>([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState<string | null>(null);
  const [expanded, setExpanded]     = useState<Set<string>>(new Set());

  // Category sheet
  const [catSheet, setCatSheet]     = useState(false);
  const [editingCat, setEditingCat] = useState<CategoryData | null>(null);

  // Product sheet
  const [prodSheet, setProdSheet]   = useState(false);
  const [editingProd, setEditingProd] = useState<ProductData | null>(null);
  const [targetCatId, setTargetCatId] = useState<string>('');

  useEffect(() => {
    getCardapio()
      .then((d) => {
        setCategories(d.categories);
        setExpanded(new Set(d.categories.map((c) => c.id)));
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  function toggleExpanded(id: string) {
    setExpanded((prev) => {
      const next = new Set(prev);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  // Category actions
  function openNewCat() { setEditingCat(null); setCatSheet(true); }
  function openEditCat(cat: CategoryData) { setEditingCat(cat); setCatSheet(true); }

  async function handleDeleteCat(cat: CategoryData) {
    if (!confirm(`Excluir categoria "${cat.name}"? Todos os produtos devem ser removidos antes.`)) return;
    try {
      await deleteCategoria(cat.id);
      setCategories((prev) => prev.filter((c) => c.id !== cat.id));
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : String(e));
    }
  }

  function onCatSaved(saved: CategoryData, isNew: boolean) {
    if (isNew) {
      setCategories((prev) => [...prev, saved].sort((a, b) => a.order - b.order || a.name.localeCompare(b.name)));
      setExpanded((prev) => new Set([...prev, saved.id]));
    } else {
      setCategories((prev) => prev.map((c) => c.id === saved.id ? saved : c));
    }
  }

  // Product actions
  function openNewProd(categoryId: string) { setEditingProd(null); setTargetCatId(categoryId); setProdSheet(true); }
  function openEditProd(prod: ProductData) { setEditingProd(prod); setTargetCatId(prod.categoryId); setProdSheet(true); }

  async function handleDeleteProd(prod: ProductData) {
    if (!confirm(`Excluir produto "${prod.name}"?`)) return;
    try {
      await deleteProduto(prod.id);
      setCategories((prev) => prev.map((c) =>
        c.id === prod.categoryId ? { ...c, products: c.products.filter((p) => p.id !== prod.id) } : c));
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : String(e));
    }
  }

  async function handleToggleAvail(prod: ProductData) {
    try {
      const updated = await updateProduto(prod.id, {
        name: prod.name, price: prod.price, isAvailable: !prod.isAvailable,
        description: prod.description ?? undefined,
        photoUrl: prod.photoUrl ?? undefined,
      });
      setCategories((prev) => prev.map((c) =>
        c.id === prod.categoryId
          ? { ...c, products: c.products.map((p) => p.id === updated.id ? updated : p) }
          : c));
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : String(e));
    }
  }

  function onProdSaved(saved: ProductData, isNew: boolean) {
    setCategories((prev) => prev.map((c) => {
      if (c.id !== saved.categoryId) return c;
      if (isNew) return { ...c, products: [...c.products, saved] };
      return { ...c, products: c.products.map((p) => p.id === saved.id ? saved : p) };
    }));
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>;
  if (error)   return <p className="text-destructive">{error}</p>;

  const nextOrder = categories.length > 0 ? Math.max(...categories.map((c) => c.order)) + 1 : 0;

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Cardápio</h1>
          <p className="text-sm text-muted-foreground mt-0.5">{categories.length} {categories.length === 1 ? 'categoria' : 'categorias'}</p>
        </div>
        <Button onClick={openNewCat} className="bg-orange-500 hover:bg-orange-600 text-white gap-1.5">
          <Plus className="h-4 w-4" /> Nova categoria
        </Button>
      </div>

      {categories.length === 0 && (
        <div className="rounded-lg border-2 border-dashed border-muted p-12 text-center">
          <p className="text-muted-foreground">Nenhuma categoria ainda.</p>
          <p className="text-sm text-muted-foreground mt-1">Crie uma categoria para começar a adicionar produtos.</p>
        </div>
      )}

      <div className="space-y-3">
        {categories.map((cat) => (
          <div key={cat.id} className="rounded-lg border bg-card">
            {/* Category header */}
            <div className="flex items-center justify-between px-4 py-3">
              <button
                onClick={() => toggleExpanded(cat.id)}
                className="flex items-center gap-2 font-semibold text-left flex-1"
              >
                {expanded.has(cat.id)
                  ? <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
                  : <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />}
                {cat.name}
                <span className="text-xs text-muted-foreground font-normal">
                  ({cat.products.length} {cat.products.length === 1 ? 'produto' : 'produtos'})
                </span>
                {!cat.isActive && <Badge variant="secondary" className="text-xs">Inativa</Badge>}
              </button>
              <div className="flex items-center gap-1">
                <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground hover:text-foreground"
                  onClick={() => openEditCat(cat)}>
                  <Pencil className="h-3.5 w-3.5" />
                </Button>
                <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground hover:text-destructive"
                  onClick={() => handleDeleteCat(cat)}>
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>

            {/* Products */}
            {expanded.has(cat.id) && (
              <div className="border-t">
                {cat.products.length === 0 && (
                  <p className="text-sm text-muted-foreground px-4 py-3">Nenhum produto nesta categoria.</p>
                )}
                {cat.products.map((prod) => (
                  <div key={prod.id} className="flex items-center gap-3 px-4 py-3 border-b last:border-b-0 hover:bg-muted/30">
                    {prod.photoUrl && (
                      <img src={prod.photoUrl} alt={prod.name}
                        className="h-10 w-10 rounded object-cover shrink-0 border"
                        onError={(e) => (e.currentTarget.style.display = 'none')} />
                    )}
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-sm truncate">{prod.name}</p>
                      {prod.description && (
                        <p className="text-xs text-muted-foreground truncate">{prod.description}</p>
                      )}
                    </div>
                    <span className="text-sm font-semibold shrink-0">{fmt(prod.price)}</span>
                    <button
                      onClick={() => handleToggleAvail(prod)}
                      title={prod.isAvailable ? 'Disponível — clique para pausar' : 'Indisponível — clique para ativar'}
                      className={`text-xs font-medium px-2 py-0.5 rounded-full shrink-0 transition-colors ${
                        prod.isAvailable
                          ? 'bg-emerald-100 text-emerald-700 hover:bg-emerald-200'
                          : 'bg-muted text-muted-foreground hover:bg-muted/80'
                      }`}
                    >
                      {prod.isAvailable ? 'Disponível' : 'Indisponível'}
                    </button>
                    <div className="flex items-center gap-0.5 shrink-0">
                      <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-foreground"
                        onClick={() => openEditProd(prod)}>
                        <Pencil className="h-3 w-3" />
                      </Button>
                      <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-destructive"
                        onClick={() => handleDeleteProd(prod)}>
                        <Trash2 className="h-3 w-3" />
                      </Button>
                    </div>
                  </div>
                ))}
                <div className="px-4 py-3">
                  <Button variant="ghost" size="sm" className="text-orange-500 hover:text-orange-600 hover:bg-orange-50 gap-1 -ml-2"
                    onClick={() => openNewProd(cat.id)}>
                    <Plus className="h-3.5 w-3.5" /> Adicionar produto
                  </Button>
                </div>
              </div>
            )}
          </div>
        ))}
      </div>

      <CategorySheet
        open={catSheet}
        onClose={() => setCatSheet(false)}
        editing={editingCat}
        nextOrder={nextOrder}
        onSaved={onCatSaved}
      />

      <ProductSheet
        open={prodSheet}
        onClose={() => setProdSheet(false)}
        categoryId={targetCatId}
        editing={editingProd}
        onSaved={onProdSaved}
      />
    </div>
  );
}
