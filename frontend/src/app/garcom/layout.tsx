'use client';

import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { getGarcomToken, getGarcomName, clearGarcomSession } from '@/lib/garcomApi';
import { LogOut } from 'lucide-react';

export default function GarcomLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [name, setName] = useState('');

  useEffect(() => {
    if (pathname !== '/garcom/login' && !getGarcomToken()) {
      router.replace('/garcom/login');
      return;
    }
    setName(getGarcomName());
  }, [pathname, router]);

  if (pathname === '/garcom/login') return <>{children}</>;

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="sticky top-0 z-20 flex items-center justify-between bg-orange-500 px-4 py-3 text-white">
        <div>
          <span className="font-bold">Delify</span>
          <span className="ml-1.5 text-xs opacity-80">Garçom</span>
        </div>
        <div className="flex items-center gap-2 text-sm">
          {name && <span className="opacity-90">{name}</span>}
          <button
            onClick={() => {
              clearGarcomSession();
              router.replace('/garcom/login');
            }}
            className="rounded-full bg-white/20 p-1.5 hover:bg-white/30"
            title="Sair"
          >
            <LogOut className="h-4 w-4" />
          </button>
        </div>
      </header>
      <main className="mx-auto max-w-lg p-4">{children}</main>
    </div>
  );
}
