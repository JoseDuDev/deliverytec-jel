import GarcomMesaClient from './GarcomMesaClient';

export default async function GarcomMesaPage({
  params,
}: {
  params: Promise<{ tableId: string }>;
}) {
  const { tableId } = await params;
  return <GarcomMesaClient tableId={tableId} />;
}
