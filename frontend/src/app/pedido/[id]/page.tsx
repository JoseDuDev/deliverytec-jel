import StatusStepper from '@/components/tracking/StatusStepper';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export default async function TrackingPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <main className="mx-auto max-w-lg px-4 py-8">
      <h1 className="mb-1 text-2xl font-bold tracking-tight">Acompanhe seu pedido</h1>
      <p className="mb-6 text-sm text-muted-foreground">
        Pedido #{id.slice(0, 8).toUpperCase()}
      </p>
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Status</CardTitle>
        </CardHeader>
        <CardContent>
          <StatusStepper orderId={id} />
        </CardContent>
      </Card>
    </main>
  );
}
