using Microsoft.EntityFrameworkCore;
using Scouting.Web.Domains.AnalysisLikeEntity;
using Scouting.Web.Infrastructure;
using Scouting.Web.Shared;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public class AnalysisLikeService : IAnalysisLikeService
{
    private readonly AppDbContext _db;

    public AnalysisLikeService(AppDbContext db) => _db = db;

    public async Task<ServiceResult> LikeAsync(Guid analysisId, Guid userId, CancellationToken ct = default)
    {
        var alreadyLiked = await _db.AnalysisLikes
            .AnyAsync(l => l.AnalysisId == analysisId && l.UserId == userId, ct);
        if (alreadyLiked)
            return ServiceResult.Fail(ErrorCodes.LIKE_ALREADY_LIKED);

        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.Id == analysisId, ct);
        if (analysis is null)
            return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        var result = AnalysisLike.Create(analysisId, userId);
        if (!result.IsSuccess)
            return ServiceResult.Fail(result.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        // Update denormalized like count on analysis
        analysis.IncrementLikeCount();

        // Update scout's total likes received (affects level)
        if (analysis.CreatedUserId.HasValue)
        {
            var scout = await _db.Users.FirstOrDefaultAsync(u => u.Id == analysis.CreatedUserId, ct);
            scout?.IncrementLikesReceived();
        }

        await _db.AnalysisLikes.AddAsync(result.Data!, ct);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnlikeAsync(Guid analysisId, Guid userId, CancellationToken ct = default)
    {
        var like = await _db.AnalysisLikes
            .FirstOrDefaultAsync(l => l.AnalysisId == analysisId && l.UserId == userId, ct);
        if (like is null)
            return ServiceResult.Fail(ErrorCodes.LIKE_NOT_LIKED);

        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.Id == analysisId, ct);
        analysis?.DecrementLikeCount();

        if (analysis?.CreatedUserId.HasValue == true)
        {
            var scout = await _db.Users.FirstOrDefaultAsync(u => u.Id == analysis.CreatedUserId, ct);
            scout?.DecrementLikesReceived();
        }

        _db.AnalysisLikes.Remove(like);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<HashSet<Guid>> GetLikedAnalysisIdsAsync(
        Guid userId, IEnumerable<Guid> analysisIds, CancellationToken ct = default)
    {
        var ids = analysisIds.ToList();
        var liked = await _db.AnalysisLikes
            .AsNoTracking()
            .Where(l => l.UserId == userId && ids.Contains(l.AnalysisId))
            .Select(l => l.AnalysisId)
            .ToListAsync(ct);
        return [.. liked];
    }
}
