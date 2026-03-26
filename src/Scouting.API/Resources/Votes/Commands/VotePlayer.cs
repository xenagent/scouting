using FluentValidation;
using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Domains.VoteEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Services;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Votes.Commands;

public static class VotePlayer
{
    public class Request : Command<FeatureResultModel>
    {
        public Guid PlayerId { get; set; }
        public VoteType VoteType { get; set; }
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
            if (!_currentUser.IsAuthenticated)
                return FeatureResultModel.Unauthorized();

            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId, ct);

            if (player is null)
                return FeatureResultModel.NotFound();

            if (player.Status != PlayerStatus.Approved)
                return FeatureResultModel.Error(new MessageItem
                {
                    Code = ErrorCodes.VOTE_PLAYER_NOT_APPROVED,
                    Property = nameof(player.Status),
                    Table = nameof(Player)
                });

            var existingVote = await _context.Votes
                .FirstOrDefaultAsync(v => v.PlayerId == request.PlayerId && v.UserId == _currentUser.Id, ct);

            if (existingVote is null)
            {
                var vote = Vote.Create(request.PlayerId, _currentUser.Id, request.VoteType);
                await _context.Votes.AddAsync(vote, ct);
                player.UpdateScore(request.VoteType == VoteType.Up ? 1 : -1);
            }
            else if (existingVote.VoteType != request.VoteType)
            {
                // Changing vote: reverse old, apply new
                player.UpdateScore(existingVote.VoteType == VoteType.Up ? -2 : 2);
                existingVote.ChangeVote(request.VoteType);
            }
            // Same vote = no-op (idempotent)

            await _context.SaveChangesAsync(ct);
            return FeatureResultModel.Ok();
        }
    }
}
