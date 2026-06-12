export default function Home() {
  return (
    <div className="flex flex-col flex-1 items-center justify-center min-h-screen bg-zinc-50">
      <div className="text-center px-8">
        <h1 className="text-4xl font-bold text-zinc-900 mb-3">Delify</h1>
        <p className="text-zinc-500 text-lg">Acesse o cardápio do seu estabelecimento pelo link fornecido.</p>
        <p className="text-zinc-400 text-sm mt-4">Exemplo: <span className="font-mono bg-zinc-100 px-2 py-1 rounded">/burger-smoke</span></p>
      </div>
    </div>
  );
}
