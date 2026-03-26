using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Services;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Players.Queries;

public static class GetPendingPlayers
{
    public class Request : Query<FeatureListResultModel<Response>>
    {
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
        public string SuggestedByUsername { get; set; } = default!;
        public string VideoUrl { get; set; } = default!;
        public string AnalysisContent { get; set; } = default!;
        public decimal? AIScore { get; set; }
        public string? AISummary { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class Handler : IQueryHandler<Request, FeatureListResultModel<Response>>
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public Handler(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<FeatureListResultModel<Response>> HandleAsync(Request request, CancellationToken ct)
        {
            if (!_currentUser.IsAdmin)
                return FeatureListResultModel<Response>.Error([new scommon.MessageItem
                {
                    Code = Shared.ErrorCodes.COMMON_MESSAGE_FORBIDDEN
                }]);

            var result = await (
                from p in _context.Players.AsNoTracking()
                where p.Status == PlayerStatus.Pending
                join u in _context.Users.AsNoTracking() on p.CreatedUserId equals u.Id into users
                from u in users.DefaultIfEmpty()
                join a in _context.Analyses.AsNoTracking() on p.Id equals a.PlayerId into analyses
                from a in analyses.OrderByDescending(x => x.CreatedTime).Take(1).DefaultIfEmpty()
                orderby p.CreatedTime descending
                select new Response
                {
                    Id = p.Id,
                    Name = p.Name!,
                    Slug = p.Slug!,
                    Age = p.Age,
                    Position = p.Position.ToString(),
                    Team = p.Team!,
                    League = p.League!,
                    Country = p.Country!,
                    SuggestedByUsername = u != null ? u.Username! : "anonymous",
                    VideoUrl = a != null ? a.VideoUrl! : "",
                    AnalysisContent = a != null ? a.Content! : "",
                    AIScore = a != null ? a.AIScore : null,
                    AISummary = a != null ? a.AISummary : null,
                    SubmittedAt = p.CreatedTime
                })
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return FeatureListResultModel<Response>.Ok(result);
        }
    }
}
