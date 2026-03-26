using FluentValidation;
using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.ScouterFollowEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Services;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Follows.Commands;

public static class UnfollowScouter
{
    public class Request : Command<FeatureResultModel>
    {
        public Guid ScouterId { get; set; }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ScouterId)
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

            var follow = await _context.ScouterFollows
                .FirstOrDefaultAsync(f => f.FollowerId == _currentUser.Id && f.ScouterId == request.ScouterId, ct);

            if (follow is null)
                return FeatureResultModel.Error(new MessageItem
                {
                    Code = ErrorCodes.FOLLOW_NOT_FOLLOWING,
                    Property = nameof(request.ScouterId),
                    Table = nameof(ScouterFollow)
                });

            var scouter = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.ScouterId, ct);

            _context.ScouterFollows.Remove(follow);
            scouter?.DecrementFollowerCount();
            await _context.SaveChangesAsync(ct);

            return FeatureResultModel.Ok();
        }
    }
}
