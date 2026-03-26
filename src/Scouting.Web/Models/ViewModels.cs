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
}

public class AnalysisVm
{
    public Guid Id { get; set; }
    public string VideoUrl { get; set; } = "";
    public string Content { get; set; } = "";
    public string? AISummary { get; set; }
    public decimal? AIScore { get; set; }
    public string ScoutUsername { get; set; } = "";
    public Guid ScoutId { get; set; }
    public DateTime CreatedAt { get; set; }
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
    public int FollowerCount { get; set; }
}

public class ScouterProfileVm : ScouterVm
{
    public bool IsFollowedByViewer { get; set; }
    public List<ScouterAnalysisVm> RecentAnalyses { get; set; } = [];
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
}
