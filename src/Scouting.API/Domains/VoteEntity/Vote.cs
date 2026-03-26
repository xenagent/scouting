using scommon;

namespace Scouting.API.Domains.VoteEntity;

public class Vote : BaseModel
{
    private Vote() { }

    public Guid PlayerId { get; private set; }
    public Guid UserId { get; private set; }
    public VoteType VoteType { get; private set; }

    public static Vote Create(Guid playerId, Guid userId, VoteType voteType)
        => new Vote
        {
            PlayerId = playerId,
            UserId = userId,
            VoteType = voteType
        };

    public void ChangeVote(VoteType newVoteType) => VoteType = newVoteType;
}
