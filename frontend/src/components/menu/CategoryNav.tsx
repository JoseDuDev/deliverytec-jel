'use client';

export default function CategoryNav({
  categories,
}: {
  categories: { id: string; name: string }[];
}) {
  function scrollTo(id: string) {
    document.getElementById(`cat-${id}`)?.scrollIntoView({ behavior: 'smooth' });
  }

  return (
    <nav className="sticky top-0 z-10 flex gap-2 overflow-x-auto bg-white px-4 py-3 shadow-sm">
      {categories.map((cat) => (
        <button
          key={cat.id}
          onClick={() => scrollTo(cat.id)}
          className="shrink-0 rounded-full border border-orange-200 px-4 py-1 text-sm font-medium text-orange-600 hover:bg-orange-50"
        >
          {cat.name}
        </button>
      ))}
    </nav>
  );
}
