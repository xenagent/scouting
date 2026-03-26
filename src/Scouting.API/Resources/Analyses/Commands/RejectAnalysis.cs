using FluentValidation;
using Microsoft.EntityFrameworkCore;
using scommon;
using Scouting.API.Infrastructure;
using Scouting.API.Services;
using Scouting.API.Shared;
using Scouting.API.Shared.Results;

namespace Scouting.API.Resources.Analyses.Commands;

public static class RejectAnalysis
{
    public class Request : Command<FeatureResultModel>
    {
        public Guid AnalysisId { get; set; }
        public string Reason { get; set; } = default!;
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.AnalysisId)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY);

            RuleFor(x => x.Reason)
                .NotEmpty().WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_EMPTY)
                .MaximumLength(512).WithErrorCode(ErrorCodes.COMMON_MESSAGE_VALUE_MAX_LENGHT_ERROR);
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

            var analysis = await _context.Analyses
                .FirstOrDefaultAsync(a => a.Id == request.AnalysisId, ct);

            if (analysis is null)
                return FeatureResultModel.NotFound();

            var result = analysis.Reject(request.Reason);
            if (!result.IsSuccess)
                return FeatureResultModel.Error(result.Messages!);

            await _context.SaveChangesAsync(ct);
            return FeatureResultModel.Ok();
        }
    }
}
