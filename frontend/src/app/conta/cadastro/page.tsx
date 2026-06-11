'use client';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { register, saveToken } from '@/lib/api';
import Link from 'next/link';

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
    <main className="mx-auto max-w-sm px-4 py-12">
      <h1 className="mb-6 text-2xl font-bold">Criar conta</h1>
      {error && <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</div>}
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {[
          { name: 'name', type: 'text', placeholder: 'Nome completo' },
          { name: 'email', type: 'email', placeholder: 'Email' },
          { name: 'phone', type: 'tel', placeholder: 'Telefone (só números)' },
          { name: 'password', type: 'password', placeholder: 'Senha' },
        ].map(({ name, type, placeholder }) => (
          <input
            key={name}
            name={name}
            type={type}
            placeholder={placeholder}
            value={form[name as keyof typeof form]}
            onChange={handleChange}
            required
            className="rounded-lg border px-4 py-2 focus:outline-none focus:ring-2 focus:ring-orange-400"
          />
        ))}
        <button
          type="submit"
          disabled={loading}
          className="rounded-full bg-orange-500 py-3 font-semibold text-white disabled:opacity-60 hover:bg-orange-600"
        >
          {loading ? 'Criando conta...' : 'Criar conta'}
        </button>
      </form>
      <p className="mt-4 text-center text-sm text-gray-500">
        Já tem conta?{' '}
        <Link href="/conta/login" className="text-orange-500 font-medium">
          Entrar
        </Link>
      </p>
    </main>
  );
}
