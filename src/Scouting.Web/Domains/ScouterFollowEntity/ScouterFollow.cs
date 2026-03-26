using scommon;
using Scouting.Web.Shared;

namespace Scouting.Web.Domains.ScouterFollowEntity;

public class ScouterFollow : BaseModel
{
    private ScouterFollow() { }

    public Guid FollowerId { get; private set; }
    public Guid ScouterId { get; private set; }

    public static ResultDomain<ScouterFollow> Create(Guid followerId, Guid scouterId)
    {
        if (followerId == scouterId)
            return ResultDomain<ScouterFollow>.Error(new MessageItem
            {
                Code = ErrorCodes.FOLLOW_CANNOT_FOLLOW_YOURSELF,
                Property = nameof(followerId),
                Table = nameof(ScouterFollow)
            });

        return ResultDomain<ScouterFollow>.Ok(new ScouterFollow { FollowerId = followerId, ScouterId = scouterId });
    }
}
