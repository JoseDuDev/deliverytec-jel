'use client';
import { useEffect, useState } from 'react';

export type TrackingStatus = {
  status: string;
  label: string;
  at: string;
};

const STEPS = [
  { key: 'Pending',        label: 'Aguardando pagamento' },
  { key: 'Confirmed',      label: 'Pagamento confirmado' },
  { key: 'Preparing',      label: 'Preparando seu pedido' },
  { key: 'OutForDelivery', label: 'Saiu para entrega' },
  { key: 'Delivered',      label: 'Pedido entregue!' },
];

export function useOrderTracking(orderId: string) {
  const [history, setHistory] = useState<TrackingStatus[]>([]);
  const [currentStatus, setCurrentStatus] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  useEffect(() => {
    const es = new EventSource(`/bff/orders/${orderId}/track`);

    es.addEventListener('status-changed', (e) => {
      const data: TrackingStatus = JSON.parse(e.data);
      setCurrentStatus(data.status);
      setHistory((prev) => [...prev, data]);

      if (data.status === 'Delivered' || data.status === 'Cancelled') {
        setDone(true);
        es.close();
      }
    });

    es.onerror = () => es.close();

    return () => es.close();
  }, [orderId]);

  return { history, currentStatus, done, steps: STEPS };
}
