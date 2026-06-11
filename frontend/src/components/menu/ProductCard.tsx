'use client';
import { useState } from 'react';
import { MenuResponse } from '@/lib/api';
import ProductModal from './ProductModal';

type Product = MenuResponse['categories'][0]['products'][0];

export default function ProductCard({
  product,
  establishmentId,
  slug,
}: {
  product: Product;
  establishmentId: string;
  slug: string;
}) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="flex w-full items-start gap-3 rounded-xl border border-gray-100 bg-white p-4 text-left shadow-sm hover:shadow-md transition-shadow"
      >
        <div className="flex-1">
          <p className="font-semibold text-gray-900">{product.name}</p>
          {product.description && (
            <p className="mt-1 text-sm text-gray-500 line-clamp-2">{product.description}</p>
          )}
          <p className="mt-2 font-bold text-orange-500">
            R$ {product.price.toFixed(2).replace('.', ',')}
          </p>
        </div>
        {product.imageUrl && (
          <img
            src={product.imageUrl}
            alt={product.name}
            className="h-20 w-20 rounded-lg object-cover"
          />
        )}
      </button>

      {open && (
        <ProductModal
          product={product}
          establishmentId={establishmentId}
          slug={slug}
          onClose={() => setOpen(false)}
        />
      )}
    </>
  );
}
