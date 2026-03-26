using System.Security.Claims;
using scommon.Auths;

namespace Scouting.Web.Services;

/// <summary>
/// Cookie auth claims'inden current user bilgisini okur.
/// BaseDbContext'in CreatedUserId/UpdatedUserId set etmesi için kullanılır.
/// </summary>
public class BlazorCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BlazorCurrentUser(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid Id
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
        set { }
    }

    public string? Phone { get => null; set { } }

    public string? Email
    {
        get => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
        set { }
    }
}
