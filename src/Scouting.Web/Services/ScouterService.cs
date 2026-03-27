using Microsoft.EntityFrameworkCore;
using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Domains.ScouterFollowEntity;
using Scouting.Web.Infrastructure;
using Scouting.Web.Models;
using Scouting.Web.Shared;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public class ScouterService : IScouterService
{
    private readonly AppDbContext _db;

    public ScouterService(AppDbContext db) => _db = db;

    public async Task<ServiceListResult<ScouterVm>> GetTopAsync(
        int count = 10, CancellationToken ct = default)
    {
        var items = await _db.Users
            .AsNoTracking()
            .Where(u => u.ApprovedAnalysisCount > 0)
            .OrderByDescending(u => u.ApprovedAnalysisCount)
            .ThenByDescending(u => u.FollowerCount)
            .Take(count)
            .Select(u => new ScouterVm
            {
                Id = u.Id,
                Username = u.Username!,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                ApprovedAnalysisCount = u.ApprovedAnalysisCount,
                TotalLikesReceived = u.TotalLikesReceived,
                BonusPoints = u.BonusPoints,
                FollowerCount = u.FollowerCount,
                Level = u.Level
            })
            .ToListAsync(ct);

        return ServiceListResult<ScouterVm>.Ok(items);
    }

    public async Task<ServiceResult<ScouterProfileVm>> GetProfileAsync(
        string username, Guid? viewerUserId = null, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Username == username)
            .Select(u => new ScouterProfileVm
            {
                Id = u.Id,
                Username = u.Username!,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                ApprovedAnalysisCount = u.ApprovedAnalysisCount,
                TotalLikesReceived = u.TotalLikesReceived,
                BonusPoints = u.BonusPoints,
                FollowerCount = u.FollowerCount,
                Level = u.Level
            })
            .FirstOrDefaultAsync(ct);

        if (user is null) return ServiceResult<ScouterProfileVm>.NotFound();

        if (viewerUserId.HasValue)
            user.IsFollowedByViewer = await _db.ScouterFollows
                .AnyAsync(f => f.FollowerId == viewerUserId && f.ScouterId == user.Id, ct);

        user.RecentAnalyses = await (
            from a in _db.Analyses.AsNoTracking()
            where a.CreatedUserId == user.Id && a.Status == AnalysisStatus.Approved
            join p in _db.Players.AsNoTracking() on a.PlayerId equals p.Id
            orderby a.CreatedTime descending
            select new ScouterAnalysisVm
            {
                Id = a.Id,
                PlayerName = p.Name!,
                PlayerSlug = p.Slug!,
                PlayerImageUrl = p.ImageUrl,
                ContentPreview = a.Content!.Length > 150
                    ? a.Content.Substring(0, 150) + "..."
                    : a.Content,
                AIScore = a.AIScore,
                CreatedAt = a.CreatedTime
            })
            .Take(5)
            .ToListAsync(ct);

        return ServiceResult<ScouterProfileVm>.Ok(user);
    }

    public async Task<ServiceResult> FollowAsync(
        Guid followerId, Guid scouterId, CancellationToken ct = default)
    {
        var result = ScouterFollow.Create(followerId, scouterId);
        if (!result.IsSuccess)
            return ServiceResult.Fail(result.Messages?.FirstOrDefault()?.Code ?? "UNKNOWN");

        var alreadyFollowing = await _db.ScouterFollows
            .AnyAsync(f => f.FollowerId == followerId && f.ScouterId == scouterId, ct);
        if (alreadyFollowing)
            return ServiceResult.Fail(ErrorCodes.FOLLOW_ALREADY_FOLLOWING);

        var scouter = await _db.Users.FirstOrDefaultAsync(u => u.Id == scouterId, ct);
        if (scouter is null) return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        await _db.ScouterFollows.AddAsync(result.Data!, ct);
        scouter.IncrementFollowerCount();
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnfollowAsync(
        Guid followerId, Guid scouterId, CancellationToken ct = default)
    {
        var follow = await _db.ScouterFollows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.ScouterId == scouterId, ct);
        if (follow is null)
            return ServiceResult.Fail(ErrorCodes.FOLLOW_NOT_FOLLOWING);

        var scouter = await _db.Users.FirstOrDefaultAsync(u => u.Id == scouterId, ct);
        _db.ScouterFollows.Remove(follow);
        scouter?.DecrementFollowerCount();
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAvatarAsync(
        Guid userId, string avatarUrl, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        user.SetAvatar(avatarUrl);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceListResult<ScouterVm>> GetFollowingAsync(
        Guid userId, CancellationToken ct = default)
    {
        var items = await (
            from f in _db.ScouterFollows.AsNoTracking()
            where f.FollowerId == userId
            join u in _db.Users.AsNoTracking() on f.ScouterId equals u.Id
            select new ScouterVm
            {
                Id = u.Id,
                Username = u.Username!,
                AvatarUrl = u.AvatarUrl,
                ApprovedAnalysisCount = u.ApprovedAnalysisCount,
                TotalLikesReceived = u.TotalLikesReceived,
                BonusPoints = u.BonusPoints,
                FollowerCount = u.FollowerCount,
                Level = u.Level
            })
            .ToListAsync(ct);

        return ServiceListResult<ScouterVm>.Ok(items);
    }
}
