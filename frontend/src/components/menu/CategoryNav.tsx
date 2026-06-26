'use client';
import { Button } from '@/components/ui/button';

export default function CategoryNav({
  categories,
}: {
  categories: { id: string; name: string }[];
}) {
  function scrollTo(id: string) {
    document.getElementById(`cat-${id}`)?.scrollIntoView({ behavior: 'smooth' });
  }

  return (
    <nav className="sticky top-0 z-10 flex gap-2 overflow-x-auto bg-background px-4 py-3 shadow-sm">
      {categories.map((cat) => (
        <Button
          key={cat.id}
          variant="outline"
          size="sm"
          onClick={() => scrollTo(cat.id)}
          className="shrink-0 rounded-full border-orange-200 text-orange-600 hover:bg-orange-50 hover:text-orange-700"
        >
          {cat.name}
        </Button>
      ))}
    </nav>
  );
}
