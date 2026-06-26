'use client';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { register, saveToken } from '@/lib/api';
import Link from 'next/link';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';

const FIELDS = [
  { name: 'name',     type: 'text',     label: 'Nome completo',        placeholder: 'João Silva' },
  { name: 'email',    type: 'email',    label: 'Email',                placeholder: 'voce@email.com' },
  { name: 'phone',    type: 'tel',      label: 'Telefone (só números)', placeholder: '11999999999' },
  { name: 'password', type: 'password', label: 'Senha',                placeholder: '••••••••' },
] as const;

export default function CadastroPage() {
  const router = useRouter();
  const [form, setForm] = useState({ name: '', email: '', phone: '', password: '' });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await register(form.name, form.email, form.password, form.phone);
      saveToken(res.token);
      router.push('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro no cadastro');
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="min-h-screen flex items-center justify-center bg-muted/30 px-4">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle className="text-2xl">Criar conta</CardTitle>
          <CardDescription>Cadastre-se para acompanhar seus pedidos</CardDescription>
        </CardHeader>
        <CardContent>
          {error && (
            <div className="mb-4 rounded-md bg-destructive/10 px-4 py-3 text-sm text-destructive">
              {error}
            </div>
          )}
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {FIELDS.map(({ name, type, label, placeholder }) => (
              <div key={name} className="flex flex-col gap-1.5">
                <Label htmlFor={name}>{label}</Label>
                <Input
                  id={name}
                  name={name}
                  type={type}
                  placeholder={placeholder}
                  value={form[name]}
                  onChange={handleChange}
                  required
                />
              </div>
            ))}
            <Button
              type="submit"
              disabled={loading}
              className="rounded-full bg-orange-500 hover:bg-orange-600 text-white"
            >
              {loading ? 'Criando conta...' : 'Criar conta'}
            </Button>
          </form>
          <p className="mt-4 text-center text-sm text-muted-foreground">
            Já tem conta?{' '}
            <Link href="/conta/login" className="text-orange-500 font-medium hover:underline">
              Entrar
            </Link>
          </p>
        </CardContent>
      </Card>
    </main>
  );
}
