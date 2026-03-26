namespace Scouting.Web.Shared.Results;

/// <summary>
/// Servis katmanından dönen sonuç modeli.
/// Pages doğrudan bu tipi kullanır — HTTP katmanı yok.
/// </summary>
public class ServiceResult
{
    public bool IsSuccess { get; private init; }
    public string? ErrorCode { get; private init; }

    public static ServiceResult Ok() => new() { IsSuccess = true };
    public static ServiceResult Fail(string errorCode) => new() { IsSuccess = false, ErrorCode = errorCode };
}

public class ServiceResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public string? ErrorCode { get; private init; }

    public static ServiceResult<T> Ok(T data) => new() { IsSuccess = true, Data = data };
    public static ServiceResult<T> Fail(string errorCode) => new() { IsSuccess = false, ErrorCode = errorCode };
    public static ServiceResult<T> NotFound() => Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);
}

public class ServiceListResult<T>
{
    public bool IsSuccess { get; private init; }
    public List<T> Data { get; private init; } = [];
    public string? ErrorCode { get; private init; }

    public static ServiceListResult<T> Ok(List<T> data) => new() { IsSuccess = true, Data = data };
    public static ServiceListResult<T> Fail(string errorCode) => new() { IsSuccess = false, ErrorCode = errorCode };
}
