'use client';

import { useState } from 'react';

/**
 * Logo do estabelecimento no topo do cardápio.
 *
 * É client component por causa do onError: a URL é digitada pelo lojista e pode
 * apontar para qualquer coisa. Sem o fallback, uma URL quebrada renderiza o
 * ícone de imagem partida no cabeçalho — pior que não mostrar nada. A página de
 * delivery é Server Component, então o tratamento precisa morar aqui.
 */
export function EstablishmentLogo({
  url,
  name,
  className = '',
}: {
  url: string | null;
  name: string;
  className?: string;
}) {
  const [failed, setFailed] = useState(false);

  if (!url || failed) return null;

  return (
    <img
      // key: sem ele o React reaproveita o elemento e uma URL que falhou antes
      // não volta a ser tentada quando o lojista corrige o cadastro.
      key={url}
      src={url}
      alt={name}
      className={`h-12 w-12 shrink-0 rounded-lg bg-white/20 object-cover ${className}`}
      onError={() => setFailed(true)}
    />
  );
}
