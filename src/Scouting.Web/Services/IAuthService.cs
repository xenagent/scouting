using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthUser>> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    Task<ServiceResult> RegisterAsync(string email, string username, string password, CancellationToken ct = default);
}

public class AuthUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}
