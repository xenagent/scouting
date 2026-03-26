using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public interface IAnalysisLikeService
{
    Task<ServiceResult> LikeAsync(Guid analysisId, Guid userId, CancellationToken ct = default);
    Task<ServiceResult> UnlikeAsync(Guid analysisId, Guid userId, CancellationToken ct = default);
    Task<HashSet<Guid>> GetLikedAnalysisIdsAsync(Guid userId, IEnumerable<Guid> analysisIds, CancellationToken ct = default);
}
