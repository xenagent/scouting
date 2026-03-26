using scommon.Auths;

namespace Scouting.API.Services;

public interface ICurrentUserService : ICurrentUser
{
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
