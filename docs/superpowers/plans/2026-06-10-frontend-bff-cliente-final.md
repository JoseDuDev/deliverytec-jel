# Frontend + BFF — Cliente Final Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar o módulo `Delify.Modules.Bff` (.NET 10) e a aplicação `frontend/` (Next.js PWA) para o fluxo completo do cliente final: cardápio → carrinho → checkout PIX → acompanhamento em tempo real via SSE.

**Architecture:** O BFF é um módulo .NET que segue o padrão `IModule` já estabelecido no projeto. Ele agrega `CatalogDbContext`, `OrdersDbContext`, `PaymentsDbContext` e `IPaymentGateway` via DI interna do monolith, sem banco próprio. O frontend Next.js roda na porta 3000 e faz proxy para a API na porta 5000 via `next.config.js` rewrites.

**Tech Stack:** .NET 10 Minimal API · MassTransit · System.Threading.Channels (SSE) · Next.js 15 App Router · Zustand · react-hook-form + zod · @ducanh2912/next-pwa · qrcode.react

---

## Mapa de arquivos

### Novos (BFF)
| Arquivo | Responsabilidade |
|---------|-----------------|
| `src/Delify.Shared/Abstractions/IOrderTrackingNotifier.cs` | Interface de notificação SSE compartilhada entre módulos |
| `src/Delify.Modules.Bff/Delify.Modules.Bff.csproj` | Projeto do módulo BFF |
| `src/Delify.Modules.Bff/BffModule.cs` | Registro de serviços e mapeamento de endpoints |
| `src/Delify.Modules.Bff/Services/OrderTrackingService.cs` | Implementação SSE: mantém `Channel<OrderStatusEvent>` por orderId |
| `src/Delify.Modules.Bff/Models/MenuResponse.cs` | DTOs do cardápio |
| `src/Delify.Modules.Bff/Models/PlaceOrderRequest.cs` | DTO de entrada para criação de pedido |
| `src/Delify.Modules.Bff/Models/PlaceOrderResponse.cs` | DTO de saída com orderId + dados PIX |
| `src/Delify.Modules.Bff/Models/OrderStatusEvent.cs` | Payload dos eventos SSE |
| `src/Delify.Modules.Bff/Endpoints/MenuEndpoints.cs` | `GET /bff/menu/{slug}` |
| `src/Delify.Modules.Bff/Endpoints/AuthEndpoints.cs` | `POST /bff/auth/guest`, `/register`, `/login` |
| `src/Delify.Modules.Bff/Endpoints/OrderEndpoints.cs` | `POST /bff/orders` |
| `src/Delify.Modules.Bff/Endpoints/TrackingEndpoints.cs` | `GET /bff/orders/{id}/track` (SSE) |

### Modificados (BFF)
| Arquivo | O que muda |
|---------|-----------|
| `src/Delify.Modules.Orders/Application/Commands/UpdateOrderStatusCommand.cs` | Injeta `IOrderTrackingNotifier`, notifica após salvar |
| `src/Delify.Modules.Orders/Application/Consumers/PaymentConfirmedConsumer.cs` | Injeta `IOrderTrackingNotifier`, notifica após confirmar |
| `src/Delify.Api/Program.cs` | Registra `NullOrderTrackingNotifier` padrão + adiciona `BffModule` + CORS |
| `Delify.sln` | Adiciona referência ao projeto BFF |

### Novos (Frontend)
| Arquivo | Responsabilidade |
|---------|-----------------|
| `frontend/package.json` | Dependências Next.js + PWA |
| `frontend/next.config.js` | Proxy rewrites para API + PWA config |
| `frontend/public/manifest.json` | PWA manifest |
| `frontend/src/lib/api.ts` | Fetch wrapper para o BFF (todas as chamadas HTTP) |
| `frontend/src/store/cart.ts` | Zustand — itens do carrinho, totais, slug do estabelecimento |
| `frontend/src/app/layout.tsx` | Root layout com providers |
| `frontend/src/app/[slug]/page.tsx` | Página do cardápio (SSR) |
| `frontend/src/app/[slug]/checkout/page.tsx` | Página de checkout |
| `frontend/src/app/pedido/[id]/page.tsx` | Página de acompanhamento |
| `frontend/src/app/conta/login/page.tsx` | Login |
| `frontend/src/app/conta/cadastro/page.tsx` | Cadastro |
| `frontend/src/components/menu/CategoryNav.tsx` | Navegação por categoria (sticky) |
| `frontend/src/components/menu/ProductCard.tsx` | Card de produto |
| `frontend/src/components/menu/ProductModal.tsx` | Modal de produto com complementos |
| `frontend/src/components/cart/CartDrawer.tsx` | Bottom sheet com resumo do carrinho |
| `frontend/src/components/cart/CartButton.tsx` | Botão flutuante com contador |
| `frontend/src/components/checkout/GuestForm.tsx` | Formulário de dados do cliente |
| `frontend/src/components/checkout/PixPanel.tsx` | QR Code + copia-e-cola + timer |
| `frontend/src/components/tracking/useOrderTracking.ts` | Hook SSE com EventSource |
| `frontend/src/components/tracking/StatusStepper.tsx` | Linha do tempo visual |

---

## FASE 1 — Módulo BFF (.NET)

---

### Task 1: Adicionar IOrderTrackingNotifier ao Delify.Shared

**Files:**
- Create: `src/Delify.Shared/Abstractions/IOrderTrackingNotifier.cs`

- [ ] **Step 1: Criar a interface no projeto Shared**

```csharp
// src/Delify.Shared/Abstractions/IOrderTrackingNotifier.cs
namespace Delify.Shared.Abstractions;

public interface IOrderTrackingNotifier
{
    void Notify(Guid orderId, string status, string label);
}

public sealed class NullOrderTrackingNotifier : IOrderTrackingNotifier
{
    public void Notify(Guid orderId, string status, string label) { }
}
```

- [ ] **Step 2: Verificar que o projeto compila**

```powershell
dotnet build src/Delify.Shared/Delify.Shared.csproj
```

Esperado: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Delify.Shared/Abstractions/IOrderTrackingNotifier.cs
git commit -m "feat(shared): add IOrderTrackingNotifier interface for SSE tracking"
```

---

### Task 2: Injetar IOrderTrackingNotifier nos consumers e command handler

**Files:**
- Modify: `src/Delify.Modules.Orders/Application/Consumers/PaymentConfirmedConsumer.cs`
- Modify: `src/Delify.Modules.Orders/Application/Commands/UpdateOrderStatusCommand.cs`

- [ ] **Step 1: Modificar PaymentConfirmedConsumer**

Substitua o conteúdo completo de `src/Delify.Modules.Orders/Application/Consumers/PaymentConfirmedConsumer.cs`:

```csharp
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.IntegrationEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Application.Consumers;

public sealed class PaymentConfirmedConsumer(
    OrdersDbContext db,
    IOrderTrackingNotifier trackingNotifier) : IConsumer<PaymentConfirmedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<PaymentConfirmedIntegrationEvent> context)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == context.Message.OrderId, context.CancellationToken);

        if (order is null) return;

        try { order.Confirm(); }
        catch (InvalidOperationException) { return; }

        await db.SaveChangesAsync(context.CancellationToken);

        trackingNotifier.Notify(order.Id, "Confirmed", "Pagamento confirmado");
    }
}
```

- [ ] **Step 2: Modificar UpdateOrderStatusCommandHandler**

Substitua o conteúdo completo de `src/Delify.Modules.Orders/Application/Commands/UpdateOrderStatusCommand.cs`:

```csharp
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Application.Commands;

public enum OrderAction { Accept, StartDelivery, Complete, Cancel }

public record UpdateOrderStatusCommand(Guid OrderId, OrderAction Action) : IRequest<Result>;

internal sealed class UpdateOrderStatusCommandHandler(
    OrdersDbContext db,
    IOrderTrackingNotifier trackingNotifier)
    : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
            return Result.Failure(Error.NotFound("Order"));

        try
        {
            switch (request.Action)
            {
                case OrderAction.Accept: order.Accept(); break;
                case OrderAction.StartDelivery: order.StartDelivery(); break;
                case OrderAction.Complete: order.Complete(); break;
                case OrderAction.Cancel: order.Cancel(); break;
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        await db.SaveChangesAsync(cancellationToken);

        var (statusKey, label) = order.Status switch
        {
            OrderStatus.InPreparation => ("Preparing", "Preparando seu pedido"),
            OrderStatus.InDelivery    => ("OutForDelivery", "Saiu para entrega"),
            OrderStatus.Delivered     => ("Delivered", "Pedido entregue!"),
            OrderStatus.Cancelled     => ("Cancelled", "Pedido cancelado"),
            _                         => (order.Status.ToString(), order.Status.ToString())
        };

        trackingNotifier.Notify(order.Id, statusKey, label);

        return Result.Success();
    }
}
```

- [ ] **Step 3: Registrar NullOrderTrackingNotifier como padrão em Program.cs**

Em `src/Delify.Api/Program.cs`, adicione logo antes do `foreach (var module in modules)`:

```csharp
// Registra implementação nula por padrão; BffModule sobrescreve com a real
builder.Services.AddSingleton<IOrderTrackingNotifier, NullOrderTrackingNotifier>();
```

E adicione o using necessário no topo:

```csharp
using Delify.Shared.Abstractions;
```

- [ ] **Step 4: Compilar a solution**

```powershell
dotnet build Delify.sln
```

Esperado: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add src/Delify.Modules.Orders/Application/Consumers/PaymentConfirmedConsumer.cs
git add src/Delify.Modules.Orders/Application/Commands/UpdateOrderStatusCommand.cs
git add src/Delify.Api/Program.cs
git commit -m "feat(orders): wire IOrderTrackingNotifier into status updates and payment confirmation"
```

---

### Task 3: Criar o projeto Delify.Modules.Bff

**Files:**
- Create: `src/Delify.Modules.Bff/Delify.Modules.Bff.csproj`

- [ ] **Step 1: Criar o projeto**

```powershell
dotnet new classlib -n Delify.Modules.Bff -o src/Delify.Modules.Bff --framework net10.0
Remove-Item src/Delify.Modules.Bff/Class1.cs
```

- [ ] **Step 2: Editar o .csproj com as dependências corretas**

Substitua o conteúdo de `src/Delify.Modules.Bff/Delify.Modules.Bff.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.7.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Delify.Shared\Delify.Shared.csproj" />
    <ProjectReference Include="..\Delify.Modules.Catalog\Delify.Modules.Catalog.csproj" />
    <ProjectReference Include="..\Delify.Modules.Orders\Delify.Modules.Orders.csproj" />
    <ProjectReference Include="..\Delify.Modules.Payments\Delify.Modules.Payments.csproj" />
    <ProjectReference Include="..\Delify.Modules.Identity\Delify.Modules.Identity.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Adicionar à solution**

```powershell
dotnet sln Delify.sln add src/Delify.Modules.Bff/Delify.Modules.Bff.csproj
```

- [ ] **Step 4: Adicionar referência ao BFF na API**

```powershell
dotnet add src/Delify.Api/Delify.Api.csproj reference src/Delify.Modules.Bff/Delify.Modules.Bff.csproj
```

- [ ] **Step 5: Build para confirmar que referências resolvem**

```powershell
dotnet build Delify.sln
```

Esperado: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add src/Delify.Modules.Bff/Delify.Modules.Bff.csproj src/Delify.Api/Delify.Api.csproj Delify.sln
git commit -m "feat(bff): scaffold Delify.Modules.Bff project with project references"
```

---

### Task 4: Models (DTOs do BFF)

**Files:**
- Create: `src/Delify.Modules.Bff/Models/MenuResponse.cs`
- Create: `src/Delify.Modules.Bff/Models/PlaceOrderRequest.cs`
- Create: `src/Delify.Modules.Bff/Models/PlaceOrderResponse.cs`
- Create: `src/Delify.Modules.Bff/Models/OrderStatusEvent.cs`

- [ ] **Step 1: Criar os DTOs**

```csharp
// src/Delify.Modules.Bff/Models/MenuResponse.cs
namespace Delify.Modules.Bff.Models;

public record MenuResponse(
    Guid EstablishmentId,
    string Name,
    string Slug,
    IReadOnlyList<MenuCategoryDto> Categories);

public record MenuCategoryDto(
    Guid Id,
    string Name,
    int Order,
    IReadOnlyList<MenuProductDto> Products);

public record MenuProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    string? ImageUrl,
    IReadOnlyList<MenuComplementDto> Complements);

public record MenuComplementDto(Guid Id, string Name, decimal Price);
```

```csharp
// src/Delify.Modules.Bff/Models/PlaceOrderRequest.cs
namespace Delify.Modules.Bff.Models;

public record PlaceOrderRequest(
    Guid EstablishmentId,
    List<OrderItemRequest> Items,
    CustomerRequest Customer,
    AddressRequest Address,
    string? Note = null);

public record OrderItemRequest(Guid ProductId, int Quantity, List<Guid>? ComplementIds = null);

public record CustomerRequest(string Name, string Phone, string Cpf);

public record AddressRequest(
    string Street,
    string Number,
    string Neighborhood,
    string City,
    string? Complement = null);
```

```csharp
// src/Delify.Modules.Bff/Models/PlaceOrderResponse.cs
namespace Delify.Modules.Bff.Models;

public record PlaceOrderResponse(Guid OrderId, decimal Total, PixResponse Pix);

public record PixResponse(string QrCode, string CopyPaste, DateTimeOffset ExpiresAt);
```

```csharp
// src/Delify.Modules.Bff/Models/OrderStatusEvent.cs
namespace Delify.Modules.Bff.Models;

public record OrderStatusEvent(Guid OrderId, string Status, string Label, DateTimeOffset At);
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/Delify.Modules.Bff/Delify.Modules.Bff.csproj
```

Esperado: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Delify.Modules.Bff/Models/
git commit -m "feat(bff): add BFF DTO models (menu, order, PIX, SSE event)"
```

---

### Task 5: OrderTrackingService (SSE)

**Files:**
- Create: `src/Delify.Modules.Bff/Services/OrderTrackingService.cs`
- Create: `tests/Delify.Modules.Bff.Tests/Services/OrderTrackingServiceTests.cs`

- [ ] **Step 1: Criar o projeto de testes**

```powershell
dotnet new xunit -n Delify.Modules.Bff.Tests -o tests/Delify.Modules.Bff.Tests --framework net10.0
dotnet sln Delify.sln add tests/Delify.Modules.Bff.Tests/Delify.Modules.Bff.Tests.csproj
dotnet add tests/Delify.Modules.Bff.Tests/Delify.Modules.Bff.Tests.csproj reference src/Delify.Modules.Bff/Delify.Modules.Bff.csproj
```

- [ ] **Step 2: Escrever os testes primeiro**

```csharp
// tests/Delify.Modules.Bff.Tests/Services/OrderTrackingServiceTests.cs
using Delify.Modules.Bff.Services;
using Delify.Modules.Bff.Models;

namespace Delify.Modules.Bff.Tests.Services;

public class OrderTrackingServiceTests
{
    [Fact]
    public async Task Subscribe_ThenNotify_DeliversEventToReader()
    {
        var svc = new OrderTrackingService();
        var orderId = Guid.NewGuid();

        var reader = svc.Subscribe(orderId);
        svc.Notify(orderId, "Confirmed", "Pagamento confirmado");

        var evt = await reader.ReadAsync();

        Assert.Equal(orderId, evt.OrderId);
        Assert.Equal("Confirmed", evt.Status);
        Assert.Equal("Pagamento confirmado", evt.Label);
    }

    [Fact]
    public void Notify_WhenNoSubscriber_DoesNotThrow()
    {
        var svc = new OrderTrackingService();
        var ex = Record.Exception(() => svc.Notify(Guid.NewGuid(), "Confirmed", "label"));
        Assert.Null(ex);
    }

    [Fact]
    public async Task Notify_WhenDelivered_CompletesChannel()
    {
        var svc = new OrderTrackingService();
        var orderId = Guid.NewGuid();

        var reader = svc.Subscribe(orderId);
        svc.Notify(orderId, "Delivered", "Pedido entregue!");

        await reader.ReadAsync(); // consome o evento
        Assert.True(reader.Completion.IsCompleted);
    }
}
```

- [ ] **Step 3: Rodar e confirmar falha**

```powershell
dotnet test tests/Delify.Modules.Bff.Tests/Delify.Modules.Bff.Tests.csproj
```

Esperado: erro de compilação — `OrderTrackingService` ainda não existe.

- [ ] **Step 4: Implementar o serviço**

```csharp
// src/Delify.Modules.Bff/Services/OrderTrackingService.cs
using System.Collections.Concurrent;
using System.Threading.Channels;
using Delify.Modules.Bff.Models;
using Delify.Shared.Abstractions;

namespace Delify.Modules.Bff.Services;

public sealed class OrderTrackingService : IOrderTrackingNotifier
{
    private readonly ConcurrentDictionary<Guid, Channel<OrderStatusEvent>> _channels = new();

    public ChannelReader<OrderStatusEvent> Subscribe(Guid orderId)
    {
        var channel = _channels.GetOrAdd(orderId, _ => Channel.CreateUnbounded<OrderStatusEvent>());
        return channel.Reader;
    }

    public void Notify(Guid orderId, string status, string label)
    {
        if (!_channels.TryGetValue(orderId, out var channel)) return;

        channel.Writer.TryWrite(new OrderStatusEvent(orderId, status, label, DateTimeOffset.UtcNow));

        if (status is "Delivered" or "Cancelled")
        {
            _channels.TryRemove(orderId, out _);
            channel.Writer.TryComplete();
        }
    }
}
```

- [ ] **Step 5: Rodar os testes e confirmar que passam**

```powershell
dotnet test tests/Delify.Modules.Bff.Tests/Delify.Modules.Bff.Tests.csproj
```

Esperado: `3 passed, 0 failed`

- [ ] **Step 6: Commit**

```bash
git add src/Delify.Modules.Bff/Services/ tests/Delify.Modules.Bff.Tests/
git commit -m "feat(bff): implement OrderTrackingService with Channel-based SSE notifications"
```

---

### Task 6: MenuEndpoints (GET /bff/menu/{slug})

**Files:**
- Create: `src/Delify.Modules.Bff/Endpoints/MenuEndpoints.cs`

- [ ] **Step 1: Implementar o endpoint**

```csharp
// src/Delify.Modules.Bff/Endpoints/MenuEndpoints.cs
using Delify.Modules.Bff.Models;
using Delify.Modules.Catalog.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Bff.Endpoints;

internal static class MenuEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/menu/{slug}", async (string slug, CatalogDbContext db) =>
        {
            var establishment = await db.Establishments
                .Include(e => e.Categories.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Products)
                        .ThenInclude(p => p.Complements)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Slug == slug);

            if (establishment is null)
                return Results.NotFound();

            var response = new MenuResponse(
                establishment.Id,
                establishment.Name,
                establishment.Slug,
                establishment.Categories
                    .Select(c => new MenuCategoryDto(
                        c.Id,
                        c.Name,
                        c.Order,
                        c.Products
                            .Select(p => new MenuProductDto(
                                p.Id,
                                p.Name,
                                p.Price,
                                p.Description,
                                null,
                                p.Complements
                                    .Select(cp => new MenuComplementDto(cp.Id, cp.Name, cp.Price))
                                    .ToList()))
                            .ToList()))
                    .ToList());

            return Results.Ok(response);
        })
        .WithName("BffGetMenu")
        .WithTags("BFF")
        .AllowAnonymous();

        return app;
    }
}
```

- [ ] **Step 2: Verificar que o Catalog.Domain expõe as propriedades necessárias**

Verificar que `Category.Order`, `Product.Description`, `Complement.Price` existem:

```powershell
dotnet build src/Delify.Modules.Bff/Delify.Modules.Bff.csproj
```

Se `Category` não tiver `Order` ou `Product` não tiver `Description`, verificar as entidades em `src/Delify.Modules.Catalog/Domain/` e ajustar o endpoint para usar apenas as propriedades existentes.

Esperado: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Delify.Modules.Bff/Endpoints/MenuEndpoints.cs
git commit -m "feat(bff): add GET /bff/menu/{slug} endpoint"
```

---

### Task 7: AuthEndpoints (guest, register, login)

**Files:**
- Create: `src/Delify.Modules.Bff/Endpoints/AuthEndpoints.cs`

- [ ] **Step 1: Implementar os endpoints de autenticação**

```csharp
// src/Delify.Modules.Bff/Endpoints/AuthEndpoints.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Delify.Modules.Identity.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Delify.Modules.Bff.Endpoints;

internal static class AuthEndpoints
{
    private record GuestRequest(string Name, string Phone);
    private record RegisterRequest(string Name, string Email, string Password, string Phone);
    private record LoginRequest(string Email, string Password);
    private record TokenResponse(string Token, DateTimeOffset ExpiresAt);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bff/auth").WithTags("BFF").AllowAnonymous();

        group.MapPost("/guest", (GuestRequest req, IConfiguration config) =>
        {
            var expires = DateTimeOffset.UtcNow.AddHours(24);
            var token = BuildToken(config, expires,
                new Claim("role", "guest"),
                new Claim("name", req.Name),
                new Claim("phone", req.Phone),
                new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()));

            return Results.Ok(new TokenResponse(token, expires));
        })
        .WithName("BffGuestSession");

        group.MapPost("/register", async (
            RegisterRequest req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            var user = new AppUser
            {
                UserName = req.Email,
                Email = req.Email,
                FullName = req.Name,
                TenantId = Guid.Empty
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors.Select(e => e.Description));

            var expires = DateTimeOffset.UtcNow.AddDays(7);
            var token = BuildToken(config, expires,
                new Claim("role", "customer"),
                new Claim("name", req.Name),
                new Claim("phone", req.Phone),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!));

            return Results.Ok(new TokenResponse(token, expires));
        })
        .WithName("BffRegister");

        group.MapPost("/login", async (
            LoginRequest req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || user.TenantId != Guid.Empty)
                return Results.Unauthorized();

            if (!await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            var expires = DateTimeOffset.UtcNow.AddDays(7);
            var token = BuildToken(config, expires,
                new Claim("role", "customer"),
                new Claim("name", user.FullName),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!));

            return Results.Ok(new TokenResponse(token, expires));
        })
        .WithName("BffLogin");

        return app;
    }

    private static string BuildToken(IConfiguration config, DateTimeOffset expires, params Claim[] claims)
    {
        var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/Delify.Modules.Bff/Delify.Modules.Bff.csproj
```

Esperado: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Delify.Modules.Bff/Endpoints/AuthEndpoints.cs
git commit -m "feat(bff): add guest/register/login auth endpoints"
```

---

### Task 8: OrderEndpoints (POST /bff/orders)

**Files:**
- Create: `src/Delify.Modules.Bff/Endpoints/OrderEndpoints.cs`

- [ ] **Step 1: Verificar quais propriedades existem no domínio Catalog**

```powershell
# Conferir estrutura das entidades do Catalog
Get-Content src/Delify.Modules.Catalog/Domain/Product.cs
Get-Content src/Delify.Modules.Catalog/Domain/Complement.cs
Get-Content src/Delify.Modules.Catalog/Domain/Establishment.cs
```

Anote os nomes exatos das propriedades — especialmente `Product.Price`, `Product.Name`, `Establishment.TenantId`.

- [ ] **Step 2: Implementar o endpoint**

```csharp
// src/Delify.Modules.Bff/Endpoints/OrderEndpoints.cs
using Delify.Modules.Bff.Models;
using Delify.Modules.Bff.Services;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.IntegrationEvents;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Bff.Endpoints;

internal static class OrderEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/bff/orders", async (
            PlaceOrderRequest req,
            CatalogDbContext catalogDb,
            OrdersDbContext ordersDb,
            PaymentsDbContext paymentsDb,
            IPaymentGateway gateway,
            IOrderTrackingNotifier trackingNotifier,
            IBus bus) =>
        {
            // 1. Resolve o estabelecimento e seu TenantId
            var establishment = await catalogDb.Establishments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == req.EstablishmentId);

            if (establishment is null)
                return Results.NotFound(new { error = "Establishment not found." });

            // 2. Resolve os produtos do catálogo
            var productIds = req.Items.Select(i => i.ProductId).ToList();
            var products = await catalogDb.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            if (products.Count != productIds.Count)
                return Results.BadRequest(new { error = "One or more products not found." });

            // 3. Cria o pedido no domínio
            var order = Order.Create(establishment.TenantId, req.EstablishmentId);
            order.CustomerNote = req.Note;

            foreach (var item in req.Items)
            {
                var product = products[item.ProductId];
                order.AddItem(product.Id, product.Name, item.Quantity, product.Price);
            }

            ordersDb.Orders.Add(order);
            await ordersDb.SaveChangesAsync();

            // 4. Publica o evento de integração (backup assíncrono)
            await bus.Publish(new OrderCreatedIntegrationEvent(
                order.Id, order.TenantId, order.Total,
                req.Customer.Cpf, req.Customer.Name));

            // 5. Cria o PIX diretamente (caminho síncrono para o cliente)
            var pixResult = await gateway.CreatePixAsync(
                new PixPaymentRequest(order.Id, order.Total, req.Customer.Cpf, req.Customer.Name));

            var payment = Payment.CreatePix(establishment.TenantId, order.Id, order.Total);
            payment.SetPixData(pixResult.GatewayId, pixResult.QrCode, pixResult.CopyPaste);
            paymentsDb.Payments.Add(payment);
            await paymentsDb.SaveChangesAsync();

            // 6. Notifica status inicial
            trackingNotifier.Notify(order.Id, "Pending", "Aguardando pagamento");

            return Results.Ok(new PlaceOrderResponse(
                order.Id,
                order.Total,
                new PixResponse(pixResult.QrCode, pixResult.CopyPaste, pixResult.ExpiresAt)));
        })
        .WithName("BffPlaceOrder")
        .WithTags("BFF")
        .RequireAuthorization();

        return app;
    }
}
```

- [ ] **Step 3: Build**

```powershell
dotnet build src/Delify.Modules.Bff/Delify.Modules.Bff.csproj
```

Se `Payment.CreatePix` ou `payment.SetPixData` causarem erros de acesso (métodos internos), verifique os modificadores em `src/Delify.Modules.Payments/Domain/Payment.cs` e ajuste para `public` se necessário.

Esperado: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/Delify.Modules.Bff/Endpoints/OrderEndpoints.cs
git commit -m "feat(bff): add POST /bff/orders endpoint with synchronous PIX creation"
```

---

### Task 9: TrackingEndpoints (GET /bff/orders/{id}/track — SSE)

**Files:**
- Create: `src/Delify.Modules.Bff/Endpoints/TrackingEndpoints.cs`

- [ ] **Step 1: Implementar o endpoint SSE**

```csharp
// src/Delify.Modules.Bff/Endpoints/TrackingEndpoints.cs
using System.Text.Json;
using Delify.Modules.Bff.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Bff.Endpoints;

internal static class TrackingEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/orders/{orderId:guid}/track", async (
            Guid orderId,
            OrderTrackingService tracker,
            HttpResponse response,
            CancellationToken ct) =>
        {
            response.ContentType = "text/event-stream";
            response.Headers.CacheControl = "no-cache";
            response.Headers.Connection = "keep-alive";

            var reader = tracker.Subscribe(orderId);

            await foreach (var evt in reader.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await response.WriteAsync($"event: status-changed\ndata: {json}\n\n", ct);
                await response.Body.FlushAsync(ct);

                if (evt.Status is "Delivered" or "Cancelled")
                    break;
            }
        })
        .WithName("BffTrackOrder")
        .WithTags("BFF")
        .AllowAnonymous();

        return app;
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build src/Delify.Modules.Bff/Delify.Modules.Bff.csproj
```

Esperado: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add src/Delify.Modules.Bff/Endpoints/TrackingEndpoints.cs
git commit -m "feat(bff): add GET /bff/orders/{id}/track SSE endpoint"
```

---

### Task 10: BffModule — registro e wire-up

**Files:**
- Create: `src/Delify.Modules.Bff/BffModule.cs`
- Modify: `src/Delify.Api/Program.cs`

- [ ] **Step 1: Criar o BffModule**

```csharp
// src/Delify.Modules.Bff/BffModule.cs
using Delify.Modules.Bff.Endpoints;
using Delify.Modules.Bff.Services;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Delify.Modules.Bff;

public sealed class BffModule : IModule
{
    public string Name => "Bff";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Substitui o NullOrderTrackingNotifier registrado em Program.cs
        services.Replace(ServiceDescriptor.Singleton<IOrderTrackingNotifier, OrderTrackingService>());
        // Registra também como OrderTrackingService para o TrackingEndpoints poder injetá-lo diretamente
        services.AddSingleton(sp => (OrderTrackingService)sp.GetRequiredService<IOrderTrackingNotifier>());

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        MenuEndpoints.Map(endpoints);
        AuthEndpoints.Map(endpoints);
        OrderEndpoints.Map(endpoints);
        TrackingEndpoints.Map(endpoints);
        return endpoints;
    }
}
```

- [ ] **Step 2: Adicionar BffModule e CORS ao Program.cs**

Abra `src/Delify.Api/Program.cs`. O arquivo completo deve ficar assim:

```csharp
using Delify.Modules.Bff;
using Delify.Modules.Catalog;
using Delify.Modules.Delivery;
using Delify.Modules.Identity;
using Delify.Modules.Orders;
using Delify.Modules.Payments;
using Delify.Shared.Abstractions;
using MassTransit;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var modules = new List<IModule>
{
    new IdentityModule(),
    new CatalogModule(),
    new OrdersModule(),
    new PaymentsModule(),
    new DeliveryModule(),
    new BffModule()          // BFF por último para override do IOrderTrackingNotifier
};

// Registra implementação nula por padrão; BffModule sobrescreve com a real
builder.Services.AddSingleton<IOrderTrackingNotifier, NullOrderTrackingNotifier>();

foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumers(typeof(OrdersModule).Assembly);
    x.AddConsumers(typeof(PaymentsModule).Assembly);

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var vhost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
        var port = builder.Configuration.GetValue<ushort>("RabbitMQ:Port", 5672);

        cfg.Host(host, port, vhost, h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "delify");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "delify");
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddOpenApi();
builder.Services.AddOutputCache();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

foreach (var module in modules)
    module.MapEndpoints(app);

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithTags("Health")
   .AllowAnonymous();

app.Run();

public partial class Program { }
```

- [ ] **Step 3: Build completo da solution**

```powershell
dotnet build Delify.sln
```

Esperado: `Build succeeded.`

- [ ] **Step 4: Rodar todos os testes**

```powershell
dotnet test Delify.sln
```

Esperado: todos os testes passando.

- [ ] **Step 5: Commit**

```bash
git add src/Delify.Modules.Bff/BffModule.cs src/Delify.Api/Program.cs
git commit -m "feat(bff): wire up BffModule in API with CORS and SSE tracking override"
```

---

## FASE 2 — Frontend Next.js PWA

---

### Task 11: Scaffold do projeto Next.js + configuração PWA

**Files:**
- Create: `frontend/` (scaffolded via CLI)
- Create: `frontend/next.config.js`
- Create: `frontend/public/manifest.json`

- [ ] **Step 1: Criar o projeto Next.js**

Execute na raiz do monorepo (`C:\Projetos\JEL\JEL\Delify`):

```powershell
npx create-next-app@latest frontend `
  --typescript `
  --tailwind `
  --eslint `
  --app `
  --src-dir `
  --no-import-alias
```

Responda `No` para "Would you like to use Turbopack?".

- [ ] **Step 2: Instalar dependências**

```powershell
cd frontend
npm install @ducanh2912/next-pwa zustand react-hook-form @hookform/resolvers zod qrcode.react
npm install --save-dev @types/qrcode.react
```

- [ ] **Step 3: Configurar next.config.js**

Substitua o conteúdo de `frontend/next.config.js`:

```js
const withPWA = require('@ducanh2912/next-pwa').default({
  dest: 'public',
  disable: process.env.NODE_ENV === 'development',
});

/** @type {import('next').NextConfig} */
const nextConfig = {
  async rewrites() {
    return [
      {
        source: '/bff/:path*',
        destination: 'http://localhost:5000/bff/:path*',
      },
    ];
  },
};

module.exports = withPWA(nextConfig);
```

- [ ] **Step 4: Criar o manifest.json**

```json
// frontend/public/manifest.json
{
  "name": "Delify",
  "short_name": "Delify",
  "description": "Peça sua comida favorita",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#f97316",
  "icons": [
    { "src": "/icons/icon-192.png", "sizes": "192x192", "type": "image/png" },
    { "src": "/icons/icon-512.png", "sizes": "512x512", "type": "image/png" }
  ]
}
```

- [ ] **Step 5: Criar ícones placeholder**

Crie a pasta `frontend/public/icons/`. Por ora, os ícones podem ser arquivos PNG de cor sólida gerados com qualquer ferramenta. Coloque `icon-192.png` e `icon-512.png` nessa pasta. O app não instalará como PWA sem eles.

- [ ] **Step 6: Verificar que o app inicia**

```powershell
npm run dev
```

Abra `http://localhost:3000` no browser. Esperado: página padrão Next.js sem erros de console.

`Ctrl+C` para parar.

- [ ] **Step 7: Commit**

```bash
cd ..
git add frontend/
git commit -m "feat(frontend): scaffold Next.js PWA with proxy rewrites to BFF"
```

---

### Task 12: API client e Zustand cart store

**Files:**
- Create: `frontend/src/lib/api.ts`
- Create: `frontend/src/store/cart.ts`

- [ ] **Step 1: Criar o API client**

```typescript
// frontend/src/lib/api.ts

export type MenuResponse = {
  establishmentId: string;
  name: string;
  slug: string;
  categories: {
    id: string;
    name: string;
    order: number;
    products: {
      id: string;
      name: string;
      price: number;
      description: string | null;
      imageUrl: string | null;
      complements: { id: string; name: string; price: number }[];
    }[];
  }[];
};

export type PlaceOrderResponse = {
  orderId: string;
  total: number;
  pix: { qrCode: string; copyPaste: string; expiresAt: string };
};

export type OrderStatusEvent = {
  orderId: string;
  status: string;
  label: string;
  at: string;
};

export type TokenResponse = { token: string; expiresAt: string };

function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('delify_token');
}

function authHeaders(): HeadersInit {
  const token = getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function fetchMenu(slug: string): Promise<MenuResponse> {
  const res = await fetch(`/bff/menu/${slug}`);
  if (!res.ok) throw new Error('Cardápio não encontrado');
  return res.json();
}

export async function guestSession(name: string, phone: string): Promise<TokenResponse> {
  const res = await fetch('/bff/auth/guest', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, phone }),
  });
  if (!res.ok) throw new Error('Erro ao criar sessão');
  return res.json();
}

export async function register(
  name: string, email: string, password: string, phone: string
): Promise<TokenResponse> {
  const res = await fetch('/bff/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, email, password, phone }),
  });
  if (!res.ok) {
    const err = await res.json();
    throw new Error(Array.isArray(err) ? err.join(', ') : 'Erro no cadastro');
  }
  return res.json();
}

export async function login(email: string, password: string): Promise<TokenResponse> {
  const res = await fetch('/bff/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new Error('Email ou senha inválidos');
  return res.json();
}

export async function placeOrder(body: {
  establishmentId: string;
  items: { productId: string; quantity: number; complementIds: string[] }[];
  customer: { name: string; phone: string; cpf: string };
  address: { street: string; number: string; neighborhood: string; city: string; complement?: string };
  note?: string;
}): Promise<PlaceOrderResponse> {
  const res = await fetch('/bff/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error('Erro ao criar pedido');
  return res.json();
}

export function saveToken(token: string): void {
  localStorage.setItem('delify_token', token);
}
```

- [ ] **Step 2: Criar o cart store com Zustand**

```typescript
// frontend/src/store/cart.ts
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export type CartItem = {
  productId: string;
  name: string;
  price: number;
  quantity: number;
  complementIds: string[];
  complementsTotal: number;
};

type CartState = {
  establishmentId: string | null;
  slug: string | null;
  items: CartItem[];
  addItem: (item: CartItem, establishmentId: string, slug: string) => void;
  removeItem: (productId: string) => void;
  updateQuantity: (productId: string, quantity: number) => void;
  clear: () => void;
  total: () => number;
};

export const useCart = create<CartState>()(
  persist(
    (set, get) => ({
      establishmentId: null,
      slug: null,
      items: [],

      addItem: (item, establishmentId, slug) => {
        const state = get();
        // Limpa o carrinho se mudar de estabelecimento
        if (state.establishmentId && state.establishmentId !== establishmentId) {
          set({ items: [item], establishmentId, slug });
          return;
        }
        const existing = state.items.find((i) => i.productId === item.productId);
        if (existing) {
          set({
            items: state.items.map((i) =>
              i.productId === item.productId
                ? { ...i, quantity: i.quantity + item.quantity }
                : i,
            ),
          });
        } else {
          set({ items: [...state.items, item], establishmentId, slug });
        }
      },

      removeItem: (productId) =>
        set((s) => ({ items: s.items.filter((i) => i.productId !== productId) })),

      updateQuantity: (productId, quantity) =>
        set((s) => ({
          items:
            quantity <= 0
              ? s.items.filter((i) => i.productId !== productId)
              : s.items.map((i) =>
                  i.productId === productId ? { ...i, quantity } : i,
                ),
        })),

      clear: () => set({ items: [], establishmentId: null, slug: null }),

      total: () =>
        get().items.reduce(
          (sum, i) => sum + (i.price + i.complementsTotal) * i.quantity,
          0,
        ),
    }),
    { name: 'delify-cart' },
  ),
);
```

- [ ] **Step 3: Build para verificar tipos**

```powershell
cd frontend
npm run build 2>&1 | Select-String -Pattern "error"
```

Esperado: nenhuma linha com `error TS`.

- [ ] **Step 4: Commit**

```bash
cd ..
git add frontend/src/lib/api.ts frontend/src/store/cart.ts
git commit -m "feat(frontend): add BFF API client and Zustand cart store"
```

---

### Task 13: Componentes do cardápio + página [slug]

**Files:**
- Create: `frontend/src/components/menu/ProductCard.tsx`
- Create: `frontend/src/components/menu/ProductModal.tsx`
- Create: `frontend/src/components/menu/CategoryNav.tsx`
- Create: `frontend/src/app/[slug]/page.tsx`

- [ ] **Step 1: ProductCard**

```tsx
// frontend/src/components/menu/ProductCard.tsx
'use client';
import { useState } from 'react';
import { MenuResponse } from '@/lib/api';
import ProductModal from './ProductModal';

type Product = MenuResponse['categories'][0]['products'][0];

export default function ProductCard({
  product,
  establishmentId,
  slug,
}: {
  product: Product;
  establishmentId: string;
  slug: string;
}) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="flex w-full items-start gap-3 rounded-xl border border-gray-100 bg-white p-4 text-left shadow-sm hover:shadow-md transition-shadow"
      >
        <div className="flex-1">
          <p className="font-semibold text-gray-900">{product.name}</p>
          {product.description && (
            <p className="mt-1 text-sm text-gray-500 line-clamp-2">{product.description}</p>
          )}
          <p className="mt-2 font-bold text-orange-500">
            R$ {product.price.toFixed(2).replace('.', ',')}
          </p>
        </div>
        {product.imageUrl && (
          <img
            src={product.imageUrl}
            alt={product.name}
            className="h-20 w-20 rounded-lg object-cover"
          />
        )}
      </button>

      {open && (
        <ProductModal
          product={product}
          establishmentId={establishmentId}
          slug={slug}
          onClose={() => setOpen(false)}
        />
      )}
    </>
  );
}
```

- [ ] **Step 2: ProductModal**

```tsx
// frontend/src/components/menu/ProductModal.tsx
'use client';
import { useState } from 'react';
import { MenuResponse } from '@/lib/api';
import { useCart } from '@/store/cart';

type Product = MenuResponse['categories'][0]['products'][0];

export default function ProductModal({
  product,
  establishmentId,
  slug,
  onClose,
}: {
  product: Product;
  establishmentId: string;
  slug: string;
  onClose: () => void;
}) {
  const [quantity, setQuantity] = useState(1);
  const [selectedComplements, setSelectedComplements] = useState<string[]>([]);
  const addItem = useCart((s) => s.addItem);

  const complementsTotal = product.complements
    .filter((c) => selectedComplements.includes(c.id))
    .reduce((sum, c) => sum + c.price, 0);

  const unitTotal = (product.price + complementsTotal) * quantity;

  function toggleComplement(id: string) {
    setSelectedComplements((prev) =>
      prev.includes(id) ? prev.filter((c) => c !== id) : [...prev, id],
    );
  }

  function handleAdd() {
    addItem(
      {
        productId: product.id,
        name: product.name,
        price: product.price,
        quantity,
        complementIds: selectedComplements,
        complementsTotal,
      },
      establishmentId,
      slug,
    );
    onClose();
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40">
      <div className="w-full max-w-lg rounded-t-2xl bg-white p-6">
        <div className="mb-4 flex items-start justify-between">
          <h2 className="text-lg font-bold">{product.name}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">✕</button>
        </div>

        {product.description && (
          <p className="mb-4 text-sm text-gray-500">{product.description}</p>
        )}

        {product.complements.length > 0 && (
          <div className="mb-4">
            <p className="mb-2 font-semibold text-gray-700">Adicionais</p>
            {product.complements.map((c) => (
              <label key={c.id} className="flex items-center justify-between py-2">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={selectedComplements.includes(c.id)}
                    onChange={() => toggleComplement(c.id)}
                    className="h-4 w-4 accent-orange-500"
                  />
                  <span className="text-sm">{c.name}</span>
                </div>
                <span className="text-sm text-orange-500">+R$ {c.price.toFixed(2).replace('.', ',')}</span>
              </label>
            ))}
          </div>
        )}

        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setQuantity((q) => Math.max(1, q - 1))}
              className="flex h-8 w-8 items-center justify-center rounded-full border text-lg font-bold"
            >
              −
            </button>
            <span className="w-6 text-center font-semibold">{quantity}</span>
            <button
              onClick={() => setQuantity((q) => q + 1)}
              className="flex h-8 w-8 items-center justify-center rounded-full border text-lg font-bold"
            >
              +
            </button>
          </div>
          <button
            onClick={handleAdd}
            className="rounded-full bg-orange-500 px-6 py-2 font-semibold text-white hover:bg-orange-600"
          >
            Adicionar · R$ {unitTotal.toFixed(2).replace('.', ',')}
          </button>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 3: CategoryNav**

```tsx
// frontend/src/components/menu/CategoryNav.tsx
'use client';

export default function CategoryNav({
  categories,
}: {
  categories: { id: string; name: string }[];
}) {
  function scrollTo(id: string) {
    document.getElementById(`cat-${id}`)?.scrollIntoView({ behavior: 'smooth' });
  }

  return (
    <nav className="sticky top-0 z-10 flex gap-2 overflow-x-auto bg-white px-4 py-3 shadow-sm">
      {categories.map((cat) => (
        <button
          key={cat.id}
          onClick={() => scrollTo(cat.id)}
          className="shrink-0 rounded-full border border-orange-200 px-4 py-1 text-sm font-medium text-orange-600 hover:bg-orange-50"
        >
          {cat.name}
        </button>
      ))}
    </nav>
  );
}
```

- [ ] **Step 4: Página do cardápio**

```tsx
// frontend/src/app/[slug]/page.tsx
import { fetchMenu } from '@/lib/api';
import { notFound } from 'next/navigation';
import CategoryNav from '@/components/menu/CategoryNav';
import ProductCard from '@/components/menu/ProductCard';
import CartButton from '@/components/cart/CartButton';

export default async function MenuPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;

  let menu;
  try {
    menu = await fetchMenu(slug);
  } catch {
    notFound();
  }

  return (
    <main className="min-h-screen bg-gray-50 pb-24">
      <header className="bg-orange-500 px-4 py-6 text-white">
        <h1 className="text-2xl font-bold">{menu.name}</h1>
      </header>

      <CategoryNav categories={menu.categories} />

      <div className="mx-auto max-w-2xl px-4 py-4">
        {menu.categories.map((cat) => (
          <section key={cat.id} id={`cat-${cat.id}`} className="mb-8">
            <h2 className="mb-3 text-lg font-bold text-gray-800">{cat.name}</h2>
            <div className="flex flex-col gap-3">
              {cat.products.map((product) => (
                <ProductCard
                  key={product.id}
                  product={product}
                  establishmentId={menu.establishmentId}
                  slug={slug}
                />
              ))}
            </div>
          </section>
        ))}
      </div>

      <CartButton slug={slug} />
    </main>
  );
}
```

- [ ] **Step 5: Build**

```powershell
cd frontend && npm run build 2>&1 | Select-String "error"
```

Esperado: nenhuma linha `error TS`.

- [ ] **Step 6: Commit**

```bash
cd ..
git add frontend/src/components/menu/ frontend/src/app/[slug]/page.tsx
git commit -m "feat(frontend): add menu page with CategoryNav, ProductCard, ProductModal"
```

---

### Task 14: CartButton e CartDrawer

**Files:**
- Create: `frontend/src/components/cart/CartButton.tsx`
- Create: `frontend/src/components/cart/CartDrawer.tsx`

- [ ] **Step 1: CartButton (botão flutuante)**

```tsx
// frontend/src/components/cart/CartButton.tsx
'use client';
import { useState } from 'react';
import { useCart } from '@/store/cart';
import CartDrawer from './CartDrawer';

export default function CartButton({ slug }: { slug: string }) {
  const [open, setOpen] = useState(false);
  const items = useCart((s) => s.items);
  const total = useCart((s) => s.total);
  const count = items.reduce((sum, i) => sum + i.quantity, 0);

  if (count === 0) return null;

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="fixed bottom-6 left-1/2 z-40 flex -translate-x-1/2 items-center gap-3 rounded-full bg-orange-500 px-6 py-3 text-white shadow-lg hover:bg-orange-600"
      >
        <span className="flex h-6 w-6 items-center justify-center rounded-full bg-white text-sm font-bold text-orange-500">
          {count}
        </span>
        <span className="font-semibold">Ver carrinho</span>
        <span className="font-bold">R$ {total().toFixed(2).replace('.', ',')}</span>
      </button>

      {open && <CartDrawer slug={slug} onClose={() => setOpen(false)} />}
    </>
  );
}
```

- [ ] **Step 2: CartDrawer**

```tsx
// frontend/src/components/cart/CartDrawer.tsx
'use client';
import { useRouter } from 'next/navigation';
import { useCart } from '@/store/cart';

export default function CartDrawer({
  slug,
  onClose,
}: {
  slug: string;
  onClose: () => void;
}) {
  const router = useRouter();
  const { items, updateQuantity, removeItem, total } = useCart();

  function goToCheckout() {
    onClose();
    router.push(`/${slug}/checkout`);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40">
      <div className="w-full max-w-lg rounded-t-2xl bg-white">
        <div className="flex items-center justify-between border-b p-4">
          <h2 className="text-lg font-bold">Seu pedido</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">✕</button>
        </div>

        <div className="max-h-80 overflow-y-auto p-4">
          {items.map((item) => (
            <div key={item.productId} className="flex items-center justify-between py-3">
              <div className="flex-1">
                <p className="font-medium">{item.name}</p>
                <p className="text-sm text-orange-500">
                  R$ {((item.price + item.complementsTotal) * item.quantity).toFixed(2).replace('.', ',')}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => updateQuantity(item.productId, item.quantity - 1)}
                  className="flex h-7 w-7 items-center justify-center rounded-full border"
                >
                  −
                </button>
                <span className="w-5 text-center text-sm font-semibold">{item.quantity}</span>
                <button
                  onClick={() => updateQuantity(item.productId, item.quantity + 1)}
                  className="flex h-7 w-7 items-center justify-center rounded-full border"
                >
                  +
                </button>
                <button
                  onClick={() => removeItem(item.productId)}
                  className="ml-1 text-gray-400 hover:text-red-500"
                >
                  🗑
                </button>
              </div>
            </div>
          ))}
        </div>

        <div className="border-t p-4">
          <div className="mb-4 flex items-center justify-between font-bold">
            <span>Total</span>
            <span>R$ {total().toFixed(2).replace('.', ',')}</span>
          </div>
          <button
            onClick={goToCheckout}
            className="w-full rounded-full bg-orange-500 py-3 font-semibold text-white hover:bg-orange-600"
          >
            Ir para o checkout
          </button>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 3: Build**

```powershell
cd frontend && npm run build 2>&1 | Select-String "error"
```

Esperado: sem erros de tipo.

- [ ] **Step 4: Commit**

```bash
cd ..
git add frontend/src/components/cart/
git commit -m "feat(frontend): add CartButton and CartDrawer components"
```

---

### Task 15: Página de checkout (GuestForm + PixPanel)

**Files:**
- Create: `frontend/src/components/checkout/GuestForm.tsx`
- Create: `frontend/src/components/checkout/PixPanel.tsx`
- Create: `frontend/src/app/[slug]/checkout/page.tsx`

- [ ] **Step 1: GuestForm**

```tsx
// frontend/src/components/checkout/GuestForm.tsx
'use client';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';

const schema = z.object({
  name: z.string().min(3, 'Nome obrigatório'),
  phone: z.string().min(10, 'Telefone inválido').max(11),
  cpf: z.string().length(11, 'CPF deve ter 11 dígitos'),
  street: z.string().min(3, 'Rua obrigatória'),
  number: z.string().min(1, 'Número obrigatório'),
  neighborhood: z.string().min(2, 'Bairro obrigatório'),
  city: z.string().min(2, 'Cidade obrigatória'),
  complement: z.string().optional(),
});

export type CheckoutFormData = z.infer<typeof schema>;

export default function GuestForm({
  onSubmit,
  loading,
}: {
  onSubmit: (data: CheckoutFormData) => void;
  loading: boolean;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CheckoutFormData>({ resolver: zodResolver(schema) });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
      <h2 className="text-lg font-bold">Seus dados</h2>

      {[
        { field: 'name', label: 'Nome completo', placeholder: 'João Silva' },
        { field: 'phone', label: 'Telefone (só números)', placeholder: '11999999999' },
        { field: 'cpf', label: 'CPF (só números)', placeholder: '00000000000' },
      ].map(({ field, label, placeholder }) => (
        <div key={field}>
          <label className="mb-1 block text-sm font-medium text-gray-700">{label}</label>
          <input
            {...register(field as keyof CheckoutFormData)}
            placeholder={placeholder}
            className="w-full rounded-lg border px-4 py-2 focus:outline-none focus:ring-2 focus:ring-orange-400"
          />
          {errors[field as keyof CheckoutFormData] && (
            <p className="mt-1 text-xs text-red-500">
              {errors[field as keyof CheckoutFormData]?.message}
            </p>
          )}
        </div>
      ))}

      <h2 className="mt-2 text-lg font-bold">Endereço de entrega</h2>

      {[
        { field: 'street', label: 'Rua', placeholder: 'Rua das Flores' },
        { field: 'number', label: 'Número', placeholder: '123' },
        { field: 'complement', label: 'Complemento (opcional)', placeholder: 'Apto 4' },
        { field: 'neighborhood', label: 'Bairro', placeholder: 'Centro' },
        { field: 'city', label: 'Cidade', placeholder: 'São Paulo' },
      ].map(({ field, label, placeholder }) => (
        <div key={field}>
          <label className="mb-1 block text-sm font-medium text-gray-700">{label}</label>
          <input
            {...register(field as keyof CheckoutFormData)}
            placeholder={placeholder}
            className="w-full rounded-lg border px-4 py-2 focus:outline-none focus:ring-2 focus:ring-orange-400"
          />
          {errors[field as keyof CheckoutFormData] && (
            <p className="mt-1 text-xs text-red-500">
              {errors[field as keyof CheckoutFormData]?.message}
            </p>
          )}
        </div>
      ))}

      <button
        type="submit"
        disabled={loading}
        className="mt-4 w-full rounded-full bg-orange-500 py-3 font-semibold text-white disabled:opacity-60 hover:bg-orange-600"
      >
        {loading ? 'Gerando PIX...' : 'Gerar PIX'}
      </button>
    </form>
  );
}
```

- [ ] **Step 2: PixPanel**

```tsx
// frontend/src/components/checkout/PixPanel.tsx
'use client';
import { useEffect, useState } from 'react';
import QRCode from 'qrcode.react';
import { useRouter } from 'next/navigation';

export default function PixPanel({
  orderId,
  qrCode,
  copyPaste,
  expiresAt,
}: {
  orderId: string;
  qrCode: string;
  copyPaste: string;
  expiresAt: string;
}) {
  const router = useRouter();
  const [copied, setCopied] = useState(false);
  const [timeLeft, setTimeLeft] = useState('');

  useEffect(() => {
    const interval = setInterval(() => {
      const diff = new Date(expiresAt).getTime() - Date.now();
      if (diff <= 0) {
        setTimeLeft('Expirado');
        clearInterval(interval);
        return;
      }
      const m = Math.floor(diff / 60000);
      const s = Math.floor((diff % 60000) / 1000);
      setTimeLeft(`${m}:${s.toString().padStart(2, '0')}`);
    }, 1000);
    return () => clearInterval(interval);
  }, [expiresAt]);

  // Escuta SSE para redirect automático quando pagamento for confirmado
  useEffect(() => {
    const es = new EventSource(`/bff/orders/${orderId}/track`);
    es.addEventListener('status-changed', (e) => {
      const data = JSON.parse(e.data);
      if (data.status === 'Confirmed') {
        es.close();
        router.push(`/pedido/${orderId}`);
      }
    });
    return () => es.close();
  }, [orderId, router]);

  function handleCopy() {
    navigator.clipboard.writeText(copyPaste);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div className="flex flex-col items-center gap-6 py-6">
      <div className="text-center">
        <p className="text-lg font-bold text-gray-800">Pague com PIX</p>
        <p className="text-sm text-gray-500">
          Expira em <span className="font-semibold text-orange-500">{timeLeft}</span>
        </p>
      </div>

      <div className="rounded-2xl border-4 border-orange-100 p-4">
        <QRCode value={qrCode} size={200} />
      </div>

      <div className="w-full">
        <p className="mb-2 text-sm font-medium text-gray-700">Ou copie o código:</p>
        <div className="flex gap-2">
          <input
            readOnly
            value={copyPaste}
            className="flex-1 rounded-lg border px-3 py-2 text-xs text-gray-600"
          />
          <button
            onClick={handleCopy}
            className="rounded-lg bg-orange-500 px-4 py-2 text-sm font-semibold text-white hover:bg-orange-600"
          >
            {copied ? 'Copiado!' : 'Copiar'}
          </button>
        </div>
      </div>

      <p className="text-center text-sm text-gray-500">
        Aguardando confirmação do pagamento...
      </p>
    </div>
  );
}
```

- [ ] **Step 3: Página de checkout**

```tsx
// frontend/src/app/[slug]/checkout/page.tsx
'use client';
import { useState } from 'react';
import { useCart } from '@/store/cart';
import { guestSession, placeOrder, saveToken, PlaceOrderResponse } from '@/lib/api';
import GuestForm, { CheckoutFormData } from '@/components/checkout/GuestForm';
import PixPanel from '@/components/checkout/PixPanel';

export default function CheckoutPage({ params }: { params: { slug: string } }) {
  const { items, establishmentId, total, clear } = useCart();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [orderResult, setOrderResult] = useState<PlaceOrderResponse | null>(null);

  async function handleSubmit(data: CheckoutFormData) {
    if (!establishmentId || items.length === 0) return;
    setLoading(true);
    setError(null);

    try {
      // Cria sessão guest se não houver token
      if (!localStorage.getItem('delify_token')) {
        const session = await guestSession(data.name, data.phone);
        saveToken(session.token);
      }

      const result = await placeOrder({
        establishmentId,
        items: items.map((i) => ({
          productId: i.productId,
          quantity: i.quantity,
          complementIds: i.complementIds,
        })),
        customer: { name: data.name, phone: data.phone, cpf: data.cpf },
        address: {
          street: data.street,
          number: data.number,
          neighborhood: data.neighborhood,
          city: data.city,
          complement: data.complement,
        },
      });

      clear();
      setOrderResult(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro desconhecido');
    } finally {
      setLoading(false);
    }
  }

  if (orderResult) {
    return (
      <main className="mx-auto max-w-lg px-4 py-8">
        <PixPanel
          orderId={orderResult.orderId}
          qrCode={orderResult.pix.qrCode}
          copyPaste={orderResult.pix.copyPaste}
          expiresAt={orderResult.pix.expiresAt}
        />
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-lg px-4 py-8">
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Checkout</h1>
        <p className="text-gray-500">
          {items.length} {items.length === 1 ? 'item' : 'itens'} ·{' '}
          R$ {total().toFixed(2).replace('.', ',')}
        </p>
      </div>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 p-4 text-sm text-red-600">{error}</div>
      )}

      <GuestForm onSubmit={handleSubmit} loading={loading} />
    </main>
  );
}
```

- [ ] **Step 4: Build**

```powershell
cd frontend && npm run build 2>&1 | Select-String "error"
```

Esperado: sem erros de tipo.

- [ ] **Step 5: Commit**

```bash
cd ..
git add frontend/src/components/checkout/ frontend/src/app/[slug]/checkout/
git commit -m "feat(frontend): add checkout page with GuestForm and PixPanel (SSE auto-redirect)"
```

---

### Task 16: Página de acompanhamento (SSE)

**Files:**
- Create: `frontend/src/components/tracking/useOrderTracking.ts`
- Create: `frontend/src/components/tracking/StatusStepper.tsx`
- Create: `frontend/src/app/pedido/[id]/page.tsx`

- [ ] **Step 1: useOrderTracking hook**

```typescript
// frontend/src/components/tracking/useOrderTracking.ts
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
```

- [ ] **Step 2: StatusStepper**

```tsx
// frontend/src/components/tracking/StatusStepper.tsx
'use client';
import { useOrderTracking } from './useOrderTracking';

export default function StatusStepper({ orderId }: { orderId: string }) {
  const { currentStatus, steps } = useOrderTracking(orderId);

  const currentIndex = steps.findIndex((s) => s.key === currentStatus);

  return (
    <div className="flex flex-col gap-0">
      {steps.map((step, i) => {
        const isCompleted = i < currentIndex;
        const isCurrent = i === currentIndex;

        return (
          <div key={step.key} className="flex items-start gap-4">
            <div className="flex flex-col items-center">
              <div
                className={`flex h-8 w-8 items-center justify-center rounded-full border-2 text-sm font-bold transition-colors ${
                  isCompleted
                    ? 'border-orange-500 bg-orange-500 text-white'
                    : isCurrent
                    ? 'border-orange-500 bg-white text-orange-500'
                    : 'border-gray-200 bg-white text-gray-300'
                }`}
              >
                {isCompleted ? '✓' : i + 1}
              </div>
              {i < steps.length - 1 && (
                <div
                  className={`mt-1 h-8 w-0.5 ${
                    isCompleted ? 'bg-orange-500' : 'bg-gray-200'
                  }`}
                />
              )}
            </div>
            <div className="pb-6 pt-1">
              <p
                className={`text-sm font-medium ${
                  isCurrent ? 'text-orange-500' : isCompleted ? 'text-gray-700' : 'text-gray-400'
                }`}
              >
                {step.label}
              </p>
            </div>
          </div>
        );
      })}
    </div>
  );
}
```

- [ ] **Step 3: Página de acompanhamento**

```tsx
// frontend/src/app/pedido/[id]/page.tsx
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
```

- [ ] **Step 4: Build**

```powershell
cd frontend && npm run build 2>&1 | Select-String "error"
```

Esperado: sem erros de tipo.

- [ ] **Step 5: Commit**

```bash
cd ..
git add frontend/src/components/tracking/ frontend/src/app/pedido/
git commit -m "feat(frontend): add order tracking page with SSE StatusStepper"
```

---

### Task 17: Páginas de conta (login e cadastro)

**Files:**
- Create: `frontend/src/app/conta/login/page.tsx`
- Create: `frontend/src/app/conta/cadastro/page.tsx`

- [ ] **Step 1: Página de login**

```tsx
// frontend/src/app/conta/login/page.tsx
'use client';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { login, saveToken } from '@/lib/api';
import Link from 'next/link';

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await login(email, password);
      saveToken(res.token);
      router.push('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro no login');
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="mx-auto max-w-sm px-4 py-12">
      <h1 className="mb-6 text-2xl font-bold">Entrar</h1>
      {error && <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</div>}
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <input
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="rounded-lg border px-4 py-2 focus:outline-none focus:ring-2 focus:ring-orange-400"
        />
        <input
          type="password"
          placeholder="Senha"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          className="rounded-lg border px-4 py-2 focus:outline-none focus:ring-2 focus:ring-orange-400"
        />
        <button
          type="submit"
          disabled={loading}
          className="rounded-full bg-orange-500 py-3 font-semibold text-white disabled:opacity-60 hover:bg-orange-600"
        >
          {loading ? 'Entrando...' : 'Entrar'}
        </button>
      </form>
      <p className="mt-4 text-center text-sm text-gray-500">
        Não tem conta?{' '}
        <Link href="/conta/cadastro" className="text-orange-500 font-medium">
          Cadastre-se
        </Link>
      </p>
    </main>
  );
}
```

- [ ] **Step 2: Página de cadastro**

```tsx
// frontend/src/app/conta/cadastro/page.tsx
'use client';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { register, saveToken } from '@/lib/api';
import Link from 'next/link';

export default function CadastroPage() {
  const router = useRouter();
  const [form, setForm] = useState({ name: '', email: '', password: '', phone: '' });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  function update(field: string, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await register(form.name, form.email, form.password, form.phone);
      saveToken(res.token);
      router.push('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro no cadastro');
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="mx-auto max-w-sm px-4 py-12">
      <h1 className="mb-6 text-2xl font-bold">Criar conta</h1>
      {error && <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</div>}
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        {[
          { field: 'name', label: 'Nome', type: 'text', placeholder: 'João Silva' },
          { field: 'email', label: 'Email', type: 'email', placeholder: 'joao@email.com' },
          { field: 'phone', label: 'Telefone', type: 'tel', placeholder: '11999999999' },
          { field: 'password', label: 'Senha (mín. 8 caracteres)', type: 'password', placeholder: '••••••••' },
        ].map(({ field, type, placeholder }) => (
          <input
            key={field}
            type={type}
            placeholder={placeholder}
            value={form[field as keyof typeof form]}
            onChange={(e) => update(field, e.target.value)}
            required
            className="rounded-lg border px-4 py-2 focus:outline-none focus:ring-2 focus:ring-orange-400"
          />
        ))}
        <button
          type="submit"
          disabled={loading}
          className="rounded-full bg-orange-500 py-3 font-semibold text-white disabled:opacity-60 hover:bg-orange-600"
        >
          {loading ? 'Criando conta...' : 'Criar conta'}
        </button>
      </form>
      <p className="mt-4 text-center text-sm text-gray-500">
        Já tem conta?{' '}
        <Link href="/conta/login" className="text-orange-500 font-medium">
          Entrar
        </Link>
      </p>
    </main>
  );
}
```

- [ ] **Step 3: Build final completo**

```powershell
cd frontend && npm run build
```

Esperado: `Compiled successfully.` sem erros de tipo.

- [ ] **Step 4: Root layout com link para conta**

Substitua `frontend/src/app/layout.tsx` pelo seguinte:

```tsx
// frontend/src/app/layout.tsx
import type { Metadata } from 'next';
import { Geist } from 'next/font/google';
import './globals.css';
import Link from 'next/link';

const geist = Geist({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'Delify',
  description: 'Peça sua comida favorita',
  manifest: '/manifest.json',
  themeColor: '#f97316',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR">
      <body className={geist.className}>
        {children}
      </body>
    </html>
  );
}
```

- [ ] **Step 5: Commit final**

```bash
cd ..
git add frontend/src/app/conta/ frontend/src/app/layout.tsx
git commit -m "feat(frontend): add login, cadastro pages and root layout"
```

---

### Task 18: Smoke test end-to-end

- [ ] **Step 1: Subir a infraestrutura**

```powershell
docker-compose up -d
```

Aguardar containers ficarem healthy (verificar com `docker-compose ps`).

- [ ] **Step 2: Subir o backend**

```powershell
dotnet run --project src/Delify.Api/Delify.Api.csproj
```

Deixar rodando em outro terminal.

- [ ] **Step 3: Subir o frontend**

```powershell
cd frontend && npm run dev
```

- [ ] **Step 4: Testar o fluxo completo**

1. Abrir `http://localhost:3000/<slug>` (substitua `<slug>` por um slug cadastrado via Scalar em `http://localhost:5000/scalar`)
2. Verificar que o cardápio carrega
3. Adicionar um produto ao carrinho — o CartButton deve aparecer
4. Abrir o CartDrawer e ir para checkout
5. Preencher os dados e gerar PIX
6. Verificar que o QR Code aparece
7. Verificar no Scalar que o pedido existe em `GET /api/orders/establishments/{id}`
8. Verificar que o SSE de tracking está ativo em `GET /bff/orders/{orderId}/track`

- [ ] **Step 5: Commit de smoke test confirmado**

```bash
git add .
git commit -m "chore: smoke test passed — full customer flow working end-to-end"
```

---

## Checklist de cobertura do spec

- [x] `GET /bff/menu/{slug}` → Task 6
- [x] `POST /bff/orders` (cria pedido + inicia PIX) → Task 8
- [x] `GET /bff/orders/{id}/track` SSE → Task 9
- [x] `POST /bff/auth/guest` → Task 7
- [x] `POST /bff/auth/register` + `POST /bff/auth/login` → Task 7
- [x] `IOrderTrackingNotifier` compartilhado entre módulos → Tasks 1 e 2
- [x] Next.js PWA com manifest + Service Worker → Task 11
- [x] Cardápio SSR + CategoryNav + ProductCard + ProductModal → Task 13
- [x] CartButton flutuante + CartDrawer → Task 14
- [x] GuestForm + PixPanel com timer e SSE redirect → Task 15
- [x] Página de tracking com StatusStepper → Task 16
- [x] Login + Cadastro → Task 17
- [x] CORS configurado → Task 10
- [x] Proxy Next.js → .NET → Task 11
