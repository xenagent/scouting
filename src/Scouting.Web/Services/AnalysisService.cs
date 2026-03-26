using Microsoft.EntityFrameworkCore;
using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Infrastructure;
using Scouting.Web.Models;
using Scouting.Web.Shared;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public class AnalysisService : IAnalysisService
{
    private readonly AppDbContext _db;

    public AnalysisService(AppDbContext db) => _db = db;

    public async Task<ServiceListResult<RecentAnalysisVm>> GetRecentAsync(
        int count = 10, CancellationToken ct = default)
    {
        var items = await (
            from a in _db.Analyses.AsNoTracking()
            where a.Status == AnalysisStatus.Approved
            join p in _db.Players.AsNoTracking() on a.PlayerId equals p.Id
            join u in _db.Users.AsNoTracking() on a.CreatedUserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            orderby a.CreatedTime descending
            select new RecentAnalysisVm
            {
                Id = a.Id,
                PlayerId = p.Id,
                PlayerName = p.Name!,
                PlayerSlug = p.Slug!,
                PlayerImageUrl = p.ImageUrl,
                PlayerPosition = p.Position.ToString(),
                VideoUrl = a.VideoUrl!,
                ContentPreview = a.Content!.Length > 200
                    ? a.Content.Substring(0, 200) + "..."
                    : a.Content,
                AISummary = a.AISummary,
                AIScore = a.AIScore,
                ScoutUsername = u != null ? u.Username! : "anonymous",
                ScoutId = a.CreatedUserId ?? Guid.Empty,
                CreatedAt = a.CreatedTime
            })
            .Take(count)
            .ToListAsync(ct);

        return ServiceListResult<RecentAnalysisVm>.Ok(items);
    }

    public async Task<ServiceResult> ApproveAsync(Guid analysisId, CancellationToken ct = default)
    {
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.Id == analysisId, ct);
        if (analysis is null) return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        var result = analysis.Approve();
        if (!result.IsSuccess)
            return ServiceResult.Fail(result.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        var scout = await _db.Users.FirstOrDefaultAsync(u => u.Id == analysis.CreatedUserId, ct);
        scout?.IncrementApprovedAnalysisCount();

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RejectAsync(Guid analysisId, string reason, CancellationToken ct = default)
    {
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.Id == analysisId, ct);
        if (analysis is null) return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        var result = analysis.Reject(reason);
        if (!result.IsSuccess)
            return ServiceResult.Fail(result.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
