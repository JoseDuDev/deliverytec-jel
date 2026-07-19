import MesaClient from './MesaClient';

export default async function MesaPage({
  params,
}: {
  params: Promise<{ slug: string; token: string }>;
}) {
  const { token } = await params;
  return <MesaClient token={token} />;
}
