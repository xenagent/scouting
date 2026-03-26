using Scouting.Web.Models;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public interface IPlayerService
{
    Task<ServiceListResult<PlayerListItemVm>> GetListAsync(PlayerFilter filter, CancellationToken ct = default);
    Task<ServiceListResult<PlayerListItemVm>> GetTopAsync(int count = 10, CancellationToken ct = default);
    Task<ServiceResult<PlayerDetailVm>> GetDetailAsync(string slug, CancellationToken ct = default);
    Task<ServiceListResult<PendingPlayerVm>> GetPendingAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ServiceResult> SuggestAsync(SuggestPlayerInput input, Guid userId, CancellationToken ct = default);
    Task<ServiceResult> ApproveAsync(Guid playerId, CancellationToken ct = default);
    Task<ServiceResult> RejectAsync(Guid playerId, string reason, CancellationToken ct = default);
    Task<ServiceResult> SetPlayerImageAsync(Guid playerId, string imageUrl, CancellationToken ct = default);
}
