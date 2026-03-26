using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.AnalysisEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Analyses.Queries;

public static class GetRecentAnalyses
{
    public class Request : Query<FeatureListResultModel<Response>>
    {
        public int Count { get; set; } = 10;
    }

    public class Response
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = default!;
        public string PlayerSlug { get; set; } = default!;
        public string? PlayerImageUrl { get; set; }
        public string? PlayerPosition { get; set; }
        public string VideoUrl { get; set; } = default!;
        public string ContentPreview { get; set; } = default!;
        public string? AISummary { get; set; }
        public decimal? AIScore { get; set; }
        public string ScoutUsername { get; set; } = default!;
        public Guid ScoutId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class Handler : IQueryHandler<Request, FeatureListResultModel<Response>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FeatureListResultModel<Response>> HandleAsync(Request request, CancellationToken ct)
        {
            var analyses = await (
                from a in _context.Analyses.AsNoTracking()
                where a.Status == AnalysisStatus.Approved
                join p in _context.Players.AsNoTracking() on a.PlayerId equals p.Id
                join u in _context.Users.AsNoTracking() on a.CreatedUserId equals u.Id into users
                from u in users.DefaultIfEmpty()
                orderby a.CreatedTime descending
                select new Response
                {
                    Id = a.Id,
                    PlayerId = p.Id,
                    PlayerName = p.Name!,
                    PlayerSlug = p.Slug!,
                    PlayerImageUrl = p.ImageUrl,
                    PlayerPosition = p.Position.ToString(),
                    VideoUrl = a.VideoUrl!,
                    ContentPreview = a.Content!.Length > 200
                        ? a.Content.Substring(0, 200) + "..."
                        : a.Content,
                    AISummary = a.AISummary,
                    AIScore = a.AIScore,
                    ScoutUsername = u != null ? u.Username! : "anonymous",
                    ScoutId = a.CreatedUserId ?? Guid.Empty,
                    CreatedAt = a.CreatedTime
                })
                .Take(request.Count)
                .ToListAsync(ct);

            return FeatureListResultModel<Response>.Ok(analyses);
        }
    }
}
