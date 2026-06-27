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

const TERMINAL = new Set(['Delivered', 'Cancelled']);

export function useOrderTracking(orderId: string) {
  const [currentStatus, setCurrentStatus] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  // Fetch current status on mount — order may already be in a later state
  useEffect(() => {
    fetch(`/bff/orders/${orderId}/status`)
      .then((r) => r.ok ? r.json() : null)
      .then((d) => {
        if (!d) return;
        setCurrentStatus(d.status);
        if (TERMINAL.has(d.status)) setDone(true);
      })
      .catch(() => {});
  }, [orderId]);

  // SSE for live future updates
  useEffect(() => {
    const es = new EventSource(`/bff/orders/${orderId}/track`);

    es.addEventListener('status-changed', (e) => {
      const data: TrackingStatus = JSON.parse(e.data);
      setCurrentStatus(data.status);

      if (TERMINAL.has(data.status)) {
        setDone(true);
        es.close();
      }
    });

    es.onerror = () => es.close();

    return () => es.close();
  }, [orderId]);

  return { currentStatus, done, steps: STEPS };
}
