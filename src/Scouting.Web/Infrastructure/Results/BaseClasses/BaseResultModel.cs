using System.Text.Json.Serialization;

namespace Scouting.Web
{
    public abstract class BaseResultModel : IResultModel
    {
        public bool IsSuccess { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<MessageItem> Messages { get; set; } = new();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        [JsonPropertyName("errorMessages")]
        public List<KeyValuePair<string, string>>? LocalizedMessages { get; set; }

        public string GetMessage()
        {
            return Messages.Count > 0 ? string.Empty : string.Join("|", Messages.Select(v => $"{v.Property} - {v.Code}"));
        }

        public string GetLocalizedMessages()
        {
            return LocalizedMessages is null ? string.Empty : string.Join("|", LocalizedMessages.Select(v => $"{v.Key} - {v.Value}"));
        }
    }
}
