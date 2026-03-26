using Microsoft.EntityFrameworkCore;
using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Domains.PlayerEntity;
using Scouting.Web.Domains.VoteEntity;
using Scouting.Web.Infrastructure;
using Scouting.Web.Models;
using Scouting.Web.Shared;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public class PlayerService : IPlayerService
{
    private readonly AppDbContext _db;
    private readonly ITransfermarktService _tm;

    public PlayerService(AppDbContext db, ITransfermarktService tm)
    {
        _db = db;
        _tm = tm;
    }

    public async Task<ServiceListResult<PlayerListItemVm>> GetListAsync(
        PlayerFilter filter, CancellationToken ct = default)
    {
        var query = _db.Players
            .AsNoTracking()
            .Where(p => p.Status == PlayerStatus.Approved);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p =>
                p.Name!.ToLower().Contains(filter.Search.ToLower()) ||
                p.Team!.ToLower().Contains(filter.Search.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.Position) &&
            Enum.TryParse<PlayerPosition>(filter.Position, out var pos))
            query = query.Where(p => p.Position == pos);

        if (!string.IsNullOrWhiteSpace(filter.League))
            query = query.Where(p => p.League == filter.League);

        if (!string.IsNullOrWhiteSpace(filter.Country))
            query = query.Where(p => p.Country == filter.Country);

        var items = await query
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.CreatedTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new PlayerListItemVm
            {
                Id = p.Id,
                Name = p.Name!,
                Slug = p.Slug!,
                Age = p.Age,
                Position = p.Position.ToString(),
                Team = p.Team!,
                League = p.League!,
                Country = p.Country!,
                ImageUrl = p.ImageUrl,
                Score = p.Score,
                AnalysisCount = _db.Analyses.Count(a =>
                    a.PlayerId == p.Id && a.Status == AnalysisStatus.Approved)
            })
            .ToListAsync(ct);

        return ServiceListResult<PlayerListItemVm>.Ok(items);
    }

    public async Task<ServiceListResult<PlayerListItemVm>> GetTopAsync(
        int count = 10, CancellationToken ct = default)
    {
        var items = await _db.Players
            .AsNoTracking()
            .Where(p => p.Status == PlayerStatus.Approved)
            .OrderByDescending(p => p.Score)
            .Take(count)
            .Select(p => new PlayerListItemVm
            {
                Id = p.Id,
                Name = p.Name!,
                Slug = p.Slug!,
                Age = p.Age,
                Position = p.Position.ToString(),
                Team = p.Team!,
                League = p.League!,
                Country = p.Country!,
                ImageUrl = p.ImageUrl,
                Score = p.Score,
                AnalysisCount = _db.Analyses.Count(a =>
                    a.PlayerId == p.Id && a.Status == AnalysisStatus.Approved)
            })
            .ToListAsync(ct);

        return ServiceListResult<PlayerListItemVm>.Ok(items);
    }

    public async Task<ServiceResult<PlayerDetailVm>> GetDetailAsync(
        string slug, Guid? viewerUserId = null, CancellationToken ct = default)
    {
        var player = await _db.Players
            .AsNoTracking()
            .Where(p => p.Slug == slug && p.Status == PlayerStatus.Approved)
            .Select(p => new PlayerDetailVm
            {
                Id = p.Id,
                Name = p.Name!,
                Slug = p.Slug!,
                Age = p.Age,
                Position = p.Position.ToString(),
                Team = p.Team!,
                League = p.League!,
                Country = p.Country!,
                ImageUrl = p.ImageUrl,
                Score = p.Score,
                CreatedAt = p.CreatedTime,
                SuggestedByUsername = _db.Users
                    .Where(u => u.Id == p.CreatedUserId)
                    .Select(u => u.Username)
                    .FirstOrDefault() ?? "anonymous",
                TransfermarktUrl = p.TransfermarktUrl,
                MarketValue = p.MarketValue,
                PreviousMarketValue = p.PreviousMarketValue,
                LastTransfermarktSync = p.LastTransfermarktSync
            })
            .FirstOrDefaultAsync(ct);

        if (player is null)
            return ServiceResult<PlayerDetailVm>.NotFound();

        player.Analyses = await (
            from a in _db.Analyses.AsNoTracking()
            where a.PlayerId == player.Id && a.Status == AnalysisStatus.Approved
            join u in _db.Users.AsNoTracking() on a.CreatedUserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            orderby a.LikeCount descending, a.AIScore descending, a.CreatedTime descending
            select new AnalysisVm
            {
                Id = a.Id,
                VideoUrl = a.VideoUrl!,
                Content = a.Content!,
                TechnicalContent = a.TechnicalContent,
                TacticalContent = a.TacticalContent,
                PhysicalContent = a.PhysicalContent,
                StrengthsContent = a.StrengthsContent,
                WeaknessesContent = a.WeaknessesContent,
                FilledSectionsCount = a.FilledSectionsCount,
                AISummary = a.AISummary,
                AIScore = a.AIScore,
                IsFlaggedAsDuplicate = a.IsFlaggedAsDuplicate,
                LikeCount = a.LikeCount,
                ScoutUsername = u != null ? u.Username! : "anonymous",
                ScoutId = a.CreatedUserId ?? Guid.Empty,
                ScoutLevel = u != null ? u.Level : default,
                CreatedAt = a.CreatedTime
            })
            .ToListAsync(ct);

        // Mark which analyses the viewer has liked
        if (viewerUserId.HasValue && player.Analyses.Count > 0)
        {
            var analysisIds = player.Analyses.Select(a => a.Id).ToList();
            var liked = await _db.AnalysisLikes
                .AsNoTracking()
                .Where(l => l.UserId == viewerUserId && analysisIds.Contains(l.AnalysisId))
                .Select(l => l.AnalysisId)
                .ToListAsync(ct);
            var likedSet = liked.ToHashSet();
            foreach (var a in player.Analyses)
                a.IsLikedByCurrentUser = likedSet.Contains(a.Id);
        }

        var votes = await _db.Votes
            .AsNoTracking()
            .Where(v => v.PlayerId == player.Id)
            .GroupBy(v => v.VoteType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        player.UpvoteCount = votes.FirstOrDefault(v => v.Type == VoteType.Up)?.Count ?? 0;
        player.DownvoteCount = votes.FirstOrDefault(v => v.Type == VoteType.Down)?.Count ?? 0;

        // Sezon istatistiklerini JSON'dan deserialize et
        var rawPlayer = await _db.Players.AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => p.SeasonStatsJson)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrEmpty(rawPlayer))
        {
            var stats = System.Text.Json.JsonSerializer
                .Deserialize<List<PlayerSeasonStats>>(rawPlayer);
            if (stats is not null)
                player.SeasonStats = stats.Select(s => new TmSeasonStatVm
                {
                    Season = s.Season,
                    Matches = s.Matches,
                    Goals = s.Goals,
                    Assists = s.Assists
                }).ToList();
        }

        return ServiceResult<PlayerDetailVm>.Ok(player);
    }

    public async Task<ServiceResult> SyncFromTransfermarktAsync(
        Guid playerId, CancellationToken ct = default)
    {
        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId, ct);
        if (player is null)
            return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        if (string.IsNullOrEmpty(player.TransfermarktId))
            return ServiceResult.Fail(ErrorCodes.TRANSFERMARKT_INVALID_URL);

        var data = await _tm.ScrapePlayerAsync(player.TransfermarktId, ct);
        if (data is null)
            return ServiceResult.Fail("TRANSFERMARKT_SCRAPE_FAILED");

        player.UpdateFromTransfermarkt(data);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceListResult<PendingPlayerVm>> GetPendingAsync(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var items = await (
            from p in _db.Players.AsNoTracking()
            where p.Status == PlayerStatus.Pending
            join u in _db.Users.AsNoTracking() on p.CreatedUserId equals u.Id into users
            from u in users.DefaultIfEmpty()
            join a in _db.Analyses.AsNoTracking() on p.Id equals a.PlayerId into analyses
            from a in analyses.OrderByDescending(x => x.CreatedTime).Take(1).DefaultIfEmpty()
            orderby p.CreatedTime descending
            select new PendingPlayerVm
            {
                Id = p.Id,
                Name = p.Name!,
                Age = p.Age,
                Position = p.Position.ToString(),
                Team = p.Team!,
                League = p.League!,
                Country = p.Country!,
                SuggestedByUsername = u != null ? u.Username! : "anonymous",
                VideoUrl = a != null ? a.VideoUrl! : "",
                AnalysisContent = a != null ? a.Content! : "",
                AIScore = a != null ? a.AIScore : null,
                AISummary = a != null ? a.AISummary : null,
                SubmittedAt = p.CreatedTime
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return ServiceListResult<PendingPlayerVm>.Ok(items);
    }

    public async Task<ServiceResult> SuggestAsync(
        SuggestPlayerInput input, Guid userId, CancellationToken ct = default)
    {
        if (!Enum.TryParse<PlayerPosition>(input.Position, out var position))
            return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE);

        // ── Transfermarkt deduplication ───────────────────────────────────────
        string? tmId = null;
        if (!string.IsNullOrWhiteSpace(input.TransfermarktUrl))
        {
            tmId = _tm.ExtractTmId(input.TransfermarktUrl);
            if (tmId is null)
                return ServiceResult.Fail(ErrorCodes.TRANSFERMARKT_INVALID_URL);

            var exists = await _db.Players
                .AsNoTracking()
                .AnyAsync(p => p.TransfermarktId == tmId, ct);

            if (exists)
                return ServiceResult.Fail(ErrorCodes.PLAYER_DUPLICATE_TRANSFERMARKT_ID);
        }

        var playerResult = Player.Create(
            input.Name, input.Age, position,
            input.Team, input.League, input.Country, userId);

        if (!playerResult.IsSuccess)
            return ServiceResult.Fail(playerResult.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        var player = playerResult.Data!;

        if (!string.IsNullOrWhiteSpace(input.ImageUrl))
            player.SetImageUrl(input.ImageUrl);

        if (tmId is not null)
            player.SetTransfermarkt(tmId, input.TransfermarktUrl!);

        var analysisResult = Analysis.Create(player.Id, input.VideoUrl, input.AnalysisContent, userId,
            technical: null, tactical: null, physical: null, strengths: null, weaknesses: null);
        if (!analysisResult.IsSuccess)
            return ServiceResult.Fail(analysisResult.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        await _db.Players.AddAsync(player, ct);
        await _db.Analyses.AddAsync(analysisResult.Data!, ct);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ApproveAsync(Guid playerId, CancellationToken ct = default)
    {
        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId, ct);
        if (player is null) return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        var approveResult = player.Approve();
        if (!approveResult.IsSuccess)
            return ServiceResult.Fail(approveResult.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        var pendingAnalyses = await _db.Analyses
            .Where(a => a.PlayerId == playerId && a.Status == AnalysisStatus.Pending)
            .ToListAsync(ct);

        foreach (var analysis in pendingAnalyses)
        {
            analysis.Approve();
            var scout = await _db.Users.FirstOrDefaultAsync(u => u.Id == analysis.CreatedUserId, ct);
            scout?.IncrementApprovedAnalysisCount();
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetPlayerImageAsync(
        Guid playerId, string imageUrl, CancellationToken ct = default)
    {
        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId, ct);
        if (player is null) return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        player.SetImageUrl(imageUrl);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RejectAsync(
        Guid playerId, string reason, CancellationToken ct = default)
    {
        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId, ct);
        if (player is null) return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        var result = player.Reject(reason);
        if (!result.IsSuccess)
            return ServiceResult.Fail(result.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
