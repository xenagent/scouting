using Microsoft.EntityFrameworkCore;
using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Domains.PlayerEntity;
using Scouting.Web.Infrastructure;
using Scouting.Web.Models;
using Scouting.Web.Shared;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public class AnalysisService : IAnalysisService
{
    private readonly AppDbContext _db;
    private readonly IAIAnalysisService _ai;
    private readonly TmSyncQueue _tmQueue;

    public AnalysisService(AppDbContext db, IAIAnalysisService ai, TmSyncQueue tmQueue)
    {
        _db = db;
        _ai = ai;
        _tmQueue = tmQueue;
    }

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

    public async Task<ServiceListResult<PendingAnalysisVm>> GetPendingAsync(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var items = await (
            from a in _db.Analyses.AsNoTracking()
            where a.Status == AnalysisStatus.Pending
            join p in _db.Players.AsNoTracking() on a.PlayerId equals p.Id
            where p.Status == PlayerStatus.Approved
            join u in _db.Users.AsNoTracking() on a.CreatedUserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            orderby a.CreatedTime descending
            select new PendingAnalysisVm
            {
                Id = a.Id,
                PlayerId = p.Id,
                PlayerName = p.Name!,
                PlayerSlug = p.Slug!,
                SuggestedByUsername = u != null ? u.Username! : "anonymous",
                SuggestedByUserId = a.CreatedUserId ?? Guid.Empty,
                VideoUrl = a.VideoUrl!,
                ContentPreview = a.Content!.Length > 300
                    ? a.Content.Substring(0, 300) + "..."
                    : a.Content,
                FilledSectionsCount = a.FilledSectionsCount,
                IsFlaggedAsDuplicate = a.IsFlaggedAsDuplicate,
                QualityScore = a.AIScore,
                SubmittedAt = a.CreatedTime
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return ServiceListResult<PendingAnalysisVm>.Ok(items);
    }

    public async Task<ServiceListResult<MyAnalysisVm>> GetMyAnalysesAsync(
        Guid userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var items = await (
            from a in _db.Analyses.AsNoTracking()
            where a.CreatedUserId == userId
            join p in _db.Players.AsNoTracking() on a.PlayerId equals p.Id
            orderby a.CreatedTime descending
            select new MyAnalysisVm
            {
                Id = a.Id,
                PlayerName = p.Name!,
                PlayerSlug = p.Slug!,
                PlayerImageUrl = p.ImageUrl,
                ContentPreview = a.Content!.Length > 200
                    ? a.Content.Substring(0, 200) + "..."
                    : a.Content,
                AIScore = a.AIScore,
                AISummary = a.AISummary,
                LikeCount = a.LikeCount,
                Status = a.Status.ToString(),
                IsFlaggedAsDuplicate = a.IsFlaggedAsDuplicate,
                CreatedAt = a.CreatedTime
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return ServiceListResult<MyAnalysisVm>.Ok(items);
    }

    public async Task<ServiceResult> AddAnalysisAsync(
        Guid playerId, AnalysisInput input, Guid userId, CancellationToken ct = default)
    {
        var player = await _db.Players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId && p.Status == PlayerStatus.Approved, ct);
        if (player is null)
            return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        // Oyuncunun TM verisi varsa, analiz girişi anında güncellemeyi tetikle
        if (!string.IsNullOrEmpty(player.TransfermarktId))
            _tmQueue.Enqueue(player.TransfermarktId);

        var result = Analysis.Create(
            playerId, input.VideoUrl, input.General, userId,
            input.Technical, input.Tactical, input.Physical,
            input.Strengths, input.Weaknesses);

        if (!result.IsSuccess)
            return ServiceResult.Fail(result.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        var analysis = result.Data!;

        // Collect existing approved analyses for duplicate detection + discovery context
        var existingApproved = await _db.Analyses
            .AsNoTracking()
            .Where(a => a.PlayerId == playerId && a.Status == AnalysisStatus.Approved)
            .Select(a => a.Content!)
            .ToListAsync(ct);

        var existingContent = existingApproved;

        // AI evaluation (stub or real Bedrock)
        var aiInput = new AIAnalysisInput
        {
            PlayerName = player.Name!,
            PlayerPosition = player.Position.ToString(),
            VideoUrl = input.VideoUrl,
            GeneralContent = input.General,
            TechnicalContent = input.Technical,
            TacticalContent = input.Tactical,
            PhysicalContent = input.Physical,
            StrengthsContent = input.Strengths,
            WeaknessesContent = input.Weaknesses,
            ExistingAnalysesContent = existingContent
        };

        var aiResult = await _ai.EvaluateAsync(aiInput, ct);
        if (aiResult.Score > 0 || !string.IsNullOrEmpty(aiResult.Summary))
            analysis.SetAIReview(
                aiResult.Summary ?? "",
                aiResult.Score,
                aiResult.IsPossibleDuplicate);

        // Keşif bağlamını kaydet — puan bonusu hesabı için oyuncunun anlık snapshot'ı
        analysis.SetDiscoveryContext(
            player.MarketValue,
            player.Age,
            existingApproved.Count);

        await _db.Analyses.AddAsync(analysis, ct);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
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
