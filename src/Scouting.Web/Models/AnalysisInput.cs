namespace Scouting.Web.Models;

/// <summary>
/// Structured input for submitting an analysis.
/// General is required (min 50 chars). All other sections are optional.
/// More filled sections → higher quality breadth score.
/// </summary>
public class AnalysisInput
{
    public string VideoUrl { get; set; } = "";

    /// <summary>Genel Yorum — required, min 50 chars</summary>
    public string General { get; set; } = "";

    /// <summary>Teknik Beceriler — dribbling, first touch, passing, finishing…</summary>
    public string? Technical { get; set; }

    /// <summary>Taktiksel Katkı — pressing, positioning, movement patterns…</summary>
    public string? Tactical { get; set; }

    /// <summary>Fiziksel Özellikler — pace, strength, stamina, aerial ability…</summary>
    public string? Physical { get; set; }

    /// <summary>Güçlü Yönler — what makes this player stand out</summary>
    public string? Strengths { get; set; }

    /// <summary>Geliştirmesi Gerekenler — areas needing improvement</summary>
    public string? Weaknesses { get; set; }

    public int TotalLength =>
        (General?.Length ?? 0) + (Technical?.Length ?? 0) + (Tactical?.Length ?? 0)
        + (Physical?.Length ?? 0) + (Strengths?.Length ?? 0) + (Weaknesses?.Length ?? 0);

    public int FilledSectionsCount => new[] { Technical, Tactical, Physical, Strengths, Weaknesses }
        .Count(s => !string.IsNullOrWhiteSpace(s));
}
