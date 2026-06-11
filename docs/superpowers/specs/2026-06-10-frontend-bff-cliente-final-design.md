# Frontend + BFF — Cliente Final Delify

**Data:** 2026-06-10
**Status:** Aprovado
**Escopo:** PWA Next.js + módulo BFF .NET para o fluxo do cliente final (cardápio → carrinho → checkout PIX → acompanhamento em tempo real)

---

## 1. Visão Geral

O cliente final acessa o cardápio de um estabelecimento via URL pública por slug (`/burger-king-centro`) ou QR Code que aponta para a mesma rota. Ele adiciona itens ao carrinho, faz checkout com PIX, e acompanha o status do pedido em tempo real via Server-Sent Events (SSE).

O sistema é composto por dois artefatos:

- **`Delify.Modules.Bff`** — novo módulo no monolith .NET 10, expõe endpoints moldados para o cliente final, agrega internamente Catalog/Orders/Payments via DI
- **`/frontend`** — aplicação Next.js (React) configurada como PWA, na raiz do monorepo

---

## 2. Arquitetura

```
┌─────────────────────────────────────────────────────┐
│                   Monorepo Delify                    │
│                                                      │
│  /frontend (Next.js PWA)                            │
│       │  HTTP (browser)                             │
│       ▼                                              │
│  Delify.Api                                         │
│       │  IModule.MapEndpoints                       │
│       ▼                                              │
│  Delify.Modules.Bff  ◄──── agrega internamente ───► │
│       │         │         Catalog / Orders /        │
│       │         │         Payments / Delivery       │
│       │         │                                   │
│       │         └── SSE /bff/orders/{id}/track      │
│       └────────────── REST /bff/menu/{slug}         │
│                            /bff/orders              │
│                            /bff/auth/*              │
└─────────────────────────────────────────────────────┘
```

**Princípio fundamental:** o frontend nunca fala diretamente com `/api/catalog`, `/api/orders` ou `/api/payments`. Todo acesso passa pelo BFF, que expõe contratos específicos para o cliente final.

O BFF não tem banco de dados próprio. Acessa os DbContexts e Mediator dos outros módulos via injeção de dependência — vantagem direta da arquitetura modular monolith.

---

## 3. Fluxo do Cliente

```
Acessa /{slug}
    → cardápio renderizado (SSR)
    → adiciona itens ao carrinho (Zustand, client-side)
    → abre checkout
    → preenche dados (guest ou login)
    → POST /bff/orders
    → recebe QR Code PIX + copia-e-cola
    → aguarda pagamento (SSE emite "Confirmed" após webhook Asaas)
    → redirect automático para /pedido/{id}
    → acompanha status em tempo real via SSE até "Delivered"
```

---

## 4. Módulo BFF (.NET)

### 4.1 Estrutura de arquivos

```
src/Delify.Modules.Bff/
├── BffModule.cs
├── Delify.Modules.Bff.csproj
├── Endpoints/
│   ├── MenuEndpoints.cs        # GET /bff/menu/{slug}
│   ├── OrderEndpoints.cs       # POST /bff/orders (cria pedido + inicia PIX)
│   ├── TrackingEndpoints.cs    # GET /bff/orders/{id}/track (SSE)
│   └── AuthEndpoints.cs        # guest + register + login
├── Models/
│   ├── MenuResponse.cs
│   ├── PlaceOrderRequest.cs
│   ├── PlaceOrderResponse.cs
│   └── OrderStatusEvent.cs
└── Services/
    └── OrderTrackingService.cs
```

### 4.2 Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|------|-----------|
| GET | `/bff/menu/{slug}` | anônimo | Cardápio completo (categories + products + complements) |
| POST | `/bff/orders` | guest token | Cria pedido + inicia PIX em uma chamada |
| GET | `/bff/orders/{id}/track` | guest token | SSE — stream de eventos de status |
| POST | `/bff/auth/guest` | — | Cria sessão anônima (JWT 24h) |
| POST | `/bff/auth/register` | — | Cadastro de conta |
| POST | `/bff/auth/login` | — | Login de conta |

### 4.3 SSE — OrderTrackingService

`OrderTrackingService` mantém um `Channel<OrderStatusEvent>` em memória indexado por `orderId`. É registrado como `Singleton`.

Quando o status de um pedido mudar (via `UpdateOrderStatusCommand` ou `PaymentConfirmedConsumer`), esses componentes resolvem `OrderTrackingService` via DI e escrevem no canal correspondente.

O endpoint SSE lê o canal com `await foreach` e escreve no response stream no formato `text/event-stream`:

```
event: status-changed
data: {"status":"Preparing","label":"Preparando seu pedido","at":"2026-06-10T..."}
```

A conexão se encerra automaticamente quando o status chegar em `Delivered` ou `Cancelled`.

---

## 5. Contratos de API

### 5.1 GET /bff/menu/{slug}

```json
{
  "establishmentId": "guid",
  "name": "Burger King Centro",
  "slug": "burger-king-centro",
  "categories": [
    {
      "id": "guid",
      "name": "Lanches",
      "order": 1,
      "products": [
        {
          "id": "guid",
          "name": "Whopper",
          "price": 32.90,
          "description": "...",
          "imageUrl": null,
          "complements": [
            { "id": "guid", "name": "Bacon extra", "price": 4.00 }
          ]
        }
      ]
    }
  ]
}
```

### 5.2 POST /bff/orders

**Request:**
```json
{
  "establishmentId": "guid",
  "items": [
    { "productId": "guid", "quantity": 2, "complementIds": ["guid"] }
  ],
  "customer": {
    "name": "João Silva",
    "phone": "11999999999",
    "cpf": "000.000.000-00"
  },
  "address": {
    "street": "Rua das Flores",
    "number": "123",
    "complement": "Apto 4",
    "neighborhood": "Centro",
    "city": "São Paulo"
  }
}
```

**Response:**
```json
{
  "orderId": "guid",
  "total": 69.80,
  "pix": {
    "qrCode": "data:image/png;base64,...",
    "copyPaste": "00020126...",
    "expiresAt": "2026-06-10T15:30:00Z"
  }
}
```

### 5.3 SSE GET /bff/orders/{id}/track

```
event: status-changed
data: {"status":"Pending","label":"Aguardando pagamento","at":"..."}

event: status-changed
data: {"status":"Confirmed","label":"Pagamento confirmado","at":"..."}

event: status-changed
data: {"status":"Preparing","label":"Preparando seu pedido","at":"..."}

event: status-changed
data: {"status":"OutForDelivery","label":"Saiu para entrega","at":"..."}

event: status-changed
data: {"status":"Delivered","label":"Pedido entregue!","at":"..."}
```

### 5.4 POST /bff/auth/guest

```json
// request
{ "name": "João Silva", "phone": "11999999999" }

// response
{ "token": "eyJ...", "expiresAt": "2026-06-11T..." }
```

---

## 6. Frontend Next.js PWA

### 6.1 Estrutura de pastas

```
frontend/
├── public/
│   ├── manifest.json
│   └── icons/
├── src/
│   ├── app/
│   │   ├── layout.tsx
│   │   ├── [slug]/
│   │   │   ├── page.tsx              # cardápio (SSR)
│   │   │   └── checkout/
│   │   │       └── page.tsx          # resumo + dados do cliente
│   │   ├── pedido/
│   │   │   └── [id]/
│   │   │       └── page.tsx          # acompanhamento em tempo real
│   │   └── conta/
│   │       ├── login/page.tsx
│   │       └── cadastro/page.tsx
│   ├── components/
│   │   ├── menu/
│   │   │   ├── CategoryNav.tsx       # navegação por categoria (sticky)
│   │   │   ├── ProductCard.tsx       # card com foto, nome, preço
│   │   │   └── ProductModal.tsx      # detalhes + complementos + qty
│   │   ├── cart/
│   │   │   ├── CartDrawer.tsx        # bottom sheet mobile
│   │   │   └── CartButton.tsx        # botão flutuante com contador
│   │   ├── checkout/
│   │   │   ├── GuestForm.tsx         # nome, telefone, CPF, endereço
│   │   │   └── PixPanel.tsx          # QR Code + copia-e-cola + timer
│   │   └── tracking/
│   │       ├── StatusStepper.tsx     # linha do tempo visual
│   │       └── useOrderTracking.ts   # hook SSE com EventSource
│   ├── store/
│   │   └── cart.ts                   # Zustand — itens, totais, slug
│   └── lib/
│       └── api.ts                    # fetch wrapper para o BFF
```

### 6.2 Dependências principais

| Pacote | Uso |
|--------|-----|
| `next` (App Router) | Framework + SSR |
| `@ducanh2912/next-pwa` | Service Worker + cache |
| `zustand` | Estado do carrinho (client-side) |
| `react-hook-form` + `zod` | Formulários + validação |
| `qrcode.react` | Renderiza QR Code do PIX |

### 6.3 PWA

- `manifest.json` com `display: standalone`, `theme_color` configurável por tenant
- Service Worker cacheia a rota `[slug]` e assets estáticos para uso offline
- Ícones 192×192 e 512×512 obrigatórios para instalação

### 6.4 Autenticação

O token (guest ou conta) é armazenado em `localStorage` e enviado como `Authorization: Bearer` em todas as chamadas autenticadas ao BFF. Tokens guest expiram em 24h; tokens de conta em 7 dias.

---

## 7. Organização no Monorepo

```
Delify/
├── src/
│   ├── Delify.Api/
│   ├── Delify.Modules.Bff/       ← novo
│   ├── Delify.Modules.Catalog/
│   ├── Delify.Modules.Identity/
│   ├── Delify.Modules.Orders/
│   ├── Delify.Modules.Payments/
│   ├── Delify.Modules.Delivery/
│   └── Delify.Shared/
├── frontend/                     ← novo
├── tests/
└── docs/
```

Em desenvolvimento, o Next.js roda na porta `3000` e faz proxy para `Delify.Api` na porta `5000` via `next.config.js` rewrites.

---

## 8. O que está fora do escopo deste spec

- Painel do estabelecimento (gestão de cardápio, aceite de pedidos)
- Notificações push ao cliente (próxima fase)
- Rastreamento de entregador em tempo real no mapa
- Histórico de pedidos na conta do cliente (CRUD básico, pode ser adicionado depois)
- Pagamento via cartão de crédito (apenas PIX nesta fase)
