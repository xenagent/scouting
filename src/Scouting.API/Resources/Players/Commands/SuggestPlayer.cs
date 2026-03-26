using FluentValidation;
using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Domains.AnalysisEntity;
using Scouting.API.Domains.PlayerEntity;
using Scouting.API.Infrastructure;
using Scouting.API.Services;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Players.Commands;

public static class SuggestPlayer
{
    public class Request : Command<FeatureObjectResultModel<Response>>
    {
        public string Name { get; set; } = default!;
        public int Age { get; set; }
        public PlayerPosition Position { get; set; }
        public string Team { get; set; } = default!;
        public string League { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public string VideoUrl { get; set; } = default!;
        public string AnalysisContent { get; set; } = default!;
    }

    public class Response
    {
        public Guid PlayerId { get; set; }
        public string PlayerName { get; set; } = default!;
        public string Slug { get; set; } = default!;
        public Guid AnalysisId { get; set; }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MaximumLength(128).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);

            RuleFor(x => x.Age)
                .InclusiveBetween(13, 50).WithErrorCode(ErrorCodes.COMMON_MESSAGE_INVALID_VALUE);

            RuleFor(x => x.Team)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MaximumLength(128).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);

            RuleFor(x => x.League)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MaximumLength(128).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);

            RuleFor(x => x.Country)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MaximumLength(64).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);

            RuleFor(x => x.VideoUrl)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MaximumLength(512).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);

            RuleFor(x => x.AnalysisContent)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MinimumLength(50).WithErrorCode(ErrorCodes.ANALYSIS_CONTENT_TOO_SHORT)
                .MaximumLength(5000).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);
        }
    }

    public class Handler : ICommandHandler<Request, FeatureObjectResultModel<Response>>
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public Handler(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<FeatureObjectResultModel<Response>> HandleAsync(Request request, CancellationToken ct)
        {
            if (!_currentUser.IsAuthenticated)
                return FeatureObjectResultModel<Response>.Unauthorized();

            var playerResult = Player.Create(
                request.Name, request.Age, request.Position,
                request.Team, request.League, request.Country,
                _currentUser.Id);

            if (!playerResult.IsSuccess)
                return FeatureObjectResultModel<Response>.Error(playerResult.Messages!);

            var player = playerResult.Data!;

            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                player.SetImageUrl(request.ImageUrl);

            var analysisResult = Analysis.Create(
                player.Id, request.VideoUrl, request.AnalysisContent, _currentUser.Id);

            if (!analysisResult.IsSuccess)
                return FeatureObjectResultModel<Response>.Error(analysisResult.Messages!);

            await _context.Players.AddAsync(player, ct);
            await _context.Analyses.AddAsync(analysisResult.Data!, ct);
            await _context.SaveChangesAsync(ct);

            return FeatureObjectResultModel<Response>.Ok(new Response
            {
                PlayerId = player.Id,
                PlayerName = player.Name!,
                Slug = player.Slug!,
                AnalysisId = analysisResult.Data!.Id
            });
        }
    }
}
