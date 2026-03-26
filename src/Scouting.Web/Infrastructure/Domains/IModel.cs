namespace Scouting.Web
{
    public interface IModel
    {
        Guid Id { get; set; }
        int OrderNumber { get; set; }
        DateTime CreatedTime { get; set; }
        DateTime? UpdatedTime { get; set; }
        DateTime? DeletedTime { get; set; }
        bool IsActive { get; set; }
        bool IsDeleted { get; set; }
    }

    public interface IUserTrackModel : IModel
    {
        
        Guid? CreatedUserId { get; set; }
        Guid? UpdatedUserId { get; set; }
        Guid? DeletedUserId { get; set; }
    }
}
