'use client';
import { useState } from 'react';
import { MenuResponse } from '@/lib/api';
import { useCart } from '@/store/cart';

type Product = MenuResponse['categories'][0]['products'][0];

export default function ProductModal({
  product,
  establishmentId,
  slug,
  onClose,
}: {
  product: Product;
  establishmentId: string;
  slug: string;
  onClose: () => void;
}) {
  const [quantity, setQuantity] = useState(1);
  const [selectedComplements, setSelectedComplements] = useState<string[]>([]);
  const addItem = useCart((s) => s.addItem);

  const complementsTotal = product.complements
    .filter((c) => selectedComplements.includes(c.id))
    .reduce((sum, c) => sum + c.price, 0);

  const unitTotal = (product.price + complementsTotal) * quantity;

  function toggleComplement(id: string) {
    setSelectedComplements((prev) =>
      prev.includes(id) ? prev.filter((c) => c !== id) : [...prev, id],
    );
  }

  function handleAdd() {
    addItem(
      {
        productId: product.id,
        name: product.name,
        price: product.price,
        quantity,
        complementIds: selectedComplements,
        complementsTotal,
      },
      establishmentId,
      slug,
    );
    onClose();
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40">
      <div className="w-full max-w-lg rounded-t-2xl bg-white p-6">
        <div className="mb-4 flex items-start justify-between">
          <h2 className="text-lg font-bold">{product.name}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">✕</button>
        </div>

        {product.description && (
          <p className="mb-4 text-sm text-gray-500">{product.description}</p>
        )}

        {product.complements.length > 0 && (
          <div className="mb-4">
            <p className="mb-2 font-semibold text-gray-700">Adicionais</p>
            {product.complements.map((c) => (
              <label key={c.id} className="flex items-center justify-between py-2">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={selectedComplements.includes(c.id)}
                    onChange={() => toggleComplement(c.id)}
                    className="h-4 w-4 accent-orange-500"
                  />
                  <span className="text-sm">{c.name}</span>
                </div>
                <span className="text-sm text-orange-500">+R$ {c.price.toFixed(2).replace('.', ',')}</span>
              </label>
            ))}
          </div>
        )}

        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setQuantity((q) => Math.max(1, q - 1))}
              className="flex h-8 w-8 items-center justify-center rounded-full border text-lg font-bold"
            >
              −
            </button>
            <span className="w-6 text-center font-semibold">{quantity}</span>
            <button
              onClick={() => setQuantity((q) => q + 1)}
              className="flex h-8 w-8 items-center justify-center rounded-full border text-lg font-bold"
            >
              +
            </button>
          </div>
          <button
            onClick={handleAdd}
            className="rounded-full bg-orange-500 px-6 py-2 font-semibold text-white hover:bg-orange-600"
          >
            Adicionar · R$ {unitTotal.toFixed(2).replace('.', ',')}
          </button>
        </div>
      </div>
    </div>
  );
}
