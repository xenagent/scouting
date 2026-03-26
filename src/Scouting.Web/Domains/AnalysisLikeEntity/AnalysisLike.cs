using scommon;
using Scouting.Web.Shared;

namespace Scouting.Web.Domains.AnalysisLikeEntity;

public class AnalysisLike : BaseModel
{
    private AnalysisLike() { }

    public Guid AnalysisId { get; private set; }
    public Guid UserId { get; private set; }

    public static ResultDomain<AnalysisLike> Create(Guid analysisId, Guid userId)
    {
        if (analysisId == Guid.Empty || userId == Guid.Empty)
            return ResultDomain<AnalysisLike>.Error(new MessageItem
                { Code = ErrorCodes.COMMON_MESSAGE_INVALID_VALUE, Property = nameof(analysisId), Table = nameof(AnalysisLike) });

        return ResultDomain<AnalysisLike>.Ok(new AnalysisLike
        {
            AnalysisId = analysisId,
            UserId = userId
        });
    }
}
