using FluentValidation;
using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Players.Queries;

public static class GetPlayerDetail
{
    public class Request : Query<FeatureObjectResultModel<Response>>
    {
        public string Slug { get; set; } = default!;
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
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public string SuggestedByUsername { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
        public List<AnalysisItem> Analyses { get; set; } = [];

        public class AnalysisItem
        {
            public Guid Id { get; set; }
            public string VideoUrl { get; set; } = default!;
            public string Content { get; set; } = default!;
            public string? AISummary { get; set; }
            public decimal? AIScore { get; set; }
            public string ScoutUsername { get; set; } = default!;
            public Guid ScoutId { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY);
        }
    }

    public class Handler : IQueryHandler<Request, FeatureObjectResultModel<Response>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FeatureObjectResultModel<Response>> HandleAsync(Request request, CancellationToken ct)
        {
            var player = await _context.Players
                .AsNoTracking()
                .Where(p => p.Slug == request.Slug && p.Status == PlayerStatus.Approved)
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
                    Score = p.Score,
                    CreatedAt = p.CreatedTime
                })
                .FirstOrDefaultAsync(ct);

            if (player is null)
                return FeatureObjectResultModel<Response>.NotFound();

            // Analyses
            player.Analyses = await _context.Analyses
                .AsNoTracking()
                .Where(a => a.PlayerId == player.Id && a.Status == AnalysisEntity.AnalysisStatus.Approved)
                .OrderByDescending(a => a.AIScore)
                .ThenByDescending(a => a.CreatedTime)
                .Join(_context.Users.AsNoTracking(),
                    a => a.CreatedUserId,
                    u => u.Id,
                    (a, u) => new Response.AnalysisItem
                    {
                        Id = a.Id,
                        VideoUrl = a.VideoUrl!,
                        Content = a.Content!,
                        AISummary = a.AISummary,
                        AIScore = a.AIScore,
                        ScoutUsername = u.Username!,
                        ScoutId = u.Id,
                        CreatedAt = a.CreatedTime
                    })
                .ToListAsync(ct);

            // Vote counts
            var votes = await _context.Votes
                .AsNoTracking()
                .Where(v => v.PlayerId == player.Id)
                .GroupBy(v => v.VoteType)
                .Select(g => new { VoteType = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            player.UpvoteCount = votes.FirstOrDefault(v => v.VoteType == VoteEntity.VoteType.Up)?.Count ?? 0;
            player.DownvoteCount = votes.FirstOrDefault(v => v.VoteType == VoteEntity.VoteType.Down)?.Count ?? 0;

            // Suggested by
            var suggestedBy = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == (Guid?)_context.Players
                    .Where(p => p.Slug == request.Slug)
                    .Select(p => p.CreatedUserId)
                    .FirstOrDefault())
                .Select(u => u.Username)
                .FirstOrDefaultAsync(ct);

            player.SuggestedByUsername = suggestedBy ?? "anonymous";

            return FeatureObjectResultModel<Response>.Ok(player);
        }
    }
}
