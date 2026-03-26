using scommon;

namespace Scouting.API.Shared.Results;

public class FeatureObjectResultModel<T> : BaseResultModel where T : class
{
    public T? Data { get; set; }

    public static FeatureObjectResultModel<T> Ok(T data) =>
        new() { IsSuccess = true, Data = data };

    public static FeatureObjectResultModel<T> Error(List<MessageItem> messages) =>
        new() { IsSuccess = false, Messages = messages };

    public static FeatureObjectResultModel<T> Error(MessageItem message) =>
        new() { IsSuccess = false, Messages = [message] };

    public static FeatureObjectResultModel<T> NotFound() =>
        new()
        {
            IsSuccess = false,
            Messages = [new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND }]
        };

    public static FeatureObjectResultModel<T> Unauthorized() =>
        new()
        {
            IsSuccess = false,
            Messages = [new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_UNAUTHORIZED }]
        };
}
