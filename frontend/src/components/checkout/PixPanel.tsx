'use client';
import { useEffect, useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { useRouter } from 'next/navigation';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';

const IS_DEV =
  process.env.NODE_ENV === 'development' ||
  process.env.NEXT_PUBLIC_DEMO_MODE === 'true';

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
  const [simulating, setSimulating] = useState(false);

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

  // Tracking SSE — redirects automatically when payment confirmed
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

  async function handleSimulate() {
    setSimulating(true);
    try {
      await fetch(`/bff/dev/simulate-payment/${orderId}`, { method: 'POST' });
      // SSE will detect the confirmation and redirect automatically
    } catch {
      setSimulating(false);
    }
  }

  // QR code value: always use copyPaste (EMV string) — it's the actual PIX payload.
  // The qrCode field from Asaas is a pre-rendered PNG image, not a text string.
  const qrValue = copyPaste || qrCode;

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
          <QRCodeSVG value={qrValue} size={200} />
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

        <p className="text-sm text-muted-foreground">
          {simulating ? 'Confirmando pagamento...' : 'Aguardando confirmação do pagamento...'}
        </p>

        {IS_DEV && (
          <Button
            variant="outline"
            size="sm"
            onClick={handleSimulate}
            disabled={simulating}
            className="border-dashed border-yellow-400 text-yellow-700 hover:bg-yellow-50 w-full"
          >
            {simulating ? 'Simulando...' : '⚡ Simular pagamento (dev)'}
          </Button>
        )}
      </CardContent>
    </Card>
  );
}
