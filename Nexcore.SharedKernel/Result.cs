namespace Nexcore.SharedKernel;

/// <summary>
/// Lightweight outcome type for service/domain operations: a success flag plus an
/// optional message and error list, so callers can branch on <see cref="Success"/>
/// instead of throwing for expected failures. Use <see cref="Result{T}"/> when the
/// operation also returns a value.
/// </summary>
public class Result
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = [];

    public static Result Ok(string? message = null) => new() { Success = true, Message = message };
    public static Result<T> Ok<T>(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public static Result Fail(string? message) => new() { Success = false, Message = message };
    public static Result Fail(params string[] errors) => new() { Success = false, Errors = [.. errors] };
    public static Result<T> Fail<T>(string? message) => new() { Success = false, Message = message };
}

/// <summary>Outcome type that also carries a payload on success (<see cref="Data"/>).</summary>
public class Result<T> : Result
{
    public T? Data { get; set; }

    public static Result<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public static new Result<T> Fail(string? message) => new() { Success = false, Message = message };
    public static new Result<T> Fail(params string[] errors) => new() { Success = false, Errors = [.. errors] };
}
