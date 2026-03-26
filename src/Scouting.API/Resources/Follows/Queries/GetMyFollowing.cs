using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Infrastructure;
using Scouting.API.Services;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Follows.Queries;

public static class GetMyFollowing
{
    public class Request : Query<FeatureListResultModel<Response>>
    {
    }

    public class Response
    {
        public Guid ScouterId { get; set; }
        public string Username { get; set; } = default!;
        public string? AvatarUrl { get; set; }
        public int ApprovedAnalysisCount { get; set; }
        public int FollowerCount { get; set; }
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
            if (!_currentUser.IsAuthenticated)
                return FeatureListResultModel<Response>.Ok([]);

            var following = await (
                from f in _context.ScouterFollows.AsNoTracking()
                where f.FollowerId == _currentUser.Id
                join u in _context.Users.AsNoTracking() on f.ScouterId equals u.Id
                select new Response
                {
                    ScouterId = u.Id,
                    Username = u.Username!,
                    AvatarUrl = u.AvatarUrl,
                    ApprovedAnalysisCount = u.ApprovedAnalysisCount,
                    FollowerCount = u.FollowerCount
                })
                .ToListAsync(ct);

            return FeatureListResultModel<Response>.Ok(following);
        }
    }
}
