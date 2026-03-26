using scommon;

namespace Scouting.API.Shared.Results;

public class FeatureResultModel : BaseResultModel
{
    public static FeatureResultModel Ok() => new() { IsSuccess = true };

    public static FeatureResultModel Error(List<MessageItem> messages) =>
        new() { IsSuccess = false, Messages = messages };

    public static FeatureResultModel Error(MessageItem message) =>
        new() { IsSuccess = false, Messages = [message] };

    public static FeatureResultModel NotFound() =>
        new()
        {
            IsSuccess = false,
            Messages = [new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND }]
        };

    public static FeatureResultModel Unauthorized() =>
        new()
        {
            IsSuccess = false,
            Messages = [new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_UNAUTHORIZED }]
        };

    public static FeatureResultModel Forbidden() =>
        new()
        {
            IsSuccess = false,
            Messages = [new MessageItem { Code = ErrorCodes.COMMON_MESSAGE_FORBIDDEN }]
        };
}
