using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using scommon;
using Scouting.API.Resources.Analyses.Commands;
using Scouting.API.Resources.Analyses.Queries;
using Scouting.API.Shared.Results;

namespace Scouting.API.Controllers;

[Route("api/analyses")]
public class AnalysesController : BaseApiController
{
    private readonly IMediator _mediator;

    public AnalysesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("recent")]
    public async Task<FeatureListResultModel<GetRecentAnalyses.Response>> GetRecent(
        [FromQuery] int count = 10, CancellationToken ct = default)
        => await _mediator.FetchAsync(new GetRecentAnalyses.Request { Count = count }, ct);

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<FeatureResultModel> Approve(Guid id, CancellationToken ct)
        => await _mediator.SendAsync(new ApproveAnalysis.Request { AnalysisId = id }, ct);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<FeatureResultModel> Reject(Guid id, [FromBody] RejectAnalysis.Request request, CancellationToken ct)
    {
        request.AnalysisId = id;
        return await _mediator.SendAsync(request, ct);
    }
}
