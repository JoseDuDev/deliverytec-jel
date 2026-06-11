import { fetchMenu } from '@/lib/api';
import { notFound } from 'next/navigation';
import CategoryNav from '@/components/menu/CategoryNav';
import ProductCard from '@/components/menu/ProductCard';
import CartButton from '@/components/cart/CartButton';

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
        <h1 className="text-2xl font-bold">{menu.name}</h1>
      </header>

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
                />
              ))}
            </div>
          </section>
        ))}
      </div>

      <CartButton slug={slug} />
    </main>
  );
}
