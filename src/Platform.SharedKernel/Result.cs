namespace Platform.SharedKernel;

public static class Result
{
    public static Result<T> Success<T>(T value) => new(value, Error.None);

    public static Result<T> Failure<T>(Error error)
    {
        if (error == Error.None)
        {
            throw new ArgumentException("Failure requires a non-empty error.", nameof(error));
        }

        return new(default, error);
    }
}

public readonly record struct Result<T>
{
    private readonly T? _value;

    internal Result(T? value, Error error)
    {
        _value = value;
        Error = error;
    }

    public bool IsSuccess => Error == Error.None;

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");
}
