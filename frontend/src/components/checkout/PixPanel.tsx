'use client';
import { useEffect, useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { useRouter } from 'next/navigation';

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
    <div className="flex flex-col items-center gap-6 py-6">
      <div className="text-center">
        <p className="text-lg font-bold text-gray-800">Pague com PIX</p>
        <p className="text-sm text-gray-500">
          Expira em <span className="font-semibold text-orange-500">{timeLeft}</span>
        </p>
      </div>

      <div className="rounded-2xl border-4 border-orange-100 p-4">
        <QRCodeSVG value={qrCode} size={200} />
      </div>

      <div className="w-full">
        <p className="mb-2 text-sm font-medium text-gray-700">Ou copie o código:</p>
        <div className="flex gap-2">
          <input
            readOnly
            value={copyPaste}
            className="flex-1 rounded-lg border px-3 py-2 text-xs text-gray-600"
          />
          <button
            onClick={handleCopy}
            className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600"
          >
            {copied ? 'Copiado!' : 'Copiar'}
          </button>
        </div>
      </div>

      <p className="text-center text-sm text-gray-500">
        Aguardando confirmação do pagamento...
      </p>
    </div>
  );
}
