using scommon;
using Scouting.Web.Shared;

namespace Scouting.Web.Domains.AnalysisEntity;

public class Analysis : BaseUserTrackModel
{
    private Analysis() { }

    public Guid PlayerId { get; private set; }
    public string? VideoUrl { get; private set; }

    // ── Main content (required) ───────────────────────────────────────────────
    public string? Content { get; private set; }

    // ── Optional structured sections ─────────────────────────────────────────
    public string? TechnicalContent { get; private set; }    // Teknik Beceriler
    public string? TacticalContent { get; private set; }     // Taktiksel Katkı
    public string? PhysicalContent { get; private set; }     // Fiziksel Özellikler
    public string? StrengthsContent { get; private set; }    // Güçlü Yönler
    public string? WeaknessesContent { get; private set; }   // Geliştirmesi Gerekenler

    // ── Quality metrics ───────────────────────────────────────────────────────
    public int FilledSectionsCount { get; private set; }     // 0-5 optional sections
    public int TotalContentLength { get; private set; }      // sum of all section lengths

    // ── AI / Quality score ────────────────────────────────────────────────────
    public string? AISummary { get; private set; }
    public decimal? AIScore { get; private set; }            // 0-10
    public bool IsFlaggedAsDuplicate { get; private set; }

    // ── Likes (denormalized count) ────────────────────────────────────────────
    public int LikeCount { get; private set; }

    public AnalysisStatus Status { get; private set; } = AnalysisStatus.Pending;
    public string? RejectionReason { get; private set; }

    public static ResultDomain<Analysis> Create(
        Guid playerId,
        string videoUrl,
        string general,
        Guid scoutUserId,
        string? technical = null,
        string? tactical = null,
        string? physical = null,
        string? strengths = null,
        string? weaknesses = null)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            return ResultDomain<Analysis>.Error(new MessageItem
                { Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY, Property = nameof(videoUrl), Table = nameof(Analysis) });

        if (string.IsNullOrWhiteSpace(general))
            return ResultDomain<Analysis>.Error(new MessageItem
                { Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY, Property = nameof(general), Table = nameof(Analysis) });

        if (general.Length < 50)
            return ResultDomain<Analysis>.Error(new MessageItem
                { Code = ErrorCodes.ANALYSIS_CONTENT_TOO_SHORT, Property = nameof(general), Table = nameof(Analysis) });

        if (general.Length > 5000)
            return ResultDomain<Analysis>.Error(new MessageItem
                { Code = ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR, Property = nameof(general), Table = nameof(Analysis) });

        var sections = new[] { technical, tactical, physical, strengths, weaknesses };
        var filledSections = sections.Count(s => !string.IsNullOrWhiteSpace(s));
        var totalLength = general.Length + sections
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Sum(s => s!.Length);

        return ResultDomain<Analysis>.Ok(new Analysis
        {
            PlayerId = playerId,
            VideoUrl = videoUrl.Trim(),
            Content = general.Trim(),
            TechnicalContent = NullIfEmpty(technical),
            TacticalContent = NullIfEmpty(tactical),
            PhysicalContent = NullIfEmpty(physical),
            StrengthsContent = NullIfEmpty(strengths),
            WeaknessesContent = NullIfEmpty(weaknesses),
            FilledSectionsCount = filledSections,
            TotalContentLength = totalLength,
            Status = AnalysisStatus.Pending,
            CreatedUserId = scoutUserId
        });
    }

    public ResultDomain Approve() { Status = AnalysisStatus.Approved; RejectionReason = null; return ResultDomain.Ok(); }

    public ResultDomain Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ResultDomain.Error(new MessageItem
                { Code = ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY, Property = nameof(reason), Table = nameof(Analysis) });
        Status = AnalysisStatus.Rejected;
        RejectionReason = reason;
        return ResultDomain.Ok();
    }

    public void SetAIReview(string summary, decimal score, bool isDuplicate = false)
    {
        AISummary = summary;
        AIScore = score;
        IsFlaggedAsDuplicate = isDuplicate;
    }

    public void IncrementLikeCount() => LikeCount++;
    public void DecrementLikeCount() { if (LikeCount > 0) LikeCount--; }

    private static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
