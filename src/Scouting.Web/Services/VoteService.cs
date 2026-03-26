using Microsoft.EntityFrameworkCore;
using Scouting.Web.Domains.PlayerEntity;
using Scouting.Web.Domains.VoteEntity;
using Scouting.Web.Infrastructure;
using Scouting.Web.Shared;
using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public class VoteService : IVoteService
{
    private readonly AppDbContext _db;

    public VoteService(AppDbContext db) => _db = db;

    public async Task<ServiceResult> VoteAsync(
        Guid playerId, Guid userId, string voteType, CancellationToken ct = default)
    {
        if (!Enum.TryParse<VoteType>(voteType, out var type))
            return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE);

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == playerId, ct);
        if (player is null) return ServiceResult.Fail(ErrorCodes.COMMON_MESSAGE_RECORD_NOT_FOUND);

        if (player.Status != PlayerStatus.Approved)
            return ServiceResult.Fail(ErrorCodes.VOTE_PLAYER_NOT_APPROVED);

        var existing = await _db.Votes
            .FirstOrDefaultAsync(v => v.PlayerId == playerId && v.UserId == userId, ct);

        if (existing is null)
        {
            await _db.Votes.AddAsync(Vote.Create(playerId, userId, type), ct);
            player.UpdateScore(type == VoteType.Up ? 1 : -1);
        }
        else if (existing.VoteType != type)
        {
            player.UpdateScore(existing.VoteType == VoteType.Up ? -2 : 2);
            existing.ChangeVote(type);
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<string?> GetUserVoteAsync(Guid playerId, Guid userId, CancellationToken ct = default)
    {
        var vote = await _db.Votes
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.PlayerId == playerId && v.UserId == userId, ct);
        return vote?.VoteType.ToString();
    }
}
