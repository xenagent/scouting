using FluentValidation;
using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.AnalysisEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Services;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Scouters.Queries;

public static class GetScouterProfile
{
    public class Request : Query<FeatureObjectResultModel<Response>>
    {
        public string Username { get; set; } = default!;
        public Guid? ViewerUserId { get; set; }
    }

    public class Response
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public int ApprovedAnalysisCount { get; set; }
        public int FollowerCount { get; set; }
        public bool IsFollowedByViewer { get; set; }
        public List<RecentAnalysis> RecentAnalyses { get; set; } = [];

        public class RecentAnalysis
        {
            public Guid Id { get; set; }
            public string PlayerName { get; set; } = default!;
            public string PlayerSlug { get; set; } = default!;
            public string? PlayerImageUrl { get; set; }
            public string ContentPreview { get; set; } = default!;
            public decimal? AIScore { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Username)
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
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Username == request.Username)
                .Select(u => new Response
                {
                    Id = u.Id,
                    Username = u.Username!,
                    AvatarUrl = u.AvatarUrl,
                    Bio = u.Bio,
                    ApprovedAnalysisCount = u.ApprovedAnalysisCount,
                    FollowerCount = u.FollowerCount
                })
                .FirstOrDefaultAsync(ct);

            if (user is null)
                return FeatureObjectResultModel<Response>.NotFound();

            if (request.ViewerUserId.HasValue)
            {
                user.IsFollowedByViewer = await _context.ScouterFollows
                    .AnyAsync(f => f.FollowerId == request.ViewerUserId && f.ScouterId == user.Id, ct);
            }

            user.RecentAnalyses = await (
                from a in _context.Analyses.AsNoTracking()
                where a.CreatedUserId == user.Id && a.Status == AnalysisStatus.Approved
                join p in _context.Players.AsNoTracking() on a.PlayerId equals p.Id
                orderby a.CreatedTime descending
                select new Response.RecentAnalysis
                {
                    Id = a.Id,
                    PlayerName = p.Name!,
                    PlayerSlug = p.Slug!,
                    PlayerImageUrl = p.ImageUrl,
                    ContentPreview = a.Content!.Length > 150
                        ? a.Content.Substring(0, 150) + "..."
                        : a.Content,
                    AIScore = a.AIScore,
                    CreatedAt = a.CreatedTime
                })
                .Take(5)
                .ToListAsync(ct);

            return FeatureObjectResultModel<Response>.Ok(user);
        }
    }
}
