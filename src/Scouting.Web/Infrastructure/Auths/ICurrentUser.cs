namespace Scouting.Web.Auths;

public interface ICurrentUser
{
    public Guid Id { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
