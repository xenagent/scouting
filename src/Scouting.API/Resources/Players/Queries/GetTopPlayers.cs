using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Players.Queries;

public static class GetTopPlayers
{
    public class Request : Query<FeatureListResultModel<Response>>
    {
        public int Count { get; set; } = 10;
    }

    public class Response
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public string Position { get; set; } = default!;
        public string Team { get; set; } = default!;
        public string League { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public int Score { get; set; }
        public int AnalysisCount { get; set; }
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
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.Status == PlayerStatus.Approved)
                .OrderByDescending(p => p.Score)
                .Take(request.Count)
                .Select(p => new Response
                {
                    Id = p.Id,
                    Name = p.Name!,
                    Slug = p.Slug!,
                    Position = p.Position.ToString(),
                    Team = p.Team!,
                    League = p.League!,
                    ImageUrl = p.ImageUrl,
                    Score = p.Score,
                    AnalysisCount = _context.Analyses.Count(a =>
                        a.PlayerId == p.Id &&
                        a.Status == AnalysisEntity.AnalysisStatus.Approved)
                })
                .ToListAsync(ct);

            return FeatureListResultModel<Response>.Ok(players);
        }
    }
}
