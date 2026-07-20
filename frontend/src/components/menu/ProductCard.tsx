'use client';
import { useState } from 'react';
import { MenuResponse } from '@/lib/api';
import ProductModal from './ProductModal';

type Product = MenuResponse['categories'][0]['products'][0];

export default function ProductCard({
  product,
  establishmentId,
  slug,
  deliveryFee,
  isOpen,
}: {
  product: Product;
  establishmentId: string;
  slug: string;
  deliveryFee: number;
  isOpen: boolean;
}) {
  const [open, setOpen] = useState(false);
  const unavailable = !product.isAvailable;

  return (
    <>
      {/* Indisponível continua clicável (dá pra ver a ficha), mas esmaecido e
          com o botão de adicionar bloqueado dentro do modal. */}
      <button
        onClick={() => setOpen(true)}
        className={`flex w-full items-start gap-3 rounded-xl border border-gray-100 bg-white p-4 text-left shadow-sm transition-shadow hover:shadow-md ${
          unavailable ? 'opacity-60' : ''
        }`}
      >
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <p className="font-semibold text-gray-900">{product.name}</p>
            {unavailable && (
              <span className="shrink-0 rounded-full bg-gray-200 px-2 py-0.5 text-xs font-medium text-gray-600">
                Indisponível
              </span>
            )}
          </div>
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
          deliveryFee={deliveryFee}
          isOpen={isOpen}
          onClose={() => setOpen(false)}
        />
      )}
    </>
  );
}
