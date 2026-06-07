namespace Osiris.Application.Common.Models;

public sealed record ResultError(string Message, string? Field = null, string? Code = null);

public class Result
{
    protected Result(bool isSuccess, IReadOnlyCollection<ResultError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyCollection<ResultError> Errors { get; }

    public static Result Success() => new(true, Array.Empty<ResultError>());

    public static Result Failure(params ResultError[] errors) => new(false, errors);

    public static Result Failure(IEnumerable<ResultError> errors) => new(false, errors.ToArray());
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, IReadOnlyCollection<ResultError> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<ResultError>());

    public static new Result<T> Failure(params ResultError[] errors) => new(false, default, errors);

    public static new Result<T> Failure(IEnumerable<ResultError> errors) => new(false, default, errors.ToArray());
}
