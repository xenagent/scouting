using scommon;

namespace Scouting.API.Domains.AnalysisEntity;

public class Analysis : BaseUserTrackModel
{
    private Analysis() { }

    public Guid PlayerId { get; private set; }
    public string? VideoUrl { get; private set; }
    public string? Content { get; private set; }
    public string? AISummary { get; private set; }
    public decimal? AIScore { get; private set; }
    public AnalysisStatus Status { get; private set; } = AnalysisStatus.Pending;
    public string? RejectionReason { get; private set; }

    public static ResultDomain<Analysis> Create(
        Guid playerId, string videoUrl, string content, Guid scoutUserId)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            return ResultDomain<Analysis>.Error(new MessageItem
            {
                Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY,
                Property = nameof(videoUrl),
                Table = nameof(Analysis)
            });

        if (string.IsNullOrWhiteSpace(content))
            return ResultDomain<Analysis>.Error(new MessageItem
            {
                Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY,
                Property = nameof(content),
                Table = nameof(Analysis)
            });

        if (content.Length < 50)
            return ResultDomain<Analysis>.Error(new MessageItem
            {
                Code = ErrorCodes.ANALYSIS_CONTENT_TOO_SHORT,
                Property = nameof(content),
                Table = nameof(Analysis)
            });

        if (content.Length > 5000)
            return ResultDomain<Analysis>.Error(new MessageItem
            {
                Code = ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR,
                Property = nameof(content),
                Table = nameof(Analysis)
            });

        return ResultDomain<Analysis>.Ok(new Analysis
        {
            PlayerId = playerId,
            VideoUrl = videoUrl.Trim(),
            Content = content.Trim(),
            Status = AnalysisStatus.Pending,
            CreatedUserId = scoutUserId
        });
    }

    public ResultDomain Approve()
    {
        Status = AnalysisStatus.Approved;
        RejectionReason = null;
        return ResultDomain.Ok();
    }

    public ResultDomain Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ResultDomain.Error(new MessageItem
            {
                Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY,
                Property = nameof(reason),
                Table = nameof(Analysis)
            });

        Status = AnalysisStatus.Rejected;
        RejectionReason = reason;
        return ResultDomain.Ok();
    }

    public void SetAIReview(string summary, decimal score)
    {
        AISummary = summary;
        AIScore = score;
    }
}
