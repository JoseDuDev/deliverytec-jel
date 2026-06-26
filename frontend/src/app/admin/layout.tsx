'use client';
import { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { getAdminToken } from '@/lib/adminApi';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (pathname !== '/admin/login' && !getAdminToken()) {
      router.replace('/admin/login');
    }
  }, [pathname, router]);

  if (pathname === '/admin/login') {
    return <>{children}</>;
  }

  return (
    <div className="flex min-h-screen bg-muted/30">
      <aside className="w-56 shrink-0 bg-gray-900 text-white flex flex-col">
        <div className="px-6 py-5">
          <span className="text-lg font-bold text-orange-400">Delify</span>
          <span className="ml-2 text-xs text-gray-400">Admin</span>
        </div>
        <Separator className="bg-gray-700" />
        <nav className="flex-1 px-3 py-4 flex flex-col gap-1">
          <Link
            href="/admin/estabelecimentos"
            className="flex items-center rounded-md px-3 py-2 text-sm font-medium text-gray-300 hover:bg-gray-800 hover:text-white transition-colors"
          >
            Estabelecimentos
          </Link>
        </nav>
        <Separator className="bg-gray-700" />
        <div className="px-4 py-4">
          <Button
            variant="ghost"
            size="sm"
            className="text-gray-400 hover:text-white hover:bg-gray-800 w-full justify-start px-3"
            onClick={() => {
              localStorage.removeItem('delify_admin_token');
              router.replace('/admin/login');
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
