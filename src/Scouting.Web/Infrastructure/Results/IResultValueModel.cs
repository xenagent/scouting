namespace Scouting.Web
{
    public interface IResultValueModel<TValue> : IResultModel
    {
        TValue Value { get; set; }
    }
}
