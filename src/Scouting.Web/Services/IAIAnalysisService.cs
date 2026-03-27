namespace Scouting.Web.Services;

/// <summary>
/// Input model for AI analysis evaluation.
/// Will be sent to AWS Bedrock (Claude Opus) when integration is configured.
/// </summary>
public class AIAnalysisInput
{
    public string PlayerName { get; set; } = "";
    public string PlayerPosition { get; set; } = "";
    public string VideoUrl { get; set; } = "";

    // Required section
    public string GeneralContent { get; set; } = "";

    // Optional sections (breadth indicators)
    public string? TechnicalContent { get; set; }
    public string? TacticalContent { get; set; }
    public string? PhysicalContent { get; set; }
    public string? StrengthsContent { get; set; }
    public string? WeaknessesContent { get; set; }

    /// <summary>
    /// Existing approved analyses for the same player.
    /// Used by AI to detect copy-paste / plagiarism.
    /// </summary>
    public List<string> ExistingAnalysesContent { get; set; } = [];
}

/// <summary>
/// Result returned by the AI evaluation pipeline.
/// Score = OriginalityScore (0-5) + DepthScore (0-5), max 10.
/// </summary>
public class AIEvaluationResult
{
    /// <summary>False when stub is active (AWS Bedrock not yet configured).</summary>
    public bool IsAvailable { get; set; }

    /// <summary>Originality vs existing analyses, 0–5.</summary>
    public decimal OriginalityScore { get; set; }

    /// <summary>Depth/detail score based on content richness and reading time, 0–5.</summary>
    public decimal DepthScore { get; set; }

    /// <summary>Total quality score 0–10 (OriginalityScore + DepthScore).</summary>
    public decimal Score => OriginalityScore + DepthScore;

    /// <summary>Estimated reading time in minutes, based on content length and density.</summary>
    public decimal EstimatedReadingMinutes { get; set; }

    /// <summary>AI-generated 2-3 sentence Turkish summary of the analysis.</summary>
    public string? Summary { get; set; }

    /// <summary>True if the content is highly similar to an existing analysis.</summary>
    public bool IsPossibleDuplicate { get; set; }

    /// <summary>Human-readable duplicate warning shown to admin.</summary>
    public string? DuplicateWarning { get; set; }
}

/// <summary>
/// Analysis AI evaluation service.
/// Production implementation: AWS Bedrock → Claude Opus (anthropic.claude-opus-4-6-v1).
/// Development: stub that computes a rule-based quality score.
/// </summary>
public interface IAIAnalysisService
{
    Task<AIEvaluationResult> EvaluateAsync(AIAnalysisInput input, CancellationToken ct = default);
}
