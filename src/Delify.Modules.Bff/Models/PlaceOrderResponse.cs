namespace Delify.Modules.Bff.Models;

public record PlaceOrderResponse(Guid OrderId, decimal Total, PixResponse Pix);

public record PixResponse(string QrCode, string CopyPaste, DateTimeOffset ExpiresAt);
