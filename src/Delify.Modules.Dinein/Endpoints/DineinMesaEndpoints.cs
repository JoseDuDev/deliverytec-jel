using Delify.Modules.Catalog.Infrastructure;
using Delify.Modules.Dinein.Domain;
using Delify.Modules.Dinein.Infrastructure;
using Delify.Modules.Dinein.Services;
using Delify.Modules.Orders.Domain;
using Delify.Modules.Orders.Infrastructure;
using Delify.Modules.Payments.Abstractions;
using Delify.Modules.Payments.Domain;
using Delify.Modules.Payments.Infrastructure;
using Delify.Shared.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Delify.Modules.Dinein.Endpoints;

internal static class DineinMesaEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        // ── Cardápio da mesa + comanda corrente ─────────────────────────────
        app.MapGet("/bff/mesa/{token}", async (
            string token,
            DineinDbContext db,
            CatalogDbContext catalogDb,
            OrdersDbContext ordersDb) =>
        {
            var table = await db.Tables.AsNoTracking()
                .FirstOrDefaultAsync(t => t.QrToken == token);
            if (table is null)
                return Results.NotFound(new { error = "Mesa não encontrada." });

            var establishment = await catalogDb.Establishments
                .Include(e => e.Categories.OrderBy(c => c.Order))
                    .ThenInclude(c => c.Products)
                        .ThenInclude(p => p.Complements)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == table.EstablishmentId);
            if (establishment is null)
                return Results.NotFound(new { error = "Estabelecimento não encontrado." });

            var session = await db.Sessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);

            var comanda = await BuildComandaAsync(session, ordersDb);

            var categories = establishment.Categories
                .Select(c => new MesaCategoryDto(
                    c.Id, c.Name, c.Order,
                    c.Products
                        .Select(p => new MesaProductDto(
                            p.Id, p.Name, p.Price, p.Description, p.PhotoUrl,
                            p.Complements
                                .Select(cp => new MesaComplementDto(cp.Id, cp.Name, cp.AdditionalPrice))
                                .ToList()))
                        .ToList()))
                .ToList();

            return Results.Ok(new MesaResponse(
                table.Id, table.Number,
                establishment.Id, establishment.Name, establishment.Slug, establishment.IsOpen,
                establishment.ServiceFeeEnabled, establishment.ServiceFeePercent,
                categories, comanda));
        })
        .WithName("MesaGetMenu")
        .WithTags("Mesa")
        .AllowAnonymous();

        // ── Enviar rodada de pedido ─────────────────────────────────────────
        app.MapPost("/bff/mesa/{token}/pedido", async (
            string token,
            PlaceMesaOrderReq req,
            DineinDbContext db,
            CatalogDbContext catalogDb,
            OrdersDbContext ordersDb,
            IPainelDashboardNotifier painelNotifier,
            MesaNotifier mesaNotifier) =>
        {
            if (req.Items is null || req.Items.Count == 0)
                return Results.BadRequest(new { error = "Adicione ao menos um item ao pedido." });

            var table = await db.Tables
                .FirstOrDefaultAsync(t => t.QrToken == token);
            if (table is null)
                return Results.NotFound(new { error = "Mesa não encontrada." });

            var establishment = await catalogDb.Establishments.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == table.EstablishmentId);
            if (establishment is null)
                return Results.NotFound(new { error = "Estabelecimento não encontrado." });
            if (!establishment.IsOpen)
                return Results.Conflict(new { error = "O estabelecimento está fechado no momento." });

            var productIds = req.Items.Select(i => i.ProductId).ToList();
            var products = await catalogDb.Products
                .Include(p => p.Complements)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var missing = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missing.Count > 0)
                return Results.BadRequest(new { error = "Um ou mais produtos não foram encontrados.", missingIds = missing });

            // Abre a comanda de forma lazy: a mesa só fica ocupada no 1º pedido.
            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);
            if (session is null)
            {
                session = TableSession.Open(table.TenantId, table.EstablishmentId, table.Id);
                db.Sessions.Add(session);
                table.Occupy();
                await db.SaveChangesAsync();
            }

            var order = Order.CreateDinein(establishment.TenantId, table.EstablishmentId, session.Id, req.Note);
            foreach (var itemReq in req.Items)
            {
                var product = products.First(p => p.Id == itemReq.ProductId);
                var complementsTotal = itemReq.ComplementIds is { Count: > 0 }
                    ? product.Complements
                        .Where(c => itemReq.ComplementIds.Contains(c.Id))
                        .Sum(c => c.AdditionalPrice)
                    : 0m;
                var unitPrice = product.Price + complementsTotal;
                order.AddItem(product.Id, product.Name, itemReq.Quantity, unitPrice);
            }

            ordersDb.Orders.Add(order);
            await ordersDb.SaveChangesAsync();

            // Empurra pro dashboard da cozinha em tempo real.
            painelNotifier.Notify(order.TenantId,
                new PainelOrderEvent(order.Id, "AwaitingConfirmation", DateTimeOffset.UtcNow));

            // Atualiza o painel de mesas (total da comanda) em tempo real.
            mesaNotifier.Notify(order.TenantId,
                new MesaEvent("table-update", table.Id, table.Number, null, null, DateTimeOffset.UtcNow));

            var comanda = await BuildComandaAsync(session, ordersDb);
            return Results.Ok(new PlaceMesaOrderResponse(order.Id, session.Id, order.Total, comanda));
        })
        .WithName("MesaPlaceOrder")
        .WithTags("Mesa")
        .AllowAnonymous();

        // ── Chamar garçom ───────────────────────────────────────────────────
        app.MapPost("/bff/mesa/{token}/garcom", async (
            string token,
            CallWaiterReq? req,
            DineinDbContext db,
            MesaNotifier mesaNotifier) =>
        {
            var table = await db.Tables
                .FirstOrDefaultAsync(t => t.QrToken == token);
            if (table is null)
                return Results.NotFound(new { error = "Mesa não encontrada." });

            // Dedupe: se já há uma chamada pendente para a mesa, não empilha outra.
            var pending = await db.WaiterCalls
                .FirstOrDefaultAsync(w => w.TableId == table.Id && w.Status == WaiterCallStatus.Pending);
            if (pending is not null)
                return Results.Ok(new CallWaiterResponse(pending.Id, true));

            var session = await db.Sessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);

            var call = WaiterCall.Create(
                table.TenantId, table.EstablishmentId, table.Id, table.Number,
                session?.Id, req?.Reason);
            db.WaiterCalls.Add(call);
            await db.SaveChangesAsync();

            mesaNotifier.Notify(table.TenantId,
                new MesaEvent("waiter-call", table.Id, table.Number, call.Id, call.Reason, DateTimeOffset.UtcNow));

            return Results.Ok(new CallWaiterResponse(call.Id, false));
        })
        .WithName("MesaCallWaiter")
        .WithTags("Mesa")
        .AllowAnonymous();

        // ── Fechar a conta (gera PIX da comanda) ────────────────────────────
        app.MapPost("/bff/mesa/{token}/conta", async (
            string token,
            CloseBillReq req,
            DineinDbContext db,
            CatalogDbContext catalogDb,
            OrdersDbContext ordersDb,
            PaymentsDbContext paymentsDb,
            IPaymentGateway gateway) =>
        {
            var table = await db.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.QrToken == token);
            if (table is null) return Results.NotFound(new { error = "Mesa não encontrada." });

            var session = await db.Sessions
                .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);
            if (session is null) return Results.Conflict(new { error = "Não há comanda aberta nesta mesa." });

            var establishment = await catalogDb.Establishments.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == table.EstablishmentId);
            if (establishment is null) return Results.NotFound(new { error = "Estabelecimento não encontrado." });

            var comanda = await BuildComandaAsync(session, ordersDb);
            var subtotal = comanda.Total;
            if (subtotal <= 0) return Results.Conflict(new { error = "A comanda está vazia." });

            var applyFee = establishment.ServiceFeeEnabled && !req.WaiveServiceFee;
            var serviceFee = applyFee ? Math.Round(subtotal * establishment.ServiceFeePercent / 100m, 2) : 0m;
            var total = subtotal + serviceFee;

            // Se a comanda já foi dividida, o pagamento é por parte — não dá pra
            // gerar uma cobrança do valor cheio por cima das partes.
            if (await paymentsDb.Payments.AnyAsync(p =>
                    p.TableSessionId == session.Id && p.ShareIndex != null && p.Status != PaymentStatus.Failed))
                return Results.Conflict(new { error = "Esta comanda foi dividida. Pague pelas partes." });

            // Reaproveita o PIX pendente da comanda — mas só se o valor ainda bater.
            // A mesa pode ter pedido outra rodada depois de gerar o PIX; nesse caso
            // a cobrança antiga está defasada e é invalidada em vez de reexibida.
            var pending = await paymentsDb.Payments
                .Where(p => p.TableSessionId == session.Id
                         && p.ShareIndex == null
                         && p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (pending is not null)
            {
                if (pending.Amount == total && pending.PixCopyPaste is not null)
                {
                    return Results.Ok(new CloseBillResponse(
                        session.Id, subtotal, serviceFee, total, applyFee,
                        new PixDto(pending.PixQrCode ?? "", pending.PixCopyPaste, pending.CreatedAt.AddHours(24))));
                }

                pending.Void();
            }

            var payment = Payment.CreateForSession(session.TenantId, session.Id, total);
            var pix = await gateway.CreatePixAsync(
                new PixPaymentRequest(payment.Id, total, req.Cpf ?? "", req.Name ?? ""));
            payment.SetPixData(pix.GatewayId, pix.QrCode, pix.CopyPaste);
            paymentsDb.Payments.Add(payment);
            await paymentsDb.SaveChangesAsync();

            return Results.Ok(new CloseBillResponse(
                session.Id, subtotal, serviceFee, total, applyFee,
                new PixDto(pix.QrCode, pix.CopyPaste, pix.ExpiresAt)));
        })
        .WithName("MesaCloseBill")
        .WithTags("Mesa")
        .AllowAnonymous();

        // ── Dividir a conta em N partes iguais ──────────────────────────────
        app.MapPost("/bff/mesa/{token}/conta/dividir", async (
            string token,
            SplitBillReq req,
            DineinDbContext db,
            CatalogDbContext catalogDb,
            OrdersDbContext ordersDb,
            PaymentsDbContext paymentsDb) =>
        {
            if (req.People < 2 || req.People > BillSplit.MaxShares)
                return Results.BadRequest(new { error = $"Divida entre 2 e {BillSplit.MaxShares} pessoas." });

            var ctx = await LoadBillContextAsync(token, req.WaiveServiceFee, db, catalogDb, ordersDb);
            if (ctx.Error is not null) return ctx.Error;
            var (session, subtotal, serviceFee, total, applyFee) = ctx;

            var existing = await paymentsDb.Payments
                .Where(p => p.TableSessionId == session!.Id && p.Status != PaymentStatus.Failed)
                .ToListAsync();

            var shares = existing.Where(p => p.ShareIndex != null).ToList();
            var anyPaid = shares.Any(p => p.Status == PaymentStatus.Confirmed);

            // Já dividida e alguém já pagou: o rateio está congelado. Mudar o número
            // de pessoas agora rebalancearia partes que já foram quitadas.
            if (anyPaid)
            {
                var currentCount = shares[0].ShareCount!.Value;
                if (currentCount != req.People || shares.Sum(p => p.Amount) != total)
                    return Results.Conflict(new
                    {
                        error = "A conta já começou a ser paga e não pode ser redividida.",
                        shareCount = currentCount
                    });

                return Results.Ok(BuildSplitResponse(session!.Id, subtotal, serviceFee, total, applyFee, shares));
            }

            // Ninguém pagou ainda: pode redividir à vontade. Invalida o que existia
            // (partes antigas E qualquer PIX do valor cheio) e refaz o rateio.
            foreach (var p in existing.Where(p => p.Status == PaymentStatus.Pending)) p.Void();

            var amounts = BillSplit.Even(total, req.People);
            var created = new List<Payment>(req.People);
            for (var i = 0; i < amounts.Length; i++)
            {
                var share = Payment.CreateSessionShare(
                    session!.TenantId, session.Id, amounts[i], i + 1, req.People);
                created.Add(share);
                paymentsDb.Payments.Add(share);
            }

            await paymentsDb.SaveChangesAsync();

            return Results.Ok(BuildSplitResponse(session!.Id, subtotal, serviceFee, total, applyFee, created));
        })
        .WithName("MesaSplitBill")
        .WithTags("Mesa")
        .AllowAnonymous();

        // ── Gerar o PIX de uma parte específica ─────────────────────────────
        app.MapPost("/bff/mesa/{token}/conta/parte/{index:int}/pix", async (
            string token,
            int index,
            SharePixReq req,
            DineinDbContext db,
            PaymentsDbContext paymentsDb,
            IPaymentGateway gateway) =>
        {
            var table = await db.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.QrToken == token);
            if (table is null) return Results.NotFound(new { error = "Mesa não encontrada." });

            var session = await db.Sessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);
            if (session is null) return Results.Conflict(new { error = "Não há comanda aberta nesta mesa." });

            var share = await paymentsDb.Payments.FirstOrDefaultAsync(p =>
                p.TableSessionId == session.Id && p.ShareIndex == index && p.Status != PaymentStatus.Failed);

            if (share is null) return Results.NotFound(new { error = "Parte não encontrada. A conta foi dividida?" });
            if (share.Status == PaymentStatus.Confirmed)
                return Results.Conflict(new { error = "Esta parte já foi paga." });

            // Reaproveita o PIX da parte — o valor de uma parte não muda depois de
            // criada, então aqui o reuso é sempre seguro.
            if (share.PixCopyPaste is not null)
            {
                return Results.Ok(new SharePixResponse(index, share.Amount,
                    new PixDto(share.PixQrCode ?? "", share.PixCopyPaste, share.CreatedAt.AddHours(24))));
            }

            var pix = await gateway.CreatePixAsync(
                new PixPaymentRequest(share.Id, share.Amount, req.Cpf ?? "", req.Name ?? ""));
            share.SetPixData(pix.GatewayId, pix.QrCode, pix.CopyPaste);
            await paymentsDb.SaveChangesAsync();

            return Results.Ok(new SharePixResponse(index, share.Amount,
                new PixDto(pix.QrCode, pix.CopyPaste, pix.ExpiresAt)));
        })
        .WithName("MesaSharePix")
        .WithTags("Mesa")
        .AllowAnonymous();

        // ── Status do pagamento da comanda (polling) ────────────────────────
        app.MapGet("/bff/conta/{sessionId:guid}/status", async (
            Guid sessionId, DineinDbContext db, PaymentsDbContext paymentsDb) =>
        {
            var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId);

            var payments = await paymentsDb.Payments.AsNoTracking()
                .Where(p => p.TableSessionId == sessionId && p.Status != PaymentStatus.Failed)
                .OrderBy(p => p.ShareIndex).ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            var shares = payments.Where(p => p.ShareIndex != null).ToList();
            var paidAmount = payments.Where(p => p.Status == PaymentStatus.Confirmed).Sum(p => p.Amount);

            // A sessão fechada é a palavra final: cobre também o caso do staff ter
            // liberado a mesa por fora do fluxo de pagamento.
            var paid = session?.Status == SessionStatus.Closed
                       || (payments.Count > 0 && payments.All(p => p.Status == PaymentStatus.Confirmed));

            return Results.Ok(new ContaStatusResponse(
                paid,
                session?.Status.ToString() ?? "Unknown",
                payments.Sum(p => p.Amount),
                paidAmount,
                shares.Count(p => p.Status == PaymentStatus.Confirmed),
                shares.Count,
                shares.Select(ToShareDto).ToList()));
        })
        .WithName("MesaContaStatus")
        .WithTags("Mesa")
        .AllowAnonymous();

        return app;
    }

    private static BillShareDto ToShareDto(Payment p) =>
        new(p.ShareIndex!.Value, p.Amount, p.Status == PaymentStatus.Confirmed, p.PixCopyPaste is not null);

    private static SplitBillResponse BuildSplitResponse(
        Guid sessionId, decimal subtotal, decimal serviceFee, decimal total, bool applyFee,
        IEnumerable<Payment> shares)
    {
        var ordered = shares.OrderBy(p => p.ShareIndex).ToList();
        return new SplitBillResponse(
            sessionId, subtotal, serviceFee, total, applyFee,
            ordered.Count, ordered.Select(ToShareDto).ToList());
    }

    /// <summary>Resolve mesa → comanda aberta → totais com taxa. Compartilhado pelos fluxos de conta.</summary>
    private static async Task<BillContext> LoadBillContextAsync(
        string token, bool waiveServiceFee,
        DineinDbContext db, CatalogDbContext catalogDb, OrdersDbContext ordersDb)
    {
        var table = await db.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.QrToken == token);
        if (table is null)
            return BillContext.Fail(Results.NotFound(new { error = "Mesa não encontrada." }));

        var session = await db.Sessions
            .FirstOrDefaultAsync(s => s.TableId == table.Id && s.Status == SessionStatus.Open);
        if (session is null)
            return BillContext.Fail(Results.Conflict(new { error = "Não há comanda aberta nesta mesa." }));

        var establishment = await catalogDb.Establishments.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == table.EstablishmentId);
        if (establishment is null)
            return BillContext.Fail(Results.NotFound(new { error = "Estabelecimento não encontrado." }));

        var comanda = await BuildComandaAsync(session, ordersDb);
        var subtotal = comanda.Total;
        if (subtotal <= 0)
            return BillContext.Fail(Results.Conflict(new { error = "A comanda está vazia." }));

        var applyFee = establishment.ServiceFeeEnabled && !waiveServiceFee;
        var serviceFee = applyFee ? Math.Round(subtotal * establishment.ServiceFeePercent / 100m, 2) : 0m;

        return new BillContext(session, subtotal, serviceFee, subtotal + serviceFee, applyFee, null);
    }

    private readonly record struct BillContext(
        TableSession? Session, decimal Subtotal, decimal ServiceFee, decimal Total, bool ApplyFee, IResult? Error)
    {
        internal static BillContext Fail(IResult error) => new(null, 0, 0, 0, false, error);

        internal void Deconstruct(
            out TableSession? session, out decimal subtotal, out decimal serviceFee,
            out decimal total, out bool applyFee)
        {
            session = Session; subtotal = Subtotal; serviceFee = ServiceFee;
            total = Total; applyFee = ApplyFee;
        }
    }

    private static async Task<ComandaDto> BuildComandaAsync(TableSession? session, OrdersDbContext ordersDb)
    {
        if (session is null)
            return new ComandaDto(null, null, [], 0);

        var orders = await ordersDb.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.TableSessionId == session.Id && o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        var items = orders
            .SelectMany(o => o.Items)
            .Select(i => new ComandaItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Total))
            .ToList();

        return new ComandaDto(session.Id, session.OpenedAt, items, items.Sum(i => i.Total));
    }
}

internal record MesaComplementDto(Guid Id, string Name, decimal Price);
internal record MesaProductDto(Guid Id, string Name, decimal Price, string? Description, string? ImageUrl, IReadOnlyList<MesaComplementDto> Complements);
internal record MesaCategoryDto(Guid Id, string Name, int Order, IReadOnlyList<MesaProductDto> Products);
internal record ComandaItemDto(string ProductName, int Quantity, decimal UnitPrice, decimal Total);
internal record ComandaDto(Guid? SessionId, DateTimeOffset? OpenedAt, IReadOnlyList<ComandaItemDto> Items, decimal Total);
internal record MesaResponse(
    Guid TableId, string TableNumber,
    Guid EstablishmentId, string EstablishmentName, string Slug, bool IsOpen,
    bool ServiceFeeEnabled, decimal ServiceFeePercent,
    IReadOnlyList<MesaCategoryDto> Categories, ComandaDto Comanda);

internal record MesaOrderItemReq(Guid ProductId, int Quantity, IReadOnlyList<Guid>? ComplementIds);
internal record PlaceMesaOrderReq(IReadOnlyList<MesaOrderItemReq> Items, string? Note);
internal record PlaceMesaOrderResponse(Guid OrderId, Guid SessionId, decimal OrderTotal, ComandaDto Comanda);

internal record CallWaiterReq(string? Reason);
internal record CallWaiterResponse(Guid CallId, bool AlreadyPending);

internal record CloseBillReq(string? Cpf, string? Name, bool WaiveServiceFee);
internal record PixDto(string QrCode, string CopyPaste, DateTimeOffset ExpiresAt);
internal record CloseBillResponse(Guid SessionId, decimal Subtotal, decimal ServiceFee, decimal Total, bool ServiceFeeApplied, PixDto Pix);

internal record SplitBillReq(int People, bool WaiveServiceFee);
internal record BillShareDto(int Index, decimal Amount, bool Paid, bool HasPix);
internal record SplitBillResponse(
    Guid SessionId, decimal Subtotal, decimal ServiceFee, decimal Total, bool ServiceFeeApplied,
    int People, IReadOnlyList<BillShareDto> Shares);
internal record SharePixReq(string? Cpf, string? Name);
internal record SharePixResponse(int Index, decimal Amount, PixDto Pix);

internal record ContaStatusResponse(
    bool Paid, string SessionStatus, decimal Total,
    decimal PaidAmount, int PaidShares, int TotalShares, IReadOnlyList<BillShareDto> Shares);
