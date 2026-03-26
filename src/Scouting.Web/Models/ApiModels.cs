namespace Scouting.Web.Models;

public class ApiResult
{
    public bool IsSuccess { get; set; }
    public List<ApiMessage>? Messages { get; set; }
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }
}

public class ApiListResult<T> : ApiResult
{
    public List<T> Data { get; set; } = [];
}

public class ApiMessage
{
    public string? Code { get; set; }
    public string? Property { get; set; }
    public string? Table { get; set; }
}

// Auth
public class LoginRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
public class RegisterRequest { public string Email { get; set; } = ""; public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
public class AuthResponse { public string Token { get; set; } = ""; public Guid UserId { get; set; } public string Username { get; set; } = ""; public string Role { get; set; } = ""; public DateTime ExpiresAt { get; set; } }

// Players
public class PlayerListItem
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

public class PlayerDetail
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
    public List<AnalysisItem> Analyses { get; set; } = [];
}

public class AnalysisItem
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

public class PendingPlayerItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
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

// Analyses
public class RecentAnalysis
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

// Scouters
public class ScouterItem
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public int ApprovedAnalysisCount { get; set; }
    public int FollowerCount { get; set; }
}

public class ScouterProfile : ScouterItem
{
    public bool IsFollowedByViewer { get; set; }
    public List<ScouterAnalysis> RecentAnalyses { get; set; } = [];
}

public class ScouterAnalysis
{
    public Guid Id { get; set; }
    public string PlayerName { get; set; } = "";
    public string PlayerSlug { get; set; } = "";
    public string? PlayerImageUrl { get; set; }
    public string ContentPreview { get; set; } = "";
    public decimal? AIScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
