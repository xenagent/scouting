using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Players.Queries;

public static class GetPlayerList
{
    public class Request : Query<FeatureListResultModel<Response>>
    {
        public string? Search { get; set; }
        public PlayerPosition? Position { get; set; }
        public string? League { get; set; }
        public string? Country { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class Response
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public int Age { get; set; }
        public string Position { get; set; } = default!;
        public string Team { get; set; } = default!;
        public string League { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public int Score { get; set; }
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
            var query = _context.Players
                .AsNoTracking()
                .Where(p => p.Status == PlayerStatus.Approved);

            if (!string.IsNullOrWhiteSpace(request.Search))
                query = query.Where(p =>
                    p.Name!.ToLower().Contains(request.Search.ToLower()) ||
                    p.Team!.ToLower().Contains(request.Search.ToLower()));

            if (request.Position.HasValue)
                query = query.Where(p => p.Position == request.Position.Value);

            if (!string.IsNullOrWhiteSpace(request.League))
                query = query.Where(p => p.League == request.League);

            if (!string.IsNullOrWhiteSpace(request.Country))
                query = query.Where(p => p.Country == request.Country);

            var players = await query
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.CreatedTime)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new Response
                {
                    Id = p.Id,
                    Name = p.Name!,
                    Slug = p.Slug!,
                    Age = p.Age,
                    Position = p.Position.ToString(),
                    Team = p.Team!,
                    League = p.League!,
                    Country = p.Country!,
                    ImageUrl = p.ImageUrl,
                    Score = p.Score
                })
                .ToListAsync(ct);

            return FeatureListResultModel<Response>.Ok(players);
        }
    }
}
