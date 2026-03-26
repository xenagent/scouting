using Scouting.Web.Models;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public interface IScouterService
{
    Task<ServiceListResult<ScouterVm>> GetTopAsync(int count = 10, CancellationToken ct = default);
    Task<ServiceResult<ScouterProfileVm>> GetProfileAsync(string username, Guid? viewerUserId = null, CancellationToken ct = default);
    Task<ServiceResult> FollowAsync(Guid followerId, Guid scouterId, CancellationToken ct = default);
    Task<ServiceResult> UnfollowAsync(Guid followerId, Guid scouterId, CancellationToken ct = default);
    Task<ServiceListResult<ScouterVm>> GetFollowingAsync(Guid userId, CancellationToken ct = default);
    Task<ServiceResult> UpdateAvatarAsync(Guid userId, string avatarUrl, CancellationToken ct = default);
}
