using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Infrastructure;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Scouters.Queries;

public static class GetTopScouters
{
    public class Request : Query<FeatureListResultModel<Response>>
    {
        public int Count { get; set; } = 10;
    }

    public class Response
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public int ApprovedAnalysisCount { get; set; }
        public int FollowerCount { get; set; }
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
            var scouters = await _context.Users
                .AsNoTracking()
                .Where(u => u.ApprovedAnalysisCount > 0)
                .OrderByDescending(u => u.ApprovedAnalysisCount)
                .ThenByDescending(u => u.FollowerCount)
                .Take(request.Count)
                .Select(u => new Response
                {
                    Id = u.Id,
                    Username = u.Username!,
                    AvatarUrl = u.AvatarUrl,
                    Bio = u.Bio,
                    ApprovedAnalysisCount = u.ApprovedAnalysisCount,
                    FollowerCount = u.FollowerCount
                })
                .ToListAsync(ct);

            return FeatureListResultModel<Response>.Ok(scouters);
        }
    }
}
