'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  getGarcons,
  createGarcom,
  updateGarcom,
  deleteGarcom,
  type GarcomData,
} from '@/lib/painelApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Plus, Trash2, KeyRound } from 'lucide-react';

export default function GarconsPage() {
  const [garcons, setGarcons] = useState<GarcomData[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    try {
      setGarcons(await getGarcons());
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim() || !email.trim() || !password.trim()) return;
    setSaving(true);
    setError(null);
    try {
      await createGarcom(name.trim(), email.trim(), password);
      setName('');
      setEmail('');
      setPassword('');
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao criar garçom');
    } finally {
      setSaving(false);
    }
  }

  async function handleResetPassword(id: string) {
    const senha = prompt('Nova senha (mínimo 8 caracteres):');
    if (!senha) return;
    try {
      await updateGarcom(id, { password: senha });
      alert('Senha atualizada.');
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao atualizar senha');
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Excluir este garçom?')) return;
    await deleteGarcom(id);
    await load();
  }

  if (loading) return <p className="text-muted-foreground">Carregando…</p>;

  return (
    <div className="mx-auto max-w-3xl">
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Garçons</h1>
        <p className="text-sm text-muted-foreground">
          Cadastre os garçons que vão usar o app de salão (login em <code>/garcom</code>).
        </p>
      </div>

      <form onSubmit={handleCreate} className="mb-6 grid grid-cols-1 gap-2 sm:grid-cols-4">
        <Input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nome" />
        <Input value={email} onChange={(e) => setEmail(e.target.value)} type="email" placeholder="E-mail" />
        <Input value={password} onChange={(e) => setPassword(e.target.value)} type="password" placeholder="Senha (mín. 8)" />
        <Button type="submit" disabled={saving}>
          <Plus className="mr-1 h-4 w-4" /> Adicionar
        </Button>
      </form>

      {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

      {garcons.length === 0 ? (
        <p className="text-muted-foreground">Nenhum garçom cadastrado ainda.</p>
      ) : (
        <div className="divide-y rounded-xl border bg-white">
          {garcons.map((g) => (
            <div key={g.id} className="flex items-center justify-between gap-3 px-4 py-3">
              <div className="min-w-0">
                <p className="font-medium">{g.name}</p>
                <p className="truncate text-sm text-muted-foreground">{g.email}</p>
              </div>
              <div className="flex shrink-0 gap-1">
                <Button variant="ghost" size="sm" className="text-gray-500" onClick={() => handleResetPassword(g.id)} title="Redefinir senha">
                  <KeyRound className="h-4 w-4" />
                </Button>
                <Button variant="ghost" size="sm" className="text-red-600 hover:text-red-700" onClick={() => handleDelete(g.id)} title="Excluir">
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
