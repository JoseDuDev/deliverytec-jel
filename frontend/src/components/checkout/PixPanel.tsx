'use client';
import { useEffect, useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { useRouter } from 'next/navigation';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';

export default function PixPanel({
  orderId,
  qrCode,
  copyPaste,
  expiresAt,
}: {
  orderId: string;
  qrCode: string;
  copyPaste: string;
  expiresAt: string;
}) {
  const router = useRouter();
  const [copied, setCopied] = useState(false);
  const [timeLeft, setTimeLeft] = useState('');

  useEffect(() => {
    const interval = setInterval(() => {
      const diff = new Date(expiresAt).getTime() - Date.now();
      if (diff <= 0) {
        setTimeLeft('Expirado');
        clearInterval(interval);
        return;
      }
      const m = Math.floor(diff / 60000);
      const s = Math.floor((diff % 60000) / 1000);
      setTimeLeft(`${m}:${s.toString().padStart(2, '0')}`);
    }, 1000);
    return () => clearInterval(interval);
  }, [expiresAt]);

  useEffect(() => {
    const es = new EventSource(`/bff/orders/${orderId}/track`);
    es.addEventListener('status-changed', (e) => {
      const data = JSON.parse(e.data);
      if (data.status === 'Confirmed') {
        es.close();
        router.push(`/pedido/${orderId}`);
      }
    });
    return () => es.close();
  }, [orderId, router]);

  function handleCopy() {
    navigator.clipboard.writeText(copyPaste);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <Card className="w-full">
      <CardContent className="flex flex-col items-center gap-6 py-8">
        <div className="text-center">
          <p className="text-xl font-bold">Pague com PIX</p>
          <p className="text-sm text-muted-foreground mt-1">
            Expira em{' '}
            <span className="font-semibold text-orange-500">{timeLeft}</span>
          </p>
        </div>

        <div className="rounded-2xl border-4 border-orange-100 p-4">
          <QRCodeSVG value={qrCode} size={200} />
        </div>

        <Separator className="w-full" />

        <div className="w-full space-y-2">
          <p className="text-sm font-medium text-muted-foreground">Ou copie o código PIX:</p>
          <div className="flex gap-2">
            <Input readOnly value={copyPaste} className="text-xs text-muted-foreground" />
            <Button
              onClick={handleCopy}
              className="bg-orange-500 hover:bg-orange-600 text-white shrink-0"
            >
              {copied ? 'Copiado!' : 'Copiar'}
            </Button>
          </div>
        </div>

        <p className="text-sm text-muted-foreground">Aguardando confirmação do pagamento...</p>
      </CardContent>
    </Card>
  );
}
