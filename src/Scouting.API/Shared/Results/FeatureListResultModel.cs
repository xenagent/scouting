using scommon;

namespace Scouting.API.Shared.Results;

public class FeatureListResultModel<T> : BaseResultModel where T : class
{
    public List<T> Data { get; set; } = [];

    public static FeatureListResultModel<T> Ok(List<T> data) =>
        new() { IsSuccess = true, Data = data };

    public static FeatureListResultModel<T> Error(List<MessageItem> messages) =>
        new() { IsSuccess = false, Messages = messages };
}
