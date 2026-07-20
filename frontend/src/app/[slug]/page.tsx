import { fetchMenu } from '@/lib/api';
import { notFound } from 'next/navigation';
import CategoryNav from '@/components/menu/CategoryNav';
import ProductCard from '@/components/menu/ProductCard';
import CartButton from '@/components/cart/CartButton';
import { EstablishmentLogo } from '@/components/menu/EstablishmentLogo';

export default async function MenuPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;

  let menu;
  try {
    menu = await fetchMenu(slug);
  } catch {
    notFound();
  }

  return (
    <main className="min-h-screen bg-gray-50 pb-24">
      <header className="bg-orange-500 px-4 py-6 text-white">
        <div className="flex items-center gap-3">
          <EstablishmentLogo url={menu.logoUrl} name={menu.name} />
          <h1 className="text-2xl font-bold">{menu.name}</h1>
        </div>
      </header>

      {!menu.isOpen && (
        <div className="bg-gray-800 text-white text-center px-4 py-3 text-sm font-medium">
          Estabelecimento fechado no momento — pedidos não estão sendo aceitos.
        </div>
      )}

      <CategoryNav categories={menu.categories} />

      <div className="mx-auto max-w-2xl px-4 py-4">
        {menu.categories.map((cat) => (
          <section key={cat.id} id={`cat-${cat.id}`} className="mb-8">
            <h2 className="mb-3 text-lg font-bold text-gray-800">{cat.name}</h2>
            <div className="flex flex-col gap-3">
              {cat.products.map((product) => (
                <ProductCard
                  key={product.id}
                  product={product}
                  establishmentId={menu.establishmentId}
                  slug={slug}
                  deliveryFee={menu.deliveryFee}
                  isOpen={menu.isOpen}
                />
              ))}
            </div>
          </section>
        ))}
      </div>

      <CartButton slug={slug} isOpen={menu.isOpen} />
    </main>
  );
}
