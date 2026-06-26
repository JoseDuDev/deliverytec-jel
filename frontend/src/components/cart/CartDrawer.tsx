'use client';
import { useRouter } from 'next/navigation';
import { useCart } from '@/store/cart';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';

export default function CartDrawer({
  slug,
  onClose,
}: {
  slug: string;
  onClose: () => void;
}) {
  const router = useRouter();
  const { items, updateQuantity, removeItem, total } = useCart();

  function goToCheckout() {
    onClose();
    router.push(`/${slug}/checkout`);
  }

  return (
    <Sheet open onOpenChange={(open) => !open && onClose()}>
      <SheetContent side="bottom" className="rounded-t-2xl max-w-lg mx-auto px-0 pb-0">
        <SheetHeader className="px-4 pb-3">
          <SheetTitle>Seu pedido</SheetTitle>
        </SheetHeader>
        <Separator />

        <ScrollArea className="max-h-72">
          <div className="px-4 py-2">
            {items.map((item) => (
              <div key={item.productId} className="flex items-center justify-between py-3">
                <div className="flex-1">
                  <p className="font-medium text-sm">{item.name}</p>
                  <p className="text-sm text-orange-500">
                    R$ {((item.price + item.complementsTotal) * item.quantity).toFixed(2).replace('.', ',')}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="icon"
                    className="h-7 w-7 rounded-full"
                    onClick={() => updateQuantity(item.productId, item.quantity - 1)}
                  >
                    −
                  </Button>
                  <span className="w-5 text-center text-sm font-semibold">{item.quantity}</span>
                  <Button
                    variant="outline"
                    size="icon"
                    className="h-7 w-7 rounded-full"
                    onClick={() => updateQuantity(item.productId, item.quantity + 1)}
                  >
                    +
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7 text-muted-foreground hover:text-destructive"
                    onClick={() => removeItem(item.productId)}
                  >
                    🗑
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </ScrollArea>

        <Separator />
        <div className="px-4 py-4 space-y-3">
          <div className="flex items-center justify-between font-bold">
            <span>Total</span>
            <span>R$ {total().toFixed(2).replace('.', ',')}</span>
          </div>
          <Button
            onClick={goToCheckout}
            className="w-full rounded-full bg-orange-500 hover:bg-orange-600 text-white"
          >
            Ir para o checkout
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
