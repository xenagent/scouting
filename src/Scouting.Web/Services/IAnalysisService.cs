using Scouting.Web.Models;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public interface IAnalysisService
{
    Task<ServiceListResult<RecentAnalysisVm>> GetRecentAsync(int count = 10, CancellationToken ct = default);
    Task<ServiceResult> ApproveAsync(Guid analysisId, CancellationToken ct = default);
    Task<ServiceResult> RejectAsync(Guid analysisId, string reason, CancellationToken ct = default);
}
