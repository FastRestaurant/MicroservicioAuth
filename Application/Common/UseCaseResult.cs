namespace Application.Common;

public enum UseCaseStatus
{
    Ok,
    BadRequest,
    Conflict,
    Unauthorized,
    NotFound
}

public sealed class UseCaseResult<T>
{
    public UseCaseStatus Status { get; init; }

    public T? Data { get; init; }

    public string? Message { get; init; }

    public IReadOnlyCollection<UseCaseError> Errors { get; init; } = Array.Empty<UseCaseError>();

    public static UseCaseResult<T> Ok(T data, string? message = null) =>
        new() { Status = UseCaseStatus.Ok, Data = data, Message = message };

    public static UseCaseResult<T> BadRequest(string message, IReadOnlyCollection<UseCaseError>? errors = null) =>
        new() { Status = UseCaseStatus.BadRequest, Message = message, Errors = errors ?? Array.Empty<UseCaseError>() };

    public static UseCaseResult<T> Conflict(string message, IReadOnlyCollection<UseCaseError>? errors = null) =>
        new() { Status = UseCaseStatus.Conflict, Message = message, Errors = errors ?? Array.Empty<UseCaseError>() };

    public static UseCaseResult<T> Unauthorized(string? message = null) =>
        new() { Status = UseCaseStatus.Unauthorized, Message = message };

    public static UseCaseResult<T> NotFound(string message) =>
        new() { Status = UseCaseStatus.NotFound, Message = message };
}
