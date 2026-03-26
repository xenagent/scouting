

namespace Scouting.Web
{
    public interface IResultModel
    {
        bool IsSuccess { get; set; }
        List<MessageItem>? Messages { get; set; }
        List<KeyValuePair<string, string>>? LocalizedMessages { get; set; }
    }
}
