'use client';
import { useState } from 'react';
import { MenuResponse } from '@/lib/api';
import { useCart } from '@/store/cart';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Separator } from '@/components/ui/separator';

type Product = MenuResponse['categories'][0]['products'][0];

export default function ProductModal({
  product,
  establishmentId,
  slug,
  deliveryFee,
  isOpen,
  onClose,
}: {
  product: Product;
  establishmentId: string;
  slug: string;
  deliveryFee: number;
  isOpen: boolean;
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
      deliveryFee,
    );
    onClose();
  }

  return (
    <Sheet open onOpenChange={(open) => !open && onClose()}>
      <SheetContent side="bottom" className="rounded-t-2xl max-w-lg mx-auto px-6 pb-8">
        {product.imageUrl && (
          <img
            key={product.imageUrl}
            src={product.imageUrl}
            alt={product.name}
            className="mb-4 h-48 w-full rounded-xl object-cover"
            onError={(e) => { e.currentTarget.style.display = 'none'; }}
          />
        )}

        <SheetHeader className="mb-4 text-left">
          <SheetTitle>{product.name}</SheetTitle>
        </SheetHeader>

        {product.description && (
          <p className="mb-4 text-sm text-muted-foreground">{product.description}</p>
        )}

        {product.complements.length > 0 && (
          <>
            <p className="mb-3 font-semibold text-sm">Adicionais</p>
            <div className="mb-4 flex flex-col gap-2">
              {product.complements.map((c) => (
                <label
                  key={c.id}
                  className="flex items-center justify-between py-2 cursor-pointer"
                >
                  <div className="flex items-center gap-3">
                    <Checkbox
                      id={c.id}
                      checked={selectedComplements.includes(c.id)}
                      onCheckedChange={() => toggleComplement(c.id)}
                      className="border-orange-300 data-[state=checked]:bg-orange-500 data-[state=checked]:border-orange-500"
                    />
                    <span className="text-sm">{c.name}</span>
                  </div>
                  <span className="text-sm text-orange-500">
                    +R$ {c.price.toFixed(2).replace('.', ',')}
                  </span>
                </label>
              ))}
            </div>
            <Separator className="mb-4" />
          </>
        )}

        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Button
              variant="outline"
              size="icon"
              className="h-8 w-8 rounded-full"
              onClick={() => setQuantity((q) => Math.max(1, q - 1))}
            >
              −
            </Button>
            <span className="w-6 text-center font-semibold">{quantity}</span>
            <Button
              variant="outline"
              size="icon"
              className="h-8 w-8 rounded-full"
              onClick={() => setQuantity((q) => q + 1)}
            >
              +
            </Button>
          </div>
          <Button
            onClick={handleAdd}
            disabled={!isOpen}
            className="rounded-full bg-orange-500 hover:bg-orange-600 text-white px-6 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isOpen ? `Adicionar · R$ ${unitTotal.toFixed(2).replace('.', ',')}` : 'Estabelecimento fechado'}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
