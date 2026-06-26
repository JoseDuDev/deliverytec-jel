'use client';
import { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { getPainelToken } from '@/lib/painelApi';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { LayoutDashboard, UtensilsCrossed, ClipboardList, Settings } from 'lucide-react';

const NAV = [
  { href: '/painel',                  label: 'Dashboard',     icon: LayoutDashboard },
  { href: '/painel/cardapio',         label: 'Cardápio',      icon: UtensilsCrossed },
  { href: '/painel/pedidos',          label: 'Pedidos',       icon: ClipboardList },
  { href: '/painel/configuracoes',    label: 'Configurações', icon: Settings },
];

export default function PainelLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (pathname !== '/painel/login' && !getPainelToken()) {
      router.replace('/painel/login');
    }
  }, [pathname, router]);

  if (pathname === '/painel/login') return <>{children}</>;

  return (
    <div className="flex min-h-screen bg-muted/30">
      <aside className="w-60 shrink-0 bg-gray-900 text-white flex flex-col">
        <div className="px-6 py-5">
          <span className="text-lg font-bold text-orange-400">Delify</span>
          <span className="ml-2 text-xs text-gray-400">Painel</span>
        </div>
        <Separator className="bg-gray-700" />
        <nav className="flex-1 px-3 py-4 flex flex-col gap-1">
          {NAV.map(({ href, label, icon: Icon }) => {
            const active = pathname === href;
            return (
              <Link
                key={href}
                href={href}
                className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                  active
                    ? 'bg-orange-500 text-white'
                    : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                }`}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {label}
              </Link>
            );
          })}
        </nav>
        <Separator className="bg-gray-700" />
        <div className="px-4 py-4">
          <Button
            variant="ghost"
            size="sm"
            className="text-gray-400 hover:text-white hover:bg-gray-800 w-full justify-start px-3"
            onClick={() => {
              localStorage.removeItem('delify_painel_token');
              router.replace('/painel/login');
            }}
          >
            Sair
          </Button>
        </div>
      </aside>
      <main className="flex-1 p-8 overflow-auto">{children}</main>
    </div>
  );
}
