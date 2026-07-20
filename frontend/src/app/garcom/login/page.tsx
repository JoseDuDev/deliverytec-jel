'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { garcomLogin, saveGarcomSession } from '@/lib/garcomApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export default function GarcomLoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const r = await garcomLogin(email, password);
      saveGarcomSession(r.token, r.name);
      router.replace('/garcom');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao entrar');
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-gray-50 px-4">
      <form
        onSubmit={submit}
        className="w-full max-w-sm rounded-2xl border bg-white p-6 shadow-sm"
      >
        <div className="mb-6 text-center">
          <span className="text-xl font-bold text-orange-500">Delify</span>
          <p className="text-sm text-muted-foreground">App do garçom</p>
        </div>

        <div className="mb-3">
          <Label htmlFor="email" className="mb-1 block text-sm">E-mail</Label>
          <Input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </div>
        <div className="mb-4">
          <Label htmlFor="password" className="mb-1 block text-sm">Senha</Label>
          <Input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </div>

        {error && <p className="mb-3 text-sm text-red-600">{error}</p>}

        <Button type="submit" disabled={busy} className="w-full bg-orange-500 text-white hover:bg-orange-600">
          {busy ? 'Entrando…' : 'Entrar'}
        </Button>
      </form>
    </main>
  );
}
