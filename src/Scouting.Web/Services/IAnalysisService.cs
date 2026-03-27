using Scouting.Web.Models;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public interface IAnalysisService
{
    Task<ServiceListResult<RecentAnalysisVm>> GetRecentAsync(int count = 10, CancellationToken ct = default);
    Task<ServiceListResult<PendingAnalysisVm>> GetPendingAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ServiceResult> AddAnalysisAsync(Guid playerId, AnalysisInput input, Guid userId, CancellationToken ct = default);
    Task<ServiceListResult<MyAnalysisVm>> GetMyAnalysesAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ServiceResult> ApproveAsync(Guid analysisId, CancellationToken ct = default);
    Task<ServiceResult> RejectAsync(Guid analysisId, string reason, CancellationToken ct = default);
}
