using Scouting.Web;

namespace Scouting.Web
{
    public abstract class BaseModel : IModel
    {
        protected BaseModel()
        {
            Id = Guid.NewGuid();
            IsDeleted = false;
            IsActive = true;
        }

        public Guid Id { get; set; }
        public int OrderNumber { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public DateTime? DeletedTime { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
public abstract class BaseUserTrackModel : BaseModel, IUserTrackModel
{
    public Guid? CreatedUserId { get; set; }
    public Guid? UpdatedUserId { get; set; }
    public Guid? DeletedUserId { get; set; }
    
}
