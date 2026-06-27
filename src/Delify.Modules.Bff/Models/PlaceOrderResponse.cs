namespace Delify.Modules.Bff.Models;

public record PlaceOrderResponse(Guid OrderId, decimal Subtotal, decimal DeliveryFee, decimal Total, PixResponse Pix);

public record PixResponse(string QrCode, string CopyPaste, DateTimeOffset ExpiresAt);
