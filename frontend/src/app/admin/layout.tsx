'use client';
import { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { getAdminToken } from '@/lib/adminApi';
import Link from 'next/link';

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
    <div className="flex min-h-screen bg-gray-100">
      <aside className="w-56 shrink-0 bg-gray-900 text-white flex flex-col">
        <div className="px-6 py-5 border-b border-gray-700">
          <span className="text-lg font-bold text-orange-400">Delify</span>
          <span className="ml-2 text-xs text-gray-400">Admin</span>
        </div>
        <nav className="flex-1 px-3 py-4">
          <Link
            href="/admin/estabelecimentos"
            className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-gray-300 hover:bg-gray-800 hover:text-white"
          >
            Estabelecimentos
          </Link>
        </nav>
        <div className="px-4 py-4 border-t border-gray-700">
          <button
            onClick={() => {
              localStorage.removeItem('delify_admin_token');
              router.replace('/admin/login');
            }}
            className="text-xs text-gray-500 hover:text-gray-300"
          >
            Sair
          </button>
        </div>
      </aside>
      <main className="flex-1 p-8 overflow-auto">{children}</main>
    </div>
  );
}
