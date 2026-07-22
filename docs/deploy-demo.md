# Deploy do demo (grátis)

Runbook para publicar o Delify num ambiente externo de **demonstração**, sem custo.
Não é um guia de produção — ver "Ressalvas" no fim.

## Arquitetura

```
Navegador ──► Vercel (Next.js)  ──proxy──►  Render (.NET 10, Docker)  ──►  Neon (Postgres)
             frontend, grátis              backend, grátis                 grátis
```

- **Sem RabbitMQ** (transporte in-memory) e **sem Redis** (não é usado). Já verificado: o
  `SessionPaidConsumer` fecha a comanda pelo bus in-memory, sem broker.
- Pagamento é **simulado** (stub + página de checkout falsa servida pelo backend).

## O que já está pronto no código

Tudo abaixo foi verificado com Docker local em 22/07/2026 — imagem builda, container sobe
contra Postgres com as env vars deste runbook (6 migrations aplicam, bus vira `loopback://`,
`/health` 200) e o seed roda e é idempotente. **Não precisa refazer essa verificação**; se
algo falhar no deploy, é configuração do Render/Vercel, não o código.

- `Dockerfile` do backend (imagens oficiais .NET 10 — ignora o SDK preview local).
  Lista os `.csproj` um a um antes do `restore` — **ao adicionar um módulo novo ao
  `Delify.Api`, adicione o `COPY` correspondente**, senão o build quebra no restore.
- Transporte in-memory quando `RabbitMQ__Enabled=false`.
- URL do backend no frontend via `BACKEND_URL` (proxy do Next).
- Botões de "simular pagamento" aparecem com `NEXT_PUBLIC_DEMO_MODE=true`.
- `scripts/seed-demo.mjs` — popula uma base vazia via API.

## O que VOCÊ cria (contas grátis)

[Neon](https://neon.tech) · [Render](https://render.com) · [Vercel](https://vercel.com).
Todas com login por GitHub. (Não crio contas por você.)

---

## Passo 1 — Postgres (Neon)

1. New Project → copie a connection string.
2. Converta para o formato Npgsql (o app não aceita a URL `postgresql://` crua):

   ```
   Host=ep-xxx-xxx.aws.neon.tech;Database=neondb;Username=xxx;Password=xxx;SslMode=Require;TrustServerCertificate=true
   ```

   **`SslMode=Require` é obrigatório** — o Neon recusa conexão sem TLS. (O Npgsql
   também aceita `SSL Mode` / `Trust Server Certificate` com espaços; a grafia sem
   espaços evita problema ao colar em campos que fazem parsing próprio.)

3. Use o **host direto, não o "pooler host"**. O pooler é PgBouncer em modo
   *transaction*, e o app roda `MigrateAsync()` no startup — DDL em transação com
   PgBouncer quebra de formas difíceis de diagnosticar. Um container sempre ligado,
   com o pool do Npgsql na frente, não precisa do pooler.

O schema é criado sozinho: o `Program.cs` roda `MigrateAsync()` no start.

## Passo 2 — Backend (Render)

1. New → **Web Service** → conecte o repo → **Runtime: Docker**.

   Em Settings → Build & Deploy:

   | Campo | Valor |
   |---|---|
   | Dockerfile Path | `./src/Delify.Api/Dockerfile` |
   | Docker Build Context Directory | `.` |

   **O `./` na frente importa** — sem ele o Render ignora o caminho, procura um
   `Dockerfile` na raiz e falha com `failed to read dockerfile: no such file or
   directory` (o log mostra `transferring dockerfile: 2B`, sinal de que não achou nada).
   O contexto precisa ser a raiz: o Dockerfile faz `COPY ["src/Delify.Api/...]` a
   partir dela.

2. Anote a URL que o Render atribui (ex.: `https://delify-api.onrender.com`).
3. Environment → adicione:

   | Chave | Valor | Por quê |
   |---|---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Development` | Habilita os endpoints de simulação de pagamento do demo |
   | `ASPNETCORE_HTTP_PORTS` | `10000` | Render roteia para a porta `PORT` (default 10000); o Kestrel precisa escutar nela |
   | `ConnectionStrings__Delify` | *(string do Passo 1)* | Banco |
   | `RabbitMQ__Enabled` | `false` | Liga o transporte in-memory |
   | `Dev__PublicApiUrl` | *(a própria URL do Render)* | O checkout de cartão redireciona o cliente para `${essa URL}/bff/dev/checkout/...`; sem isso vai para localhost e quebra |
   | `Jwt__Key` | *(uma frase secreta de 32+ caracteres)* | O default é um placeholder |

4. **Confirme que as 6 aparecem na aba Environment e clique em Save Changes.** O
   Render só aplica no deploy seguinte (salvar já dispara um). Duas armadilhas:

   - `ConnectionStrings__Delify` tem **dois underscores**. Com um só, a variável é
     ignorada em silêncio e o app cai no default do `appsettings.json`
     (`Host=localhost;Port=5435`) — o sintoma é `SocketException (111): Connection
     refused` em `127.0.0.1:5435` durante o `MigrateAsync`, seguido de
     `Exited with status 139`. Não é erro de código nem do Neon: é a env var faltando.
   - O valor não pode estar entre aspas — o Npgsql leria o `"` como parte do host.

5. Deploy. O primeiro build leva alguns minutos (restore + publish .NET).

## Passo 3 — Frontend (Vercel)

1. Import Project → o repo → **Root Directory: `frontend`**.
2. Environment Variables:

   | Chave | Valor |
   |---|---|
   | `BACKEND_URL` | a URL do Render (Passo 2) |
   | `NEXT_PUBLIC_DEMO_MODE` | `true` |

3. Deploy. Anote a URL (ex.: `https://delify-demo.vercel.app`).

## Passo 4 — Seed

Com o backend no ar, popule a base pela API:

```bash
FRONT_URL=https://delify-demo.vercel.app  node scripts/seed-demo.mjs  https://delify-api.onrender.com
```

Ele imprime as URLs e credenciais do demo (cardápio, painel, garçom, admin, QRs das mesas).
É idempotente — pode rodar de novo.

## Credenciais do demo (definidas no seed)

| Área | Login |
|---|---|
| Painel do lojista | `demo@delify.com` / `Demo@123` |
| App do garçom | `garcom@delify.com` / `Demo@123` |
| Admin | `admin@delify.com` / `Admin@123` |

Troque no `scripts/seed-demo.mjs` se for deixar público por muito tempo.

---

## Ressalvas (ler antes de apresentar)

1. **Cold start do Render — e do Neon.** O tier grátis do Render dorme após 15 min sem
   acesso e leva ~1 min para acordar; o Neon free suspende o compute após ~5 min ocioso.
   Os dois somam. **Abra a URL 1 minuto antes de apresentar** para esquentar.
2. **Tempo real via proxy (SSE).** As telas ao vivo por *polling* — status da conta,
   divisão — são robustas. As por *SSE* — quadro da cozinha, feed de chamadas do garçom —
   passam pelo proxy Vercel→Render e **podem não fazer streaming suave** (buffer/timeout do
   proxy). Se travar, o "Atualizar" manual resolve. Não é bug do app, é limitação do
   proxy no tier grátis.
3. **Pagamento é simulado.** Cartão: redireciona para a página de checkout falsa do
   backend (aprovar/recusar). PIX: gera um QR fake e confirma pelo botão "⚡ Simular"
   (visível por causa do `NEXT_PUBLIC_DEMO_MODE`). Nada cobra de verdade — deixe isso
   claro para quem assistir.
4. **Não é config de produção.** `ASPNETCORE_ENVIRONMENT=Development` liga páginas de erro
   detalhadas e os endpoints de simulação. Bom para demo, não para produção real. O
   caminho do Asaas de verdade continua sem ter sido exercitado (precisa de credencial
   sandbox + webhook público).
5. **Fotos placeholder.** O seed usa imagens genéricas (picsum). Troque por fotos de
   comida no painel para o demo ficar melhor.
