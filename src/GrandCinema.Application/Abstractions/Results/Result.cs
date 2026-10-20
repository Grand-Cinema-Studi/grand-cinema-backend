using System.Diagnostics.CodeAnalysis;

namespace GrandCinema.Application.Abstractions.Results;

// An expected business failure (email already used, not enough seats...) is returned, not thrown:
// the endpoint translates it into an HTTP status code.
public sealed class Result<TValue>
{
    private readonly TValue? _value;

    private Result(TValue value)
    {
        _value = value;
    }

    private Result(Error error)
    {
        Error = error;
    }

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<TValue> Success(TValue value) => new(value);

    public static Result<TValue> Failure(Error error) => new(error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
