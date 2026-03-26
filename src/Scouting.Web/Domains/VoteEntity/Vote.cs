using scommon;

namespace Scouting.Web.Domains.VoteEntity;

public class Vote : BaseModel
{
    private Vote() { }

    public Guid PlayerId { get; private set; }
    public Guid UserId { get; private set; }
    public VoteType VoteType { get; private set; }

    public static Vote Create(Guid playerId, Guid userId, VoteType voteType) =>
        new() { PlayerId = playerId, UserId = userId, VoteType = voteType };

    public void ChangeVote(VoteType newType) => VoteType = newType;
}
