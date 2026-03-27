using Scouting.Web.Domains.UserEntity;

namespace Scouting.Web.Models;

// ── Players ───────────────────────────────────────────────────────────────────

public class PlayerListItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int Age { get; set; }
    public string Position { get; set; } = "";
    public string Team { get; set; } = "";
    public string League { get; set; } = "";
    public string Country { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Score { get; set; }
    public int AnalysisCount { get; set; }
}

public class PlayerDetailVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int Age { get; set; }
    public string Position { get; set; } = "";
    public string Team { get; set; } = "";
    public string League { get; set; } = "";
    public string Country { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int Score { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public string SuggestedByUsername { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<AnalysisVm> Analyses { get; set; } = [];

    // Transfermarkt
    public string? TransfermarktUrl { get; set; }
    public decimal? MarketValue { get; set; }
    public decimal? PreviousMarketValue { get; set; }
    public DateTime? LastTransfermarktSync { get; set; }
    public List<TmSeasonStatVm> SeasonStats { get; set; } = [];
}

public class TmSeasonStatVm
{
    public string Season { get; set; } = "";
    public int Matches { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
}

public class AnalysisVm
{
    public Guid Id { get; set; }
    public string VideoUrl { get; set; } = "";

    // Sections
    public string Content { get; set; } = ""; // General (required)
    public string? TechnicalContent { get; set; }
    public string? TacticalContent { get; set; }
    public string? PhysicalContent { get; set; }
    public string? StrengthsContent { get; set; }
    public string? WeaknessesContent { get; set; }
    public int FilledSectionsCount { get; set; }

    // Quality / AI
    public string? AISummary { get; set; }
    public decimal? AIScore { get; set; }
    public bool IsFlaggedAsDuplicate { get; set; }

    // Scout
    public string ScoutUsername { get; set; } = "";
    public Guid ScoutId { get; set; }
    public UserLevel ScoutLevel { get; set; }
    public DateTime CreatedAt { get; set; }

    // Likes
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}

public class PendingPlayerVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Position { get; set; } = "";
    public string Team { get; set; } = "";
    public string League { get; set; } = "";
    public string Country { get; set; } = "";
    public string SuggestedByUsername { get; set; } = "";
    public string VideoUrl { get; set; } = "";
    public string AnalysisContent { get; set; } = "";
    public decimal? AIScore { get; set; }
    public string? AISummary { get; set; }
    public DateTime SubmittedAt { get; set; }
}

// ── Analyses ──────────────────────────────────────────────────────────────────

public class RecentAnalysisVm
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public string PlayerSlug { get; set; } = "";
    public string? PlayerImageUrl { get; set; }
    public string? PlayerPosition { get; set; }
    public string VideoUrl { get; set; } = "";
    public string ContentPreview { get; set; } = "";
    public string? AISummary { get; set; }
    public decimal? AIScore { get; set; }
    public string ScoutUsername { get; set; } = "";
    public Guid ScoutId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Scouters ──────────────────────────────────────────────────────────────────

public class ScouterVm
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public int ApprovedAnalysisCount { get; set; }
    public int TotalLikesReceived { get; set; }
    public int FollowerCount { get; set; }
    public UserLevel Level { get; set; }
}

public class ScouterProfileVm : ScouterVm
{
    public bool IsFollowedByViewer { get; set; }
    public List<ScouterAnalysisVm> RecentAnalyses { get; set; } = new();
}

public class ScouterAnalysisVm
{
    public Guid Id { get; set; }
    public string PlayerName { get; set; } = "";
    public string PlayerSlug { get; set; } = "";
    public string? PlayerImageUrl { get; set; }
    public string ContentPreview { get; set; } = "";
    public decimal? AIScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PendingAnalysisVm
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public string PlayerSlug { get; set; } = "";
    public string SuggestedByUsername { get; set; } = "";
    public Guid SuggestedByUserId { get; set; }
    public string VideoUrl { get; set; } = "";
    public string ContentPreview { get; set; } = "";
    public int FilledSectionsCount { get; set; }
    public bool IsFlaggedAsDuplicate { get; set; }
    public decimal? QualityScore { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class MyAnalysisVm
{
    public Guid Id { get; set; }
    public string PlayerName { get; set; } = "";
    public string PlayerSlug { get; set; } = "";
    public string? PlayerImageUrl { get; set; }
    public string ContentPreview { get; set; } = "";
    public decimal? AIScore { get; set; }
    public string? AISummary { get; set; }
    public int LikeCount { get; set; }
    public string Status { get; set; } = "";
    public bool IsFlaggedAsDuplicate { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Auth ──────────────────────────────────────────────────────────────────────

public class PlayerFilter
{
    public string? Search { get; set; }
    public string? Position { get; set; }
    public string? League { get; set; }
    public string? Country { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SuggestPlayerInput
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Position { get; set; } = "";
    public string Team { get; set; } = "";
    public string League { get; set; } = "";
    public string Country { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string VideoUrl { get; set; } = "";
    public string AnalysisContent { get; set; } = "";

    /// <summary>Optional: full Transfermarkt profile URL, e.g. https://www.transfermarkt.com.tr/.../spieler/990148</summary>
    public string? TransfermarktUrl { get; set; }
}
