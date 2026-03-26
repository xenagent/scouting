using Scouting.Web.Shared.Results;

namespace Scouting.Web.Services;

public interface IVoteService
{
    Task<ServiceResult> VoteAsync(Guid playerId, Guid userId, string voteType, CancellationToken ct = default);
    Task<string?> GetUserVoteAsync(Guid playerId, Guid userId, CancellationToken ct = default);
}
