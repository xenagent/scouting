using System.Net.Http.Headers;
using System.Net.Http.Json;
using Scouting.Web.Models;

namespace Scouting.Web.Services;

public interface IScoutingApiService
{
    void SetToken(string? token);

    // Auth
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResult<object>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    // Players
    Task<ApiListResult<PlayerListItem>> GetPlayersAsync(string? search = null, string? position = null, string? league = null, string? country = null, int page = 1, CancellationToken ct = default);
    Task<ApiListResult<PlayerListItem>> GetTopPlayersAsync(int count = 10, CancellationToken ct = default);
    Task<ApiResult<PlayerDetail>> GetPlayerDetailAsync(string slug, CancellationToken ct = default);
    Task<ApiListResult<PendingPlayerItem>> GetPendingPlayersAsync(int page = 1, CancellationToken ct = default);
    Task<ApiResult<object>> SuggestPlayerAsync(SuggestPlayerRequest request, CancellationToken ct = default);
    Task<ApiResult<object>> ApprovePlayerAsync(Guid playerId, CancellationToken ct = default);
    Task<ApiResult<object>> RejectPlayerAsync(Guid playerId, string reason, CancellationToken ct = default);

    // Analyses
    Task<ApiListResult<RecentAnalysis>> GetRecentAnalysesAsync(int count = 10, CancellationToken ct = default);
    Task<ApiResult<object>> ApproveAnalysisAsync(Guid analysisId, CancellationToken ct = default);
    Task<ApiResult<object>> RejectAnalysisAsync(Guid analysisId, string reason, CancellationToken ct = default);

    // Votes
    Task<ApiResult<object>> VoteAsync(Guid playerId, string voteType, CancellationToken ct = default);

    // Scouters
    Task<ApiListResult<ScouterItem>> GetTopScoutersAsync(int count = 10, CancellationToken ct = default);
    Task<ApiResult<ScouterProfile>> GetScouterProfileAsync(string username, CancellationToken ct = default);
    Task<ApiResult<object>> FollowScouterAsync(Guid scouterId, CancellationToken ct = default);
    Task<ApiResult<object>> UnfollowScouterAsync(Guid scouterId, CancellationToken ct = default);
    Task<ApiListResult<ScouterItem>> GetMyFollowingAsync(CancellationToken ct = default);
}

public class SuggestPlayerRequest
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

public class ScoutingApiService : IScoutingApiService
{
    private readonly HttpClient _http;

    public ScoutingApiService(HttpClient http)
    {
        _http = http;
    }

    public void SetToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = null;
        else
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        => await PostAsync<AuthResponse>("api/auth/login", request, ct);

    public async Task<ApiResult<object>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        => await PostAsync<object>("api/auth/register", request, ct);

    public async Task<ApiListResult<PlayerListItem>> GetPlayersAsync(string? search = null, string? position = null, string? league = null, string? country = null, int page = 1, CancellationToken ct = default)
    {
        var qs = BuildQueryString(("search", search), ("position", position), ("league", league), ("country", country), ("page", page.ToString()));
        return await GetListAsync<PlayerListItem>($"api/players{qs}", ct);
    }

    public async Task<ApiListResult<PlayerListItem>> GetTopPlayersAsync(int count = 10, CancellationToken ct = default)
        => await GetListAsync<PlayerListItem>($"api/players/top?count={count}", ct);

    public async Task<ApiResult<PlayerDetail>> GetPlayerDetailAsync(string slug, CancellationToken ct = default)
        => await GetAsync<PlayerDetail>($"api/players/{Uri.EscapeDataString(slug)}", ct);

    public async Task<ApiListResult<PendingPlayerItem>> GetPendingPlayersAsync(int page = 1, CancellationToken ct = default)
        => await GetListAsync<PendingPlayerItem>($"api/players/pending?page={page}", ct);

    public async Task<ApiResult<object>> SuggestPlayerAsync(SuggestPlayerRequest request, CancellationToken ct = default)
        => await PostAsync<object>("api/players", request, ct);

    public async Task<ApiResult<object>> ApprovePlayerAsync(Guid playerId, CancellationToken ct = default)
        => await PostAsync<object>($"api/players/{playerId}/approve", null, ct);

    public async Task<ApiResult<object>> RejectPlayerAsync(Guid playerId, string reason, CancellationToken ct = default)
        => await PostAsync<object>($"api/players/{playerId}/reject", new { reason }, ct);

    public async Task<ApiListResult<RecentAnalysis>> GetRecentAnalysesAsync(int count = 10, CancellationToken ct = default)
        => await GetListAsync<RecentAnalysis>($"api/analyses/recent?count={count}", ct);

    public async Task<ApiResult<object>> ApproveAnalysisAsync(Guid analysisId, CancellationToken ct = default)
        => await PostAsync<object>($"api/analyses/{analysisId}/approve", null, ct);

    public async Task<ApiResult<object>> RejectAnalysisAsync(Guid analysisId, string reason, CancellationToken ct = default)
        => await PostAsync<object>($"api/analyses/{analysisId}/reject", new { reason }, ct);

    public async Task<ApiResult<object>> VoteAsync(Guid playerId, string voteType, CancellationToken ct = default)
        => await PostAsync<object>("api/votes", new { playerId, voteType }, ct);

    public async Task<ApiListResult<ScouterItem>> GetTopScoutersAsync(int count = 10, CancellationToken ct = default)
        => await GetListAsync<ScouterItem>($"api/scouters/top?count={count}", ct);

    public async Task<ApiResult<ScouterProfile>> GetScouterProfileAsync(string username, CancellationToken ct = default)
        => await GetAsync<ScouterProfile>($"api/scouters/{Uri.EscapeDataString(username)}", ct);

    public async Task<ApiResult<object>> FollowScouterAsync(Guid scouterId, CancellationToken ct = default)
        => await PostAsync<object>($"api/scouters/{scouterId}/follow", null, ct);

    public async Task<ApiResult<object>> UnfollowScouterAsync(Guid scouterId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"api/scouters/{scouterId}/follow", ct);
        return await response.Content.ReadFromJsonAsync<ApiResult<object>>(ct) ?? new ApiResult<object> { IsSuccess = false };
    }

    public async Task<ApiListResult<ScouterItem>> GetMyFollowingAsync(CancellationToken ct = default)
        => await GetListAsync<ScouterItem>("api/scouters/me/following", ct);

    private async Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiResult<T>>(url, ct)
                   ?? new ApiResult<T> { IsSuccess = false };
        }
        catch { return new ApiResult<T> { IsSuccess = false }; }
    }

    private async Task<ApiListResult<T>> GetListAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            return await _http.GetFromJsonAsync<ApiListResult<T>>(url, ct)
                   ?? new ApiListResult<T> { IsSuccess = false };
        }
        catch { return new ApiListResult<T> { IsSuccess = false }; }
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object? body, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response;
            if (body is null)
                response = await _http.PostAsync(url, null, ct);
            else
                response = await _http.PostAsJsonAsync(url, body, ct);

            return await response.Content.ReadFromJsonAsync<ApiResult<T>>(ct)
                   ?? new ApiResult<T> { IsSuccess = false };
        }
        catch { return new ApiResult<T> { IsSuccess = false }; }
    }

    private static string BuildQueryString(params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");
        var qs = string.Join("&", parts);
        return string.IsNullOrEmpty(qs) ? "" : $"?{qs}";
    }
}
