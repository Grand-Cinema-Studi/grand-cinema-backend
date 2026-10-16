namespace GrandCinema.Application.Abstractions.Results;

public sealed record class Error(ErrorType Type, string Code, string Message)
{
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }

    public static Error Validation(IReadOnlyDictionary<string, string[]> validationErrors) =>
        new(ErrorType.Validation, "validation", "One or more fields are invalid.") { ValidationErrors = validationErrors };

    public static Error Unauthorized(string code, string message) => new(ErrorType.Unauthorized, code, message);

    public static Error Forbidden(string code, string message) => new(ErrorType.Forbidden, code, message);

    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);
}
