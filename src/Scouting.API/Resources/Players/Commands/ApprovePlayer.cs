using FluentValidation;
using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Domains.UserEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Services;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Players.Commands;

public static class ApprovePlayer
{
    public class Request : Command<FeatureResultModel>
    {
        public Guid PlayerId { get; set; }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.PlayerId)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY);
        }
    }

    public class Handler : ICommandHandler<Request, FeatureResultModel>
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public Handler(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<FeatureResultModel> HandleAsync(Request request, CancellationToken ct)
        {
            if (!_currentUser.IsAdmin)
                return FeatureResultModel.Forbidden();

            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId, ct);

            if (player is null)
                return FeatureResultModel.NotFound();

            // Also approve all pending analyses for this player
            var pendingAnalyses = await _context.Analyses
                .Where(a => a.PlayerId == request.PlayerId && a.Status == AnalysisEntity.AnalysisStatus.Pending)
                .ToListAsync(ct);

            var approveResult = player.Approve();
            if (!approveResult.IsSuccess)
                return FeatureResultModel.Error(approveResult.Messages!);

            foreach (var analysis in pendingAnalyses)
            {
                analysis.Approve();

                // Increment scout's approved analysis count
                var scout = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == analysis.CreatedUserId, ct);
                scout?.IncrementApprovedAnalysisCount();
            }

            await _context.SaveChangesAsync(ct);
            return FeatureResultModel.Ok();
        }
    }
}
