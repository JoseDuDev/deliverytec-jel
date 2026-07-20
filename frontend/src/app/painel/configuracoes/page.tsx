'use client';
import { useEffect, useState } from 'react';
import { getDashboard, updateEstabelecimento, DashboardData } from '@/lib/painelApi';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Checkbox } from '@/components/ui/checkbox';

export default function ConfiguracoesPage() {
  const [data, setData]       = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving]   = useState(false);
  const [saved, setSaved]     = useState(false);
  const [error, setError]     = useState<string | null>(null);

  const [name, setName]               = useState('');
  const [description, setDescription] = useState('');
  const [logoUrl, setLogoUrl]         = useState('');
  const [deliveryFee, setDeliveryFee] = useState(0);
  const [serviceFeeEnabled, setServiceFeeEnabled] = useState(true);
  const [serviceFeePercent, setServiceFeePercent] = useState(10);

  useEffect(() => {
    getDashboard()
      .then((d) => {
        setData(d);
        setName(d.name);
        setDescription(d.description ?? '');
        setLogoUrl(d.logoUrl ?? '');
        setDeliveryFee(d.deliveryFee ?? 0);
        setServiceFeeEnabled(d.serviceFeeEnabled ?? true);
        setServiceFeePercent(d.serviceFeePercent ?? 10);
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    setSaved(false);
    setError(null);
    try {
      const updated = await updateEstabelecimento({
        name,
        description: description || null,
        logoUrl: logoUrl || null,
        deliveryFee,
        serviceFeeEnabled,
        serviceFeePercent,
      });
      setData((d) => d && { ...d, ...updated });
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>;
  if (!data)   return <p className="text-muted-foreground">Estabelecimento não encontrado.</p>;

  return (
    <div className="space-y-6 max-w-xl">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Configurações</h1>
        <p className="text-sm text-muted-foreground mt-0.5">Dados do seu estabelecimento</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Informações gerais</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSave} className="flex flex-col gap-5">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="slug">Slug (URL)</Label>
              <Input id="slug" value={data.slug} disabled className="font-mono text-muted-foreground" />
              <p className="text-xs text-muted-foreground">O slug não pode ser alterado.</p>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="name">Nome do estabelecimento *</Label>
              <Input
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                placeholder="Ex: Hamburgueria do João"
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="description">Descrição</Label>
              <Textarea
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Conta um pouco sobre o seu estabelecimento..."
                rows={3}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="deliveryFee">Taxa de entrega (R$)</Label>
              <Input
                id="deliveryFee"
                type="number"
                min={0}
                step={0.01}
                value={deliveryFee}
                onChange={(e) => setDeliveryFee(Math.max(0, parseFloat(e.target.value) || 0))}
                placeholder="0,00"
              />
              <p className="text-xs text-muted-foreground">
                {deliveryFee === 0 ? 'Entrega grátis — será exibida como gratuita no cardápio.' : `R$ ${deliveryFee.toFixed(2).replace('.', ',')} adicionado ao total do pedido.`}
              </p>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label>Taxa de serviço (mesa)</Label>
              <label className="flex cursor-pointer items-center gap-2">
                <Checkbox
                  checked={serviceFeeEnabled}
                  onCheckedChange={(v) => setServiceFeeEnabled(!!v)}
                  className="border-orange-300 data-[state=checked]:bg-orange-500 data-[state=checked]:border-orange-500"
                />
                <span className="text-sm">Cobrar taxa de serviço na conta da mesa</span>
              </label>
              <div className="flex items-center gap-2">
                <Input
                  type="number"
                  min={0}
                  step={0.5}
                  value={serviceFeePercent}
                  onChange={(e) => setServiceFeePercent(Math.max(0, parseFloat(e.target.value) || 0))}
                  disabled={!serviceFeeEnabled}
                  className="w-28"
                />
                <span className="text-sm text-muted-foreground">% do total</span>
              </div>
              <p className="text-xs text-muted-foreground">
                Aplicada no fechamento da conta da mesa. O cliente pode optar por não pagar.
              </p>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label htmlFor="logoUrl">URL da logo</Label>
              <Input
                id="logoUrl"
                type="url"
                value={logoUrl}
                onChange={(e) => setLogoUrl(e.target.value)}
                placeholder="https://exemplo.com/logo.png"
              />
              {logoUrl && (
                <img
                  src={logoUrl}
                  alt="Preview da logo"
                  className="mt-2 h-16 w-16 rounded-md object-cover border"
                  onError={(e) => (e.currentTarget.style.display = 'none')}
                />
              )}
            </div>

            {error && (
              <p className="text-sm text-destructive">{error}</p>
            )}

            <div className="flex items-center gap-3">
              <Button
                type="submit"
                disabled={saving}
                className="bg-orange-500 hover:bg-orange-600 text-white"
              >
                {saving ? 'Salvando...' : 'Salvar alterações'}
              </Button>
              {saved && (
                <span className="text-sm text-emerald-600 font-medium">Salvo com sucesso!</span>
              )}
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
