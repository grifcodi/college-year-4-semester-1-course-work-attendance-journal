namespace AttendanceJournal.Services.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? ErrorKey { get; }
    public object[] ErrorArgs { get; }

    protected Result(bool isSuccess, string? errorKey, object[]? errorArgs = null)
    {
        IsSuccess = isSuccess;
        ErrorKey = errorKey;
        ErrorArgs = errorArgs ?? [];
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string errorKey, params object[] args) => new(false, errorKey, args);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? errorKey, object[]? errorArgs = null)
        : base(isSuccess, errorKey, errorArgs)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(string errorKey, params object[] args) => new(false, default, errorKey, args);
}
