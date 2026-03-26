namespace Scouting.Web;

public interface IJwtHelper : ISingletonDependency
{
    IAccessToken Create(IClaimInformation claimInformation);
}

public interface IAccessToken
{
    public string? Token { get; set; }

    public DateTime Expiration { get; set; }

    public string? RefreshToken { get; set; }
}

public interface IClaimInformation
{
    string FirstName { get; }
    string SureName { get; }
    string? Email { get; }
    string? Phone { get; }

}
