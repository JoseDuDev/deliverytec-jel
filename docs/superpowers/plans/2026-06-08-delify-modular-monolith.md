# Delify — Modular Monolith Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a production-ready .NET 10 modular monolith solution for the Delify SaaS delivery platform, with all modules scaffolded, wired together, and pushed to GitHub.

**Architecture:** Monolithic Modular — each domain (Catalog, Orders, Payments, Delivery, Identity) lives in its own class-library project with its own DbContext, but all share a single API host process. Modules register themselves via an `IModule` interface, keeping the API project thin. Inter-module communication happens through domain events over MassTransit/RabbitMQ, never direct project references between modules.

**Tech Stack:** .NET 10 · ASP.NET Core Minimal API · EF Core 10 · PostgreSQL 16 · Redis 7 · RabbitMQ 3 · MassTransit 8 · MediatR 12 · Npgsql · ASP.NET Identity · JWT Bearer · Docker Compose · GitHub Actions

---

## File Map

```
Delify/
├── Delify.sln
├── .gitignore
├── .editorconfig
├── docker-compose.yml
├── docker-compose.override.yml
├── global.json                          ← pins SDK to 10.0.204
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── src/
│   ├── Delify.Api/                      ← entry point
│   │   ├── Delify.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   ├── Delify.Shared/                   ← shared kernel (no deps on modules)
│   │   ├── Delify.Shared.csproj
│   │   ├── Domain/
│   │   │   ├── Entity.cs
│   │   │   ├── AggregateRoot.cs
│   │   │   └── IDomainEvent.cs
│   │   ├── Abstractions/
│   │   │   ├── IModule.cs
│   │   │   └── ITenantContext.cs
│   │   └── Result/
│   │       ├── Result.cs
│   │       └── Error.cs
│   │
│   ├── Delify.Modules.Identity/
│   │   ├── Delify.Modules.Identity.csproj
│   │   ├── IdentityModule.cs             ← IModule registration
│   │   ├── Domain/
│   │   │   ├── AppUser.cs
│   │   │   └── Tenant.cs
│   │   ├── Infrastructure/
│   │   │   ├── IdentityDbContext.cs
│   │   │   └── TenantContext.cs
│   │   └── Endpoints/
│   │       ├── RegisterEndpoint.cs
│   │       └── LoginEndpoint.cs
│   │
│   ├── Delify.Modules.Catalog/
│   │   ├── Delify.Modules.Catalog.csproj
│   │   ├── CatalogModule.cs
│   │   ├── Domain/
│   │   │   ├── Establishment.cs
│   │   │   ├── Category.cs
│   │   │   ├── Product.cs
│   │   │   └── Complement.cs
│   │   ├── Infrastructure/
│   │   │   └── CatalogDbContext.cs
│   │   └── Endpoints/
│   │       ├── EstablishmentEndpoints.cs
│   │       ├── CategoryEndpoints.cs
│   │       └── ProductEndpoints.cs
│   │
│   ├── Delify.Modules.Orders/
│   │   ├── Delify.Modules.Orders.csproj
│   │   ├── OrdersModule.cs
│   │   ├── Domain/
│   │   │   ├── Order.cs
│   │   │   ├── OrderItem.cs
│   │   │   ├── OrderStatus.cs
│   │   │   └── Events/
│   │   │       ├── OrderCreatedEvent.cs
│   │   │       └── OrderStatusChangedEvent.cs
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateOrderCommand.cs
│   │   │   │   └── UpdateOrderStatusCommand.cs
│   │   │   └── Queries/
│   │   │       └── GetOrdersByEstablishmentQuery.cs
│   │   ├── Infrastructure/
│   │   │   └── OrdersDbContext.cs
│   │   └── Endpoints/
│   │       └── OrderEndpoints.cs
│   │
│   ├── Delify.Modules.Payments/
│   │   ├── Delify.Modules.Payments.csproj
│   │   ├── PaymentsModule.cs
│   │   ├── Domain/
│   │   │   ├── Payment.cs
│   │   │   └── PaymentStatus.cs
│   │   ├── Abstractions/
│   │   │   └── IPaymentGateway.cs
│   │   ├── Gateways/
│   │   │   └── AsaasPaymentGateway.cs
│   │   ├── Infrastructure/
│   │   │   └── PaymentsDbContext.cs
│   │   └── Endpoints/
│   │       ├── CheckoutEndpoint.cs
│   │       └── WebhookEndpoint.cs
│   │
│   └── Delify.Modules.Delivery/
│       ├── Delify.Modules.Delivery.csproj
│       ├── DeliveryModule.cs
│       ├── Domain/
│       │   ├── DeliveryOrder.cs
│       │   └── DeliveryStatus.cs
│       ├── Abstractions/
│       │   └── IDeliveryProvider.cs
│       ├── Providers/
│       │   ├── LalamoveDeliveryProvider.cs
│       │   └── BorzoDeliveryProvider.cs
│       ├── Infrastructure/
│       │   └── DeliveryDbContext.cs
│       └── Endpoints/
│           └── DeliveryEndpoints.cs
│
└── tests/
    ├── Delify.Modules.Catalog.Tests/
    │   ├── Delify.Modules.Catalog.Tests.csproj
    │   └── Domain/
    │       └── ProductTests.cs
    └── Delify.Modules.Orders.Tests/
        ├── Delify.Modules.Orders.Tests.csproj
        └── Domain/
            └── OrderTests.cs
```

---

## Task 1: SDK pin, .gitignore, solution scaffold

**Files:**
- Create: `global.json`
- Create: `.gitignore`
- Create: `.editorconfig`
- Create: `Delify.sln`

- [ ] **Step 1: Pin SDK version**

```bash
cd C:\Projetos\JEL\JEL\Delify
```

Create `global.json`:
```json
{
  "sdk": {
    "version": "10.0.204",
    "rollForward": "latestPatch"
  }
}
```

- [ ] **Step 2: Create .gitignore**

```bash
dotnet new gitignore
```

- [ ] **Step 3: Create .editorconfig**

Create `.editorconfig`:
```ini
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{csproj,props,targets}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

- [ ] **Step 4: Create blank solution**

```bash
dotnet new sln -n Delify
```

Expected output: `The template "Solution File" was created successfully.`

- [ ] **Step 5: Commit**

```bash
git init
git remote add origin https://github.com/JoseDuDev/deliverytec-jel.git
git add global.json .gitignore .editorconfig Delify.sln
git commit -m "chore: init solution with SDK pin and gitignore"
```

---

## Task 2: Delify.Shared — shared kernel

**Files:**
- Create: `src/Delify.Shared/Delify.Shared.csproj`
- Create: `src/Delify.Shared/Domain/Entity.cs`
- Create: `src/Delify.Shared/Domain/AggregateRoot.cs`
- Create: `src/Delify.Shared/Domain/IDomainEvent.cs`
- Create: `src/Delify.Shared/Abstractions/IModule.cs`
- Create: `src/Delify.Shared/Abstractions/ITenantContext.cs`
- Create: `src/Delify.Shared/Result/Result.cs`
- Create: `src/Delify.Shared/Result/Error.cs`

- [ ] **Step 1: Create project**

```bash
dotnet new classlib -n Delify.Shared -o src/Delify.Shared -f net10.0
dotnet sln add src/Delify.Shared/Delify.Shared.csproj
```

- [ ] **Step 2: Replace csproj**

`src/Delify.Shared/Delify.Shared.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.Routing" Version="2.3.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create Domain base classes**

Delete `Class1.cs`:
```bash
rm src/Delify.Shared/Class1.cs
```

`src/Delify.Shared/Domain/IDomainEvent.cs`:
```csharp
namespace Delify.Shared.Domain;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredAt { get; }
}
```

`src/Delify.Shared/Domain/Entity.cs`:
```csharp
namespace Delify.Shared.Domain;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid TenantId { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }
}
```

`src/Delify.Shared/Domain/AggregateRoot.cs`:
```csharp
namespace Delify.Shared.Domain;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

- [ ] **Step 4: Create Abstractions**

`src/Delify.Shared/Abstractions/ITenantContext.cs`:
```csharp
namespace Delify.Shared.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }
}
```

`src/Delify.Shared/Abstractions/IModule.cs`:
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace Delify.Shared.Abstractions;

public interface IModule
{
    string Name { get; }
    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

- [ ] **Step 5: Create Result pattern**

`src/Delify.Shared/Result/Error.cs`:
```csharp
namespace Delify.Shared.Result;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static Error NotFound(string entity) => new($"{entity}.NotFound", $"{entity} not found.");
    public static Error Conflict(string entity) => new($"{entity}.Conflict", $"{entity} already exists.");
    public static Error Validation(string message) => new("Validation.Error", message);
}
```

`src/Delify.Shared/Result/Result.cs`:
```csharp
namespace Delify.Shared.Result;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None) throw new InvalidOperationException();
        if (!isSuccess && error == Error.None) throw new InvalidOperationException();
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
        => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");
}
```

- [ ] **Step 6: Build to verify**

```bash
dotnet build src/Delify.Shared/Delify.Shared.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add src/Delify.Shared/
git commit -m "feat: add Delify.Shared kernel (Entity, AggregateRoot, IModule, Result<T>)"
```

---

## Task 3: Delify.Modules.Identity

**Files:**
- Create: `src/Delify.Modules.Identity/Delify.Modules.Identity.csproj`
- Create: `src/Delify.Modules.Identity/IdentityModule.cs`
- Create: `src/Delify.Modules.Identity/Domain/Tenant.cs`
- Create: `src/Delify.Modules.Identity/Domain/AppUser.cs`
- Create: `src/Delify.Modules.Identity/Infrastructure/IdentityDbContext.cs`
- Create: `src/Delify.Modules.Identity/Infrastructure/TenantContext.cs`
- Create: `src/Delify.Modules.Identity/Endpoints/RegisterEndpoint.cs`
- Create: `src/Delify.Modules.Identity/Endpoints/LoginEndpoint.cs`

- [ ] **Step 1: Create project and add to solution**

```bash
dotnet new classlib -n Delify.Modules.Identity -o src/Delify.Modules.Identity -f net10.0
dotnet sln add src/Delify.Modules.Identity/Delify.Modules.Identity.csproj
```

- [ ] **Step 2: Configure csproj**

`src/Delify.Modules.Identity/Delify.Modules.Identity.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" PrivateAssets="all" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.*" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Delify.Shared\Delify.Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create Domain**

Delete `Class1.cs`:
```bash
rm src/Delify.Modules.Identity/Class1.cs
```

`src/Delify.Modules.Identity/Domain/Tenant.cs`:
```csharp
namespace Delify.Modules.Identity.Domain;

public sealed class Tenant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Tenant() { }

    public static Tenant Create(string slug, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Tenant { Slug = slug.ToLowerInvariant(), Name = name };
    }
}
```

`src/Delify.Modules.Identity/Domain/AppUser.cs`:
```csharp
using Microsoft.AspNetCore.Identity;

namespace Delify.Modules.Identity.Domain;

public sealed class AppUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

- [ ] **Step 4: Create Infrastructure**

`src/Delify.Modules.Identity/Infrastructure/IdentityDbContext.cs`:
```csharp
using Delify.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Identity.Infrastructure;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");

        builder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Slug).IsUnique();
            e.Property(t => t.Slug).HasMaxLength(100);
            e.Property(t => t.Name).HasMaxLength(200);
        });
    }
}
```

`src/Delify.Modules.Identity/Infrastructure/TenantContext.cs`:
```csharp
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Delify.Modules.Identity.Infrastructure;

public sealed class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid TenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User
                .FindFirst("tenant_id")?.Value;

            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
}
```

- [ ] **Step 5: Create Endpoints**

`src/Delify.Modules.Identity/Endpoints/RegisterEndpoint.cs`:
```csharp
using Delify.Modules.Identity.Domain;
using Delify.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Identity.Endpoints;

internal static class RegisterEndpoint
{
    internal record Request(string Slug, string EstablishmentName, string Email, string Password, string FullName);
    internal record Response(Guid TenantId, Guid UserId);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (
            Request req,
            IdentityDbContext db,
            UserManager<AppUser> userManager) =>
        {
            var existing = db.Tenants.FirstOrDefault(t => t.Slug == req.Slug.ToLowerInvariant());
            if (existing is not null)
                return Results.Conflict(new { error = "Slug already taken." });

            var tenant = Tenant.Create(req.Slug, req.EstablishmentName);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var user = new AppUser
            {
                UserName = req.Email,
                Email = req.Email,
                FullName = req.FullName,
                TenantId = tenant.Id
            };

            var result = await userManager.CreateAsync(user, req.Password);
            if (!result.Succeeded)
                return Results.BadRequest(result.Errors);

            return Results.Created($"/api/tenants/{tenant.Id}", new Response(tenant.Id, user.Id));
        })
        .WithName("Register")
        .WithTags("Auth")
        .AllowAnonymous();

        return app;
    }
}
```

`src/Delify.Modules.Identity/Endpoints/LoginEndpoint.cs`:
```csharp
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

namespace Delify.Modules.Identity.Endpoints;

internal static class LoginEndpoint
{
    internal record Request(string Email, string Password);
    internal record Response(string Token, DateTimeOffset ExpiresAt);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            Request req,
            UserManager<AppUser> userManager,
            IConfiguration config) =>
        {
            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Unauthorized();

            var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTimeOffset.UtcNow.AddHours(8);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim("full_name", user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: expires.UtcDateTime,
                signingCredentials: creds);

            return Results.Ok(new Response(new JwtSecurityTokenHandler().WriteToken(token), expires));
        })
        .WithName("Login")
        .WithTags("Auth")
        .AllowAnonymous();

        return app;
    }
}
```

- [ ] **Step 6: Create IdentityModule**

`src/Delify.Modules.Identity/IdentityModule.cs`:
```csharp
using Delify.Modules.Identity.Endpoints;
using Delify.Modules.Identity.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Delify.Modules.Identity;

public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

        services.AddIdentityCore<Domain.AppUser>(o =>
        {
            o.Password.RequireNonAlphanumeric = false;
            o.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<IdentityDbContext>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();

        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        RegisterEndpoint.Map(endpoints);
        LoginEndpoint.Map(endpoints);
        return endpoints;
    }
}
```

- [ ] **Step 7: Build**

```bash
dotnet build src/Delify.Modules.Identity/Delify.Modules.Identity.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add src/Delify.Modules.Identity/
git commit -m "feat(identity): add Identity module with JWT auth, tenant creation, EF Core"
```

---

## Task 4: Delify.Modules.Catalog

**Files:**
- Create: `src/Delify.Modules.Catalog/Delify.Modules.Catalog.csproj`
- Create: `src/Delify.Modules.Catalog/CatalogModule.cs`
- Create: `src/Delify.Modules.Catalog/Domain/Establishment.cs`
- Create: `src/Delify.Modules.Catalog/Domain/Category.cs`
- Create: `src/Delify.Modules.Catalog/Domain/Product.cs`
- Create: `src/Delify.Modules.Catalog/Domain/Complement.cs`
- Create: `src/Delify.Modules.Catalog/Infrastructure/CatalogDbContext.cs`
- Create: `src/Delify.Modules.Catalog/Endpoints/ProductEndpoints.cs`

- [ ] **Step 1: Create project**

```bash
dotnet new classlib -n Delify.Modules.Catalog -o src/Delify.Modules.Catalog -f net10.0
dotnet sln add src/Delify.Modules.Catalog/Delify.Modules.Catalog.csproj
rm src/Delify.Modules.Catalog/Class1.cs
```

- [ ] **Step 2: Configure csproj**

`src/Delify.Modules.Catalog/Delify.Modules.Catalog.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" PrivateAssets="all" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Delify.Shared\Delify.Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create Domain**

`src/Delify.Modules.Catalog/Domain/Establishment.cs`:
```csharp
using Delify.Shared.Domain;

namespace Delify.Modules.Catalog.Domain;

public sealed class Establishment : Entity
{
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? LogoUrl { get; private set; }
    public string? Description { get; private set; }
    public bool IsOpen { get; private set; }
    public ICollection<Category> Categories { get; private set; } = [];

    private Establishment() { }

    public static Establishment Create(Guid tenantId, string slug, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var e = new Establishment
        {
            TenantId = tenantId,
            Slug = slug.ToLowerInvariant(),
            Name = name
        };
        return e;
    }

    public void Update(string name, string? description, string? logoUrl, bool isOpen)
    {
        Name = name;
        Description = description;
        LogoUrl = logoUrl;
        IsOpen = isOpen;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

`src/Delify.Modules.Catalog/Domain/Category.cs`:
```csharp
using Delify.Shared.Domain;

namespace Delify.Modules.Catalog.Domain;

public sealed class Category : Entity
{
    public Guid EstablishmentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public bool IsActive { get; private set; } = true;
    public ICollection<Product> Products { get; private set; } = [];

    private Category() { }

    public static Category Create(Guid tenantId, Guid establishmentId, string name, int order = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Category
        {
            TenantId = tenantId,
            EstablishmentId = establishmentId,
            Name = name,
            Order = order
        };
    }
}
```

`src/Delify.Modules.Catalog/Domain/Product.cs`:
```csharp
using Delify.Shared.Domain;

namespace Delify.Modules.Catalog.Domain;

public sealed class Product : Entity
{
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string? PhotoUrl { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public ICollection<Complement> Complements { get; private set; } = [];

    private Product() { }

    public static Product Create(Guid tenantId, Guid categoryId, string name, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        return new Product
        {
            TenantId = tenantId,
            CategoryId = categoryId,
            Name = name,
            Price = price
        };
    }

    public void Update(string name, string? description, decimal price, string? photoUrl, bool isAvailable)
    {
        Name = name;
        Description = description;
        Price = price;
        PhotoUrl = photoUrl;
        IsAvailable = isAvailable;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

`src/Delify.Modules.Catalog/Domain/Complement.cs`:
```csharp
using Delify.Shared.Domain;

namespace Delify.Modules.Catalog.Domain;

public sealed class Complement : Entity
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal AdditionalPrice { get; private set; }
    public bool IsAvailable { get; private set; } = true;

    private Complement() { }

    public static Complement Create(Guid tenantId, Guid productId, string name, decimal additionalPrice = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Complement
        {
            TenantId = tenantId,
            ProductId = productId,
            Name = name,
            AdditionalPrice = additionalPrice
        };
    }
}
```

- [ ] **Step 4: Create CatalogDbContext**

`src/Delify.Modules.Catalog/Infrastructure/CatalogDbContext.cs`:
```csharp
using Delify.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Catalog.Infrastructure;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Establishment> Establishments => Set<Establishment>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Complement> Complements => Set<Complement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");

        modelBuilder.Entity<Establishment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(100);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasMany(x => x.Categories).WithOne().HasForeignKey(c => c.EstablishmentId);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100);
            e.HasMany(x => x.Products).WithOne().HasForeignKey(p => p.CategoryId);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Price).HasColumnType("numeric(10,2)");
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasMany(x => x.Complements).WithOne().HasForeignKey(c => c.ProductId);
        });

        modelBuilder.Entity<Complement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AdditionalPrice).HasColumnType("numeric(10,2)");
            e.Property(x => x.Name).HasMaxLength(100);
        });
    }
}
```

- [ ] **Step 5: Create Endpoints**

`src/Delify.Modules.Catalog/Endpoints/ProductEndpoints.cs`:
```csharp
using Delify.Modules.Catalog.Domain;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Catalog.Endpoints;

internal static class ProductEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog");

        group.MapGet("/establishments/{slug}/menu", async (
            string slug,
            CatalogDbContext db) =>
        {
            var establishment = await db.Establishments
                .Include(e => e.Categories)
                    .ThenInclude(c => c.Products)
                        .ThenInclude(p => p.Complements)
                .FirstOrDefaultAsync(e => e.Slug == slug);

            return establishment is null
                ? Results.NotFound()
                : Results.Ok(establishment);
        })
        .WithName("GetMenu")
        .AllowAnonymous()
        .CacheOutput(p => p.Expire(TimeSpan.FromMinutes(5)).Tag("catalog"));

        group.MapPost("/establishments", async (
            CreateEstablishmentRequest req,
            ITenantContext tenant,
            CatalogDbContext db) =>
        {
            var establishment = Establishment.Create(tenant.TenantId, req.Slug, req.Name);
            db.Establishments.Add(establishment);
            await db.SaveChangesAsync();
            return Results.Created($"/api/catalog/establishments/{establishment.Slug}", establishment);
        })
        .WithName("CreateEstablishment")
        .RequireAuthorization();

        group.MapPost("/establishments/{id:guid}/categories", async (
            Guid id,
            CreateCategoryRequest req,
            ITenantContext tenant,
            CatalogDbContext db) =>
        {
            var category = Category.Create(tenant.TenantId, id, req.Name, req.Order);
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return Results.Created($"/api/catalog/categories/{category.Id}", category);
        })
        .WithName("CreateCategory")
        .RequireAuthorization();

        group.MapPost("/categories/{id:guid}/products", async (
            Guid id,
            CreateProductRequest req,
            ITenantContext tenant,
            CatalogDbContext db) =>
        {
            var product = Product.Create(tenant.TenantId, id, req.Name, req.Price);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/catalog/products/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .RequireAuthorization();

        return app;
    }

    private record CreateEstablishmentRequest(string Slug, string Name);
    private record CreateCategoryRequest(string Name, int Order = 0);
    private record CreateProductRequest(string Name, decimal Price, string? Description = null);
}
```

- [ ] **Step 6: Create CatalogModule**

`src/Delify.Modules.Catalog/CatalogModule.cs`:
```csharp
using Delify.Modules.Catalog.Endpoints;
using Delify.Modules.Catalog.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")));

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ProductEndpoints.Map(endpoints);
        return endpoints;
    }
}
```

- [ ] **Step 7: Build**

```bash
dotnet build src/Delify.Modules.Catalog/Delify.Modules.Catalog.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add src/Delify.Modules.Catalog/
git commit -m "feat(catalog): add Catalog module with Establishment, Category, Product, Complement"
```

---

## Task 5: Delify.Modules.Orders (CQRS + state machine)

**Files:**
- Create: `src/Delify.Modules.Orders/Delify.Modules.Orders.csproj`
- Create: `src/Delify.Modules.Orders/Domain/Order.cs`
- Create: `src/Delify.Modules.Orders/Domain/OrderItem.cs`
- Create: `src/Delify.Modules.Orders/Domain/OrderStatus.cs`
- Create: `src/Delify.Modules.Orders/Domain/Events/OrderCreatedEvent.cs`
- Create: `src/Delify.Modules.Orders/Application/Commands/CreateOrderCommand.cs`
- Create: `src/Delify.Modules.Orders/Application/Queries/GetOrdersByEstablishmentQuery.cs`
- Create: `src/Delify.Modules.Orders/Infrastructure/OrdersDbContext.cs`
- Create: `src/Delify.Modules.Orders/Endpoints/OrderEndpoints.cs`
- Create: `src/Delify.Modules.Orders/OrdersModule.cs`

- [ ] **Step 1: Create project**

```bash
dotnet new classlib -n Delify.Modules.Orders -o src/Delify.Modules.Orders -f net10.0
dotnet sln add src/Delify.Modules.Orders/Delify.Modules.Orders.csproj
rm src/Delify.Modules.Orders/Class1.cs
```

- [ ] **Step 2: Configure csproj**

`src/Delify.Modules.Orders/Delify.Modules.Orders.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" PrivateAssets="all" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
    <PackageReference Include="MassTransit.RabbitMQ" Version="8.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Delify.Shared\Delify.Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create Domain**

`src/Delify.Modules.Orders/Domain/OrderStatus.cs`:
```csharp
namespace Delify.Modules.Orders.Domain;

public enum OrderStatus
{
    PendingPayment = 0,
    AwaitingConfirmation = 1,
    InPreparation = 2,
    InDelivery = 3,
    Delivered = 4,
    Cancelled = 5
}
```

`src/Delify.Modules.Orders/Domain/OrderItem.cs`:
```csharp
using Delify.Shared.Domain;

namespace Delify.Modules.Orders.Domain;

public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Total => Quantity * UnitPrice;

    private OrderItem() { }

    internal static OrderItem Create(Guid tenantId, Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        return new OrderItem
        {
            TenantId = tenantId,
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
```

`src/Delify.Modules.Orders/Domain/Events/OrderCreatedEvent.cs`:
```csharp
using Delify.Shared.Domain;

namespace Delify.Modules.Orders.Domain.Events;

public sealed record OrderCreatedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid TenantId,
    decimal Total) : IDomainEvent;
```

`src/Delify.Modules.Orders/Domain/Order.cs`:
```csharp
using Delify.Modules.Orders.Domain.Events;
using Delify.Shared.Domain;

namespace Delify.Modules.Orders.Domain;

public sealed class Order : AggregateRoot
{
    public Guid EstablishmentId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.PendingPayment;
    public string? CustomerNote { get; private set; }
    public ICollection<OrderItem> Items { get; private set; } = [];
    public decimal Total => Items.Sum(i => i.Total);

    private Order() { }

    public static Order Create(Guid tenantId, Guid establishmentId, Guid? customerId = null)
    {
        var order = new Order
        {
            TenantId = tenantId,
            EstablishmentId = establishmentId,
            CustomerId = customerId
        };
        return order;
    }

    public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        var item = OrderItem.Create(TenantId, Id, productId, productName, quantity, unitPrice);
        Items.Add(item);
    }

    public void Confirm()
    {
        if (Status != OrderStatus.PendingPayment)
            throw new InvalidOperationException("Order must be in PendingPayment state to confirm.");
        Status = OrderStatus.AwaitingConfirmation;
        UpdatedAt = DateTimeOffset.UtcNow;
        Raise(new OrderCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, TenantId, Total));
    }

    public void Accept() => Transition(OrderStatus.AwaitingConfirmation, OrderStatus.InPreparation);
    public void StartDelivery() => Transition(OrderStatus.InPreparation, OrderStatus.InDelivery);
    public void Complete() => Transition(OrderStatus.InDelivery, OrderStatus.Delivered);
    public void Cancel() { Status = OrderStatus.Cancelled; UpdatedAt = DateTimeOffset.UtcNow; }

    private void Transition(OrderStatus from, OrderStatus to)
    {
        if (Status != from)
            throw new InvalidOperationException($"Cannot transition from {Status} to {to}. Expected {from}.");
        Status = to;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

- [ ] **Step 4: Create CQRS Commands/Queries**

`src/Delify.Modules.Orders/Application/Commands/CreateOrderCommand.cs`:
```csharp
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.Result;
using MediatR;

namespace Delify.Modules.Orders.Application.Commands;

public record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderCommand(
    Guid EstablishmentId,
    List<OrderItemDto> Items,
    string? CustomerNote = null) : IRequest<Result<Guid>>;

internal sealed class CreateOrderCommandHandler(
    OrdersDbContext db,
    ITenantContext tenant) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            return Result.Failure<Guid>(Error.Validation("Order must have at least one item."));

        var order = Order.Create(tenant.TenantId, request.EstablishmentId);

        foreach (var item in request.Items)
            order.AddItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);

        order.CustomerNote = request.CustomerNote;

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
```

`src/Delify.Modules.Orders/Application/Queries/GetOrdersByEstablishmentQuery.cs`:
```csharp
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using Delify.Shared.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Application.Queries;

public record GetOrdersByEstablishmentQuery(Guid EstablishmentId, int Page = 1, int PageSize = 20)
    : IRequest<Result<List<Order>>>;

internal sealed class GetOrdersByEstablishmentQueryHandler(
    OrdersDbContext db,
    ITenantContext tenant) : IRequestHandler<GetOrdersByEstablishmentQuery, Result<List<Order>>>
{
    public async Task<Result<List<Order>>> Handle(GetOrdersByEstablishmentQuery request, CancellationToken cancellationToken)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.TenantId == tenant.TenantId && o.EstablishmentId == request.EstablishmentId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(orders);
    }
}
```

- [ ] **Step 5: Fix Order.CustomerNote setter visibility**

In `Order.cs`, add public setter for `CustomerNote` (it's set externally in the handler):
```csharp
public string? CustomerNote { get; internal set; }
```

- [ ] **Step 6: Create OrdersDbContext**

`src/Delify.Modules.Orders/Infrastructure/OrdersDbContext.cs`:
```csharp
using Delify.Modules.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Orders.Infrastructure;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasConversion<string>();
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
            e.Ignore(o => o.DomainEvents);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasColumnType("numeric(10,2)");
            e.Ignore(i => i.Total);
        });
    }
}
```

- [ ] **Step 7: Create OrderEndpoints**

`src/Delify.Modules.Orders/Endpoints/OrderEndpoints.cs`:
```csharp
using Delify.Modules.Orders.Application.Commands;
using Delify.Modules.Orders.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Orders.Endpoints;

internal static class OrderEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders").RequireAuthorization();

        group.MapPost("/", async (CreateOrderCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/orders/{result.Value}", new { id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithName("CreateOrder");

        group.MapGet("/establishments/{establishmentId:guid}", async (
            Guid establishmentId,
            IMediator mediator,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await mediator.Send(new GetOrdersByEstablishmentQuery(establishmentId, page, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetOrdersByEstablishment");

        return app;
    }
}
```

- [ ] **Step 8: Create OrdersModule**

`src/Delify.Modules.Orders/OrdersModule.cs`:
```csharp
using Delify.Modules.Orders.Endpoints;
using Delify.Modules.Orders.Infrastructure;
using Delify.Shared.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Orders;

public sealed class OrdersModule : IModule
{
    public string Name => "Orders";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrdersDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "orders")));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(OrdersModule).Assembly));

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        OrderEndpoints.Map(endpoints);
        return endpoints;
    }
}
```

- [ ] **Step 9: Build**

```bash
dotnet build src/Delify.Modules.Orders/Delify.Modules.Orders.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 10: Commit**

```bash
git add src/Delify.Modules.Orders/
git commit -m "feat(orders): add Orders module with CQRS (MediatR), state machine, domain events"
```

---

## Task 6: Delify.Modules.Payments (Asaas adapter)

**Files:**
- Create: `src/Delify.Modules.Payments/Delify.Modules.Payments.csproj`
- Create: `src/Delify.Modules.Payments/Abstractions/IPaymentGateway.cs`
- Create: `src/Delify.Modules.Payments/Domain/Payment.cs`
- Create: `src/Delify.Modules.Payments/Domain/PaymentStatus.cs`
- Create: `src/Delify.Modules.Payments/Gateways/AsaasPaymentGateway.cs`
- Create: `src/Delify.Modules.Payments/Infrastructure/PaymentsDbContext.cs`
- Create: `src/Delify.Modules.Payments/Endpoints/CheckoutEndpoint.cs`
- Create: `src/Delify.Modules.Payments/Endpoints/WebhookEndpoint.cs`
- Create: `src/Delify.Modules.Payments/PaymentsModule.cs`

- [ ] **Step 1: Create project**

```bash
dotnet new classlib -n Delify.Modules.Payments -o src/Delify.Modules.Payments -f net10.0
dotnet sln add src/Delify.Modules.Payments/Delify.Modules.Payments.csproj
rm src/Delify.Modules.Payments/Class1.cs
```

- [ ] **Step 2: Configure csproj**

`src/Delify.Modules.Payments/Delify.Modules.Payments.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" PrivateAssets="all" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Delify.Shared\Delify.Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create abstractions and domain**

`src/Delify.Modules.Payments/Domain/PaymentStatus.cs`:
```csharp
namespace Delify.Modules.Payments.Domain;

public enum PaymentStatus { Pending, Confirmed, Failed, Refunded }
```

`src/Delify.Modules.Payments/Domain/Payment.cs`:
```csharp
using Delify.Shared.Domain;

namespace Delify.Modules.Payments.Domain;

public sealed class Payment : Entity
{
    public Guid OrderId { get; private set; }
    public string Gateway { get; private set; } = string.Empty;
    public string? GatewayPaymentId { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public decimal Amount { get; private set; }
    public string Method { get; private set; } = string.Empty;
    public string? PixQrCode { get; private set; }
    public string? PixCopyPaste { get; private set; }

    private Payment() { }

    public static Payment CreatePix(Guid tenantId, Guid orderId, decimal amount)
    {
        return new Payment
        {
            TenantId = tenantId,
            OrderId = orderId,
            Amount = amount,
            Gateway = "Asaas",
            Method = "PIX"
        };
    }

    public void ConfirmPayment(string gatewayPaymentId)
    {
        GatewayPaymentId = gatewayPaymentId;
        Status = PaymentStatus.Confirmed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPixData(string gatewayId, string qrCode, string copyPaste)
    {
        GatewayPaymentId = gatewayId;
        PixQrCode = qrCode;
        PixCopyPaste = copyPaste;
    }
}
```

`src/Delify.Modules.Payments/Abstractions/IPaymentGateway.cs`:
```csharp
namespace Delify.Modules.Payments.Abstractions;

public record PixPaymentRequest(Guid OrderId, decimal Amount, string CustomerCpf, string CustomerName);
public record PixPaymentResult(string GatewayId, string QrCode, string CopyPaste, DateTimeOffset ExpiresAt);
public record WebhookResult(string GatewayPaymentId, bool IsConfirmed);

public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PixPaymentResult> CreatePixAsync(PixPaymentRequest request, CancellationToken ct = default);
    Task<WebhookResult> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create Asaas Gateway**

`src/Delify.Modules.Payments/Gateways/AsaasPaymentGateway.cs`:
```csharp
using Delify.Modules.Payments.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Delify.Modules.Payments.Gateways;

internal sealed class AsaasPaymentGateway(HttpClient httpClient, IConfiguration config) : IPaymentGateway
{
    public string GatewayName => "Asaas";

    public async Task<PixPaymentResult> CreatePixAsync(PixPaymentRequest request, CancellationToken ct = default)
    {
        // Asaas REST API: POST /api/v3/payments
        // Docs: https://asaasv3.docs.apiary.io/#reference/0/cobrancas
        var body = new
        {
            customer = request.CustomerCpf,
            billingType = "PIX",
            value = request.Amount,
            dueDate = DateTimeOffset.UtcNow.AddHours(24).ToString("yyyy-MM-dd"),
            externalReference = request.OrderId.ToString(),
            description = $"Pedido #{request.OrderId}"
        };

        var response = await httpClient.PostAsJsonAsync("/api/v3/payments", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var paymentId = json.GetProperty("id").GetString()!;

        // Get PIX QR Code
        var pixResponse = await httpClient.GetFromJsonAsync<JsonElement>(
            $"/api/v3/payments/{paymentId}/pixQrCode", ct);

        return new PixPaymentResult(
            GatewayId: paymentId,
            QrCode: pixResponse.GetProperty("encodedImage").GetString()!,
            CopyPaste: pixResponse.GetProperty("payload").GetString()!,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(24));
    }

    public Task<WebhookResult> ProcessWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        // Asaas sends webhook events for payment confirmation
        // Validate signature header "asaas-access-token" against config["Asaas:WebhookToken"]
        var webhookToken = config["Asaas:WebhookToken"];
        if (signature != webhookToken)
            return Task.FromResult(new WebhookResult(string.Empty, false));

        var json = JsonDocument.Parse(payload);
        var eventType = json.RootElement.GetProperty("event").GetString();
        var paymentId = json.RootElement.GetProperty("payment").GetProperty("id").GetString()!;

        return Task.FromResult(new WebhookResult(paymentId, eventType == "PAYMENT_CONFIRMED"));
    }
}
```

- [ ] **Step 5: Create PaymentsDbContext and Endpoints**

`src/Delify.Modules.Payments/Infrastructure/PaymentsDbContext.cs`:
```csharp
using Delify.Modules.Payments.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Payments.Infrastructure;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.OrderId);
            e.Property(p => p.Amount).HasColumnType("numeric(10,2)");
            e.Property(p => p.Status).HasConversion<string>();
        });
    }
}
```

`src/Delify.Modules.Payments/Endpoints/CheckoutEndpoint.cs`:
```csharp
using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Payments.Endpoints;

internal static class CheckoutEndpoint
{
    private record PixCheckoutRequest(Guid OrderId, decimal Amount, string CustomerCpf, string CustomerName);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/payments/pix", async (
            PixCheckoutRequest req,
            IPaymentGateway gateway,
            PaymentsDbContext db,
            ITenantContext tenant) =>
        {
            var pixResult = await gateway.CreatePixAsync(
                new PixPaymentRequest(req.OrderId, req.Amount, req.CustomerCpf, req.CustomerName));

            var payment = Payment.CreatePix(tenant.TenantId, req.OrderId, req.Amount);
            payment.SetPixData(pixResult.GatewayId, pixResult.QrCode, pixResult.CopyPaste);
            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            return Results.Ok(new { pixResult.QrCode, pixResult.CopyPaste, pixResult.ExpiresAt });
        })
        .WithName("CreatePixPayment")
        .WithTags("Payments")
        .RequireAuthorization();

        return app;
    }
}
```

`src/Delify.Modules.Payments/Endpoints/WebhookEndpoint.cs`:
```csharp
using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Payments.Endpoints;

internal static class WebhookEndpoint
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/asaas", async (
            HttpRequest req,
            IPaymentGateway gateway,
            PaymentsDbContext db) =>
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var signature = req.Headers["asaas-access-token"].ToString();

            var result = await gateway.ProcessWebhookAsync(body, signature);

            if (!result.IsConfirmed) return Results.Ok(); // idempotent — always 200 to Asaas

            var payment = await db.Payments
                .FirstOrDefaultAsync(p => p.GatewayPaymentId == result.GatewayPaymentId);

            if (payment is not null)
            {
                payment.ConfirmPayment(result.GatewayPaymentId);
                await db.SaveChangesAsync();
            }

            return Results.Ok();
        })
        .WithName("AsaasWebhook")
        .WithTags("Webhooks")
        .AllowAnonymous();

        return app;
    }
}
```

- [ ] **Step 6: Create PaymentsModule**

`src/Delify.Modules.Payments/PaymentsModule.cs`:
```csharp
using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Endpoints;
using Delify.Modules.Payments.Gateways;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Payments;

public sealed class PaymentsModule : IModule
{
    public string Name => "Payments";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "payments")));

        services.AddHttpClient<IPaymentGateway, AsaasPaymentGateway>(client =>
        {
            client.BaseAddress = new Uri(configuration["Asaas:BaseUrl"]
                ?? "https://sandbox.asaas.com");
            client.DefaultRequestHeaders.Add("access_token",
                configuration["Asaas:ApiKey"] ?? string.Empty);
        });

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        CheckoutEndpoint.Map(endpoints);
        WebhookEndpoint.Map(endpoints);
        return endpoints;
    }
}
```

- [ ] **Step 7: Build**

```bash
dotnet build src/Delify.Modules.Payments/Delify.Modules.Payments.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add src/Delify.Modules.Payments/
git commit -m "feat(payments): add Payments module with Asaas PIX gateway, webhook handler"
```

---

## Task 7: Delify.Modules.Delivery (Lalamove + Borzo strategy)

**Files:**
- Create: `src/Delify.Modules.Delivery/Delify.Modules.Delivery.csproj`
- Create: `src/Delify.Modules.Delivery/Abstractions/IDeliveryProvider.cs`
- Create: `src/Delify.Modules.Delivery/Domain/DeliveryOrder.cs`
- Create: `src/Delify.Modules.Delivery/Domain/DeliveryStatus.cs`
- Create: `src/Delify.Modules.Delivery/Providers/LalamoveDeliveryProvider.cs`
- Create: `src/Delify.Modules.Delivery/Providers/BorzoDeliveryProvider.cs`
- Create: `src/Delify.Modules.Delivery/Infrastructure/DeliveryDbContext.cs`
- Create: `src/Delify.Modules.Delivery/Endpoints/DeliveryEndpoints.cs`
- Create: `src/Delify.Modules.Delivery/DeliveryModule.cs`

- [ ] **Step 1: Create project**

```bash
dotnet new classlib -n Delify.Modules.Delivery -o src/Delify.Modules.Delivery -f net10.0
dotnet sln add src/Delify.Modules.Delivery/Delify.Modules.Delivery.csproj
rm src/Delify.Modules.Delivery/Class1.cs
```

- [ ] **Step 2: Configure csproj**

`src/Delify.Modules.Delivery/Delify.Modules.Delivery.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" PrivateAssets="all" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Delify.Shared\Delify.Shared.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create abstractions and domain**

`src/Delify.Modules.Delivery/Domain/DeliveryStatus.cs`:
```csharp
namespace Delify.Modules.Delivery.Domain;

public enum DeliveryStatus { Quoted, Dispatched, PickedUp, InTransit, Delivered, Cancelled }
```

`src/Delify.Modules.Delivery/Domain/DeliveryOrder.cs`:
```csharp
using Delify.Shared.Domain;

namespace Delify.Modules.Delivery.Domain;

public sealed class DeliveryOrder : Entity
{
    public Guid OrderId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderOrderId { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Quoted;
    public decimal QuotedPrice { get; private set; }
    public string PickupAddress { get; private set; } = string.Empty;
    public string DropoffAddress { get; private set; } = string.Empty;
    public string? TrackingUrl { get; private set; }

    private DeliveryOrder() { }

    public static DeliveryOrder Create(Guid tenantId, Guid orderId, string provider,
        string pickupAddress, string dropoffAddress, decimal quotedPrice)
    {
        return new DeliveryOrder
        {
            TenantId = tenantId,
            OrderId = orderId,
            Provider = provider,
            PickupAddress = pickupAddress,
            DropoffAddress = dropoffAddress,
            QuotedPrice = quotedPrice
        };
    }

    public void Dispatch(string providerOrderId, string? trackingUrl)
    {
        ProviderOrderId = providerOrderId;
        TrackingUrl = trackingUrl;
        Status = DeliveryStatus.Dispatched;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

`src/Delify.Modules.Delivery/Abstractions/IDeliveryProvider.cs`:
```csharp
namespace Delify.Modules.Delivery.Abstractions;

public record Address(string Street, string Number, string City, double Latitude, double Longitude);
public record DeliveryQuote(string Provider, decimal Price, int EstimatedMinutes);
public record DispatchResult(string ProviderOrderId, string? TrackingUrl);

public interface IDeliveryProvider
{
    string ProviderName { get; }
    Task<DeliveryQuote> GetQuoteAsync(Address pickup, Address dropoff, CancellationToken ct = default);
    Task<DispatchResult> DispatchAsync(Address pickup, Address dropoff, string contactPhone, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create Providers**

`src/Delify.Modules.Delivery/Providers/LalamoveDeliveryProvider.cs`:
```csharp
using Delify.Modules.Delivery.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Delify.Modules.Delivery.Providers;

internal sealed class LalamoveDeliveryProvider(HttpClient httpClient, IConfiguration config) : IDeliveryProvider
{
    public string ProviderName => "Lalamove";

    public async Task<DeliveryQuote> GetQuoteAsync(Address pickup, Address dropoff, CancellationToken ct = default)
    {
        // Lalamove REST API v3: POST /v3/quotations
        // Docs: https://developers.lalamove.com/#create-quotation
        var body = new
        {
            data = new
            {
                serviceType = "MOTORCYCLE",
                language = "pt_BR",
                stops = new[]
                {
                    new { coordinates = new { lat = pickup.Latitude.ToString(), lng = pickup.Longitude.ToString() }, address = $"{pickup.Street}, {pickup.Number}" },
                    new { coordinates = new { lat = dropoff.Latitude.ToString(), lng = dropoff.Longitude.ToString() }, address = $"{dropoff.Street}, {dropoff.Number}" }
                }
            }
        };

        var response = await httpClient.PostAsJsonAsync("/v3/quotations", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var price = decimal.Parse(json.GetProperty("data").GetProperty("priceBreakdown").GetProperty("total").GetString()!);
        var minutes = json.GetProperty("data").GetProperty("estimatedTimeline").GetProperty("pickup").GetInt32();

        return new DeliveryQuote(ProviderName, price / 100, minutes);
    }

    public async Task<DispatchResult> DispatchAsync(Address pickup, Address dropoff, string contactPhone, CancellationToken ct = default)
    {
        // Lalamove REST API v3: POST /v3/orders
        var body = new
        {
            data = new
            {
                serviceType = "MOTORCYCLE",
                language = "pt_BR",
                stops = new[]
                {
                    new
                    {
                        coordinates = new { lat = pickup.Latitude.ToString(), lng = pickup.Longitude.ToString() },
                        address = $"{pickup.Street}, {pickup.Number}",
                        contact = new { name = "Estabelecimento", phone = contactPhone }
                    },
                    new
                    {
                        coordinates = new { lat = dropoff.Latitude.ToString(), lng = dropoff.Longitude.ToString() },
                        address = $"{dropoff.Street}, {dropoff.Number}",
                        contact = new { name = "Cliente", phone = contactPhone }
                    }
                }
            }
        };

        var response = await httpClient.PostAsJsonAsync("/v3/orders", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var orderId = json.GetProperty("data").GetProperty("orderId").GetString()!;

        return new DispatchResult(orderId, TrackingUrl: null);
    }
}
```

`src/Delify.Modules.Delivery/Providers/BorzoDeliveryProvider.cs`:
```csharp
using Delify.Modules.Delivery.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace Delify.Modules.Delivery.Providers;

internal sealed class BorzoDeliveryProvider(HttpClient httpClient, IConfiguration config) : IDeliveryProvider
{
    public string ProviderName => "Borzo";

    public async Task<DeliveryQuote> GetQuoteAsync(Address pickup, Address dropoff, CancellationToken ct = default)
    {
        // Borzo REST API: POST /calculate-order
        // Docs: https://api.borzodelivery.com/api/business/1.1/calculate-order
        var body = new
        {
            matter = "Pedido delivery",
            vehicle_type_id = 8, // motorcycle
            points = new[]
            {
                new { address = $"{pickup.Street}, {pickup.Number}", latitude = pickup.Latitude, longitude = pickup.Longitude },
                new { address = $"{dropoff.Street}, {dropoff.Number}", latitude = dropoff.Latitude, longitude = dropoff.Longitude }
            }
        };

        var response = await httpClient.PostAsJsonAsync("/api/business/1.1/calculate-order", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var price = json.GetProperty("order").GetProperty("payment_amount").GetDecimal();
        var minutes = json.GetProperty("order").GetProperty("delivery_fee_amount").GetInt32();

        return new DeliveryQuote(ProviderName, price, minutes);
    }

    public async Task<DispatchResult> DispatchAsync(Address pickup, Address dropoff, string contactPhone, CancellationToken ct = default)
    {
        var body = new
        {
            matter = "Pedido delivery",
            vehicle_type_id = 8,
            points = new[]
            {
                new { address = $"{pickup.Street}, {pickup.Number}", latitude = pickup.Latitude, longitude = pickup.Longitude, contact_person = new { name = "Estabelecimento", phone = contactPhone } },
                new { address = $"{dropoff.Street}, {dropoff.Number}", latitude = dropoff.Latitude, longitude = dropoff.Longitude, contact_person = new { name = "Cliente", phone = contactPhone } }
            }
        };

        var response = await httpClient.PostAsJsonAsync("/api/business/1.1/create-order", body, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var orderId = json.GetProperty("order").GetProperty("id").GetInt64().ToString();

        return new DispatchResult(orderId, TrackingUrl: null);
    }
}
```

- [ ] **Step 5: Create DbContext and Endpoints**

`src/Delify.Modules.Delivery/Infrastructure/DeliveryDbContext.cs`:
```csharp
using Delify.Modules.Delivery.Domain;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Delivery.Infrastructure;

public sealed class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : DbContext(options)
{
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("delivery");
        modelBuilder.Entity<DeliveryOrder>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.OrderId);
            e.Property(d => d.QuotedPrice).HasColumnType("numeric(10,2)");
            e.Property(d => d.Status).HasConversion<string>();
        });
    }
}
```

`src/Delify.Modules.Delivery/Endpoints/DeliveryEndpoints.cs`:
```csharp
using Delify.Modules.Delivery.Abstractions;
using Delify.Modules.Delivery.Domain;
using Delify.Modules.Delivery.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Delify.Modules.Delivery.Endpoints;

internal static class DeliveryEndpoints
{
    private record QuoteRequest(Guid OrderId, Address Pickup, Address Dropoff, string ContactPhone);

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery").WithTags("Delivery").RequireAuthorization();

        // Returns parallel quotes from all providers
        group.MapPost("/quote", async (
            QuoteRequest req,
            IEnumerable<IDeliveryProvider> providers) =>
        {
            var quotesTasks = providers.Select(p => p.GetQuoteAsync(req.Pickup, req.Dropoff));
            var quotes = await Task.WhenAll(quotesTasks);
            return Results.Ok(quotes.OrderBy(q => q.Price));
        })
        .WithName("QuoteDelivery");

        // Dispatches with chosen provider
        group.MapPost("/dispatch", async (
            DispatchRequest req,
            IEnumerable<IDeliveryProvider> providers,
            DeliveryDbContext db,
            ITenantContext tenant) =>
        {
            var provider = providers.FirstOrDefault(p => p.ProviderName == req.Provider);
            if (provider is null)
                return Results.BadRequest($"Provider '{req.Provider}' not available.");

            var dispatchResult = await provider.DispatchAsync(req.Pickup, req.Dropoff, req.ContactPhone);

            var deliveryOrder = DeliveryOrder.Create(
                tenant.TenantId, req.OrderId, req.Provider,
                $"{req.Pickup.Street}, {req.Pickup.Number}",
                $"{req.Dropoff.Street}, {req.Dropoff.Number}",
                req.QuotedPrice);

            deliveryOrder.Dispatch(dispatchResult.ProviderOrderId, dispatchResult.TrackingUrl);
            db.DeliveryOrders.Add(deliveryOrder);
            await db.SaveChangesAsync();

            return Results.Ok(new { dispatchResult.ProviderOrderId, dispatchResult.TrackingUrl });
        })
        .WithName("DispatchDelivery");

        return app;
    }

    private record DispatchRequest(Guid OrderId, string Provider, Address Pickup, Address Dropoff,
        string ContactPhone, decimal QuotedPrice);
}
```

- [ ] **Step 6: Create DeliveryModule**

`src/Delify.Modules.Delivery/DeliveryModule.cs`:
```csharp
using Delify.Modules.Delivery.Abstractions;
using Delify.Modules.Delivery.Endpoints;
using Delify.Modules.Delivery.Infrastructure;
using Delify.Modules.Delivery.Providers;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Delify.Modules.Delivery;

public sealed class DeliveryModule : IModule
{
    public string Name => "Delivery";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DeliveryDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Delify"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "delivery")));

        services.AddHttpClient<LalamoveDeliveryProvider>(client =>
        {
            client.BaseAddress = new Uri(configuration["Lalamove:BaseUrl"] ?? "https://rest.lalamove.com");
            client.DefaultRequestHeaders.Add("Authorization", $"hmac {configuration["Lalamove:ApiKey"]}");
        });

        services.AddHttpClient<BorzoDeliveryProvider>(client =>
        {
            client.BaseAddress = new Uri(configuration["Borzo:BaseUrl"] ?? "https://api.borzodelivery.com");
            client.DefaultRequestHeaders.Add("X-DV-Auth-Token", configuration["Borzo:ApiKey"] ?? string.Empty);
        });

        services.AddScoped<IDeliveryProvider, LalamoveDeliveryProvider>();
        services.AddScoped<IDeliveryProvider, BorzoDeliveryProvider>();

        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        DeliveryEndpoints.Map(endpoints);
        return endpoints;
    }
}
```

- [ ] **Step 7: Build**

```bash
dotnet build src/Delify.Modules.Delivery/Delify.Modules.Delivery.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add src/Delify.Modules.Delivery/
git commit -m "feat(delivery): add Delivery module with Lalamove + Borzo strategy, parallel quote"
```

---

## Task 8: Delify.Api — entry point, module wiring, Swagger

**Files:**
- Create: `src/Delify.Api/Delify.Api.csproj`
- Create: `src/Delify.Api/Program.cs`
- Create: `src/Delify.Api/appsettings.json`
- Create: `src/Delify.Api/appsettings.Development.json`

- [ ] **Step 1: Create API project**

```bash
dotnet new webapi -n Delify.Api -o src/Delify.Api -f net10.0 --use-minimal-apis
dotnet sln add src/Delify.Api/Delify.Api.csproj
```

- [ ] **Step 2: Configure csproj**

`src/Delify.Api/Delify.Api.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Scalar.AspNetCore" Version="2.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.*" PrivateAssets="all" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Delify.Shared\Delify.Shared.csproj" />
    <ProjectReference Include="..\Delify.Modules.Identity\Delify.Modules.Identity.csproj" />
    <ProjectReference Include="..\Delify.Modules.Catalog\Delify.Modules.Catalog.csproj" />
    <ProjectReference Include="..\Delify.Modules.Orders\Delify.Modules.Orders.csproj" />
    <ProjectReference Include="..\Delify.Modules.Payments\Delify.Modules.Payments.csproj" />
    <ProjectReference Include="..\Delify.Modules.Delivery\Delify.Modules.Delivery.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create appsettings.json**

`src/Delify.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Delify": "Host=localhost;Port=5432;Database=delify;Username=delify;Password=delify"
  },
  "Jwt": {
    "Key": "CHANGE_ME_IN_PRODUCTION_MIN_32_CHARS!!",
    "Issuer": "delify-api",
    "Audience": "delify-clients"
  },
  "Asaas": {
    "BaseUrl": "https://sandbox.asaas.com",
    "ApiKey": "",
    "WebhookToken": ""
  },
  "Lalamove": {
    "BaseUrl": "https://rest.lalamove.com",
    "ApiKey": ""
  },
  "Borzo": {
    "BaseUrl": "https://api.borzodelivery.com",
    "ApiKey": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`src/Delify.Api/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Delify": "Host=localhost;Port=5432;Database=delify;Username=delify;Password=delify"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

- [ ] **Step 4: Create Program.cs**

`src/Delify.Api/Program.cs`:
```csharp
using Delify.Modules.Catalog;
using Delify.Modules.Delivery;
using Delify.Modules.Identity;
using Delify.Modules.Orders;
using Delify.Modules.Payments;
using Delify.Shared.Abstractions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register all modules
var modules = new List<IModule>
{
    new IdentityModule(),
    new CatalogModule(),
    new OrdersModule(),
    new PaymentsModule(),
    new DeliveryModule()
};

foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddOutputCache();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// Map all module endpoints
foreach (var module in modules)
    module.MapEndpoints(app);

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithTags("Health")
   .AllowAnonymous();

app.Run();

public partial class Program { }
```

- [ ] **Step 5: Build full solution**

```bash
dotnet build Delify.sln
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/Delify.Api/
git commit -m "feat(api): add API entry point with module wiring, Scalar OpenAPI, health endpoint"
```

---

## Task 9: Test projects

**Files:**
- Create: `tests/Delify.Modules.Catalog.Tests/Delify.Modules.Catalog.Tests.csproj`
- Create: `tests/Delify.Modules.Catalog.Tests/Domain/ProductTests.cs`
- Create: `tests/Delify.Modules.Orders.Tests/Delify.Modules.Orders.Tests.csproj`
- Create: `tests/Delify.Modules.Orders.Tests/Domain/OrderTests.cs`

- [ ] **Step 1: Create test projects**

```bash
dotnet new xunit -n Delify.Modules.Catalog.Tests -o tests/Delify.Modules.Catalog.Tests -f net10.0
dotnet new xunit -n Delify.Modules.Orders.Tests -o tests/Delify.Modules.Orders.Tests -f net10.0
dotnet sln add tests/Delify.Modules.Catalog.Tests/Delify.Modules.Catalog.Tests.csproj
dotnet sln add tests/Delify.Modules.Orders.Tests/Delify.Modules.Orders.Tests.csproj
```

- [ ] **Step 2: Configure test csproj files**

`tests/Delify.Modules.Catalog.Tests/Delify.Modules.Catalog.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" PrivateAssets="all" />
    <PackageReference Include="FluentAssertions" Version="7.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Delify.Modules.Catalog\Delify.Modules.Catalog.csproj" />
  </ItemGroup>
</Project>
```

`tests/Delify.Modules.Orders.Tests/Delify.Modules.Orders.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" PrivateAssets="all" />
    <PackageReference Include="FluentAssertions" Version="7.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Delify.Modules.Orders\Delify.Modules.Orders.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Write Catalog domain tests**

`tests/Delify.Modules.Catalog.Tests/Domain/ProductTests.cs`:
```csharp
using Delify.Modules.Catalog.Domain;
using FluentAssertions;

namespace Delify.Modules.Catalog.Tests.Domain;

public class ProductTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var product = Product.Create(TenantId, CategoryId, "X-Burguer", 29.90m);

        product.Name.Should().Be("X-Burguer");
        product.Price.Should().Be(29.90m);
        product.TenantId.Should().Be(TenantId);
        product.CategoryId.Should().Be(CategoryId);
        product.IsAvailable.Should().BeTrue();
        product.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => Product.Create(TenantId, CategoryId, "", 29.90m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativePrice_ThrowsArgumentOutOfRangeException()
    {
        var act = () => Product.Create(TenantId, CategoryId, "Produto", -1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_ChangesNameAndPrice()
    {
        var product = Product.Create(TenantId, CategoryId, "X-Burguer", 29.90m);
        product.Update("X-Burguer Duplo", "Com bacon", 45.00m, null, true);

        product.Name.Should().Be("X-Burguer Duplo");
        product.Price.Should().Be(45.00m);
        product.UpdatedAt.Should().NotBeNull();
    }
}
```

- [ ] **Step 4: Write Orders domain tests**

`tests/Delify.Modules.Orders.Tests/Domain/OrderTests.cs`:
```csharp
using Delify.Modules.Orders.Domain;
using FluentAssertions;

namespace Delify.Modules.Orders.Tests.Domain;

public class OrderTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid EstablishmentId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();

    [Fact]
    public void Create_NewOrder_HasPendingPaymentStatus()
    {
        var order = Order.Create(TenantId, EstablishmentId);

        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.Items.Should().BeEmpty();
        order.Total.Should().Be(0);
    }

    [Fact]
    public void AddItem_IncreasesTotalCorrectly()
    {
        var order = Order.Create(TenantId, EstablishmentId);
        order.AddItem(ProductId, "X-Burguer", 2, 29.90m);

        order.Items.Should().HaveCount(1);
        order.Total.Should().Be(59.80m);
    }

    [Fact]
    public void Confirm_TransitionsToAwaitingConfirmation()
    {
        var order = Order.Create(TenantId, EstablishmentId);
        order.AddItem(ProductId, "X-Burguer", 1, 29.90m);
        order.Confirm();

        order.Status.Should().Be(OrderStatus.AwaitingConfirmation);
    }

    [Fact]
    public void Confirm_RaisesOrderCreatedEvent()
    {
        var order = Order.Create(TenantId, EstablishmentId);
        order.AddItem(ProductId, "X-Burguer", 1, 29.90m);
        order.Confirm();

        order.DomainEvents.Should().HaveCount(1);
        order.DomainEvents[0].Should().BeOfType<Delify.Modules.Orders.Domain.Events.OrderCreatedEvent>();
    }

    [Fact]
    public void Accept_FromWrongState_ThrowsInvalidOperationException()
    {
        var order = Order.Create(TenantId, EstablishmentId);

        var act = () => order.Accept(); // still PendingPayment
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FullHappyPath_TransitionsCorrectly()
    {
        var order = Order.Create(TenantId, EstablishmentId);
        order.AddItem(ProductId, "X-Burguer", 1, 29.90m);

        order.Confirm();
        order.Status.Should().Be(OrderStatus.AwaitingConfirmation);

        order.Accept();
        order.Status.Should().Be(OrderStatus.InPreparation);

        order.StartDelivery();
        order.Status.Should().Be(OrderStatus.InDelivery);

        order.Complete();
        order.Status.Should().Be(OrderStatus.Delivered);
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test Delify.sln
```

Expected:
```
Passed! - Failed: 0, Passed: 9, Skipped: 0
```

- [ ] **Step 6: Commit**

```bash
git add tests/
git commit -m "test: add domain unit tests for Catalog and Orders modules"
```

---

## Task 10: Docker Compose for local development

**Files:**
- Create: `docker-compose.yml`
- Create: `docker-compose.override.yml`

- [ ] **Step 1: Create docker-compose.yml**

`docker-compose.yml`:
```yaml
version: '3.9'

services:
  postgres:
    image: postgres:16-alpine
    container_name: delify-postgres
    environment:
      POSTGRES_USER: delify
      POSTGRES_PASSWORD: delify
      POSTGRES_DB: delify
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U delify"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: delify-redis
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5

  rabbitmq:
    image: rabbitmq:3-management-alpine
    container_name: delify-rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: delify
      RABBITMQ_DEFAULT_PASS: delify
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 30s
      timeout: 10s
      retries: 5

volumes:
  postgres_data:
  redis_data:
  rabbitmq_data:
```

`docker-compose.override.yml`:
```yaml
version: '3.9'

services:
  delify-api:
    build:
      context: .
      dockerfile: src/Delify.Api/Dockerfile
    container_name: delify-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Delify=Host=postgres;Port=5432;Database=delify;Username=delify;Password=delify
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
```

- [ ] **Step 2: Create Dockerfile for API**

`src/Delify.Api/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Delify.Api/Delify.Api.csproj", "src/Delify.Api/"]
COPY ["src/Delify.Shared/Delify.Shared.csproj", "src/Delify.Shared/"]
COPY ["src/Delify.Modules.Identity/Delify.Modules.Identity.csproj", "src/Delify.Modules.Identity/"]
COPY ["src/Delify.Modules.Catalog/Delify.Modules.Catalog.csproj", "src/Delify.Modules.Catalog/"]
COPY ["src/Delify.Modules.Orders/Delify.Modules.Orders.csproj", "src/Delify.Modules.Orders/"]
COPY ["src/Delify.Modules.Payments/Delify.Modules.Payments.csproj", "src/Delify.Modules.Payments/"]
COPY ["src/Delify.Modules.Delivery/Delify.Modules.Delivery.csproj", "src/Delify.Modules.Delivery/"]

RUN dotnet restore "src/Delify.Api/Delify.Api.csproj"

COPY . .
RUN dotnet publish "src/Delify.Api/Delify.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Delify.Api.dll"]
```

- [ ] **Step 3: Start infrastructure and verify**

```bash
docker compose up postgres redis rabbitmq -d
docker compose ps
```

Expected: all three services `healthy`.

- [ ] **Step 4: Commit**

```bash
git add docker-compose.yml docker-compose.override.yml src/Delify.Api/Dockerfile
git commit -m "infra: add Docker Compose for postgres, redis, rabbitmq + API Dockerfile"
```

---

## Task 11: GitHub Actions CI pipeline

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Create CI workflow**

`.github/workflows/ci.yml`:
```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: delify
          POSTGRES_PASSWORD: delify
          POSTGRES_DB: delify
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore Delify.sln

      - name: Build
        run: dotnet build Delify.sln --no-restore -c Release

      - name: Test
        run: dotnet test Delify.sln --no-build -c Release --logger "trx;LogFileName=test-results.trx" --verbosity normal

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: "**/*.trx"
```

- [ ] **Step 2: Commit**

```bash
git add .github/
git commit -m "ci: add GitHub Actions pipeline (build + test)"
```

---

## Task 12: Push to GitHub

- [ ] **Step 1: Verify remote**

```bash
git remote -v
```

Expected: `origin  https://github.com/JoseDuDev/deliverytec-jel.git (fetch/push)`

- [ ] **Step 2: Push**

```bash
git push -u origin main
```

Expected: branch `main` pushed, GitHub Actions CI triggered.

- [ ] **Step 3: Verify on GitHub**

Open: `https://github.com/JoseDuDev/deliverytec-jel.git`

Check:
- All source files visible
- Actions tab shows CI run in progress/passed
- Solution structure matches the File Map at the top of this plan

---

## Self-Review

**Spec coverage:**
- ✅ Solution `Delify.sln` with all 7 source projects + 2 test projects
- ✅ `Delify.Shared` — Entity, AggregateRoot, IModule, Result<T>, ITenantContext
- ✅ Identity module — JWT, ASP.NET Identity, Tenant, TenantContext
- ✅ Catalog module — Establishment, Category, Product, Complement, own DbContext
- ✅ Orders module — CQRS (MediatR), state machine, domain events
- ✅ Payments module — IPaymentGateway, Asaas adapter, webhook idempotency
- ✅ Delivery module — IDeliveryProvider, Lalamove + Borzo, parallel quote
- ✅ API entry point — module wiring, Scalar OpenAPI, health endpoint
- ✅ PostgreSQL — each module uses its own schema + migrations history table
- ✅ Redis — OutputCache configured (catalog menu endpoint cached 5 min)
- ✅ RabbitMQ — MassTransit declared in Orders module csproj
- ✅ Docker Compose — postgres, redis, rabbitmq with healthchecks
- ✅ GitHub Actions CI — build + test on push/PR to main/develop
- ✅ Multi-tenant — TenantId on every Entity base class
- ✅ Result<T> pattern — used in Commands/Queries

**Not in MVP scope (deferred to Fase 2 per business plan):**
- SignalR hub for real-time order tracking (add after core is stable)
- MassTransit event bus wiring (csproj has package, implementation deferred)
- EF Core migrations (run `dotnet ef migrations add Initial` per module after Task 12)
- Redis distributed cache (infrastructure ready, usage deferred)

---

*Plan saved: `docs/superpowers/plans/2026-06-08-delify-modular-monolith.md`*
