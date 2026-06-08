namespace Delify.Shared.Result;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static Error NotFound(string entity) => new($"{entity}.NotFound", $"{entity} not found.");
    public static Error Conflict(string entity) => new($"{entity}.Conflict", $"{entity} already exists.");
    public static Error Validation(string message) => new("Validation.Error", message);
}
