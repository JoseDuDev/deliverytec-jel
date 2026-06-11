import StatusStepper from '@/components/tracking/StatusStepper';

export default async function TrackingPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <main className="mx-auto max-w-lg px-4 py-8">
      <h1 className="mb-2 text-2xl font-bold">Acompanhe seu pedido</h1>
      <p className="mb-8 text-sm text-gray-500">Pedido #{id.slice(0, 8).toUpperCase()}</p>
      <StatusStepper orderId={id} />
    </main>
  );
}
