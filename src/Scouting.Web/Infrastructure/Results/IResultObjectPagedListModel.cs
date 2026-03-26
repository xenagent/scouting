
namespace Scouting.Web
{
    public interface IResultObjectPagedListModel<TData> : IResultObjectListModel<TData>, IResultPagedListModel
         where TData : class, new()
    {

    }
}
